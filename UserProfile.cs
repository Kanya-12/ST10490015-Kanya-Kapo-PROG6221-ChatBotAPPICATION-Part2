using System;
using System.Collections.Generic;

namespace CyberSecurityChatBot
{
    public class UserProfile
    {
        public string UserName { get; set; } = "";
        public List<string> FavoriteTopics { get; set; } = new List<string>();
        public List<ConversationEntry> Conversations { get; set; } = new List<ConversationEntry>();
        public string ThemePreference { get; set; } = "Dark";
    }

    public class ConversationEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Role { get; set; } // "user" or "bot"
        public string Message { get; set; }
    }
}
