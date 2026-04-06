using System.Reflection;
using System.Runtime.InteropServices;

namespace Devices.Pos;

/// <summary>
/// Gerçek Ingenico DLL'ini runtime'da yükler.
/// DLL, native (P/Invoke) ya da managed (.NET) olabilir;
/// her ikisi için de aynı arayüz kullanılır.
/// </summary>
public sealed class IngenicoLibraryAdapter : IIngenicoLibrary
{
    // ── Native export imzaları (DLL'e göre güncellenecek) ──────────────
    [DllImport("IngenicoPos.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int ING_Initialize([MarshalAs(UnmanagedType.LPStr)] string terminalId,
                                             [MarshalAs(UnmanagedType.LPStr)] string merchantId);

    [DllImport("IngenicoPos.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int ING_StartSale(long amountKurus,
                                            [MarshalAs(UnmanagedType.LPStr)] string currency);

    [DllImport("IngenicoPos.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int ING_StartRefund(long amountKurus,
                                              [MarshalAs(UnmanagedType.LPStr)] string currency,
                                              [MarshalAs(UnmanagedType.LPStr)] string originalAuthCode);

    [DllImport("IngenicoPos.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int ING_Cancel();

    [DllImport("IngenicoPos.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int ING_GetLastResult(out NativeTransactionResult result);

    // ── Native struct (DLL dokümantasyonuna göre düzenle) ──────────────
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct NativeTransactionResult
    {
        public int ResponseCode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string AuthorizationCode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ReferenceNumber;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string ErrorDescription;
    }

    // ── IIngenicoLibrary ───────────────────────────────────────────────
    public int Initialize(string terminalId, string merchantId)
        => ING_Initialize(terminalId, merchantId);

    public int StartSale(long amountKurus, string currency)
        => ING_StartSale(amountKurus, currency);

    public int StartRefund(long amountKurus, string currency, string originalAuthCode)
        => ING_StartRefund(amountKurus, currency, originalAuthCode);

    public int Cancel()
        => ING_Cancel();

    public IngenicoTransactionResult GetLastResult()
    {
        ING_GetLastResult(out var native);
        return new IngenicoTransactionResult
        {
            ResponseCode      = native.ResponseCode,
            AuthorizationCode = native.AuthorizationCode,
            ReferenceNumber   = native.ReferenceNumber,
            ErrorDescription  = native.ErrorDescription
        };
    }

    public void Dispose() { /* Native DLL kaynak serbest bırakma buraya */ }
}
