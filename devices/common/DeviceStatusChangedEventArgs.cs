namespace Devices.Common;

public class DeviceStatusChangedEventArgs : EventArgs
{
    public string DeviceId { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public DeviceStatus OldStatus { get; init; } = DeviceStatus.NotInitialized;
    public DeviceStatus NewStatus { get; init; } = DeviceStatus.NotInitialized;
}
