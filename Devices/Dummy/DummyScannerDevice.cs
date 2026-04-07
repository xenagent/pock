using System.Threading.Channels;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Dummy;

public sealed partial class DummyScannerDevice(ILogger<DummyScannerDevice> logger) : IScannerDevice
{
    private readonly Channel<BarcodeScanResult> _channel = Channel.CreateBounded<BarcodeScanResult>(32);
    private readonly string[] _barcodes = ["8690000000001", "8690000000002", "8690000000003", "8690000000004"];
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private bool _enabled;

    public string DeviceId => "DUMMY-SCANNER-001";
    public string DeviceName => "Dummy Barcode Scanner";
    public ChannelReader<BarcodeScanResult> Scans => _channel.Reader;

    public Task ConnectAsync(CancellationToken ct = default)
    {
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

    public Task EnableAsync(CancellationToken ct = default)
    {
        if (_enabled) return Task.CompletedTask;

        _enabled = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _simulationTask = SimulateScansAsync(_cts.Token);
        LogEnabled(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken ct = default)
    {
        _enabled = false;
        _cts?.Cancel();
        LogDisabled(logger, DeviceName);
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

    private async Task SimulateScansAsync(CancellationToken ct)
    {
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(random.Next(2, 6)), ct).ConfigureAwait(false);
            var barcode = _barcodes[random.Next(_barcodes.Length)];
            var result = new BarcodeScanResult(barcode, BarcodeSymbology.EAN13, DateTimeOffset.UtcNow);
            await _channel.Writer.WriteAsync(result, ct).ConfigureAwait(false);
            LogScanned(logger, DeviceName, barcode);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Connected")]
    private static partial void LogConnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Disconnected")]
    private static partial void LogDisconnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Enabled")]
    private static partial void LogEnabled(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Disabled")]
    private static partial void LogDisabled(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Scanned: {Barcode}")]
    private static partial void LogScanned(ILogger logger, string deviceName, string barcode);
}
