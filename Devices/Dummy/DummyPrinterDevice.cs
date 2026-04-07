using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Abstractions.Printer;
using Microsoft.Extensions.Logging;

namespace KoopPOS.Checkout.Agent.Devices.Dummy;

public sealed partial class DummyPrinterDevice(ILogger<DummyPrinterDevice> logger) : IPrinterDevice
{
    public string DeviceId => "DUMMY-PRINTER-001";
    public string DeviceName => "Dummy Receipt Printer";

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

    public Task<PrintResult> PrintAsync(PrintJob job, CancellationToken ct = default)
    {
        LogPrinting(logger, DeviceName, job.Lines.Count);
        foreach (var line in job.Lines)
            LogPrintLine(logger, line.Alignment, line.Bold, line.Text);

        return Task.FromResult(new PrintResult(true));
    }

    public Task<bool> HasPaperAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task CutAsync(CancellationToken ct = default)
    {
        LogPaperCut(logger, DeviceName);
        return Task.CompletedTask;
    }

    public Task OpenCashDrawerAsync(CancellationToken ct = default)
    {
        LogCashDrawerOpened(logger, DeviceName);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Connected")]
    private static partial void LogConnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Disconnected")]
    private static partial void LogDisconnected(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Printing {LineCount} lines")]
    private static partial void LogPrinting(ILogger logger, string deviceName, int lineCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  [{Alignment}]{Bold} {Text}")]
    private static partial void LogPrintLine(ILogger logger, PrintAlignment alignment, bool bold, string text);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[{DeviceName}] Paper cut")]
    private static partial void LogPaperCut(ILogger logger, string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{DeviceName}] Cash drawer opened")]
    private static partial void LogCashDrawerOpened(ILogger logger, string deviceName);
}
