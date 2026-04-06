using System.Text;
using System.Text.RegularExpressions;

namespace Devices.Pos;

/// <summary>
/// ImpProDLL ile XML veri alışverişi için yardımcı sınıf.
/// DLL iso-8859-9 (Türkçe Windows-1254 uyumlu) encoding kullanır.
/// </summary>
internal static class ImpProXmlHelper
{
    private static readonly Encoding TurkishEncoding = Encoding.GetEncoding("iso-8859-9");

    // ── Encode / Decode ───────────────────────────────────────────────

    public static byte[] Encode(string xml) => TurkishEncoding.GetBytes(xml);

    public static string Decode(byte[] buffer, int length)
        => TurkishEncoding.GetString(buffer, 0, length).Trim('\0');

    // ── XML Builder ───────────────────────────────────────────────────

    /// <summary>
    /// Imp_CreateInterface için arayüz konfigürasyon XML'i oluşturur.
    /// </summary>
    public static string BuildInterfaceXml(PosConfig cfg)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?>");
        sb.AppendLine("<XML>");
        sb.AppendLine($"  <IsTcpConnection>{(cfg.UseTcp ? 1 : 0)}</IsTcpConnection>");

        if (cfg.UseTcp)
        {
            sb.AppendLine($"  <szIP>{cfg.IpAddress}</szIP>");
            sb.AppendLine($"  <IpPort>{cfg.IpPort}</IpPort>");
        }
        else
        {
            sb.AppendLine($"  <ComPort>{cfg.ComPort}</ComPort>");
            sb.AppendLine($"  <BaudRate>{cfg.BaudRate}</BaudRate>");
        }

        sb.AppendLine($"  <szTerminalID>{cfg.TerminalId}</szTerminalID>");
        sb.AppendLine($"  <szMerchantID>{cfg.MerchantId}</szMerchantID>");
        sb.AppendLine($"  <szEcrBrand>{cfg.EcrBrand}</szEcrBrand>");
        sb.AppendLine($"  <szEcrModel>{cfg.EcrModel}</szEcrModel>");
        sb.AppendLine($"  <szEcrSerialNumber>{cfg.EcrSerialNumber}</szEcrSerialNumber>");
        sb.AppendLine("</XML>");
        return sb.ToString();
    }

    /// <summary>
    /// Imp_UpdateInterfaceXmlDataByHandle için işlem parametresi XML'i oluşturur.
    /// </summary>
    public static string BuildTransactionXml(
        int    transType,
        long   amountKurus,
        string currency,
        int    sessionId,
        int    timeoutSeconds,
        int    installmentCount = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?>");
        sb.AppendLine("<XML>");
        sb.AppendLine($"  <TransType>{transType}</TransType>");
        sb.AppendLine($"  <Amount>{amountKurus}</Amount>");
        sb.AppendLine($"  <TransCurrency>{currency}</TransCurrency>");
        sb.AppendLine($"  <InstallmentCount>{installmentCount}</InstallmentCount>");
        sb.AppendLine($"  <TransactionTimeout>{timeoutSeconds}</TransactionTimeout>");
        sb.AppendLine($"  <SessionID>{sessionId}</SessionID>");
        sb.AppendLine("</XML>");
        return sb.ToString();
    }

    /// <summary>
    /// Imp_UpdateInterfaceXmlDataByHandle için void/iptal işlemi XML'i oluşturur.
    /// </summary>
    public static string BuildReverseXml(
        string originalTransUniqueId,
        string originalAuthCode,
        long   amountKurus,
        string currency,
        int    sessionId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?>");
        sb.AppendLine("<XML>");
        sb.AppendLine($"  <TransType>{ImpProTransType.Void}</TransType>");
        sb.AppendLine($"  <Amount>{amountKurus}</Amount>");
        sb.AppendLine($"  <TransCurrency>{currency}</TransCurrency>");
        sb.AppendLine($"  <SessionID>{sessionId}</SessionID>");
        sb.AppendLine("  <stOriginalTransData>");
        sb.AppendLine($"    <szTransUniqueID>{originalTransUniqueId}</szTransUniqueID>");
        sb.AppendLine($"    <szAuthorizationNumber>{originalAuthCode}</szAuthorizationNumber>");
        sb.AppendLine("  </stOriginalTransData>");
        sb.AppendLine("</XML>");
        return sb.ToString();
    }

    // ── XML Parser ────────────────────────────────────────────────────

    public static int ParseTransStep(string xml)
        => int.TryParse(GetTag(xml, "TransStep"), out var v) ? v : 0;

    public static int ParseTransactionResult(string xml)
        => int.TryParse(GetTag(xml, "TransactionResult"), out var v) ? v : -1;

    public static int ParseStepInfo(string xml)
        => int.TryParse(GetTag(xml, "PosTransStepInfo"), out var v) ? v : 0;

    public static PosPaymentResult ParseApprovalResult(string xml, decimal amount, string currency)
    {
        var result = ParseTransactionResult(xml);
        var success = result == 0 && GetTag(xml, "szBankAppResponseCode") == "00";

        return new PosPaymentResult
        {
            Success           = success,
            AuthorizationCode = GetTag(xml, "szAuthorizationNumber"),
            ReferenceNumber   = GetTag(xml, "szRRN"),
            Stan              = GetTag(xml, "szStan"),
            TransUniqueId     = GetTag(xml, "szTransUniqueID"),
            TransactionDateTime = GetTag(xml, "szTransactionDateTime"),
            BankResponseCode  = GetTag(xml, "szBankAppResponseCode"),
            EmvResponseCode   = GetTag(xml, "szEmvResponseCode"),
            ErrorCode         = result.ToString(),
            ErrorMessage      = success ? "" : GetTag(xml, "szBankAppResponseCode"),
            Amount            = amount,
            Currency          = currency,
            ProcessedAt       = DateTime.Now
        };
    }

    /// <summary>Slip satırlarını slip XML adımından çıkarır.</summary>
    public static string[] ParseSlipLines(string xml)
    {
        if (!int.TryParse(GetTag(xml, "SlipLineCount"), out var count) || count <= 0)
            return Array.Empty<string>();

        var lines = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var line = GetTag(xml, $"LineData{i}");
            lines.Add(line);
        }
        return lines.ToArray();
    }

    public static string GetTag(string xml, string tag)
    {
        var match = Regex.Match(xml, $@"<{tag}>(.*?)</{tag}>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }
}
