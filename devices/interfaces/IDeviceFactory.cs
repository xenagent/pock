namespace Devices.Interfaces;

public interface IDeviceFactory
{
    IHardwareDevice Create(IDeviceConfig config);
}
