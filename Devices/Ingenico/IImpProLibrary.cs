namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

/// <summary>
/// ImpProDLL.dll metodlarının soyutlaması.
/// Production : <see cref="ImpProLibrary"/> (P/Invoke)
/// Test / Geliştirme : <see cref="FakeImpProLibrary"/> (DLL gerektirmez)
/// </summary>
public interface IImpProLibrary
{
    int     SetXmlFilePath(string filePath);
    IntPtr  CreateInterface(byte[] xmlData, int xmlDataLen);
    int     RemoveInterfaceByHandle(IntPtr hInterface);
    int     UpdateInterfaceXmlDataByHandle(IntPtr hInterface, byte[] xmlData, int xmlDataLen);
    int     GetInterfaceXmlDataByHandle(IntPtr hInterface, byte[] buffer, ref int bufferLen);
    int     ExecuteTransactionStep(IntPtr hInterface);
    int     CancelReceive(IntPtr hInterface);
    int     ReverseTransaction(IntPtr hInterface);
    int     LastTransactionLookup(IntPtr hInterface);
    int     GenerateSesionID(out int sessionId);
    int     GetErrorTurkishDescription(int errorCode, byte[] buffer, ref int bufferLen);
}
