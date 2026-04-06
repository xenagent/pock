using Devices.Barcodes;
using Devices.Interfaces;
using Devices.Printers;
using Devices.Scales;

namespace Devices.Common;

public class DeviceManager
{
    private readonly IDeviceFactory _factory;
    private readonly IConfigProvider _configProvider;
    private readonly Dictionary<string, IHardwareDevice> _devices = new();

    // ── Typed shortcuts ────────────────────────────────────────────────
    public SerialBarcodeReader? BarcodeReader { get; private set; }
    public SerialScaleDevice? Scale { get; private set; }
    public IPosTerminal? PosTerminal { get; private set; }
    public IPrinter? Printer { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────
    public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

    public DeviceManager(IDeviceFactory factory, IConfigProvider configProvider)
    {
        _factory = factory;
        _configProvider = configProvider;
    }

    public async Task StartAsync(string storeId, string kasaId)
    {
        var configs = await _configProvider.GetConfigsAsync(storeId, kasaId);

        foreach (var config in configs.Where(x => x.Enabled))
        {
            var device = _factory.Create(config);

            await device.InitializeAsync(config);

            var oldStatus = device.Status;
            await device.ConnectAsync();
            NotifyStatusChange(device, config.DeviceName, oldStatus);

            _devices[config.DeviceId] = device;
            AssignTypedReference(device);
        }
    }

    public async Task StopAsync()
    {
        foreach (var (_, device) in _devices)
        {
            var oldStatus = device.Status;
            await device.DisconnectAsync();
            NotifyStatusChange(device, "", oldStatus);
        }

        _devices.Clear();
        BarcodeReader = null;
        Scale = null;
        PosTerminal = null;
        Printer = null;
    }

    public IHardwareDevice? Get(string deviceId)
        => _devices.TryGetValue(deviceId, out var d) ? d : null;

    // ── Helpers ────────────────────────────────────────────────────────
    private void AssignTypedReference(IHardwareDevice device)
    {
        switch (device)
        {
            case SerialBarcodeReader br: BarcodeReader = br; break;
            case SerialScaleDevice sc:   Scale = sc;         break;
            case IPosTerminal pos:       PosTerminal = pos;  break;
            case IPrinter pr:            Printer = pr;       break;
        }
    }

    private void NotifyStatusChange(IHardwareDevice device, string deviceName, DeviceStatus oldStatus)
    {
        if (device.Status == oldStatus) return;

        DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
        {
            DeviceId   = device.DeviceId,
            DeviceName = deviceName,
            OldStatus  = oldStatus,
            NewStatus  = device.Status
        });
    }
}
