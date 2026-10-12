using System.Collections.Generic;
using System.Collections.ObjectModel;
using Fusion;
using UnityEngine;

namespace Hotdrop.Network
{
    /// <summary>
    /// Server-owned position and sanitized-input samples for one live match.
    /// This is an in-memory capture source only; it is not replicated or uploaded.
    /// </summary>
    public sealed class ServerMatchReplay
    {
        public const float SampleIntervalSeconds = 0.1f;

        public struct Sample
        {
            public float matchTime;
            public int slot;
            public int team;
            public Vector3 position;
            public Vector3 velocity;
            public bool alive;
            public byte weaponIndex;
            public int shotCounter;
            public int hitConfirmCounter;
            public int headshotConfirmCounter;
            public Vector2 move;
            public float yaw;
            public float pitch;
            public byte buttons;
            public byte weaponChoice;
        }

        readonly List<Sample> samples;
        readonly ReadOnlyCollection<Sample> readOnlySamples;
        readonly int maxSamples;
        float sampleAccumulator;

        public IReadOnlyList<Sample> Samples => readOnlySamples;
        public int SampleCount => samples.Count;
        public int MaxSamples => maxSamples;

        public ServerMatchReplay(float maxMatchDurationSeconds, int participantCapacity)
        {
            float duration = float.IsNaN(maxMatchDurationSeconds) || float.IsInfinity(maxMatchDurationSeconds)
                ? 0f : Mathf.Max(0f, maxMatchDurationSeconds);
            int participants = Mathf.Clamp(participantCapacity, 1, 64);
            float frames = Mathf.Ceil(duration / SampleIntervalSeconds);
            maxSamples = frames >= int.MaxValue / participants
                ? int.MaxValue : Mathf.Max(1, Mathf.CeilToInt(frames) * participants);
            samples = new List<Sample>(Mathf.Min(maxSamples, 16384));
            readOnlySamples = samples.AsReadOnly();
        }

        /// <summary>Called only by the match state authority during live play.</summary>
        public void Capture(NetworkRunner runner, float deltaTime, float matchTime)
        {
            if (runner == null || !runner.IsServer || !IsFinite(deltaTime) || deltaTime <= 0f || !IsFinite(matchTime)) return;
            if (samples.Count >= maxSamples) return;

            sampleAccumulator += deltaTime;
            if (sampleAccumulator + 0.000001f < SampleIntervalSeconds) return;
            // Preserve the sampling phase after a slow/hitching server tick. We only have the
            // current authoritative snapshot, so emit one sample and discard missed intervals
            // instead of recording duplicate current-state samples on subsequent ticks.
            sampleAccumulator %= SampleIntervalSeconds;
            if (sampleAccumulator + 0.000001f >= SampleIntervalSeconds) sampleAccumulator = 0f;

            foreach (var player in NetPlayer.All)
            {
                if (player == null || player.Runner != runner) continue;
                var input = player.LastInput;
                byte buttons = 0;
                if (input.IsDown(NetButton.Fire)) buttons |= 1 << (int)NetButton.Fire;
                if (input.IsDown(NetButton.Jump)) buttons |= 1 << (int)NetButton.Jump;
                if (input.IsDown(NetButton.Pad)) buttons |= 1 << (int)NetButton.Pad;
                if (input.IsDown(NetButton.Reload)) buttons |= 1 << (int)NetButton.Reload;
                if (input.IsDown(NetButton.Ping)) buttons |= 1 << (int)NetButton.Ping;

                samples.Add(new Sample
                {
                    matchTime = matchTime,
                    slot = player.Slot,
                    team = player.Team,
                    position = player.transform.position,
                    velocity = player.Velocity,
                    alive = !player.Dead,
                    weaponIndex = (byte)Mathf.Clamp(player.WeaponIndex, 0, 255),
                    shotCounter = player.ShotCounter,
                    hitConfirmCounter = player.HitConfirmCounter,
                    headshotConfirmCounter = player.HeadshotConfirmCounter,
                    move = input.Move,
                    yaw = input.Yaw,
                    pitch = input.Pitch,
                    buttons = buttons,
                    weaponChoice = input.WeaponChoice,
                });
                if (samples.Count >= maxSamples) break;
            }
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
