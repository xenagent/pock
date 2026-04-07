using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>
/// Ingenico ImpProDLL.dll tabanlı POS terminali — <see cref="IPOSDevice"/> implementasyonu.
///
/// İşlem akışı:
///   CreateInterface → UpdateInterfaceXmlData (parametreler)
///   → loop: ExecuteTransactionStep (BLOKLAR) → GetInterfaceXmlData
///   → TransApproval/Finish adımında çık → RemoveInterface
///
/// NOT: ImpProDLL.dll x86 native'dir; projeyi x86 / Prefer-32-bit derle.
/// </summary>
public sealed partial class IngenicoDevice(
    PosTerminalConfig config,
    IImpProLibrary lib,
    ILogger<IngenicoDevice> logger) : IPOSDevice
{
    private const int XmlBufferSize = 65_536;

    private IntPtr     _handle = IntPtr.Zero;
    private DeviceState _state = DeviceState.Disconnected;

    public string DeviceId   => config.DeviceId;
    public string DeviceName => config.DeviceName;

    // ── IDevice ───────────────────────────────────────────────────────

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _state = DeviceState.Connecting;
        lib.SetXmlFilePath(config.XmlConfigPath);

        var xmlBytes = ImpProXmlHelper.Encode(ImpProXmlHelper.BuildInterfaceXml(config));
        _handle = lib.CreateInterface(xmlBytes, xmlBytes.Length);

        if (_handle == IntPtr.Zero)
        {
            _state = DeviceState.Error;
            throw new InvalidOperationException(
                $"[{DeviceName}] CreateInterface başarısız. Port/IP ve ImpProConfig.xml dosyasını kontrol edin.");
        }

        _state = DeviceState.Ready;
        LogConnected(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_handle != IntPtr.Zero)
        {
            lib.RemoveInterfaceByHandle(_handle);
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
        if (_handle != IntPtr.Zero) lib.CancelReceive(_handle);
        return Task.FromResult(new PaymentResult(PaymentOutcome.Cancelled));
    }

    public ValueTask DisposeAsync()
    {
        if (_handle != IntPtr.Zero) lib.RemoveInterfaceByHandle(_handle);
        return ValueTask.CompletedTask;
    }

    // ── Step döngüsü ─────────────────────────────────────────────────

    private async Task<PaymentResult> RunAsync(
        int transType, decimal amount, string currency,
        string? originalAuthCode = null, CancellationToken ct = default)
    {
        lib.GenerateSesionID(out var sessionId);
        var amountKurus = (long)(amount * 100);

        var paramXml = transType == ImpProTransType.Void && originalAuthCode is not null
            ? ImpProXmlHelper.BuildReverseXml(originalAuthCode, amountKurus, currency, sessionId)
            : ImpProXmlHelper.BuildTransactionXml(transType, amountKurus, currency, sessionId, config.TimeoutSeconds);

        var paramBytes = ImpProXmlHelper.Encode(paramXml);
        var rc = lib.UpdateInterfaceXmlDataByHandle(_handle, paramBytes, paramBytes.Length);
        if (rc != ImpProRetCode.Success)
            return new PaymentResult(PaymentOutcome.Error, ErrorMessage: GetError(rc));

        // Imp_ExecuteTransactionStep bloklar — Task.Run ile UI thread'i koruyoruz
        return await Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                var ret = lib.ExecuteTransactionStep(_handle);
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
                        LogStepInfo(logger, DeviceName, StepDesc(ImpProXmlHelper.ParseStepInfo(xml)));
                        break;

                    case ImpProTransStep.SlipInfo:
                        LogSlip(logger, DeviceName, ImpProXmlHelper.ParseSlipLines(xml).Length);
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
        lib.GetInterfaceXmlDataByHandle(_handle, buf, ref len);
        return ImpProXmlHelper.Decode(buf, len);
    }

    private string GetError(int code)
    {
        var buf = new byte[512];
        var len = buf.Length;
        return lib.GetErrorTurkishDescription(code, buf, ref len) == ImpProRetCode.Success
            ? ImpProXmlHelper.Decode(buf, len)
            : $"Hata: 0x{code:X4}";
    }

    private static PaymentResult BuildResult(string xml, decimal amount)
    {
        var result  = ImpProXmlHelper.ParseTransactionResult(xml);
        var resCode = ImpProXmlHelper.GetTag(xml, "szBankAppResponseCode");
        var success = result == 0 && resCode == "00";

        return new PaymentResult(
            success ? PaymentOutcome.Approved : PaymentOutcome.Declined,
            TransactionId:     ImpProXmlHelper.GetTag(xml, "szTransUniqueID"),
            AuthorizationCode: ImpProXmlHelper.GetTag(xml, "szAuthorizationNumber"),
            ErrorMessage:      success ? null : resCode);
    }

    private static string MapCurrency(string iso4217) => iso4217.ToUpperInvariant() switch
    {
        "TRY" => ImpProCurrency.TRY,
        "USD" => ImpProCurrency.USD,
        "EUR" => ImpProCurrency.EUR,
        _     => ImpProCurrency.TRY
    };

    private static string StepDesc(int info) => info switch
    {
        ImpProStepInfo.WaitingCardRead   => "Kart bekleniyor...",
        ImpProStepInfo.CardInChipReader  => "Kart chip okuyucuda",
        ImpProStepInfo.PinIsRequested    => "PIN bekleniyor...",
        ImpProStepInfo.PinRetry          => "PIN tekrar girin",
        ImpProStepInfo.PinLastRetry      => "Son PIN denemesi!",
        ImpProStepInfo.PinIsBlocked      => "PIN bloke",
        ImpProStepInfo.TransGoOnline     => "Banka iletişimi...",
        ImpProStepInfo.ClessSuccessRead  => "Temassız kart okundu",
        ImpProStepInfo.CardMustBeRemoved => "Kartı çıkarın",
        _                                => $"Adım {info}"
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Bağlandı")]
    private static partial void LogConnected(ILogger l, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] POS: {Description}")]
    private static partial void LogStepInfo(ILogger l, string deviceName, string description);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Fiş alındı ({LineCount} satır)")]
    private static partial void LogSlip(ILogger l, string deviceName, int lineCount);
}
