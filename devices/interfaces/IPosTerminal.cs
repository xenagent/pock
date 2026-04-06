using Devices.Pos;

namespace Devices.Interfaces;

public interface IPosTerminal : IHardwareDevice
{
    Task<PosPaymentResult> ProcessPaymentAsync(decimal amount, string currency = "TRY");
    Task<PosPaymentResult> ProcessRefundAsync(decimal amount, string originalAuthCode);
    Task CancelAsync();
}
