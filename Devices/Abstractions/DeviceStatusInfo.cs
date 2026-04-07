namespace KoopPOS.Checkout.Agent.Devices.Abstractions;

public enum DeviceState
{
    Disconnected,
    Connecting,
    Ready,
    Busy,
    Error,
    Offline
}

public sealed record DeviceStatusInfo(
    DeviceState State,
    string? ErrorMessage = null,
    DateTimeOffset LastSeen = default);
