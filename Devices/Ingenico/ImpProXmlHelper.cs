using System.Text;
using System.Text.RegularExpressions;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>ImpProDLL ile XML veri alışverişi. Encoding: iso-8859-9 (Türkçe).</summary>
internal static class ImpProXmlHelper
{
    private static readonly Encoding Turkish = Encoding.GetEncoding("iso-8859-9");

    public static byte[] Encode(string xml)           => Turkish.GetBytes(xml);
    public static string  Decode(byte[] buf, int len)  => Turkish.GetString(buf, 0, len).Trim('\0');

    /// <summary>
    /// Imp_CreateInterface için arayüz XML'i.
    /// POS her zaman TCP/IP üzerinden bağlanır.
    /// </summary>
    public static string BuildInterfaceXml(PosTerminalConfig cfg) =>
        $"<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>" +
        $"<IsTcpConnection>1</IsTcpConnection>" +
        $"<szIP>{cfg.IpAddress}</szIP>" +
        $"<IpPort>{cfg.IpPort}</IpPort>" +
        $"<szTerminalID>{cfg.TerminalId}</szTerminalID>" +
        $"<szMerchantID>{cfg.MerchantId}</szMerchantID>" +
        $"<szEcrBrand>{cfg.EcrBrand}</szEcrBrand>" +
        $"<szEcrModel>{cfg.EcrModel}</szEcrModel>" +
        $"<szEcrSerialNumber>{cfg.EcrSerialNumber}</szEcrSerialNumber>" +
        $"</XML>";

    public static string BuildTransactionXml(
        int transType, long amountKurus, string currency,
        int sessionId, int timeoutSec, int installments = 0) =>
        $"<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>" +
        $"<TransType>{transType}</TransType>" +
        $"<Amount>{amountKurus}</Amount>" +
        $"<TransCurrency>{currency}</TransCurrency>" +
        $"<InstallmentCount>{installments}</InstallmentCount>" +
        $"<TransactionTimeout>{timeoutSec}</TransactionTimeout>" +
        $"<SessionID>{sessionId}</SessionID>" +
        $"</XML>";

    public static string BuildReverseXml(
        string originalAuthCode, long amountKurus, string currency, int sessionId) =>
        $"<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>" +
        $"<TransType>{ImpProTransType.Void}</TransType>" +
        $"<Amount>{amountKurus}</Amount>" +
        $"<TransCurrency>{currency}</TransCurrency>" +
        $"<SessionID>{sessionId}</SessionID>" +
        $"<stOriginalTransData>" +
        $"<szAuthorizationNumber>{originalAuthCode}</szAuthorizationNumber>" +
        $"</stOriginalTransData>" +
        $"</XML>";

    public static int    ParseTransStep(string xml)         => TryGetInt(xml, "TransStep");
    public static int    ParseTransactionResult(string xml) => TryGetInt(xml, "TransactionResult", -1);
    public static int    ParseStepInfo(string xml)          => TryGetInt(xml, "PosTransStepInfo");

    public static string[] ParseSlipLines(string xml)
    {
        var count = TryGetInt(xml, "SlipLineCount");
        return count <= 0
            ? []
            : Enumerable.Range(0, count).Select(i => GetTag(xml, $"LineData{i}")).ToArray();
    }

    public static string GetTag(string xml, string tag)
    {
        var m = Regex.Match(xml, $@"<{tag}>(.*?)</{tag}>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
    }

    private static int TryGetInt(string xml, string tag, int fallback = 0)
        => int.TryParse(GetTag(xml, tag), out var v) ? v : fallback;
}
