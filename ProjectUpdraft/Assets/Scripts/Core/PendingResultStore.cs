using System;
using System.Collections.Generic;
using System.IO;

namespace Updraft.Core
{
    /// <summary>
    /// QA-017: durable outbox for match results the dedicated server could not deliver.
    /// One JSON file per match id; the server retries them on start and between matches
    /// until the backend accepts them. The backend records each participant once per match,
    /// so re-delivery can never grant a reward twice.
    /// </summary>
    public sealed class PendingResultStore
    {
        readonly string directory;

        public PendingResultStore(string directory)
        {
            this.directory = directory;
            Directory.CreateDirectory(directory);
        }

        string PathFor(string matchId) => Path.Combine(directory, Sanitize(matchId) + ".json");

        public void Save(string matchId, string resultJson)
        {
            string final = PathFor(matchId);
            string temp = final + ".tmp";
            File.WriteAllText(temp, resultJson);   // write-then-rename: a crash never leaves half a result
            if (File.Exists(final)) File.Delete(final);
            File.Move(temp, final);
        }

        public IReadOnlyList<(string matchId, string json)> Pending()
        {
            var list = new List<(string, string)>();
            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try { list.Add((Path.GetFileNameWithoutExtension(file), File.ReadAllText(file))); }
                catch (IOException) { /* locked or vanished: retried next pass */ }
            }
            return list;
        }

        public void Remove(string matchId)
        {
            string file = PathFor(matchId);
            if (File.Exists(file)) File.Delete(file);
        }

        static string Sanitize(string matchId)
        {
            var chars = (matchId ?? "").ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-') chars[i] = '_';
            return chars.Length == 0 ? "unknown" : new string(chars);
        }
    }
}
