using System;
using System.Collections.Generic;

namespace AgenteIALocalVSIX.Chats
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
    }

    public class ChatSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CreatedAt { get; set; }
        public string LastUpdated { get; set; }
        public List<ChatMessage> Messages { get; set; }

        public ChatSession()
        {
            Messages = new List<ChatMessage>();
        }
    }
}