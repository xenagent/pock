using Devices.Interfaces;

namespace Devices.Scales;

public class ScaleConfig : ISerialDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public string PortName { get; set; } = "COM5";
    public int BaudRate { get; set; } = 9600;
    public string LineEnding { get; set; } = "\r\n";

    public ScaleProtocol Protocol { get; set; } = ScaleProtocol.Standard;
    public string WeightUnit { get; set; } = "kg";
}
