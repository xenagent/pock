namespace Devices.Pos;

public class PosPaymentResult
{
    public bool Success { get; init; }
    public string AuthorizationCode { get; init; } = "";
    public string ReferenceNumber { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime ProcessedAt { get; init; }

    public static PosPaymentResult Fail(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
        ProcessedAt = DateTime.Now
    };
}
