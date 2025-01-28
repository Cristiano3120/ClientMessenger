namespace ClientMessenger.Enums
{
    internal enum OpCode : byte
    {
        ReceiveRSA = 0,
        SendAes = 1,
        ServerReadyToreceive = 2,
        RequestCreateAccount = 3,
        AnswerCreateAccount = 4,
        RequestLogin = 5,
        AnswerLogin = 6,
        VerificationProcess = 7,
        VerificationWentWrong = 8,
    }
}
