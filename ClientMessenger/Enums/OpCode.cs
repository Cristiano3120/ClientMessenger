﻿namespace ClientMessenger.Enums
{
    internal enum OpCode : byte
    {
        ReceiveRSA = 0,
        SendAes = 1,
        ServerReadyToReceive = 2,
        RequestCreateAccount = 3,
        AnswerCreateAccount = 4,
        RequestLogin = 5,
        AnswerLogin = 6,
        VerificationProcess = 7,
        VerificationWentWrong = 8,
        AutoLoginRequest = 9,
        AutoLoginResponse = 10,
    }
}
