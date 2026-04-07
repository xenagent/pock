using Devices.Barcodes;
using Devices.Interfaces;
using Devices.Pos;
using Devices.Printers;
using Devices.Scales;

namespace Devices.Common;

public class DeviceFactory : IDeviceFactory
{
    private readonly IImpProLibrary? _impProLibrary;

    /// <summary>
    /// Production: <c>new DeviceFactory()</c>
    /// Geliştirme:  <c>new DeviceFactory(new FakeImpProLibrary())</c>
    /// </summary>
    public DeviceFactory(IImpProLibrary? impProLibrary = null)
    {
        _impProLibrary = impProLibrary;
    }

    public IHardwareDevice Create(IDeviceConfig config) => config switch
    {
        BarcodeReaderConfig bc => bc.ConnectionMode.CreateDevice(),
        ScaleConfig            => new SerialScaleDevice(),
        PosConfig              => new IngenicoPos(_impProLibrary),
        PrinterConfig          => new EscPosPrinter(),
        _                      => throw new NotSupportedException($"Unsupported config type: {config.GetType().Name}")
    };
}
