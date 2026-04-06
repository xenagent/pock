using Devices.Interfaces;

namespace Devices.Pos;

public class PosConfig : IDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool Enabled { get; set; } = true;

    // ── DLL ───────────────────────────────────────────────────────────
    /// <summary>ImpProDLL.dll'in tam yolu (varsayılan: uygulama dizini).</summary>
    public string DllPath { get; set; } = "ImpProDLL.dll";

    /// <summary>ImpProDLL'in kullandığı XML konfigürasyon dosyasının yolu.</summary>
    public string XmlConfigFilePath { get; set; } = "ImpProConfig.xml";

    // ── Bağlantı ──────────────────────────────────────────────────────
    /// <summary>true → TCP/IP, false → Serial (COM) port.</summary>
    public bool UseTcp { get; set; } = false;

    // TCP
    public string IpAddress { get; set; } = "192.168.1.100";
    public int IpPort { get; set; } = 8080;

    // Serial
    public string ComPort { get; set; } = "COM1";
    public int BaudRate { get; set; } = 115200;

    // ── Terminal kimliği ──────────────────────────────────────────────
    /// <summary>POS terminal ID (bankayla anlaşmalı, 8 karakter).</summary>
    public string TerminalId { get; set; } = "";

    /// <summary>Merchant (işyeri) ID.</summary>
    public string MerchantId { get; set; } = "";

    // ── ECR kimliği ───────────────────────────────────────────────────
    public string EcrBrand { get; set; } = "KOOP";
    public string EcrModel { get; set; } = "MARKET";
    public string EcrSerialNumber { get; set; } = "001";

    // ── İşlem ayarları ────────────────────────────────────────────────
    public int TimeoutSeconds { get; set; } = 120;
}
