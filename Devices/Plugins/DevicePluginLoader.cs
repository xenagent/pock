using KoopPOS.Checkout.Agent.Devices.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Plugins;

public static partial class DevicePluginLoader
{
    public static void LoadPlugins(
        IServiceCollection services,
        IConfiguration configuration,
        string pluginsPath,
        ILogger? logger = null)
    {
        if (!Directory.Exists(pluginsPath))
        {
            if (logger is not null) LogPluginsNotFound(logger, pluginsPath);
            return;
        }

        foreach (var dll in Directory.GetFiles(pluginsPath, "KoopPOS.Devices.*.dll"))
        {
            try
            {
                var loadContext = new PluginLoadContext(dll);
                var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dll));

                foreach (var type in assembly.GetTypes())
                {
                    if (type is { IsAbstract: false, IsInterface: false }
                        && typeof(IDevicePlugin).IsAssignableFrom(type)
                        && Activator.CreateInstance(type) is IDevicePlugin plugin)
                    {
                        plugin.RegisterServices(services, configuration);
                        if (logger is not null) LogPluginLoaded(logger, plugin.Name, plugin.Version);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger is not null) LogPluginFailed(logger, ex, Path.GetFileName(dll));
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugins directory not found: {PluginsPath}")]
    private static partial void LogPluginsNotFound(ILogger logger, string pluginsPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded plugin: {Name} v{Version}")]
    private static partial void LogPluginLoaded(ILogger logger, string name, string version);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load plugin: {Assembly}")]
    private static partial void LogPluginFailed(ILogger logger, Exception ex, string assembly);
}
