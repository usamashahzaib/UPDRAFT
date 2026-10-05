using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Core
{
    [Serializable]
    public class ChallengeRecord
    {
        public string challengeId;
        public bool isCompleted;
        public ChallengeMedal medal;
        public float personalBestTimeSeconds;
        public float personalBestScore;
        public int challengeVersion = 1;
        public string controlPreset = "Classic";
        public string difficulty = "Standard";
        public long lastUpdatedTicks;
    }

    [Serializable]
    public class LocalChallengeSaveSnapshot
    {
        public string challengeId;
        public int challengeVersion = 1;
        public string controlPreset = "Classic";
        public string difficulty = "Standard";
        public int stepIndex;
        public float elapsedTime;
        public float score;
        public int targetsCompleted;
        public int shotsFired;
        public int shotsHit;
        public string customStateJson;
        public long timestampTicks;
    }

    [Serializable]
    public class LocalChallengeSaveData
    {
        public int version = 2;
        public List<ChallengeRecord> records = new List<ChallengeRecord>();
        public LocalChallengeSaveSnapshot activeSnapshot;
        public string checksum;
    }

    /// <summary>Device-only progress. Checksum detects accidental corruption, not cheating.
    /// No backend, wallet, inventory or online-reward dependency.</summary>
    public sealed class LocalChallengeSave
    {
        public const int CurrentSaveVersion = 2;
        readonly string saveFilePath;
        LocalChallengeSaveData cacheData;
        bool newerVersion;
        public LocalChallengeSaveData Data => cacheData;
        public bool LastSaveSucceeded { get; private set; } = true;
        public string RecoveryMessage { get; private set; }

        public LocalChallengeSave(string customSaveFilePath = null)
        {
            saveFilePath = string.IsNullOrEmpty(customSaveFilePath)
                ? Path.Combine(Application.persistentDataPath, "offline_challenges_save.json") : customSaveFilePath;
            Load();
        }

        public bool Load()
        {
            cacheData = new LocalChallengeSaveData();
            newerVersion = false;
            RecoveryMessage = null;
            if (!File.Exists(saveFilePath)) return false;
            try
            {
                var data = ReadValid(saveFilePath);
                if (data != null) { cacheData = data; return true; }
                if (!newerVersion && File.Exists(saveFilePath + ".bak"))
                {
                    data = ReadValid(saveFilePath + ".bak");
                    if (data != null)
                    {
                        cacheData = data;
                        RecoveryMessage = "Recovered the previous local save. The latest attempt may need restarting.";
                        return true;
                    }
                }
                RecoveryMessage = newerVersion ? "This save needs a newer game version. Existing data has not been overwritten."
                    : "Local save could not be recovered. Start a new attempt; no medals were added.";
                Debug.LogWarning("[LocalChallengeSave] " + RecoveryMessage);
            }
            catch (Exception ex)
            {
                RecoveryMessage = "Local save is unavailable. No medals were added.";
                Debug.LogWarning("[LocalChallengeSave] " + ex.Message);
            }
            return false;
        }

        LocalChallengeSaveData ReadValid(string path)
        {
            try
            {
                var data = JsonUtility.FromJson<LocalChallengeSaveData>(File.ReadAllText(path));
                if (data == null) return null;
                if (data.version > CurrentSaveVersion) { newerVersion = true; return null; }
                if (data.version < 1 || data.records == null || data.records.Count > 1000) return null;
                // Unity serializes inline null classes as empty objects on some versions.
                if (data.activeSnapshot != null && string.IsNullOrEmpty(data.activeSnapshot.challengeId)) data.activeSnapshot = null;
                var expected = data.version == 1 ? LegacyChecksum(data) : ComputeChecksum(data);
                if (!string.Equals(data.checksum, expected, StringComparison.Ordinal)) return null;
                var keys = new HashSet<string>();
                foreach (var r in data.records)
                {
                    if (r == null || string.IsNullOrEmpty(r.challengeId) || r.challengeVersion < 1 ||
                        !FinitePositive(r.personalBestTimeSeconds) || !FinitePositive(r.personalBestScore) ||
                        r.medal < ChallengeMedal.None || r.medal > ChallengeMedal.Gold ||
                        (!r.isCompleted && r.medal != ChallengeMedal.None) || !keys.Add(RecordKey(r))) return null;
                }
                var s = data.activeSnapshot;
                if (s != null && (string.IsNullOrEmpty(s.challengeId) || s.challengeVersion < 1 ||
                    !FinitePositive(s.elapsedTime) || !FinitePositive(s.score) || s.targetsCompleted < 0 ||
                    s.stepIndex < 0 || s.shotsFired < 0 || s.shotsHit < 0 || s.shotsHit > s.shotsFired)) return null;
                if (data.version == 1)
                {
                    // Legacy checksum did not protect world/elapsed state. Keep validated
                    // records; never pretend its interrupted attempt can safely resume.
                    data.activeSnapshot = null;
                    RecoveryMessage = "Local records migrated. Restart any interrupted drill.";
                }
                data.version = CurrentSaveVersion;
                return data;
            }
            catch { return null; }
        }

        static bool FinitePositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        public void Save()
        {
            LastSaveSucceeded = false;
            if (newerVersion) return;
            try
            {
                cacheData.version = CurrentSaveVersion;
                cacheData.checksum = ComputeChecksum(cacheData);
                var dir = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var temp = saveFilePath + ".tmp";
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(cacheData, true));
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // Never delete the live file before replacement. A filesystem failure
                // preserves the last committed save and surfaces an honest warning.
                if (File.Exists(saveFilePath)) File.Replace(temp, saveFilePath, saveFilePath + ".bak");
                else File.Move(temp, saveFilePath);
                LastSaveSucceeded = true;
            }
            catch (Exception ex) { Debug.LogWarning("[LocalChallengeSave] Local progress was not saved: " + ex.Message); }
        }

        static string RecordKey(ChallengeRecord r) => r.challengeId + "|" + r.challengeVersion + "|" + r.controlPreset + "|" + r.difficulty;
        public ChallengeRecord GetRecord(string id) => cacheData.records.FindLast(r => r.challengeId == id);
        public ChallengeRecord GetRecord(string id, int version, string preset, string difficulty) =>
            cacheData.records.Find(r => r.challengeId == id && r.challengeVersion == version && r.controlPreset == preset && r.difficulty == difficulty);
        public void SaveRecord(ChallengeRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.challengeId)) return;
            record.lastUpdatedTicks = DateTime.UtcNow.Ticks;
            int index = cacheData.records.FindIndex(r => RecordKey(r) == RecordKey(record));
            if (index < 0) cacheData.records.Add(record); else cacheData.records[index] = record;
            Save();
        }
        public void SaveSnapshot(LocalChallengeSaveSnapshot snapshot)
        {
            cacheData.activeSnapshot = snapshot;
            if (snapshot != null) snapshot.timestampTicks = DateTime.UtcNow.Ticks;
            Save();
        }
        public LocalChallengeSaveSnapshot GetSnapshot(string id) => cacheData.activeSnapshot?.challengeId == id ? cacheData.activeSnapshot : null;
        public void ClearSnapshot() { cacheData.activeSnapshot = null; Save(); }
        public void ResetToDefaults() { cacheData = new LocalChallengeSaveData(); Save(); }

        public static string ComputeChecksum(LocalChallengeSaveData data)
        {
            if (data == null) return "";
            // Explicit canonical fields: no JSON null/inline-object ambiguity, no ambiguous
            // concatenation or rounded floats, and every record/snapshot field is protected.
            byte[] payload;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(data.version); writer.Write(data.records?.Count ?? 0);
                    if (data.records != null) foreach (var r in data.records)
                    {
                        if (r == null) { writer.Write(false); continue; }
                        writer.Write(true); writer.Write(r.challengeId ?? ""); writer.Write(r.isCompleted); writer.Write((int)r.medal);
                        writer.Write(r.personalBestTimeSeconds); writer.Write(r.personalBestScore); writer.Write(r.challengeVersion);
                        writer.Write(r.controlPreset ?? ""); writer.Write(r.difficulty ?? ""); writer.Write(r.lastUpdatedTicks);
                    }
                    var s = data.activeSnapshot;
                    bool present = !string.IsNullOrEmpty(s?.challengeId); writer.Write(present);
                    if (present)
                    {
                        writer.Write(s.challengeId); writer.Write(s.challengeVersion); writer.Write(s.controlPreset ?? "");
                        writer.Write(s.difficulty ?? ""); writer.Write(s.stepIndex); writer.Write(s.elapsedTime); writer.Write(s.score);
                        writer.Write(s.targetsCompleted); writer.Write(s.shotsFired); writer.Write(s.shotsHit);
                        writer.Write(s.customStateJson ?? ""); writer.Write(s.timestampTicks);
                    }
                }
                payload = stream.ToArray();
            }
            ulong hash = 14695981039346656037UL;
            foreach (byte b in payload) { hash ^= b; hash *= 1099511628211UL; }
            return hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }
        static string LegacyChecksum(LocalChallengeSaveData data)
        {
            var sb = new StringBuilder(); sb.Append(data.version);
            foreach (var r in data.records)
            {
                if (r == null) return "";
                sb.Append(r.challengeId ?? ""); sb.Append(r.isCompleted ? "1" : "0"); sb.Append((int)r.medal);
                sb.Append(r.personalBestTimeSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(r.personalBestScore.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)); sb.Append(r.challengeVersion);
            }
            if (!string.IsNullOrEmpty(data.activeSnapshot?.challengeId))
            { sb.Append(data.activeSnapshot.challengeId); sb.Append(data.activeSnapshot.stepIndex); }
            ulong hash = 14695981039346656037UL;
            foreach (char c in sb.ToString()) { hash ^= (byte)c; hash *= 1099511628211UL; }
            return hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
