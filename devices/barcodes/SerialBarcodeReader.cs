using Devices.Common;
using Devices.Interfaces;

namespace Devices.Barcodes;

public class SerialBarcodeReader : SerialDeviceBase
{
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    protected override void ProcessLine(string line)
    {
        BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs { Barcode = line });
    }
}
