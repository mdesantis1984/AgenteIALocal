using System;
using AgenteIALocalVSIX.Chats;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        // DefaultChatService moved to separate file but remains nested for private access.
        internal sealed class DefaultChatService : IChatService
        {
            private readonly AgenteIALocalControl _owner;

            public DefaultChatService(AgenteIALocalControl owner)
            {
                _owner = owner;
            }

            public void EnsureActiveChatExists() => _owner.EnsureActiveChatExists();
            public void AddChatMessage(ChatSession chat, string sender, string content, int? tokens) => _owner.AddChatMessage(chat, sender, content, tokens);
            public void TryPersistChat(ChatSession chat) => _owner.TryPersistChat(chat);
            public void RefreshChatCombo() => _owner.RefreshChatCombo();
            public void LoadActiveChatToUi() => _owner.LoadActiveChatToUi();
            public void RenderActiveChatToUi() => _owner.RenderActiveChatToUi();
            public void TryRemoveEmptyAiBubble(ChatSession chat, ChatMessage aiBubble) => _owner.TryRemoveEmptyAiBubble(chat, aiBubble);
            public void TryUpdateLastUserBubbleTokens(ChatSession chat, int tokens) => _owner.TryUpdateLastUserBubbleTokens(chat, tokens);
            public void TrySetTokensForMessage(string chatId, DateTime tsUtc, string sender, string content, int tokens) => _owner.TrySetTokensForMessage(chatId, tsUtc, sender, content, tokens);
            public DateTime TryGetDateTimeProp(object obj, string propName) => AgenteIALocalControl.TryGetDateTimeProp(obj, propName);
        }
    }
}
