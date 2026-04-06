using Devices.Common;
using Devices.Config;
using Devices.Interfaces;
using Devices.Printers;

// ============================================================
// KULLANIM ÖRNEĞİ — KasaForm
// ============================================================
// DI kullanıyorsan:
//   services.AddSingleton<IDeviceFactory, DeviceFactory>();
//   services.AddSingleton<IConfigProvider>(sp =>
//       new ApiDeviceConfigProvider(sp.GetRequiredService<HttpClient>(),
//                                   "https://api.kooppos.com"));
//   services.AddSingleton<DeviceManager>();
// ============================================================

public class KasaForm
{
    private readonly DeviceManager _deviceManager;

    public KasaForm(DeviceManager deviceManager)
    {
        _deviceManager = deviceManager;
    }

    // ── Başlatma ──────────────────────────────────────────────
    public async Task InitializeDevicesAsync()
    {
        // Durum değişikliklerini dinle (UI güncelleme için)
        _deviceManager.DeviceStatusChanged += (s, e) =>
        {
            Console.WriteLine($"[{e.DeviceName}] {e.OldStatus} → {e.NewStatus}");
            // UI: ikonu güncelle, bağlantı durumunu göster...
        };

        await _deviceManager.StartAsync("STORE-001", "KASA-01");

        // Barkod okuyucu
        if (_deviceManager.BarcodeReader != null)
        {
            _deviceManager.BarcodeReader.BarcodeScanned += (s, e) =>
            {
                Console.WriteLine($"Barkod okundu: {e.Barcode}");
                // Ürün sorgula, sepete ekle...
            };
        }

        // Terazi
        if (_deviceManager.Scale != null)
        {
            _deviceManager.Scale.WeightReceived += (s, e) =>
            {
                if (e.IsStable)
                    Console.WriteLine($"Tartım: {e.Weight} {e.Unit}");
                // Ürün fiyatını güncelle...
            };
        }
    }

    // ── Ödeme ─────────────────────────────────────────────────
    public async Task ProcessPaymentAsync(decimal amount)
    {
        var pos = _deviceManager.PosTerminal
            ?? throw new InvalidOperationException("POS terminali bağlı değil.");

        var result = await pos.ProcessPaymentAsync(amount);

        if (!result.Success)
        {
            Console.WriteLine($"Ödeme başarısız: [{result.ErrorCode}] {result.ErrorMessage}");
            return;
        }

        Console.WriteLine($"Ödeme onaylandı. Onay No: {result.AuthorizationCode}");
        await PrintPaymentReceiptAsync(amount, result.AuthorizationCode);
    }

    // ── İade ──────────────────────────────────────────────────
    public async Task ProcessRefundAsync(decimal amount, string originalAuthCode)
    {
        var pos = _deviceManager.PosTerminal
            ?? throw new InvalidOperationException("POS terminali bağlı değil.");

        var result = await pos.ProcessRefundAsync(amount, originalAuthCode);

        if (result.Success)
            Console.WriteLine($"İade onaylandı. Onay No: {result.AuthorizationCode}");
        else
            Console.WriteLine($"İade başarısız: {result.ErrorMessage}");
    }

    // ── Fiş yazdır ────────────────────────────────────────────
    private async Task PrintPaymentReceiptAsync(decimal amount, string authCode)
    {
        var printer = _deviceManager.Printer;
        if (printer == null) return;

        await printer.PrintReceiptAsync(new ReceiptDocument
        {
            Header = "KOOP MARKET",
            Lines  = new List<ReceiptLine>
            {
                new() { Text = "Ödeme Başarılı", Alignment = TextAlignment.Center, Bold = true },
                new() { Type = ReceiptLineType.Separator },
                new() { Text = "Tutar",    RightText = $"{amount:N2} TL" },
                new() { Text = "Onay No",  RightText = authCode },
            },
            Footer  = "Teşekkür ederiz!",
            AutoCut = true
        });
    }

    // ── Kapatma ───────────────────────────────────────────────
    public async Task ShutdownAsync()
    {
        await _deviceManager.StopAsync();
    }
}
