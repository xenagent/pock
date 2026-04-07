using System.Runtime.InteropServices;

namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>
/// ImpProDLL.dll için ham P/Invoke bildirimleri.
/// DLL x86 (32-bit) native C, StdCall convention.
/// Projeyi x86 veya AnyCPU (Prefer 32-bit) olarak derle.
/// </summary>
internal static class ImpProNative
{
    private const string Dll = "ImpProDLL.dll";

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int Imp_SetXmlFilePath(string filePath);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr Imp_CreateInterface([In] byte[] xmlData, int xmlDataLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_RemoveInterfaceByHandle(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_UpdateInterfaceXmlDataByHandle(IntPtr hInterface, [In] byte[] xmlData, int xmlDataLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetInterfaceXmlDataByHandle(IntPtr hInterface, [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_ExecuteTransactionStep(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_CancelReceive(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_ReverseTransaction(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_LastTransactionLookup(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GenerateSesionID(out int sessionId);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetErrorTurkishDescription(int errorCode, [Out] byte[] buffer, ref int bufferLen);
}
