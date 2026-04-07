namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;

public sealed record WeightReading(
    decimal WeightInGrams,
    bool IsStable,
    DateTimeOffset MeasuredAt);
