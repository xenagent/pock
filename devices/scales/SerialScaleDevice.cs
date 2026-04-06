using Devices.Common;
using Devices.Interfaces;

namespace Devices.Scales;

public class SerialScaleDevice : SerialDeviceBase
{
    private ScaleConfig? _config;

    public event EventHandler<WeightReceivedEventArgs>? WeightReceived;

    public override Task InitializeAsync(IDeviceConfig config)
    {
        _config = (ScaleConfig)config;
        return base.InitializeAsync(config);
    }

    protected override void ProcessLine(string line)
    {
        var result = _config!.Protocol.Parse(line, _config.WeightUnit);

        if (result != null)
            WeightReceived?.Invoke(this, result);
    }
}
