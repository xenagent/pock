using Devices.Common;
using Devices.Interfaces;
using Devices.Pos;
using Devices.Printers;

// ============================================================
// KULLANIM ÖRNEĞİ — KasaForm
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
        _deviceManager.DeviceStatusChanged += (s, e) =>
            Console.WriteLine($"[{e.DeviceName}] {e.OldStatus} → {e.NewStatus}");

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
            };
        }

        // POS — Ingenico step bilgileri
        if (_deviceManager.PosTerminal is IngenicoPos pos)
        {
            pos.StepInfoReceived += (s, e) =>
            {
                // UI'da müşteriye ne yapması gerektiğini göster
                Console.WriteLine($"POS: {e.Description}");
                // UpdateStatusLabel(e.Description);
            };

            pos.SlipReceived += (s, e) =>
            {
                // POS ECR-print modundaysa fişi kendin yazdırırsın
                Console.WriteLine("Fiş geldi:");
                foreach (var line in e.Lines)
                    Console.WriteLine($"  {line}");
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

        Console.WriteLine($"Ödeme onaylandı. Onay: {result.AuthorizationCode}  RRN: {result.ReferenceNumber}");

        // POS fiş gönderdiyse (SlipLines doluysa) printer'dan yazdır,
        // aksi hâlde POS kendi yazdırmıştır.
        if (result.SlipLines.Length > 0)
            await PrintPosSlipAsync(result);
        else
            await PrintPaymentReceiptAsync(amount, result);
    }

    // ── İade ──────────────────────────────────────────────────
    public async Task ProcessRefundAsync(decimal amount, string originalAuthCode)
    {
        var pos = _deviceManager.PosTerminal
            ?? throw new InvalidOperationException("POS terminali bağlı değil.");

        var result = await pos.ProcessRefundAsync(amount, originalAuthCode);

        if (result.Success)
            Console.WriteLine($"İade onaylandı. Onay: {result.AuthorizationCode}");
        else
            Console.WriteLine($"İade başarısız: {result.ErrorMessage}");
    }

    // ── POS'tan gelen slip satırlarını yazdır ─────────────────
    private async Task PrintPosSlipAsync(PosPaymentResult result)
    {
        var printer = _deviceManager.Printer;
        if (printer == null) return;

        var lines = result.SlipLines
            .Select(l => new ReceiptLine { Text = l })
            .ToList();

        await printer.PrintReceiptAsync(new ReceiptDocument
        {
            Lines   = lines,
            AutoCut = true
        });
    }

    // ── Kendi oluşturduğun fiş ────────────────────────────────
    private async Task PrintPaymentReceiptAsync(decimal amount, PosPaymentResult result)
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
                new() { Text = "Tutar",     RightText = $"{amount:N2} TL" },
                new() { Text = "Onay No",   RightText = result.AuthorizationCode },
                new() { Text = "RRN",       RightText = result.ReferenceNumber },
                new() { Text = "Tarih",     RightText = result.TransactionDateTime },
            },
            Footer  = "Teşekkür ederiz!",
            AutoCut = true
        });
    }

    // ── Kapatma ───────────────────────────────────────────────
    public async Task ShutdownAsync() => await _deviceManager.StopAsync();
}
