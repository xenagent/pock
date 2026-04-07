using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Printer;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scale;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoopPOS.Checkout.Agent.Devices.Dummy;

public sealed class DummyDevicePlugin : IDevicePlugin
{
    public string Name => "DummyDevices";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IScannerDevice, DummyScannerDevice>();
        services.AddSingleton<IScaleDevice, DummyScaleDevice>();
        services.AddSingleton<IPrinterDevice, DummyPrinterDevice>();
        services.AddSingleton<IPOSDevice, DummyPOSDevice>();
    }
}
