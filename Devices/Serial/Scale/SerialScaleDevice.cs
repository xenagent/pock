using System.Globalization;
using System.Threading.Channels;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Serial.Scale;

/// <summary>
/// Serial port üzerinden bağlanan şarküteri terazisi.
/// Ağırlık okumaları <see cref="Weights"/> kanalından tüketilir.
/// <see cref="WeightReading.WeightInGrams"/> birimi gramdır.
/// </summary>
public sealed partial class SerialScaleDevice(
    ScaleConfig config,
    ILogger<SerialScaleDevice> logger) : SerialDeviceBase(logger), IScaleDevice
{
    private readonly Channel<WeightReading> _channel =
        Channel.CreateBounded<WeightReading>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public override string DeviceId   => config.DeviceId;
    public override string DeviceName => config.DeviceName;

    public ChannelReader<WeightReading> Weights => _channel.Reader;

    public override Task ConnectAsync(CancellationToken ct = default)
    {
        Configure(config);
        return base.ConnectAsync(ct);
    }

    protected override void ProcessLine(string line)
    {
        var reading = Parse(line);
        if (reading is null) return;

        _channel.Writer.TryWrite(reading);
        LogWeight(Logger, DeviceName, reading.WeightInGrams, reading.IsStable);
    }

    public Task ZeroAsync(CancellationToken ct = default)
    {
        SendCommand("Z\r\n");
        return Task.CompletedTask;
    }

    public Task TareAsync(CancellationToken ct = default)
    {
        SendCommand("T\r\n");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sürekli mod kapalıysa teraziye anlık tartım komutu gönderir ve cevabı bekler.
    /// </summary>
    public async Task<WeightReading> RequestWeightAsync(CancellationToken ct = default)
    {
        SendCommand(config.RequestWeightCommand);
        return await _channel.Reader.ReadAsync(ct);
    }

    public override async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await base.DisposeAsync();
    }

    // ── Parser ────────────────────────────────────────────────────────

    private WeightReading? Parse(string raw)
    {
        try
        {
            var isStable = !raw.Contains("US", StringComparison.OrdinalIgnoreCase);

            var numeric = new string(raw.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray())
                .Replace(',', '.');

            if (!decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return null;

            // Protokol birimine göre gram'a çevir
            var grams = config.Protocol switch
            {
                ScaleProtocol.Standard => ToGrams(raw, value),
                ScaleProtocol.Mettler  => ToGrams(raw, value),
                _                      => value * 1000 // varsayılan kg → gram
            };

            return new WeightReading(grams, isStable, DateTimeOffset.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    private static decimal ToGrams(string raw, decimal value)
    {
        if (raw.Contains("kg", StringComparison.OrdinalIgnoreCase))  return value * 1000;
        if (raw.Contains('g') && !raw.Contains("kg"))                 return value;
        // Ondalık basamak sayısından birim tahmini
        return value < 10 ? value * 1000 : value;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Ağırlık: {Weight}g (kararlı={Stable})")]
    private static partial void LogWeight(ILogger l, string deviceName, decimal weight, bool stable);
}
