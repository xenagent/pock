using System.Text;

namespace Devices.Pos;

/// <summary>
/// DLL olmadan geliştirme ve test için sahte ImpProDLL implementasyonu.
///
/// Kullanım (DI):
///   #if DEBUG
///   services.AddSingleton&lt;IImpProLibrary, FakeImpProLibrary&gt;();
///   #else
///   services.AddSingleton&lt;IImpProLibrary, ImpProLibrary&gt;();
///   #endif
///
/// Ya da doğrudan:
///   new IngenicoPos(new FakeImpProLibrary { SimulateSuccess = true })
/// </summary>
public class FakeImpProLibrary : IImpProLibrary
{
    private static readonly IntPtr FakeHandle = new(0x1234ABCD);
    private static readonly Encoding Turkish = Encoding.GetEncoding("iso-8859-9");

    private int _sessionCounter = 100;
    private int _stepIndex;
    private int[] _steps = Array.Empty<int>();
    private string _slipXml = "";

    // ── Senaryo ayarları ──────────────────────────────────────────────

    /// <summary>true → başarılı ödeme simüle edilir, false → red.</summary>
    public bool SimulateSuccess { get; set; } = true;

    /// <summary>Her adım arasındaki gecikme (gerçekçi hissiyat için).</summary>
    public TimeSpan StepDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Slip satırları göndermek istiyorsan doldur.</summary>
    public string[] FakeSlipLines { get; set; } = Array.Empty<string>();

    // ── IImpProLibrary ────────────────────────────────────────────────

    public int SetXmlFilePath(string filePath) => ImpProRetCode.Success;

    public IntPtr CreateInterface(byte[] xmlData, int xmlDataLen) => FakeHandle;

    public int RemoveInterfaceByHandle(IntPtr hInterface) => ImpProRetCode.Success;

    public int UpdateInterfaceXmlDataByHandle(IntPtr hInterface, byte[] xmlData, int xmlDataLen)
    {
        // Gelen XML'den TransType'ı oku ve uygun adım dizisini hazırla
        var xml = Turkish.GetString(xmlData, 0, xmlDataLen);
        var transType = int.TryParse(ImpProXmlHelper.GetTag(xml, "TransType"), out var t) ? t : 1;

        _steps = BuildStepSequence(transType);
        _stepIndex = 0;
        _slipXml = BuildSlipXml();
        return ImpProRetCode.Success;
    }

    public int GetInterfaceXmlDataByHandle(IntPtr hInterface, byte[] buffer, ref int bufferLen)
    {
        var currentStep = _stepIndex > 0 ? _steps[_stepIndex - 1] : ImpProTransStep.Start;
        var xml = BuildResponseXml(currentStep);
        var bytes = Turkish.GetBytes(xml);
        var len = Math.Min(bytes.Length, bufferLen);
        Array.Copy(bytes, buffer, len);
        bufferLen = len;
        return ImpProRetCode.Success;
    }

    public int ExecuteTransactionStep(IntPtr hInterface)
    {
        if (_stepIndex >= _steps.Length) return ImpProRetCode.Success;

        Thread.Sleep(StepDelay); // gerçekçi gecikme
        _stepIndex++;
        return ImpProRetCode.Success;
    }

    public int CancelReceive(IntPtr hInterface) => ImpProRetCode.Success;
    public int ReverseTransaction(IntPtr hInterface) => ImpProRetCode.Success;
    public int LastTransactionLookup(IntPtr hInterface) => ImpProRetCode.Success;

    public int GenerateSesionID(out int sessionId)
    {
        sessionId = ++_sessionCounter;
        return ImpProRetCode.Success;
    }

    public int GetErrorTurkishDescription(int errorCode, byte[] buffer, ref int bufferLen)
    {
        var msg = $"Fake hata: 0x{errorCode:X4}";
        var bytes = Turkish.GetBytes(msg);
        var len = Math.Min(bytes.Length, bufferLen);
        Array.Copy(bytes, buffer, len);
        bufferLen = len;
        return ImpProRetCode.Success;
    }

    // ── Senaryo inşası ────────────────────────────────────────────────

    private static int[] BuildStepSequence(int transType) =>
    [
        ImpProTransStep.Start,
        ImpProTransStep.PosStepInfo,  // kart bekleniyor
        ImpProTransStep.PosStepInfo,  // kart okundu
        ImpProTransStep.PosStepInfo,  // PIN bekleniyor
        ImpProTransStep.PosStepInfo,  // online gidiyor
        ImpProTransStep.SlipInfo,
        ImpProTransStep.TransApproval,
        ImpProTransStep.Finish
    ];

    private string BuildResponseXml(int step)
    {
        var amount = 10000; // test miktarı

        return step switch
        {
            ImpProTransStep.PosStepInfo => BuildStepInfoXml(step),
            ImpProTransStep.SlipInfo    => _slipXml,
            ImpProTransStep.TransApproval or ImpProTransStep.Finish => BuildApprovalXml(amount),
            _ => $"<XML><TransStep>{step}</TransStep></XML>"
        };
    }

    private string BuildStepInfoXml(int step)
    {
        // Adım sırasına göre farklı step info değerleri
        var info = _stepIndex switch
        {
            2 => ImpProStepInfo.WaitingCardRead,
            3 => ImpProStepInfo.CardInChipReader,
            4 => ImpProStepInfo.PinIsRequested,
            5 => ImpProStepInfo.TransGoOnline,
            _ => ImpProStepInfo.WaitingCardRead
        };
        return $"<XML><TransStep>{step}</TransStep><PosTransStepInfo>{info}</PosTransStepInfo></XML>";
    }

    private string BuildApprovalXml(int amountKurus)
    {
        if (!SimulateSuccess)
        {
            return $@"<XML>
  <TransStep>{ImpProTransStep.TransApproval}</TransStep>
  <TransactionResult>1</TransactionResult>
  <szBankAppResponseCode>51</szBankAppResponseCode>
</XML>";
        }

        return $@"<XML>
  <TransStep>{ImpProTransStep.TransApproval}</TransStep>
  <TransactionResult>0</TransactionResult>
  <AuthorizedAmount>{amountKurus}</AuthorizedAmount>
  <szAuthorizationNumber>FAKE12</szAuthorizationNumber>
  <szRRN>999999999999</szRRN>
  <szStan>999999</szStan>
  <szTransUniqueID>FAKE-TUID-001</szTransUniqueID>
  <szTransactionDateTime>{DateTime.Now:yyyyMMddHHmmss}</szTransactionDateTime>
  <szBankAppResponseCode>00</szBankAppResponseCode>
</XML>";
    }

    private string BuildSlipXml()
    {
        if (FakeSlipLines.Length == 0) return $"<XML><TransStep>{ImpProTransStep.SlipInfo}</TransStep><SlipLineCount>0</SlipLineCount></XML>";

        var sb = new StringBuilder();
        sb.AppendLine($"<XML>");
        sb.AppendLine($"  <TransStep>{ImpProTransStep.SlipInfo}</TransStep>");
        sb.AppendLine($"  <SlipLineCount>{FakeSlipLines.Length}</SlipLineCount>");
        for (var i = 0; i < FakeSlipLines.Length; i++)
            sb.AppendLine($"  <LineData{i}>{FakeSlipLines[i]}</LineData{i}>");
        sb.AppendLine("</XML>");
        return sb.ToString();
    }
}
