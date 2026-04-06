using Devices.Barcodes;
using Devices.Interfaces;
using Devices.Scales;

namespace Devices.Common;

public class DeviceFactory : IDeviceFactory
{
    public IHardwareDevice Create(IDeviceConfig config) => config switch
    {
        BarcodeReaderConfig bc => bc.ConnectionMode.CreateDevice(),
        ScaleConfig           => new SerialScaleDevice(),
        _                     => throw new NotSupportedException($"Unsupported config type: {config.GetType().Name}")
    };
}
