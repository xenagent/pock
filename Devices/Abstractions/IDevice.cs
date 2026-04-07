namespace KoopPOS.Checkout.Agent.Devices.Abstractions;

public interface IDevice : IAsyncDisposable
{
    string DeviceId { get; }
    string DeviceName { get; }
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<DeviceStatusInfo> GetStatusAsync(CancellationToken ct = default);
}
