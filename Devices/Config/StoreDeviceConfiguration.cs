using System.Text.Json.Serialization;
using KoopPOS.Checkout.Agent.Devices.Abstractions;
using KoopPOS.Checkout.Agent.Devices.Serial.Scale;
using KoopPOS.Checkout.Agent.Devices.Serial.Scanner;
using KoopPOS.Checkout.Agent.Devices.Ingenico;

namespace KoopPOS.Checkout.Agent.Devices.Config;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "deviceType")]
[JsonDerivedType(typeof(BarcodeReaderConfig), "barcode")]
[JsonDerivedType(typeof(ScaleConfig),          "scale")]
[JsonDerivedType(typeof(PosTerminalConfig),     "posTerminal")]
public class StoreDeviceConfiguration
{
    public string StoreId    { get; set; } = string.Empty;
    public string RegisterId { get; set; } = string.Empty;
    public List<IDeviceConfig> Devices { get; set; } = [];
}
