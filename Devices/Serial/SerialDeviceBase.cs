using System.IO.Ports;
using System.Text;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Serial;

/// <summary>
/// Serial port üzerinden haberleşen tüm cihazların ortak altyapısı.
/// Buffer yönetimi, satır ayrıştırma ve otomatik yeniden bağlanma burada.
/// </summary>
public abstract partial class SerialDeviceBase : IDevice
{
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();
    private ISerialDeviceConfig? _config;
    private CancellationTokenSource? _reconnectCts;
    private int _reconnectAttempt;
    private DeviceState _state = DeviceState.Disconnected;

    protected ILogger Logger { get; }

    public abstract string DeviceId { get; }
    public abstract string DeviceName { get; }

    protected SerialDeviceBase(ILogger logger) => Logger = logger;

    public Task<DeviceStatusInfo> GetStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new DeviceStatusInfo(_state, LastSeen: DateTimeOffset.UtcNow));

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (_config is null)
            throw new InvalidOperationException($"[{DeviceName}] InitializeAsync henüz çağrılmadı.");

        SetState(DeviceState.Connecting);
        _reconnectAttempt = 0;

        try
        {
            OpenPort();
        }
        catch (Exception ex)
        {
            LogConnectFailed(Logger, DeviceName, ex.Message);
            SetState(DeviceState.Error, ex.Message);
            _ = TryReconnectAsync(CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _reconnectCts?.Cancel();
        ClosePort();
        SetState(DeviceState.Disconnected);
        return Task.CompletedTask;
    }

    /// <summary>Alt sınıf, konfigürasyonu bu yolla okur.</summary>
    protected void Configure(ISerialDeviceConfig config) => _config = config;

    /// <summary>Serial porta ham veri yazar (terazi komutları vb.).</summary>
    protected void SendCommand(string command)
    {
        if (_port?.IsOpen != true)
            throw new InvalidOperationException($"[{DeviceName}] Port açık değil.");
        _port.Write(command);
    }

    /// <summary>Gelen satır alt sınıf tarafından işlenir.</summary>
    protected abstract void ProcessLine(string line);

    // ── Port yönetimi ─────────────────────────────────────────────────

    private void OpenPort()
    {
        _port = new SerialPort
        {
            PortName  = _config!.PortName,
            BaudRate  = _config.BaudRate,
            Parity    = _config.Parity,
            DataBits  = _config.DataBits,
            StopBits  = _config.StopBits,
            Handshake = _config.Handshake,
            Encoding  = Encoding.ASCII,
            ReadTimeout  = 3000,
            WriteTimeout = 3000
        };
        _port.DataReceived  += OnDataReceived;
        _port.ErrorReceived += OnErrorReceived;
        _port.Open();

        SetState(DeviceState.Ready);
        LogConnected(Logger, DeviceName, _config.PortName);
    }

    private void ClosePort()
    {
        if (_port is null) return;
        _port.DataReceived  -= OnDataReceived;
        _port.ErrorReceived -= OnErrorReceived;
        try { if (_port.IsOpen) _port.Close(); } catch { /* ignore */ }
        _port.Dispose();
        _port = null;
        _buffer.Clear();
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_state != DeviceState.Ready || _port is null) return;
        try
        {
            _buffer.Append(_port.ReadExisting());
            var ending = _config!.LineEnding;

            while (_buffer.ToString().Contains(ending))
            {
                var str = _buffer.ToString();
                var idx = str.IndexOf(ending, StringComparison.Ordinal);
                var line = str[..idx].Trim();
                _buffer.Remove(0, idx + ending.Length);

                if (!string.IsNullOrEmpty(line))
                    ProcessLine(line);
            }
        }
        catch (Exception ex)
        {
            LogReadError(Logger, DeviceName, ex.Message);
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        LogPortError(Logger, DeviceName, e.EventType.ToString());
        SetState(DeviceState.Error, e.EventType.ToString());
        _ = TryReconnectAsync(CancellationToken.None);
    }

    private async Task TryReconnectAsync(CancellationToken ct)
    {
        if (_config is null) return;

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _reconnectCts.Token);

        while (_reconnectAttempt < _config.MaxReconnectAttempts && !linked.Token.IsCancellationRequested)
        {
            _reconnectAttempt++;
            SetState(DeviceState.Connecting);
            LogReconnecting(Logger, DeviceName, _reconnectAttempt, _config.MaxReconnectAttempts);

            await Task.Delay(_config.ReconnectIntervalMs, linked.Token).ConfigureAwait(false);

            try
            {
                ClosePort();
                OpenPort();
                return;
            }
            catch { /* bir sonraki deneme */ }
        }

        if (_state != DeviceState.Ready)
            SetState(DeviceState.Error, $"Yeniden bağlanma başarısız ({_config.MaxReconnectAttempts} deneme).");
    }

    private void SetState(DeviceState state, string? error = null) => _state = state;

    public async ValueTask DisposeAsync()
    {
        _reconnectCts?.Cancel();
        ClosePort();
        _reconnectCts?.Dispose();
        await ValueTask.CompletedTask;
    }

    // ── Log mesajları ─────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,  Message = "[{DeviceName}] Bağlandı → {Port}")]
    private static partial void LogConnected(ILogger l, string deviceName, string port);

    [LoggerMessage(Level = LogLevel.Warning,      Message = "[{DeviceName}] Bağlantı hatası: {Error}")]
    private static partial void LogConnectFailed(ILogger l, string deviceName, string error);

    [LoggerMessage(Level = LogLevel.Debug,        Message = "[{DeviceName}] Serial okuma hatası: {Error}")]
    private static partial void LogReadError(ILogger l, string deviceName, string error);

    [LoggerMessage(Level = LogLevel.Warning,      Message = "[{DeviceName}] Port hatası: {Error}")]
    private static partial void LogPortError(ILogger l, string deviceName, string error);

    [LoggerMessage(Level = LogLevel.Information,  Message = "[{DeviceName}] Yeniden bağlanıyor ({Attempt}/{Max})...")]
    private static partial void LogReconnecting(ILogger l, string deviceName, int attempt, int max);
}
