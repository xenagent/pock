namespace Devices.Pos;

/// <summary>
/// Ingenico DLL'inin yüklenmesini ve metodlarını soyutlar.
/// Gerçek DLL yerine test double yazılabilmesini sağlar.
/// </summary>
public interface IIngenicoLibrary
{
    int Initialize(string terminalId, string merchantId);
    int StartSale(long amountKurus, string currency);
    int StartRefund(long amountKurus, string currency, string originalAuthCode);
    int Cancel();
    IngenicoTransactionResult GetLastResult();
    void Dispose();
}

public class IngenicoTransactionResult
{
    public int ResponseCode { get; init; }
    public string AuthorizationCode { get; init; } = "";
    public string ReferenceNumber { get; init; } = "";
    public string ErrorDescription { get; init; } = "";
}
