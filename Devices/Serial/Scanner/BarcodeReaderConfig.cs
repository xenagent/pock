using System.IO.Ports;
using KoopPOS.Checkout.Agent.Devices.Abstractions;

namespace KoopPOS.Checkout.Agent.Devices.Serial.Scanner;

public sealed class BarcodeReaderConfig : ISerialDeviceConfig
{
    public string DeviceId            { get; set; } = string.Empty;
    public string DeviceName          { get; set; } = string.Empty;
    public bool   Enabled             { get; set; } = true;
    public int    ReconnectIntervalMs { get; set; } = 5_000;
    public int    MaxReconnectAttempts{ get; set; } = 3;

    public string    PortName  { get; set; } = "COM3";
    public int       BaudRate  { get; set; } = 9600;
    public Parity    Parity    { get; set; } = Parity.None;
    public int       DataBits  { get; set; } = 8;
    public StopBits  StopBits  { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;
    public string    LineEnding{ get; set; } = "\r\n";

    public BarcodeConnectionMode ConnectionMode { get; set; } = BarcodeConnectionMode.SerialPort;
}

public enum BarcodeConnectionMode { SerialPort, UsbHid, KeyboardWedge }
