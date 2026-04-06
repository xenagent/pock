using Devices.Interfaces;

namespace Devices.Common;

public class DeviceManager
{
    private readonly IDeviceFactory _factory;
    private readonly Dictionary<string, IHardwareDevice> _devices = new();

    public DeviceManager(IDeviceFactory factory)
    {
        _factory = factory;
    }

    public async Task StartAsync(IEnumerable<IDeviceConfig> configs)
    {
        foreach (var config in configs.Where(x => x.Enabled))
        {
            var device = _factory.Create(config);

            await device.InitializeAsync(config);
            await device.ConnectAsync();

            _devices[config.DeviceId] = device;
        }
    }

    public async Task StopAsync()
    {
        foreach (var device in _devices.Values)
            await device.DisconnectAsync();
    }

    public IHardwareDevice? Get(string deviceId)
        => _devices.TryGetValue(deviceId, out var d) ? d : null;
}
