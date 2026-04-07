namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Printer;

public sealed record PrintJob(IReadOnlyList<PrintLine> Lines)
{
    public IReadOnlyList<PrintLine> Lines { get; } =
        Lines is { Count: > 0 } ? Lines : throw new ArgumentException("A PrintJob must contain at least one line.", nameof(Lines));
}

public sealed record PrintLine(
    string Text,
    PrintAlignment Alignment = PrintAlignment.Left,
    bool Bold = false);

public enum PrintAlignment
{
    Left,
    Center,
    Right
}

public sealed record PrintResult(bool Success, string? ErrorMessage = null);
