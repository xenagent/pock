using System.Runtime.InteropServices;

namespace Devices.Pos;

/// <summary>
/// ImpProDLL.dll için ham P/Invoke bildirimleri.
/// DLL x86 (32-bit) native C, StdCall convention.
/// Projeyi x86 veya AnyCPU (32-bit preferred) olarak derlemeyi unutma.
/// </summary>
internal static class ImpProNative
{
    private const string Dll = "ImpProDLL.dll";

    // ── Başlatma ──────────────────────────────────────────────────────

    /// <summary>
    /// DLL'in kullanacağı XML konfigürasyon dosyasının yolunu ayarlar.
    /// Uygulama başlangıcında bir kez çağrılır.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int Imp_SetXmlFilePath(string filePath);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int Imp_GetXmlFilePath([Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetDllVersion([Out] byte[] buffer, ref int bufferLen);

    // ── Interface (bağlantı kanalı) ───────────────────────────────────

    /// <summary>
    /// POS terminaliyle yeni bir bağlantı kanalı açar.
    /// XML ile bağlantı tipi (Serial/TCP), port/IP, ECR kimliği verilir.
    /// Başarıda sıfır-olmayan HANDLE döner; hata durumunda IntPtr.Zero.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr Imp_CreateInterface([In] byte[] xmlData, int xmlDataLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_RemoveInterfaceByHandle(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_RemoveInterfaceByID(int interfaceId);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr Imp_GetInterfaceHandleByID(int interfaceId);

    // ── XML veri alışverişi ───────────────────────────────────────────

    /// <summary>
    /// Handle'a bağlı arayüzün XML verisini günceller.
    /// İşlem parametrelerini (TransType, Amount, vb.) bu yolla gönderirsin.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_UpdateInterfaceXmlDataByHandle(IntPtr hInterface,
        [In] byte[] xmlData, int xmlDataLen);

    /// <summary>
    /// Handle'a bağlı arayüzün güncel XML verisini okur.
    /// Her adımdan sonra TransStep, TransactionResult ve diğer alanları buradan çekersin.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetInterfaceXmlDataByHandle(IntPtr hInterface,
        [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetGlobalXmlData([Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_UpdateGlobalXmlData([In] byte[] xmlData, int xmlDataLen);

    // ── İşlem adımı ───────────────────────────────────────────────────

    /// <summary>
    /// Bir işlem adımını çalıştırır. Bir sonraki adım tamamlanana kadar BLOKLAR.
    /// Task.Run içinde çağrılmalı; UI thread'i dondurur.
    /// Dönen int değeri DLL_RETCODE'dur (0 = başarı).
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_ExecuteTransactionStep(IntPtr hInterface);

    /// <summary>Devam eden alımı iptal eder.</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_CancelReceive(IntPtr hInterface);

    /// <summary>EOT sinyali gönderir (bazı işlem tipleri gerektirir).</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_SendEOT(IntPtr hInterface);

    // ── İşlem sorgulama ───────────────────────────────────────────────

    /// <summary>Son işlemi tersine çevirir (void/iptal).</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_ReverseTransaction(IntPtr hInterface);

    /// <summary>Son işlem bilgilerini sorgular.</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_LastTransactionLookup(IntPtr hInterface);

    // ── POS'tan veri alma ─────────────────────────────────────────────

    /// <summary>
    /// POS cihazından kullanıcı girişi alır (PIN, menü seçimi vb.).
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetInputFromPOS(IntPtr hInterface,
        [Out] byte[] buffer, ref int bufferLen);

    // ── Banka / Terminal bilgisi ──────────────────────────────────────

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetBankList([Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetBankInfo(int bankBkmId, [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetTerminalInfo(IntPtr hInterface,
        [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_SetDefaultBank(int bankBkmId);

    // ── Yardımcılar ───────────────────────────────────────────────────

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GenerateSesionID(out int sessionId);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetErrorMessage(int errorCode,
        [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_GetErrorTurkishDescription(int errorCode,
        [Out] byte[] buffer, ref int bufferLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Imp_AddLog(int logLevel,
        [MarshalAs(UnmanagedType.LPStr)] string message);

    // ── JSON varyantları (XML yerine JSON döner) ──────────────────────

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr Json_Imp_CreateInterface([In] byte[] jsonData, int jsonDataLen);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Json_Imp_ExecuteTransactionStep(IntPtr hInterface);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int Json_Imp_GetInterfaceXmlDataByHandle(IntPtr hInterface,
        [Out] byte[] buffer, ref int bufferLen);
}
