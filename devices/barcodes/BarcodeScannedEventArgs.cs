namespace Devices.Barcodes;

public class BarcodeScannedEventArgs : EventArgs
{
    public string Barcode { get; init; } = "";
}
