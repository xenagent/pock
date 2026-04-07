namespace KoopPOS.Checkout.Agent.Devices.Ingenico;

public static class ImpProRetCode
{
    public const int Success       = 0x0000;
    public const int RecvEot       = 0x0001;
    public const int Timeout       = 0x0100;
    public const int PortOpenError = 0x0101;
    public const int MemoryError   = 0x0300;
    public const int SessionIdError= 0x0303;
}

public static class ImpProTransType
{
    public const int Sale            = 1;
    public const int Void            = 2;
    public const int Refund          = 3;
    public const int PreAuth         = 4;
    public const int PreAuthComplete = 5;
}

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

public static class ImpProStepInfo
{
    public const int WaitingCardRead        = 1;
    public const int CardInChipReader       = 2;
    public const int MagstripeRead          = 3;
    public const int PinIsRequested         = 5;
    public const int PinRetry               = 6;
    public const int PinLastRetry           = 7;
    public const int PinIsBlocked           = 8;
    public const int PinIsBypassed          = 9;
    public const int PinIsSuccess           = 10;
    public const int TransGoOnline          = 13;
    public const int ClessSuccessRead       = 14;
    public const int CardMustBeRemoved      = 16;
    public const int CardRemoved            = 17;
}

public static class ImpProCurrency
{
    public const string TRY = "949";
    public const string USD = "840";
    public const string EUR = "978";
}
