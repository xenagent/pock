namespace KoopPOS.Checkout.Agent.Devices.Abstractions.POS;

public interface IPOSDevice : IDevice
{
    Task<PaymentResult> StartPaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentResult> CancelPaymentAsync(CancellationToken ct = default);
    Task<PaymentResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
}
