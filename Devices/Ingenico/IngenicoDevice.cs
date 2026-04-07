using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>
/// Ingenico POS terminali — TCP/IP üzerinden ImpProDLL.dll ile haberleşir.
///
/// Bağlantı kurulumu:
///   ImpProDLL → XML'deki IP:Port → Fiziksel Ingenico cihazı
///
/// İşlem akışı:
///   CreateInterface → UpdateInterfaceXmlData (parametreler)
///   → loop: ExecuteTransactionStep (BLOKLAR, kart/PIN/banka bekler)
///            → GetInterfaceXmlData ile adımı oku
///   → TransApproval/Finish → sonucu döndür → RemoveInterface
///
/// NOT: ImpProDLL.dll x86 native'dir; projeyi x86 / Prefer-32-bit derle.
/// </summary>
public sealed partial class IngenicoDevice(
    PosTerminalConfig config,
    ILogger<IngenicoDevice> logger) : IPOSDevice
{
    private const int XmlBufferSize = 65_536;

    private IntPtr      _handle = IntPtr.Zero;
    private DeviceState _state  = DeviceState.Disconnected;

    public string DeviceId   => config.DeviceId;
    public string DeviceName => config.DeviceName;

    // ── IDevice ───────────────────────────────────────────────────────

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _state = DeviceState.Connecting;
        ImpProNative.Imp_SetXmlFilePath(config.XmlConfigPath);

        var xmlBytes = ImpProXmlHelper.Encode(ImpProXmlHelper.BuildInterfaceXml(config));
        _handle = ImpProNative.Imp_CreateInterface(xmlBytes, xmlBytes.Length);

        if (_handle == IntPtr.Zero)
        {
            _state = DeviceState.Error;
            throw new InvalidOperationException(
                $"[{DeviceName}] CreateInterface başarısız — IP: {config.IpAddress}:{config.IpPort}. " +
                "ImpProConfig.xml ve ağ bağlantısını kontrol edin.");
        }

        _state = DeviceState.Ready;
        LogConnected(logger, DeviceName, config.IpAddress, config.IpPort);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_handle != IntPtr.Zero)
        {
            ImpProNative.Imp_RemoveInterfaceByHandle(_handle);
            _handle = IntPtr.Zero;
        }
        _state = DeviceState.Disconnected;
        return Task.CompletedTask;
    }

    public Task<DeviceStatusInfo> GetStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new DeviceStatusInfo(_state, LastSeen: DateTimeOffset.UtcNow));

    // ── IPOSDevice ────────────────────────────────────────────────────

    public Task<PaymentResult> StartPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        EnsureReady();
        return RunAsync(ImpProTransType.Sale, request.Amount, MapCurrency(request.Currency), ct: ct);
    }

    public Task<PaymentResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        EnsureReady();
        return RunAsync(ImpProTransType.Refund, request.Amount, MapCurrency(request.Currency),
                        originalAuthCode: request.OriginalTransactionId, ct: ct);
    }

    public Task<PaymentResult> CancelPaymentAsync(CancellationToken ct = default)
    {
        if (_handle != IntPtr.Zero)
            ImpProNative.Imp_CancelReceive(_handle);
        return Task.FromResult(new PaymentResult(PaymentOutcome.Cancelled));
    }

    public ValueTask DisposeAsync()
    {
        if (_handle != IntPtr.Zero)
        {
            ImpProNative.Imp_RemoveInterfaceByHandle(_handle);
            _handle = IntPtr.Zero;
        }
        return ValueTask.CompletedTask;
    }

    // ── Step döngüsü ─────────────────────────────────────────────────

    private async Task<PaymentResult> RunAsync(
        int transType, decimal amount, string currency,
        string? originalAuthCode = null, CancellationToken ct = default)
    {
        ImpProNative.Imp_GenerateSesionID(out var sessionId);
        var amountKurus = (long)(amount * 100);

        var paramXml = transType == ImpProTransType.Void && originalAuthCode is not null
            ? ImpProXmlHelper.BuildReverseXml(originalAuthCode, amountKurus, currency, sessionId)
            : ImpProXmlHelper.BuildTransactionXml(transType, amountKurus, currency, sessionId, config.TimeoutSeconds);

        var paramBytes = ImpProXmlHelper.Encode(paramXml);
        var rc = ImpProNative.Imp_UpdateInterfaceXmlDataByHandle(_handle, paramBytes, paramBytes.Length);

        if (rc != ImpProRetCode.Success)
            return new PaymentResult(PaymentOutcome.Error, ErrorMessage: GetError(rc));

        // Imp_ExecuteTransactionStep fiziksel cihaz yanıt verene kadar BLOKLAR.
        // Task.Run ile UI / servis thread'i serbest bırakıyoruz.
        return await Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                var ret = ImpProNative.Imp_ExecuteTransactionStep(_handle);

                if (ret != ImpProRetCode.Success && ret != ImpProRetCode.RecvEot)
                    return new PaymentResult(PaymentOutcome.Error, ErrorMessage: GetError(ret));

                var xml  = ReadXml();
                var step = ImpProXmlHelper.ParseTransStep(xml);

                switch (step)
                {
                    case ImpProTransStep.TransApproval:
                    case ImpProTransStep.Finish:
                        return BuildResult(xml, amount);

                    case ImpProTransStep.PosStepInfo:
                        LogStepInfo(logger, DeviceName,
                            StepDescription(ImpProXmlHelper.ParseStepInfo(xml)));
                        break;

                    case ImpProTransStep.SlipInfo:
                        LogSlip(logger, DeviceName,
                            ImpProXmlHelper.ParseSlipLines(xml).Length);
                        break;
                }
            }
            return new PaymentResult(PaymentOutcome.Cancelled);
        }, ct);
    }

    // ── Yardımcılar ───────────────────────────────────────────────────

    private void EnsureReady()
    {
        if (_state != DeviceState.Ready || _handle == IntPtr.Zero)
            throw new InvalidOperationException($"[{DeviceName}] POS terminali bağlı değil.");
    }

    private string ReadXml()
    {
        var buf = new byte[XmlBufferSize];
        var len = XmlBufferSize;
        ImpProNative.Imp_GetInterfaceXmlDataByHandle(_handle, buf, ref len);
        return ImpProXmlHelper.Decode(buf, len);
    }

    private string GetError(int code)
    {
        var buf = new byte[512];
        var len = buf.Length;
        return ImpProNative.Imp_GetErrorTurkishDescription(code, buf, ref len) == ImpProRetCode.Success
            ? ImpProXmlHelper.Decode(buf, len)
            : $"Hata: 0x{code:X4}";
    }

    private static PaymentResult BuildResult(string xml, decimal amount)
    {
        var resultCode = ImpProXmlHelper.ParseTransactionResult(xml);
        var bankCode   = ImpProXmlHelper.GetTag(xml, "szBankAppResponseCode");
        var approved   = resultCode == 0 && bankCode == "00";

        return new PaymentResult(
            approved ? PaymentOutcome.Approved : PaymentOutcome.Declined,
            TransactionId:     ImpProXmlHelper.GetTag(xml, "szTransUniqueID"),
            AuthorizationCode: ImpProXmlHelper.GetTag(xml, "szAuthorizationNumber"),
            ErrorMessage:      approved ? null : bankCode);
    }

    private static string MapCurrency(string iso4217) => iso4217.ToUpperInvariant() switch
    {
        "TRY" => ImpProCurrency.TRY,
        "USD" => ImpProCurrency.USD,
        "EUR" => ImpProCurrency.EUR,
        _     => ImpProCurrency.TRY
    };

    private static string StepDescription(int info) => info switch
    {
        ImpProStepInfo.WaitingCardRead   => "Kart bekleniyor...",
        ImpProStepInfo.CardInChipReader  => "Kart chip okuyucuda",
        ImpProStepInfo.MagstripeRead     => "Manyetik şerit okundu",
        ImpProStepInfo.PinIsRequested    => "PIN bekleniyor...",
        ImpProStepInfo.PinRetry          => "PIN tekrar girin",
        ImpProStepInfo.PinLastRetry      => "Son PIN denemesi!",
        ImpProStepInfo.PinIsBlocked      => "PIN bloke",
        ImpProStepInfo.PinIsBypassed     => "PIN atlandı",
        ImpProStepInfo.PinIsSuccess      => "PIN doğrulandı",
        ImpProStepInfo.TransGoOnline     => "Banka iletişimi kuruluyor...",
        ImpProStepInfo.ClessSuccessRead  => "Temassız kart okundu",
        ImpProStepInfo.CardMustBeRemoved => "Kartı okuyucudan çıkarın",
        ImpProStepInfo.CardRemoved       => "Kart çıkarıldı",
        _                                => $"Adım: {info}"
    };

    // ── Log ───────────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[{DeviceName}] Bağlandı → {Ip}:{Port}")]
    private static partial void LogConnected(ILogger l, string deviceName, string ip, int port);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[{DeviceName}] POS: {Description}")]
    private static partial void LogStepInfo(ILogger l, string deviceName, string description);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "[{DeviceName}] Fiş alındı ({LineCount} satır)")]
    private static partial void LogSlip(ILogger l, string deviceName, int lineCount);
}
