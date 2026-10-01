using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Updraft.Core
{
    /// <summary>
    /// Spec section 24 schema-versioned events. Every event carries build and
    /// balance version, a random pseudonymous id (never a device or account
    /// identifier), match id, region, device tier, party size, bot flag, cohort
    /// and timestamp. Nothing personal (contacts, chat) is ever collected.
    ///
    /// Events are appended to a local JSON-lines file so offline playtests (the
    /// pad study) can be collected with `adb pull`, and client-owned events are
    /// uploaded by BackendClient when a backend is configured. Offline match
    /// facts are marked source=offline_sim: the server remains the only source
    /// of match facts for online play.
    /// </summary>
    public static class Telemetry
    {
        public const int SchemaVersion = 1;
        public const string QueueFileName = "analytics_queue.jsonl";
        const long MaxQueueBytes = 4 * 1024 * 1024;
        const string PseudonymKey = "Telemetry.PseudonymousId";
        public const string CohortKey = "Study.Cohort";
        public const string IncentivizedTesterKey = "Study.IncentivizedTester";

        public static string BalanceVersion = "v1.0-spec";
        public static string CurrentMatchId = "";
        public static bool CurrentMatchHasBots;
        public static bool CurrentMatchIsBotSubstituted;
        public static int CurrentMatchBotCount;
        public static int PartySize = 1;
        public static string Region = "offline"; // QA-027: the joined match region online, "offline" otherwise

        static string pseudonymousId;
        static readonly StringBuilder sb = new StringBuilder(512);

        public static string QueuePath => Path.Combine(Application.persistentDataPath, QueueFileName);

        public static bool IncentivizedTester => PlayerPrefs.GetInt(IncentivizedTesterKey, 0) != 0;

        public static string PseudonymousId
        {
            get
            {
                if (!string.IsNullOrEmpty(pseudonymousId)) return pseudonymousId;
                pseudonymousId = PlayerPrefs.GetString(PseudonymKey, "");
                if (string.IsNullOrEmpty(pseudonymousId))
                {
                    pseudonymousId = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PseudonymKey, pseudonymousId);
                    PlayerPrefs.Save();
                }
                return pseudonymousId;
            }
        }

        public static string Cohort => PlayerPrefs.GetString(CohortKey, "default");

        /// <summary>low (&lt;= 3 GB), mid (&lt;= 6 GB), high — matches the spec's device matrix tiers.</summary>
        public static string DeviceTier
        {
            get
            {
                int mb = SystemInfo.systemMemorySize;
                return mb <= 3500 ? "low" : mb <= 6500 ? "mid" : "high";
            }
        }

        /// <summary>Record an event. payload values are flattened into the JSON "payload" object.</summary>
        public static void Track(string eventName, params (string key, object value)[] payload) =>
            Track(eventName, false, payload);

        public static void TrackOfflineMatchFact(string eventName, params (string key, object value)[] payload) =>
            Track(eventName, true, payload);

        static void Track(string eventName, bool offlineMatchFact, (string key, object value)[] payload)
        {
            try
            {
                sb.Clear();
                sb.Append('{');
                Field("eventName", eventName); sb.Append(',');
                sb.Append("\"schemaVersion\":").Append(SchemaVersion).Append(',');
                Field("source", offlineMatchFact ? "offline_sim" : "client"); sb.Append(',');
                Field("buildVersion", Application.version); sb.Append(',');
                Field("balanceVersion", BalanceVersion); sb.Append(',');
                Field("pseudonymousId", PseudonymousId); sb.Append(',');
                Field("matchId", CurrentMatchId); sb.Append(',');
                Field("region", Region); sb.Append(',');
                Field("deviceTier", DeviceTier); sb.Append(',');
                sb.Append("\"partySize\":").Append(PartySize).Append(',');
                sb.Append("\"hasBots\":").Append(CurrentMatchHasBots ? "true" : "false").Append(',');
                sb.Append("\"botSubstituted\":").Append(CurrentMatchIsBotSubstituted ? "true" : "false").Append(',');
                sb.Append("\"botCount\":").Append(CurrentMatchBotCount).Append(',');
                sb.Append("\"incentivizedTester\":").Append(IncentivizedTester ? "true" : "false").Append(',');
                Field("cohort", Cohort); sb.Append(',');
                Field("timestamp", DateTime.UtcNow.ToString("o")); sb.Append(',');
                sb.Append("\"payload\":{");
                for (int i = 0; i < payload.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(Escape(payload[i].key)).Append("\":");
                    AppendValue(payload[i].value);
                }
                sb.Append("}}\n");
                Append(sb.ToString());
            }
            catch (Exception e)
            {
                // Telemetry must never break gameplay.
                Debug.LogWarning("Telemetry write failed: " + e.Message);
            }
        }

        static void Append(string line)
        {
            string path = QueuePath;
            var info = new FileInfo(path);
            if (info.Exists && info.Length > MaxQueueBytes) return; // bounded; oldest data kept for the study
            File.AppendAllText(path, line);
        }

        public static string[] ReadQueued() => File.Exists(QueuePath) ? File.ReadAllLines(QueuePath) : Array.Empty<string>();

        public static void ReplaceQueue(IEnumerable<string> remaining)
        {
            var text = string.Join("\n", remaining);
            File.WriteAllText(QueuePath, text.Length > 0 ? text + "\n" : "");
        }

        static void Field(string key, string value) => sb.Append('"').Append(key).Append("\":\"").Append(Escape(value ?? "")).Append('"');

        static void AppendValue(object value)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int or long or short: sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)); break;
                default: sb.Append('"').Append(Escape(value.ToString())).Append('"'); break;
            }
        }

        static string Escape(string s)
        {
            if (s.IndexOfAny(new[] { '"', '\\', '\n', '\r', '\t' }) < 0) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
