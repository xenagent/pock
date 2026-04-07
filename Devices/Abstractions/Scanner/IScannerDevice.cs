using System.Threading.Channels;

namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;

public interface IScannerDevice : IDevice
{
    ChannelReader<BarcodeScanResult> Scans { get; }
    Task EnableAsync(CancellationToken ct = default);
    Task DisableAsync(CancellationToken ct = default);
}
