using System.Text.Json.Serialization;

namespace Devices.Config;

/// <summary>
/// API'den gelen ham cihaz tanımı.
/// </summary>
public class DeviceConfigDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = "";

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = ""; // Barcode | Scale | Pos | Printer

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("portName")]
    public string PortName { get; set; } = "";

    [JsonPropertyName("baudRate")]
    public int BaudRate { get; set; } = 9600;

    // Barcode
    [JsonPropertyName("connectionMode")]
    public string? ConnectionMode { get; set; }

    // Scale
    [JsonPropertyName("scaleProtocol")]
    public string? ScaleProtocol { get; set; }

    [JsonPropertyName("weightUnit")]
    public string? WeightUnit { get; set; }

    // Pos
    [JsonPropertyName("dllPath")]
    public string? DllPath { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }

    [JsonPropertyName("merchantId")]
    public string? MerchantId { get; set; }

    // Printer
    [JsonPropertyName("lineWidth")]
    public int LineWidth { get; set; } = 48;
}
