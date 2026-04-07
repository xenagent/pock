using System.Threading.Channels;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Dummy;

public sealed partial class DummyScaleDevice(ILogger<DummyScaleDevice> logger) : IScaleDevice
{
    private readonly Channel<WeightReading> _channel = Channel.CreateBounded<WeightReading>(32);
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private decimal _tare;

    public string DeviceId => "DUMMY-SCALE-001";
    public string DeviceName => "Dummy Scale";
    public ChannelReader<WeightReading> Weights => _channel.Reader;

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _simulationTask = SimulateWeightsAsync(_cts.Token);
        LogConnected(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _channel.Writer.TryComplete();
        LogDisconnected(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task<DeviceStatusInfo> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new DeviceStatusInfo(DeviceState.Ready, LastSeen: DateTimeOffset.UtcNow));

    public Task ZeroAsync(CancellationToken ct = default)
    {
        _tare = 0;
        LogZeroed(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task TareAsync(CancellationToken ct = default)
    {
        _tare = 50m;
        LogTared(logger, DeviceName, _tare);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _channel.Writer.TryComplete();
        if (_simulationTask is not null)
            await _simulationTask.ConfigureAwait(false);
        _cts?.Dispose();
    }

    private async Task SimulateWeightsAsync(CancellationToken ct)
    {
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(random.Next(3, 8)), ct).ConfigureAwait(false);
            var rawWeight = random.Next(100, 2500);
            var weight = rawWeight - _tare;
            var reading = new WeightReading(weight, IsStable: true, DateTimeOffset.UtcNow);
            await _channel.Writer.WriteAsync(reading, ct).ConfigureAwait(false);
            LogWeight(logger, DeviceName, weight);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Connected")]
    private static partial void LogConnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Disconnected")]
    private static partial void LogDisconnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Zeroed")]
    private static partial void LogZeroed(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Tared at {Tare}g")]
    private static partial void LogTared(ILogger logger, string deviceName, decimal tare);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Weight: {Weight}g")]
    private static partial void LogWeight(ILogger logger, string deviceName, decimal weight);
}
