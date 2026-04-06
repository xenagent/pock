namespace Devices.Pos;

/// <summary>
/// ImpProDLL.dll sabit değerleri.
/// Değerlerin tamamı Ingenico ImpPro SDK dokümantasyonundan alınmıştır.
/// Versiyon farklılığı durumunda DLL sağlayıcısıyla teyit et.
/// </summary>
public static class ImpProRetCode
{
    public const int Success           = 0x0000;
    public const int RecvEot           = 0x0001;
    public const int RecvAck           = 0x0002;
    public const int RecvNak           = 0x0003;
    public const int Timeout           = 0x0100;
    public const int PortOpenError     = 0x0101;
    public const int PortNotOpen       = 0x0102;
    public const int DataSendError     = 0x0103;
    public const int DataRecvError     = 0x0104;
    public const int AckNotReceived    = 0x0105;
    public const int LengthError       = 0x0200;
    public const int LenTooLong        = 0x0201;
    public const int ErrorStx          = 0x0202;
    public const int ErrorEtx          = 0x0203;
    public const int ErrorCrc          = 0x0204;
    public const int ErrorEot          = 0x0205;
    public const int ErrorErr          = 0x0206;
    public const int ErrorMsgType      = 0x0207;
    public const int MemoryError       = 0x0300;
    public const int InvalidStructSize = 0x0301;
    public const int DecryptionError   = 0x0302;
    public const int SessionIdError    = 0x0303;
    public const int JsonError         = 0x0400;
}

/// <summary>İşlem tipi (TransType XML alanı).</summary>
public static class ImpProTransType
{
    public const int Sale            = 1;  // Satış
    public const int Void            = 2;  // İptal
    public const int Refund          = 3;  // İade
    public const int PreAuth         = 4;  // Ön Otorizasyon
    public const int PreAuthComplete = 5;  // Ön Oto Tamamlama
    public const int Installment     = 6;  // Taksitli Satış (SubTransType ile birlikte)
    public const int PointInquiry    = 11; // Puan Sorgulama
    public const int PointPayment    = 12; // Puanla Ödeme
}

/// <summary>
/// İşlem adımı (TransStep XML alanı).
/// Imp_ExecuteTransactionStep blokladıktan sonra
/// Imp_GetInterfaceXmlDataByHandle ile bu değer okunur.
/// </summary>
public static class ImpProTransStep
{
    public const int Start              = 1;
    public const int BankSelection      = 2;
    public const int PosStepInfo        = 3;
    public const int SlipInfo           = 4;
    public const int InfoRequest        = 5;
    public const int TransApproval      = 6;
    public const int Finish             = 7;
    public const int WaitingNextMessage = 8;
}

/// <summary>POS adım bilgisi (StepInfo XML alanı — PosStepInfo adımında).</summary>
public static class ImpProStepInfo
{
    public const int WaitingCardRead          = 1;
    public const int CardInChipReader         = 2;
    public const int MagstripeRead            = 3;
    public const int ChipNotReadOrRemoveCard  = 4;
    public const int PinIsRequested           = 5;
    public const int PinRetry                 = 6;
    public const int PinLastRetry             = 7;
    public const int PinIsBlocked             = 8;
    public const int PinIsBypassed            = 9;
    public const int PinIsSuccess             = 10;
    public const int PinIsWrong               = 11;
    public const int PinEnd                   = 12;
    public const int TransGoOnline            = 13;
    public const int ClessSuccessRead         = 14;
    public const int ClessReadError           = 15;
    public const int CardMustBeRemoved        = 16;
    public const int CardRemoved              = 17;
    public const int RemoveCardForFallback    = 18;
}

/// <summary>Para birimi kodu (ISO 4217 nümerik).</summary>
public static class ImpProCurrency
{
    public const string TRY = "949";
    public const string USD = "840";
    public const string EUR = "978";
    public const string GBP = "826";
}

/// <summary>Bağlantı tipi (IsTcpConnection XML alanı).</summary>
public static class ImpProConnectionType
{
    public const int Serial = 0;
    public const int Tcp    = 1;
}
