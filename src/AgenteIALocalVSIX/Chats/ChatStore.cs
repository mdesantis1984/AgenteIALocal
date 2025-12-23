using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AgenteIALocalVSIX.Chats
{
    public static class ChatStore
    {
        private static string GetChatsDirectory()
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = Path.Combine(local ?? string.Empty, "AgenteIALocal", "chats");
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch
            {
                return Path.Combine(".", "chats");
            }
        }

        public static IEnumerable<ChatSession> LoadAll()
        {
            var dir = GetChatsDirectory();
            var list = new List<ChatSession>();
            try
            {
                var files = Directory.GetFiles(dir, "*.json");
                foreach (var f in files)
                {
                    try
                    {
                        var txt = File.ReadAllText(f, Encoding.UTF8);
                        var s = JsonConvert.DeserializeObject<ChatSession>(txt);
                        if (s != null) list.Add(s);
                    }
                    catch
                    {
                        // skip corrupted
                    }
                }
            }
            catch
            {
                // ignore
            }

            // sort by LastUpdated desc
            return list.OrderByDescending(x => x.LastUpdated ?? x.CreatedAt).ToList();
        }

        public static ChatSession CreateNew(string title = null)
        {
            var now = DateTime.UtcNow.ToString("o");
            var session = new ChatSession
            {
                Id = Guid.NewGuid().ToString(),
                Title = title ?? "New chat",
                CreatedAt = now,
                LastUpdated = now
            };

            Save(session);
            return session;
        }

        public static void Save(ChatSession session)
        {
            try
            {
                var dir = GetChatsDirectory();
                var path = Path.Combine(dir, session.Id + ".json");
                session.LastUpdated = DateTime.UtcNow.ToString("o");
                var txt = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(path, txt, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        public static void Delete(string id)
        {
            try
            {
                var dir = GetChatsDirectory();
                var path = Path.Combine(dir, id + ".json");
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        public static ChatSession Load(string id)
        {
            try
            {
                var dir = GetChatsDirectory();
                var path = Path.Combine(dir, id + ".json");
                if (!File.Exists(path)) return null;
                var txt = File.ReadAllText(path, Encoding.UTF8);
                var s = JsonConvert.DeserializeObject<ChatSession>(txt);
                return s;
            }
            catch
            {
                return null;
            }
        }
    }
}
