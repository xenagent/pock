// ============================================================================
// YAZARKASA DONANIM ENTEGRASYON KATMANI
// Barcode Reader, Termal Yazıcı, POS Terminal, Şarküteri Terazisi
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HardwareIntegration
{
    // ========================================================================
    // 1) KONFIGÜRASYON MODELLERİ (API'den gelecek)
    // ========================================================================

    /// <summary>
    /// Tüm cihaz config'lerinin kontratı.
    /// API response'unda "deviceType" alanına göre polimorfik deserialize edilir.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "deviceType")]
    [JsonDerivedType(typeof(BarcodeReaderConfig), "barcode")]
    [JsonDerivedType(typeof(ThermalPrinterConfig), "thermalPrinter")]
    [JsonDerivedType(typeof(PosTerminalConfig), "posTerminal")]
    [JsonDerivedType(typeof(ScaleConfig), "scale")]
    public interface IDeviceConfig
    {
        string DeviceId { get; set; }
        string DeviceName { get; set; }
        bool Enabled { get; set; }
        int ReconnectIntervalMs { get; set; }
        int MaxReconnectAttempts { get; set; }
    }

    /// <summary>Serial port kullanan cihazlar için ek kontrat.</summary>
    public interface ISerialDeviceConfig : IDeviceConfig
    {
        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        Handshake Handshake { get; set; }
        string LineEnding { get; set; }
    }

    public class BarcodeReaderConfig : ISerialDeviceConfig
    {
        // IDeviceConfig
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;

        // ISerialDeviceConfig
        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public string LineEnding { get; set; } = "\r\n";

        // Barkod-spesifik
        public BarcodeConnectionMode ConnectionMode { get; set; } = BarcodeConnectionMode.SerialPort;
    }

    public enum BarcodeConnectionMode { SerialPort, UsbHid, KeyboardWedge }

    public class ThermalPrinterConfig : IDeviceConfig
    {
        // IDeviceConfig
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;

        // Yazıcı-spesifik
        public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.Usb;
        public string PrinterName { get; set; } = string.Empty;       // USB / Windows Printer Name
        public string IpAddress { get; set; } = string.Empty;         // Network yazıcı
        public int Port { get; set; } = 9100;
        public string ComPort { get; set; } = string.Empty;           // Serial yazıcı
        public int BaudRate { get; set; } = 19200;
        public int PaperWidthMm { get; set; } = 80;
        public string CodePage { get; set; } = "857";                 // Türkçe
        public bool AutoCut { get; set; } = true;
    }

    public enum PrinterConnectionType { Usb, Network, Serial }

    public class PosTerminalConfig : IDeviceConfig
    {
        // IDeviceConfig
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;

        // POS-spesifik
        public PosConnectionType ConnectionType { get; set; } = PosConnectionType.Tcp;
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public string ComPort { get; set; } = string.Empty;
        public int TimeoutMs { get; set; } = 60000;
        public string TerminalId { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
    }

    public enum PosConnectionType { Tcp, Serial, Api }

    public class ScaleConfig : ISerialDeviceConfig
    {
        // IDeviceConfig
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;

        // ISerialDeviceConfig
        public string PortName { get; set; } = "COM5";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public string LineEnding { get; set; } = "\r\n";

        // Terazi-spesifik
        public ScaleProtocol Protocol { get; set; } = ScaleProtocol.Standard;
        public string WeightUnit { get; set; } = "kg";
        public int DecimalPlaces { get; set; } = 3;
        public string RequestWeightCommand { get; set; } = "W\r\n";
        public bool ContinuousMode { get; set; } = false;
    }

    public enum ScaleProtocol { Standard, Mettler, CAS, Dibal, Bizerba }

    /// <summary>API'den gelen tüm cihaz config'lerinin wrapper'ı.</summary>
    public class StoreDeviceConfiguration
    {
        public string StoreId { get; set; } = string.Empty;
        public string RegisterId { get; set; } = string.Empty;
        public List<IDeviceConfig> Devices { get; set; } = new();
    }

    // ========================================================================
    // 2) CIHAZ DURUM & EVENT MODELLERİ
    // ========================================================================

    public enum DeviceStatus
    {
        NotInitialized,
        Disconnected,
        Connecting,
        Connected,
        Error,
        Reconnecting
    }

    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; init; } = string.Empty;
        public DeviceStatus OldStatus { get; init; }
        public DeviceStatus NewStatus { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; init; } = string.Empty;
        public DateTime ScannedAt { get; init; } = DateTime.Now;
    }

    public class WeightReceivedEventArgs : EventArgs
    {
        public decimal Weight { get; init; }
        public string Unit { get; init; } = "kg";
        public bool IsStable { get; init; }
        public DateTime ReceivedAt { get; init; } = DateTime.Now;
    }

    public class PaymentResultEventArgs : EventArgs
    {
        public bool Success { get; init; }
        public string? AuthorizationCode { get; init; }
        public string? ReferenceNumber { get; init; }
        public decimal Amount { get; init; }
        public string? ErrorMessage { get; init; }
        public string? SlipText { get; init; }
    }

    // ========================================================================
    // 3) CIHAZ INTERFACE'LERİ
    // ========================================================================

    /// <summary>Tüm donanım cihazlarının ortak kontratı.</summary>
    public interface IHardwareDevice : IDisposable
    {
        string DeviceId { get; }
        DeviceStatus Status { get; }

        Task InitializeAsync(IDeviceConfig config);
        Task ConnectAsync(CancellationToken ct = default);
        Task DisconnectAsync();

        event EventHandler<DeviceStatusChangedEventArgs>? StatusChanged;
    }

    /// <summary>Barkod okuyucu — sadece event-based okuma.</summary>
    public interface IBarcodeReader : IHardwareDevice
    {
        event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
    }

    /// <summary>Termal yazıcı — komut gönderme.</summary>
    public interface IThermalPrinter : IHardwareDevice
    {
        Task PrintTextAsync(string text);
        Task PrintReceiptAsync(ReceiptDocument receipt);
        Task FeedAndCutAsync();
        Task OpenCashDrawerAsync();
    }

    /// <summary>POS terminal — ödeme akışı.</summary>
    public interface IPosTerminal : IHardwareDevice
    {
        Task<PaymentResultEventArgs> ProcessPaymentAsync(decimal amount, CancellationToken ct = default);
        Task<PaymentResultEventArgs> ProcessRefundAsync(string referenceNumber, decimal amount, CancellationToken ct = default);
        Task<PaymentResultEventArgs> CancelPaymentAsync(CancellationToken ct = default);
    }

    /// <summary>Şarküteri terazisi — tartım okuma + komut.</summary>
    public interface IScaleDevice : IHardwareDevice
    {
        event EventHandler<WeightReceivedEventArgs>? WeightReceived;

        /// <summary>Terazi sürekli moda değilse, tek seferlik tartım iste.</summary>
        Task<WeightReceivedEventArgs> RequestWeightAsync(CancellationToken ct = default);

        Task TareAsync();
        Task ZeroAsync();
    }

    // ========================================================================
    // 4) RECEIPT MODELI (Fiş yazdırma için)
    // ========================================================================

    public class ReceiptDocument
    {
        public string? Header { get; set; }
        public List<ReceiptLine> Lines { get; set; } = new();
        public string? Footer { get; set; }
        public bool CutAfterPrint { get; set; } = true;
    }

    public class ReceiptLine
    {
        public ReceiptLineType Type { get; set; } = ReceiptLineType.Text;
        public string Text { get; set; } = string.Empty;
        public string? RightText { get; set; }  // Satırın sağ tarafı (fiyat vs.)
        public bool Bold { get; set; }
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    }

    public enum ReceiptLineType { Text, Separator, Barcode, QrCode, Image, BlankLine }
    public enum TextAlignment { Left, Center, Right }

    // ========================================================================
    // 5) SERIAL PORT BASE — Barcode & Terazi için ortak altyapı
    // ========================================================================

    /// <summary>
    /// Serial port üzerinden haberleşen cihazların ortak logic'i.
    /// Buffer yönetimi, auto-reconnect, line parsing burada.
    /// </summary>
    public abstract class SerialDeviceBase : IHardwareDevice
    {
        private SerialPort? _port;
        private readonly StringBuilder _buffer = new();
        private ISerialDeviceConfig? _config;
        private CancellationTokenSource? _reconnectCts;
        private int _reconnectAttempt;

        public string DeviceId { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.NotInitialized;

        public event EventHandler<DeviceStatusChangedEventArgs>? StatusChanged;

        public Task InitializeAsync(IDeviceConfig config)
        {
            if (config is not ISerialDeviceConfig serialConfig)
                throw new ArgumentException($"Config tipi ISerialDeviceConfig olmalı, gelen: {config.GetType().Name}");

            _config = serialConfig;
            DeviceId = config.DeviceId;
            SetStatus(DeviceStatus.Disconnected);
            return Task.CompletedTask;
        }

        public Task ConnectAsync(CancellationToken ct = default)
        {
            if (_config == null)
                throw new InvalidOperationException("Cihaz henüz initialize edilmedi.");

            SetStatus(DeviceStatus.Connecting);
            _reconnectAttempt = 0;

            try
            {
                _port = new SerialPort
                {
                    PortName = _config.PortName,
                    BaudRate = _config.BaudRate,
                    Parity = _config.Parity,
                    DataBits = _config.DataBits,
                    StopBits = _config.StopBits,
                    Handshake = _config.Handshake,
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };
                _port.DataReceived += OnSerialDataReceived;
                _port.ErrorReceived += OnSerialError;
                _port.Open();

                SetStatus(DeviceStatus.Connected);
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, ex.Message);
                _ = TryReconnectAsync();
            }

            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            _reconnectCts?.Cancel();
            ClosePort();
            SetStatus(DeviceStatus.Disconnected);
            return Task.CompletedTask;
        }

        /// <summary>Serial port'a veri gönder (terazi komutları vb. için).</summary>
        protected void SendCommand(string command)
        {
            if (_port?.IsOpen != true)
                throw new InvalidOperationException("Port açık değil.");
            _port.Write(command);
        }

        /// <summary>Alt sınıf, satır satır gelen veriyi burada işler.</summary>
        protected abstract void ProcessLine(string line);

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Status != DeviceStatus.Connected || _port == null) return;

            try
            {
                var data = _port.ReadExisting();
                _buffer.Append(data);

                var lineEnding = _config?.LineEnding ?? "\r\n";
                string bufferStr;

                while ((bufferStr = _buffer.ToString()).Contains(lineEnding))
                {
                    var idx = bufferStr.IndexOf(lineEnding, StringComparison.Ordinal);
                    var line = bufferStr[..idx].Trim();
                    _buffer.Remove(0, idx + lineEnding.Length);

                    if (!string.IsNullOrEmpty(line))
                        ProcessLine(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DeviceId}] Serial read hatası: {ex.Message}");
            }
        }

        private void OnSerialError(object sender, SerialErrorReceivedEventArgs e)
        {
            SetStatus(DeviceStatus.Error, e.EventType.ToString());
            _ = TryReconnectAsync();
        }

        private async Task TryReconnectAsync()
        {
            if (_config == null) return;

            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var ct = _reconnectCts.Token;

            while (_reconnectAttempt < _config.MaxReconnectAttempts && !ct.IsCancellationRequested)
            {
                _reconnectAttempt++;
                SetStatus(DeviceStatus.Reconnecting);

                await Task.Delay(_config.ReconnectIntervalMs, ct).ConfigureAwait(false);

                try
                {
                    ClosePort();
                    await ConnectAsync(ct);
                    if (Status == DeviceStatus.Connected) return;
                }
                catch { /* bir sonraki deneme */ }
            }

            if (Status != DeviceStatus.Connected)
                SetStatus(DeviceStatus.Error, $"Reconnect başarısız ({_config.MaxReconnectAttempts} deneme).");
        }

        private void ClosePort()
        {
            if (_port == null) return;
            _port.DataReceived -= OnSerialDataReceived;
            _port.ErrorReceived -= OnSerialError;
            try { if (_port.IsOpen) _port.Close(); } catch { }
            _port.Dispose();
            _port = null;
            _buffer.Clear();
        }

        private void SetStatus(DeviceStatus newStatus, string? error = null)
        {
            var old = Status;
            Status = newStatus;
            StatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
            {
                DeviceId = DeviceId,
                OldStatus = old,
                NewStatus = newStatus,
                ErrorMessage = error
            });
        }

        public void Dispose()
        {
            _reconnectCts?.Cancel();
            ClosePort();
            GC.SuppressFinalize(this);
        }
    }

    // ========================================================================
    // 6) BARKOD OKUYUCU IMPLEMENTASYONU
    // ========================================================================

    public class SerialBarcodeReader : SerialDeviceBase, IBarcodeReader
    {
        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        protected override void ProcessLine(string line)
        {
            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs
            {
                Barcode = line,
                ScannedAt = DateTime.Now
            });
        }
    }

    // ========================================================================
    // 7) ŞARKÜTERİ TERAZİSİ IMPLEMENTASYONU
    // ========================================================================

    public class SerialScaleDevice : SerialDeviceBase, IScaleDevice
    {
        private ScaleConfig? _scaleConfig;
        private TaskCompletionSource<WeightReceivedEventArgs>? _pendingWeightRequest;

        public event EventHandler<WeightReceivedEventArgs>? WeightReceived;

        public new Task InitializeAsync(IDeviceConfig config)
        {
            _scaleConfig = config as ScaleConfig
                ?? throw new ArgumentException("ScaleConfig bekleniyor.");
            return base.InitializeAsync(config);
        }

        protected override void ProcessLine(string line)
        {
            // Terazi protokolüne göre parse et.
            // Örnek standart format: "ST,GS,  1.234kg" veya "  1.234"
            var result = ParseWeight(line);
            if (result == null) return;

            WeightReceived?.Invoke(this, result);
            _pendingWeightRequest?.TrySetResult(result);
        }

        public Task<WeightReceivedEventArgs> RequestWeightAsync(CancellationToken ct = default)
        {
            if (_scaleConfig == null)
                throw new InvalidOperationException("Terazi initialize edilmedi.");

            _pendingWeightRequest = new TaskCompletionSource<WeightReceivedEventArgs>();
            ct.Register(() => _pendingWeightRequest.TrySetCanceled());

            // Teraziye "tartım gönder" komutu at
            SendCommand(_scaleConfig.RequestWeightCommand);

            return _pendingWeightRequest.Task;
        }

        public Task TareAsync()
        {
            SendCommand("T\r\n"); // Protokole göre değişir
            return Task.CompletedTask;
        }

        public Task ZeroAsync()
        {
            SendCommand("Z\r\n");
            return Task.CompletedTask;
        }

        private WeightReceivedEventArgs? ParseWeight(string raw)
        {
            // Basit parser — gerçek uygulamada protokole göre genişletilmeli
            try
            {
                var isStable = raw.Contains("ST") || !raw.Contains("US");

                // Sayısal kısmı çıkar
                var numericPart = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-').ToArray());
                numericPart = numericPart.Replace(',', '.');

                if (decimal.TryParse(numericPart, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var weight))
                {
                    return new WeightReceivedEventArgs
                    {
                        Weight = weight,
                        Unit = _scaleConfig?.WeightUnit ?? "kg",
                        IsStable = isStable,
                        ReceivedAt = DateTime.Now
                    };
                }
            }
            catch { /* parse hatası — geçersiz veri */ }

            return null;
        }
    }

    // ========================================================================
    // 8) CONFIG PROVIDER — API'den cihaz ayarlarını çeker
    // ========================================================================

    public interface IDeviceConfigProvider
    {
        Task<StoreDeviceConfiguration> GetConfigurationAsync(string storeId, string registerId, CancellationToken ct = default);
    }

    public class ApiDeviceConfigProvider : IDeviceConfigProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiDeviceConfigProvider(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<StoreDeviceConfiguration> GetConfigurationAsync(
            string storeId, string registerId, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/api/device-config/{storeId}/{registerId}";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Deserialize<StoreDeviceConfiguration>(json, options)
                ?? throw new InvalidOperationException("API boş config döndü.");
        }
    }

    // ========================================================================
    // 9) DEVICE FACTORY — Config'e göre doğru implementasyonu üretir
    // ========================================================================

    public interface IDeviceFactory
    {
        IHardwareDevice Create(IDeviceConfig config);
    }

    public class DefaultDeviceFactory : IDeviceFactory
    {
        public IHardwareDevice Create(IDeviceConfig config) => config switch
        {
            BarcodeReaderConfig bc => bc.ConnectionMode switch
            {
                BarcodeConnectionMode.SerialPort => new SerialBarcodeReader(),
                BarcodeConnectionMode.KeyboardWedge => throw new NotImplementedException("KeyboardWedge henüz implemente edilmedi."),
                _ => throw new NotSupportedException($"Desteklenmeyen barkod modu: {bc.ConnectionMode}")
            },
            ScaleConfig => new SerialScaleDevice(),
            ThermalPrinterConfig => throw new NotImplementedException("ThermalPrinter implementasyonu eklenecek."),
            PosTerminalConfig => throw new NotImplementedException("PosTerminal implementasyonu eklenecek."),
            _ => throw new NotSupportedException($"Bilinmeyen cihaz config tipi: {config.GetType().Name}")
        };
    }

    // ========================================================================
    // 10) DEVICE MANAGER — Tüm cihazların orkestratörü
    // ========================================================================

    public class DeviceManager : IDisposable
    {
        private readonly IDeviceConfigProvider _configProvider;
        private readonly IDeviceFactory _factory;
        private readonly Dictionary<string, IHardwareDevice> _devices = new();
        private StoreDeviceConfiguration? _currentConfig;

        // Kısayol erişim — UI tarafı doğrudan bağlanabilir
        public IBarcodeReader? BarcodeReader => _devices.Values.OfType<IBarcodeReader>().FirstOrDefault();
        public IThermalPrinter? Printer => _devices.Values.OfType<IThermalPrinter>().FirstOrDefault();
        public IPosTerminal? PosTerminal => _devices.Values.OfType<IPosTerminal>().FirstOrDefault();
        public IScaleDevice? Scale => _devices.Values.OfType<IScaleDevice>().FirstOrDefault();

        public IReadOnlyDictionary<string, IHardwareDevice> AllDevices => _devices;

        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public DeviceManager(IDeviceConfigProvider configProvider, IDeviceFactory? factory = null)
        {
            _configProvider = configProvider;
            _factory = factory ?? new DefaultDeviceFactory();
        }

        /// <summary>API'den config çek, cihazları oluştur ve bağlan.</summary>
        public async Task StartAsync(string storeId, string registerId, CancellationToken ct = default)
        {
            _currentConfig = await _configProvider.GetConfigurationAsync(storeId, registerId, ct);

            foreach (var deviceConfig in _currentConfig.Devices.Where(d => d.Enabled))
            {
                try
                {
                    var device = _factory.Create(deviceConfig);
                    device.StatusChanged += (s, e) => DeviceStatusChanged?.Invoke(s, e);

                    await device.InitializeAsync(deviceConfig);
                    await device.ConnectAsync(ct);

                    _devices[deviceConfig.DeviceId] = device;

                    Console.WriteLine($"[DeviceManager] {deviceConfig.DeviceName} ({deviceConfig.DeviceId}) bağlandı.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DeviceManager] {deviceConfig.DeviceName} başlatılamadı: {ex.Message}");
                }
            }
        }

        /// <summary>Tüm cihazları durdur.</summary>
        public async Task StopAsync()
        {
            foreach (var (id, device) in _devices)
            {
                try { await device.DisconnectAsync(); }
                catch (Exception ex) { Console.WriteLine($"[DeviceManager] {id} kapatılırken hata: {ex.Message}"); }
            }
        }

        /// <summary>Cihaz durum özeti.</summary>
        public Dictionary<string, DeviceStatus> GetStatusSummary()
            => _devices.ToDictionary(d => d.Key, d => d.Value.Status);

        public void Dispose()
        {
            foreach (var device in _devices.Values)
                device.Dispose();
            _devices.Clear();
        }
    }

    // ========================================================================
    // 11) KULLANIM ÖRNEĞİ
    // ========================================================================

    /*
    public class KasaForm
    {
        private DeviceManager _deviceManager;

        public async Task InitializeDevicesAsync()
        {
            var httpClient = new HttpClient();
            var configProvider = new ApiDeviceConfigProvider(httpClient, "https://api.kooppos.com");
            _deviceManager = new DeviceManager(configProvider);

            // Durum değişikliklerini dinle (UI güncelleme)
            _deviceManager.DeviceStatusChanged += (s, e) =>
            {
                Console.WriteLine($"Cihaz {e.DeviceId}: {e.OldStatus} → {e.NewStatus}");
                // UI'da ikon güncelle, bağlantı durumu göster vs.
            };

            await _deviceManager.StartAsync("STORE-001", "KASA-01");

            // Barkod okuyucuya bağlan
            if (_deviceManager.BarcodeReader != null)
            {
                _deviceManager.BarcodeReader.BarcodeScanned += (s, e) =>
                {
                    Console.WriteLine($"Barkod okundu: {e.Barcode}");
                    // Ürün sorgula, sepete ekle...
                };
            }

            // Teraziye bağlan
            if (_deviceManager.Scale != null)
            {
                _deviceManager.Scale.WeightReceived += (s, e) =>
                {
                    if (e.IsStable)
                        Console.WriteLine($"Tartım: {e.Weight} {e.Unit}");
                };
            }
        }

        public async Task ProcessPaymentAsync(decimal amount)
        {
            var pos = _deviceManager.PosTerminal;
            if (pos == null) throw new InvalidOperationException("POS terminali bağlı değil.");

            var result = await pos.ProcessPaymentAsync(amount);
            if (result.Success)
            {
                // Fiş yazdır
                var printer = _deviceManager.Printer;
                if (printer != null)
                {
                    await printer.PrintReceiptAsync(new ReceiptDocument
                    {
                        Header = "KOOP MARKET",
                        Lines = new List<ReceiptLine>
                        {
                            new() { Text = "Ödeme Başarılı", Alignment = TextAlignment.Center, Bold = true },
                            new() { Type = ReceiptLineType.Separator },
                            new() { Text = "Tutar", RightText = $"{amount:N2} TL" },
                            new() { Text = "Onay No", RightText = result.AuthorizationCode },
                        },
                        Footer = "Teşekkür ederiz!"
                    });
                }
            }
        }
    }
    */
}
