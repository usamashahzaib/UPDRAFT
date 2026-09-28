using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Updraft.Pads;
using Updraft.Match;

namespace Updraft.UI
{
    public enum PingKind { GoHere, EnemyHere, GroupUp, PadReady }

    /// <summary>
    /// Spec section 14 contextual pings. The kind comes from what the crosshair is
    /// on: a visible enemy marks that location (never the player), a usable pad
    /// marks "pad ready", open sky means "group up" on the pinger, anything else
    /// is "go here". 3 s expiry, one ping per 2 s per player, others' pings mutable.
    /// </summary>
    public class PingSystem : MonoBehaviour
    {
        public const float LifetimeSeconds = 3f;
        public const float CooldownSeconds = 2f;
        public const string MuteKey = "PingsMuted";
        const int MaxVisible = 8;

        public struct Ping
        {
            public PingKind kind;
            public Vector3 position;
            public int teamId;
            public int senderId;
            public string senderName;
            public float expiresAt;
        }

        public static PingSystem Instance { get; private set; }
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set { PlayerPrefs.SetInt(MuteKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        readonly List<Ping> active = new List<Ping>();
        readonly Dictionary<int, float> lastPingAt = new Dictionary<int, float>();
        readonly List<(RectTransform root, Image icon, TextMeshProUGUI label)> markers = new List<(RectTransform, Image, TextMeshProUGUI)>();
        readonly RaycastHit[] hits = new RaycastHit[16];
        RectTransform canvasRect;

        public IReadOnlyList<Ping> ActivePings => active;
        public System.Action<Ping> OnPingSent;

        void Awake()
        {
            Instance = this;
            canvasRect = GetComponent<RectTransform>();
            for (int i = 0; i < MaxVisible; i++)
            {
                var root = Ui.Rect("Ping", transform, new Vector2(200, 80));
                var icon = Ui.Rect("Icon", root, new Vector2(26, 26));
                icon.localRotation = Quaternion.Euler(0, 0, 45);
                icon.anchoredPosition = new Vector2(0, 16);
                var image = icon.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                var label = Ui.Text("Label", root, "", 20, Color.white);
                label.rectTransform.anchoredPosition = new Vector2(0, -16);
                label.rectTransform.sizeDelta = new Vector2(200, 28);
                root.gameObject.SetActive(false);
                markers.Add((root, image, label));
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Clear() { active.Clear(); lastPingAt.Clear(); }

        /// <summary>Resolve a ping kind from the sender's view ray and send it. Returns false when rate-limited.</summary>
        public bool RequestPing(IMatchParticipant sender, Ray view, float maxDistance = 120f)
        {
            if (sender == null || !sender.IsAlive || !CanPing(sender.ParticipantId)) return false;
            Classify(sender, view, Physics.defaultPhysicsScene, hits, out var kind, out var position, maxDistance);
            Send(new Ping { kind = kind, position = position, teamId = sender.TeamId, senderId = sender.ParticipantId, senderName = sender.DisplayName });
            return true;
        }

        public bool CanPing(int senderId) =>
            !lastPingAt.TryGetValue(senderId, out float last) || Time.time - last >= CooldownSeconds;

        /// <summary>
        /// Contextual kind from the first thing the view ray hits. Shared by offline
        /// play and the network server (which re-runs it to validate a client's ping).
        /// </summary>
        public static void Classify(IMatchParticipant sender, Ray view, PhysicsScene physics, RaycastHit[] buffer,
            out PingKind kind, out Vector3 position, float maxDistance = 120f)
        {
            kind = PingKind.GroupUp;
            position = sender.Position;
            int count = physics.Raycast(view.origin, view.direction, buffer, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = buffer[i];
                if (hit.distance >= nearest) continue;
                var player = hit.collider.GetComponentInParent<IMatchParticipant>();
                if (ReferenceEquals(player, sender) || (player != null && player.TeamId == sender.TeamId)) continue;
                nearest = hit.distance;
                position = hit.point;
                if (player != null && player.IsAlive) kind = PingKind.EnemyHere;       // visible: first thing the ray hit
                else if (hit.collider.GetComponentInParent<IPingablePad>() is IPingablePad pad && pad.Usable)
                { kind = PingKind.PadReady; position = pad.PadPosition; }
                else kind = PingKind.GoHere;
            }
        }

        public void Send(Ping ping)
        {
            ping.expiresAt = Time.time + LifetimeSeconds;
            lastPingAt[ping.senderId] = Time.time;
            // One live ping per sender: a new one replaces the old.
            active.RemoveAll(p => p.senderId == ping.senderId);
            active.Add(ping);
            OnPingSent?.Invoke(ping);
            var local = Updraft.Match.MatchView.Current?.LocalParticipant;
            if (local != null && ping.teamId == local.TeamId && (!Muted || ping.senderId == local.ParticipantId))
                Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.Ping, 0.6f);
        }

        void LateUpdate()
        {
            active.RemoveAll(p => Time.time >= p.expiresAt);
            var local = Updraft.Match.MatchView.Current?.LocalParticipant;
            var cam = UnityEngine.Camera.main;
            int shown = 0;
            if (local != null && cam != null)
            {
                bool muted = Muted;
                foreach (var ping in active)
                {
                    if (shown >= markers.Count) break;
                    if (ping.teamId != local.TeamId) continue;                    // team-only communication
                    if (muted && ping.senderId != local.ParticipantId) continue;
                    Vector3 screen = cam.WorldToScreenPoint(ping.position + Vector3.up * 0.5f);
                    if (screen.z < 0f) continue;
                    var marker = markers[shown++];
                    marker.root.gameObject.SetActive(true);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var localPoint);
                    marker.root.localPosition = localPoint;
                    Color color = ping.kind == PingKind.EnemyHere ? MatchHUD.Enemy : ping.kind == PingKind.PadReady ? MatchHUD.Objective : MatchHUD.Friendly;
                    marker.icon.color = color;
                    marker.label.color = color;
                    float distance = Vector3.Distance(local.Position, ping.position);
                    marker.label.text = $"{Label(ping.kind)} {distance:0}m";
                }
            }
            for (int i = shown; i < markers.Count; i++)
                if (markers[i].root.gameObject.activeSelf) markers[i].root.gameObject.SetActive(false);
        }

        public static string Label(PingKind kind) => kind switch
        {
            PingKind.EnemyHere => "ENEMY HERE",
            PingKind.GroupUp => "GROUP UP",
            PingKind.PadReady => "PAD READY",
            _ => "GO HERE",
        };

        /// <summary>Most recent live "go here"/"group up" ping from a teammate, for bot teammates to follow.</summary>
        public bool TryGetTeamRally(int teamId, int excludeSender, out Vector3 position)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p.teamId == teamId && p.senderId != excludeSender && (p.kind == PingKind.GoHere || p.kind == PingKind.GroupUp))
                { position = p.position; return true; }
            }
            position = default;
            return false;
        }
    }
}

namespace Updraft.UI
{
    /// <summary>Any pad (offline or networked) that a PAD READY ping can mark.</summary>
    public interface IPingablePad
    {
        bool Usable { get; }
        Vector3 PadPosition { get; }
    }
}
