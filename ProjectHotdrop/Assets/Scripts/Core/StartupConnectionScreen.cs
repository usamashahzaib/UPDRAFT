using System;
using Hotdrop.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Hotdrop.Player;

namespace Hotdrop.Core
{
    /// <summary>Brief, actionable first screen. It never simulates a connection progress bar:
    /// every non-normal state explains what happened and retains local play when possible.</summary>
    public sealed class StartupConnectionScreen : MonoBehaviour
    {
        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleMediumDp;
        static string L(string key, string fallback) => UiDraw.L("startup." + key, fallback);
        static string Format(string key, string fallback, params object[] args) =>
            UiDraw.LFormat("startup." + key, fallback, args);
        GameObject panel;
        GameBootstrap game;
        bool advanced;
        float normalAdvanceAt;
        GameObject recoveryActions;
        HotdropButton recoveryActionButton;
        bool held;
        TextMeshProUGUI frameLabel;
        string frameLabelText;
        int showRevision;
        StartupConnectionState visibleState;
        string remoteStatusDetail;
        string remoteStoreUrl;

        /// <summary>How long a regular branded connection moment stays up before advancing.</summary>
        public const float NormalHoldSeconds = 1.1f;
        const float FirstFlightQuestionHoldSeconds = 2.5f;
        bool firstFlightQuestion;

        public void Show(GameBootstrap bootstrap)
        {
            game = bootstrap;
            int revision = ++showRevision;
            advanced = false;
            firstFlightQuestion = game != null && game.ShouldShowFirstFlightQuestion;
            if (firstFlightQuestion) game.MarkFirstFlightQuestionSeen();
            var state = ResolveState();
            visibleState = state;
            remoteStatusDetail = null;
            remoteStoreUrl = null;
            if (panel == null) Build(state);
            else
            {
                panel.SetActive(true);
                Hotdrop.UI.UiScreenEnterMotion.Reopen(panel);
                Refresh(state);
            }

            // Normal launches should feel immediate but the branded connection moment remains
            // available for a screenshot and does not block automated or keyboard-free starts.
            float holdSeconds = firstFlightQuestion ? FirstFlightQuestionHoldSeconds : NormalHoldSeconds;
            normalAdvanceAt = state == StartupConnectionState.Normal ? Time.unscaledTime + holdSeconds : -1f;
            var client = BackendClient.Instance;
            if (Application.internetReachability != NetworkReachability.NotReachable &&
                (client.HasSession || client.HasSavedSession))
                _ = RefreshAccountRigPreferenceAsync(revision, client);
            if (state == StartupConnectionState.Normal && Application.internetReachability != NetworkReachability.NotReachable)
                _ = RefreshRemoteAvailabilityAsync(revision);
        }

        async Task RefreshAccountRigPreferenceAsync(int revision, BackendClient client)
        {
            BackendResult<ProfileDto> profile;
            try { profile = await client.GetProfileAsync(); }
            catch (Exception exception)
            {
                Debug.LogWarning($"Account body-rig preference could not be loaded: {exception.Message}");
                return;
            }

            if (this == null || revision != showRevision || !profile.Ok || profile.Value == null ||
                BodyRigStore.SyncPending || !BodyRigs.TryParse(profile.Value.bodyRigId, out var accountRig)) return;

            var home = game != null ? game.GetComponent<OfflineHomeScreen>() : null;
            if (home != null) home.ApplyAccountRigPreference(accountRig);
            else BodyRigStore.ApplyAccountPreference(accountRig);
        }

        void Update()
        {
            // The composed frame is not known when the screen is built - an evidence capture pins it
            // moments later - so the annotation follows the frame instead of freezing a guess.
            RefreshFrameLabel();
            Animate();
            if (held) return;
            if (!advanced && normalAdvanceAt > 0f && Time.unscaledTime >= normalAdvanceAt) Continue();
        }

        static string FrameLabelText()
        {
            var frame = SafeAreaRoot.ScreenSize;
            return Format("frameLabelTemplate", "ANDROID LANDSCAPE  •  {0}×{1}", Mathf.RoundToInt(frame.x), Mathf.RoundToInt(frame.y));
        }

        /// <summary>
        /// Re-reads the composed frame into the top-right annotation.
        ///
        /// <para>Public because the one moment it has to be right is the moment an evidence capture
        /// pins the simulated screen - <see cref="Update"/> never runs between that pin and the
        /// render, so the harness asks for the refresh itself. A player sees the same value the
        /// device reports, refreshed every frame as the frame can change on rotation.</para>
        /// </summary>
        public void RefreshFrameLabel()
        {
            if (frameLabel == null) return;
            var text = FrameLabelText();
            if (text == frameLabelText) return;
            frameLabelText = text;
            frameLabel.text = text;
        }

        /// <summary>
        /// Evidence hook: hold the branded startup frame until CONTINUE calls it off, and bring it
        /// back if the self-advance already fired.
        ///
        /// <para>The screen advances itself after <see cref="NormalHoldSeconds"/>, which is right for
        /// a player and wrong for a screenshot: in a batch-mode PlayMode run the scene load and test
        /// setup can outlast the hold, so the capture photographed whatever the game had already
        /// moved on to (the runner choice) and labelled it "01_startup". Re-showing the real screen
        /// through its own <see cref="Show"/> is what makes the evidence deterministic; nothing here
        /// changes what a player sees.</para>
        /// </summary>
        public void HoldForCapture(bool hold)
        {
            held = hold;
            if (!hold)
            {
                normalAdvanceAt = Time.unscaledTime + NormalHoldSeconds;
                return;
            }
            if (!advanced && panel != null && panel.activeSelf)
            {
                normalAdvanceAt = -1f;
                return;
            }
            advanced = false;
            Show(game);
            normalAdvanceAt = -1f;
            // If the self-advance already ran, the screen it moved to is a full-screen sibling
            // built after this one, so re-showing this panel alone would photograph that screen
            // instead. Raising the panel puts the real first frame back in front of it; the
            // caller's CONTINUE then rebuilds the next screen through its own entry point.
            panel.transform.SetAsLastSibling();
        }

        public void Retry()
        {
            var previousPanel = panel;
            panel = null;
            if (previousPanel != null)
                Hotdrop.UI.UiScreenEnterMotion.Exit(previousPanel, () => Destroy(previousPanel));
            Show(game);
        }

        public void Continue()
        {
            if (advanced || game == null) return;
            advanced = true;
            HidePanel();
            game.ContinueFromStartup();
        }

        public void PlayOffline()
        {
            if (advanced || game == null) return;
            advanced = true;
            HidePanel();
            game.StartOfflinePractice();
        }

        void HidePanel()
        {
            var closingPanel = panel;
            if (closingPanel == null) return;
            Hotdrop.UI.UiScreenEnterMotion.Exit(closingPanel, () =>
            {
                if (closingPanel != null) closingPanel.SetActive(false);
            });
        }

        StartupConnectionState ResolveState()
        {
            var supplied = OnlineHooks.StartupState?.Invoke();
            if (supplied.HasValue) return supplied.Value;
            return Application.internetReachability == NetworkReachability.NotReachable
                ? StartupConnectionState.NoInternet : StartupConnectionState.Normal;
        }

        async Task RefreshRemoteAvailabilityAsync(int revision)
        {
            BackendResult<ClientStatusDto> result;
            try { result = await BackendClient.Instance.ClientStatusAsync(); }
            catch (Exception exception)
            {
                Debug.LogWarning($"Backend availability check failed: {exception.Message}");
                ShowRemoteAvailabilityFailure(revision, StartupConnectionState.ServiceUnavailable);
                return;
            }
            if (this == null || revision != showRevision || advanced || panel == null || !panel.activeSelf) return;

            if (result.Error == BackendError.UpdateRequired)
            {
                visibleState = StartupConnectionState.UpdateRequired;
                remoteStoreUrl = SafeStoreUrl(result.Value?.storeUrl);
                remoteStatusDetail = result.Value != null && result.Value.minClientProtocol > 0
                    ? Format("protocolRequiredTemplate", "Install protocol {0} or newer to play online.", result.Value.minClientProtocol)
                    : null;
            }
            else if (result.Ok && result.Value != null && result.Value.state == "maintenance")
            {
                visibleState = StartupConnectionState.Maintenance;
                remoteStatusDetail = FormatRemoteStatus(result.Value.message, result.Value.etaUtc, result.Value.compensationNote);
            }
            else if (result.Ok && result.Value != null && result.Value.state == "queue_paused")
            {
                visibleState = StartupConnectionState.ServiceUnavailable;
                remoteStatusDetail = FormatRemoteStatus(result.Value.message, null, null);
            }
            else if (result.Ok && result.Value != null && result.Value.state == "available") return;
            else
            {
                ShowRemoteAvailabilityFailure(revision, result.Error == BackendError.NoInternet
                    ? StartupConnectionState.NoInternet : StartupConnectionState.ServiceUnavailable);
                return;
            }

            normalAdvanceAt = -1f;
            Refresh(visibleState);
        }

        void ShowRemoteAvailabilityFailure(int revision, StartupConnectionState state)
        {
            if (this == null || revision != showRevision || advanced || panel == null || !panel.activeSelf) return;
            visibleState = state;
            remoteStatusDetail = null;
            normalAdvanceAt = -1f;
            Refresh(state);
        }

        static string FormatRemoteStatus(string message, string etaUtc, string compensationNote)
        {
            string detail = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (!string.IsNullOrWhiteSpace(etaUtc)) detail += Format("status.expectedUpdateTemplate", "\nExpected update: {0} UTC.", etaUtc.Trim());
            if (!string.IsNullOrWhiteSpace(compensationNote)) detail += $"\n{compensationNote.Trim()}";
            return detail;
        }

        TextMeshProUGUI statusLine;
        Image beamFill;
        RectTransform ringPulse;
        float shownAt;

        // Screen 01 is rebuilt from its UIUX HTML: every number below is the reference's CSS px
        // (one canvas unit each). The stack is centred 21 px above the frame centre, which is
        // where the reference's header, footer and -mt-6 offset put it.
        const float SideInset = 32f, EdgeInset = 24f;

        void Build(StartupConnectionState state)
        {
            var screen = MenuLayout.Create(game.matchHUD.transform, "Startup", "HOTDROP", null);
            panel = screen.Root;
            panel.transform.Find("Nav_Bar").gameObject.SetActive(false);
            panel.transform.Find("Nav_Scroll").gameObject.SetActive(false);
            panel.transform.Find("StatusFill").gameObject.SetActive(false);
            screen.Status.gameObject.SetActive(false);
            var root = (RectTransform)panel.transform;
            BuildBackdrop(root);

            // Header: pulsing dot + relay caption left, frame annotation right.
            var dot = UiDraw.Box(root, "LiveDot", UiDraw.Hex("#30d5e8", .7f), 4f);
            UiDraw.Place(dot.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(SideInset, -EdgeInset - 8f), new Vector2(8f, 8f));
            var caption = UiDraw.Text(root, "UpperLeft", L("header.caption", "Air-Ground Kinetic Relay"), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#7f8ca3"), .1f);
            UiDraw.Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(SideInset + 16f, -EdgeInset - 8f), new Vector2(400f, 16f));
            frameLabel = UiDraw.Text(root, "UpperRight", FrameLabelText(), UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#7f8ca3", .8f), .1f,
                TextAlignmentOptions.MidlineRight);
            UiDraw.Place(frameLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-SideInset, -EdgeInset - 8f), new Vector2(400f, 16f));

            // Central stack: wordmark (96 + mb 24), route loader (my 8 + 96), status block.
            var stack = UiDraw.Place(UiDraw.Node(root, "Stage"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 21f), new Vector2(512f, 310f));
            UiDraw.Place(UiDraw.Art(stack, "BrandLockup", "startup_wordmark", Vector2.zero).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -48f), new Vector2(400f, 156f));
            var loader = UiDraw.Place(UiDraw.Node(stack, "RouteMotif"), new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -176f), new Vector2(360f, 130f));
            UiDraw.Fill(UiDraw.Art(loader, "RouteBase", "startup_loader_base", Vector2.zero).rectTransform);
            // The beam grows out of the pad along the route and restarts, as the reference's 3 s loop.
            beamFill = UiDraw.ArtSprite(loader, "RouteBeam", "startup_loader_beam", Vector2.zero);
            UiDraw.Fill(beamFill.rectTransform);
            beamFill.type = Image.Type.Filled;
            beamFill.fillMethod = Image.FillMethod.Horizontal;
            beamFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            ringPulse = UiDraw.Place(UiDraw.Node(loader, "RelayRingPivot"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(120f, 0f), Vector2.zero);
            UiDraw.Place(UiDraw.Art(ringPulse, "RelayRing", "startup_loader_ring", Vector2.zero).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-120f, 0f), new Vector2(360f, 130f));
            string preparingCopy = firstFlightQuestion
                ? L("firstFlightQuestion", "Do you want to fly?")
                : L("preparing", "Preparing the Rooftop");
            var preparing = UiDraw.Text(stack, "Preparing", preparingCopy, UiFont.Heading,
                firstFlightQuestion ? TitleDp : UiDp, UiDraw.Hex("#f4f7fb"), .25f,
                TextAlignmentOptions.Midline);
            UiDraw.Place(preparing.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -258f),
                new Vector2(512f, firstFlightQuestion ? 26f : 20f));
            UiDraw.Place(UiDraw.Art(stack, "RouteProgress", "startup_progress", Vector2.zero).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -279f), new Vector2(296f, 46f));
            statusLine = UiDraw.Text(stack, "Nav_Status", "", UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#bbc6d8"), 0f, TextAlignmentOptions.Midline);
            statusLine.richText = false;
            UiDraw.Place(statusLine.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -302f), new Vector2(720f, 16f));

            // Footer: slogan left, gesture pill centre, build chip right.
            var slogan = UiDraw.Text(root, "Tagline", L("tagline", "Three players. One team. Three minutes."), UiFont.HeadingSemiBold, CaptionDp,
                UiDraw.Hex("#bbc6d8"), .025f);
            UiDraw.Place(slogan.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(SideInset, EdgeInset + 26.5f), new Vector2(400f, 16f));
            var sub = UiDraw.Text(root, "TaglineSub", L("taglineSub", "Build shortcuts your enemies can steal."), UiFont.Body, CaptionDp, UiDraw.Hex("#bbc6d8"), .025f);
            UiDraw.Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(SideInset, EdgeInset + 8.25f), new Vector2(400f, 16.5f));
            var pill = UiDraw.Box(root, "SafeZonePill", UiDraw.Hex("#49627b", .3f), 2f);
            UiDraw.Place(pill.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, EdgeInset + 19.25f), new Vector2(144f, 4f));
            BuildChip(root);

            recoveryActions = new GameObject("RecoveryActions", typeof(RectTransform));
            recoveryActions.transform.SetParent(panel.transform, false);
            var recovery = recoveryActions.GetComponent<RectTransform>();
            recovery.anchorMin = recovery.anchorMax = new Vector2(0.5f, 0.5f);
            recovery.anchoredPosition = new Vector2(0f, -200f);
            recovery.sizeDelta = new Vector2(640f, 52f);
            RecoveryButton(recovery, L("button.continue", "Continue"), Continue, "Continue", -210f);
            RecoveryButton(recovery, L("button.playOffline", "Play Offline"), PlayOffline, "Offline", 0f);
            recoveryActionButton = RecoveryButton(recovery, L("button.retryConnection", "Retry Connection"), OpenStoreOrRetry, "Retry", 210f);
            shownAt = Time.unscaledTime;
            Animate();
            Refresh(state);
        }

        /// <summary>The reference's two loops: the route dash (3 s) and the relay ring pulse (2.4 s).
        /// Phase 0 is the reference frame, so a capture taken on show matches the comp.</summary>
        void Animate()
        {
            if (beamFill == null) return;
            float t = Time.unscaledTime - shownAt;
            // Eased 0..1 across 3 s; phase 0 holds the reference frame, head at 54% of the art.
            float u = Mathf.Repeat(t / 3f + 0.50f, 1f);
            float eased = u < .5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) / 2f;
            beamFill.fillAmount = Mathf.Lerp(.08f, .80f, eased);
            float p = .5f - .5f * Mathf.Cos(Mathf.Repeat(t / 2.4f + .5f, 1f) * Mathf.PI * 2f);
            ringPulse.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, p);
            var ring = ringPulse.GetChild(0).GetComponent<RawImage>();
            ring.color = new Color(1f, 1f, 1f, Mathf.Lerp(.8f, 1f, p));
        }

        /// <summary>
        /// Bottom-right version chip: mono 11 px on a #1b2940/80 plate with a 4 px radius and an
        /// outline/40 rail. It prints the real <see cref="Application.version"/> so support can ask
        /// a player for it.
        /// </summary>
        static void BuildChip(Transform parent)
        {
            string value = Format("buildChipTemplate", "BUILD v{0}", Application.version);
            var chip = UiDraw.Box(parent, "BuildChip", UiDraw.Hex("#1b2940", .8f), 4f, UiDraw.Hex("#49627b", .4f));
            var text = UiDraw.Text(chip.transform, "Version", value, UiFont.MonoRegular, CaptionDp, UiDraw.Hex("#7f8ca3"), 0f, TextAlignmentOptions.Midline);
            UiDraw.Fill(text.rectTransform);
            float width = text.GetPreferredValues(value).x + 22f;
            UiDraw.Place(chip.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-SideInset, EdgeInset), new Vector2(width, 26.5f));
        }

        static Texture2D verticalShade, radialShade;

        /// <summary>
        /// The reference backdrop: the venue photo at 45% brightness, a bottom-to-top shade
        /// (#060e1d -> #0b1322/80 -> #0b1322/40), an elliptical vignette to #060e1d, and a 48 px
        /// cyan grid at 7%.
        /// </summary>
        static void BuildBackdrop(RectTransform root)
        {
            var baseFill = UiDraw.Box(root, "StartupBase", UiDraw.Hex("#0b1322"));
            UiDraw.Fill(baseFill.rectTransform);
            var rooftop = UiPresentationProfile.LoadRooftopArtwork();
            if (rooftop != null)
            {
                var photo = UiDraw.Node(root, "StartupRooftop").gameObject.AddComponent<RawImage>();
                photo.texture = rooftop;
                photo.color = new Color(.42f, .43f, .45f, 1f);
                photo.raycastTarget = false;
                UiDraw.Fill(photo.rectTransform);
            }
            if (verticalShade == null)
            {
                var deep = UiDraw.Hex("#060e1d");
                var mid = UiDraw.Hex("#0b1322");
                verticalShade = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int y = 0; y < 256; y++)
                {
                    float v = y / 255f; // 0 = bottom
                    Color c = v < .5f
                        ? Color.Lerp(new Color(deep.r, deep.g, deep.b, 1f), new Color(mid.r, mid.g, mid.b, .8f), v * 2f)
                        : Color.Lerp(new Color(mid.r, mid.g, mid.b, .8f), new Color(mid.r, mid.g, mid.b, .4f), (v - .5f) * 2f);
                    verticalShade.SetPixel(0, y, c);
                }
                verticalShade.Apply(false, true);
                const int n = 128;
                radialShade = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // ellipse at centre, farthest-corner: each corner sits at distance 1.
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                    radialShade.SetPixel(x, y, new Color(deep.r, deep.g, deep.b, Mathf.InverseLerp(.40f, .95f, d)));
                }
                radialShade.Apply(false, true);
            }
            foreach (var (name, tex) in new[] { ("StartupShade", verticalShade), ("StartupVignette", radialShade) })
            {
                var layer = UiDraw.Node(root, name).gameObject.AddComponent<RawImage>();
                UiDraw.Fill(layer.rectTransform);
                layer.texture = tex;
                layer.raycastTarget = false;
            }
            var grid = UiDraw.Node(root, "StartupGrid");
            UiDraw.Fill(grid);
            grid.gameObject.AddComponent<RectMask2D>();
            var gridColor = UiDraw.Hex("#30d5e8", .07f);
            for (int i = 0; i < 60; i++)
            {
                var v = UiDraw.Box(grid, "GridV", gridColor).rectTransform;
                v.anchorMin = new Vector2(0f, 0f); v.anchorMax = new Vector2(0f, 1f); v.pivot = new Vector2(0f, .5f);
                v.sizeDelta = new Vector2(1f, 0f); v.anchoredPosition = new Vector2(i * 48f, 0f);
                var h = UiDraw.Box(grid, "GridH", gridColor).rectTransform;
                h.anchorMin = new Vector2(0f, 1f); h.anchorMax = new Vector2(1f, 1f); h.pivot = new Vector2(.5f, 1f);
                h.sizeDelta = new Vector2(0f, 1f); h.anchoredPosition = new Vector2(0f, -i * 48f);
            }
        }

        static HotdropButton RecoveryButton(Transform parent, string caption, Action onClick, string name, float x)
        {
            var button = UIKit.Button(parent, caption, ButtonVariant.Secondary, onClick, name);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(196f, 52f);
            return button;
        }

        void OpenStoreOrRetry()
        {
            if (!string.IsNullOrEmpty(remoteStoreUrl)) Application.OpenURL(remoteStoreUrl);
            else Retry();
        }

        static string SafeStoreUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                ? uri.AbsoluteUri : string.Empty;

        void Refresh(StartupConnectionState state)
        {
            var status = statusLine;
            if (status == null) return;
            string message = state switch
            {
                StartupConnectionState.NoInternet => L("state.noInternet", "NO INTERNET — local Offline play is ready."),
                StartupConnectionState.ServiceUnavailable => L("state.serviceUnavailable", "SERVICE UNAVAILABLE — retry or play Offline."),
                StartupConnectionState.UpdateRequired => L("state.updateRequired", "UPDATE REQUIRED FOR ONLINE PLAY — Offline remains available."),
                StartupConnectionState.Maintenance => L("state.maintenance", "SERVICE MAINTENANCE — local Offline play is ready."),
                StartupConnectionState.RecoveringLocalSave => L("state.recoveringLocalSave", "RECOVERING LOCAL SAVE — your saved training progress is kept safe."),
                _ => L("state.synchronizing", "Synchronizing Relay protocol nodes..."),
            };
            if (!string.IsNullOrWhiteSpace(remoteStatusDetail)) message += "\n" + remoteStatusDetail;
            status.text = message;
            if (recoveryActionButton != null)
                recoveryActionButton.SetCaption(state == StartupConnectionState.UpdateRequired && !string.IsNullOrEmpty(remoteStoreUrl)
                    ? L("button.updateGame", "Update Game") : L("button.retryConnection", "Retry Connection"));
            if (recoveryActions != null) recoveryActions.SetActive(state != StartupConnectionState.Normal);
        }
    }
}
