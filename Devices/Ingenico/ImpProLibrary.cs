namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>Gerçek ImpProDLL.dll P/Invoke implementasyonu (production).</summary>
public sealed class ImpProLibrary : IImpProLibrary
{
    public int    SetXmlFilePath(string f)                                       => ImpProNative.Imp_SetXmlFilePath(f);
    public IntPtr CreateInterface(byte[] d, int l)                               => ImpProNative.Imp_CreateInterface(d, l);
    public int    RemoveInterfaceByHandle(IntPtr h)                              => ImpProNative.Imp_RemoveInterfaceByHandle(h);
    public int    UpdateInterfaceXmlDataByHandle(IntPtr h, byte[] d, int l)      => ImpProNative.Imp_UpdateInterfaceXmlDataByHandle(h, d, l);
    public int    GetInterfaceXmlDataByHandle(IntPtr h, byte[] b, ref int l)     => ImpProNative.Imp_GetInterfaceXmlDataByHandle(h, b, ref l);
    public int    ExecuteTransactionStep(IntPtr h)                               => ImpProNative.Imp_ExecuteTransactionStep(h);
    public int    CancelReceive(IntPtr h)                                        => ImpProNative.Imp_CancelReceive(h);
    public int    ReverseTransaction(IntPtr h)                                   => ImpProNative.Imp_ReverseTransaction(h);
    public int    LastTransactionLookup(IntPtr h)                                => ImpProNative.Imp_LastTransactionLookup(h);
    public int    GenerateSesionID(out int id)                                   => ImpProNative.Imp_GenerateSesionID(out id);
    public int    GetErrorTurkishDescription(int code, byte[] b, ref int l)      => ImpProNative.Imp_GetErrorTurkishDescription(code, b, ref l);
}
