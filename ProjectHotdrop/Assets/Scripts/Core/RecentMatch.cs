using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>
    /// QA-025: the roster of the player's last online match, kept on the device so they can
    /// report someone they actually played with by picking their name — never by typing an
    /// internal account id. Bots and the player themselves are not listed.
    /// </summary>
    public static class RecentMatch
    {
        const string Key = "RecentMatchRoster";

        [Serializable] public class Entry { public string alias; public int slot; public int team; }
        [Serializable] class Roster { public string matchId; public string endedUtc; public List<Entry> players = new List<Entry>(); }

        public static void Remember(string matchId, IEnumerable<Entry> others)
        {
            if (string.IsNullOrEmpty(matchId)) return;
            var roster = new Roster { matchId = matchId, endedUtc = DateTime.UtcNow.ToString("o") };
            foreach (var e in others)
                if (e != null && !string.IsNullOrEmpty(e.alias) && !e.alias.StartsWith("[BOT]")) roster.players.Add(e);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(roster));
            PlayerPrefs.Save();
        }

        public static bool TryGet(out string matchId, out IReadOnlyList<Entry> players)
        {
            matchId = null; players = Array.Empty<Entry>();
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return false;
            var roster = JsonUtility.FromJson<Roster>(json);
            if (roster == null || string.IsNullOrEmpty(roster.matchId) || roster.players == null || roster.players.Count == 0) return false;
            matchId = roster.matchId; players = roster.players;
            return true;
        }

        public static void Forget() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}

