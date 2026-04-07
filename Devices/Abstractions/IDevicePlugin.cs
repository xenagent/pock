using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoopPOS.Checkout.Agent.Devices.Abstractions;

public interface IDevicePlugin
{
    string Name { get; }
    string Version { get; }
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
