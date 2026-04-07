using System.Threading.Channels;

namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;

public interface IScaleDevice : IDevice
{
    ChannelReader<WeightReading> Weights { get; }
    Task ZeroAsync(CancellationToken ct = default);
    Task TareAsync(CancellationToken ct = default);
}
