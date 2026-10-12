using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Core
{
    // Order of the first entries is relied on (weapon index == cue index).
    public enum GameCue
    {
        Arc, Zip, Brick, Reload, Step, Land, Jump, Hurt, Hit, Elimination, PadDeploy, PadArm, PadLaunch, PadDestroy,
        Empty, Ping, ZoneActivate, ZoneContest, ZoneMilestone, Countdown, CountdownGo, Victory, Defeat, UiTap,
        LowHealth, EliminationConfirm, PadCharging, UiPositive, UiNegative, OwnDeath,
        PingEnemyHere, PingGroupUp, PingPadReady, PingGoHere,
    }

    public enum AudioCategory { Sfx, Voice, Music }

    /// <summary>
    /// Original synthetic prototype sounds, generated in code (no third-party
    /// audio, so nothing to license). Each cue has its own recipe so weapons,
    /// pads and objective events stay distinguishable by ear.
    /// </summary>
    public static class OriginalGameAudio
    {
        const int Rate = 22050;
        static readonly AudioClip[] clips = new AudioClip[System.Enum.GetValues(typeof(GameCue)).Length];
        static readonly AudioClip[] weaponBody = new AudioClip[3], weaponTransient = new AudioClip[3];
        static readonly AudioClip[] weaponMechanism = new AudioClip[3], weaponTail = new AudioClip[3], weaponDistantReport = new AudioClip[3];
        static AudioClip music, musicRhythm, musicLead, defeatResolution;
        static AudioClip flightWind, flightCloth, flightCityRush;

        public static void Warmup()
        {
            for (int i = 0; i < clips.Length; i++)
            {
                var cue = (GameCue)i;
                if (IsWeaponReport(cue))
                {
                    WeaponBody(cue);
                    WeaponTransient(cue);
                }
                else Get(cue);
            }
        }

        public static AudioClip Get(GameCue cue)
        {
            int index = (int)cue;
            if (clips[index] != null) return clips[index];
            var r = Recipe(cue);
            var samples = new float[Mathf.RoundToInt(Rate * r.length)];
            uint seed = 0x91E10DA5u + (uint)index;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float p = t / r.length;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float noise = (seed & 65535) / 32767.5f - 1f;
                float freq = Mathf.Lerp(r.startHz, r.endHz, p);
                // Optional second note (two-tone cues such as countdown GO / victory).
                if (r.stepAt > 0 && p >= r.stepAt) freq *= r.stepRatio;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t);
                float attack = Mathf.Min(1f, t * r.attack);
                float env = attack * Mathf.Pow(Mathf.Max(0f, 1f - p), r.decay);
                if (r.pulses > 0) env *= Mathf.Sin(Mathf.PI * Mathf.Repeat(p * r.pulses, 1f)) > 0.2f ? 1f : 0.15f;
                samples[i] = (noise * r.noise + tone * (1f - r.noise)) * env * r.gain;
            }
            clips[index] = AudioClip.Create("HOTDROP " + cue, samples.Length, 1, Rate, false);
            clips[index].SetData(samples, 0);
            return clips[index];
        }

        struct R { public float length, startHz, endHz, noise, decay, attack, gain, stepAt, stepRatio; public int pulses; }

        static R Recipe(GameCue cue) => cue switch
        {
            GameCue.Arc => new R { length = .13f, startHz = 190, endHz = 90, noise = .6f, decay = 4, attack = 900, gain = .5f },
            GameCue.Zip => new R { length = .07f, startHz = 260, endHz = 160, noise = .55f, decay = 5, attack = 1200, gain = .42f },
            GameCue.Brick => new R { length = .26f, startHz = 110, endHz = 45, noise = .75f, decay = 3, attack = 900, gain = .6f },
            GameCue.Reload => new R { length = .22f, startHz = 700, endHz = 500, noise = .35f, decay = 2, attack = 400, gain = .35f, pulses = 2 },
            GameCue.Empty => new R { length = .06f, startHz = 1400, endHz = 1300, noise = .2f, decay = 6, attack = 2000, gain = .3f },
            GameCue.Step => new R { length = .07f, startHz = 120, endHz = 80, noise = .8f, decay = 3, attack = 500, gain = .25f },
            GameCue.Land => new R { length = .16f, startHz = 90, endHz = 50, noise = .7f, decay = 2.5f, attack = 700, gain = .5f },
            GameCue.Jump => new R { length = .12f, startHz = 300, endHz = 520, noise = .3f, decay = 2, attack = 500, gain = .3f },
            GameCue.Hurt => new R { length = .18f, startHz = 240, endHz = 140, noise = .45f, decay = 2, attack = 900, gain = .5f },
            GameCue.LowHealth => new R { length = .5f, startHz = 70, endHz = 70, noise = 0f, decay = .5f, attack = 60, gain = .45f, pulses = 2 },
            GameCue.Hit => new R { length = .06f, startHz = 1600, endHz = 1500, noise = .1f, decay = 4, attack = 3000, gain = .35f },
            GameCue.EliminationConfirm => new R { length = .3f, startHz = 900, endHz = 900, noise = 0f, decay = 1.5f, attack = 1500, gain = .4f, stepAt = .4f, stepRatio = 1.5f },
            GameCue.Elimination => new R { length = .45f, startHz = 300, endHz = 120, noise = .3f, decay = 1.5f, attack = 600, gain = .45f },
            // Local player's own death: short, low, soft drone with no noise (deaths are frequent; never harsh).
            // Kept shorter than Elimination (.45 s) so it reads as a brief drone, not a lingering tone.
            GameCue.OwnDeath => new R { length = .38f, startHz = 110, endHz = 98, noise = 0f, decay = .8f, attack = 40, gain = .26f },
            GameCue.PadDeploy => new R { length = .18f, startHz = 420, endHz = 380, noise = .4f, decay = 2, attack = 600, gain = .4f },
            GameCue.PadCharging => new R { length = .75f, startHz = 300, endHz = 700, noise = .1f, decay = .3f, attack = 40, gain = .3f },
            GameCue.PadArm => new R { length = .2f, startHz = 880, endHz = 880, noise = 0f, decay = 1.5f, attack = 1200, gain = .35f, stepAt = .5f, stepRatio = 1.33f },
            GameCue.PadLaunch => new R { length = .38f, startHz = 180, endHz = 900, noise = .45f, decay = 1.2f, attack = 800, gain = .6f },
            GameCue.PadDestroy => new R { length = .35f, startHz = 500, endHz = 90, noise = .7f, decay = 1.5f, attack = 900, gain = .55f },
            GameCue.Ping => new R { length = .22f, startHz = 1200, endHz = 1200, noise = 0f, decay = 2, attack = 1500, gain = .3f, stepAt = .45f, stepRatio = 1.25f },
            // Radar-only pings use distinct, low-cost tonal patterns. The HUD also captions
            // each kind with its localized label, so the sound is a parallel cue, not a code
            // players must decode without text.
            GameCue.PingEnemyHere => new R { length = .28f, startHz = 920, endHz = 560, noise = 0f, decay = 1.8f, attack = 900, gain = .28f, stepAt = .42f, stepRatio = .78f },
            GameCue.PingGroupUp => new R { length = .3f, startHz = 540, endHz = 820, noise = 0f, decay = 1.8f, attack = 900, gain = .27f, stepAt = .58f, stepRatio = 1.2f, pulses = 2 },
            GameCue.PingPadReady => new R { length = .3f, startHz = 1040, endHz = 1040, noise = 0f, decay = 1.8f, attack = 1400, gain = .25f, pulses = 3 },
            GameCue.PingGoHere => new R { length = .24f, startHz = 620, endHz = 980, noise = 0f, decay = 1.6f, attack = 1100, gain = .26f, stepAt = .62f, stepRatio = 1.22f },
            GameCue.ZoneActivate => new R { length = .6f, startHz = 520, endHz = 520, noise = 0f, decay = 1, attack = 300, gain = .35f, stepAt = .5f, stepRatio = 1.5f },
            GameCue.ZoneContest => new R { length = .4f, startHz = 460, endHz = 460, noise = .05f, decay = .8f, attack = 300, gain = .3f, pulses = 3 },
            GameCue.ZoneMilestone => new R { length = .35f, startHz = 660, endHz = 660, noise = 0f, decay = 1.2f, attack = 500, gain = .3f, stepAt = .5f, stepRatio = 1.26f },
            GameCue.Countdown => new R { length = .15f, startHz = 740, endHz = 740, noise = 0f, decay = 2, attack = 1500, gain = .35f },
            GameCue.CountdownGo => new R { length = .4f, startHz = 988, endHz = 988, noise = 0f, decay = 1.2f, attack = 1500, gain = .4f },
            GameCue.Victory => new R { length = 1.1f, startHz = 523, endHz = 523, noise = 0f, decay = 1, attack = 200, gain = .35f, stepAt = .35f, stepRatio = 1.5f },
            GameCue.Defeat => new R { length = 1.1f, startHz = 392, endHz = 330, noise = 0f, decay = 1, attack = 200, gain = .3f, stepAt = .4f, stepRatio = .84f },
            GameCue.UiTap => new R { length = .05f, startHz = 1000, endHz = 900, noise = 0f, decay = 3, attack = 3000, gain = .2f },
            GameCue.UiPositive => new R { length = .08f, startHz = 620, endHz = 920, noise = 0f, decay = 2.2f, attack = 1200, gain = .12f },
            GameCue.UiNegative => new R { length = .07f, startHz = 220, endHz = 220, noise = 0f, decay = 2.2f, attack = 1200, gain = .14f },
            _ => new R { length = .12f, startHz = 400, endHz = 300, noise = .3f, decay = 2, attack = 800, gain = .35f },
        };

        /// <summary>Pulse stem: the only layer used in the hub and the sparse match opening.</summary>
        public static AudioClip Music()
        {
            if (music != null) return music;
            const float length = 8f;
            var samples = new float[Mathf.RoundToInt(Rate * length)];
            float[][] chords = { new[] { 220f, 261.6f, 329.6f }, new[] { 196f, 246.9f, 293.7f }, new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 246.9f, 311.1f } };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                var chord = chords[Mathf.Min(3, (int)(t / 2f))];
                float local = Mathf.Repeat(t, 2f);
                float env = Mathf.Min(1f, local * 3f) * Mathf.Min(1f, (2f - local) * 3f);
                float v = 0f;
                foreach (var f in chord) v += Mathf.Sin(2f * Mathf.PI * f * t);
                float beat = Mathf.Repeat(t, 0.5f);
                float kick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(74f, 42f, Mathf.Exp(-beat * 22f)) * beat) * Mathf.Exp(-beat * 13f);
                samples[i] = v / 3f * env * 0.08f + kick * 0.035f;
            }
            music = AudioClip.Create("HOTDROP adaptive pulse stem", samples.Length, 1, Rate, false);
            music.SetData(samples, 0);
            return music;
        }

        /// <summary>Soft rhythmic layer, aligned to the 8-second pulse loop at 120 BPM.</summary>
        public static AudioClip MusicRhythm()
        {
            if (musicRhythm != null) return musicRhythm;
            const float length = 8f;
            var samples = new float[Mathf.RoundToInt(Rate * length)];
            uint seed = 0x7135B9A1u;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                int beatIndex = Mathf.FloorToInt(t / 0.5f);
                float beat = Mathf.Repeat(t, 0.5f);
                float hat = (beatIndex % 2 == 1 ? 0.018f : 0.01f) * Mathf.Exp(-beat * 34f);
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float noise = (seed & 65535) / 32767.5f - 1f;
                float backbeat = beatIndex % 4 == 1 || beatIndex % 4 == 3 ? noise * 0.025f * Mathf.Exp(-beat * 20f) : 0f;
                float loopEnvelope = Mathf.Min(1f, Mathf.Min(t * 12f, (length - t) * 12f));
                samples[i] = (hat + backbeat) * loopEnvelope;
            }
            musicRhythm = AudioClip.Create("HOTDROP adaptive rhythm stem", samples.Length, 1, Rate, false);
            musicRhythm.SetData(samples, 0);
            return musicRhythm;
        }

        /// <summary>Simple four-bar lead phrase aligned to the pulse and rhythm stems.</summary>
        public static AudioClip MusicLead()
        {
            if (musicLead != null) return musicLead;
            const float length = 8f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 698.46f, 880f, 698.46f,
                523.25f, 659.25f, 783.99f, 987.77f, 880f, 783.99f, 659.25f, 523.25f };
            var samples = new float[Mathf.RoundToInt(Rate * length)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float local = Mathf.Repeat(t, 0.5f);
                float env = Mathf.Min(1f, local * 24f) * Mathf.Min(1f, (0.5f - local) * 5f);
                float note = notes[Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / 0.5f))];
                samples[i] = Mathf.Sin(2f * Mathf.PI * note * t) * env * 0.022f;
            }
            musicLead = AudioClip.Create("HOTDROP adaptive lead stem", samples.Length, 1, Rate, false);
            musicLead.SetData(samples, 0);
            return musicLead;
        }

        /// <summary>A soft dominant-to-tonic cadence used when the local player loses.</summary>
        public static AudioClip DefeatResolution()
        {
            if (defeatResolution != null) return defeatResolution;
            const float length = 4f;
            float[][] chords = { new[] { 392f, 493.88f, 587.33f, 698.46f }, new[] { 349.23f, 440f, 523.25f },
                new[] { 392f, 493.88f, 587.33f, 698.46f }, new[] { 261.63f, 329.63f, 392f } };
            var samples = new float[Mathf.RoundToInt(Rate * length)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                int chordIndex = Mathf.Min(chords.Length - 1, Mathf.FloorToInt(t));
                float local = Mathf.Repeat(t, 1f);
                float env = Mathf.Min(1f, local * 3f) * Mathf.Min(1f, (1f - local) * 2f);
                float sum = 0f;
                foreach (float note in chords[chordIndex]) sum += Mathf.Sin(2f * Mathf.PI * note * t);
                samples[i] = sum / chords[chordIndex].Length * env * 0.045f;
            }
            defeatResolution = AudioClip.Create("HOTDROP gentle defeat resolution", samples.Length, 1, Rate, false);
            defeatResolution.SetData(samples, 0);
            return defeatResolution;
        }

        public static AudioClip FlightWind() => flightWind != null ? flightWind : flightWind = MakeFlightLoop("HOTDROP Flight Wind", 0.24f, 0.08f);
        public static AudioClip FlightCloth() => flightCloth != null ? flightCloth : flightCloth = MakeFlightLoop("HOTDROP Flight Cloth", 0.12f, 0.025f);
        public static AudioClip FlightCityRush() => flightCityRush != null ? flightCityRush : flightCityRush = MakeFlightCityRush();

        static AudioClip MakeFlightLoop(string name, float low, float high)
        {
            const int frames = Rate * 2;
            var samples = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float phase = (float)i / frames * 2f * Mathf.PI;
                float swell = 0.65f + 0.35f * Mathf.Sin(phase);
                samples[i] = (Mathf.Sin(phase * 7f) * low + Mathf.Sin(phase * 31f) * high) * swell;
            }
            var clip = AudioClip.Create(name, frames, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip MakeFlightCityRush()
        {
            const float length = 2f;
            int frames = Mathf.RoundToInt(Rate * length);
            const int noisePoints = 128;
            var noiseCurve = new float[noisePoints];
            uint seed = 0x7F4A7C15u;
            for (int i = 0; i < noiseCurve.Length; i++)
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                noiseCurve[i] = (seed & 65535) / 32767.5f - 1f;
            }

            var samples = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / Rate;
                float position = (float)i / frames * noisePoints;
                int point = Mathf.FloorToInt(position);
                float blend = Mathf.SmoothStep(0f, 1f, position - point);
                float noise = Mathf.Lerp(noiseCurve[point], noiseCurve[(point + 1) % noisePoints], blend);
                float lowRumble = Mathf.Sin(2f * Mathf.PI * 43f * t) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * 73f * t) * 0.3f
                    + Mathf.Sin(2f * Mathf.PI * 109f * t) * 0.15f;
                samples[i] = (noise * 0.7f + lowRumble * 0.3f) * 0.025f;
            }

            var clip = AudioClip.Create("HOTDROP City Rush Below", frames, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static bool IsWeaponReport(GameCue cue) => cue is GameCue.Arc or GameCue.Zip or GameCue.Brick;

        public static AudioClip WeaponBody(GameCue cue) => WeaponReportLayer(cue, weaponBody, true);
        public static AudioClip WeaponTransient(GameCue cue) => WeaponReportLayer(cue, weaponTransient, false);
        public static AudioClip WeaponMechanism(GameCue cue) => WeaponLayer(cue, weaponMechanism, false);
        public static AudioClip WeaponTail(GameCue cue) => WeaponLayer(cue, weaponTail, true);
        public static AudioClip DistantWeaponReport(GameCue cue)
        {
            int index = (int)cue;
            if (index < 0 || index >= weaponDistantReport.Length) return null;
            if (weaponDistantReport[index] != null) return weaponDistantReport[index];
            const float length = 0.045f;
            int frames = Mathf.RoundToInt(Rate * length);
            var samples = new float[frames];
            float[] signatures = { 2100f, 2850f, 1650f };
            float frequency = signatures[index];
            uint seed = 0xC13FA9A9u ^ (uint)(index * 104729 + 31);
            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / Rate;
                float p = (float)i / frames;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float noise = (seed & 65535) / 32767.5f - 1f;
                float envelope = Mathf.Exp(-t * 95f);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                samples[i] = (noise * 0.72f + tone * 0.28f) * envelope * (0.11f + 0.06f * (1f - p));
            }
            weaponDistantReport[index] = AudioClip.Create($"HOTDROP Distant Report {cue}", frames, 1, Rate, false);
            weaponDistantReport[index].SetData(samples, 0);
            return weaponDistantReport[index];
        }

        static AudioClip WeaponLayer(GameCue cue, AudioClip[] cache, bool tail)
        {
            int index = (int)cue;
            if (index < 0 || index >= cache.Length) return null;
            if (cache[index] != null) return cache[index];
            int frames = Mathf.RoundToInt(Rate * (tail ? 0.42f : 0.055f));
            var samples = new float[frames];
            float[] fundamentals = { 720f, 1180f, 210f };
            float baseHz = fundamentals[index];
            uint seed = 0xA511E9B3u ^ (uint)(index * 7919 + (tail ? 97 : 13));
            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / Rate;
                float p = (float)i / frames;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float noise = (seed & 65535) / 32767.5f - 1f;
                float envelope = tail ? Mathf.Pow(1f - p, 2.8f) : Mathf.Exp(-t * 85f);
                float tone = Mathf.Sin(2f * Mathf.PI * baseHz * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * baseHz * 1.51f * t);
                samples[i] = tail ? (noise * 0.14f + tone * 0.86f) * envelope * 0.11f
                    : (noise * 0.35f + tone * 0.65f) * envelope * 0.16f;
            }
            cache[index] = AudioClip.Create($"HOTDROP {(tail ? "Environment Tail" : "Mechanism")} {cue}", frames, 1, Rate, false);
            cache[index].SetData(samples, 0);
            return cache[index];
        }

        static AudioClip WeaponReportLayer(GameCue cue, AudioClip[] cache, bool body)
        {
            int index = (int)cue;
            if (!IsWeaponReport(cue) || index < 0 || index >= cache.Length) return null;
            if (cache[index] != null) return cache[index];
            var recipe = Recipe(cue);
            var samples = new float[Mathf.RoundToInt(Rate * recipe.length)];
            uint seed = 0x91E10DA5u + (uint)index;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float p = t / recipe.length;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float noise = (seed & 65535) / 32767.5f - 1f;
                float frequency = Mathf.Lerp(recipe.startHz, recipe.endHz, p);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t)
                    + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                float attack = Mathf.Min(1f, t * recipe.attack);
                float envelope = attack * Mathf.Pow(Mathf.Max(0f, 1f - p), recipe.decay);
                samples[i] = (body ? tone * (1f - recipe.noise) : noise * recipe.noise)
                    * envelope * recipe.gain;
            }
            string layerName = body ? "Body" : "Transient";
            cache[index] = AudioClip.Create($"HOTDROP Weapon {layerName} {cue}", samples.Length, 1, Rate, false);
            cache[index].SetData(samples, 0);
            return cache[index];
        }
    }

    /// <summary>
    /// Spec section 16 playback: pooled 3D sources with stereo positioning and
    /// distance attenuation, simple obstruction (muffled through cover),
    /// gameplay priority (threats > launch/landing > objective > menu) and
    /// separate master / SFX / music / announcer volumes.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public const string MasterKey = "Volume.Master", SfxKey = "Volume.Sfx", MusicKey = "Volume.Music", VoiceKey = "Volume.Voice";
        [System.Obsolete("Use VoiceKey. Existing Announcer preference is migrated at startup.")]
        public const string AnnouncerKey = VoiceKey;
        public const string MonoOutputKey = "Audio.MonoOutput";
        public const string SoundCaptionsKey = "GameplaySoundCaptions";
        public static System.Action<string, int> OnCriticalCaption;
        const int PoolSize = 24;
        const float NearbyFootstepCaptionIntervalSeconds = 1.6f;

        static GameAudio instance;
        static MatchRulesConfig matchRules;
        static MatchRulesConfig fallbackAudioRules;
        static readonly float[] nextWeaponCaptionAt = new float[3];
        static float nextFootstepCaptionAt;
        readonly List<(AudioSource source, AudioLowPassFilter filter)> pool = new List<(AudioSource, AudioLowPassFilter)>();
        AudioSource musicSource, musicRhythmSource, musicLeadSource, musicResolveSource;
        float musicPulseTarget, musicRhythmTarget, musicLeadTarget, musicPitchTarget = 1f;
        bool musicLoopsStarted, musicKeepPlaying, matchMusicActive, matchResolutionPlayed;
        AudioSource flightWindSource, flightClothSource, flightCityRushSource;
        float flightWindTarget, flightClothTarget, flightCityRushTarget, flightDiveTarget;
        bool flightCaptionActive;
        AudioSpeakerMode startupSpeakerMode;
        bool monoWarningLogged;
        int obstructionMask;
        int next;

        public static GameAudio Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("GameAudio");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<GameAudio>();
                }
                return instance;
            }
        }

        static MatchRulesConfig MatchRules
        {
            get
            {
                if (matchRules == null)
                {
                    var config = Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
                    if (config == null)
                    {
                        Debug.LogWarning("MatchRulesConfig asset is missing; using the existing MatchRulesConfig type defaults for audio.");
                        config = CreateFallbackAudioRules();
                    }
                    else
                    {
                        try { config.ValidateSpatialWeaponAudio(); }
                        catch (System.InvalidOperationException exception)
                        {
                            Debug.LogError($"Invalid spatial weapon audio settings; using MatchRulesConfig type defaults for audio. {exception.Message}");
                            config = CreateFallbackAudioRules();
                        }
                    }
                    matchRules = config;
                }
                return matchRules;
            }
        }

        static MatchRulesConfig CreateFallbackAudioRules()
        {
            if (fallbackAudioRules == null)
            {
                fallbackAudioRules = ScriptableObject.CreateInstance<MatchRulesConfig>();
                fallbackAudioRules.hideFlags = HideFlags.HideAndDontSave;
                fallbackAudioRules.ValidateSpatialWeaponAudio();
            }
            return fallbackAudioRules;
        }

        /// <summary>Uses the same configured audio bands as the active match.</summary>
        public static void SetMatchRulesConfig(MatchRulesConfig config)
        {
            if (config == null) return;
            try { config.ValidateSpatialWeaponAudio(); }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError($"Invalid spatial weapon audio settings; using MatchRulesConfig type defaults for audio. {exception.Message}");
                config = CreateFallbackAudioRules();
            }
            matchRules = config;
            if (instance != null) instance.ApplySpatialAudioRange();
        }

        void Awake()
        {
            instance = this;
            _ = MatchRules;
            if (!PlayerPrefs.HasKey(VoiceKey) && PlayerPrefs.HasKey("Volume.Announcer"))
            {
                PlayerPrefs.SetFloat(VoiceKey, PlayerPrefs.GetFloat("Volume.Announcer", 1f));
                PlayerPrefs.Save();
            }
            startupSpeakerMode = AudioSettings.GetConfiguration().speakerMode;
            obstructionMask = LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = MatchRules.spatialAudioMinDistanceMeters;
                source.maxDistance = MatchRules.spatialAudioMaxDistanceMeters;
                source.dopplerLevel = 0f;
                var filter = go.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 22000f;
                pool.Add((source, filter));
            }
            musicSource = CreateMusicSource("Music_Pulse", OriginalGameAudio.Music(), true);
            musicRhythmSource = CreateMusicSource("Music_Rhythm", OriginalGameAudio.MusicRhythm(), true);
            musicLeadSource = CreateMusicSource("Music_Lead", OriginalGameAudio.MusicLead(), true);
            musicResolveSource = CreateMusicSource("Music_Resolve", null, false);
            flightWindSource = CreateFlightSource("FlightWind", OriginalGameAudio.FlightWind());
            flightClothSource = CreateFlightSource("FlightCloth", OriginalGameAudio.FlightCloth());
            flightCityRushSource = CreateFlightSource("FlightCityRush", OriginalGameAudio.FlightCityRush());
            ApplyVolumes();
            if (!ApplyMonoOutput() && PlayerPrefs.GetInt(MonoOutputKey, 0) == 1)
            {
                PlayerPrefs.SetInt(MonoOutputKey, 0);
                PlayerPrefs.Save();
            }
        }

        void ApplySpatialAudioRange()
        {
            float minDistance = MatchRules.spatialAudioMinDistanceMeters;
            float maxDistance = MatchRules.spatialAudioMaxDistanceMeters;
            foreach (var entry in pool)
            {
                if (entry.source == null) continue;
                entry.source.minDistance = minDistance;
                entry.source.maxDistance = maxDistance;
            }
        }

        public static float Volume(string key) => PlayerPrefs.GetFloat(key, key == MusicKey ? 0.5f : 1f);

        /// <summary>QA-022: SFX multiplier for sounds played outside the pool (e.g. PlayerController's own AudioSource).</summary>
        public static float SfxVolume => Volume(SfxKey);

        public static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Instance.ApplyVolumes();
        }

        AudioSource CreateFlightSource(string name, AudioClip clip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 180;
            source.volume = 0f;
            source.Play();
            return source;
        }

        AudioSource CreateMusicSource(string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 250;
            source.volume = 0f;
            return source;
        }

        void Update()
        {
            float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f);
            float volume = Volume(SfxKey);
            flightWindSource.volume = Mathf.Lerp(flightWindSource.volume, flightWindTarget * volume, blend);
            flightClothSource.volume = Mathf.Lerp(flightClothSource.volume, flightClothTarget * volume, blend);
            flightCityRushSource.volume = Mathf.Lerp(flightCityRushSource.volume, flightCityRushTarget * volume, blend);
            float divePitch = Mathf.Lerp(1f, 1.18f, flightDiveTarget);
            flightWindSource.pitch = Mathf.Lerp(flightWindSource.pitch, divePitch, blend);
            flightClothSource.pitch = Mathf.Lerp(flightClothSource.pitch, divePitch * 1.06f, blend);
            flightCityRushSource.pitch = Mathf.Lerp(flightCityRushSource.pitch, divePitch, blend);
            musicSource.volume = Mathf.Lerp(musicSource.volume, musicPulseTarget * Volume(MusicKey), blend);
            musicRhythmSource.volume = Mathf.Lerp(musicRhythmSource.volume, musicRhythmTarget * Volume(MusicKey), blend);
            musicLeadSource.volume = Mathf.Lerp(musicLeadSource.volume, musicLeadTarget * Volume(MusicKey), blend);
            musicSource.pitch = Mathf.Lerp(musicSource.pitch, musicPitchTarget, blend);
            musicRhythmSource.pitch = Mathf.Lerp(musicRhythmSource.pitch, musicPitchTarget, blend);
            musicLeadSource.pitch = Mathf.Lerp(musicLeadSource.pitch, musicPitchTarget, blend);
            if (!musicKeepPlaying && musicLoopsStarted && musicSource.volume < 0.001f && musicRhythmSource.volume < 0.001f && musicLeadSource.volume < 0.001f)
            {
                musicSource.Stop(); musicRhythmSource.Stop(); musicLeadSource.Stop();
                musicLoopsStarted = false;
            }
        }

        public static void SetFlightMix(float planarSpeed, float verticalSpeed, bool airborne)
        {
            var audio = Instance;
            if (airborne && !audio.flightCaptionActive && PlayerPrefs.GetInt(SoundCaptionsKey, 1) == 1)
                OnCriticalCaption?.Invoke(Caption("flightWind", "Flight Wind"), Priority(GameCue.Land));
            audio.flightCaptionActive = airborne;
            float speed = FlightSpeedStrength(planarSpeed);
            float dive = airborne ? Mathf.InverseLerp(1f, 12f, Mathf.Max(0f, -verticalSpeed)) : 0f;
            audio.flightWindTarget = airborne ? 0.12f + 0.34f * speed + 0.18f * dive : 0f;
            audio.flightClothTarget = airborne ? 0.035f + 0.09f * speed + 0.08f * dive : 0f;
            audio.flightCityRushTarget = airborne ? 0.015f + 0.025f * speed + 0.01f * dive : 0f;
            audio.flightDiveTarget = dive;
        }

        /// <summary>Shared planar-speed envelope for flight audio and the HUD's peripheral speed cue.</summary>
        public static float FlightSpeedStrength(float planarSpeed) =>
            Mathf.InverseLerp(4f, 18f, Mathf.Max(0f, planarSpeed));

        public static void ClearFlightMix()
        {
            // Teardown may run after the presenter/player is gone; clearing must not
            // lazily create a new persistent audio object just to silence its targets.
            if (instance == null) return;
            instance.flightWindTarget = 0f;
            instance.flightClothTarget = 0f;
            instance.flightCityRushTarget = 0f;
            instance.flightDiveTarget = 0f;
            instance.flightCaptionActive = false;
        }

        public static void SetMonoOutput(bool enabled)
        {
            PlayerPrefs.SetInt(MonoOutputKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!Instance.ApplyMonoOutput() && enabled)
            {
                PlayerPrefs.SetInt(MonoOutputKey, 0);
                PlayerPrefs.Save();
                if (!Instance.monoWarningLogged)
                {
                    Instance.monoWarningLogged = true;
                    Debug.LogWarning("This audio device could not switch to mono output.");
                }
            }
        }

        bool ApplyMonoOutput()
        {
            var configuration = AudioSettings.GetConfiguration();
            var target = PlayerPrefs.GetInt(MonoOutputKey, 0) == 1 ? AudioSpeakerMode.Mono : startupSpeakerMode;
            if (configuration.speakerMode == target) return true;
            configuration.speakerMode = target;
            return AudioSettings.Reset(configuration) && AudioSettings.GetConfiguration().speakerMode == target;
        }

        void ApplyVolumes()
        {
            AudioListener.volume = Volume(MasterKey);
            if (musicSource != null)
            {
                musicSource.volume = musicPulseTarget * Volume(MusicKey);
                musicRhythmSource.volume = musicRhythmTarget * Volume(MusicKey);
                musicLeadSource.volume = musicLeadTarget * Volume(MusicKey);
                musicResolveSource.volume = Volume(MusicKey);
            }
        }

        static void EnsureMusicLoops(GameAudio audio)
        {
            if (audio.musicLoopsStarted) return;
            double startAt = AudioSettings.dspTime + 0.05;
            audio.musicSource.PlayScheduled(startAt);
            audio.musicRhythmSource.PlayScheduled(startAt);
            audio.musicLeadSource.PlayScheduled(startAt);
            audio.musicLoopsStarted = true;
        }

        public static void SetHubMusic(bool playing)
        {
            var audio = Instance;
            audio.matchMusicActive = false;
            if (!playing) { StopMusic(); return; }
            audio.musicResolveSource.Stop();
            audio.musicKeepPlaying = true;
            audio.matchResolutionPlayed = false;
            EnsureMusicLoops(audio);
            audio.musicPulseTarget = 0.16f;
            audio.musicRhythmTarget = audio.musicLeadTarget = 0f;
            audio.musicPitchTarget = 1f;
        }

        public static void SetMatchMusic(float matchTime, bool overtime, Hotdrop.Config.MatchRulesConfig rules)
        {
            if (rules == null) { StopMusic(); return; }
            var audio = Instance;
            if (!audio.matchMusicActive)
            {
                audio.matchResolutionPlayed = false;
                audio.musicResolveSource.Stop();
            }
            audio.matchMusicActive = true;
            audio.musicKeepPlaying = true;
            EnsureMusicLoops(audio);
            audio.musicPulseTarget = 0.14f;
            audio.musicRhythmTarget = matchTime >= Mathf.Max(0f, rules.musicRhythmStartSeconds) ? 0.075f : 0f;
            audio.musicLeadTarget = matchTime >= Mathf.Max(0f, rules.musicLeadStartSeconds) ? 0.055f : 0f;
            bool endgame = overtime || matchTime >= Mathf.Max(0f, rules.regulationDurationSeconds - 60f);
            if (endgame)
            {
                audio.musicRhythmTarget = 0.095f;
                audio.musicLeadTarget = 0.075f;
            }
            float baseTempo = Mathf.Max(1f, rules.musicBaseTempoBpm);
            audio.musicPitchTarget = endgame ? (baseTempo + Mathf.Max(0f, rules.musicEndgameTempoBonusBpm)) / baseTempo : 1f;
        }

        public static void SetCountdownMusic(float elapsed, float duration, Hotdrop.Config.MatchRulesConfig rules)
        {
            if (rules == null) { StopMusic(); return; }
            var audio = Instance;
            if (audio.matchMusicActive || audio.matchResolutionPlayed)
            {
                audio.musicResolveSource.Stop();
                audio.matchResolutionPlayed = false;
            }
            audio.matchMusicActive = false;
            audio.musicPitchTarget = 1f;
            bool nearSilence = elapsed >= Mathf.Max(0f, duration - Mathf.Max(0f, rules.musicPreUnlockSilenceSeconds));
            audio.musicKeepPlaying = !nearSilence;
            EnsureMusicLoops(audio);
            audio.musicPulseTarget = nearSilence ? 0f : 0.12f;
            audio.musicRhythmTarget = audio.musicLeadTarget = 0f;
        }

        public static void ResolveMatchMusic(bool localPlayerLost)
        {
            var audio = Instance;
            audio.musicKeepPlaying = false;
            audio.musicPulseTarget = audio.musicRhythmTarget = audio.musicLeadTarget = 0f;
            audio.musicPitchTarget = 1f;
            audio.matchMusicActive = false;
            if (!localPlayerLost || audio.matchResolutionPlayed) return;
            audio.matchResolutionPlayed = true;
            audio.musicResolveSource.PlayOneShot(OriginalGameAudio.DefeatResolution());
        }

        public static void StopMusic()
        {
            var audio = Instance;
            audio.musicKeepPlaying = false;
            audio.musicPulseTarget = audio.musicRhythmTarget = audio.musicLeadTarget = 0f;
            audio.musicPitchTarget = 1f;
            audio.matchMusicActive = false;
        }

        // Compatibility entry point for callers that only know whether music is active.
        public static void SetMusic(bool playing)
        {
            if (!playing) { StopMusic(); return; }
            var audio = Instance;
            audio.musicKeepPlaying = true;
            audio.matchMusicActive = true;
            audio.matchResolutionPlayed = false;
            EnsureMusicLoops(audio);
            audio.musicPulseTarget = 0.14f;
            audio.musicRhythmTarget = 0.095f;
            audio.musicLeadTarget = 0.075f;
        }

        /// <summary>World-positioned cue.</summary>
        public static void Play(GameCue cue, Vector3 position, float volume = 1f, bool publishCaption = true) =>
            Instance.PlayInternal(cue, position, volume, true, publishCaption);

        /// <summary>Non-positional cue (UI, announcer, local feedback).</summary>
        public static void Play2D(GameCue cue, float volume = 1f) => Instance.PlayInternal(cue, Vector3.zero, volume, false, true);

        /// <summary>Publishes a caption for a world sound played by an external AudioSource.</summary>
        public static void ReportSpatialCue(GameCue cue, Vector3 position) => PublishCriticalCaption(cue, position, true);

        /// <summary>Captions audible remote footsteps without captioning the local avatar's own steps.</summary>
        public static void ReportNearbyFootsteps(Vector3 position)
        {
            if (PlayerPrefs.GetInt(SoundCaptionsKey, 1) != 1 || Time.unscaledTime < nextFootstepCaptionAt) return;
            var listenerCamera = UnityEngine.Camera.main;
            if (listenerCamera == null || Vector3.Distance(listenerCamera.transform.position, position) > MatchRules.spatialAudioMaxDistanceMeters) return;
            nextFootstepCaptionAt = Time.unscaledTime + NearbyFootstepCaptionIntervalSeconds;
            OnCriticalCaption?.Invoke(Caption("nearbyFootsteps", "Nearby Footsteps"), Priority(GameCue.Step));
        }

        /// <summary>Publishes a caption for a local sound played outside GameAudio's pooled sources.</summary>
        public static void ReportLocalCue(GameCue cue) => PublishCriticalCaption(cue, Vector3.zero, false);

        /// <summary>Publishes localized or data-driven text for a local critical sound cue.</summary>
        public static void ReportLocalCaption(GameCue cue, string caption)
        {
            if (PlayerPrefs.GetInt(SoundCaptionsKey, 1) != 1 || string.IsNullOrWhiteSpace(caption)) return;
            OnCriticalCaption?.Invoke(caption, Priority(cue));
        }

        void PlayInternal(GameCue cue, Vector3 position, float volume, bool spatial, bool publishCaption)
        {
            if (publishCaption) PublishCriticalCaption(cue, position, spatial);
            var category = Category(cue);
            int priority = Priority(cue);
            var (source, filter) = Acquire(priority);
            if (source == null) return;
            source.transform.position = position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.priority = priority;
            float categoryVolume = category == AudioCategory.Voice ? Volume(VoiceKey) : Volume(SfxKey);
            float obstruction = 1f;
            filter.cutoffFrequency = 22000f;
            var listener = UnityEngine.Camera.main;
            if (spatial && listener != null && obstructionMask != 0)
            {
                Vector3 from = listener.transform.position;
                if (Physics.Linecast(from, position + Vector3.up * 0.5f, obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    filter.cutoffFrequency = 1800f;
                    obstruction = 0.6f;
                }
            }
            float mixedVolume = volume * categoryVolume * obstruction;
            var rules = MatchRules;
            bool weaponReport = cue is GameCue.Arc or GameCue.Zip or GameCue.Brick;
            float distance = listener != null ? Vector3.Distance(listener.transform.position, position) : 0f;
            if (weaponReport && spatial && listener != null && distance >= rules.weaponDistantReportDistanceMeters)
            {
                // The far band is a short, cue-specific report so shots remain directional
                // without carrying the close body, mechanism and roof-tail layers.
                source.PlayOneShot(OriginalGameAudio.DistantWeaponReport(cue), mixedVolume);
            }
            else
            {
                if (weaponReport && spatial && listener != null && distance >= rules.weaponFullMixDistanceMeters)
                    filter.cutoffFrequency = Mathf.Min(filter.cutoffFrequency, rules.weaponMidDistanceLowPassHz);
                source.PlayOneShot(OriginalGameAudio.WeaponBody(cue), mixedVolume);
                if (!weaponReport || !spatial || listener == null || distance < rules.weaponFullMixDistanceMeters)
                    source.PlayOneShot(OriginalGameAudio.WeaponTransient(cue), mixedVolume);
                if (weaponReport && (!spatial || listener == null || distance < rules.weaponFullMixDistanceMeters))
                {
                    source.PlayOneShot(OriginalGameAudio.WeaponMechanism(cue), mixedVolume);
                    source.PlayOneShot(OriginalGameAudio.WeaponTail(cue), mixedVolume);
                }
            }
        }

        static void PublishCriticalCaption(GameCue cue, Vector3 position, bool spatial)
        {
            if (PlayerPrefs.GetInt(SoundCaptionsKey, 1) != 1) return;
            if (!spatial)
            {
                string localCaption = cue switch
                {
                    GameCue.ZoneActivate => Caption("zoneActivate", "Objective Zone Active"),
                    GameCue.ZoneContest => Caption("zoneContest", "Objective Zone Contested"),
                    GameCue.ZoneMilestone => Caption("zoneMilestone", "Objective Score Milestone"),
                    GameCue.Hurt => Caption("hurt", "Damage Taken"),
                    GameCue.LowHealth => Caption("lowHealth", "Low Health"),
                    GameCue.Hit => Caption("hit", "Hit Confirmed"),
                    GameCue.EliminationConfirm => Caption("eliminationConfirm", "Elimination Confirmed"),
                    GameCue.OwnDeath => Caption("ownDeath", "Eliminated"),
                    _ => null,
                };
                if (localCaption != null) OnCriticalCaption?.Invoke(localCaption, Priority(cue));
                return;
            }

            string caption = cue switch
            {
                GameCue.PadLaunch => Caption("nearbyPadLaunch", "Nearby Pad Launch"),
                GameCue.PadArm => Caption("nearbyPadArm", "Nearby Pad Armed"),
                GameCue.PadCharging => Caption("nearbyPadCharging", "Nearby Pad Charging"),
                GameCue.PadDeploy => Caption("nearbyPadDeploy", "Nearby Pad Deployed"),
                GameCue.PadDestroy => Caption("nearbyPadDestroy", "Nearby Pad Destroyed"),
                _ => null,
            };
            var listenerCamera = UnityEngine.Camera.main;
            if (listenerCamera == null) return;
            float distance = Vector3.Distance(listenerCamera.transform.position, position);
            if (cue is GameCue.Arc or GameCue.Zip or GameCue.Brick)
            {
                // Local fire is suppressed at the NetPlayer call site. Remote fire is
                // captioned at every audible distance with clear LOS and the same
                // per-weapon throttle, so close shots are not silently omitted.
                int weaponIndex = (int)cue;
                if (distance > MatchRules.spatialAudioMaxDistanceMeters || Time.unscaledTime < nextWeaponCaptionAt[weaponIndex]) return;
                int mask = instance != null ? instance.obstructionMask : 0;
                if (mask != 0 && Physics.Linecast(listenerCamera.transform.position,
                    position + Vector3.up * 0.5f, mask, QueryTriggerInteraction.Ignore)) return;
                nextWeaponCaptionAt[weaponIndex] = Time.unscaledTime + 3f;
                caption = WeaponFireCaption(cue, distance < MatchRules.nearbyWeaponCaptionMeters);
                OnCriticalCaption?.Invoke(caption, Priority(cue));
                return;
            }
            if (caption != null && distance <= MatchRules.spatialAudioMaxDistanceMeters) OnCriticalCaption?.Invoke(caption, Priority(cue));
        }

        static string WeaponFireCaption(GameCue cue, bool nearby) => cue switch
        {
            GameCue.Arc => nearby ? Caption("nearbyArcFire", "Nearby Arc Fire") : Caption("distantArcFire", "Distant Arc Fire"),
            GameCue.Zip => nearby ? Caption("nearbyZipFire", "Nearby Zip Fire") : Caption("distantZipFire", "Distant Zip Fire"),
            GameCue.Brick => nearby ? Caption("nearbyBrickFire", "Nearby Brick Fire") : Caption("distantBrickFire", "Distant Brick Fire"),
            _ => null,
        };

        static string Caption(string key, string fallback) => Hotdrop.UI.UiDraw.L("hud.sound." + key, fallback);

        (AudioSource, AudioLowPassFilter) Acquire(int priority)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var candidate = pool[(next + i) % pool.Count];
                if (!candidate.source.isPlaying) { next = (next + i + 1) % pool.Count; return candidate; }
            }
            // All busy: steal the least important voice, but never for a less important cue.
            int worst = -1, worstPriority = -1;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].source.priority > worstPriority) { worstPriority = pool[i].source.priority; worst = i; }
            if (worst < 0 || worstPriority < priority) return (null, null);
            pool[worst].source.Stop();
            return pool[worst];
        }

        // Lower number = more important (Unity AudioSource priority convention).
        public static int Priority(GameCue cue) => cue switch
        {
            GameCue.Arc or GameCue.Zip or GameCue.Brick or GameCue.Hurt or GameCue.LowHealth or GameCue.Step or GameCue.Hit
                or GameCue.EliminationConfirm => 20,
            GameCue.PadLaunch or GameCue.Land or GameCue.PadDestroy or GameCue.PadCharging or GameCue.PadArm or GameCue.PadDeploy => 40,
            GameCue.ZoneActivate or GameCue.ZoneContest or GameCue.ZoneMilestone or GameCue.Countdown or GameCue.CountdownGo
                or GameCue.Victory or GameCue.Defeat
                or GameCue.Ping or GameCue.PingEnemyHere or GameCue.PingGroupUp or GameCue.PingPadReady or GameCue.PingGoHere
                or GameCue.Elimination or GameCue.OwnDeath => 80,
            _ => 160,
        };

        static AudioCategory Category(GameCue cue) => cue switch
        {
            GameCue.ZoneActivate or GameCue.ZoneContest or GameCue.ZoneMilestone or GameCue.Countdown or GameCue.CountdownGo
                or GameCue.Victory or GameCue.Defeat => AudioCategory.Voice,
            _ => AudioCategory.Sfx,
        };
    }
}

