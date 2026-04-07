using System.Threading.Channels;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Serial.Scanner;

/// <summary>
/// Serial port üzerinden bağlanan barkod okuyucu.
/// Okunan barkodlar <see cref="Scans"/> kanalından tüketilir.
/// </summary>
public sealed partial class SerialScannerDevice(
    BarcodeReaderConfig config,
    ILogger<SerialScannerDevice> logger) : SerialDeviceBase(logger), IScannerDevice
{
    private readonly Channel<BarcodeScanResult> _channel =
        Channel.CreateBounded<BarcodeScanResult>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    private bool _enabled;

    public override string DeviceId   => config.DeviceId;
    public override string DeviceName => config.DeviceName;

    public ChannelReader<BarcodeScanResult> Scans => _channel.Reader;

    public override Task ConnectAsync(CancellationToken ct = default)
    {
        Configure(config);
        return base.ConnectAsync(ct);
    }

    public Task EnableAsync(CancellationToken ct = default)
    {
        _enabled = true;
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken ct = default)
    {
        _enabled = false;
        return Task.CompletedTask;
    }

    protected override void ProcessLine(string line)
    {
        if (!_enabled) return;

        var result = new BarcodeScanResult(line, DetectSymbology(line), DateTimeOffset.UtcNow);
        _channel.Writer.TryWrite(result);
        LogScanned(Logger, DeviceName, line);
    }

    public override async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await base.DisposeAsync();
    }

    private static BarcodeSymbology DetectSymbology(string barcode) => barcode.Length switch
    {
        13 => BarcodeSymbology.EAN13,
        8  => BarcodeSymbology.EAN8,
        12 => BarcodeSymbology.UpcA,
        _  => BarcodeSymbology.Unknown
    };

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Barkod okundu: {Barcode}")]
    private static partial void LogScanned(ILogger l, string deviceName, string barcode);
}
