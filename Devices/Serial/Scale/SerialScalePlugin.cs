using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoopPOS.Checkout.Agent.Devices.Serial.Scale;

public sealed class SerialScalePlugin : IDevicePlugin
{
    public string Name    => "SerialScale";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection("Devices:Scale").Get<ScaleConfig>()
                  ?? new ScaleConfig();

        services.AddSingleton(cfg);
        services.AddSingleton<IScaleDevice, SerialScaleDevice>();
    }
}
