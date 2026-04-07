using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Scanner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoopPOS.Checkout.Agent.Devices.Serial.Scanner;

public sealed class SerialScannerPlugin : IDevicePlugin
{
    public string Name    => "SerialScanner";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.GetSection("Devices:Scanner").Get<BarcodeReaderConfig>()
                  ?? new BarcodeReaderConfig();

        services.AddSingleton(cfg);
        services.AddSingleton<IScannerDevice, SerialScannerDevice>();
    }
}
