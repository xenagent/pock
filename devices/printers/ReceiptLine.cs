namespace Devices.Printers;

public class ReceiptLine
{
    public ReceiptLineType Type { get; init; } = ReceiptLineType.Text;

    /// <summary>Sol taraf metni (veya tek satır metin).</summary>
    public string Text { get; init; } = "";

    /// <summary>Sağ tarafa hizalanmış metin (fiyat, kod vb.).</summary>
    public string RightText { get; init; } = "";

    public TextAlignment Alignment { get; init; } = TextAlignment.Left;
    public bool Bold { get; init; }
    public bool DoubleHeight { get; init; }
}
