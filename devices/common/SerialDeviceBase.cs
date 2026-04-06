using System.IO.Ports;
using System.Text;
using Devices.Interfaces;

namespace Devices.Common;

public abstract class SerialDeviceBase : IHardwareDevice
{
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();
    private ISerialDeviceConfig? _config;

    public string DeviceId { get; private set; } = "";
    public DeviceStatus Status { get; private set; } = DeviceStatus.NotInitialized;

    public virtual Task InitializeAsync(IDeviceConfig config)
    {
        _config = (ISerialDeviceConfig)config;
        DeviceId = config.DeviceId;
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task ConnectAsync()
    {
        _port = new SerialPort(_config!.PortName, _config.BaudRate);
        _port.DataReceived += OnData;
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

    private void OnData(object sender, SerialDataReceivedEventArgs e)
    {
        var data = _port!.ReadExisting();
        _buffer.Append(data);

        var ending = _config!.LineEnding;

        while (_buffer.ToString().Contains(ending))
        {
            var str = _buffer.ToString();
            var idx = str.IndexOf(ending);
            var line = str[..idx].Trim();

            _buffer.Remove(0, idx + ending.Length);

            if (!string.IsNullOrEmpty(line))
                ProcessLine(line);
        }
    }

    protected abstract void ProcessLine(string line);

    public void Dispose()
    {
        _port?.Dispose();
    }
}
