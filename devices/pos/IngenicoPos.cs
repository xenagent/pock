using Devices.Common;
using Devices.Interfaces;

namespace Devices.Pos;

/// <summary>
/// Ingenico POS terminali.
/// DLL bağımlılığı IIngenicoLibrary arkasında izole edilmiştir;
/// DI veya test ortamında farklı bir implementasyon verilebilir.
/// </summary>
public class IngenicoPos : IPosTerminal
{
    private readonly IIngenicoLibrary _lib;
    private PosConfig? _config;

    public string DeviceId { get; private set; } = "";
    public DeviceStatus Status { get; private set; } = DeviceStatus.NotInitialized;

    /// <summary>
    /// Production: <c>new IngenicoPos(new IngenicoLibraryAdapter())</c>
    /// Test:       <c>new IngenicoPos(new FakeIngenicoLibrary())</c>
    /// </summary>
    public IngenicoPos(IIngenicoLibrary lib)
    {
        _lib = lib;
    }

    public Task InitializeAsync(IDeviceConfig config)
    {
        _config = (PosConfig)config;
        DeviceId = config.DeviceId;
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task ConnectAsync()
    {
        var rc = _lib.Initialize(_config!.TerminalId, _config.MerchantId);

        Status = rc == 0
            ? DeviceStatus.Connected
            : DeviceStatus.Error;

        if (rc != 0)
            throw new InvalidOperationException($"Ingenico init hatası: {rc}");

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task<PosPaymentResult> ProcessPaymentAsync(decimal amount, string currency = "TRY")
    {
        EnsureConnected();

        var amountKurus = (long)(amount * 100);
        var rc = _lib.StartSale(amountKurus, currency);

        if (rc != 0)
            return Task.FromResult(PosPaymentResult.Fail(rc.ToString(), "Satış başlatılamadı."));

        return Task.FromResult(BuildResult(amount, currency));
    }

    public Task<PosPaymentResult> ProcessRefundAsync(decimal amount, string originalAuthCode)
    {
        EnsureConnected();

        var amountKurus = (long)(amount * 100);
        var rc = _lib.StartRefund(amountKurus, "TRY", originalAuthCode);

        if (rc != 0)
            return Task.FromResult(PosPaymentResult.Fail(rc.ToString(), "İade başlatılamadı."));

        return Task.FromResult(BuildResult(amount, "TRY"));
    }

    public Task CancelAsync()
    {
        _lib.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose() => _lib.Dispose();

    // ── Helpers ────────────────────────────────────────────────────────
    private void EnsureConnected()
    {
        if (!Status.IsConnected)
            throw new InvalidOperationException("POS terminali bağlı değil.");
    }

    private PosPaymentResult BuildResult(decimal amount, string currency)
    {
        var r = _lib.GetLastResult();
        return new PosPaymentResult
        {
            Success           = r.ResponseCode == 0,
            AuthorizationCode = r.AuthorizationCode,
            ReferenceNumber   = r.ReferenceNumber,
            ErrorCode         = r.ResponseCode.ToString(),
            ErrorMessage      = r.ErrorDescription,
            Amount            = amount,
            Currency          = currency,
            ProcessedAt       = DateTime.Now
        };
    }
}
