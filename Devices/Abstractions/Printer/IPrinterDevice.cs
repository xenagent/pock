namespace KoopPOS.Checkout.Agent.Devices.Abstractions.Printer;

public interface IPrinterDevice : IDevice
{
    Task<PrintResult> PrintAsync(PrintJob job, CancellationToken ct = default);
    Task<bool> HasPaperAsync(CancellationToken ct = default);
    Task CutAsync(CancellationToken ct = default);
    Task OpenCashDrawerAsync(CancellationToken ct = default);
}
