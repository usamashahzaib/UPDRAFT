using System.Collections.Generic;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// The in-match HUD of UIUX screen 08 (classic preset): roster cards, broadcast scoreboard with
    /// the zone tug bar, region chip and radar, and the segmented armor pill. Numbers are the
    /// reference's CSS px. It draws only what the match reports - nothing here is a placeholder:
    /// scores, clock and zone arrive from <see cref="MatchHUD"/>, the rest is read from the live
    /// <see cref="MatchView"/> each frame. Touch controls stay with <see cref="TouchControlsView"/>.
    /// </summary>
    public sealed class UiuxHud : MonoBehaviour
    {
        static string L(string key, string fallback) => UiDraw.L("hud." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("hud." + key, fallback, args);

        // Cockpit/roster fade tuning lives in MatchRulesConfig; these HUD spec estimates apply only
        // when neither the match nor Resources provides that config.
        const float DefaultCombatRangeMeters = 30f, DefaultFadedOpacity = 0.15f, DefaultFadeSeconds = 0.2f;
        const float DefaultRosterIdleFadeSeconds = 5f;
        static MatchRulesConfig fallbackRules;

        // Layout is in dp: every panel below sits in a UiDpPanel scaled so one local unit is one dp.
        // A landscape phone is only ~770-960 x 432 dp, so the HUD carries few words and keeps the
        // centre of the screen open. The 24 dp edge matches the touch controls' own margin.
        const float Margin = 24f;
        const float RosterWidth = 176f, CardHeight = 42f, CardGap = 4f, RosterHeader = 18f, CardBar = 160f;
        const float BoardWidth = 176f, BoardHeight = 30f, ZoneHeight = 24f, ZoneMinWidth = 176f, TugHeight = 3f;
        // The top-centre cluster ends at TopMargin + 30 + 3 + 24 = 71 dp, which keeps it above the
        // combat-feedback strip (MatchHUD.CombatFeedbackBandY) on phones up to 2x density.
        const float TopMargin = 14f, ZoneGap = 3f;
        const float RadarSize = 112f, RadarTop = 46f, RadarRing = 46f, StatusLine = 10f, CompassLine = 18f;
        const float ArmorWidth = 232f, ArmorHeight = 42f, ArmorBar = 152f, ArmorBottom = 14f;
        const float HudCaptionDp = UIDesignTokens.CaptionDp;
        const float HudValueDp = UIDesignTokens.BodyLargeDp;
        const float HudEmphasisDp = UIDesignTokens.TitleMediumDp;
        // These were literal screen-08 reference hex values, frozen regardless of the active
        // A/B/C presentation profile. Routed through UIDesignTokens so this HUD recolors live
        // with everything else; the light/base pairing (Primary/PrimaryContainer etc.) mirrors
        // how the original hexes related to each other (e.g. #7cefff was a lightened #30d5e8).
        static Color Primary => Color.Lerp(UIDesignTokens.PrimaryAction, UIDesignTokens.TextPrimary, .35f);
        static Color PrimaryContainer => UIDesignTokens.PrimaryAction;
        static Color Secondary => Color.Lerp(UIDesignTokens.HazardCoral, UIDesignTokens.TextPrimary, .35f);
        static Color Tertiary => Color.Lerp(UIDesignTokens.ObjectiveGold, UIDesignTokens.TextPrimary, .35f);
        static Color TertiaryDim => UIDesignTokens.ObjectiveGold;
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnSurfaceVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.SurfaceStructural;
        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Highest => Color.Lerp(UIDesignTokens.SurfaceSelected, UIDesignTokens.TextPrimary, .06f);
        static Color Coral => UIDesignTokens.HazardCoral;

        RectTransform root, roster, board, zonePanel, topRight, radar, armor;
        RectTransform soundCaptionRoot;
        TextMeshProUGUI soundCaptionText;
        float soundCaptionRemaining;
        int soundCaptionPriority = int.MaxValue;
        CanvasGroup rosterFade, radarFade;
        TextMeshProUGUI rosterCount, allyScore, enemyScore, clock, zoneName, zoneState, zoneNext, regionText, armorValue, armorZone, armorPad;
        RectTransform tugAlly, tugEnemy, tugMark;
        readonly List<Image> armorSegments = new List<Image>();
        readonly List<RosterCard> cards = new List<RosterCard>();
        RectTransform radarSelf, radarObjective, radarField, radarMap;
        RectTransform compassStrip;
        CanvasGroup compassFade;
        readonly TextMeshProUGUI[] compassLabels = new TextMeshProUGUI[4];
        readonly List<RadarMateMarker> radarMates = new List<RadarMateMarker>();
        readonly List<RadarPingMarker> radarPings = new List<RadarPingMarker>();
        TextMeshProUGUI radarHeading, radarSector, latencyText, performanceText, identityText;
        RectTransform spinRing;

        int ally, rival;
        float tugWidth = ZoneMinWidth - 16f;
        bool liveMatch;

        public IMatchParticipant Local { get; set; }
        public Hotdrop.Input.TouchInputManager Touch { get; set; }

        sealed class RosterCard
        {
            public RectTransform rect;
            public Image fill;
            public TextMeshProUGUI name, hp, weapon, pad, state;
        }

        sealed class RadarMateMarker
        {
            public RectTransform root;
            public RectTransform facing;
            public Image ring;
            public Image[] chevron = new Image[2];
            public RectTransform elevation;
            public Image elevationBack;
            public TextMeshProUGUI elevationUp, elevationDown;
        }

        sealed class RadarPingMarker
        {
            public RectTransform root;
            public Image icon;
            public RectTransform elevation;
            public Image elevationBack;
            public TextMeshProUGUI elevationUp, elevationDown;
        }

        public static UiuxHud Create(Transform parent)
        {
            var node = UiDraw.Node(parent, "UiuxHud");
            UiDraw.Fill(node);
            UiDpPanel.Mark(node);
            node.SetAsFirstSibling();
            var hud = node.gameObject.AddComponent<UiuxHud>();
            hud.Build();
            return hud;
        }

        /// <summary>Readable live-match panels that world markers and pings must not cover.</summary>
        public void ReadablePanels(List<RectTransform> into)
        {
            into.Add(roster); into.Add(board); into.Add(zonePanel); into.Add(armor);
            // The container stays active outside a live match while its contents are hidden.
            if (liveMatch) into.Add(topRight);
        }

        void Build()
        {
            root = (RectTransform)transform;
            BuildRoster();
            BuildBoard();
            BuildTopRight();
            BuildArmor();
            BuildSoundCaptions();
            rosterFade = roster.gameObject.AddComponent<CanvasGroup>();
            radarFade = radar.gameObject.AddComponent<CanvasGroup>();
            SetLiveMatch(false);
        }

        // ------------------------------------------------------------ roster (top left)

        void BuildRoster()
        {
            roster = UiDraw.Place(UiDraw.Node(root, "Roster"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -Margin),
                new Vector2(RosterWidth, RosterHeader + 2f * (CardHeight + CardGap)));
            UiDraw.Place(UiDraw.Icon(roster, "GroupIcon", "group", 12f, Primary).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(2f, -7f), new Vector2(12f, 12f));
            var title = UiDraw.Text(roster, "RosterTitle", L("roster.title", "Squad"), UiFont.Heading, HudCaptionDp, OnSurfaceVariant, .08f);
            UiDraw.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(18f, -7f), new Vector2(UiDraw.Width(title) + 2f, 14f));
            rosterCount = UiDraw.Text(roster, "RosterCount", "", UiFont.MonoMedium, HudCaptionDp, Primary, .02f);
            UiDraw.Place(rosterCount.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(24f + UiDraw.Width(title), -7f), new Vector2(40f, 14f));
            // The local player's own K/D sits on the squad header so the right column stays map-only.
            performanceText = UiDraw.Text(roster, "Performance", F("performance.template", "K/D {0}/{1}", 0, 0), UiFont.MonoMedium, HudCaptionDp, Tertiary, 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(performanceText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-2f, -7f), new Vector2(76f, 14f));
            for (int i = 0; i < 2; i++)
            {
                var card = new RosterCard();
                var box = UiDraw.Box(roster, "Mate" + i, UiDraw.Hex("#0b1322", .82f), 6f);
                card.rect = UiDraw.Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -RosterHeader - i * (CardHeight + CardGap)), new Vector2(RosterWidth, CardHeight));
                UiDraw.Place(UiDraw.Dot(card.rect, "Live", 6f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -11f), new Vector2(6f, 6f));
                card.name = UiDraw.Text(card.rect, "Name", "", UiFont.BodySemiBold, HudCaptionDp, OnSurface, .01f);
                UiDraw.Place(card.name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(18f, -11f), new Vector2(76f, 14f));
                card.name.overflowMode = TextOverflowModes.Ellipsis;
                card.state = UiDraw.Text(card.rect, "State", "", UiFont.BodySemiBold, HudCaptionDp, Tertiary, 0f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(card.state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-8f, -11f), new Vector2(78f, 14f));
                card.state.overflowMode = TextOverflowModes.Ellipsis;
                var track = UiDraw.Box(card.rect, "Track", Highest, 1.5f);
                UiDraw.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -20f), new Vector2(CardBar, 3f));
                card.fill = UiDraw.Box(track.transform, "Fill", Primary, 1.5f);
                card.fill.rectTransform.anchorMin = Vector2.zero; card.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                card.fill.rectTransform.pivot = new Vector2(0f, .5f);
                card.fill.rectTransform.sizeDelta = new Vector2(CardBar, 0f);
                card.pad = UiDraw.Text(card.rect, "PadStatus", "", UiFont.MonoMedium, HudCaptionDp, Primary, .02f);
                UiDraw.Place(card.pad.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -32f), new Vector2(90f, 14f));
                card.pad.overflowMode = TextOverflowModes.Ellipsis;
                card.hp = UiDraw.Text(card.rect, "Hp", "", UiFont.MonoMedium, HudCaptionDp, Primary, .02f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(card.hp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-8f, -32f), new Vector2(64f, 14f));
                // Kept for the roster update, but not drawn: at phone size it crowded out pad state.
                card.weapon = UiDraw.Text(card.rect, "Weapon", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant);
                card.weapon.gameObject.SetActive(false);
                cards.Add(card);
            }
        }

        // ------------------------------------------------------------ scoreboard + zone (top centre)

        void BuildBoard()
        {
            var plate = UiDraw.Box(root, "Scoreboard", UiDraw.Hex("#060e1d", .86f), 6f);
            board = UiDraw.Place(plate.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -TopMargin), new Vector2(BoardWidth, BoardHeight));
            // Sides read by shape as well as colour (HUD spec): ally chevron, enemy diamond.
            var allyMark = UiDraw.Place(UiDraw.Node(board, "AllyMark"), new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(14f, 0f), new Vector2(12f, 12f));
            for (int j = 0; j < 2; j++)
            {
                var arm = UiDraw.Box(allyMark, "Chevron" + j, Primary, 1f).rectTransform;
                UiDraw.Place(arm, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(j == 0 ? -2.5f : 2.5f, 0f), new Vector2(2.5f, 9f));
                arm.localRotation = Quaternion.Euler(0f, 0f, j == 0 ? -35f : 35f);
            }
            allyScore = UiDraw.Text(board, "AllyScore", "0", UiFont.Heading, 18f, Primary, -.02f);
            UiDraw.Place(allyScore.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(34f, 26f));
            var clockBox = UiDraw.Box(board, "ClockBox", High, 4f);
            UiDraw.Place(clockBox.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(58f, 22f));
            clock = UiDraw.Text(clockBox.transform, "Clock", "03:00", UiFont.Mono, 15f, OnSurface, .03f, TextAlignmentOptions.Midline);
            UiDraw.Fill(clock.rectTransform);
            var enemyMark = UiDraw.Box(board, "EnemyMark", new Color(0f, 0f, 0f, 0f), 1f, Secondary, 1.5f).rectTransform;
            UiDraw.Place(enemyMark, new Vector2(1f, .5f), new Vector2(.5f, .5f), new Vector2(-14f, 0f), new Vector2(8f, 8f));
            enemyMark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            enemyScore = UiDraw.Text(board, "EnemyScore", "0", UiFont.Heading, 18f, Secondary, -.02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(enemyScore.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, 0f), new Vector2(34f, 26f));

            var zone = UiDraw.Box(root, "ZonePanel", UiDraw.Hex("#0b1322", .86f), 6f);
            zonePanel = UiDraw.Place(zone.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -TopMargin - BoardHeight - ZoneGap), new Vector2(ZoneMinWidth, ZoneHeight));
            UiDraw.Place(UiDraw.Icon(zonePanel, "Spin", "refresh", 12f, Primary).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -9f), new Vector2(12f, 12f));
            zoneName = UiDraw.Text(zonePanel, "ZoneName", "", UiFont.MonoMedium, HudCaptionDp, Primary, .02f);
            UiDraw.Place(zoneName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(24f, -9f), new Vector2(90f, 14f));
            zoneState = UiDraw.Text(zonePanel, "ZoneState", "", UiFont.Heading, HudCaptionDp, Tertiary, .06f, TextAlignmentOptions.Midline);
            UiDraw.Place(zoneState.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(4f, -9f), new Vector2(70f, 14f));
            zoneNext = UiDraw.Text(zonePanel, "ZoneNext", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant, .02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(zoneNext.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-8f, -9f), new Vector2(80f, 14f));
            var track = UiDraw.Box(zonePanel, "Tug", Highest, 1.5f);
            var t = UiDraw.Place(track.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 3f), new Vector2(tugWidth, TugHeight));
            t.gameObject.AddComponent<RectMask2D>();
            tugAlly = UiDraw.Box(t, "Ally", Primary).rectTransform;
            tugEnemy = UiDraw.Box(t, "Enemy", Secondary).rectTransform;
            tugMark = UiDraw.Box(t, "Neutral", Tertiary).rectTransform;
            SetTug(.5f);
        }

        void SetTug(float allyShare)
        {
            float width = tugWidth; const float mark = 4f;
            float a = (width - mark) * Mathf.Clamp01(allyShare);
            UiDraw.Place(tugAlly, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(a, TugHeight));
            UiDraw.Place(tugMark, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(a, 0f), new Vector2(mark, TugHeight));
            UiDraw.Place(tugEnemy, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(a + mark, 0f), new Vector2(width - a - mark, TugHeight));
        }

        // ------------------------------------------------------------ region + radar (top right)

        void BuildTopRight()
        {
            topRight = UiDraw.Place(UiDraw.Node(root, "TopRight"), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -Margin),
                new Vector2(RadarSize, RadarTop + RadarSize + 2f * StatusLine + CompassLine));
            // Ping, menu and Focus Aim are the touch controls' own 40 dp buttons in the row above;
            // the compass sits under the radar, followed by the region/latency status row.
            var chip = UiDraw.Node(topRight, "RegionChip");
            UiDraw.Place(UiDraw.Dot(chip, "Live", 6f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(2f, 0f), new Vector2(6f, 6f));
            regionText = UiDraw.Text(chip, "Region", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant, .02f);
            UiDraw.Place(regionText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(RadarSize - 40f - 12f, 14f));
            regionText.overflowMode = TextOverflowModes.Overflow;
            latencyText = UiDraw.Text(topRight, "Latency", L("latency.compactUnknown", "—"), UiFont.MonoMedium, HudCaptionDp, Primary, 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(latencyText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f),
                new Vector2(0f, -(RadarTop + RadarSize + StatusLine + CompassLine)), new Vector2(40f, 14f));
            // The player's own name is already on every menu; the HUD no longer repeats it.
            identityText = UiDraw.Text(topRight, "Identity", "", UiFont.BodySemiBold, HudCaptionDp, OnSurface);
            identityText.gameObject.SetActive(false);
            SetRegion("PRACTICE");

            var radarBox = UiDraw.Box(topRight, "Radar", UiDraw.Hex("#060e1d", .86f), 10f);
            radar = UiDraw.Place(radarBox.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -RadarTop), new Vector2(RadarSize, RadarSize));
            radar.gameObject.AddComponent<RectMask2D>();
            var grid = UiDraw.Node(radar, "DotGrid").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(grid.rectTransform);
            grid.texture = DotGridTexture();
            grid.color = UIDesignTokens.PrimaryAction;
            grid.uvRect = new Rect(0f, 0f, RadarSize / 14f, RadarSize / 14f);
            grid.raycastTarget = false;
            UiDraw.Place(UiDraw.Ring(radar, "OuterRing", RadarRing * 2f, 1f, UiDraw.Hex("#7cefff", .2f)).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(RadarRing * 2f, RadarRing * 2f));
            spinRing = UiDraw.Ring(radar, "SweepRing", 52f, 1f, UiDraw.Hex("#859396", .3f), 16).rectTransform;
            UiDraw.Place(spinRing, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(52f, 52f));
            radarMap = UiDraw.Place(UiDraw.Node(radar, "MapFootprint"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(RadarSize, RadarSize));
            radarField = UiDraw.Place(UiDraw.Node(radar, "Field"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(RadarSize, RadarSize));
            radarObjective = UiDraw.Place(UiDraw.Node(radarField, "Objective"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(16f, 16f));
            UiDraw.Fill(UiDraw.Dot(radarObjective, "Halo", 16f, UiDraw.Hex("#ffda88", .2f)).rectTransform);
            UiDraw.Place(UiDraw.Dot(radarObjective, "Core", 11f, Tertiary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(11f, 11f));
            UiDraw.Place(UiDraw.Dot(radarObjective, "Pip", 5f, Lowest).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(5f, 5f));
            for (int i = 0; i < 2; i++)
            {
                var mate = new RadarMateMarker
                {
                    root = UiDraw.Node(radarField, "Mate" + i),
                };
                UiDraw.Place(mate.root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(18f, 18f));
                mate.facing = UiDraw.Node(mate.root, "Facing");
                UiDraw.Fill(mate.facing);
                UiDraw.Fill(UiDraw.Dot(mate.facing, "Plate", 14f, UIDesignTokens.SurfacePanel).rectTransform);
                var ring = UiDraw.Ring(mate.facing, "AllyRing", 15f, 1.5f, UIDesignTokens.Ally);
                UiDraw.Fill(ring.rectTransform);
                mate.ring = ring;
                for (int j = 0; j < mate.chevron.Length; j++)
                {
                    var arm = UiDraw.Box(mate.facing, "Chevron" + j, UIDesignTokens.Ally, 1f);
                    arm.rectTransform.anchorMin = arm.rectTransform.anchorMax = new Vector2(.5f, .5f);
                    arm.rectTransform.sizeDelta = new Vector2(2f, 6f);
                    arm.rectTransform.anchoredPosition = new Vector2(j == 0 ? -2f : 2f, 1f);
                    arm.rectTransform.localRotation = Quaternion.Euler(0f, 0f, j == 0 ? 35f : -35f);
                    mate.chevron[j] = arm;
                }
                mate.elevation = UiDraw.Node(mate.root, "Elevation");
                UiDraw.Place(mate.elevation, Vector2.one, Vector2.one, new Vector2(4f, 4f), new Vector2(12f, 12f));
                mate.elevationBack = UiDraw.Dot(mate.elevation, "Back", 12f, UIDesignTokens.SurfacePanel);
                UiDraw.Fill(mate.elevationBack.rectTransform);
                mate.elevationUp = UiDraw.Icon(mate.elevation, "Above", "keyboard_double_arrow_up", 12f, UIDesignTokens.TextPrimary, true);
                UiDraw.Fill(mate.elevationUp.rectTransform);
                mate.elevationDown = UiDraw.Icon(mate.elevation, "Below", "keyboard_double_arrow_down", 12f, UIDesignTokens.TextPrimary, true);
                UiDraw.Fill(mate.elevationDown.rectTransform);
                mate.elevation.gameObject.SetActive(false);
                mate.elevationUp.gameObject.SetActive(false);
                mate.elevationDown.gameObject.SetActive(false);
                radarMates.Add(mate);
            }
            for (int i = 0; i < 8; i++)
            {
                var icon = UiDraw.Box(radarField, "Ping" + i, Color.white, 1f);
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(8f, 8f);
                rect.gameObject.SetActive(false);
                var marker = new RadarPingMarker { root = rect, icon = icon };
                marker.elevation = UiDraw.Node(rect, "Elevation");
                UiDraw.Place(marker.elevation, Vector2.one, Vector2.one, new Vector2(2f, 2f), new Vector2(8f, 8f));
                marker.elevationBack = UiDraw.Dot(marker.elevation, "Back", 8f, UIDesignTokens.SurfacePanel);
                UiDraw.Fill(marker.elevationBack.rectTransform);
                marker.elevationUp = UiDraw.Icon(marker.elevation, "Above", "keyboard_double_arrow_up", 8f, UIDesignTokens.TextPrimary, true);
                UiDraw.Fill(marker.elevationUp.rectTransform);
                marker.elevationDown = UiDraw.Icon(marker.elevation, "Below", "keyboard_double_arrow_down", 8f, UIDesignTokens.TextPrimary, true);
                UiDraw.Fill(marker.elevationDown.rectTransform);
                marker.elevation.gameObject.SetActive(false);
                marker.elevationUp.gameObject.SetActive(false);
                marker.elevationDown.gameObject.SetActive(false);
                radarPings.Add(marker);
            }
            radarSelf = UiDraw.Place(UiDraw.Dot(radar, "Self", 14f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(14f, 14f));
            UiDraw.Fill(UiDraw.Icon(radarSelf, "Heading", "navigation", 12f, UiDraw.Hex("#00363c"), true).rectTransform);
            radarHeading = UiDraw.Text(radar, "Hdg", F("radar.headingTemplate", "HDG {0:000}°", 0), UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant);
            UiDraw.Place(radarHeading.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(6f, 2f), new Vector2(72f, 14f));
            radarSector = UiDraw.Text(radar, "Sector", "", UiFont.MonoMedium, HudCaptionDp, Primary, 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(radarSector.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -2f), new Vector2(72f, 14f));

            compassStrip = UiDraw.Place(UiDraw.Node(topRight, "CompassStrip"), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(RadarTop + RadarSize + StatusLine)), new Vector2(RadarSize, CompassLine));
            compassStrip.gameObject.AddComponent<RectMask2D>();
            compassFade = compassStrip.gameObject.AddComponent<CanvasGroup>();
            compassFade.interactable = false;
            compassFade.blocksRaycasts = false;
            var compassPlate = UiDraw.Box(compassStrip, "Track", UIDesignTokens.SurfacePanel, 4f,
                UIDesignTokens.WithAlpha(UIDesignTokens.BorderColor(), .55f)).GetComponent<Image>();
            UiDraw.Fill(compassPlate.rectTransform);
            compassPlate.raycastTarget = false;
            string[] directions = { L("compass.north", "N"), L("compass.east", "E"), L("compass.south", "S"), L("compass.west", "W") };
            for (int i = 0; i < compassLabels.Length; i++)
            {
                compassLabels[i] = UiDraw.Text(compassStrip, "Direction" + i, directions[i], UiFont.MonoMedium,
                    HudCaptionDp, OnSurfaceVariant, 0f, TextAlignmentOptions.Midline);
                UiDraw.Place(compassLabels[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(0f, -2f), new Vector2(16f, 14f));
            }
            var forwardTick = UiDraw.Box(compassStrip, "ForwardTick", Primary, 1f);
            UiDraw.Place(forwardTick.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, 7f), new Vector2(2f, 4f));
            forwardTick.raycastTarget = false;
        }

        static Texture2D dotGrid;

        static Texture2D DotGridTexture()
        {
            if (dotGrid != null) return dotGrid;
            const int n = 28; // 14 px cells at 2x
            dotGrid = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f));
                float alpha = .12f * Mathf.Clamp01(2.5f - d);
                dotGrid.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            dotGrid.Apply(false, true);
            return dotGrid;
        }

        /// <summary>Prints the connection the match is using; offline practice says so.</summary>
        public void SetRegion(string value)
        {
            regionText.text = string.Equals(value, "PRACTICE", System.StringComparison.OrdinalIgnoreCase)
                ? L("region.practice", "Practice") : value;
            var chip = (RectTransform)regionText.transform.parent;
            UiDraw.Place(chip, new Vector2(0f, 1f), new Vector2(0f, .5f),
                new Vector2(0f, -(RadarTop + RadarSize + StatusLine + CompassLine)), new Vector2(RadarSize - 40f, 14f));
        }

        void BuildSoundCaptions()
        {
            // Subtitle-sized, just above the suit bar: a caption names a sound, it is not a banner.
            var plate = UiDraw.Box(root, "GameplaySoundCaption", UiDraw.Hex("#060e1d", .86f), 6f);
            soundCaptionRoot = UiDraw.Place(plate.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, ArmorBottom + ArmorHeight + 8f), new Vector2(160f, 24f));
            soundCaptionText = UiDraw.Text(soundCaptionRoot, "Caption", "", UiFont.HeadingSemiBold, HudCaptionDp,
                UiDraw.Hex("#f4f7fb"), .04f, TextAlignmentOptions.Midline);
            UiDraw.Fill(soundCaptionText.rectTransform, 10f, 0f, 10f, 0f);
            soundCaptionText.alignment = TextAlignmentOptions.Midline;
            soundCaptionRoot.gameObject.SetActive(false);
        }

        void OnEnable() => Hotdrop.Core.GameAudio.OnCriticalCaption += ShowSoundCaption;
        void OnDisable() => Hotdrop.Core.GameAudio.OnCriticalCaption -= ShowSoundCaption;

        void ShowSoundCaption(string caption, int priority)
        {
            if (!liveMatch || PlayerPrefs.GetInt(Hotdrop.Core.GameAudio.SoundCaptionsKey, 1) != 1 || soundCaptionRoot == null) return;
            // Unity's audio priority uses lower numbers for more important cues. Keep an
            // active threat caption visible when a less important cue arrives.
            if (soundCaptionRemaining > 0f && priority > soundCaptionPriority) return;
            soundCaptionText.text = caption;
            soundCaptionRoot.sizeDelta = new Vector2(Mathf.Min(320f, UiDraw.Width(soundCaptionText) + 24f), 24f);
            soundCaptionRemaining = 1.6f;
            soundCaptionPriority = priority;
            soundCaptionRoot.gameObject.SetActive(true);
        }

        public void SetLatency(int milliseconds)
        {
            if (latencyText != null) latencyText.text = milliseconds > 0 ? F("latency.compactTemplate", "{0}ms", milliseconds) : L("latency.compactUnknown", "—");
        }

        // ------------------------------------------------------------ armor (bottom centre)

        void BuildArmor()
        {
            var pill = UiDraw.Box(root, "ArmorPill", UiDraw.Hex("#060e1d", .82f), 8f);
            armor = UiDraw.Place(pill.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, ArmorBottom), new Vector2(ArmorWidth, ArmorHeight));
            // Row 1: pad state (the action this game is built around) and the zone.
            UiDraw.Place(UiDraw.Icon(armor, "Air", "air", 12f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(10f, -11f), new Vector2(12f, 12f));
            armorPad = UiDraw.Text(armor, "ArmorPad", "", UiFont.MonoMedium, HudCaptionDp, Primary, .04f);
            UiDraw.Place(armorPad.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(26f, -11f), new Vector2(124f, 14f));
            armorZone = UiDraw.Text(armor, "ArmorZone", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant, .02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(armorZone.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -11f), new Vector2(76f, 14f));
            armorZone.overflowMode = TextOverflowModes.Ellipsis;
            // Row 2: suit integrity as a number and five segments.
            UiDraw.Place(UiDraw.Icon(armor, "Heart", "favorite", 12f, Primary).rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(10f, 13f), new Vector2(12f, 12f));
            armorValue = UiDraw.Text(armor, "ArmorValue", "100", UiFont.Mono, 16f, Primary, .02f);
            UiDraw.Place(armorValue.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(26f, 13f), new Vector2(40f, 18f));
            var track = UiDraw.Box(armor, "Segments", Highest, 2f);
            var t = UiDraw.Place(track.rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-10f, 13f), new Vector2(ArmorBar, 8f));
            float seg = (ArmorBar - 4 * 2f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                var s = UiDraw.Box(t, "Segment" + i, Primary, 2f);
                UiDraw.Place(s.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(i * (seg + 2f), 0f), new Vector2(seg, 8f));
                armorSegments.Add(s);
            }
        }

        // ------------------------------------------------------------ pushed state

        /// <summary>Training has no score, clock, roster or rotation; a match shows them all.</summary>
        public void SetLiveMatch(bool live)
        {
            liveMatch = live;
            if (!live && soundCaptionRoot != null) soundCaptionRoot.gameObject.SetActive(false);
            if (live) BuildMapFootprint(MatchView.Current?.MapConfig);
            if (rosterFade != null) rosterFade.alpha = 1f;
            if (radarFade != null) radarFade.alpha = 1f;
            roster.gameObject.SetActive(live);
            board.gameObject.SetActive(live);
            zonePanel.gameObject.SetActive(live);
            radar.gameObject.SetActive(live);
            regionText.transform.parent.gameObject.SetActive(live);
            latencyText.gameObject.SetActive(live);
            performanceText.gameObject.SetActive(live);
            identityText.gameObject.SetActive(false);
            if (!live) armorZone.text = L("armor.trainingCompact", "TRAINING");
        }

        public void SetScores(int allyScoreValue, int rivalScoreValue)
        {
            ally = allyScoreValue; rival = rivalScoreValue;
            allyScore.text = ally.ToString();
            enemyScore.text = rival.ToString();
            SetTug(ally + rival > 0 ? (float)ally / (ally + rival) : .5f);
        }

        /// <summary>Remaining seconds, shown as the reference's two-digit MM:SS clock.</summary>
        public void SetClock(float remaining, bool overtime)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(remaining));
            clock.text = (overtime ? L("clock.overtimePrefix", "OT ") : "") + $"{total / 60:00}:{total % 60:00}";
        }

        public void SetZone(string zone, string status, Color statusColor)
        {
            zoneName.text = F("zone.relayTemplate", "{0} Relay", zone);
            zoneState.text = status;
            zoneState.color = statusColor;
            armorZone.text = F("armor.zoneCompactTemplate", "{0}", zone);
            LayoutZoneRow();
        }

        /// <summary>The row is justify-between: name left, state centred in the gap, next right.
        /// The panel grows past its 280 px minimum rather than letting them collide.</summary>
        void LayoutZoneRow()
        {
            float left = 24f + UiDraw.Width(zoneName), state = UiDraw.Width(zoneState), next = UiDraw.Width(zoneNext);
            float width = Mathf.Max(ZoneMinWidth, left + 12f + state + 12f + (next > 0f ? next + 8f : 8f));
            float rightEdge = width - (next > 0f ? next + 8f + 12f : 8f);
            UiDraw.Place(zonePanel, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -TopMargin - BoardHeight - ZoneGap), new Vector2(width, ZoneHeight));
            zoneName.rectTransform.sizeDelta = new Vector2(left - 24f + 2f, 14f);
            zoneNext.rectTransform.sizeDelta = new Vector2(next + 2f, 14f);
            UiDraw.Place(zoneState.rectTransform, new Vector2(0f, 1f), new Vector2(.5f, .5f), new Vector2((left + rightEdge) / 2f, -9f), new Vector2(state + 2f, 14f));
            ((RectTransform)zonePanel.Find("Tug")).sizeDelta = new Vector2(width - 16f, TugHeight);
            tugWidth = width - 16f;
            SetTug(ally + rival > 0 ? (float)ally / (ally + rival) : .5f);
        }

        /// <summary>Upcoming rotation as "NEXT: W-08s"; empty when there is none.</summary>
        public void SetNext(string zone, int seconds)
        {
            zoneNext.text = string.IsNullOrEmpty(zone) ? "" : F("rotation.compactTemplate", "NEXT: {0}-{1:00}s", zone.Substring(0, 1), seconds);
            LayoutZoneRow();
        }

        public void SetHealth(float normalized)
        {
            int hp = Mathf.CeilToInt(Mathf.Clamp01(normalized) * 100f);
            armorValue.text = hp.ToString();
            Color c = normalized <= .2f ? Coral : Primary;
            armorValue.color = c;
            for (int i = 0; i < armorSegments.Count; i++)
            {
                float lit = Mathf.Clamp01(normalized * armorSegments.Count - i);
                armorSegments[i].color = lit > 0f ? new Color(c.r, c.g, c.b, Mathf.Lerp(.35f, 1f, lit)) : new Color(0f, 0f, 0f, 0f);
            }
        }

        // ------------------------------------------------------------ physical size

        RectTransform[] scaledPanels;
        Vector2[] panelBasePositions;
        float appliedScale = 1f;

        /// <summary>
        /// Draws the edge-anchored panels at physical dp on phones where a canvas unit is smaller
        /// than one (the same factor the touch controls use), so the clock, scores and suit
        /// integrity are not half-size on dense screens. Each panel scales about its own anchor
        /// edge and its offset from that edge scales with it, keeping the composition intact.
        /// </summary>
        /// <summary>Re-applies the dp scale now, for a frame composed without a LateUpdate (captures).</summary>
        public void RefreshPhysicalScale() => ApplyPhysicalScale(UiDpPanel.Scale);

        void ApplyPhysicalScale(float k)
        {
            k = Mathf.Max(1f, k);
            if (scaledPanels == null)
            {
                scaledPanels = new[] { roster, board, zonePanel, topRight, armor, soundCaptionRoot };
                panelBasePositions = new Vector2[scaledPanels.Length];
                for (int i = 0; i < scaledPanels.Length; i++) panelBasePositions[i] = scaledPanels[i].anchoredPosition;
            }
            if (Mathf.Approximately(k, appliedScale)) return;
            appliedScale = k;
            for (int i = 0; i < scaledPanels.Length; i++)
            {
                scaledPanels[i].localScale = new Vector3(k, k, 1f);
                scaledPanels[i].anchoredPosition = panelBasePositions[i] * k;
            }
        }

        // ------------------------------------------------------------ per-frame reads

        void LateUpdate()
        {
            if (soundCaptionRemaining > 0f)
            {
                if (PlayerPrefs.GetInt(Hotdrop.Core.GameAudio.SoundCaptionsKey, 1) != 1)
                {
                    soundCaptionRemaining = 0f;
                    soundCaptionPriority = int.MaxValue;
                    if (soundCaptionRoot != null) soundCaptionRoot.gameObject.SetActive(false);
                }
                else
                {
                    soundCaptionRemaining -= Time.unscaledDeltaTime;
                    if (soundCaptionRemaining <= 0f && soundCaptionRoot != null)
                    {
                        soundCaptionPriority = int.MaxValue;
                        soundCaptionRoot.gameObject.SetActive(false);
                    }
                }
            }
            if (spinRing != null) spinRing.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 20f);
            ApplyPhysicalScale(UiDpPanel.Scale);
            if (Touch != null)
            {
                armorPad.text = Touch.LaunchAvailable ? L("pad.launchReady", "Launch Ready")
                    : !Touch.PadAvailable ? L("pad.recharging", "Pad Recharging") : L("pad.ready", "Pad Ready");
            }
            var view = MatchView.Current;
            if (!liveMatch || view == null || Local == null) return;
            if (Local is Object unityLocal && unityLocal == null) return;
            performanceText.text = F("performance.template", "K/D {0}/{1}", Local.Eliminations, Local.Deaths);
            identityText.text = Local.DisplayName;
            UpdateRoster(view);
            UpdateRadar(view);
            UpdateCombatFade(view);
        }

        static float FiniteOr(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        void UpdateCombatFade(IMatchView view)
        {
            var rules = view.Rules != null ? view.Rules
                : fallbackRules != null ? fallbackRules : fallbackRules = Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
            float combatRange = rules != null ? Mathf.Max(0f, FiniteOr(rules.cockpitCombatRangeMeters, DefaultCombatRangeMeters)) : DefaultCombatRangeMeters;
            float fadedOpacity = rules != null ? Mathf.Clamp01(FiniteOr(rules.cockpitFadedOpacity, DefaultFadedOpacity)) : DefaultFadedOpacity;
            float fadeSeconds = rules != null ? Mathf.Max(0.01f, FiniteOr(rules.cockpitFadeDurationSeconds, DefaultFadeSeconds)) : DefaultFadeSeconds;
            float rosterIdleSeconds = rules != null ? Mathf.Max(0f, FiniteOr(rules.rosterIdleFadeSeconds, DefaultRosterIdleFadeSeconds)) : DefaultRosterIdleFadeSeconds;

            bool combatNearby = Touch != null && Touch.FireHeld;
            if (!combatNearby)
            {
                for (int i = 0; i < view.Participants.Count; i++)
                {
                    var player = view.Participants[i];
                    if (player == null || !player.IsAlive || player.TeamId == Local.TeamId) continue;
                    if (Vector3.Distance(Local.Position, player.Position) <= combatRange)
                    {
                        combatNearby = true;
                        break;
                    }
                }
            }

            bool cockpitMode = PlayerPrefs.GetInt(Hotdrop.Core.OfflinePauseMenu.CockpitModeKey, 1) == 1;
            bool lowPriority = cockpitMode && (combatNearby || view.IsEndgame);
            bool rosterIdle = Touch != null && Time.unscaledTime - Touch.LastGameplayInputAt >= rosterIdleSeconds;
            float target = lowPriority ? fadedOpacity : 1f;
            float rosterTarget = lowPriority || rosterIdle ? fadedOpacity : 1f;
            // Full fade between 1 and the faded opacity takes fadeSeconds; a zero-span config still restores.
            float fadeSpan = 1f - fadedOpacity;
            float step = (fadeSpan > 0f ? fadeSpan : 1f) * Time.unscaledDeltaTime / fadeSeconds;
            if (rosterFade != null) rosterFade.alpha = Mathf.MoveTowards(rosterFade.alpha, rosterTarget, step);
            if (radarFade != null)
            {
                radarFade.alpha = Mathf.MoveTowards(radarFade.alpha, target, step);
                if (compassFade != null) compassFade.alpha = radarFade.alpha;
            }
        }

        void UpdateRoster(IMatchView view)
        {
            int slot = 0, alive = 1, total = 1;
            var players = view.Participants;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || ReferenceEquals(p, Local) || p.TeamId != Local.TeamId) continue;
                total++;
                if (p.IsAlive) alive++;
                if (slot >= cards.Count) continue;
                var card = cards[slot++];
                card.rect.gameObject.SetActive(true);
                card.name.text = p.DisplayName;
                int hp = Mathf.CeilToInt(p.HealthFraction * 100f);
                card.hp.text = p.IsAlive ? F("roster.healthTemplate", "{0} HP", hp) : L("roster.down", "Down");
                card.hp.color = !p.IsAlive || p.HealthFraction < .25f ? Coral : p.HealthFraction < .7f ? TertiaryDim : Primary;
                card.fill.rectTransform.sizeDelta = new Vector2(CardBar * (p.IsAlive ? p.HealthFraction : 0f), 0f);
                card.fill.color = p.HealthFraction >= .99f ? Primary : PrimaryContainer;
                card.weapon.text = p is PlayerController pc && pc.CurrentWeapon != null ? PreparationView.WeaponShortName(pc.CurrentWeapon) : "";
                bool padsEnabled = view.PadsEnabled;
                card.pad.text = !padsEnabled ? L("roster.padDisabled", "Pad Off")
                    : p.HasPadCharge ? L("roster.padReady", "Pad Ready")
                    : F("roster.padRechargeTemplate", "PAD {0}s", Mathf.CeilToInt(Mathf.Max(0f, p.PadRechargeRemaining)));
                card.pad.color = padsEnabled && p.HasPadCharge ? Primary : OnSurfaceVariant;
                float toZone = Vector3.Distance(p.Position, view.ActiveObjectivePosition);
                if (!p.IsAlive) { card.state.text = F("roster.respawnTemplate", "RESPAWN {0}s", Mathf.CeilToInt(p.RespawnRemaining)); card.state.color = Coral; }
                else if (toZone < 6f) { card.state.text = L("roster.onObjective", "On Objective"); card.state.color = Tertiary; }
                else { card.state.text = F("roster.distanceTemplate", "{0:0} m out", toZone); card.state.color = OnSurfaceVariant; }
            }
            for (int i = slot; i < cards.Count; i++) cards[i].rect.gameObject.SetActive(false);
            rosterCount.text = F("roster.compactTemplate", "{0}/{1}", alive, total);
        }

        void UpdateRadar(IMatchView view)
        {
            var cam = UnityEngine.Camera.main;
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            bool northUp = PlayerPrefs.GetInt(Hotdrop.Core.OfflinePauseMenu.NorthUpRadarKey, 0) == 1;
            float relativeYaw = northUp ? 0f : yaw;
            UpdateCompass(yaw);
            radarHeading.text = F("radar.headingTemplate", "HDG {0:000}°", Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)));
            radarSector.text = PreparationView.ZoneShortLabel(view.ScoringZone);
            // Same convention as the teammate arrows below: heading minus map rotation, negated for UI Z.
            // Zero in player-up mode, so the arrow stays upright there.
            if (radarSelf != null) radarSelf.localRotation = Quaternion.Euler(0f, 0f, -(yaw - relativeYaw));
            var origin = Local.Position;
            float range = 40f; // metres from the centre to the outer ring
            Vector2 ToRadarUnclamped(Vector3 world)
            {
                var d = world - origin;
                var flat = Quaternion.Euler(0f, -relativeYaw, 0f) * new Vector3(d.x, 0f, d.z);
                return new Vector2(flat.x, flat.z) / range * RadarRing;
            }
            Vector2 ToRadar(Vector3 world) =>
                Vector2.ClampMagnitude(ToRadarUnclamped(world), RadarRing + 6f);
            var map = view.MapConfig;
            float elevationThreshold = map != null ? Mathf.Max(0f, map.platformHeight * .5f) : float.PositiveInfinity;
            if (map != null && radarMap != null)
            {
                var center = new Vector3((map.boundsMin.x + map.boundsMax.x) * .5f, 0f,
                    (map.boundsMin.y + map.boundsMax.y) * .5f);
                radarMap.anchoredPosition = ToRadarUnclamped(center);
                radarMap.localRotation = Quaternion.Euler(0f, 0f, relativeYaw);
            }
            radarObjective.anchoredPosition = ToRadar(view.ActiveObjectivePosition);
            int slot = 0;
            foreach (var p in view.Participants)
            {
                if (p == null || ReferenceEquals(p, Local) || p.TeamId != Local.TeamId || slot >= radarMates.Count) continue;
                var marker = radarMates[slot++];
                marker.root.gameObject.SetActive(p.IsAlive);
                if (!p.IsAlive) continue;
                marker.root.anchoredPosition = ToRadar(p.Position);
                Vector3 facing = Quaternion.Euler(0f, -relativeYaw, 0f) * p.FacingForward;
                marker.facing.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg);
                Color allyColor = UIDesignTokens.Ally;
                marker.ring.color = allyColor;
                for (int j = 0; j < marker.chevron.Length; j++) marker.chevron[j].color = allyColor;
                float heightDelta = p.Position.y - origin.y;
                bool above = heightDelta > elevationThreshold;
                bool below = heightDelta < -elevationThreshold;
                marker.elevation.gameObject.SetActive(above || below);
                marker.elevationUp.gameObject.SetActive(above);
                marker.elevationDown.gameObject.SetActive(below);
                marker.elevationBack.color = UIDesignTokens.SurfacePanel;
            }
            for (int i = slot; i < radarMates.Count; i++) radarMates[i].root.gameObject.SetActive(false);
            UpdateRadarPings(view, ToRadar, origin, elevationThreshold);
        }

        void UpdateCompass(float yaw)
        {
            float unitsPerDegree = (RadarSize - 16f) / 360f;
            for (int i = 0; i < compassLabels.Length; i++)
            {
                float cardinalBearing = i * 90f;
                float relativeBearing = Mathf.DeltaAngle(yaw, cardinalBearing);
                compassLabels[i].rectTransform.anchoredPosition = new Vector2(relativeBearing * unitsPerDegree, 0f);
            }
        }

        void UpdateRadarPings(IMatchView view, System.Func<Vector3, Vector2> toRadar, Vector3 origin, float elevationThreshold)
        {
            var pings = PingSystem.Instance != null ? PingSystem.Instance.ActivePings : null;
            int slot = 0;
            if (pings != null)
            {
                for (int i = 0; i < pings.Count && slot < radarPings.Count; i++)
                {
                    var ping = pings[i];
                    if (ping.teamId != Local.TeamId || Time.time >= ping.expiresAt) continue;
                    var marker = radarPings[slot++];
                    marker.root.gameObject.SetActive(true);
                    marker.root.anchoredPosition = toRadar(ping.position);
                    marker.root.localRotation = Quaternion.identity;
                    marker.icon.sprite = RadarPingSprites.For(ping.kind);
                    marker.icon.type = Image.Type.Simple;
                    Color color = ping.kind switch
                    {
                        PingKind.EnemyHere => UIDesignTokens.HazardCoral,
                        PingKind.GroupUp => UIDesignTokens.Ally,
                        PingKind.PadReady => UIDesignTokens.ObjectiveGold,
                        _ => UIDesignTokens.PrimaryAction,
                    };
                    marker.icon.color = color;
                    float heightDelta = ping.position.y - origin.y;
                    bool above = heightDelta > elevationThreshold;
                    bool below = heightDelta < -elevationThreshold;
                    marker.elevation.gameObject.SetActive(above || below);
                    marker.elevationUp.gameObject.SetActive(above);
                    marker.elevationDown.gameObject.SetActive(below);
                    marker.elevationBack.color = UIDesignTokens.SurfacePanel;
                }
            }
            for (int i = slot; i < radarPings.Count; i++) radarPings[i].root.gameObject.SetActive(false);
        }

        void BuildMapFootprint(Hotdrop.Config.MapConfig map)
        {
            if (map == null || radarMap == null || radarMap.childCount > 0) return;
            const float pixelsPerMetre = RadarRing / 40f;
            Vector2 center = (map.boundsMin + map.boundsMax) * .5f;
            Vector2 MapPosition(Vector3 world) => new Vector2(
                (world.x - center.x) * pixelsPerMetre,
                (world.z - center.y) * pixelsPerMetre);

            DrawFootprint("ArenaBoundary", new Vector2((map.boundsMax.x - map.boundsMin.x) * pixelsPerMetre,
                (map.boundsMax.y - map.boundsMin.y) * pixelsPerMetre), Vector2.zero, UIDesignTokens.WithAlpha(UIDesignTokens.TextSecondary, .28f));
            DrawFootprint("NorthSpawn", new Vector2(map.spawnRoomSize.x * pixelsPerMetre, map.spawnRoomSize.y * pixelsPerMetre),
                MapPosition(map.northSpawnCenter), UIDesignTokens.WithAlpha(UIDesignTokens.Ally, .22f));
            DrawFootprint("SouthSpawn", new Vector2(map.spawnRoomSize.x * pixelsPerMetre, map.spawnRoomSize.y * pixelsPerMetre),
                MapPosition(map.southSpawnCenter), UIDesignTokens.WithAlpha(UIDesignTokens.Ally, .22f));
            if (map.platformCenters != null)
            {
                for (int i = 0; i < map.platformCenters.Length; i++)
                {
                    var platform = map.platformCenters[i];
                    var plate = DrawFootprint("RaisedPlatform_" + i,
                        new Vector2(map.platformSize.x * pixelsPerMetre, map.platformSize.y * pixelsPerMetre),
                        MapPosition(platform), UIDesignTokens.WithAlpha(UIDesignTokens.ObjectiveGold, .34f));
                    if (platform.y > map.groundLevelY)
                    {
                        var level = UiDraw.Box(plate, "ElevationCue", UIDesignTokens.WithAlpha(UIDesignTokens.ObjectiveGold, .75f), 1f);
                        UiDraw.Place(level.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(7f, 7f));
                        level.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    }
                }
            }
        }

        RectTransform DrawFootprint(string name, Vector2 size, Vector2 position, Color accent)
        {
            var box = UiDraw.Box(radarMap, name, UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .12f),
                2f, accent, 1f);
            var rect = box.rectTransform;
            UiDraw.Place(rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            return rect;
        }
    }
}
