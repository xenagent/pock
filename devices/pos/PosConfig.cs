using Devices.Interfaces;

namespace Devices.Pos;

public class PosConfig : IDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool Enabled { get; set; } = true;

    /// <summary>Ingenico DLL'inin bulunduğu tam yol.</summary>
    public string DllPath { get; set; } = "IngenicoPos.dll";

    /// <summary>Terminal ID (bankayla anlaşmalı).</summary>
    public string TerminalId { get; set; } = "";

    /// <summary>Merchant ID.</summary>
    public string MerchantId { get; set; } = "";

    /// <summary>İşlem zaman aşımı (saniye).</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
