using System.Collections.Generic;
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
        const float Margin = 24f;
        const float HudCaptionDp = UIDesignTokens.CaptionDp;
        const float HudValueDp = UIDesignTokens.BodyLargeDp;
        const float HudEmphasisDp = UIDesignTokens.TitleMediumDp;
        static Color Primary => UiDraw.Hex("#7cefff");
        static Color PrimaryContainer => UiDraw.Hex("#30d5e8");
        static Color Secondary => UiDraw.Hex("#ffb2b7");
        static Color Tertiary => UiDraw.Hex("#ffda88");
        static Color TertiaryDim => UiDraw.Hex("#edc157");
        static Color OnSurface => UiDraw.Hex("#dbe2f8");
        static Color OnSurfaceVariant => UiDraw.Hex("#bbc9cb");
        static Color Outline => UiDraw.Hex("#859396");
        static Color Lowest => UiDraw.Hex("#060e1d");
        static Color Low => UiDraw.Hex("#141c2b");
        static Color High => UiDraw.Hex("#222a3a");
        static Color Highest => UiDraw.Hex("#2d3545");
        static Color Coral => UiDraw.Hex("#ff6376");

        RectTransform root, roster, board, zonePanel, topRight, radar, armor;
        RectTransform soundCaptionRoot;
        TextMeshProUGUI soundCaptionText;
        float soundCaptionRemaining;
        CanvasGroup rosterFade, topRightFade;
        TextMeshProUGUI rosterCount, allyScore, enemyScore, clock, zoneName, zoneState, zoneNext, regionText, armorValue, armorZone, armorPad;
        RectTransform tugAlly, tugEnemy, tugMark;
        readonly List<Image> armorSegments = new List<Image>();
        readonly List<RosterCard> cards = new List<RosterCard>();
        RectTransform radarSelf, radarObjective, radarField, radarMap;
        readonly List<RectTransform> radarMates = new List<RectTransform>();
        readonly List<(RectTransform root, Image icon, RectTransform[] triangle)> radarPings = new List<(RectTransform, Image, RectTransform[])>();
        TextMeshProUGUI radarHeading, radarSector, latencyText, performanceText, identityText;
        RectTransform spinRing;

        int ally, rival;
        float tugWidth = 256f;
        bool liveMatch;

        public IMatchParticipant Local { get; set; }
        public Hotdrop.Input.TouchInputManager Touch { get; set; }

        sealed class RosterCard
        {
            public RectTransform rect;
            public Image fill;
            public TextMeshProUGUI name, hp, weapon, state;
        }

        public static UiuxHud Create(Transform parent)
        {
            var node = UiDraw.Node(parent, "UiuxHud");
            UiDraw.Fill(node);
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
            topRightFade = topRight.gameObject.AddComponent<CanvasGroup>();
            SetLiveMatch(false);
        }

        // ------------------------------------------------------------ roster (top left)

        void BuildRoster()
        {
            roster = UiDraw.Place(UiDraw.Node(root, "Roster"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -Margin), new Vector2(256f, 160f));
            UiDraw.Place(UiDraw.Icon(roster, "GroupIcon", "group", 14f, Primary).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(4f, -7f), new Vector2(14f, 14f));
            UiDraw.Place(UiDraw.Text(roster, "RosterTitle", "SQUAD", UiFont.Heading, HudCaptionDp, OnSurfaceVariant, .08f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -7f), new Vector2(180f, 14f));
            rosterCount = UiDraw.Text(roster, "RosterCount", "", UiFont.MonoMedium, HudCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(rosterCount.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-4f, -7f), new Vector2(120f, 14f));
            for (int i = 0; i < 2; i++)
            {
                var card = new RosterCard();
                var box = UiDraw.Box(roster, "Mate" + i, UiDraw.Hex("#141c2b", .9f), 8f);
                card.rect = UiDraw.Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -16f - i * 70f), new Vector2(256f, 66f));
                UiDraw.Place(UiDraw.Dot(card.rect, "Live", 8f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -19f), new Vector2(8f, 8f));
                card.name = UiDraw.Text(card.rect, "Name", "", UiFont.BodySemiBold, HudValueDp, OnSurface, .01f);
                UiDraw.Place(card.name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -19f), new Vector2(170f, 22f));
                card.name.overflowMode = TextOverflowModes.Ellipsis;
                card.hp = UiDraw.Text(card.rect, "Hp", "", UiFont.MonoMedium, HudCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(card.hp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-8f, -19f), new Vector2(70f, 14f));
                var track = UiDraw.Box(card.rect, "Track", Highest, 3f);
                UiDraw.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -34f), new Vector2(240f, 6f));
                card.fill = UiDraw.Box(track.transform, "Fill", Primary, 3f);
                card.fill.rectTransform.anchorMin = Vector2.zero; card.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                card.fill.rectTransform.pivot = new Vector2(0f, .5f);
                card.fill.rectTransform.sizeDelta = new Vector2(240f, 0f);
                card.weapon = UiDraw.Text(card.rect, "Weapon", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant, .05f);
                UiDraw.Place(card.weapon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -51f), new Vector2(150f, 14f));
                card.state = UiDraw.Text(card.rect, "State", "", UiFont.MonoMedium, HudCaptionDp, Tertiary, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(card.state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-8f, -51f), new Vector2(120f, 14f));
                cards.Add(card);
            }
        }

        // ------------------------------------------------------------ scoreboard + zone (top centre)

        void BuildBoard()
        {
            var plate = UiDraw.Box(root, "Scoreboard", UiDraw.Hex("#060e1d", .9f), 8f);
            board = UiDraw.Place(plate.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -Margin), new Vector2(228f, 52f));
            // Ally block.
            UiDraw.Place(UiDraw.Box(board, "AllyBar", Primary, 2f).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(8f, 24f));
            UiDraw.Place(UiDraw.Text(board, "AllyLabel", "ALLY", UiFont.Heading, HudCaptionDp, Primary, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 13f), new Vector2(40f, 14f));
            allyScore = UiDraw.Text(board, "AllyScore", "0", UiFont.Heading, HudEmphasisDp, Primary, -.025f);
            UiDraw.Place(allyScore.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, -7f), new Vector2(40f, 26f));
            // Clock.
            var clockBox = UiDraw.Box(board, "ClockBox", High, 4f);
            UiDraw.Place(clockBox.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(76f, 44f));
            UiDraw.Place(UiDraw.Text(clockBox.transform, "ClockLabel", "CLOCK", UiFont.Heading, HudCaptionDp, Outline, .1f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 11f), new Vector2(80f, 14f));
            clock = UiDraw.Text(clockBox.transform, "Clock", "03:00", UiFont.Mono, HudValueDp, OnSurface, .05f, TextAlignmentOptions.Midline);
            UiDraw.Place(clock.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -7f), new Vector2(80f, 22f));
            // Enemy block.
            UiDraw.Place(UiDraw.Box(board, "EnemyBar", Secondary, 2f).rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(8f, 24f));
            UiDraw.Place(UiDraw.Text(board, "EnemyLabel", "ENEMY", UiFont.Heading, HudCaptionDp, Secondary, .05f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, 13f), new Vector2(60f, 14f));
            enemyScore = UiDraw.Text(board, "EnemyScore", "0", UiFont.Heading, HudEmphasisDp, Secondary, -.025f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(enemyScore.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, -7f), new Vector2(60f, 26f));

            var zone = UiDraw.Box(root, "ZonePanel", UiDraw.Hex("#141c2b", .95f), 8f);
            zonePanel = UiDraw.Place(zone.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -Margin - 56f), new Vector2(280f, 38f));
            UiDraw.Place(UiDraw.Icon(zonePanel, "Spin", "refresh", 13f, Primary).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(12f, -13f), new Vector2(13f, 13f));
            zoneName = UiDraw.Text(zonePanel, "ZoneName", "", UiFont.MonoMedium, HudCaptionDp, Primary, .05f);
            UiDraw.Place(zoneName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(29f, -13f), new Vector2(110f, 14f));
            zoneState = UiDraw.Text(zonePanel, "ZoneState", "", UiFont.Heading, HudCaptionDp, Tertiary, .1f, TextAlignmentOptions.Midline);
            UiDraw.Place(zoneState.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(4f, -13f), new Vector2(110f, 14f));
            zoneNext = UiDraw.Text(zonePanel, "ZoneNext", "", UiFont.MonoMedium, HudCaptionDp, Outline, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(zoneNext.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -13f), new Vector2(100f, 14f));
            var track = UiDraw.Box(zonePanel, "Tug", Highest, 4f);
            var t = UiDraw.Place(track.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 6f), new Vector2(256f, 8f));
            t.gameObject.AddComponent<RectMask2D>();
            tugAlly = UiDraw.Box(t, "Ally", Primary).rectTransform;
            tugEnemy = UiDraw.Box(t, "Enemy", Secondary).rectTransform;
            tugMark = UiDraw.Box(t, "Neutral", Tertiary).rectTransform;
            SetTug(.5f);
        }

        void SetTug(float allyShare)
        {
            float width = tugWidth; const float mark = 6f;
            float a = (width - mark) * Mathf.Clamp01(allyShare);
            UiDraw.Place(tugAlly, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(a, 8f));
            UiDraw.Place(tugMark, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(a, 0f), new Vector2(mark, 8f));
            UiDraw.Place(tugEnemy, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(a + mark, 0f), new Vector2(width - a - mark, 8f));
        }

        // ------------------------------------------------------------ region + radar (top right)

        void BuildTopRight()
        {
            topRight = UiDraw.Place(UiDraw.Node(root, "TopRight"), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -Margin), new Vector2(240f, 188f));
            // Ping and menu are the touch controls' own buttons, placed at 40x40 in this row by
            // TouchInputManager; the region chip sits to their left.
            var chip = UiDraw.Box(topRight, "RegionChip", UiDraw.Hex("#060e1d", .8f), 4f);
            regionText = UiDraw.Text(chip.transform, "Region", "", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant, .05f);
            UiDraw.Place(UiDraw.Dot(chip.transform, "Live", 8f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(8f, 8f));
            UiDraw.Place(regionText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f, 0f), new Vector2(150f, 14f));
            SetRegion("PRACTICE");

            // Live facts sit in the narrow column beside the radar; they never cover the map
            // or the two 40 px controls above it. Offline sessions have no network latency.
            latencyText = UiDraw.Text(topRight, "Latency", "Ping —", UiFont.MonoMedium, HudCaptionDp, Primary);
            UiDraw.Place(latencyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -51f), new Vector2(92f, 15f));
            performanceText = UiDraw.Text(topRight, "Performance", "K/D 0/0", UiFont.MonoMedium, HudCaptionDp, Tertiary);
            UiDraw.Place(performanceText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -70f), new Vector2(92f, 15f));
            identityText = UiDraw.Text(topRight, "Identity", "", UiFont.BodySemiBold, HudCaptionDp, OnSurface);
            UiDraw.Place(identityText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -89f), new Vector2(92f, 30f));
            identityText.textWrappingMode = TextWrappingModes.Normal;
            identityText.overflowMode = TextOverflowModes.Ellipsis;

            var radarBox = UiDraw.Box(topRight, "Radar", UiDraw.Hex("#060e1d", .9f), 12f);
            radar = UiDraw.Place(radarBox.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(144f, 144f));
            radar.gameObject.AddComponent<RectMask2D>();
            var grid = UiDraw.Node(radar, "DotGrid").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(grid.rectTransform);
            grid.texture = DotGridTexture();
            grid.color = UIDesignTokens.PrimaryAction;
            grid.uvRect = new Rect(0f, 0f, 144f / 14f, 144f / 14f);
            grid.raycastTarget = false;
            UiDraw.Place(UiDraw.Ring(radar, "OuterRing", 112f, 1f, UiDraw.Hex("#7cefff", .2f)).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(112f, 112f));
            spinRing = UiDraw.Ring(radar, "SweepRing", 64f, 1f, UiDraw.Hex("#859396", .3f), 16).rectTransform;
            UiDraw.Place(spinRing, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(64f, 64f));
            radarMap = UiDraw.Place(UiDraw.Node(radar, "MapFootprint"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(144f, 144f));
            radarField = UiDraw.Place(UiDraw.Node(radar, "Field"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(144f, 144f));
            radarObjective = UiDraw.Place(UiDraw.Node(radarField, "Objective"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(20f, 20f));
            UiDraw.Fill(UiDraw.Dot(radarObjective, "Halo", 20f, UiDraw.Hex("#ffda88", .2f)).rectTransform);
            UiDraw.Place(UiDraw.Dot(radarObjective, "Core", 14f, Tertiary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(14f, 14f));
            UiDraw.Place(UiDraw.Dot(radarObjective, "Pip", 6f, Lowest).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(6f, 6f));
            for (int i = 0; i < 2; i++)
            {
                var mate = UiDraw.Dot(radarField, "Mate" + i, 8f, Primary).rectTransform;
                mate.anchorMin = mate.anchorMax = new Vector2(.5f, .5f);
                radarMates.Add(mate);
            }
            for (int i = 0; i < 8; i++)
            {
                var icon = UiDraw.Box(radarField, "Ping" + i, Color.white, 1f);
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(9f, 9f);
                var triangle = new RectTransform[3];
                for (int j = 0; j < triangle.Length; j++)
                {
                    var edge = UiDraw.Box(rect, "TriangleEdge" + j, Color.white, .5f).rectTransform;
                    edge.anchorMin = edge.anchorMax = new Vector2(.5f, .5f);
                    edge.sizeDelta = new Vector2(3f, 3f);
                    triangle[j] = edge;
                    edge.gameObject.SetActive(false);
                }
                rect.gameObject.SetActive(false);
                radarPings.Add((rect, icon, triangle));
            }
            radarSelf = UiDraw.Place(UiDraw.Dot(radar, "Self", 12f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(12f, 12f));
            UiDraw.Fill(UiDraw.Icon(radarSelf, "Heading", "navigation", 10f, UiDraw.Hex("#00363c"), true).rectTransform);
            radarHeading = UiDraw.Text(radar, "Hdg", "HDG 000°", UiFont.MonoMedium, HudCaptionDp, OnSurfaceVariant);
            UiDraw.Place(radarHeading.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 4f), new Vector2(80f, 12f));
            radarSector = UiDraw.Text(radar, "Sector", "", UiFont.MonoMedium, HudCaptionDp, Primary, 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(radarSector.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -4f), new Vector2(100f, 12f));
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
            regionText.text = value;
            var chip = (RectTransform)regionText.transform.parent;
            float w = 22f + UiDraw.Width(regionText) + 8f;
            // Left of both touch targets, including their larger hit rectangles.
            UiDraw.Place(chip, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-112f, -20f), new Vector2(w, 22f));
        }

        void BuildSoundCaptions()
        {
            var plate = UiDraw.Box(root, "GameplaySoundCaption", UiDraw.Hex("#060e1d", .92f), 8f);
            soundCaptionRoot = UiDraw.Place(plate.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 112f), new Vector2(320f, 42f));
            soundCaptionText = UiDraw.Text(soundCaptionRoot, "Caption", "", UiFont.HeadingSemiBold, HudValueDp,
                UiDraw.Hex("#f4f7fb"), .04f, TextAlignmentOptions.Midline);
            UiDraw.Fill(soundCaptionText.rectTransform, 12f, 0f, 12f, 0f);
            soundCaptionText.alignment = TextAlignmentOptions.Midline;
            soundCaptionRoot.gameObject.SetActive(false);
        }

        void OnEnable() => Hotdrop.Core.GameAudio.OnCriticalCaption += ShowSoundCaption;
        void OnDisable() => Hotdrop.Core.GameAudio.OnCriticalCaption -= ShowSoundCaption;

        void ShowSoundCaption(string caption)
        {
            if (!liveMatch || PlayerPrefs.GetInt(Hotdrop.Core.GameAudio.SoundCaptionsKey, 1) != 1 || soundCaptionRoot == null) return;
            soundCaptionText.text = caption;
            soundCaptionRemaining = 1.6f;
            soundCaptionRoot.gameObject.SetActive(true);
        }

        public void SetLatency(int milliseconds)
        {
            if (latencyText != null) latencyText.text = milliseconds > 0 ? $"Ping {milliseconds}ms" : "Ping —";
        }

        // ------------------------------------------------------------ armor (bottom centre)

        void BuildArmor()
        {
            var pill = UiDraw.Box(root, "ArmorPill", UiDraw.Hex("#060e1d", .9f), 12f);
            armor = UiDraw.Place(pill.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 36f), new Vector2(292f, 62f));
            UiDraw.Place(UiDraw.Icon(armor, "Heart", "favorite", 15f, Primary).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(12f, -13f), new Vector2(15f, 15f));
            UiDraw.Place(UiDraw.Text(armor, "ArmorLabel", "SUIT INTEGRITY", UiFont.Mono, HudCaptionDp, Primary, .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(33f, -13f), new Vector2(160f, 14f));
            armorValue = UiDraw.Text(armor, "ArmorValue", "100 / 100", UiFont.Mono, HudValueDp, Primary, .03f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(armorValue.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -13f), new Vector2(110f, 18f));
            var track = UiDraw.Box(armor, "Segments", Highest, 2f);
            var t = UiDraw.Place(track.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -26f), new Vector2(268f, 12f));
            float seg = (268f - 4 * 2f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                var s = UiDraw.Box(t, "Segment" + i, Primary, 2f);
                UiDraw.Place(s.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(i * (seg + 2f), 0f), new Vector2(seg, 12f));
                armorSegments.Add(s);
            }
            armorZone = UiDraw.Text(armor, "ArmorZone", "", UiFont.MonoMedium, HudCaptionDp, Outline, .05f);
            UiDraw.Place(armorZone.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(12f, 11f), new Vector2(140f, 14f));
            UiDraw.Place(UiDraw.Icon(armor, "Air", "air", 13f, Primary).rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-150f, 11f), new Vector2(13f, 13f));
            armorPad = UiDraw.Text(armor, "ArmorPad", "", UiFont.MonoMedium, HudCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(armorPad.rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-12f, 11f), new Vector2(134f, 14f));
        }

        // ------------------------------------------------------------ pushed state

        /// <summary>Training has no score, clock, roster or rotation; a match shows them all.</summary>
        public void SetLiveMatch(bool live)
        {
            liveMatch = live;
            if (!live && soundCaptionRoot != null) soundCaptionRoot.gameObject.SetActive(false);
            if (live) BuildMapFootprint(MatchView.Current?.MapConfig);
            if (rosterFade != null) rosterFade.alpha = 1f;
            if (topRightFade != null) topRightFade.alpha = 1f;
            roster.gameObject.SetActive(live);
            board.gameObject.SetActive(live);
            zonePanel.gameObject.SetActive(live);
            radar.gameObject.SetActive(live);
            regionText.transform.parent.gameObject.SetActive(live);
            latencyText.gameObject.SetActive(live);
            performanceText.gameObject.SetActive(live);
            identityText.gameObject.SetActive(live);
            if (!live) armorZone.text = "ZONE: TRAINING";
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
            clock.text = (overtime ? "OT " : "") + $"{total / 60:00}:{total % 60:00}";
        }

        public void SetZone(string zone, string status, Color statusColor)
        {
            zoneName.text = zone + " RELAY";
            zoneState.text = status;
            zoneState.color = statusColor;
            armorZone.text = "ZONE: " + zone;
            LayoutZoneRow();
        }

        /// <summary>The row is justify-between: name left, state centred in the gap, next right.
        /// The panel grows past its 280 px minimum rather than letting them collide.</summary>
        void LayoutZoneRow()
        {
            float left = 29f + UiDraw.Width(zoneName), state = UiDraw.Width(zoneState), next = UiDraw.Width(zoneNext);
            float width = Mathf.Max(280f, left + 16f + state + 16f + (next > 0f ? next + 12f : 12f));
            float rightEdge = width - (next > 0f ? next + 12f + 16f : 12f);
            UiDraw.Place(zonePanel, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -Margin - 56f), new Vector2(width, 38f));
            zoneName.rectTransform.sizeDelta = new Vector2(left - 29f + 2f, 14f);
            zoneNext.rectTransform.sizeDelta = new Vector2(next + 2f, 14f);
            UiDraw.Place(zoneState.rectTransform, new Vector2(0f, 1f), new Vector2(.5f, .5f), new Vector2((left + rightEdge) / 2f, -13f), new Vector2(state + 2f, 14f));
            ((RectTransform)zonePanel.Find("Tug")).sizeDelta = new Vector2(width - 24f, 8f);
            tugWidth = width - 24f;
            SetTug(ally + rival > 0 ? (float)ally / (ally + rival) : .5f);
        }

        /// <summary>Upcoming rotation as "NEXT: W-08s"; empty when there is none.</summary>
        public void SetNext(string zone, int seconds)
        {
            zoneNext.text = string.IsNullOrEmpty(zone) ? "" : $"NEXT: {zone.Substring(0, 1)}-{seconds:00}s";
            LayoutZoneRow();
        }

        public void SetHealth(float normalized)
        {
            int hp = Mathf.CeilToInt(Mathf.Clamp01(normalized) * 100f);
            armorValue.text = $"{hp} / 100";
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
                soundCaptionRemaining -= Time.unscaledDeltaTime;
                if (soundCaptionRemaining <= 0f && soundCaptionRoot != null) soundCaptionRoot.gameObject.SetActive(false);
            }
            if (spinRing != null) spinRing.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 20f);
            ApplyPhysicalScale(Touch != null ? Touch.ControlScale : 1f);
            if (Touch != null)
            {
                armorPad.text = Touch.LaunchAvailable ? "LAUNCH READY" : !Touch.PadAvailable ? "PAD RECHARGING" : "PAD READY";
                // The glyph leads its caption by 4 px, as the reference's inline icon does.
                var air = (RectTransform)armor.Find("Air");
                if (air != null) air.anchoredPosition = new Vector2(-12f - UiDraw.Width(armorPad) - 4f, air.anchoredPosition.y);
            }
            var view = MatchView.Current;
            if (!liveMatch || view == null || Local == null) return;
            if (Local is Object unityLocal && unityLocal == null) return;
            performanceText.text = $"K/D {Local.Eliminations}/{Local.Deaths}";
            identityText.text = Local.DisplayName;
            UpdateRoster(view);
            UpdateRadar(view);
            UpdateCombatFade(view);
        }

        void UpdateCombatFade(IMatchView view)
        {
            bool combatNearby = Touch != null && Touch.FireHeld;
            if (!combatNearby)
            {
                for (int i = 0; i < view.Participants.Count; i++)
                {
                    var player = view.Participants[i];
                    if (player == null || !player.IsAlive || player.TeamId == Local.TeamId) continue;
                    if (Vector3.Distance(Local.Position, player.Position) <= 30f)
                    {
                        combatNearby = true;
                        break;
                    }
                }
            }

            float target = PlayerPrefs.GetInt(Hotdrop.Core.OfflinePauseMenu.CockpitModeKey, 1) == 1 && combatNearby
                ? 0.15f : 1f;
            float step = 0.85f * Time.unscaledDeltaTime / 0.2f;
            if (rosterFade != null) rosterFade.alpha = Mathf.MoveTowards(rosterFade.alpha, target, step);
            if (topRightFade != null) topRightFade.alpha = Mathf.MoveTowards(topRightFade.alpha, target, step);
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
                card.hp.text = p.IsAlive ? hp + " HP" : "DOWN";
                card.hp.color = !p.IsAlive || p.HealthFraction < .25f ? Coral : p.HealthFraction < .7f ? TertiaryDim : Primary;
                card.fill.rectTransform.sizeDelta = new Vector2(240f * (p.IsAlive ? p.HealthFraction : 0f), 0f);
                card.fill.color = p.HealthFraction >= .99f ? Primary : PrimaryContainer;
                card.weapon.text = p is PlayerController pc && pc.CurrentWeapon != null ? pc.CurrentWeapon.displayName.ToUpperInvariant() : "";
                float toZone = Vector3.Distance(p.Position, view.ActiveObjectivePosition);
                if (!p.IsAlive) { card.state.text = $"RESPAWN {Mathf.CeilToInt(p.RespawnRemaining)}s"; card.state.color = Coral; }
                else if (toZone < 6f) { card.state.text = "ON OBJECTIVE"; card.state.color = Tertiary; }
                else { card.state.text = $"{toZone:0}M OUT"; card.state.color = Outline; }
            }
            for (int i = slot; i < cards.Count; i++) cards[i].rect.gameObject.SetActive(false);
            rosterCount.text = $"{alive} / {total} ACTIVE";
        }

        void UpdateRadar(IMatchView view)
        {
            var cam = UnityEngine.Camera.main;
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            radarHeading.text = $"HDG {Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)):000}°";
            radarSector.text = view.ScoringZone.ToString().ToUpperInvariant();
            var origin = Local.Position;
            float range = 40f; // metres from the centre to the outer ring (56 px)
            Vector2 ToRadar(Vector3 world)
            {
                var d = world - origin;
                var flat = Quaternion.Euler(0f, -yaw, 0f) * new Vector3(d.x, 0f, d.z);
                var p = new Vector2(flat.x, flat.z) / range * 56f;
                return Vector2.ClampMagnitude(p, 64f);
            }
            var map = view.MapConfig;
            if (map != null && radarMap != null)
            {
                var center = new Vector3((map.boundsMin.x + map.boundsMax.x) * .5f, 0f,
                    (map.boundsMin.y + map.boundsMax.y) * .5f);
                radarMap.anchoredPosition = ToRadar(center);
                radarMap.localRotation = Quaternion.Euler(0f, 0f, yaw);
            }
            radarObjective.anchoredPosition = ToRadar(view.ActiveObjectivePosition);
            int slot = 0;
            foreach (var p in view.Participants)
            {
                if (p == null || ReferenceEquals(p, Local) || p.TeamId != Local.TeamId || slot >= radarMates.Count) continue;
                radarMates[slot].gameObject.SetActive(p.IsAlive);
                radarMates[slot++].anchoredPosition = ToRadar(p.Position);
            }
            for (int i = slot; i < radarMates.Count; i++) radarMates[i].gameObject.SetActive(false);
            UpdateRadarPings(view, ToRadar);
        }

        void UpdateRadarPings(IMatchView view, System.Func<Vector3, Vector2> toRadar)
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
                    marker.root.localRotation = ping.kind == PingKind.EnemyHere
                        ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;
                    marker.icon.sprite = ping.kind == PingKind.GroupUp
                        ? UIRoundedSprites.RoundedRect(4f) : UIRoundedSprites.RoundedRect(1f);
                    marker.icon.type = Image.Type.Sliced;
                    Color color = ping.kind switch
                    {
                        PingKind.EnemyHere => UIDesignTokens.HazardCoral,
                        PingKind.GroupUp => UIDesignTokens.Ally,
                        PingKind.PadReady => UIDesignTokens.ObjectiveGold,
                        _ => UIDesignTokens.PrimaryAction,
                    };
                    marker.icon.color = color;
                    for (int j = 0; j < marker.triangle.Length; j++)
                    {
                        marker.triangle[j].gameObject.SetActive(ping.kind == PingKind.PadReady);
                        marker.triangle[j].GetComponent<Image>().color = color;
                        marker.triangle[j].localRotation = Quaternion.Euler(0f, 0f, j * 120f);
                        marker.triangle[j].anchoredPosition = Quaternion.Euler(0f, 0f, j * 120f) * new Vector2(0f, 2.5f);
                    }
                }
            }
            for (int i = slot; i < radarPings.Count; i++) radarPings[i].root.gameObject.SetActive(false);
        }

        void BuildMapFootprint(Hotdrop.Config.MapConfig map)
        {
            if (map == null || radarMap == null || radarMap.childCount > 0) return;
            const float pixelsPerMetre = 56f / 40f;
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
