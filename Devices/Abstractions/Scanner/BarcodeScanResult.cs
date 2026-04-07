namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;

public sealed record BarcodeScanResult(
    string Barcode,
    BarcodeSymbology Symbology,
    DateTimeOffset ScannedAt);

public enum BarcodeSymbology
{
    EAN13,
    EAN8,
    UpcA,
    UpcE,
    Code128,
    Code39,
    QR,
    DataMatrix,
    Unknown
}
