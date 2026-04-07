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
/// Kullanım:
///   await manager.StartAsync("STORE-001", "KASA-01");
///   manager.Scanner?.Scans   → ChannelReader&lt;BarcodeScanResult&gt;
///   manager.Scale?.Weights   → ChannelReader&lt;WeightReading&gt;
///   manager.POS              → IPOSDevice (TCP/IP, Ingenico)
/// </summary>
public sealed partial class DeviceManager(
    IDeviceConfigProvider configProvider,
    ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private readonly List<IDevice> _devices = [];

    // ── Typed erişim ──────────────────────────────────────────────────
    public IScannerDevice? Scanner => _devices.OfType<IScannerDevice>().FirstOrDefault();
    public IScaleDevice?   Scale   => _devices.OfType<IScaleDevice>().FirstOrDefault();
    public IPOSDevice?     POS     => _devices.OfType<IPOSDevice>().FirstOrDefault();
    public IPrinterDevice? Printer => _devices.OfType<IPrinterDevice>().FirstOrDefault();

    public IReadOnlyList<IDevice> All => _devices;

    // ── Başlatma ──────────────────────────────────────────────────────

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
                LogStarted(loggerFactory.CreateLogger<DeviceManager>(), deviceCfg.DeviceName, deviceCfg.DeviceId);
            }
            catch (Exception ex)
            {
                LogFailed(loggerFactory.CreateLogger<DeviceManager>(), deviceCfg.DeviceName, ex.Message);
            }
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var device in _devices)
        {
            try   { await device.DisconnectAsync(ct); }
            catch (Exception ex)
            {
                LogStopError(loggerFactory.CreateLogger<DeviceManager>(), device.DeviceName, ex.Message);
            }
        }
    }

    public async Task<IReadOnlyDictionary<string, DeviceStatusInfo>> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, DeviceStatusInfo>();
        foreach (var d in _devices)
            result[d.DeviceId] = await d.GetStatusAsync(ct);
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var d in _devices)
            await d.DisposeAsync();
        _devices.Clear();
    }

    // ── Factory ───────────────────────────────────────────────────────

    private IDevice? CreateDevice(IDeviceConfig cfg) => cfg switch
    {
        BarcodeReaderConfig bc =>
            new SerialScannerDevice(bc, loggerFactory.CreateLogger<SerialScannerDevice>()),

        ScaleConfig sc =>
            new SerialScaleDevice(sc, loggerFactory.CreateLogger<SerialScaleDevice>()),

        PosTerminalConfig pc =>
            new IngenicoDevice(pc, loggerFactory.CreateLogger<IngenicoDevice>()),

        _ => LogUnknown(cfg.GetType().Name)
    };

    private IDevice? LogUnknown(string typeName)
    {
        LogUnsupported(loggerFactory.CreateLogger<DeviceManager>(), typeName);
        return null;
    }

    [LoggerMessage(Level = LogLevel.Information,  Message = "[DeviceManager] {Name} ({Id}) başlatıldı")]
    private static partial void LogStarted(ILogger l, string name, string id);

    [LoggerMessage(Level = LogLevel.Warning,      Message = "[DeviceManager] {Name} başlatılamadı: {Error}")]
    private static partial void LogFailed(ILogger l, string name, string error);

    [LoggerMessage(Level = LogLevel.Warning,      Message = "[DeviceManager] {Name} durdurulurken hata: {Error}")]
    private static partial void LogStopError(ILogger l, string name, string error);

    [LoggerMessage(Level = LogLevel.Warning,      Message = "[DeviceManager] Bilinmeyen cihaz tipi: {Type}")]
    private static partial void LogUnsupported(ILogger l, string type);
}
