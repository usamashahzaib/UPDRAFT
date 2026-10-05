using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Player;
using Hotdrop.UI;

namespace Hotdrop.Core
{
    /// <summary>
    /// Amendment 1 home screen: one primary Play action, one always-working offline option,
    /// the real in-game character, a single honest goal card and persistent navigation.
    /// Account, support and deletion stay reachable behind More; study tooling is unchanged.
    /// </summary>
    public class OfflineHomeScreen : MonoBehaviour
    {
        GameObject panel;
        GameBootstrap game;
        TextMeshProUGUI characterLabel, goalText, onlineStatus;
        RawImage previewImage;
        RawImage rooftopBackdrop;
        RectTransform homeStrip;
        RectTransform goalBar;
        TextMeshProUGUI goalProgressLabel;
        HomeCharacterPreview preview;
        GameObject morePanel;
        GameObject rigChoicePanel;
        HomeCharacterPreview femaleChoicePreview, maleChoicePreview;
        BodyRig selectedRig;
        Button femaleChoice, maleChoice;
        RectTransform femaleCard, maleCard;

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (rigChoicePanel != null) rigChoicePanel.SetActive(false);
            DisposeChoicePreviews();
            DisposePreview();
        }

        void DisposePreview()
        {
            if (preview != null) { preview.Dispose(); preview = null; }
        }

        public void Show(GameBootstrap game)
        {
            this.game = game;
            if (rigChoicePanel != null) rigChoicePanel.SetActive(false);
            DisposeChoicePreviews();
            if (panel != null) { panel.SetActive(true); FitBackdrop(); RefreshPreview(); Refresh(); return; }
            // QA-019/020: density-aware menu layout — every control stays above the 48 dp floor.
            var screen = Hotdrop.UI.MenuLayout.Create(game.matchHUD.transform, "Home", "HOTDROP", null);
            panel = screen.Root;
            BuildHomeStage(screen);
            RefreshPreview();
            Refresh();
        }

        // Screen 03 is rebuilt from its UIUX HTML; numbers are the reference's CSS px.
        RectTransform chipRow;
        TextMeshProUGUI aliasText, levelText, levelDot, sealsCount, netText, goalEyebrowChip, goalDesc, goalProgressName,
            rewardEyebrow, rewardText, fairPlayNote;
        Image netDot, goalFill;
        RectTransform aliasChip, sealsChip, netChip;

        void BuildHomeStage(MenuScreen screen)
        {
            panel.transform.Find("Nav_Bar").gameObject.SetActive(false);
            panel.transform.Find("Nav_Scroll").gameObject.SetActive(false);
            panel.transform.Find("StatusFill").gameObject.SetActive(false);
            screen.Status.gameObject.SetActive(false);
            var root = (RectTransform)panel.transform;
            var ink = UiDraw.Hex("#101827");
            Color Ink(float a) => new Color(ink.r, ink.g, ink.b, a);

            // Venue photo at 90% with the reference's three read-shades.
            var layer = UiDraw.Node(root, "HomeBackdrop");
            UiDraw.Fill(layer);
            layer.SetAsFirstSibling();
            UiDraw.Fill(UiDraw.Box(layer, "Base", UiDraw.Hex("#0b1322")).rectTransform);
            var rooftop = Resources.Load<Texture2D>("HomeRooftop");
            if (rooftop != null)
            {
                rooftopBackdrop = UiDraw.Node(layer, "HomeRooftopBackdrop").gameObject.AddComponent<RawImage>();
                UiDraw.Fill(rooftopBackdrop.rectTransform);
                rooftopBackdrop.texture = rooftop;
                rooftopBackdrop.color = new Color(.9f, .9f, .9f, 1f);
                rooftopBackdrop.raycastTarget = false;
                FitBackdrop();
            }
            UiDraw.VerticalGradient(layer, "ShadeVertical", ink, Ink(0f), Ink(.75f));
            var leftShade = UiDraw.HorizontalGradient(layer, "ShadeLeft", Ink(.95f), Ink(.6f), Ink(0f)).rectTransform;
            leftShade.anchorMax = new Vector2(.42f, 1f);
            var rightShade = UiDraw.HorizontalGradient(layer, "ShadeRight", Ink(0f), Ink(.6f), Ink(.95f)).rectTransform;
            rightShade.anchorMin = new Vector2(.62f, 0f);

            // The real equipped runner stands where the reference's athlete stands.
            previewImage = UiDraw.Node(root, "RunnerPreview").gameObject.AddComponent<RawImage>();
            UiDraw.Place(previewImage.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(2f, -380f), new Vector2(184f, 230f));
            previewImage.raycastTarget = false;
            characterLabel = UiDraw.Text(root, "RunnerLabel", "", UiFont.Body, 12f, Color.white);
            characterLabel.gameObject.SetActive(false);

            BuildHomeHeader(root);

            // Tagline plate under the header.
            var tagline = UiDraw.Box(root, "Home_Tagline", Ink(.75f), 16f, UiDraw.Hex("#49627b", .4f));
            var tag1 = UiDraw.Text(tagline.transform, "Tagline", "THREE PLAYERS. ONE TEAM. THREE MINUTES.", UiFont.HeadingMedium, 12f,
                UiDraw.Hex("#bbc6d8"), .05f, TextAlignmentOptions.Midline);
            var tag2 = UiDraw.Text(tagline.transform, "TaglineSub", "Build shortcuts your enemies can steal.", UiFont.Body, 11f,
                UiDraw.Hex("#859396"), .025f, TextAlignmentOptions.Midline);
            float tagWidth = Mathf.Max(UiDraw.Width(tag1), UiDraw.Width(tag2)) + 40f + 2f;
            UiDraw.Place(tagline.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -73f), new Vector2(tagWidth, 47f));
            UiDraw.Place(tag1.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -15f), new Vector2(tagWidth, 16f));
            UiDraw.Place(tag2.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -32.5f), new Vector2(tagWidth, 16f));

            BuildPlayCards(root);
            BuildGoalCard(root, Ink);
            BuildHomeFooter(root, Ink);
            onlineStatus = fairPlayNote;
        }

        void BuildHomeHeader(RectTransform root)
        {
            var bar = UiDraw.Box(root, "Home_Header", UiDraw.Hex("#101827", .8f));
            var rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 65f); rect.anchoredPosition = Vector2.zero;
            var rule = UiDraw.Box(rect, "Rule", UiDraw.Hex("#49627b", .3f)).rectTransform;
            rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1f, 0f); rule.pivot = new Vector2(.5f, 0f);
            rule.sizeDelta = new Vector2(0f, 1f); rule.anchoredPosition = Vector2.zero;

            var logo = UiDraw.Art(rect, "HomeLogo", "home_logo", Vector2.zero);
            UiDraw.Place(logo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(32f, -34f), new Vector2(160f, 32f));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Facilitator-only study controls: five taps on the logo. Never a player-facing route.
            logo.raycastTarget = true;
            var titleTap = logo.gameObject.AddComponent<Button>();
            titleTap.transition = Selectable.Transition.None;
            titleTap.onClick.AddListener(() =>
            {
                if (Time.unscaledTime - lastTitleTap > 1.5f) titleTaps = 0;
                lastTitleTap = Time.unscaledTime;
                if (++titleTaps >= 5) { titleTaps = 0; ToggleStudyPanel(); }
            });
#endif
            chipRow = UiDraw.Node(rect, "Home_Strip");
            UiDraw.Place(chipRow, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-32f, -34f), new Vector2(900f, 44f));
            homeStrip = chipRow;

            var chipFill = UiDraw.Hex("#1b2940", .9f);
            aliasChip = UiDraw.Box(chipRow, "IdentityChip", chipFill, 12f, UiDraw.Hex("#49627b", .4f)).rectTransform;
            aliasText = UiDraw.Text(aliasChip, "Identity", "GUEST", UiFont.Heading, 14f, Color.white, .025f);
            levelDot = UiDraw.Text(aliasChip, "Dot", "•", UiFont.Body, 14f, UiDraw.Hex("#49627b"));
            levelText = UiDraw.Text(aliasChip, "Level", "", UiFont.MonoMedium, 12f, UiDraw.Hex("#30d5e8"), .05f);

            sealsChip = UiDraw.Box(chipRow, "SealsChip", chipFill, 12f, UiDraw.Hex("#ffd166", .4f)).rectTransform;
            UiDraw.Icon(sealsChip, "SealIcon", "military_tech", 20f, UiDraw.Hex("#ffd166"));
            sealsCount = UiDraw.Text(sealsChip, "Seals", "", UiFont.Mono, 14f, UiDraw.Hex("#ffd166"));
            UiDraw.Text(sealsChip, "SealsCaption", "Flight Seals", UiFont.BodySemiBold, 12f, UiDraw.Hex("#bbc6d8"));

            netChip = UiDraw.Box(chipRow, "NetworkChip", chipFill, 12f, UiDraw.Hex("#49627b", .4f)).rectTransform;
            netDot = UiDraw.Box(netChip, "NetDot", UiDraw.Hex("#30d5e8"), 4f);
            netText = UiDraw.Text(netChip, "Region", "", UiFont.MonoRegular, 12f, UiDraw.Hex("#bbc6d8"));

            var settings = UiDraw.Tap(chipRow, "SettingsButton", chipFill, 12f,
                () => { GameAudio.Play2D(GameCue.UiTap); game.GetComponent<OfflinePauseMenu>().OpenSettings(); }, UiDraw.Hex("#49627b", .5f));
            UiDraw.Place((RectTransform)settings.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), Vector2.zero, new Vector2(44f, 44f));
            UiDraw.Fill(UiDraw.Icon(settings.transform, "Icon", "settings", 20f, UiDraw.Hex("#bbc6d8")).rectTransform);
            LayoutChips();
        }

        /// <summary>Right-aligned chip row, 12 px apart, each chip as wide as its contents.</summary>
        void LayoutChips()
        {
            if (chipRow == null) return;
            float x = -44f - 12f;
            float Inline(RectTransform chip, float padLeft, float padRight, params (Component part, float width, float gapAfter)[] parts)
            {
                float width = padLeft;
                foreach (var p in parts) width += p.width + p.gapAfter;
                width += padRight;
                float cursor = padLeft;
                foreach (var p in parts)
                {
                    var rt = (RectTransform)p.part.transform;
                    // Dots and icons are square; text runs get one 16 px line box.
                    bool square = p.part is Image || (p.part is TextMeshProUGUI t && t.font == UIFonts.Get(UiFont.Icon));
                    var size = square ? new Vector2(p.width, p.width) : new Vector2(p.width + 1f, 16f);
                    UiDraw.Place(rt, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(cursor, 0f), size);
                    cursor += p.width + p.gapAfter;
                }
                UiDraw.Place(chip, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(x, 0f), new Vector2(width, 44f));
                x -= width + 12f;
                return width;
            }
            netChip.gameObject.SetActive(true);
            Inline(netChip, 12f, 12f, (netDot, 8f, 8f), (netText, UiDraw.Width(netText), 0f));
            bool hasSeals = !string.IsNullOrEmpty(sealsCount.text);
            sealsChip.gameObject.SetActive(hasSeals);
            if (hasSeals)
            {
                var caption = sealsChip.Find("SealsCaption").GetComponent<TextMeshProUGUI>();
                Inline(sealsChip, 14f, 14f, (sealsChip.Find("SealIcon").GetComponent<TextMeshProUGUI>(), 20f, 8f),
                    (sealsCount, UiDraw.Width(sealsCount), 4f), (caption, UiDraw.Width(caption), 0f));
            }
            bool hasLevel = !string.IsNullOrEmpty(levelText.text);
            levelDot.gameObject.SetActive(hasLevel);
            levelText.gameObject.SetActive(hasLevel);
            if (hasLevel)
                Inline(aliasChip, 14f, 14f, (aliasText, UiDraw.Width(aliasText), 8f), (levelDot, UiDraw.Width(levelDot), 8f),
                    (levelText, UiDraw.Width(levelText), 0f));
            else Inline(aliasChip, 14f, 14f, (aliasText, UiDraw.Width(aliasText), 0f));
        }

        void BuildPlayCards(RectTransform root)
        {
            // Left action cluster: 400 wide, 40 from the edge, centred on the stage (y 378.5).
            var cluster = UiDraw.Place(UiDraw.Node(root, "Home_Actions"), new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(40f, -378.5f), new Vector2(400f, 260f));

            var play = UiDraw.Art(cluster, "PLAY 3-MINUTE RELAY", "home_play_card", Vector2.zero);
            play.raycastTarget = true;
            UiDraw.Place(play.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(400f, 176f));
            HomeButton(play.rectTransform, PlayRelay);
            var p = play.rectTransform;
            var chipLabel = UiDraw.Text(p, "Mode", "RELAY", UiFont.Mono, 12f, UiDraw.Hex("#30d5e8"), .1f);
            float chipW = 10f + 6f + 8f + UiDraw.Width(chipLabel) + 10f + 2f;
            var chip = UiDraw.Box(p, "ModeChip", UiDraw.Hex("#30d5e8", .2f), 4f, UiDraw.Hex("#30d5e8", .4f));
            UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -32f), new Vector2(chipW, 22f));
            UiDraw.Place(UiDraw.Box(chip.transform, "Ping", UiDraw.Hex("#30d5e8"), 3f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(11f, 0f), new Vector2(6f, 6f));
            chipLabel.transform.SetParent(chip.transform, false);
            UiDraw.Place(chipLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(25f, 0f), new Vector2(UiDraw.Width(chipLabel) + 2f, 16f));
            var ready = UiDraw.Text(p, "Ready", "SOLO • READY", UiFont.Heading, 12f, Color.white, .05f, TextAlignmentOptions.Midline);
            float readyW = UiDraw.Width(ready) + 20f + 2f;
            var pill = UiDraw.Box(p, "ReadyPill", UiDraw.Hex("#101827", .8f), 11f, UiDraw.Hex("#49627b", .5f));
            UiDraw.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -32f), new Vector2(readyW, 22f));
            ready.transform.SetParent(pill.transform, false);
            UiDraw.Fill(ready.rectTransform);
            UiDraw.Place(UiDraw.Text(p, "PlayTitle", "PLAY 3-MINUTE RELAY", UiFont.Heading, 24f, Color.white, -.025f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -64.7f), new Vector2(356f, 32f));
            UiDraw.Place(UiDraw.Text(p, "PlayDetail", "3v3 • One active objective • Zone rotates every 30 sec", UiFont.Body, 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -94f), new Vector2(356f, 20f));
            UiDraw.Place(UiDraw.Box(p, "CueRule", UiDraw.Hex("#49627b", .4f)).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -116.5f), new Vector2(356f, 1f));
            UiDraw.Place(UiDraw.Box(p, "CueDot", UiDraw.Hex("#30d5e8"), 4f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -140.7f), new Vector2(8f, 8f));
            UiDraw.Place(UiDraw.Text(p, "PlayCue", "TOUCH TO START MATCH", UiFont.HeadingSemiBold, 12f, UiDraw.Hex("#bbc6d8"), .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(38f, -140.7f), new Vector2(300f, 16f));
            UiDraw.Place(UiDraw.Icon(p, "PlayArrow", "arrow_forward", 18f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -140.7f), new Vector2(18f, 18f));

            var offline = UiDraw.Box(cluster, "PLAY OFFLINE", UiDraw.Hex("#1b2940", .9f), 12f, UiDraw.Hex("#49627b", .5f));
            offline.raycastTarget = true;
            UiDraw.Place(offline.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -190f), new Vector2(400f, 70f));
            HomeButton(offline.rectTransform, PlayOffline);
            var o = offline.rectTransform;
            var offTitle = UiDraw.Text(o, "OfflineTitle", "PLAY OFFLINE", UiFont.Heading, 16f, Color.white);
            UiDraw.Place(offTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(21f, -27f), new Vector2(UiDraw.Width(offTitle) + 2f, 24f));
            var solo = UiDraw.Text(o, "SoloLabel", "SOLO", UiFont.MonoRegular, 10f, UiDraw.Hex("#bbc6d8"), 0f, TextAlignmentOptions.Midline);
            var soloBox = UiDraw.Box(o, "SoloChip", UiDraw.Hex("#101827"), 4f, UiDraw.Hex("#49627b", .3f));
            UiDraw.Place(soloBox.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(21f + UiDraw.Width(offTitle) + 8f, -27f),
                new Vector2(UiDraw.Width(solo) + 14f, 19f));
            solo.transform.SetParent(soloBox.transform, false);
            UiDraw.Fill(solo.rectTransform);
            int count = 12;
            var runtime = game != null ? game.GetComponent<OfflineChallengeRuntime>() : null;
            if (runtime?.Controller?.config?.entries != null && runtime.Controller.config.entries.Count > 0)
                count = runtime.Controller.config.entries.Count;
            UiDraw.Place(UiDraw.Text(o, "OfflineDetail", count + " Launch Challenges • Works without internet", UiFont.Body, 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(21f, -48.5f), new Vector2(320f, 16f));
            UiDraw.Place(UiDraw.Icon(o, "OfflineIcon", "sports_esports", 24f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-21f, 0f), new Vector2(24f, 24f));
        }

        void BuildGoalCard(RectTransform root, System.Func<float, Color> ink)
        {
            // Right goal card: 380 wide, 40 from the edge, centred on the stage.
            var goal = UiDraw.Box(root, "Home_Goal", UiDraw.Hex("#1b2940", .95f), 16f, UiDraw.Hex("#49627b"));
            var g = UiDraw.Place(goal.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-40f, -378.5f), new Vector2(380f, 290f));
            const float pad = 21f;
            UiDraw.Place(UiDraw.Icon(g, "GoalFlag", "flag", 16f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -32.7f), new Vector2(16f, 16f));
            UiDraw.Place(UiDraw.Text(g, "GoalEyebrow", "NEXT GOAL", UiFont.Heading, 12f, UiDraw.Hex("#30d5e8"), .1f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 24f, -32.7f), new Vector2(220f, 16f));
            goalEyebrowChip = UiDraw.Text(g, "GoalContext", "", UiFont.MonoRegular, 11f, UiDraw.Hex("#bbc6d8"), 0f, TextAlignmentOptions.Midline);
            var contextPlate = UiDraw.Box(g, "GoalContextPlate", UiDraw.Hex("#141c2b"), 4f, UiDraw.Hex("#49627b", .4f));
            contextPlate.transform.SetSiblingIndex(goalEyebrowChip.transform.GetSiblingIndex());
            UiDraw.HRule(g, "HeaderRule", UiDraw.Hex("#49627b", .4f), 54.7f, pad, pad);
            goalText = UiDraw.Text(g, "Goal", "", UiFont.Heading, 18f, Color.white, -.025f);
            UiDraw.Place(goalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -82.7f), new Vector2(338f, 28f));
            goalDesc = UiDraw.Text(g, "GoalDetail", "", UiFont.Body, 12f, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(goalDesc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -106.7f), new Vector2(338f, 16f));
            goalProgressName = UiDraw.Text(g, "GoalProgressName", "", UiFont.MonoRegular, 12f, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(goalProgressName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -136.7f), new Vector2(200f, 16f));
            goalProgressLabel = UiDraw.Text(g, "GoalProgressLabel", "", UiFont.Mono, 12f, UiDraw.Hex("#ffd166"), 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(goalProgressLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad, -136.7f), new Vector2(200f, 16f));
            var track = UiDraw.Box(g, "GoalProgress", UiDraw.Hex("#101827"), 5f, UiDraw.Hex("#49627b", .5f));
            goalBar = UiDraw.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -156.3f), new Vector2(338f, 10f));
            goalFill = UiDraw.Box(goalBar, "Fill", UiDraw.Hex("#ffd166"), 4f);
            goalFill.rectTransform.anchorMin = new Vector2(0f, 0f); goalFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            goalFill.rectTransform.pivot = new Vector2(0f, .5f);
            goalFill.rectTransform.offsetMin = new Vector2(2f, 2f); goalFill.rectTransform.offsetMax = new Vector2(2f, -2f);

            var reward = UiDraw.Box(g, "GoalReward", UiDraw.Hex("#101827", .8f), 12f, UiDraw.Hex("#ffd166", .3f));
            UiDraw.Place(reward.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(pad, -175.3f), new Vector2(338f, 52.7f));
            var badge = UiDraw.Box(reward.transform, "RewardBadge", UiDraw.Hex("#ffd166", .2f), 8f, UiDraw.Hex("#ffd166", .4f));
            UiDraw.Place(badge.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(11f, 0f), new Vector2(32f, 32f));
            UiDraw.Fill(UiDraw.Icon(badge.transform, "Icon", "military_tech", 20f, UiDraw.Hex("#ffd166")).rectTransform);
            rewardEyebrow = UiDraw.Text(reward.transform, "RewardEyebrow", "", UiFont.Heading, 10f, UiDraw.Hex("#ffd166"), .05f);
            UiDraw.Place(rewardEyebrow.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(55f, 8f), new Vector2(270f, 14f));
            rewardText = UiDraw.Text(reward.transform, "RewardText", "", UiFont.Heading, 12f, Color.white);
            UiDraw.Place(rewardText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(55f, -8f), new Vector2(270f, 16f));

            UiDraw.HRule(g, "NoteRule", UiDraw.Hex("#49627b", .3f), 242.7f, pad, pad);
            UiDraw.Place(UiDraw.Icon(g, "ShieldIcon", "shield", 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -259f), new Vector2(12f, 12f));
            fairPlayNote = UiDraw.Text(g, "FairPlayNote", "All 3 weapons unlocked • Cosmetics never change power", UiFont.Body, 11f,
                UiDraw.Hex("#bbc6d8", .9f));
            UiDraw.Place(fairPlayNote.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 20f, -258f), new Vector2(318f, 14f));

            var goalHit = new GameObject("HomeGoalAction", typeof(RectTransform), typeof(Image), typeof(Button));
            goalHit.transform.SetParent(g, false);
            UiDraw.Fill((RectTransform)goalHit.transform);
            var goalHitImage = goalHit.GetComponent<Image>();
            goalHitImage.color = new Color(1f, 1f, 1f, 0f);
            var goalAction = goalHit.GetComponent<Button>();
            goalAction.targetGraphic = goalHitImage;
            goalAction.transition = Selectable.Transition.None;
            goalAction.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); PlayGoal(); });
        }

        void BuildHomeFooter(RectTransform root, System.Func<float, Color> ink)
        {
            var footer = UiDraw.Box(root, "Home_Footer", ink(.85f));
            var f = footer.rectTransform;
            f.anchorMin = Vector2.zero; f.anchorMax = new Vector2(1f, 0f); f.pivot = new Vector2(.5f, 0f);
            f.sizeDelta = new Vector2(0f, 83f); f.anchoredPosition = Vector2.zero;
            UiDraw.HRule(f, "Rule", UiDraw.Hex("#49627b", .3f), 0f);
            var nav = UiDraw.Place(UiDraw.Node(f, "Home_Nav"), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -9f), new Vector2(600f, 48f));
            var items = new (string name, string caption, string icon, float minWidth, Color iconColor, UnityEngine.Events.UnityAction action)[]
            {
                ("PARTY", "Party", "group", 130f, UiDraw.Hex("#30d5e8"), () => PlayerProductScreens.Open(game, panel, "Party")),
                ("APPEARANCE", "Appearance", "checkroom", 140f, UiDraw.Hex("#bbc6d8"), () => PlayerProductScreens.Open(game, panel, "Appearance")),
                ("MORE", "More", "more_horiz", 120f, UiDraw.Hex("#bbc6d8"), ToggleMore),
            };
            var widths = new float[items.Length];
            var labels = new TextMeshProUGUI[items.Length];
            float total = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                labels[i] = UiDraw.Text(nav, "Caption", items[i].caption, UiFont.Heading, 12f, Color.white, .05f);
                widths[i] = Mathf.Max(items[i].minWidth, 20f + 20f + 10f + UiDraw.Width(labels[i]) + 20f + 2f);
                total += widths[i] + (i > 0 ? 16f : 0f);
            }
            float x = -total / 2f;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var button = UiDraw.Tap(nav, item.name, UiDraw.Hex("#1b2940"), 12f, () => { GameAudio.Play2D(GameCue.UiTap); item.action(); },
                    UiDraw.Hex("#49627b", .5f));
                UiDraw.Place((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(widths[i], 48f));
                float content = 20f + 10f + UiDraw.Width(labels[i]);
                float start = (widths[i] - content) / 2f;
                UiDraw.Place(UiDraw.Icon(button.transform, "Icon", item.icon, 20f, item.iconColor).rectTransform,
                    new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(start, 0f), new Vector2(20f, 20f));
                labels[i].transform.SetParent(button.transform, false);
                UiDraw.Place(labels[i].rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(start + 30f, 0f), new Vector2(UiDraw.Width(labels[i]) + 2f, 16f));
                x += widths[i] + 16f;
            }
            var gesture = UiDraw.Box(f, "GesturePill", UiDraw.Hex("#49627b", .5f), 2f);
            UiDraw.Place(gesture.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 13f), new Vector2(144f, 4f));
        }

        static void HomeButton(RectTransform root, UnityEngine.Events.UnityAction action)
        {
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Graphic>();
            button.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); action(); });
        }

        void LateUpdate()
        {
            if (rooftopBackdrop != null && rooftopBackdrop.gameObject.activeInHierarchy) FitBackdrop();
        }

        void FitBackdrop()
        {
            if (rooftopBackdrop == null || rooftopBackdrop.texture == null) return;
            var rect = rooftopBackdrop.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            float imageAspect = (float)rooftopBackdrop.texture.width / rooftopBackdrop.texture.height;
            float viewAspect = rect.width / rect.height;
            if (viewAspect > imageAspect)
            {
                float visibleHeight = imageAspect / viewAspect;
                rooftopBackdrop.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            else
            {
                float visibleWidth = viewAspect / imageAspect;
                rooftopBackdrop.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
        }

        void PlayOnline()
        {
            string reason = OnlineHooks.UnavailableReason?.Invoke() ?? (OnlineHooks.PlayOnline == null ? "Online play is not included in this build." : null);
            if (reason != null) { onlineStatus.text = reason; return; }
            CloseForMatch();
            OnlineHooks.PlayOnline(game);
        }

        void PlayRelay()
        {
            game.StartOfflinePractice();
        }

        void PlayOffline()
        {
            game.OpenOfflineLibrary();
        }

        void CloseForMatch()
        {
            if (panel != null) panel.SetActive(false);
            DisposePreview();
            game.touchInput.enabled = true;
        }

        void Refresh()
        {
            if (characterLabel != null)
                characterLabel.text = BodyRigs.DisplayName(BodyRigStore.Current).ToUpperInvariant() + " RUNNER";
            if (goalText != null)
            {
                string line = GoalLine();
                int split = line.IndexOf('\n');
                goalText.text = split < 0 ? line : line.Substring(0, split);
                if (goalDesc != null) goalDesc.text = split < 0 ? "" : line.Substring(split + 1);
            }
            if (onlineStatus != null)
                onlineStatus.text = OnlineHooks.UnavailableReason?.Invoke() ?? "All 3 weapons unlocked • Cosmetics never change power";
            RefreshGoalProgress();
            RefreshIdentityStrip();
        }

        /// <summary>
        /// Real authored-challenge progress for the goal card. Reads only what the local save
        /// already knows, so the bar can never claim progress the player has not made.
        /// </summary>
        void RefreshGoalProgress()
        {
            if (goalBar == null) return;
            bool trained = PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) != 0;
            var runtime = game != null ? game.GetComponent<OfflineChallengeRuntime>() : null;
            var entries = runtime?.Controller?.config?.entries;
            int done = 0, total = 0;
            if (trained && entries != null && entries.Count > 0)
            {
                total = entries.Count;
                foreach (var entry in entries)
                    if (runtime.Controller.RecordForCurrentContext(entry.id)?.isCompleted == true) done++;
            }
            float fraction;
            if (total == 0)
            {
                // Training not finished, or no library yet: the route shown honestly, not "100%".
                fraction = 0f;
                SetGoalContext("TRAINING");
                goalProgressName.text = "Guided Training";
                goalProgressLabel.text = "0 / 1 <color=#bbc6d8>(0%)</color>";
                rewardEyebrow.text = "UNLOCKS";
                rewardText.text = "Launch Challenges (offline)";
            }
            else
            {
                fraction = Mathf.Clamp01((float)done / total);
                SetGoalContext("OFFLINE");
                goalProgressName.text = "Challenge Progress";
                goalProgressLabel.text = $"{done} / {total} <color=#bbc6d8>({Mathf.RoundToInt(fraction * 100f)}%)</color>";
                rewardEyebrow.text = done >= total ? "ALL CLEARED" : "NEXT UP";
                rewardText.text = done >= total ? "Beat your best times" : "Tap to start this challenge";
            }
            float width = ((RectTransform)goalBar).rect.width > 0f ? ((RectTransform)goalBar).rect.width : 338f;
            goalFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, (width - 4f) * fraction), -4f);
            goalFill.enabled = fraction > 0f;
        }

        void SetGoalContext(string value)
        {
            goalEyebrowChip.text = value;
            float w = UiDraw.Width(goalEyebrowChip) + 16f + 2f;
            var card = goalEyebrowChip.transform.parent;
            UiDraw.Place(goalEyebrowChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-21f, -32.7f), new Vector2(w, 16f));
            var plate = card.Find("GoalContextPlate") as RectTransform;
            if (plate != null) UiDraw.Place(plate, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-21f, -32.7f), new Vector2(w, 22.5f));
        }

        /// <summary>
        /// Alias, level, seals and connection come from the player's own backend profile. Offline
        /// with no session the strip stays honest ("GUEST", no seals, OFFLINE) rather than
        /// showing placeholder numbers.
        /// </summary>
        async void RefreshIdentityStrip()
        {
            if (homeStrip == null) return;
            if (BackendClient.Instance == null || !BackendClient.Instance.HasSession)
            {
                SetStrip("GUEST", "", "", "");
                return;
            }

            SetStrip("GUEST", "", "", "ONLINE");
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (profile.Ok && profile.Value != null && !string.IsNullOrEmpty(profile.Value.alias))
            {
                string level = $"Level {Mathf.Clamp(profile.Value.level, 1, 20)}";
                SetStrip(profile.Value.alias, level, "", "ONLINE");
                // Seals are permanent server-derived progress, so only ask when a goal exists.
                if (ContractsMasteryFeature.Enabled)
                {
                    var goals = await BackendClient.Instance.GoalsAsync();
                    if (goals.Ok && goals.Value != null)
                        SetStrip(profile.Value.alias, level, goals.Value.seals.ToString(), "ONLINE");
                }
            }
        }

        void SetStrip(string alias, string level, string seals, string region)
        {
            if (aliasText == null) return;
            aliasText.text = alias ?? "";
            levelText.text = level ?? "";
            sealsCount.text = seals ?? "";
            bool online = !string.IsNullOrEmpty(region);
            netText.text = online ? region : "OFFLINE";
            netDot.color = online ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#ff6376");
            LayoutChips();
        }

        string GoalLine()
        {
            // Shared with the results screen so Home and post-match always agree.
            if (PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) != 0 &&
                game.GetComponent<OfflineChallengeRuntime>() == null)
                game.gameObject.AddComponent<OfflineChallengeRuntime>();
            return NextGoal.Describe(game.gameObject);
        }

        void PlayGoal()
        {
            if (PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0)
            { Hide(); game.StartTutorial(); return; }
            game.OpenOfflineLibrary();
            var controller = game.GetComponent<OfflineChallengeRuntime>().Controller;
            foreach (var entry in controller.config.entries)
                if (controller.RecordForCurrentContext(entry.id)?.isCompleted != true)
                { controller.SelectChallenge(entry.id); return; }
            game.StartOfflinePractice();
        }

        void RefreshPreview()
        {
            if (previewImage == null) return;
            DisposePreview();
            try
            {
                preview = new HomeCharacterPreview();
                previewImage.texture = preview.Texture;
            }
            catch (System.Exception exception)
            {
                preview = null;
                previewImage.texture = null;
                Debug.LogWarning("Home character preview unavailable: " + exception.Message);
            }
        }

        void ToggleMore()
        {
            if (morePanel != null)
            {
                morePanel.SetActive(!morePanel.activeSelf);
                if (morePanel.activeSelf) morePanel.transform.SetAsLastSibling();
                return;
            }
            var screen = Hotdrop.UI.MenuLayout.Create(panel.transform, "More", "MORE", () => morePanel.SetActive(false));
            morePanel = screen.Root;
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "PROFILE", () => PlayerProductScreens.Open(game, panel, "Profile"), "PROFILE");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "ONLINE RELAY", PlayOnline, "ONLINE RELAY");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "TRAINING", () => { Hide(); game.StartTutorial(); }, "TRAINING");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "SETTINGS", () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "SETTINGS");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "ACCOUNT", () => PlayerProductScreens.Open(game, panel, "Account"), "ACCOUNT");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "SUPPORT / REPORT", () => PlayerProductScreens.Open(game, panel, "Support"), "SUPPORT");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "CONTRACTS", () => PlayerProductScreens.Open(game, panel, "Contracts"), "CONTRACTS");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "MASTERY", () => PlayerProductScreens.Open(game, panel, "Mastery"), "MASTERY");
            screen.Status.text = "Account, support and deletion stay reachable here.";
        }

        /// <summary>One body-presentation card's repaintable parts.</summary>
        sealed class RigCardView
        {
            public Image fill, border, glow, check, art;
            public TextMeshProUGUI checkIcon, title, badge, dot;
            public Image badgePlate;
            public RawImage venue, runner;
        }

        RigCardView femaleView, maleView;
        bool faceFraming;
        Image fullBodyTab, faceTab;
        TextMeshProUGUI fullBodyText, faceText, fullBodyIcon, faceIcon;

        // Screen 02 is rebuilt from its UIUX HTML; numbers are the reference's CSS px.
        const float RigSide = 32f, RigEdge = 24f;

        /// <summary>
        /// Amendment 1: first launch offers a skippable runner choice with a ready-to-play default;
        /// the same chooser is reachable later from Home and Appearance.
        /// </summary>
        public void ShowRigChoice(bool firstRun)
        {
            if (rigChoicePanel != null) { Destroy(rigChoicePanel); rigChoicePanel = null; }
            DisposeChoicePreviews();
            // First run opens this before Home.Show has assigned the bootstrap reference.
            // Bind the already-initialized singleton rather than creating a second home flow.
            if (game == null) game = GameBootstrap.Instance;
            if (game == null || game.matchHUD == null)
            {
                Debug.LogError("Cannot open runner choice before GameBootstrap HUD initialization.");
                return;
            }
            var parent = panel != null ? panel.transform : game.matchHUD.transform;
            var screen = Hotdrop.UI.MenuLayout.Create(parent, "RigChoice", firstRun ? "CHOOSE YOUR LOOK" : "CHANGE YOUR LOOK", null);
            rigChoicePanel = screen.Root;
            selectedRig = BodyRigStore.Current;
            faceFraming = false;
            screen.Root.transform.Find("Nav_Bar").gameObject.SetActive(false);
            screen.Content.parent.parent.gameObject.SetActive(false);
            screen.Root.transform.Find("StatusFill").gameObject.SetActive(false);
            screen.Status.gameObject.SetActive(false);
            var root = (RectTransform)screen.Root.transform;
            var mid = UiDraw.Hex("#0b1322");
            UiDraw.Backdrop(root, .55f, UiDraw.Hex("#060e1d"), new Color(mid.r, mid.g, mid.b, .85f), new Color(mid.r, mid.g, mid.b, .6f),
                false, 192f);

            // Header: title + starter chip, the equal-hitbox guarantee, framing toggle.
            var title = UiDraw.Text(root, "Title", firstRun ? "CHOOSE YOUR LOOK" : "CHANGE YOUR LOOK", UiFont.Heading, 24f,
                Color.white, -.025f);
            float titleWidth = UiDraw.Width(title);
            UiDraw.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide, -RigEdge - 16f), new Vector2(titleWidth + 4f, 32f));
            if (firstRun)
            {
                // The chip marks these as the provided starter looks, which is only true on first run.
                var chipText = UiDraw.Text(root, "StarterOutfitLabel", "STARTER OUTFIT", UiFont.Mono, 11f, UiDraw.Hex("#30d5e8"), .05f,
                    TextAlignmentOptions.Midline);
                float chipWidth = UiDraw.Width(chipText) + 22f;
                var chip = UiDraw.Box(root, "StarterOutfitChip", UiDraw.Hex("#1b2940"), 4f, UiDraw.Hex("#49627b", .4f));
                UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide + titleWidth + 12f, -RigEdge - 16f), new Vector2(chipWidth, 22.5f));
                chipText.transform.SetParent(chip.transform, false);
                UiDraw.Fill(chipText.rectTransform);
            }
            UiDraw.Place(UiDraw.Icon(root, "VerifiedIcon", "verified", 14f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide, -RigEdge - 42f), new Vector2(14f, 14f));
            var note = UiDraw.Text(root, "RigNote", "Appearance only. Same movement, abilities, animations, and hitbox.", UiFont.Body, 12f,
                UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide + 20f, -RigEdge - 42f), new Vector2(560f, 16f));
            BuildFramingToggle(root);

            // Two equal cards, 440 x 480, 40 apart, centred between header and footer.
            // Presentation 01 is the ready-to-play default, so it sits first and starts selected.
            var second = BodyRigs.Default == BodyRig.Male ? BodyRig.Female : BodyRig.Male;
            AddRigOption(root, BodyRigs.Default, new Vector2(-240f, 5f), "BODY PRESENTATION 01");
            AddRigOption(root, second, new Vector2(240f, 5f), "BODY PRESENTATION 02");

            // Footer: reassurance note left, Decide Later + Continue right.
            var footer = UiDraw.Box(root, "RigFooter", UiDraw.Hex("#060e1d", .7f));
            var footerRect = footer.rectTransform;
            footerRect.anchorMin = Vector2.zero; footerRect.anchorMax = new Vector2(1f, 0f); footerRect.pivot = new Vector2(.5f, 0f);
            footerRect.sizeDelta = new Vector2(0f, 84f); footerRect.anchoredPosition = Vector2.zero;
            UiDraw.HRule(footerRect, "TopRule", UiDraw.Hex("#49627b", .3f), 0f);
            UiDraw.Place(UiDraw.Icon(footerRect, "ShuffleIcon", "shuffle", 16f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(RigSide, RigEdge + 24f), new Vector2(16f, 16f));
            var reassure = UiDraw.Text(footerRect, "Reassurance", firstRun
                    ? "You can freely change your appearance anytime from <color=#f4f7fb><b>Appearance</b></color>."
                    : "Applies to your next spawn in offline play.",
                UiFont.MonoRegular, 12f, UiDraw.Hex("#7f8ca3"));
            UiDraw.Place(reassure.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(RigSide + 24f, RigEdge + 24f), new Vector2(640f, 16f));

            var continueText = UiDraw.Text(footerRect, "Caption", firstRun ? "CONTINUE" : "SAVE RUNNER", UiFont.Heading, 14f,
                UiDraw.Hex("#060e1d"), .05f, TextAlignmentOptions.MidlineLeft);
            float continueWidth = 40f + UiDraw.Width(continueText) + 8f + 16f + 40f;
            var continueButton = UiDraw.Tap(footerRect, "RigContinue", UiDraw.Hex("#30d5e8"), 12f, () => ChooseRig(selectedRig, firstRun));
            UiDraw.Place((RectTransform)continueButton.transform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-RigSide, RigEdge + 24f), new Vector2(continueWidth, 48f));
            UiDraw.Glow((RectTransform)continueButton.transform, UiDraw.Hex("#30d5e8", .4f), 12f, 20f);
            continueText.transform.SetParent(continueButton.transform, false);
            UiDraw.Place(continueText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(UiDraw.Width(continueText) + 2f, 20f));
            UiDraw.Place(UiDraw.Icon(continueButton.transform, "Arrow", "arrow_forward", 16f, UiDraw.Hex("#060e1d")).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-40f, 0f), new Vector2(16f, 16f));

            System.Action later = firstRun
                ? (System.Action)(() => ChooseRig(BodyRigs.Default, true))
                : () => { rigChoicePanel.SetActive(false); DisposeChoicePreviews(); };
            var laterText = UiDraw.Text(footerRect, "Caption", firstRun ? "DECIDE LATER" : "BACK", UiFont.Heading, 12f,
                UiDraw.Hex("#bbc6d8"), .05f, TextAlignmentOptions.Midline);
            float laterWidth = 24f + UiDraw.Width(laterText) + 24f;
            var laterButton = UiDraw.Tap(footerRect, firstRun ? "DECIDE LATER" : "BACK", new Color(0f, 0f, 0f, 0f), 12f, later);
            UiDraw.Place((RectTransform)laterButton.transform, new Vector2(1f, 0f), new Vector2(1f, .5f),
                new Vector2(-RigSide - continueWidth - 16f, RigEdge + 24f), new Vector2(laterWidth, 48f));
            laterText.transform.SetParent(laterButton.transform, false);
            UiDraw.Fill(laterText.rectTransform);
            RefreshRigSelection();
        }

        void BuildFramingToggle(RectTransform root)
        {
            var toggle = UiDraw.Box(root, "FramingToggle", UiDraw.Hex("#1b2940", .9f), 12f, UiDraw.Hex("#49627b", .5f));
            var rect = toggle.rectTransform;
            float x = 5f;
            Image Tab(string name, string icon, string caption, bool face, out TextMeshProUGUI text, out TextMeshProUGUI glyph)
            {
                var label = UiDraw.Text(rect, "Caption", caption, UiFont.Mono, 12f, Color.white);
                float width = 14f + 16f + 6f + UiDraw.Width(label) + 14f;
                var tab = UiDraw.Tap(rect, name, new Color(0f, 0f, 0f, 0f), 8f, () => SetFraming(face));
                UiDraw.Place((RectTransform)tab.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(width, 40f));
                glyph = UiDraw.Icon(tab.transform, "Icon", icon, 16f, Color.white);
                UiDraw.Place(glyph.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(16f, 16f));
                label.transform.SetParent(tab.transform, false);
                UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(UiDraw.Width(label) + 2f, 16f));
                text = label;
                x += width + 8f;
                return tab.GetComponent<Image>();
            }
            fullBodyTab = Tab("FramingFullBody", "accessibility_new", "FULL BODY", false, out fullBodyText, out fullBodyIcon);
            faceTab = Tab("FramingFace", "face", "FACE", true, out faceText, out faceIcon);
            var divider = UiDraw.Box(rect, "Divider", UiDraw.Hex("#49627b", .4f));
            UiDraw.Place(divider.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x + 4f, 0f), new Vector2(1f, 24f));
            x += 9f + 8f;
            // The 360 control turns both runners a quarter, the same rotation a drag gives.
            var spin = UiDraw.Tap(rect, "RotateRunners", new Color(0f, 0f, 0f, 0f), 8f, () =>
            {
                femaleChoicePreview?.Rotate(90f);
                maleChoicePreview?.Rotate(90f);
            });
            UiDraw.Place((RectTransform)spin.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(spin.transform, "Icon", "360", 18f, UiDraw.Hex("#bbc6d8")).rectTransform);
            x += 40f + 5f;
            UiDraw.Place(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RigSide, -RigEdge), new Vector2(x, 50f));
            RefreshFramingToggle();
        }

        void SetFraming(bool face)
        {
            faceFraming = face;
            femaleChoicePreview?.SetFraming(face);
            maleChoicePreview?.SetFraming(face);
            RefreshFramingToggle();
        }

        void RefreshFramingToggle()
        {
            if (fullBodyTab == null) return;
            var on = UiDraw.Hex("#263a59");
            var off = new Color(0f, 0f, 0f, 0f);
            fullBodyTab.color = faceFraming ? off : on;
            faceTab.color = faceFraming ? on : off;
            fullBodyText.color = faceFraming ? UiDraw.Hex("#7f8ca3") : Color.white;
            faceText.color = faceFraming ? Color.white : UiDraw.Hex("#7f8ca3");
            fullBodyIcon.color = faceFraming ? UiDraw.Hex("#7f8ca3") : UiDraw.Hex("#30d5e8");
            faceIcon.color = faceFraming ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#7f8ca3");
        }

        void AddRigOption(RectTransform parent, BodyRig rig, Vector2 centre, string caption)
        {
            const float pad = 21f;
            var view = new RigCardView();
            var hit = new GameObject("RigCard_" + BodyRigs.Id(rig), typeof(RectTransform));
            hit.transform.SetParent(parent, false);
            var card = UiDraw.Place((RectTransform)hit.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), centre, new Vector2(440f, 480f));
            if (rig == BodyRig.Female) femaleCard = card; else maleCard = card;
            view.glow = UiDraw.Glow(card, UiDraw.Hex("#30d5e8", .25f), 16f, 30f);
            view.fill = UiDraw.Box(card, "Fill", UiDraw.Hex("#101827", .85f), 16f);
            UiDraw.Fill(view.fill.rectTransform);
            view.border = UiDraw.Box(card, "Frame", Color.white);
            UiDraw.Fill(view.border.rectTransform);
            view.border.type = Image.Type.Sliced;

            // Header row.
            view.check = UiDraw.Box(card, "Check", UiDraw.Hex("#30d5e8"), 12f);
            UiDraw.Place(view.check.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -pad - 12f), new Vector2(24f, 24f));
            view.checkIcon = UiDraw.Icon(view.check.transform, "Icon", "check", 14f, UiDraw.Hex("#060e1d"));
            UiDraw.Fill(view.checkIcon.rectTransform);
            view.title = UiDraw.Text(card, "CardTitle", caption,
                UiFont.Heading, 14f, Color.white, .025f);
            UiDraw.Place(view.title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 32f, -pad - 12f), new Vector2(260f, 20f));
            view.badgePlate = UiDraw.Box(card, "SelectionBadgePlate", UiDraw.Hex("#30d5e8", .1f), 4f, UiDraw.Hex("#30d5e8", .3f));
            view.badge = UiDraw.Text(card, "SelectionBadge", "", UiFont.Mono, 11f, UiDraw.Hex("#30d5e8"), 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(view.badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad - 8f, -pad - 12f), new Vector2(160f, 16f));

            // Art: the venue shot with the live runner standing in it, centred in the art area.
            float artTop = pad + 24f + 8f, artBottom = pad + 16f + 12f + 1f + 8f;
            var area = UiDraw.Node(card, "ArtArea");
            UiDraw.Fill(area, pad, artTop, pad, artBottom);
            view.venue = UiDraw.Node(area, "Rooftop").gameObject.AddComponent<RawImage>();
            UiDraw.Place(view.venue.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(398f, 224f));
            view.venue.texture = Resources.Load<Texture2D>("HomeRooftop");
            view.venue.uvRect = caption.EndsWith("01") ? new Rect(0f, 0f, 1f, 1f) : new Rect(1f, 0f, -1f, 1f);
            view.venue.raycastTarget = false;
            view.runner = UiDraw.Node(area, "RunnerPreview").gameObject.AddComponent<RawImage>();
            // The runner stands on the rooftop inside the shot, at the scale the reference art shows.
            UiDraw.Place(view.runner.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(caption.EndsWith("01") ? -8f : 8f, 0f), new Vector2(104f, 130f));
            view.runner.raycastTarget = false;
            var rigPreview = new HomeCharacterPreview(rig: rig);
            view.runner.texture = rigPreview.Texture;
            if (rig == BodyRig.Female) femaleChoicePreview = rigPreview; else maleChoicePreview = rigPreview;
            var hint = UiDraw.Box(area, "RotateHint", UiDraw.Hex("#060e1d", .6f), 13f, UiDraw.Hex("#49627b", .3f));
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f); hintRect.anchorMax = new Vector2(1f, 0f); hintRect.pivot = new Vector2(.5f, 0f);
            hintRect.offsetMin = new Vector2(32f, 8f); hintRect.offsetMax = new Vector2(-32f, 8f + 26.5f);
            var hintText = UiDraw.Text(hint.transform, "Caption", "DRAG TO ROTATE", UiFont.MonoRegular, 11f, UiDraw.Hex("#7f8ca3"), 0f,
                TextAlignmentOptions.MidlineLeft);
            float hintWidth = UiDraw.Width(hintText);
            UiDraw.Place(hintText.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(-(hintWidth + 16f) / 2f + 16f, 0f), new Vector2(hintWidth + 2f, 16f));
            UiDraw.Place(UiDraw.Icon(hint.transform, "Icon", "drag_pan", 12f, UiDraw.Hex("#7f8ca3")).rectTransform,
                new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(-(hintWidth + 16f) / 2f, 0f), new Vector2(12f, 12f));

            // Footer row: equipment and the equal-hitbox fact.
            UiDraw.Place(UiDraw.Box(card, "SpecRule", UiDraw.Hex("#49627b", .4f)).rectTransform,
                new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, pad + 16f + 12f), new Vector2(440f - pad * 2f, 1f));
            view.dot = null;
            var dot = UiDraw.Box(card, "EquipDot", UiDraw.Hex("#30d5e8"), 4f);
            UiDraw.Place(dot.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(pad, pad + 8f), new Vector2(8f, 8f));
            view.art = dot;
            UiDraw.Place(UiDraw.Text(card, "Equipment", "ARC Rifle (Equipped)", UiFont.MonoRegular, 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(pad + 14f, pad + 8f), new Vector2(200f, 16f));
            UiDraw.Place(UiDraw.Text(card, "HitboxNote", "Standard Hitbox Footprint", UiFont.MonoRegular, 12f, UiDraw.Hex("#7f8ca3"), 0f,
                    TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-pad, pad + 8f), new Vector2(220f, 16f));

            // The whole card is the tap/drag target.
            var tap = new GameObject("Rig_" + BodyRigs.Id(rig), typeof(RectTransform), typeof(Image), typeof(Button), typeof(HomePreviewDrag));
            tap.transform.SetParent(card, false);
            UiDraw.Fill((RectTransform)tap.transform);
            var tapImage = tap.GetComponent<Image>();
            tapImage.color = new Color(1f, 1f, 1f, 0f);
            var choice = tap.GetComponent<Button>();
            choice.targetGraphic = tapImage;
            choice.transition = Selectable.Transition.None;
            choice.onClick.AddListener(() => { selectedRig = rig; RefreshRigSelection(); });
            tap.GetComponent<HomePreviewDrag>().Preview = rigPreview;
            if (rig == BodyRig.Female) { femaleChoice = choice; femaleView = view; } else { maleChoice = choice; maleView = view; }
        }

        void RefreshRigSelection()
        {
            foreach (var (choice, view) in new[] { (femaleChoice, femaleView), (maleChoice, maleView) })
            {
                if (choice == null || view == null) continue;
                bool selected = choice == (selectedRig == BodyRig.Female ? femaleChoice : maleChoice);
                view.glow.enabled = selected;
                view.fill.color = UiDraw.Hex("#101827", selected ? .85f : .6f);
                view.border.sprite = UIRoundedSprites.RoundedOutline(16f, selected ? 2f : 1f);
                view.border.color = selected ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#49627b", .6f);
                view.check.sprite = selected ? UIRoundedSprites.RoundedRect(12f) : UIRoundedSprites.RoundedOutline(12f, 2f);
                view.check.color = selected ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#49627b", .6f);
                view.checkIcon.enabled = selected;
                view.title.color = selected ? Color.white : UiDraw.Hex("#bbc6d8");
                view.badge.text = selected ? "SELECTED" : "TAP TO SELECT";
                view.badge.font = UIFonts.Get(selected ? UiFont.Mono : UiFont.MonoRegular);
                view.badge.color = selected ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#7f8ca3");
                var badgeRect = view.badge.rectTransform;
                badgeRect.anchoredPosition = new Vector2(selected ? -21f - 8f : -21f, badgeRect.anchoredPosition.y);
                float w = UiDraw.Width(view.badge) + 16f + 2f;
                UiDraw.Place(view.badgePlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-21f, -33f), new Vector2(w, 22.5f));
                view.badgePlate.gameObject.SetActive(selected);
                view.art.color = selected ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#49627b");
                view.venue.color = new Color(1f, 1f, 1f, selected ? 1f : .85f);
                view.runner.color = new Color(1f, 1f, 1f, selected ? 1f : .85f);
            }
        }

        void DisposeChoicePreviews()
        {
            femaleChoicePreview?.Dispose(); maleChoicePreview?.Dispose();
            femaleChoicePreview = null; maleChoicePreview = null;
        }

        void OnDestroy() { DisposePreview(); DisposeChoicePreviews(); }

        void ChooseRig(BodyRig rig, bool firstRun)
        {
            BodyRigStore.Set(rig);
            if (rigChoicePanel != null) rigChoicePanel.SetActive(false);
            DisposeChoicePreviews();
            if (firstRun)
            {
                game.StartTutorial();
                return;
            }
            RefreshPreview();
            Refresh();
        }

        static RectTransform Column(Transform parent, string name, float flexibleWidth)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var element = column.GetComponent<LayoutElement>(); element.preferredWidth = 0; element.flexibleWidth = flexibleWidth;
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Hotdrop.UI.MenuLayout.Spacing;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return column.GetComponent<RectTransform>();
        }

        int titleTaps;
        float lastTitleTap;
        GameObject studyPanel;
        TextMeshProUGUI studyStatus;

        void ToggleStudyPanel()
        {
            if (studyPanel != null) { studyPanel.SetActive(!studyPanel.activeSelf); RefreshStudy(); return; }
            var root = panel;
            var screen = Hotdrop.UI.MenuLayout.Create(root.transform, "StudyPanel", "PAD STUDY — FACILITATOR", () => studyPanel.SetActive(false));
            studyPanel = screen.Root;
            var content = screen.Content;
            studyStatus = Hotdrop.UI.MenuLayout.AddText(content, "", 22, Hotdrop.UI.MatchHUD.MainText, "StudyStatus");
            var versions = Hotdrop.UI.MenuLayout.Row(content);
            Hotdrop.UI.MenuLayout.AddButton(versions, "VERSION: WITH PADS", () => { StudySettings.SetCohort(StudySettings.PadsOn); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(versions, "VERSION: WITHOUT PADS", () => { StudySettings.SetCohort(StudySettings.PadsOff); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "NORMAL PLAY (NOT STUDY)", () => { StudySettings.SetCohort("default"); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "TOGGLE INCENTIVIZED TESTER FLAG", () => { StudySettings.SetIncentivizedTester(!StudySettings.IncentivizedTester); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "NEW PARTICIPANT (REPLAY TUTORIAL)", () =>
            {
                PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
                Telemetry.Track("study_participant_started");
                studyPanel.SetActive(false);
                root.SetActive(false);
                game.touchInput.enabled = true;
                game.StartTutorial();
            });
            Hotdrop.UI.MenuLayout.AddText(content, "Collect data:  adb pull /sdcard/Android/data/" + Application.identifier + "/files/" + Telemetry.QueueFileName, 20, Hotdrop.UI.MatchHUD.SecondaryText);
            screen.Status.text = "Never reward participants for choosing either version (spec section 25).";
            RefreshStudy();
        }

        void RefreshStudy()
        {
            if (studyStatus == null) return;
            studyStatus.text = $"Cohort: {Telemetry.Cohort}   ·   Pads {(StudySettings.PadsEnabled ? "ON" : "OFF")}   ·   Incentivized: {(Telemetry.IncentivizedTester ? "YES" : "NO")}   ·   " +
                               $"Queued events: {Telemetry.ReadQueued().Length}   ·   Tutorial will {(PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0 ? "run" : "not run")} next launch";
        }
    }
}


