namespace Devices.Interfaces;

public interface IDeviceConfig
{
    string DeviceId { get; set; }
    string DeviceName { get; set; }
    bool Enabled { get; set; }
}
