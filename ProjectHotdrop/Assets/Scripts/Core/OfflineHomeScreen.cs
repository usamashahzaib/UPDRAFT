using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
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
        static string L(string key, string fallback) => UiDraw.L("home." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("home." + key, fallback, args);
        static string RigName(BodyRig rig) => L(rig == BodyRig.Female ? "rig.femaleName" : "rig.maleName", rig == BodyRig.Female ? "FEMALE" : "MALE");

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleMediumDp;
        GameObject panel;
        GameBootstrap game;
        TextMeshProUGUI characterLabel, goalText, onlineStatus;
        TextMeshProUGUI serviceStatusIcon;
        RawImage previewImage;
        RawImage rooftopBackdrop;
        RectTransform homeStrip;
        RectTransform homeHeader, homeTagline;
        RectTransform goalBar;
        TextMeshProUGUI goalProgressLabel;
        HomeCharacterPreview preview;
        GameObject morePanel;
        GameObject rigChoicePanel;
        HomeCharacterPreview femaleChoicePreview, maleChoicePreview;
        BodyRig selectedRig;
        bool rigChoiceInteracted;
        Button femaleChoice, maleChoice;
        RectTransform femaleCard, maleCard;
        Coroutine homeStatusPolling;
        RectTransform moreContent;
        TextMeshProUGUI moreStatus;
        GameObject moreLinkReminder;
        bool moreLinkReminderAdded;
        int morePanelRevision;
        int identityRefreshRevision;
        string displayedAlias = L("guest", "Guest"), displayedLevel = "", displayedSeals = "", connectionState = L("status.checking", "Checking");
        string connectionDetail = "";
        string defaultHomeNote = L("fairPlayNote", "All 3 weapons unlocked • Cosmetics never change power");
        const string AccountLinkReminderShownKey = "hotdrop.accountLinkReminderShown";

        public void Hide()
        {
            StopHomeStatusPolling();
            HideScreen(panel);
            if (rigChoicePanel != null && (panel == null || !rigChoicePanel.transform.IsChildOf(panel.transform)))
                HideScreen(rigChoicePanel);
            DisposeChoicePreviews();
            DisposePreview();
        }

        static void HideScreen(GameObject screen)
        {
            if (screen == null) return;
            UiScreenEnterMotion.Exit(screen, () =>
            {
                if (screen != null) screen.SetActive(false);
            });
        }

        void DisposePreview()
        {
            if (preview != null) { preview.Dispose(); preview = null; }
        }

        public void Show(GameBootstrap game)
        {
            this.game = game;
            Hotdrop.Core.GameAudio.SetHubMusic(true);
            HideScreen(rigChoicePanel);
            DisposeChoicePreviews();
            if (panel != null)
            {
                panel.SetActive(true);
                UiScreenEnterMotion.Reopen(panel);
                FitBackdrop(); RefreshPreview(); Refresh(); StartHomeStatusPolling(); return;
            }
            // QA-019/020: density-aware menu layout — every control stays above the 48 dp floor.
            var screen = Hotdrop.UI.MenuLayout.Create(game.matchHUD.transform, "Home", "HOTDROP", null);
            panel = screen.Root;
            BuildHomeStage(screen);
            RefreshPreview();
            Refresh();
            StartHomeStatusPolling();
        }

        public void OpenSupportReport(int participantSlot)
        {
            if (game == null || panel == null || participantSlot < 0) return;
            PlayerProductScreens.OpenSupportReport(game, panel, participantSlot);
        }

        void OnEnable() => StreamSafePresentation.Changed += RefreshStreamSafeStrip;

        void OnDisable()
        {
            StopHomeStatusPolling();
            StreamSafePresentation.Changed -= RefreshStreamSafeStrip;
        }

        void RefreshStreamSafeStrip()
        {
            SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
            RefreshMoreAccountPrivacy();
        }

        void RefreshMoreAccountPrivacy()
        {
            if (moreLinkReminder != null)
            {
                bool showReminder = !StreamSafePresentation.Enabled;
                moreLinkReminder.SetActive(showReminder);
                if (moreStatus != null)
                    moreStatus.text = showReminder
                        ? L("more.linkReminder", "Link your guest account to recover progress after reinstall or on another phone.")
                        : L("more.status", "Account, support and deletion stay reachable here.");
            }
            else if (!StreamSafePresentation.Enabled && morePanel != null && morePanel.activeInHierarchy && !moreLinkReminderAdded)
            {
                RefreshAccountLinkReminder(++morePanelRevision);
            }
        }

        void StartHomeStatusPolling()
        {
            if (homeStatusPolling == null && panel != null && panel.activeInHierarchy)
                homeStatusPolling = StartCoroutine(PollHomeStatus());
        }

        void StopHomeStatusPolling()
        {
            identityRefreshRevision++;
            if (homeStatusPolling == null) return;
            StopCoroutine(homeStatusPolling);
            homeStatusPolling = null;
        }

        IEnumerator PollHomeStatus()
        {
            var wait = new WaitForSecondsRealtime(30f);
            while (panel != null && panel.activeInHierarchy)
            {
                yield return wait;
                if (panel == null || !panel.activeInHierarchy) break;
                _ = RefreshConnectionStatusAsync(++identityRefreshRevision);
            }
            homeStatusPolling = null;
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
            var rooftop = UiPresentationProfile.LoadRooftopArtwork();
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
            characterLabel = UiDraw.Text(root, "RunnerLabel", "", UiFont.Body, CaptionDp, Color.white);
            characterLabel.gameObject.SetActive(false);

            BuildHomeHeader(root);

            // Tagline plate under the header.
            var tagline = UiDraw.Box(root, "Home_Tagline", Ink(.75f), 16f, UiDraw.Hex("#49627b", .4f));
            var tag1 = UiDraw.Text(tagline.transform, "Tagline", L("tagline", "Three players. One team. Three minutes."), UiFont.HeadingMedium, CaptionDp,
                UiDraw.Hex("#bbc6d8"), .05f, TextAlignmentOptions.Midline);
            var tag2 = UiDraw.Text(tagline.transform, "TaglineSub", L("taglineSub", "Build shortcuts your enemies can steal."), UiFont.Body, CaptionDp,
                UiDraw.Hex("#bbc6d8"), .025f, TextAlignmentOptions.Midline);
            float tagWidth = Mathf.Max(UiDraw.Width(tag1), UiDraw.Width(tag2)) + 40f + 2f;
            // Sits 8 px under the header, which grows on dense phones (see HeaderHeight).
            UiDraw.Place(tagline.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -(HeaderHeight + 8f)), new Vector2(tagWidth, 47f));
            homeTagline = tagline.rectTransform;
            UiDraw.Place(tag1.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -15f), new Vector2(tagWidth, 16f));
            UiDraw.Place(tag2.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -32.5f), new Vector2(tagWidth, 16f));

            BuildPlayCards(root);
            BuildGoalCard(root, Ink);
            BuildHomeFooter(root, Ink);
            onlineStatus = fairPlayNote;
        }

        // 65 px design header around 44 px chips/settings. On dense phones the 48 dp floor raises
        // SETTINGS (and the dev-build logo tap) past that, so the header grows instead of pushing
        // them over the top edge.
        static float HeaderHeight => UiMetrics.BarHeight(65f, 44f);

        void BuildHomeHeader(RectTransform root)
        {
            var bar = UiDraw.Box(root, "Home_Header", UiDraw.Hex("#101827", .8f));
            var rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(0f, HeaderHeight); rect.anchoredPosition = Vector2.zero;
            homeHeader = rect;
            var rule = UiDraw.Box(rect, "Rule", UiDraw.Hex("#49627b", .3f)).rectTransform;
            rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1f, 0f); rule.pivot = new Vector2(.5f, 0f);
            rule.sizeDelta = new Vector2(0f, 1f); rule.anchoredPosition = Vector2.zero;

            var logo = UiDraw.Art(rect, "HomeLogo", "home_logo", Vector2.zero);
            // Header contents are centred (1.5 px low, as designed) so they follow the header when it grows.
            UiDraw.Place(logo.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, -1.5f), new Vector2(160f, 32f));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Facilitator-only study controls: five taps on the logo. Never a player-facing route.
            // The tap target is an invisible child, so the 48 dp floor enlarges it, not the logo art.
            var tapArea = UiDraw.Place(UiDraw.Node(logo.rectTransform, "StudyTap"), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(160f, 32f));
            tapArea.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var titleTap = tapArea.gameObject.AddComponent<Button>();
            titleTap.transition = Selectable.Transition.None;
            titleTap.onClick.AddListener(() =>
            {
                if (Time.unscaledTime - lastTitleTap > 1.5f) titleTaps = 0;
                lastTitleTap = Time.unscaledTime;
                if (++titleTaps >= 5) { titleTaps = 0; ToggleStudyPanel(); }
            });
#endif
            chipRow = UiDraw.Node(rect, "Home_Strip");
            UiDraw.Place(chipRow, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, -1.5f), new Vector2(900f, 44f));
            homeStrip = chipRow;

            var chipFill = UiDraw.Hex("#1b2940", .9f);
            aliasChip = UiDraw.Box(chipRow, "IdentityChip", chipFill, 12f, UiDraw.Hex("#49627b", .4f)).rectTransform;
            aliasText = UiDraw.Text(aliasChip, "Identity", L("guest", "Guest"), UiFont.Heading, UiDp, Color.white, .025f);
            levelDot = UiDraw.Text(aliasChip, "Dot", "•", UiFont.Body, UiDp, UiDraw.Hex("#49627b"));
            levelText = UiDraw.Text(aliasChip, "Level", "", UiFont.MonoMedium, CaptionDp, UiDraw.Hex("#30d5e8"), .05f);

            sealsChip = UiDraw.Box(chipRow, "SealsChip", chipFill, 12f, UiDraw.Hex("#ffd166", .4f)).rectTransform;
            UiDraw.Icon(sealsChip, "SealIcon", "military_tech", 20f, UiDraw.Hex("#ffd166"));
            sealsCount = UiDraw.Text(sealsChip, "Seals", "", UiFont.Mono, UiDp, UiDraw.Hex("#ffd166"));
            UiDraw.Text(sealsChip, "SealsCaption", L("flightSeals", "Flight Seals"), UiFont.BodySemiBold, CaptionDp, UiDraw.Hex("#bbc6d8"));

            netChip = UiDraw.Box(chipRow, "NetworkChip", chipFill, 12f, UiDraw.Hex("#49627b", .4f)).rectTransform;
            netDot = UiDraw.Box(netChip, "NetDot", UiDraw.Hex("#30d5e8"), 4f);
            netText = UiDraw.Text(netChip, "Region", "", UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"));

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
            var chipLabel = UiDraw.Text(p, "Mode", L("relay", "Relay"), UiFont.Mono, CaptionDp, UiDraw.Hex("#30d5e8"), .1f);
            float chipW = 10f + 6f + 8f + UiDraw.Width(chipLabel) + 10f + 2f;
            var chip = UiDraw.Box(p, "ModeChip", UiDraw.Hex("#30d5e8", .2f), 4f, UiDraw.Hex("#30d5e8", .4f));
            UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -32f), new Vector2(chipW, 22f));
            UiDraw.Place(UiDraw.Box(chip.transform, "Ping", UiDraw.Hex("#30d5e8"), 3f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(11f, 0f), new Vector2(6f, 6f));
            chipLabel.transform.SetParent(chip.transform, false);
            UiDraw.Place(chipLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(25f, 0f), new Vector2(UiDraw.Width(chipLabel) + 2f, 16f));
            var ready = UiDraw.Text(p, "Ready", L("soloReady", "SOLO • READY"), UiFont.Heading, CaptionDp, Color.white, .05f, TextAlignmentOptions.Midline);
            float readyW = UiDraw.Width(ready) + 20f + 2f;
            var pill = UiDraw.Box(p, "ReadyPill", UiDraw.Hex("#101827", .8f), 11f, UiDraw.Hex("#49627b", .5f));
            UiDraw.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -32f), new Vector2(readyW, 22f));
            ready.transform.SetParent(pill.transform, false);
            UiDraw.Fill(ready.rectTransform);
            UiDraw.Place(UiDraw.Text(p, "PlayTitle", L("playTitle", "Play 3-Minute Relay"), UiFont.Heading, TitleDp, Color.white, -.025f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -64.7f), new Vector2(356f, 32f));
            UiDraw.Place(UiDraw.Text(p, "PlayDetail", L("playDetail", "3v3 • One active objective • Zone rotates every 30 sec"), UiFont.Body, CaptionDp, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -94f), new Vector2(356f, 20f));
            UiDraw.Place(UiDraw.Box(p, "CueRule", UiDraw.Hex("#49627b", .4f)).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -116.5f), new Vector2(356f, 1f));
            UiDraw.Place(UiDraw.Box(p, "CueDot", UiDraw.Hex("#30d5e8"), 4f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -140.7f), new Vector2(8f, 8f));
            UiDraw.Place(UiDraw.Text(p, "PlayCue", L("playCue", "Touch to Start Match"), UiFont.HeadingSemiBold, CaptionDp, UiDraw.Hex("#bbc6d8"), .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(38f, -140.7f), new Vector2(300f, 16f));
            UiDraw.Place(UiDraw.Icon(p, "PlayArrow", "arrow_forward", 18f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -140.7f), new Vector2(18f, 18f));

            var offline = UiDraw.Box(cluster, "PLAY OFFLINE", UiDraw.Hex("#1b2940", .9f), 12f, UiDraw.Hex("#49627b", .5f));
            offline.raycastTarget = true;
            UiDraw.Place(offline.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -190f), new Vector2(400f, 70f));
            HomeButton(offline.rectTransform, PlayOffline);
            var o = offline.rectTransform;
            var offTitle = UiDraw.Text(o, "OfflineTitle", L("offlineTitle", "Play Offline"), UiFont.Heading, UiDp, Color.white);
            UiDraw.Place(offTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(21f, -27f), new Vector2(UiDraw.Width(offTitle) + 2f, 24f));
            var solo = UiDraw.Text(o, "SoloLabel", L("solo", "Solo"), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"), 0f, TextAlignmentOptions.Midline);
            var soloBox = UiDraw.Box(o, "SoloChip", UiDraw.Hex("#101827"), 4f, UiDraw.Hex("#49627b", .3f));
            UiDraw.Place(soloBox.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(21f + UiDraw.Width(offTitle) + 8f, -27f),
                new Vector2(UiDraw.Width(solo) + 14f, 19f));
            solo.transform.SetParent(soloBox.transform, false);
            UiDraw.Fill(solo.rectTransform);
            int count = 12;
            var runtime = game != null ? game.GetComponent<OfflineChallengeRuntime>() : null;
            if (runtime?.Controller?.config?.entries != null && runtime.Controller.config.entries.Count > 0)
                count = runtime.Controller.config.entries.Count;
            UiDraw.Place(UiDraw.Text(o, "OfflineDetail", F("offlineDetail", "{0} Launch Challenges • Works without internet", count), UiFont.Body, CaptionDp, UiDraw.Hex("#bbc6d8")).rectTransform,
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
            UiDraw.Place(UiDraw.Text(g, "GoalEyebrow", L("nextGoal", "Next Goal"), UiFont.Heading, CaptionDp, UiDraw.Hex("#30d5e8"), .1f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 24f, -32.7f), new Vector2(220f, 16f));
            goalEyebrowChip = UiDraw.Text(g, "GoalContext", "", UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"), 0f, TextAlignmentOptions.Midline);
            var contextPlate = UiDraw.Box(g, "GoalContextPlate", UiDraw.Hex("#141c2b"), 4f, UiDraw.Hex("#49627b", .4f));
            contextPlate.transform.SetSiblingIndex(goalEyebrowChip.transform.GetSiblingIndex());
            UiDraw.HRule(g, "HeaderRule", UiDraw.Hex("#49627b", .4f), 54.7f, pad, pad);
            goalText = UiDraw.Text(g, "Goal", "", UiFont.Heading, UiDp, Color.white, -.025f);
            UiDraw.Place(goalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -82.7f), new Vector2(338f, 28f));
            goalDesc = UiDraw.Text(g, "GoalDetail", "", UiFont.Body, CaptionDp, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(goalDesc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -106.7f), new Vector2(338f, 16f));
            goalProgressName = UiDraw.Text(g, "GoalProgressName", "", UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(goalProgressName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -136.7f), new Vector2(200f, 16f));
            goalProgressLabel = UiDraw.Text(g, "GoalProgressLabel", "", UiFont.Mono, CaptionDp, UiDraw.Hex("#ffd166"), 0f, TextAlignmentOptions.MidlineRight);
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
            rewardEyebrow = UiDraw.Text(reward.transform, "RewardEyebrow", "", UiFont.Heading, CaptionDp, UiDraw.Hex("#ffd166"), .05f);
            UiDraw.Place(rewardEyebrow.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(55f, 8f), new Vector2(270f, 14f));
            rewardText = UiDraw.Text(reward.transform, "RewardText", "", UiFont.Heading, CaptionDp, Color.white);
            UiDraw.Place(rewardText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(55f, -8f), new Vector2(270f, 16f));

            UiDraw.HRule(g, "NoteRule", UiDraw.Hex("#49627b", .3f), 242.7f, pad, pad);
            serviceStatusIcon = UiDraw.Icon(g, "ShieldIcon", "shield", 12f, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(serviceStatusIcon.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -259f), new Vector2(12f, 12f));
            fairPlayNote = UiDraw.Text(g, "FairPlayNote", L("fairPlayNote", "All 3 weapons unlocked • Cosmetics never change power"), UiFont.Body, CaptionDp,
                UiDraw.Hex("#bbc6d8", .9f));
            fairPlayNote.richText = false;
            fairPlayNote.enableAutoSizing = true;
            fairPlayNote.fontSizeMin = UiMetrics.MinTextUnits;
            fairPlayNote.fontSizeMax = CaptionDp;
            fairPlayNote.textWrappingMode = TextWrappingModes.Normal;
            fairPlayNote.alignment = TextAlignmentOptions.MidlineLeft;
            UiDraw.Place(fairPlayNote.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 20f, -258f), new Vector2(318f, 30f));

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
                ("PARTY", L("nav.party", "Party"), "group", 130f, UiDraw.Hex("#30d5e8"), () => PlayerProductScreens.Open(game, panel, "Party")),
                ("APPEARANCE", L("nav.appearance", "Appearance"), "checkroom", 140f, UiDraw.Hex("#bbc6d8"), () => PlayerProductScreens.Open(game, panel, "Appearance")),
                ("MORE", L("nav.more", "More"), "more_horiz", 120f, UiDraw.Hex("#bbc6d8"), ToggleMore),
            };
            var widths = new float[items.Length];
            var labels = new TextMeshProUGUI[items.Length];
            float total = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                labels[i] = UiDraw.Text(nav, "Caption", items[i].caption, UiFont.Heading, CaptionDp, Color.white, .05f);
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
            FitHeader();
        }

        /// <summary>Home is built once and reused; re-fit the header when the display density changes.</summary>
        void FitHeader()
        {
            if (homeHeader == null || !homeHeader.gameObject.activeInHierarchy) return;
            float height = HeaderHeight;
            if (Mathf.Abs(homeHeader.sizeDelta.y - height) < .5f) return;
            homeHeader.sizeDelta = new Vector2(homeHeader.sizeDelta.x, height);
            if (homeTagline != null) homeTagline.anchoredPosition = new Vector2(homeTagline.anchoredPosition.x, -(height + 8f));
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
            HideScreen(panel);
            DisposePreview();
            game.touchInput.enabled = true;
        }

        void Refresh()
        {
            if (characterLabel != null)
                characterLabel.text = F("runner.nameTemplate", "{0} Runner", RigName(BodyRigStore.Current));
            if (goalText != null)
            {
                string line = GoalLine();
                int split = line.IndexOf('\n');
                goalText.text = split < 0 ? line : line.Substring(0, split);
                if (goalDesc != null) goalDesc.text = split < 0 ? "" : line.Substring(split + 1);
            }
            if (onlineStatus != null)
            {
                defaultHomeNote = OnlineHooks.UnavailableReason?.Invoke() ?? L("fairPlayNote", "All 3 weapons unlocked • Cosmetics never change power");
                onlineStatus.text = defaultHomeNote;
            }
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
                SetGoalContext(L("goal.context.training", "Training"));
                goalProgressName.text = L("goal.guidedTraining", "Guided Training");
                goalProgressLabel.text = F("goal.progress", "{0} / {1} <color=#bbc6d8>({2}%)</color>", 0, 1, 0);
                rewardEyebrow.text = L("goal.unlocks", "Unlocks");
                rewardText.text = L("goal.launchChallenges", "Launch Challenges (offline)");
            }
            else
            {
                fraction = Mathf.Clamp01((float)done / total);
                SetGoalContext(L("goal.context.offline", "Offline"));
                goalProgressName.text = L("goal.challengeProgress", "Challenge Progress");
                goalProgressLabel.text = F("goal.progress", "{0} / {1} <color=#bbc6d8>({2}%)</color>", done, total, Mathf.RoundToInt(fraction * 100f));
                rewardEyebrow.text = done >= total ? L("goal.allCleared", "All Cleared") : L("goal.nextUp", "Next Up");
                rewardText.text = done >= total ? L("goal.beatBestTimes", "Beat your best times") : L("goal.startChallenge", "Tap to start this challenge");
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
            int revision = ++identityRefreshRevision;
            var client = BackendClient.Instance;
            bool hasSession = client.HasSession || client.HasSavedSession;
            displayedAlias = hasSession
                ? UiDraw.L("profile.identity.aliasSyncing", "Account Syncing")
                : L("guest", "Guest");
            displayedLevel = displayedSeals = "";
            connectionDetail = "";
            SetStrip(displayedAlias, displayedLevel, displayedSeals, L("status.checking", "Checking"));
            var availability = await ReadConnectionStateAsync(client);
            connectionState = availability.state;
            connectionDetail = availability.detail;
            if (this == null || revision != identityRefreshRevision || homeStrip == null) return;
            SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
            if (!hasSession) return;

            var profile = await client.GetProfileAsync();
            if (this == null || revision != identityRefreshRevision || homeStrip == null) return;
            if (profile.Ok && profile.Value != null)
            {
                await SyncBodyRigPreferenceAsync(client, profile.Value.bodyRigId);
                if (this == null || revision != identityRefreshRevision || homeStrip == null) return;
            }
            if (profile.Ok && profile.Value != null && !string.IsNullOrEmpty(profile.Value.alias))
            {
                displayedAlias = profile.Value.alias;
                displayedLevel = F("identity.levelTemplate", "Level {0}", Mathf.Clamp(profile.Value.level, 1, 20));
                SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
                // Seals are permanent server-derived progress, so only ask when a goal exists.
                if (ContractsMasteryFeature.Enabled)
                {
                    var goals = await client.GoalsAsync();
                    if (this == null || revision != identityRefreshRevision || homeStrip == null) return;
                    if (goals.Ok && goals.Value != null)
                    {
                        displayedSeals = goals.Value.seals.ToString();
                        SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
                    }
                }
            }
            else
            {
                displayedAlias = UiDraw.L("profile.identity.aliasUnavailable", "Account Unavailable");
                displayedLevel = displayedSeals = "";
                SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
            }
        }

        async Task SyncBodyRigPreferenceAsync(BackendClient client, string accountRigId)
        {
            // The account is authoritative after a successful prior save. A local choice that
            // has not reached the account wins until its retry succeeds, so an older profile
            // response cannot undo the player's latest selection.
            if (!BodyRigStore.SyncPending && BodyRigs.TryParse(accountRigId, out var accountRig))
            {
                ApplyAccountRigPreference(accountRig);
                return;
            }

            var localRig = BodyRigStore.Current;
            if (!BodyRigStore.SyncPending) BodyRigStore.Set(localRig);
            var result = await client.EquipAsync(BodyRigs.Id(localRig));
            if (result.Ok) BodyRigStore.MarkAccountSynced(localRig);
            else Debug.LogWarning("HOTDROP account body-rig sync is pending; the local selection remains active.");
        }

        async void PersistChosenBodyRig(BodyRig rig)
        {
            var client = BackendClient.Instance;
            if (!client.HasSession && !client.HasSavedSession) return;
            var result = await client.EquipAsync(BodyRigs.Id(rig));
            if (result.Ok) BodyRigStore.MarkAccountSynced(rig);
            else Debug.LogWarning("HOTDROP account body-rig sync is pending; the local selection remains active.");
        }

        async Task<(string state, string detail)> ReadConnectionStateAsync(BackendClient client)
        {
            try
            {
                var status = await client.ClientStatusAsync();
                if (status.Error == BackendError.UpdateRequired) return (L("status.updateRequired", "Update Required"), L("status.updateRequiredDetail", "A newer version is required for online play."));
                if (!status.Ok || status.Value == null)
                    return (status.Error == BackendError.NoInternet || status.Error == BackendError.NotConfigured
                        ? L("status.offline", "Offline") : L("status.serverUnavailable", "Server Unavailable"), "");
                switch (status.Value.state)
                {
                    case "maintenance":
                        return (L("status.maintenance", "Maintenance"), FormatHomeStatusDetail(status.Value.message, status.Value.etaUtc));
                    case "queue_paused":
                        return (L("status.queuePaused", "Queue Paused"), FormatHomeStatusDetail(status.Value.message, null));
                    case "available":
                        return (L("status.online", "Online"), "");
                    default:
                        return (L("status.unknown", "Server Status Unknown"), "");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Home server status check failed: {exception.Message}");
                return (L("status.serverUnavailable", "Server Unavailable"), "");
            }
        }

        static string FormatHomeStatusDetail(string message, string etaUtc)
        {
            // This string is operator-controlled, so remove control characters and keep rich text
            // disabled on the label. Preserve a compact ETA on its own line when one is supplied.
            string cleanMessage = CleanStatusText(message, 110);
            string cleanEta = CleanStatusText(etaUtc, 48);
            if (string.IsNullOrEmpty(cleanMessage)) cleanMessage = L("status.matchmakingUnavailable", "Online matchmaking is temporarily unavailable.");
            return string.IsNullOrEmpty(cleanEta) ? cleanMessage : F("status.etaTemplate", "{0}\nETA: {1} UTC", cleanMessage, cleanEta);
        }

        static string CleanStatusText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var builder = new System.Text.StringBuilder(Mathf.Min(value.Length, maxLength));
            bool previousSpace = false;
            bool truncated = false;
            foreach (char c in value.Trim())
            {
                if (char.IsControl(c))
                {
                    if (!previousSpace) builder.Append(' ');
                    previousSpace = true;
                }
                else
                {
                    builder.Append(c);
                    previousSpace = char.IsWhiteSpace(c);
                }
                if (builder.Length >= maxLength) { truncated = true; break; }
            }
            string result = builder.ToString().Trim();
            return truncated ? result.TrimEnd() + "…" : result;
        }

        async Task RefreshConnectionStatusAsync(int revision)
        {
            var availability = await ReadConnectionStateAsync(BackendClient.Instance);
            if (this == null || revision != identityRefreshRevision || homeStrip == null || panel == null || !panel.activeInHierarchy) return;
            connectionState = availability.state;
            connectionDetail = availability.detail;
            SetStrip(displayedAlias, displayedLevel, displayedSeals, connectionState);
        }

        void SetStrip(string alias, string level, string seals, string state)
        {
            if (aliasText == null) return;
            aliasText.text = StreamSafePresentation.AccountAlias(alias);
            levelText.text = StreamSafePresentation.AccountProgress(level);
            sealsCount.text = StreamSafePresentation.AccountProgress(seals);
            netText.text = string.IsNullOrEmpty(state) ? L("status.offline", "Offline") : state;
            bool hasServiceDetail = !string.IsNullOrWhiteSpace(connectionDetail);
            onlineStatus.text = hasServiceDetail ? connectionDetail : defaultHomeNote;
            serviceStatusIcon.gameObject.SetActive(!hasServiceDetail);
            onlineStatus.color = hasServiceDetail ? UiDraw.Hex("#ffda88") : UiDraw.Hex("#bbc6d8", .9f);
            netDot.color = state == L("status.online", "Online") ? UiDraw.Hex("#30d5e8")
                : state == L("status.maintenance", "Maintenance") || state == L("status.queuePaused", "Queue Paused") ? UiDraw.Hex("#ffda88") : UIDesignTokens.SignalWarning;
            LayoutChips();
        }

        string GoalLine()
        {
            // Shared with the results screen so Home and post-match always agree. Building the
            // runtime binds its view to the controller's current (Library) state and renders it,
            // which would pop the Offline Challenges screen over Home the first time a trained
            // player's goal text is ever computed; hide it immediately so only the text — the
            // thing this call actually wants — escapes the read.
            if (PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) != 0 &&
                game.GetComponent<OfflineChallengeRuntime>() == null)
                game.gameObject.AddComponent<OfflineChallengeRuntime>().View.Hide();
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
                if (morePanel.activeSelf)
                {
                    morePanelRevision++;
                    HideScreen(morePanel);
                }
                else
                {
                    morePanel.SetActive(true);
                    UiScreenEnterMotion.Reopen(morePanel);
                    morePanel.transform.SetAsLastSibling();
                    RefreshMoreAccountPrivacy();
                    RefreshAccountLinkReminder(++morePanelRevision);
                }
                return;
            }
            var screen = Hotdrop.UI.MenuLayout.Create(panel.transform, "More", L("moreTitle", "More"), () => HideScreen(morePanel));
            morePanel = screen.Root;
            moreContent = screen.Content;
            moreStatus = screen.Status;
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.profile", "Profile"), () => PlayerProductScreens.Open(game, panel, "Profile"), "PROFILE");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.onlineRelay", "Online Relay"), PlayOnline, "ONLINE RELAY");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.training", "Training"), () => { Hide(); game.StartTutorial(); }, "TRAINING");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.settings", "Settings"), () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "SETTINGS");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.account", "Account"), () => PlayerProductScreens.Open(game, panel, "Account"), "ACCOUNT");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.support", "Support / Report"), () => PlayerProductScreens.Open(game, panel, "Support"), "SUPPORT");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.contracts", "Contracts"), () => PlayerProductScreens.Open(game, panel, "Contracts"), "CONTRACTS");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, L("more.mastery", "Mastery"), () => PlayerProductScreens.Open(game, panel, "Mastery"), "MASTERY");
            screen.Status.text = L("more.status", "Account, support and deletion stay reachable here.");
            RefreshAccountLinkReminder(++morePanelRevision);
        }

        async void RefreshAccountLinkReminder(int revision)
        {
            if (StreamSafePresentation.Enabled || morePanel == null || moreContent == null || !morePanel.activeInHierarchy || moreLinkReminderAdded ||
                PlayerPrefs.GetInt(AccountLinkReminderShownKey, 0) != 0 || !PlatformIdentity.Available)
                return;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (this == null || revision != morePanelRevision || morePanel == null || !morePanel.activeInHierarchy ||
                moreLinkReminderAdded || StreamSafePresentation.Enabled || PlayerPrefs.GetInt(AccountLinkReminderShownKey, 0) != 0)
                return;
            if (!profile.Ok || profile.Value == null)
            {
                if (moreStatus != null)
                    moreStatus.text = L("more.accountLevelUnavailable", "Account level could not be checked. Reopen More when online to retry the account-link reminder.");
                return;
            }
            if (!profile.Value.isGuest || profile.Value.level < 5) return;

            var reminder = Hotdrop.UI.MenuLayout.AddButton(moreContent, L("more.linkAccount", "Link Account"), () =>
                PlayerProductScreens.Open(game, panel, "Account"), "ACCOUNT_LINK_REMINDER");
            moreLinkReminder = reminder.gameObject;
            reminder.transform.SetSiblingIndex(0);
            if (moreStatus != null)
                moreStatus.text = L("more.linkReminder", "Link your guest account to recover progress after reinstall or on another phone.");
            moreLinkReminderAdded = true;
            PlayerPrefs.SetInt(AccountLinkReminderShownKey, 1);
            PlayerPrefs.Save();
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
            if (rigChoicePanel != null)
            {
                var previousChoice = rigChoicePanel;
                rigChoicePanel = null;
                UiScreenEnterMotion.Exit(previousChoice, () => Destroy(previousChoice));
            }
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
            var screen = Hotdrop.UI.MenuLayout.Create(parent, "RigChoice", firstRun ? L("rig.firstRunTitle", "Choose Your Look") : L("rig.changeTitle", "Change Your Look"), null);
            rigChoicePanel = screen.Root;
            selectedRig = BodyRigStore.Current;
            rigChoiceInteracted = false;
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
            var title = UiDraw.Text(root, "Title", firstRun ? L("rig.firstRunTitle", "Choose Your Look") : L("rig.changeTitle", "Change Your Look"), UiFont.Heading, TitleDp,
                Color.white, -.025f);
            float titleWidth = UiDraw.Width(title);
            UiDraw.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide, -RigEdge - 16f), new Vector2(titleWidth + 4f, 32f));
            if (firstRun)
            {
                // The chip marks these as the provided starter looks, which is only true on first run.
                var chipText = UiDraw.Text(root, "StarterOutfitLabel", L("rig.starterOutfit", "Starter Outfit"), UiFont.Mono, CaptionDp, UiDraw.Hex("#30d5e8"), .05f,
                    TextAlignmentOptions.Midline);
                float chipWidth = UiDraw.Width(chipText) + 22f;
                var chip = UiDraw.Box(root, "StarterOutfitChip", UiDraw.Hex("#1b2940"), 4f, UiDraw.Hex("#49627b", .4f));
                UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide + titleWidth + 12f, -RigEdge - 16f), new Vector2(chipWidth, 22.5f));
                chipText.transform.SetParent(chip.transform, false);
                UiDraw.Fill(chipText.rectTransform);
            }
            UiDraw.Place(UiDraw.Icon(root, "VerifiedIcon", "verified", 14f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide, -RigEdge - 42f), new Vector2(14f, 14f));
            var note = UiDraw.Text(root, "RigNote", L("rig.note", "Appearance only. Same movement, abilities, animations, and hitbox."), UiFont.Body, CaptionDp,
                UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(RigSide + 20f, -RigEdge - 42f), new Vector2(560f, 16f));
            BuildFramingToggle(root);

            // Two equal cards, 440 x 480, 40 apart, centred between header and footer.
            // Presentation 01 is the ready-to-play default, so it sits first and starts selected.
            var second = BodyRigs.Default == BodyRig.Male ? BodyRig.Female : BodyRig.Male;
            AddRigOption(root, BodyRigs.Default, new Vector2(-240f, 5f), RigName(BodyRigs.Default), mirrorVenue: false);
            AddRigOption(root, second, new Vector2(240f, 5f), RigName(second), mirrorVenue: true);

            // Footer: reassurance note left, Decide Later + Continue right.
            var footer = UiDraw.Box(root, "RigFooter", UiDraw.Hex("#060e1d", .7f));
            var footerRect = footer.rectTransform;
            footerRect.anchorMin = Vector2.zero; footerRect.anchorMax = new Vector2(1f, 0f); footerRect.pivot = new Vector2(.5f, 0f);
            footerRect.sizeDelta = new Vector2(0f, 84f); footerRect.anchoredPosition = Vector2.zero;
            UiDraw.HRule(footerRect, "TopRule", UiDraw.Hex("#49627b", .3f), 0f);
            UiDraw.Place(UiDraw.Icon(footerRect, "ShuffleIcon", "shuffle", 16f, UiDraw.Hex("#30d5e8")).rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(RigSide, RigEdge + 24f), new Vector2(16f, 16f));
            var reassure = UiDraw.Text(footerRect, "Reassurance", firstRun
                    ? L("rig.firstRunReassurance", "You can freely change your appearance anytime from <color=#f4f7fb><b>Appearance</b></color>.")
                    : L("rig.changeReassurance", "Applies to your next spawn in offline play."),
                UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"));
            UiDraw.Place(reassure.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(RigSide + 24f, RigEdge + 24f), new Vector2(640f, 16f));

            var continueText = UiDraw.Text(footerRect, "Caption", firstRun ? L("rig.continue", "Continue") : L("rig.saveRunner", "Save Runner"), UiFont.Heading, UiDp,
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
                : () => { HideScreen(rigChoicePanel); DisposeChoicePreviews(); };
            var laterText = UiDraw.Text(footerRect, "Caption", firstRun ? L("rig.decideLater", "Decide Later") : L("rig.back", "Back"), UiFont.Heading, CaptionDp,
                UiDraw.Hex("#bbc6d8"), .05f, TextAlignmentOptions.Midline);
            float laterWidth = 24f + UiDraw.Width(laterText) + 24f;
            var laterButton = UiDraw.Tap(footerRect, firstRun ? L("rig.decideLater", "Decide Later") : L("rig.back", "Back"), new Color(0f, 0f, 0f, 0f), 12f, later);
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
                var label = UiDraw.Text(rect, "Caption", caption, UiFont.Mono, CaptionDp, Color.white);
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
            fullBodyTab = Tab("FramingFullBody", "accessibility_new", L("rig.fullBody", "Full Body"), false, out fullBodyText, out fullBodyIcon);
            faceTab = Tab("FramingFace", "face", L("rig.face", "Face"), true, out faceText, out faceIcon);
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
            fullBodyText.color = faceFraming ? UiDraw.Hex("#bbc6d8") : Color.white;
            faceText.color = faceFraming ? Color.white : UiDraw.Hex("#bbc6d8");
            fullBodyIcon.color = faceFraming ? UiDraw.Hex("#bbc6d8") : UiDraw.Hex("#30d5e8");
            faceIcon.color = faceFraming ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#bbc6d8");
        }

        void AddRigOption(RectTransform parent, BodyRig rig, Vector2 centre, string caption, bool mirrorVenue)
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
                UiFont.Heading, UiDp, Color.white, .025f);
            UiDraw.Place(view.title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 32f, -pad - 12f), new Vector2(260f, 20f));
            view.badgePlate = UiDraw.Box(card, "SelectionBadgePlate", UiDraw.Hex("#30d5e8", .1f), 4f, UiDraw.Hex("#30d5e8", .3f));
            view.badge = UiDraw.Text(card, "SelectionBadge", "", UiFont.Mono, CaptionDp, UiDraw.Hex("#30d5e8"), 0f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(view.badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad - 8f, -pad - 12f), new Vector2(160f, 16f));

            // Art: the venue shot with the live runner standing in it, centred in the art area.
            float artTop = pad + 24f + 8f, artBottom = pad + 16f + 12f + 1f + 8f;
            var area = UiDraw.Node(card, "ArtArea");
            UiDraw.Fill(area, pad, artTop, pad, artBottom);
            view.venue = UiDraw.Node(area, "Rooftop").gameObject.AddComponent<RawImage>();
            UiDraw.Place(view.venue.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(398f, 224f));
            view.venue.texture = UiPresentationProfile.LoadRooftopArtwork();
            view.venue.uvRect = mirrorVenue ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
            view.venue.raycastTarget = false;
            view.runner = UiDraw.Node(area, "RunnerPreview").gameObject.AddComponent<RawImage>();
            // The runner stands on the rooftop inside the shot, at the scale the reference art shows.
            UiDraw.Place(view.runner.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(mirrorVenue ? 8f : -8f, 0f), new Vector2(104f, 130f));
            view.runner.raycastTarget = false;
            var rigPreview = new HomeCharacterPreview(rig: rig);
            view.runner.texture = rigPreview.Texture;
            if (rig == BodyRig.Female) femaleChoicePreview = rigPreview; else maleChoicePreview = rigPreview;
            var hint = UiDraw.Box(area, "RotateHint", UiDraw.Hex("#060e1d", .6f), 13f, UiDraw.Hex("#49627b", .3f));
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f); hintRect.anchorMax = new Vector2(1f, 0f); hintRect.pivot = new Vector2(.5f, 0f);
            hintRect.offsetMin = new Vector2(32f, 8f); hintRect.offsetMax = new Vector2(-32f, 8f + 26.5f);
            var hintText = UiDraw.Text(hint.transform, "Caption", L("rig.dragRotate", "Drag to Rotate"), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"), 0f,
                TextAlignmentOptions.MidlineLeft);
            float hintWidth = UiDraw.Width(hintText);
            UiDraw.Place(hintText.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(-(hintWidth + 16f) / 2f + 16f, 0f), new Vector2(hintWidth + 2f, 16f));
            UiDraw.Place(UiDraw.Icon(hint.transform, "Icon", "drag_pan", 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(-(hintWidth + 16f) / 2f, 0f), new Vector2(12f, 12f));

            // Footer row: equipment and the equal-hitbox fact.
            UiDraw.Place(UiDraw.Box(card, "SpecRule", UiDraw.Hex("#49627b", .4f)).rectTransform,
                new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, pad + 16f + 12f), new Vector2(440f - pad * 2f, 1f));
            view.dot = null;
            var dot = UiDraw.Box(card, "EquipDot", UiDraw.Hex("#30d5e8"), 4f);
            UiDraw.Place(dot.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(pad, pad + 8f), new Vector2(8f, 8f));
            view.art = dot;
            UiDraw.Place(UiDraw.Text(card, "Equipment", L("rig.equipment", "ARC Rifle (Equipped)"), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(pad + 14f, pad + 8f), new Vector2(200f, 16f));
            UiDraw.Place(UiDraw.Text(card, "HitboxNote", L("rig.hitboxNote", "Standard Hitbox Footprint"), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#7f8ca3"), 0f,
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
            choice.onClick.AddListener(() => { rigChoiceInteracted = true; selectedRig = rig; RefreshRigSelection(); });
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
                view.badge.text = selected ? L("rig.selected", "Selected") : L("rig.tapToSelect", "Tap to Select");
                view.badge.font = UIFonts.Get(selected ? UiFont.Mono : UiFont.MonoRegular);
                view.badge.color = selected ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#bbc6d8");
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

        /// <summary>Hydrates account-owned appearance without replacing an explicit in-flight choice.</summary>
        public bool ApplyAccountRigPreference(BodyRig rig)
        {
            if (BodyRigStore.SyncPending || (rigChoicePanel != null && rigChoiceInteracted)) return false;
            BodyRigStore.ApplyAccountPreference(rig);
            if (rigChoicePanel != null)
            {
                selectedRig = rig;
                RefreshRigSelection();
            }
            return true;
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
            identityRefreshRevision++;
            PersistChosenBodyRig(rig);
            HideScreen(rigChoicePanel);
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
            var screen = Hotdrop.UI.MenuLayout.Create(root.transform, "StudyPanel", L("study.title", "Pad Study — Facilitator"), () => studyPanel.SetActive(false));
            studyPanel = screen.Root;
            var content = screen.Content;
            studyStatus = Hotdrop.UI.MenuLayout.AddText(content, "", 22, Hotdrop.UI.MatchHUD.MainText, "StudyStatus");
            var versions = Hotdrop.UI.MenuLayout.Row(content);
            Hotdrop.UI.MenuLayout.AddButton(versions, L("study.version.withPads", "Version: With Pads"), () => { StudySettings.SetCohort(StudySettings.PadsOn); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(versions, L("study.version.withoutPads", "Version: Without Pads"), () => { StudySettings.SetCohort(StudySettings.PadsOff); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, L("study.normalPlay", "Normal Play (Not Study)"), () => { StudySettings.SetCohort("default"); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, L("study.toggleIncentivizedTester", "Toggle Incentivized Tester Flag"), () => { StudySettings.SetIncentivizedTester(!StudySettings.IncentivizedTester); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, L("study.newParticipant", "New Participant (Replay Tutorial)"), () =>
            {
                PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
                Telemetry.Track("study_participant_started");
                studyPanel.SetActive(false);
                root.SetActive(false);
                game.touchInput.enabled = true;
                game.StartTutorial();
            });
            Hotdrop.UI.MenuLayout.AddText(content, F("study.collectDataTemplate", "Collect data:  adb pull {0}", "/sdcard/Android/data/" + Application.identifier + "/files/" + Telemetry.QueueFileName), 20, Hotdrop.UI.MatchHUD.SecondaryText);
            screen.Status.text = L("study.noRewardRule", "Never reward participants for choosing either version (spec section 25).");
            RefreshStudy();
        }

        void RefreshStudy()
        {
            if (studyStatus == null) return;
            bool tutorialWillRun = PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0;
            studyStatus.text = F("study.statusTemplate",
                "Cohort: {0}   ·   Pads {1}   ·   Incentivized: {2}   ·   Queued events: {3}   ·   Tutorial will {4} next launch",
                Telemetry.Cohort,
                L(StudySettings.PadsEnabled ? "study.padsOn" : "study.padsOff", StudySettings.PadsEnabled ? "ON" : "OFF"),
                L(Telemetry.IncentivizedTester ? "study.incentivized" : "study.notIncentivized", Telemetry.IncentivizedTester ? "YES" : "NO"),
                Telemetry.ReadQueued().Length,
                L(tutorialWillRun ? "study.tutorialWillRun" : "study.tutorialWillNotRun", tutorialWillRun ? "run" : "not run"));
        }
    }
}
