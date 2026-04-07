namespace KoopPOS.Checkout.Agent.Devices.Config;

public interface IDeviceConfigProvider
{
    Task<StoreDeviceConfiguration> GetConfigurationAsync(
        string storeId,
        string registerId,
        CancellationToken ct = default);
}
