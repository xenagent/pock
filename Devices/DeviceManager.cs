using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Printer;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;
using KoopPOS.Checkout.Agent.Devices.Config;
using KoopPOS.Checkout.Agent.Devices.Ingenico;
using KoopPOS.Checkout.Agent.Devices.Serial.Scale;
using KoopPOS.Checkout.Agent.Devices.Serial.Scanner;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices;

/// <summary>
/// Tüm donanım cihazlarının orkestratörü.
///
/// Başlatma:
///   await manager.StartAsync("STORE-001", "KASA-01");
///
/// Erişim:
///   manager.Scanner?.Scans  (ChannelReader)
///   manager.Scale?.Weights  (ChannelReader)
///   manager.POS             (IPOSDevice)
///   manager.Printer         (IPrinterDevice)
/// </summary>
public sealed partial class DeviceManager(
    IDeviceConfigProvider configProvider,
    IImpProLibrary? impProLibrary,
    ILogger<DeviceManager> logger) : IAsyncDisposable
{
    private readonly List<IDevice> _devices = [];

    // ── Typed erişim ──────────────────────────────────────────────────
    public IScannerDevice?  Scanner => _devices.OfType<IScannerDevice>().FirstOrDefault();
    public IScaleDevice?    Scale   => _devices.OfType<IScaleDevice>().FirstOrDefault();
    public IPOSDevice?      POS     => _devices.OfType<IPOSDevice>().FirstOrDefault();
    public IPrinterDevice?  Printer => _devices.OfType<IPrinterDevice>().FirstOrDefault();

    public IReadOnlyList<IDevice> All => _devices;

    // ── Başlatma ──────────────────────────────────────────────────────

    /// <summary>API'den konfigürasyonu çeker, tüm aktif cihazları oluşturup bağlar.</summary>
    public async Task StartAsync(string storeId, string registerId, CancellationToken ct = default)
    {
        var config = await configProvider.GetConfigurationAsync(storeId, registerId, ct);

        foreach (var deviceCfg in config.Devices.Where(d => d.Enabled))
        {
            try
            {
                var device = CreateDevice(deviceCfg);
                if (device is null) continue;

                await device.ConnectAsync(ct);
                _devices.Add(device);

                LogDeviceStarted(logger, deviceCfg.DeviceName, deviceCfg.DeviceId);
            }
            catch (Exception ex)
            {
                LogDeviceFailed(logger, deviceCfg.DeviceName, ex.Message);
            }
        }
    }

    /// <summary>Tüm cihazları bağlantıyı keser.</summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var device in _devices)
        {
            try   { await device.DisconnectAsync(ct); }
            catch (Exception ex) { LogDeviceStopError(logger, device.DeviceName, ex.Message); }
        }
    }

    /// <summary>Her cihazın anlık durumunu döner.</summary>
    public async Task<IReadOnlyDictionary<string, DeviceStatusInfo>> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, DeviceStatusInfo>();
        foreach (var device in _devices)
            result[device.DeviceId] = await device.GetStatusAsync(ct);
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var device in _devices)
            await device.DisposeAsync();
        _devices.Clear();
    }

    // ── Factory ───────────────────────────────────────────────────────

    private IDevice? CreateDevice(IDeviceConfig cfg) => cfg switch
    {
        BarcodeReaderConfig bc => new SerialScannerDevice(bc,
            logger.CreateLogger<SerialScannerDevice>()),

        ScaleConfig sc => new SerialScaleDevice(sc,
            logger.CreateLogger<SerialScaleDevice>()),

        PosTerminalConfig pc => new IngenicoDevice(pc,
            impProLibrary ?? new ImpProLibrary(),
            logger.CreateLogger<IngenicoDevice>()),

        _ => LogUnsupported(logger, cfg.GetType().Name)
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "[DeviceManager] {DeviceName} ({DeviceId}) başlatıldı")]
    private static partial void LogDeviceStarted(ILogger l, string deviceName, string deviceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[DeviceManager] {DeviceName} başlatılamadı: {Error}")]
    private static partial void LogDeviceFailed(ILogger l, string deviceName, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[DeviceManager] {DeviceName} durdurulurken hata: {Error}")]
    private static partial void LogDeviceStopError(ILogger l, string deviceName, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[DeviceManager] Bilinmeyen cihaz tipi: {ConfigType}")]
    private static partial void LogUnsupportedMsg(ILogger l, string configType);

    private IDevice? LogUnsupported(ILogger l, string configType)
    {
        LogUnsupportedMsg(l, configType);
        return null;
    }
}

/// <summary>ILogger extension — <see cref="DeviceManager.CreateDevice"/> içinde kullanılır.</summary>
file static class LoggerExtensions
{
    public static ILogger<T> CreateLogger<T>(this ILogger logger)
    {
        if (logger is ILoggerFactory factory) return factory.CreateLogger<T>();
        // Fallback: ILogger'ı T için sarar
        return new TypedLogger<T>(logger);
    }

    private sealed class TypedLogger<T>(ILogger inner) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
        public bool IsEnabled(LogLevel level) => inner.IsEnabled(level);
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> fmt)
            => inner.Log(level, id, state, ex, fmt);
    }
}
