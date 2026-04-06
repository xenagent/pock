using Devices.Common;
using Devices.Interfaces;

namespace Devices.Barcodes;

public sealed class BarcodeConnectionMode : Enumeration
{
    public static readonly BarcodeConnectionMode SerialPort     = new(1, "SerialPort");
    public static readonly BarcodeConnectionMode UsbHid         = new(2, "UsbHid");
    public static readonly BarcodeConnectionMode KeyboardWedge  = new(3, "KeyboardWedge");

    private BarcodeConnectionMode(int id, string name) : base(id, name) { }

    public IHardwareDevice CreateDevice() => this switch
    {
        var x when x == SerialPort => new SerialBarcodeReader(),
        _ => throw new NotSupportedException($"Desteklenmeyen mod: {Name}")
    };
}
