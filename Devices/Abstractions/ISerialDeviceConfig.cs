using System.IO.Ports;

namespace KoopPOS.Checkout.Agent.Devices.Abstractions;

/// <summary>Serial port üzerinden haberleşen cihazların ek konfigürasyon sözleşmesi.</summary>
public interface ISerialDeviceConfig : IDeviceConfig
{
    string PortName { get; set; }
    int BaudRate { get; set; }
    Parity Parity { get; set; }
    int DataBits { get; set; }
    StopBits StopBits { get; set; }
    Handshake Handshake { get; set; }
    string LineEnding { get; set; }
}
