namespace Devices.Scales;

public class WeightReceivedEventArgs : EventArgs
{
    public decimal Weight { get; init; }
    public string Unit { get; init; } = "";
    public bool IsStable { get; init; }
    public DateTime ReceivedAt { get; init; }
}
