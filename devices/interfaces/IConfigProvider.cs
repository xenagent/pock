namespace Devices.Interfaces;

public interface IConfigProvider
{
    Task<IEnumerable<IDeviceConfig>> GetConfigsAsync(string storeId, string kasaId);
}
