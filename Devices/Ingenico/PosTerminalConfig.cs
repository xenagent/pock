using KoopPOS.Checkout.Agent.Devices.Abstractions;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

public sealed class PosTerminalConfig : IDeviceConfig
{
    public string DeviceId            { get; set; } = string.Empty;
    public string DeviceName          { get; set; } = string.Empty;
    public bool   Enabled             { get; set; } = true;
    public int    ReconnectIntervalMs { get; set; } = 5_000;
    public int    MaxReconnectAttempts{ get; set; } = 3;

    // ── DLL ───────────────────────────────────────────────────────────
    public string DllPath      { get; set; } = "ImpProDLL.dll";
    public string XmlConfigPath{ get; set; } = "ImpProConfig.xml";

    // ── TCP/IP bağlantı (POS her zaman TCP) ──────────────────────────
    public string IpAddress    { get; set; } = "192.168.1.100";
    public int    IpPort       { get; set; } = 8080;

    // ── Terminal kimliği ──────────────────────────────────────────────
    public string TerminalId   { get; set; } = string.Empty;
    public string MerchantId   { get; set; } = string.Empty;

    // ── ECR kimliği ───────────────────────────────────────────────────
    public string EcrBrand        { get; set; } = "KOOP";
    public string EcrModel        { get; set; } = "MARKET";
    public string EcrSerialNumber { get; set; } = "001";

    public int TimeoutSeconds  { get; set; } = 120;
}
