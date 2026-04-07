using System.Text;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>
/// DLL olmadan geliştirme ve test için sahte Ingenico implementasyonu.
/// Gerçekçi adım dizisi (kart bekleme → PIN → online → onay) simüle edilir.
///
/// Kullanım:
///   services.AddSingleton&lt;IImpProLibrary&gt;(_ => new FakeImpProLibrary { SimulateSuccess = true });
/// </summary>
public sealed class FakeImpProLibrary : IImpProLibrary
{
    private static readonly IntPtr FakeHandle = new(0x1234ABCD);
    private static readonly Encoding Turkish   = Encoding.GetEncoding("iso-8859-9");

    private int   _sessionCounter;
    private int   _stepIndex;
    private int[] _steps = [];

    public bool        SimulateSuccess { get; set; } = true;
    public TimeSpan    StepDelay       { get; set; } = TimeSpan.FromMilliseconds(300);
    public string[]    FakeSlipLines   { get; set; } = [];

    // ── IImpProLibrary ────────────────────────────────────────────────

    public int    SetXmlFilePath(string f)                            => ImpProRetCode.Success;
    public IntPtr CreateInterface(byte[] d, int l)                    => FakeHandle;
    public int    RemoveInterfaceByHandle(IntPtr h)                   => ImpProRetCode.Success;
    public int    CancelReceive(IntPtr h)                             => ImpProRetCode.Success;
    public int    ReverseTransaction(IntPtr h)                        => ImpProRetCode.Success;
    public int    LastTransactionLookup(IntPtr h)                     => ImpProRetCode.Success;

    public int GenerateSesionID(out int id) { id = ++_sessionCounter; return ImpProRetCode.Success; }

    public int UpdateInterfaceXmlDataByHandle(IntPtr h, byte[] d, int l)
    {
        _steps     = BuildSteps();
        _stepIndex = 0;
        return ImpProRetCode.Success;
    }

    public int GetInterfaceXmlDataByHandle(IntPtr h, byte[] buffer, ref int bufferLen)
    {
        var step  = _stepIndex > 0 ? _steps[_stepIndex - 1] : ImpProTransStep.Start;
        var xml   = BuildXml(step);
        var bytes = Turkish.GetBytes(xml);
        var len   = Math.Min(bytes.Length, bufferLen);
        Array.Copy(bytes, buffer, len);
        bufferLen = len;
        return ImpProRetCode.Success;
    }

    public int ExecuteTransactionStep(IntPtr h)
    {
        if (_stepIndex < _steps.Length)
        {
            Thread.Sleep(StepDelay);
            _stepIndex++;
        }
        return ImpProRetCode.Success;
    }

    public int GetErrorTurkishDescription(int code, byte[] b, ref int l)
    {
        var bytes = Turkish.GetBytes($"Fake hata: 0x{code:X4}");
        var len   = Math.Min(bytes.Length, l);
        Array.Copy(bytes, b, len);
        l = len;
        return ImpProRetCode.Success;
    }

    // ── Senaryo ───────────────────────────────────────────────────────

    private static int[] BuildSteps() =>
    [
        ImpProTransStep.Start,
        ImpProTransStep.PosStepInfo,   // WaitingCard
        ImpProTransStep.PosStepInfo,   // CardInChip
        ImpProTransStep.PosStepInfo,   // PinRequested
        ImpProTransStep.PosStepInfo,   // GoOnline
        ImpProTransStep.SlipInfo,
        ImpProTransStep.TransApproval,
        ImpProTransStep.Finish
    ];

    private string BuildXml(int step) => step switch
    {
        ImpProTransStep.PosStepInfo   => BuildStepInfoXml(),
        ImpProTransStep.SlipInfo      => BuildSlipXml(),
        ImpProTransStep.TransApproval or
        ImpProTransStep.Finish        => BuildApprovalXml(),
        _                             => $"<XML><TransStep>{step}</TransStep></XML>"
    };

    private string BuildStepInfoXml()
    {
        var info = _stepIndex switch
        {
            2 => ImpProStepInfo.WaitingCardRead,
            3 => ImpProStepInfo.CardInChipReader,
            4 => ImpProStepInfo.PinIsRequested,
            5 => ImpProStepInfo.TransGoOnline,
            _ => ImpProStepInfo.WaitingCardRead
        };
        return $"<XML><TransStep>{ImpProTransStep.PosStepInfo}</TransStep><PosTransStepInfo>{info}</PosTransStepInfo></XML>";
    }

    private string BuildSlipXml()
    {
        if (FakeSlipLines.Length == 0)
            return $"<XML><TransStep>{ImpProTransStep.SlipInfo}</TransStep><SlipLineCount>0</SlipLineCount></XML>";

        var lines = string.Join("", FakeSlipLines.Select((l, i) => $"<LineData{i}>{l}</LineData{i}>"));
        return $"<XML><TransStep>{ImpProTransStep.SlipInfo}</TransStep><SlipLineCount>{FakeSlipLines.Length}</SlipLineCount>{lines}</XML>";
    }

    private string BuildApprovalXml() => SimulateSuccess
        ? $"<XML><TransStep>{ImpProTransStep.TransApproval}</TransStep>" +
          $"<TransactionResult>0</TransactionResult>" +
          $"<szAuthorizationNumber>FAKE12</szAuthorizationNumber>" +
          $"<szRRN>999999999999</szRRN><szStan>999999</szStan>" +
          $"<szTransUniqueID>FAKE-TUID-001</szTransUniqueID>" +
          $"<szTransactionDateTime>{DateTime.Now:yyyyMMddHHmmss}</szTransactionDateTime>" +
          $"<szBankAppResponseCode>00</szBankAppResponseCode></XML>"
        : $"<XML><TransStep>{ImpProTransStep.TransApproval}</TransStep>" +
          $"<TransactionResult>1</TransactionResult>" +
          $"<szBankAppResponseCode>51</szBankAppResponseCode></XML>";
}
