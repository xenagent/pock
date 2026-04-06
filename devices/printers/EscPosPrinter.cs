using System.IO.Ports;
using System.Text;
using Devices.Common;
using Devices.Interfaces;

namespace Devices.Printers;

/// <summary>
/// ESC/POS protokolü kullanan serial yazıcı (Epson, Bixolon, Citizen vb.).
/// </summary>
public class EscPosPrinter : IPrinter
{
    // ESC/POS komutları
    private static readonly byte[] CmdInit       = { 0x1B, 0x40 };
    private static readonly byte[] CmdCut        = { 0x1D, 0x56, 0x41, 0x00 };
    private static readonly byte[] CmdCashDrawer  = { 0x1B, 0x70, 0x00, 0x19, 0xFF };
    private static readonly byte[] CmdBoldOn      = { 0x1B, 0x45, 0x01 };
    private static readonly byte[] CmdBoldOff     = { 0x1B, 0x45, 0x00 };
    private static readonly byte[] CmdAlignLeft   = { 0x1B, 0x61, 0x00 };
    private static readonly byte[] CmdAlignCenter = { 0x1B, 0x61, 0x01 };
    private static readonly byte[] CmdAlignRight  = { 0x1B, 0x61, 0x02 };
    private static readonly byte[] CmdDblHeightOn  = { 0x1B, 0x21, 0x10 };
    private static readonly byte[] CmdDblHeightOff = { 0x1B, 0x21, 0x00 };

    private SerialPort? _port;
    private PrinterConfig? _config;

    public string DeviceId { get; private set; } = "";
    public DeviceStatus Status { get; private set; } = DeviceStatus.NotInitialized;

    public Task InitializeAsync(IDeviceConfig config)
    {
        _config = (PrinterConfig)config;
        DeviceId = config.DeviceId;
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task ConnectAsync()
    {
        _port = new SerialPort(_config!.PortName, _config.BaudRate);
        _port.Open();
        Status = DeviceStatus.Connected;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        _port?.Close();
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task PrintReceiptAsync(ReceiptDocument document)
    {
        EnsureConnected();
        var width = _config!.LineWidth;

        Write(CmdInit);

        // Header
        if (!string.IsNullOrEmpty(document.Header))
        {
            Write(CmdAlignCenter);
            Write(CmdBoldOn);
            WriteLine(document.Header);
            Write(CmdBoldOff);
            WriteLine(new string('-', width));
        }

        // Lines
        foreach (var line in document.Lines)
        {
            switch (line.Type)
            {
                case ReceiptLineType.Separator:
                    Write(CmdAlignLeft);
                    WriteLine(new string('-', width));
                    break;

                case ReceiptLineType.Text:
                    WriteTextLine(line, width);
                    break;
            }
        }

        // Footer
        if (!string.IsNullOrEmpty(document.Footer))
        {
            WriteLine(new string('-', width));
            Write(CmdAlignCenter);
            WriteLine(document.Footer);
        }

        if (document.AutoCut)
            Write(CmdCut);

        return Task.CompletedTask;
    }

    public Task CutPaperAsync()
    {
        EnsureConnected();
        Write(CmdCut);
        return Task.CompletedTask;
    }

    public Task OpenCashDrawerAsync()
    {
        EnsureConnected();
        Write(CmdCashDrawer);
        return Task.CompletedTask;
    }

    public void Dispose() => _port?.Dispose();

    // ── Helpers ────────────────────────────────────────────────────────
    private void EnsureConnected()
    {
        if (!Status.IsConnected)
            throw new InvalidOperationException("Yazıcı bağlı değil.");
    }

    private void WriteTextLine(ReceiptLine line, int width)
    {
        if (line.Bold) Write(CmdBoldOn);
        if (line.DoubleHeight) Write(CmdDblHeightOn);

        Write(line.Alignment switch
        {
            TextAlignment.Center => CmdAlignCenter,
            TextAlignment.Right  => CmdAlignRight,
            _                    => CmdAlignLeft
        });

        // Sol+sağ metin varsa boşluk doldur
        if (!string.IsNullOrEmpty(line.RightText))
        {
            var gap = width - line.Text.Length - line.RightText.Length;
            WriteLine(line.Text + new string(' ', Math.Max(1, gap)) + line.RightText);
        }
        else
        {
            WriteLine(line.Text);
        }

        if (line.Bold) Write(CmdBoldOff);
        if (line.DoubleHeight) Write(CmdDblHeightOff);
    }

    private void Write(byte[] cmd) => _port!.Write(cmd, 0, cmd.Length);

    private void WriteLine(string text)
    {
        var bytes = Encoding.GetEncoding("cp857").GetBytes(text + "\n");
        _port!.Write(bytes, 0, bytes.Length);
    }
}
