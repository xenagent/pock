namespace Devices.Interfaces;

public interface ISerialDeviceConfig : IDeviceConfig
{
    string PortName { get; set; }
    int BaudRate { get; set; }
    string LineEnding { get; set; }
}
