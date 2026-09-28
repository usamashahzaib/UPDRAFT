using System.Collections.Generic;
using UnityEngine;

namespace Updraft.Core
{
    // Order of the first entries is relied on (weapon index == cue index).
    public enum GameCue
    {
        Arc, Zip, Brick, Reload, Step, Land, Jump, Hurt, Hit, Elimination, PadDeploy, PadArm, PadLaunch, PadDestroy,
        Empty, Ping, ZoneActivate, ZoneContest, ZoneMilestone, Countdown, CountdownGo, Victory, Defeat, UiTap,
        LowHealth, EliminationConfirm, PadCharging,
    }

    public enum AudioCategory { Sfx, Announcer, Music }

    /// <summary>
    /// Original synthetic prototype sounds, generated in code (no third-party
    /// audio, so nothing to license). Each cue has its own recipe so weapons,
    /// pads and objective events stay distinguishable by ear.
    /// </summary>
    public static class OriginalGameAudio
    {
        const int Rate = 22050;
        static readonly AudioClip[] clips = new AudioClip[System.Enum.GetValues(typeof(GameCue)).Length];
        static AudioClip music;

        public static void Warmup() { for (int i = 0; i < clips.Length; i++) Get((GameCue)i); }

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
            clips[index] = AudioClip.Create("UPDRAFT " + cue, samples.Length, 1, Rate, false);
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
            GameCue.PadDeploy => new R { length = .18f, startHz = 420, endHz = 380, noise = .4f, decay = 2, attack = 600, gain = .4f },
            GameCue.PadCharging => new R { length = .75f, startHz = 300, endHz = 700, noise = .1f, decay = .3f, attack = 40, gain = .3f },
            GameCue.PadArm => new R { length = .2f, startHz = 880, endHz = 880, noise = 0f, decay = 1.5f, attack = 1200, gain = .35f, stepAt = .5f, stepRatio = 1.33f },
            GameCue.PadLaunch => new R { length = .38f, startHz = 180, endHz = 900, noise = .45f, decay = 1.2f, attack = 800, gain = .6f },
            GameCue.PadDestroy => new R { length = .35f, startHz = 500, endHz = 90, noise = .7f, decay = 1.5f, attack = 900, gain = .55f },
            GameCue.Ping => new R { length = .22f, startHz = 1200, endHz = 1200, noise = 0f, decay = 2, attack = 1500, gain = .3f, stepAt = .45f, stepRatio = 1.25f },
            GameCue.ZoneActivate => new R { length = .6f, startHz = 520, endHz = 520, noise = 0f, decay = 1, attack = 300, gain = .35f, stepAt = .5f, stepRatio = 1.5f },
            GameCue.ZoneContest => new R { length = .4f, startHz = 460, endHz = 460, noise = .05f, decay = .8f, attack = 300, gain = .3f, pulses = 3 },
            GameCue.ZoneMilestone => new R { length = .35f, startHz = 660, endHz = 660, noise = 0f, decay = 1.2f, attack = 500, gain = .3f, stepAt = .5f, stepRatio = 1.26f },
            GameCue.Countdown => new R { length = .15f, startHz = 740, endHz = 740, noise = 0f, decay = 2, attack = 1500, gain = .35f },
            GameCue.CountdownGo => new R { length = .4f, startHz = 988, endHz = 988, noise = 0f, decay = 1.2f, attack = 1500, gain = .4f },
            GameCue.Victory => new R { length = 1.1f, startHz = 523, endHz = 523, noise = 0f, decay = 1, attack = 200, gain = .35f, stepAt = .35f, stepRatio = 1.5f },
            GameCue.Defeat => new R { length = 1.1f, startHz = 392, endHz = 330, noise = 0f, decay = 1, attack = 200, gain = .3f, stepAt = .4f, stepRatio = .84f },
            GameCue.UiTap => new R { length = .05f, startHz = 1000, endHz = 900, noise = 0f, decay = 3, attack = 3000, gain = .2f },
            _ => new R { length = .12f, startHz = 400, endHz = 300, noise = .3f, decay = 2, attack = 800, gain = .35f },
        };

        /// <summary>A quiet 8-second chord loop for combat music (spec: quiet, never masking footsteps).</summary>
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
                samples[i] = v / 3f * env * 0.12f;
            }
            music = AudioClip.Create("UPDRAFT combat loop", samples.Length, 1, Rate, false);
            music.SetData(samples, 0);
            return music;
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
        public const string MasterKey = "Volume.Master", SfxKey = "Volume.Sfx", MusicKey = "Volume.Music", AnnouncerKey = "Volume.Announcer";
        const int PoolSize = 24;

        static GameAudio instance;
        readonly List<(AudioSource source, AudioLowPassFilter filter)> pool = new List<(AudioSource, AudioLowPassFilter)>();
        AudioSource musicSource;
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

        void Awake()
        {
            instance = this;
            obstructionMask = LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3f;
                source.maxDistance = 50f;
                source.dopplerLevel = 0f;
                var filter = go.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 22000f;
                pool.Add((source, filter));
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 250;
            ApplyVolumes();
        }

        public static float Volume(string key) => PlayerPrefs.GetFloat(key, key == MusicKey ? 0.5f : 1f);

        public static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Instance.ApplyVolumes();
        }

        void ApplyVolumes()
        {
            AudioListener.volume = Volume(MasterKey);
            if (musicSource != null) musicSource.volume = 0.35f * Volume(MusicKey);
        }

        public static void SetMusic(bool playing)
        {
            var m = Instance.musicSource;
            if (playing && !m.isPlaying) { m.clip = OriginalGameAudio.Music(); m.Play(); }
            else if (!playing && m.isPlaying) m.Stop();
        }

        /// <summary>World-positioned cue.</summary>
        public static void Play(GameCue cue, Vector3 position, float volume = 1f) => Instance.PlayInternal(cue, position, volume, true);

        /// <summary>Non-positional cue (UI, announcer, local feedback).</summary>
        public static void Play2D(GameCue cue, float volume = 1f) => Instance.PlayInternal(cue, Vector3.zero, volume, false);

        void PlayInternal(GameCue cue, Vector3 position, float volume, bool spatial)
        {
            var category = Category(cue);
            int priority = Priority(cue);
            var (source, filter) = Acquire(priority);
            if (source == null) return;
            source.transform.position = position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.priority = priority;
            float categoryVolume = category == AudioCategory.Announcer ? Volume(AnnouncerKey) : Volume(SfxKey);
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
            source.PlayOneShot(OriginalGameAudio.Get(cue), volume * categoryVolume * obstruction);
        }

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
            GameCue.Arc or GameCue.Zip or GameCue.Brick or GameCue.Hurt or GameCue.LowHealth or GameCue.Step or GameCue.Hit => 20,
            GameCue.PadLaunch or GameCue.Land or GameCue.PadDestroy or GameCue.PadCharging or GameCue.PadArm or GameCue.PadDeploy => 40,
            GameCue.ZoneActivate or GameCue.ZoneContest or GameCue.ZoneMilestone or GameCue.Countdown or GameCue.CountdownGo
                or GameCue.Ping or GameCue.EliminationConfirm or GameCue.Elimination => 80,
            _ => 160,
        };

        static AudioCategory Category(GameCue cue) => cue switch
        {
            GameCue.ZoneActivate or GameCue.ZoneContest or GameCue.ZoneMilestone or GameCue.Countdown or GameCue.CountdownGo
                or GameCue.Victory or GameCue.Defeat => AudioCategory.Announcer,
            _ => AudioCategory.Sfx,
        };
    }
}
