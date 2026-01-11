using System;

namespace AgenteIALocalVSIX.Chats
{
    internal interface IChatService
    {
        void EnsureActiveChatExists();
        void AddChatMessage(ChatSession chat, string sender, string content, int? tokens);
        void TryPersistChat(ChatSession chat);
        void RefreshChatCombo();
        void LoadActiveChatToUi();
        void RenderActiveChatToUi();
        void TryRemoveEmptyAiBubble(ChatSession chat, ChatMessage aiBubble);
        void TryUpdateLastUserBubbleTokens(ChatSession chat, int tokens);
        void TrySetTokensForMessage(string chatId, DateTime tsUtc, string sender, string content, int tokens);
        DateTime TryGetDateTimeProp(object obj, string propName);
    }
}
