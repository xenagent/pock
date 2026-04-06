using System.Globalization;
using Devices.Common;

namespace Devices.Scales;

public sealed class ScaleProtocol : Enumeration
{
    public static readonly ScaleProtocol Standard = new(1, "Standard");
    public static readonly ScaleProtocol Mettler  = new(2, "Mettler");

    private ScaleProtocol(int id, string name) : base(id, name) { }

    public WeightReceivedEventArgs? Parse(string raw, string unit) => this switch
    {
        var x when x == Standard => ParseStandard(raw, unit),
        _ => null
    };

    private static WeightReceivedEventArgs? ParseStandard(string raw, string unit)
    {
        var numeric = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
            .Replace(',', '.');

        if (!decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return null;

        return new WeightReceivedEventArgs
        {
            Weight = val,
            Unit = unit,
            IsStable = raw.Contains("ST"),
            ReceivedAt = DateTime.Now
        };
    }
}
