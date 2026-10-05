using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.Pads;
using Hotdrop.Match;

namespace Hotdrop.UI
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

        /// <summary>
        /// Pings and the objective marker share one exclusion policy, owned by
        /// <see cref="HudMarkerLayer"/>. This method is kept as a thin alias so existing callers
        /// and tests read naturally, but there is no second copy of the numbers here.
        /// </summary>
        public static Vector2 ClampToReadableBand(Vector2 point, Rect canvasRect) =>
            HudMarkerLayer.ClampToReadableBand(point, canvasRect);

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

        /// <summary>
        /// Where each visible ping landed this frame, in canvas-local units. <see cref="MatchHUD"/>
        /// reads this to keep the objective caption off the ping captions; it is reset by every
        /// <see cref="RefreshMarkers"/> call, so a stale entry can never outlive a frame.
        /// </summary>
        readonly List<Vector2> placed = new List<Vector2>();
        public IReadOnlyList<Vector2> PlacedMarkers => placed;

        /// <summary>Panels on screen right now, measured by the HUD that owns them. See MatchHUD.ActivePanelRects.</summary>
        readonly List<Rect> panelExclusions = new List<Rect>();
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
                // Icon and caption share one row inside HudMarkerLayer.MarkerFootprintSize, for the same
                // reason as the objective marker: stacked, a ping is 60 units tall and the only free
                // gap inside the readable band on the preparation screen is 40.
                var root = Ui.Rect("Ping", transform, HudMarkerLayer.MarkerFootprintSize);
                // 22, not 26: rotated 45 degrees the drawn box is 22 x sqrt(2) = 31.1 tall.
                var icon = Ui.Rect("Icon", root, new Vector2(22, 22));
                icon.localRotation = Quaternion.Euler(0, 0, 45);
                icon.anchoredPosition = new Vector2(HudMarkerLayer.MarkerIconCenterX, 0);
                var image = icon.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                var label = Ui.Text("Label", root, "", 20, Color.white);
                label.rectTransform.anchoredPosition = new Vector2(HudMarkerLayer.MarkerCaptionCenterX, 0);
                label.rectTransform.sizeDelta = HudMarkerLayer.MarkerCaptionSize;
                // A world-projected label lands on arbitrary scene pixels: over a bright wall it
                // vanished, over the dark HUD it was fine. A translucent plate behind the text
                // makes the same caption legible against either, without hiding what it points at.
                var plate = Ui.Rect("Plate", root, HudMarkerLayer.MarkerCaptionSize);
                plate.anchoredPosition = new Vector2(HudMarkerLayer.MarkerCaptionCenterX, 0);
                var plateImage = plate.gameObject.AddComponent<UnityEngine.UI.Image>();
                plateImage.color = HudAppearance.MarkerPlateColor();
                plateImage.raycastTarget = false;
                plate.SetSiblingIndex(label.rectTransform.GetSiblingIndex());
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
            var local = Hotdrop.Match.MatchView.Current?.LocalParticipant;
            if (local != null && ping.teamId == local.TeamId && (!Muted || ping.senderId == local.ParticipantId))
                Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.Ping, 0.6f);
        }

        void LateUpdate() => RefreshMarkers();

        /// <summary>
        /// Re-places live ping markers against the canvas as it is right now. Public because a
        /// caller that changes canvas geometry mid-frame (the capture harness) must be able to
        /// ask for the same placement a device would perform on its next LateUpdate, rather
        /// than rendering markers clamped against a stale canvas rect.
        /// </summary>
        public void RefreshMarkers()
        {
            active.RemoveAll(p => Time.time >= p.expiresAt);
            placed.Clear();
            var hud = GetComponent<MatchHUD>();
            if (hud != null) hud.ActivePanelRects(panelExclusions); else panelExclusions.Clear();
            var local = Hotdrop.Match.MatchView.Current?.LocalParticipant;
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
                    // Observed in the 1920x1080 gameplay capture: a distant ping projected over
                    // the score bar and made both the ping and the zone status unreadable.
                    // Observed in the 1920x1080 capture: two pings whose world points project a
                    // few pixels apart print their captions through each other, and a ping printed
                    // across whatever HUD panel was up. Clamp into the readable band, hold the
                    // caption off the panels that are on screen, then off the captions already
                    // placed - last one wins, because a marker-marker smear is the worse of the two.
                    var slot = HudMarkerLayer.ClampToReadableBand(localPoint, canvasRect.rect);
                    slot = HudMarkerLayer.AvoidPanels(slot, panelExclusions, canvasRect.rect,
                        HudMarkerLayer.MarkerRowHeight, placed);
                    slot = HudMarkerLayer.SeparateFromPlaced(slot, placed, canvasRect.rect,
                        HudMarkerLayer.MarkerRowHeight, panelExclusions);
                    placed.Add(slot);
                    marker.root.localPosition = slot;
                    Color color = ping.kind == PingKind.EnemyHere ? MatchHUD.Enemy : ping.kind == PingKind.PadReady ? MatchHUD.Objective : MatchHUD.Friendly;
                    marker.icon.color = color;
                    marker.label.color = color;
                    var plate = marker.root.Find("Plate")?.GetComponent<UnityEngine.UI.Image>();
                    if (plate != null) plate.enabled = true;
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

namespace Hotdrop.UI
{
    /// <summary>Any pad (offline or networked) that a PAD READY ping can mark.</summary>
    public interface IPingablePad
    {
        bool Usable { get; }
        Vector3 PadPosition { get; }
    }
}



