using Devices.Printers;

namespace Devices.Interfaces;

public interface IPrinter : IHardwareDevice
{
    Task PrintReceiptAsync(ReceiptDocument document);
    Task CutPaperAsync();
    Task OpenCashDrawerAsync();
}
