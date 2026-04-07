namespace Devices.Pos;

/// <summary>
/// Gerçek ImpProDLL.dll P/Invoke implementasyonu.
/// Production ortamında kullanılır.
/// </summary>
public class ImpProLibrary : IImpProLibrary
{
    public int SetXmlFilePath(string filePath)
        => ImpProNative.Imp_SetXmlFilePath(filePath);

    public IntPtr CreateInterface(byte[] xmlData, int xmlDataLen)
        => ImpProNative.Imp_CreateInterface(xmlData, xmlDataLen);

    public int RemoveInterfaceByHandle(IntPtr hInterface)
        => ImpProNative.Imp_RemoveInterfaceByHandle(hInterface);

    public int UpdateInterfaceXmlDataByHandle(IntPtr hInterface, byte[] xmlData, int xmlDataLen)
        => ImpProNative.Imp_UpdateInterfaceXmlDataByHandle(hInterface, xmlData, xmlDataLen);

    public int GetInterfaceXmlDataByHandle(IntPtr hInterface, byte[] buffer, ref int bufferLen)
        => ImpProNative.Imp_GetInterfaceXmlDataByHandle(hInterface, buffer, ref bufferLen);

    public int ExecuteTransactionStep(IntPtr hInterface)
        => ImpProNative.Imp_ExecuteTransactionStep(hInterface);

    public int CancelReceive(IntPtr hInterface)
        => ImpProNative.Imp_CancelReceive(hInterface);

    public int ReverseTransaction(IntPtr hInterface)
        => ImpProNative.Imp_ReverseTransaction(hInterface);

    public int LastTransactionLookup(IntPtr hInterface)
        => ImpProNative.Imp_LastTransactionLookup(hInterface);

    public int GenerateSesionID(out int sessionId)
        => ImpProNative.Imp_GenerateSesionID(out sessionId);

    public int GetErrorTurkishDescription(int errorCode, byte[] buffer, ref int bufferLen)
        => ImpProNative.Imp_GetErrorTurkishDescription(errorCode, buffer, ref bufferLen);
}
