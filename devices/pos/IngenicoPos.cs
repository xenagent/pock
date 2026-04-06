using Devices.Common;
using Devices.Interfaces;

namespace Devices.Pos;

/// <summary>
/// Ingenico ImpProDLL.dll tabanlı POS terminali.
///
/// İşlem akışı:
///   1. Imp_SetXmlFilePath  → DLL konfigürasyon dosyasını belirle
///   2. Imp_CreateInterface → POS bağlantı kanalı aç (HANDLE al)
///   3. Imp_UpdateInterfaceXmlDataByHandle → işlem parametrelerini gönder
///   4. Loop: Imp_ExecuteTransactionStep (BLOKLAR)
///            → Imp_GetInterfaceXmlDataByHandle ile adımı oku
///            → TRANS_STEP_TRANS_APPROVAL veya TRANS_STEP_FINISH gelince çık
///   5. Imp_RemoveInterfaceByHandle → kanalı temizle
///
/// NOT: ImpProDLL.dll x86 native DLL'dir.
/// Proje hedefini x86 veya "Prefer 32-bit" olarak ayarla.
/// </summary>
public class IngenicoPos : IPosTerminal
{
    private const int XmlBufferSize = 65536; // 64 KB — slip içerebilir

    private PosConfig? _config;
    private IntPtr _handle = IntPtr.Zero;

    public string DeviceId { get; private set; } = "";
    public DeviceStatus Status { get; private set; } = DeviceStatus.NotInitialized;

    // ── Olaylar ───────────────────────────────────────────────────────

    /// <summary>Kart okuma, PIN girişi gibi anlık adım bilgileri.</summary>
    public event EventHandler<PosStepInfoEventArgs>? StepInfoReceived;

    /// <summary>POS'tan gelen fiş satırları (ECR yazdırma aktifse).</summary>
    public event EventHandler<PosSlipEventArgs>? SlipReceived;

    // ── IHardwareDevice ───────────────────────────────────────────────

    public Task InitializeAsync(IDeviceConfig config)
    {
        _config = (PosConfig)config;
        DeviceId = config.DeviceId;

        ImpProNative.Imp_SetXmlFilePath(_config.XmlConfigFilePath);

        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task ConnectAsync()
    {
        Status = DeviceStatus.Connecting;

        var xml = ImpProXmlHelper.BuildInterfaceXml(_config!);
        var xmlBytes = ImpProXmlHelper.Encode(xml);

        _handle = ImpProNative.Imp_CreateInterface(xmlBytes, xmlBytes.Length);

        if (_handle == IntPtr.Zero)
        {
            Status = DeviceStatus.Error;
            throw new InvalidOperationException(
                $"[{_config.DeviceName}] ImpProDLL: Imp_CreateInterface başarısız. " +
                "COM portu/IP adresi ve konfigürasyon dosyasını kontrol edin.");
        }

        Status = DeviceStatus.Connected;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        if (_handle != IntPtr.Zero)
        {
            ImpProNative.Imp_RemoveInterfaceByHandle(_handle);
            _handle = IntPtr.Zero;
        }

        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    // ── IPosTerminal ──────────────────────────────────────────────────

    public Task<PosPaymentResult> ProcessPaymentAsync(
        decimal amount,
        string currency = ImpProCurrency.TRY)
    {
        EnsureConnected();

        return RunTransactionAsync(
            ImpProTransType.Sale,
            amount,
            currency);
    }

    public Task<PosPaymentResult> ProcessRefundAsync(decimal amount, string originalAuthCode)
    {
        EnsureConnected();

        // İade için önce LastTransactionLookup ile TUID almak gerekebilir.
        // Burada originalAuthCode ile yeterli olan akış kullanılıyor.
        return RunTransactionAsync(
            ImpProTransType.Refund,
            amount,
            ImpProCurrency.TRY,
            originalAuthCode: originalAuthCode);
    }

    public Task CancelAsync()
    {
        if (_handle != IntPtr.Zero)
            ImpProNative.Imp_CancelReceive(_handle);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }

    // ── Ana işlem döngüsü ─────────────────────────────────────────────

    private async Task<PosPaymentResult> RunTransactionAsync(
        int transType,
        decimal amount,
        string currency,
        int installmentCount = 0,
        string? originalAuthCode = null)
    {
        ImpProNative.Imp_GenerateSesionID(out var sessionId);
        var amountKurus = (long)(amount * 100);

        // İşlem parametrelerini POS'a gönder
        string paramXml = transType == ImpProTransType.Void && originalAuthCode != null
            ? ImpProXmlHelper.BuildReverseXml("", originalAuthCode, amountKurus, currency, sessionId)
            : ImpProXmlHelper.BuildTransactionXml(
                transType, amountKurus, currency, sessionId,
                _config!.TimeoutSeconds, installmentCount);

        var paramBytes = ImpProXmlHelper.Encode(paramXml);
        var updateRc = ImpProNative.Imp_UpdateInterfaceXmlDataByHandle(
            _handle, paramBytes, paramBytes.Length);

        if (updateRc != ImpProRetCode.Success)
            return PosPaymentResult.Fail(updateRc.ToString(), GetErrorDescription(updateRc));

        var slipLines = new List<string>();

        // Step döngüsü — her çağrı bloklar, Task.Run ile UI thread'i koruyoruz
        return await Task.Run(() =>
        {
            while (true)
            {
                var rc = ImpProNative.Imp_ExecuteTransactionStep(_handle);

                if (rc != ImpProRetCode.Success && rc != ImpProRetCode.RecvEot)
                    return PosPaymentResult.Fail(rc.ToString(), GetErrorDescription(rc));

                var responseXml = ReadXmlFromHandle(_handle);
                var step = ImpProXmlHelper.ParseTransStep(responseXml);

                switch (step)
                {
                    case ImpProTransStep.TransApproval:
                    case ImpProTransStep.Finish:
                        var result = ImpProXmlHelper.ParseApprovalResult(responseXml, amount, currency);
                        return result with { SlipLines = slipLines.ToArray() };

                    case ImpProTransStep.PosStepInfo:
                        var stepInfo = ImpProXmlHelper.ParseStepInfo(responseXml);
                        StepInfoReceived?.Invoke(this, new PosStepInfoEventArgs
                        {
                            StepInfo    = stepInfo,
                            Description = StepInfoDescription(stepInfo)
                        });
                        break;

                    case ImpProTransStep.SlipInfo:
                        var lines = ImpProXmlHelper.ParseSlipLines(responseXml);
                        slipLines.AddRange(lines);
                        SlipReceived?.Invoke(this, new PosSlipEventArgs { Lines = lines });
                        break;

                    case ImpProTransStep.BankSelection:
                        // Çok bankalı kurulum: UI'da banka seçtirip
                        // Imp_UpdateInterfaceXmlDataByHandle ile bankaBkmID gönderilebilir.
                        // Tek bankalı kurulumda POS otomatik seçer, devam et.
                        break;

                    case ImpProTransStep.InfoRequest:
                    case ImpProTransStep.WaitingNextMessage:
                    case ImpProTransStep.Start:
                    default:
                        break; // döngüye devam
                }
            }
        });
    }

    // ── Yardımcılar ───────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (!Status.IsConnected || _handle == IntPtr.Zero)
            throw new InvalidOperationException("POS terminali bağlı değil.");
    }

    private static string ReadXmlFromHandle(IntPtr handle)
    {
        var buffer = new byte[XmlBufferSize];
        var len = XmlBufferSize;
        ImpProNative.Imp_GetInterfaceXmlDataByHandle(handle, buffer, ref len);
        return ImpProXmlHelper.Decode(buffer, len);
    }

    private string GetErrorDescription(int retCode)
    {
        var buffer = new byte[512];
        var len = buffer.Length;
        var rc = ImpProNative.Imp_GetErrorTurkishDescription(retCode, buffer, ref len);
        return rc == ImpProRetCode.Success
            ? ImpProXmlHelper.Decode(buffer, len)
            : $"Hata kodu: 0x{retCode:X4}";
    }

    private static string StepInfoDescription(int stepInfo) => stepInfo switch
    {
        ImpProStepInfo.WaitingCardRead         => "Kart bekleniyor...",
        ImpProStepInfo.CardInChipReader        => "Kart chip okuyucuda",
        ImpProStepInfo.MagstripeRead           => "Manyetik şerit okundu",
        ImpProStepInfo.PinIsRequested          => "PIN bekleniyor...",
        ImpProStepInfo.PinRetry                => "PIN tekrar girin",
        ImpProStepInfo.PinLastRetry            => "Son PIN denemesi!",
        ImpProStepInfo.PinIsBlocked            => "PIN bloke",
        ImpProStepInfo.PinIsBypassed           => "PIN atlandı",
        ImpProStepInfo.PinIsSuccess            => "PIN doğrulama başarılı",
        ImpProStepInfo.TransGoOnline           => "Banka ile iletişim kuruluyor...",
        ImpProStepInfo.ClessSuccessRead        => "Temassız kart okundu",
        ImpProStepInfo.CardMustBeRemoved       => "Kartı okuyucudan çıkarın",
        ImpProStepInfo.CardRemoved             => "Kart çıkarıldı",
        _ => $"Adım: {stepInfo}"
    };
}

// ── Event args ────────────────────────────────────────────────────────

public class PosStepInfoEventArgs : EventArgs
{
    public int StepInfo { get; init; }
    public string Description { get; init; } = "";
}

public class PosSlipEventArgs : EventArgs
{
    public string[] Lines { get; init; } = Array.Empty<string>();
}
