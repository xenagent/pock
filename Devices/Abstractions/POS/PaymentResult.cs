namespace KoopPOS.Checkout.Agent.Devices.Abstractions.POS;

public sealed record PaymentRequest(
    decimal Amount,
    string Currency,
    PaymentMethod Method);

public sealed record RefundRequest(
    decimal Amount,
    string Currency,
    string OriginalTransactionId);

public enum PaymentOutcome
{
    Approved,
    Declined,
    Cancelled,
    Error
}

public sealed record PaymentResult(
    PaymentOutcome Outcome,
    string? TransactionId = null,
    string? AuthorizationCode = null,
    string? ErrorMessage = null,
    PaymentMethod Method = PaymentMethod.Unknown)
{
    public bool IsApproved => Outcome == PaymentOutcome.Approved;
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    Contactless,
    Unknown
}
