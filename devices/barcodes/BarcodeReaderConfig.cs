using Devices.Interfaces;

namespace Devices.Barcodes;

public class BarcodeReaderConfig : ISerialDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public string PortName { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public string LineEnding { get; set; } = "\r\n";

    public BarcodeConnectionMode ConnectionMode { get; set; } = BarcodeConnectionMode.SerialPort;
}
