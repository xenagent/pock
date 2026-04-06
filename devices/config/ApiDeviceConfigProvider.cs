using System.Net.Http.Json;
using Devices.Barcodes;
using Devices.Interfaces;
using Devices.Pos;
using Devices.Printers;
using Devices.Scales;

namespace Devices.Config;

/// <summary>
/// Cihaz konfigürasyonlarını REST API'den çeker.
/// GET {baseUrl}/api/devices/{storeId}/{kasaId}
/// </summary>
public class ApiDeviceConfigProvider : IConfigProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ApiDeviceConfigProvider(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<IEnumerable<IDeviceConfig>> GetConfigsAsync(string storeId, string kasaId)
    {
        var url = $"{_baseUrl}/api/devices/{storeId}/{kasaId}";
        var dtos = await _http.GetFromJsonAsync<List<DeviceConfigDto>>(url)
                   ?? new List<DeviceConfigDto>();

        return dtos.Select(Map).Where(c => c is not null).Cast<IDeviceConfig>();
    }

    private static IDeviceConfig? Map(DeviceConfigDto dto) => dto.DeviceType switch
    {
        "Barcode" => new BarcodeReaderConfig
        {
            DeviceId       = dto.DeviceId,
            DeviceName     = dto.DeviceName,
            Enabled        = dto.Enabled,
            PortName       = dto.PortName,
            BaudRate       = dto.BaudRate,
            ConnectionMode = dto.ConnectionMode == "SerialPort"
                                 ? BarcodeConnectionMode.SerialPort
                                 : BarcodeConnectionMode.SerialPort
        },

        "Scale" => new ScaleConfig
        {
            DeviceId   = dto.DeviceId,
            DeviceName = dto.DeviceName,
            Enabled    = dto.Enabled,
            PortName   = dto.PortName,
            BaudRate   = dto.BaudRate,
            Protocol   = dto.ScaleProtocol == "Mettler"
                             ? ScaleProtocol.Mettler
                             : ScaleProtocol.Standard,
            WeightUnit = dto.WeightUnit ?? "kg"
        },

        "Pos" => new PosConfig
        {
            DeviceId   = dto.DeviceId,
            DeviceName = dto.DeviceName,
            Enabled    = dto.Enabled,
            DllPath    = dto.DllPath ?? "IngenicoPos.dll",
            TerminalId = dto.TerminalId ?? "",
            MerchantId = dto.MerchantId ?? ""
        },

        "Printer" => new PrinterConfig
        {
            DeviceId   = dto.DeviceId,
            DeviceName = dto.DeviceName,
            Enabled    = dto.Enabled,
            PortName   = dto.PortName,
            BaudRate   = dto.BaudRate,
            LineWidth  = dto.LineWidth
        },

        _ => null
    };
}
