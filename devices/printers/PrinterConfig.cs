using Devices.Interfaces;

namespace Devices.Printers;

public class PrinterConfig : ISerialDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public string PortName { get; set; } = "COM4";
    public int BaudRate { get; set; } = 9600;
    public string LineEnding { get; set; } = "\n";

    /// <summary>Fiş genişliği (karakter sayısı). 80mm kağıt için 48.</summary>
    public int LineWidth { get; set; } = 48;
}
