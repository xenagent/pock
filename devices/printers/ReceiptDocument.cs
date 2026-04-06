namespace Devices.Printers;

public class ReceiptDocument
{
    public string Header { get; init; } = "";
    public string Footer { get; init; } = "";
    public List<ReceiptLine> Lines { get; init; } = new();
    public bool AutoCut { get; init; } = true;
}
