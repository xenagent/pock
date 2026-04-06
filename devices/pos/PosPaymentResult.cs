namespace Devices.Pos;

public class PosPaymentResult
{
    public bool Success { get; init; }

    // ── Onay bilgileri ────────────────────────────────────────────────
    public string AuthorizationCode { get; init; } = "";
    public string ReferenceNumber { get; init; } = "";    // RRN
    public string Stan { get; init; } = "";
    public string TransUniqueId { get; init; } = "";
    public string TransactionDateTime { get; init; } = "";
    public string BankResponseCode { get; init; } = "";
    public string EmvResponseCode { get; init; } = "";

    // ── Hata bilgileri ────────────────────────────────────────────────
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    // ── İşlem detayı ─────────────────────────────────────────────────
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime ProcessedAt { get; init; }

    /// <summary>POS'tan gelen fiş satırları (boşsa POS yazdırmıştır).</summary>
    public string[] SlipLines { get; init; } = Array.Empty<string>();

    public static PosPaymentResult Fail(string code, string message) => new()
    {
        Success      = false,
        ErrorCode    = code,
        ErrorMessage = message,
        ProcessedAt  = DateTime.Now
    };
}
