using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.POS;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Dummy;

public sealed partial class DummyPOSDevice(ILogger<DummyPOSDevice> logger) : IPOSDevice
{
    public string DeviceId => "DUMMY-POS-001";
    public string DeviceName => "Dummy Payment Terminal";

    public Task ConnectAsync(CancellationToken ct = default)
    {
        LogConnected(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        LogDisconnected(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task<DeviceStatusInfo> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new DeviceStatusInfo(DeviceState.Ready, LastSeen: DateTimeOffset.UtcNow));

    public async Task<PaymentResult> StartPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        LogPaymentStarted(logger, DeviceName, request.Amount, request.Currency, request.Method);
        await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);

        var txId = Guid.NewGuid().ToString("N")[..12];
        LogPaymentApproved(logger, DeviceName, txId);

        return new PaymentResult(
            PaymentOutcome.Approved,
            TransactionId: txId,
            AuthorizationCode: $"AUTH{Random.Shared.Next(100000, 999999)}",
            Method: request.Method);
    }

    public Task<PaymentResult> CancelPaymentAsync(CancellationToken ct = default)
    {
        LogPaymentCancelled(logger, DeviceName);
        return Task.FromResult(new PaymentResult(PaymentOutcome.Cancelled));
    }

    public async Task<PaymentResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        LogRefundStarted(logger, DeviceName, request.Amount, request.Currency, request.OriginalTransactionId);
        await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);

        return new PaymentResult(
            PaymentOutcome.Approved,
            TransactionId: Guid.NewGuid().ToString("N")[..12]);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Connected")]
    private static partial void LogConnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Disconnected")]
    private static partial void LogDisconnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Payment started: {Amount} {Currency} via {Method}")]
    private static partial void LogPaymentStarted(ILogger logger, string deviceName, decimal amount, string currency, PaymentMethod method);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Payment approved: {TransactionId}")]
    private static partial void LogPaymentApproved(ILogger logger, string deviceName, string transactionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Payment cancelled")]
    private static partial void LogPaymentCancelled(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Refund started: {Amount} {Currency} for {OriginalTxId}")]
    private static partial void LogRefundStarted(ILogger logger, string deviceName, decimal amount, string currency, string originalTxId);
}
