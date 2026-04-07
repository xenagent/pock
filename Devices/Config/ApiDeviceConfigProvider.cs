using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoopPOS.Checkout.Agent.Devices.Config;

/// <summary>
/// Cihaz konfigürasyonunu REST API'den çeker.
/// GET {baseUrl}/api/device-config/{storeId}/{registerId}
/// </summary>
public sealed class ApiDeviceConfigProvider(HttpClient httpClient, string baseUrl) : IDeviceConfigProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<StoreDeviceConfiguration> GetConfigurationAsync(
        string storeId,
        string registerId,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/device-config/{storeId}/{registerId}";

        return await httpClient.GetFromJsonAsync<StoreDeviceConfiguration>(url, JsonOptions, ct)
               ?? throw new InvalidOperationException("API boş konfigürasyon döndü.");
    }
}
