using System.Text;
using System.Text.RegularExpressions;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>ImpProDLL ile XML veri alışverişi. Encoding: iso-8859-9 (Türkçe).</summary>
internal static class ImpProXmlHelper
{
    private static readonly Encoding Turkish = Encoding.GetEncoding("iso-8859-9");

    public static byte[] Encode(string xml)      => Turkish.GetBytes(xml);
    public static string  Decode(byte[] buf, int len) => Turkish.GetString(buf, 0, len).Trim('\0');

    public static string BuildInterfaceXml(PosTerminalConfig cfg)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>");
        sb.AppendLine($"  <IsTcpConnection>{(cfg.UseTcp ? 1 : 0)}</IsTcpConnection>");
        if (cfg.UseTcp) { sb.AppendLine($"  <szIP>{cfg.IpAddress}</szIP>"); sb.AppendLine($"  <IpPort>{cfg.IpPort}</IpPort>"); }
        else            { sb.AppendLine($"  <ComPort>{cfg.ComPort}</ComPort>"); sb.AppendLine($"  <BaudRate>{cfg.BaudRate}</BaudRate>"); }
        sb.AppendLine($"  <szTerminalID>{cfg.TerminalId}</szTerminalID>");
        sb.AppendLine($"  <szMerchantID>{cfg.MerchantId}</szMerchantID>");
        sb.AppendLine($"  <szEcrBrand>{cfg.EcrBrand}</szEcrBrand>");
        sb.AppendLine($"  <szEcrModel>{cfg.EcrModel}</szEcrModel>");
        sb.AppendLine($"  <szEcrSerialNumber>{cfg.EcrSerialNumber}</szEcrSerialNumber>");
        sb.Append("</XML>");
        return sb.ToString();
    }

    public static string BuildTransactionXml(int transType, long amountKurus, string currency, int sessionId, int timeoutSec, int installments = 0)
        => $"<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>" +
           $"<TransType>{transType}</TransType><Amount>{amountKurus}</Amount>" +
           $"<TransCurrency>{currency}</TransCurrency><InstallmentCount>{installments}</InstallmentCount>" +
           $"<TransactionTimeout>{timeoutSec}</TransactionTimeout><SessionID>{sessionId}</SessionID></XML>";

    public static string BuildReverseXml(string originalAuthCode, long amountKurus, string currency, int sessionId)
        => $"<?xml version=\"1.0\" encoding=\"iso-8859-9\" ?><XML>" +
           $"<TransType>{ImpProTransType.Void}</TransType><Amount>{amountKurus}</Amount>" +
           $"<TransCurrency>{currency}</TransCurrency><SessionID>{sessionId}</SessionID>" +
           $"<stOriginalTransData><szAuthorizationNumber>{originalAuthCode}</szAuthorizationNumber></stOriginalTransData></XML>";

    public static int    ParseTransStep(string xml)         => int.TryParse(GetTag(xml, "TransStep"),         out var v) ? v : 0;
    public static int    ParseTransactionResult(string xml) => int.TryParse(GetTag(xml, "TransactionResult"), out var v) ? v : -1;
    public static int    ParseStepInfo(string xml)          => int.TryParse(GetTag(xml, "PosTransStepInfo"),  out var v) ? v : 0;

    public static string[] ParseSlipLines(string xml)
    {
        if (!int.TryParse(GetTag(xml, "SlipLineCount"), out var count) || count <= 0) return [];
        return Enumerable.Range(0, count).Select(i => GetTag(xml, $"LineData{i}")).ToArray();
    }

    public static string GetTag(string xml, string tag)
    {
        var m = Regex.Match(xml, $@"<{tag}>(.*?)</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
    }
}
