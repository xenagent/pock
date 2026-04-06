namespace Devices.Common;

public sealed class DeviceStatus : Enumeration
{
    public static readonly DeviceStatus NotInitialized = new(0, "NotInitialized");
    public static readonly DeviceStatus Disconnected   = new(1, "Disconnected");
    public static readonly DeviceStatus Connecting     = new(2, "Connecting");
    public static readonly DeviceStatus Connected      = new(3, "Connected");
    public static readonly DeviceStatus Error          = new(4, "Error");
    public static readonly DeviceStatus Reconnecting   = new(5, "Reconnecting");

    private DeviceStatus(int id, string name) : base(id, name) { }

    public bool IsConnected  => this == Connected;
    public bool CanReconnect => this == Error || this == Disconnected;
}
