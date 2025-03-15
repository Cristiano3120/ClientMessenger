namespace ClientMessenger.Enums
{
    public enum OpCode : byte
    {
        ReceiveRSA = 0,
        SendAes = 1,
        ServerReadyToReceive = 2,
        RequestToCreateAccount = 3,
        AnswerToCreateAccount = 4,
        RequestToLogin = 5,
        AnswerToLogin = 6,
        VerificationProcess = 7,
        VerificationWentWrong = 8,
        AnswerToAutoLogin = 10,
        UpdateRelationship = 11,
        AnswerToRequestedRelationshipUpdate = 12,
        ReceiveRelationships = 13,
        ARelationshipWasUpdated = 14,
        SendChatMessage = 15,
        ReceiveChatMessage = 16,
        ReceiveChats = 17,
    }
}