namespace KoopPOS.Checkout.Agent.Devices.Abstractions;

/// <summary>Tüm cihaz konfigürasyonlarının temel sözleşmesi.</summary>
public interface IDeviceConfig
{
    string DeviceId { get; set; }
    string DeviceName { get; set; }
    bool Enabled { get; set; }
    int ReconnectIntervalMs { get; set; }
    int MaxReconnectAttempts { get; set; }
}
