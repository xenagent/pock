using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

public sealed class IngenicoPlugin : IDevicePlugin
{
    public string Name    => "Ingenico";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection("Devices:POS").Get<PosTerminalConfig>()
                  ?? new PosTerminalConfig();

        services.AddSingleton(cfg);

        // Production: ImpProLibrary  |  Test/Geliştirme: FakeImpProLibrary
        services.AddSingleton<IImpProLibrary, ImpProLibrary>();

        services.AddSingleton<IPOSDevice, IngenicoDevice>();
    }
}
