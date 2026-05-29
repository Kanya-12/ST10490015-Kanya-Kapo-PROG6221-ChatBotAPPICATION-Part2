using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CyberSecurityChatBot
{
    public class MemoryManager
    {
        private const string MemoryFile = "user_memory.json";

        public UserProfile Profile { get; private set; } = new UserProfile();

        public MemoryManager()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(MemoryFile))
                {
                    string json = File.ReadAllText(MemoryFile);
                    Profile = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile();
                }
            }
            catch
            {
                // ignore corrupt memory file
                Profile = new UserProfile();
            }
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Profile, options);
                File.WriteAllText(MemoryFile, json);
            }
            catch
            {
                // non-fatal
            }
        }

        public void UpdateUserName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Profile.UserName = name.Trim();
                Save();
            }
        }

        public void AddConversationEntry(ConversationEntry entry)
        {
            if (Profile.Conversations == null)
                Profile.Conversations = new List<ConversationEntry>();

            Profile.Conversations.Add(entry);
            // Keep last 200 entries to avoid unbounded growth
            if (Profile.Conversations.Count > 200)
                Profile.Conversations.RemoveRange(0, Profile.Conversations.Count - 200);

            Save();
        }
    }
}
