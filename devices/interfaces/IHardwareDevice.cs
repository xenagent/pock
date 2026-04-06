using Devices.Common;

namespace Devices.Interfaces;

public interface IHardwareDevice : IDisposable
{
    string DeviceId { get; }
    DeviceStatus Status { get; }

    Task InitializeAsync(IDeviceConfig config);
    Task ConnectAsync();
    Task DisconnectAsync();
}
