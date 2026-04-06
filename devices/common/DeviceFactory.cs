using Devices.Barcodes;
using Devices.Interfaces;
using Devices.Pos;
using Devices.Printers;
using Devices.Scales;

namespace Devices.Common;

public class DeviceFactory : IDeviceFactory
{
    public IHardwareDevice Create(IDeviceConfig config) => config switch
    {
        BarcodeReaderConfig bc => bc.ConnectionMode.CreateDevice(),
        ScaleConfig            => new SerialScaleDevice(),
        PosConfig              => new IngenicoPos(new IngenicoLibraryAdapter()),
        PrinterConfig          => new EscPosPrinter(),
        _                      => throw new NotSupportedException($"Unsupported config type: {config.GetType().Name}")
    };
}
