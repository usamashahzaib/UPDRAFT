using System;
using System.Collections.Generic;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>What the support page's controls do; the screens layer owns the backend calls.</summary>
    public sealed class SupportHubActions
    {
        public Action Back, Recover, Link, Revoke, AccountSettings, WebDeletion, Profile, Diagnostics, Submit, Purge, RetryAccount;
        public Action<string> Navigate;
        public Action<RecentMatch.Entry> Choose;
    }

    /// <summary>
    /// Screen 17 account security, recovery and integrity, rebuilt from its UIUX HTML (CSS px) as a
    /// scrolling three-column page under a fixed top bar. Every value is the account's or the last
    /// online match's own; nothing the backend does not hold (UIDs, ping, premium currency) is shown.
    /// </summary>
    public static class SupportHubView
    {
        static string L(string key, string fallback) => UiDraw.L("support." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("support." + key, fallback, args);

        static Color Surface => UIDesignTokens.Background;
        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color Mid => Color.Lerp(UIDesignTokens.Background, UIDesignTokens.SurfacePanel, .55f);
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Highest => UIDesignTokens.SurfaceDisabled;
        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color PrimaryC => UIDesignTokens.PrimaryAction;
        static Color OnPrimary => UIDesignTokens.TextOnAccent;
        static Color OnPrimaryC => UIDesignTokens.TextOnAccent;
        static Color Secondary => UIDesignTokens.SignalWarning;
        static Color SecondaryC => UiPresentationProfile.Palette.WarningDim;
        static Color OnSecondary => UIDesignTokens.TextOnAccent;
        static Color OnSecondaryC => UIDesignTokens.TextPrimary;
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color OutlineVariant => UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .45f);

        const float Pad = 24f, Gap = 12f, BarH = 64f, NavRowH = 52f;
        // Below this page width (in dp) the three columns cannot hold their dp-sized text.
        const float ThreeColumnMinDp = 1100f;
        // Three lines of 17 dp mono plus the field's padding, so the hint is never cut off.
        const float IncidentLogH = 96f;
        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleMediumDp;
        const string Root = "SupportHubVisual";

        static UnityEngine.Events.UnityAction<string> counterListener;
        static TMP_InputField counterSource;
        // A null profile is either still loading or failed to load; the page must say which
        // instead of showing a permanent "syncing" state after the account request fails.
        static bool accountUnavailable;

        /// <summary>Label for an account value that is not loaded: loading or unavailable.</summary>
        static string Missing(string loading, string unavailable) => accountUnavailable ? unavailable : loading;

        public static void Clear(GameObject owner)
        {
            var visual = owner != null ? owner.transform.Find(Root) : null;
            if (visual == null) return;
            // The report inputs belong to the screens layer; keep them alive across redraws.
            foreach (var input in visual.GetComponentsInChildren<TMP_InputField>(true))
                if (input.name != "ConfirmDelete")
                {
                    input.transform.SetParent(owner.transform, false);
                    input.gameObject.SetActive(false);
                }
            if (counterSource != null && counterListener != null) counterSource.onValueChanged.RemoveListener(counterListener);
            counterSource = null; counterListener = null;
            // Destroy is deferred to the end of the frame. Detach and hide the old page now so a
            // same-frame rebuild (resize, synchronous capture) neither finds nor draws it.
            visual.gameObject.SetActive(false);
            visual.SetParent(null, false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(visual.gameObject);
            else UnityEngine.Object.DestroyImmediate(visual.gameObject);
        }

        /// <summary>Shows a message in the bottom toast; an empty message hides it.</summary>
        public static void SetStatus(GameObject owner, string message)
        {
            var toast = owner != null ? owner.transform.Find(Root + "/Toast") : null;
            if (toast == null) return;
            var label = toast.Find("Text")?.GetComponent<TextMeshProUGUI>();
            bool show = !string.IsNullOrEmpty(message);
            toast.gameObject.SetActive(show);
            if (label == null || !show) return;
            label.text = message;
            float width = ((RectTransform)toast.parent).rect.width;
            float w = Mathf.Min(UiDraw.Width(label) + 64f, (width > 0f ? width : 1280f) - Pad * 2f);
            ((RectTransform)toast).sizeDelta = new Vector2(w, 44f);
            label.rectTransform.sizeDelta = new Vector2(w - 56f, 20f);
        }

        /// <summary>Opens the typed second step of account deletion (after the 3-second hold).</summary>
        public static void RevealPurge(GameObject owner)
        {
            var gate = owner != null ? owner.transform.Find(Root + "/Viewport/Page/Purge") : null;
            if (gate == null) return;
            var stage = gate.Find("StageTwo");
            if (stage == null || stage.gameObject.activeSelf) return;
            stage.gameObject.SetActive(true);
            var card = (RectTransform)gate;
            card.sizeDelta = new Vector2(card.sizeDelta.x, card.sizeDelta.y + ((RectTransform)stage).sizeDelta.y + Gap);
            var status = gate.Find("HoldStatus")?.GetComponent<TextMeshProUGUI>();
            if (status != null) status.text = L("purge.gateOpen", "Gate Open");
            SetCaption(gate.Find("DeleteAccount/Content/Label")?.GetComponent<TextMeshProUGUI>(), L("purge.safetyReleaseActive", "Safety Release Active"));
            var hold = gate.Find("DeleteAccount")?.GetComponent<CanvasGroup>();
            if (hold != null) hold.alpha = .5f;
            // The opened step lengthens its column: push the footer and page end down when needed,
            // instead of reserving empty space for it while the gate is closed.
            var page = gate.parent as RectTransform;
            var foot = page != null ? page.Find("PageFooter") as RectTransform : null;
            if (foot == null) return;
            float need = -card.anchoredPosition.y + card.sizeDelta.y + 32f + foot.anchoredPosition.y;
            if (need <= 0f) return;
            foot.anchoredPosition -= new Vector2(0f, need);
            page.sizeDelta += new Vector2(0f, need);
        }

        /// <summary>
        /// The page is placed in absolute canvas units from the width it is built at, so it must be
        /// rebuilt when the canvas width changes after it opened (resolution or density change, split
        /// screen, synchronous capture). The rebuild keeps the player's in-progress choices.
        /// </summary>
        sealed class Resize : MonoBehaviour
        {
            internal GameObject owner;
            internal TMP_InputField category, detail;
            internal string matchId;
            internal IReadOnlyList<RecentMatch.Entry> roster;
            internal int localTeam;
            internal ProfileDto profile;
            internal WalletDto wallet;
            internal bool online, accountUnavailable, walletUnavailable;
            internal bool builtStreamSafe;
            internal SupportHubActions act;
            internal float builtWidth, builtScale;

            internal void RefreshIfNeeded()
            {
                if (owner == null || !gameObject.activeInHierarchy) return;
                float width = owner.GetComponent<RectTransform>().rect.width;
                float scale = UiMetrics.UnitsForDp(1f);
                bool streamSafe = StreamSafePresentation.Enabled;
                if (width <= 0f || (Mathf.Abs(width - builtWidth) <= 4f && Mathf.Abs(scale - builtScale) <= .001f && streamSafe == builtStreamSafe)) return;
                var root = transform;
                string reason = category != null ? category.text : null;
                var stage = root.Find("Viewport/Page/Purge/StageTwo");
                bool purgeOpen = stage != null && stage.gameObject.activeSelf;
                string typed = stage != null ? stage.GetComponentInChildren<TMP_InputField>(true)?.text : null;
                var toast = root.Find("Toast");
                string toastText = toast != null && toast.gameObject.activeSelf
                    ? toast.Find("Text")?.GetComponent<TextMeshProUGUI>()?.text : null;
                var page = root.Find("Viewport/Page") as RectTransform;
                float scrollY = page != null ? page.anchoredPosition.y : 0f;

                Build(owner, category, detail, matchId, roster, localTeam, profile, wallet, online, act,
                    accountUnavailable, walletUnavailable, reason);
                if (purgeOpen)
                {
                    RevealPurge(owner);
                    var input = owner.transform.Find(Root + "/Viewport/Page/Purge/StageTwo")?.GetComponentInChildren<TMP_InputField>(true);
                    if (input != null && !string.IsNullOrEmpty(typed)) input.text = typed;
                }
                if (!string.IsNullOrEmpty(toastText)) SetStatus(owner, toastText);
                var rebuilt = owner.transform.Find(Root + "/Viewport/Page") as RectTransform;
                var view = owner.transform.Find(Root + "/Viewport") as RectTransform;
                if (rebuilt != null && view != null)
                    rebuilt.anchoredPosition = new Vector2(0f, Mathf.Clamp(scrollY, 0f, Mathf.Max(0f, rebuilt.sizeDelta.y - view.rect.height)));
            }

            void LateUpdate() => RefreshIfNeeded();
        }

        /// <summary>Rebuilds a visible Support page whose canvas width changed. Also called by
        /// synchronous screenshot rendering, which resizes the canvas without a LateUpdate.</summary>
        public static void RefreshVisible(Canvas canvas)
        {
            if (canvas == null) return;
            foreach (var page in canvas.GetComponentsInChildren<Resize>()) page.RefreshIfNeeded();
        }

        /// <summary>Scrolls a visible Support page to a fraction (0 top, 1 end) of its range.</summary>
        public static void ScrollTo(Canvas canvas, float fraction)
        {
            if (canvas == null) return;
            foreach (var page in canvas.GetComponentsInChildren<Resize>())
            {
                var content = page.transform.Find("Viewport/Page") as RectTransform;
                var view = page.transform.Find("Viewport") as RectTransform;
                if (content == null || view == null) continue;
                float range = Mathf.Max(0f, content.sizeDelta.y - view.rect.height);
                content.anchoredPosition = new Vector2(0f, range * Mathf.Clamp01(fraction));
            }
        }

        // Selected roster entry, kept so a resize rebuild re-marks it; -1 when none.
        static int chosenParticipant = -1;

        public static void Show(GameObject owner, TMP_InputField category, TMP_InputField detail,
            string matchId, IReadOnlyList<RecentMatch.Entry> roster, int localTeam,
            ProfileDto profile, WalletDto wallet, bool online, SupportHubActions act,
            bool accountUnavailable = false, bool walletUnavailable = false, int selectedSlot = -1)
        {
            // A fresh draw from the screens layer clears the report target, so it clears the marks too.
            chosenParticipant = -1;
            if (selectedSlot >= 0 && roster != null)
                for (int i = 0; i < roster.Count; i++)
                    if (roster[i] != null && roster[i].slot == selectedSlot) { chosenParticipant = i; break; }
            Build(owner, category, detail, matchId, roster, localTeam, profile, wallet, online, act,
                accountUnavailable, walletUnavailable, null);
        }

        static void Build(GameObject owner, TMP_InputField category, TMP_InputField detail,
            string matchId, IReadOnlyList<RecentMatch.Entry> roster, int localTeam,
            ProfileDto profile, WalletDto wallet, bool online, SupportHubActions act,
            bool accountUnavailable, bool walletUnavailable, string keepReason)
        {
            Clear(owner);
            SupportHubView.accountUnavailable = profile == null && accountUnavailable;
            if (owner == null) return;
            act ??= new SupportHubActions();
            // The page is authored in dp (its CSS px), so the root is laid out at the screen's dp size
            // and scaled up to canvas units. Boxes, icons, 48 dp hit areas and 12/17/22 dp text then
            // share one unit at any density, instead of dp text overflowing px-sized boxes.
            var ownerRect = owner.GetComponent<RectTransform>().rect;
            float canvasWidth = ownerRect.width > 0f ? ownerRect.width : UiMetrics.DesignWidth;
            float canvasHeight = ownerRect.height > 0f ? ownerRect.height : UiMetrics.DesignHeight;
            float unitsPerDp = Mathf.Max(.01f, UiMetrics.UnitsForDp(1f));
            float width = canvasWidth / unitsPerDp;
            Debug.Log($"[SUPPORTDBG] unitsPerDp={unitsPerDp} canvasW={canvasWidth} screen={UiMetrics.CurrentScreen} ownerLossy={owner.transform.lossyScale}");
            var root = UiDraw.Node(owner.transform, Root);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f); root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(width, canvasHeight / unitsPerDp);
            root.localScale = new Vector3(unitsPerDp, unitsPerDp, 1f);
            float previousTextScale = UiDraw.TextUnitScale;
            UiDraw.TextUnitScale = unitsPerDp;
            try
            {
                UiDraw.Fill(UiDraw.Box(root, "Base", Surface).rectTransform);

                var viewport = UiDraw.Node(root, "Viewport");
                UiDraw.Fill(viewport, 0f, BarH, 0f, 0f);
                viewport.gameObject.AddComponent<RectMask2D>();
                viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                var page = UiDraw.Node(viewport, "Page");
                page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.content = page; scroll.viewport = viewport; scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;

                bool linked = profile != null && !profile.isGuest;
                float top = Title(page, width, profile, online, act) + 20f, end;
                if (width >= ThreeColumnMinDp)
                {
                    float colW = (width - Pad * 2f - Gap * 2f) / 3f;
                    float lh = Identity(page, Pad, top, colW, profile, matchId, act, walletUnavailable);
                    float mh = Report(page, Pad + colW + Gap, top, colW, category, detail, roster, localTeam, act, keepReason);
                    float rh = Compliance(page, Pad + (colW + Gap) * 2f, top, colW, act);
                    end = top + Mathf.Max(lh, mh, rh) + 32f;
                }
                else
                {
                    // Phone landscape (about 760-960 dp): account then compliance on the left, the
                    // report form on the right.
                    float colW = (width - Pad * 2f - Gap) / 2f;
                    float lh = Identity(page, Pad, top, colW, profile, matchId, act, walletUnavailable);
                    float rh = Compliance(page, Pad, top + lh + Gap, colW, act);
                    float mh = Report(page, Pad + colW + Gap, top, colW, category, detail, roster, localTeam, act, keepReason);
                    end = top + Mathf.Max(lh + Gap + rh, mh) + 32f;
                }
                float footH = PageFooter(page, width, end, profile, linked);
                page.sizeDelta = new Vector2(width, end + footH);
                page.anchoredPosition = Vector2.zero;

                float barHeight = TopBar(root, width, profile, wallet, online, act, walletUnavailable);
                UiDraw.Fill(viewport, 0f, barHeight, 0f, 0f);
                var toast = UiDraw.Box(root, "Toast", High, 8f, UIDesignTokens.WithAlpha(PrimaryC, .45f));
                UiDraw.Place(toast.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 20f), new Vector2(400f, 44f));
                UiDraw.Place(UiDraw.Icon(toast.transform, "Icon", "info", 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
                var toastText = UiDraw.Text(toast.transform, "Text", "", UiFont.MonoMedium, CaptionDp, OnSurface, .03f);
                toastText.overflowMode = TextOverflowModes.Ellipsis;
                UiDraw.Place(toastText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(340f, 20f));
                toast.gameObject.SetActive(false);
            }
            finally { UiDraw.TextUnitScale = previousTextScale; }

            var resize = root.gameObject.AddComponent<Resize>();
            resize.owner = owner; resize.category = category; resize.detail = detail; resize.matchId = matchId;
            resize.roster = roster; resize.localTeam = localTeam; resize.profile = profile; resize.wallet = wallet;
            resize.online = online; resize.act = act; resize.accountUnavailable = accountUnavailable;
            resize.walletUnavailable = walletUnavailable; resize.builtWidth = canvasWidth; resize.builtScale = unitsPerDp;
            resize.builtStreamSafe = StreamSafePresentation.Enabled;
        }

        // ---------------------------------------------------------------- top bar

        /// <summary>Builds the fixed top bar and returns the height it occupies. The centre tabs move
        /// to a second row when they would collide with the brand or the account cluster.</summary>
        static float TopBar(RectTransform root, float width, ProfileDto profile, WalletDto wallet, bool online, SupportHubActions act, bool walletUnavailable)
        {
            var bar = UiDraw.Box(root, "TopBar", UIDesignTokens.WithAlpha(Low, .9f)).rectTransform;
            bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(.5f, 1f);
            bar.sizeDelta = new Vector2(0f, BarH); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Shadow", UiDraw.Hex("#000000", .25f), -BarH);

            var back = UiDraw.Tap(bar, "Back", High, 4f, () => Tap(act.Back));
            UiDraw.Place((RectTransform)back.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(48f, 48f));
            UiDraw.Fill(UiDraw.Icon(back.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform);

            var brand = UiDraw.Text(bar, "Brand", L("brand", "HOTDROP"), UiFont.HeadingSemiBold, TitleDp, Primary, .05f);
            var sub = UiDraw.Text(bar, "Build", F("build.versionTemplate", "Version {0}", Application.version.ToUpperInvariant()), UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            TL(brand.rectTransform, 76f, 11f, UiDraw.Width(brand) + 4f, 24f);
            TL(sub.rectTransform, 76f, 36f, UiDraw.Width(sub) + 4f, 14f);
            float chipX = 76f + Mathf.Max(UiDraw.Width(brand), UiDraw.Width(sub)) + 16f;
            var chip = UiDraw.Box(bar, "SystemChip", High, 4f);
            var chipText = UiDraw.Text(chip.transform, "Text", online ? L("account.online", "ACCOUNT  ·  ONLINE") : L("account.offline", "ACCOUNT  ·  OFFLINE"), UiFont.MonoMedium, CaptionDp, OnSurface, .1f);
            float cw = 8f + 8f + 8f + UiDraw.Width(chipText) + 8f;
            UiDraw.Place(chip.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(chipX, 0f), new Vector2(cw, 22f));
            UiDraw.Place(UiDraw.Dot(chip.transform, "Dot", 8f, online ? Primary : Secondary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(8f, 8f));
            UiDraw.Place(chipText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(UiDraw.Width(chipText) + 2f, 14f));

            // Centre navigation: each tab opens the real page it names.
            var tabs = new (string label, Action go)[]
            {
                (L("nav.operations", "Back"), act.Back),
                (L("nav.armory", "Appearance"), () => act.Navigate?.Invoke("Appearance")),
                (L("nav.telemetry", "Profile"), () => act.Navigate?.Invoke("Profile")),
                (L("nav.pass", "Goals"), () => act.Navigate?.Invoke("Contracts")),
            };
            var nav = UiDraw.Box(bar, "Nav", Mid, 4f);
            float nx = 4f;
            var navItems = new List<(RectTransform rt, float w)>();
            for (int i = 0; i < tabs.Length; i++)
            {
                var t = tabs[i];
                bool active = i == 0;
                var item = UiDraw.Tap(nav.transform, "Tab_" + t.label, active ? PrimaryC : new Color(0f, 0f, 0f, 0f), 4f, () => Tap(t.go));
                var label = active
                    ? UiDraw.Text(item.transform, "Label", t.label, UiFont.BodyBold, UiDp, OnPrimaryC, 0f, TextAlignmentOptions.Midline)
                    : UiDraw.Text(item.transform, "Label", t.label, UiFont.Heading, CaptionDp, OnVariant, .08f, TextAlignmentOptions.Midline);
                float iw = UiDraw.Width(label) + 24f;
                UiDraw.Fill(label.rectTransform);
                navItems.Add(((RectTransform)item.transform, iw));
                UiDraw.Place((RectTransform)item.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nx, 0f), new Vector2(iw, 48f));
                nx += iw + 4f;
            }
            // The tabs keep a 48 px hit area; only the visible pill is the design's 28 px.
            foreach (var (rt, w) in navItems)
            {
                var hit = rt.GetComponent<Image>();
                var visible = UiDraw.Box(rt, "Pill", hit.color, 4f);
                UiDraw.Place(visible.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(w, 28f));
                visible.transform.SetAsFirstSibling();
                hit.color = new Color(0f, 0f, 0f, 0f);
            }

            // Right cluster: avatar, operative alias, earned credits.
            float rx = width - 16f;
            var avatar = UiDraw.Dot(bar, "Avatar", 32f, Primary);
            UiDraw.Place(avatar.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(rx, 0f), new Vector2(32f, 32f));
            UiDraw.Fill(UiDraw.Icon(avatar.transform, "Icon", "person", 18f, OnPrimary).rectTransform);
            rx -= 32f + 12f;
            var op = UiDraw.Box(bar, "Operative", High, 4f);
            var opText = UiDraw.Text(op.transform, "Text", F("account.aliasTemplate", "<color=#{0}>OP:</color> {1}", ColorUtility.ToHtmlStringRGB(OnVariant), StreamSafePresentation.AccountAlias(profile?.alias ?? Missing(L("state.syncing", "Syncing"), L("state.unavailable", "Unavailable")))), UiFont.MonoMedium, CaptionDp, Primary, .05f);
            float ow = UiDraw.Width(opText) + 16f;
            UiDraw.Place(op.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(rx, 0f), new Vector2(ow, 26f));
            UiDraw.Fill(opText.rectTransform, 8f, 0f, 8f, 0f);
            rx -= ow + 12f;
            var credits = UiDraw.Box(bar, "Credits", High, 4f);
            var crText = UiDraw.Text(credits.transform, "Value", StreamSafePresentation.AccountProgress(walletUnavailable ? L("state.unavailable", "Unavailable") : wallet == null ? "—" : wallet.credits.ToString("N0")), UiFont.Mono, walletUnavailable ? CaptionDp : UiDp, walletUnavailable ? OnVariant : Tertiary, .03f);
            float crw = 8f + 18f + 4f + UiDraw.Width(crText) + 8f;
            UiDraw.Place(credits.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(rx, 0f), new Vector2(crw, 26f));
            UiDraw.Place(UiDraw.Icon(credits.transform, "Icon", "toll", 18f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(crText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f, 0f), new Vector2(UiDraw.Width(crText) + 2f, 18f));
            rx -= crw;

            float leftEnd = chipX + cw + 16f, rightStart = rx - 16f;
            bool fits = width * .5f - nx * .5f >= leftEnd && width * .5f + nx * .5f <= rightStart;
            if (fits)
            {
                UiDraw.Place(nav.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(nx, 36f));
                return BarH;
            }
            var row = UiDraw.Box(root, "NavRow", UIDesignTokens.WithAlpha(Low, .9f)).rectTransform;
            row.anchorMin = new Vector2(0f, 1f); row.anchorMax = Vector2.one; row.pivot = new Vector2(.5f, 1f);
            row.sizeDelta = new Vector2(0f, NavRowH); row.anchoredPosition = new Vector2(0f, -BarH);
            nav.transform.SetParent(row, false);
            UiDraw.Place(nav.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(nx, 36f));
            UiDraw.HRule(row, "Shadow", UiDraw.Hex("#000000", .25f), -NavRowH);
            return BarH + NavRowH;
        }

        // ---------------------------------------------------------------- title

        /// <summary>Builds the title card and returns its height. The session and fair-play pills sit
        /// beside the heading when they fit, and on a second row inside the card when they do not.</summary>
        static float Title(RectTransform page, float width, ProfileDto profile, bool online, SupportHubActions act)
        {
            var card = Card(page, "Title", Mid, Pad, 0f, width - Pad * 2f, 72f);
            var grid = DotGrid(card, UIDesignTokens.WithAlpha(PrimaryC, .2f));
            UiDraw.Fill(grid.rectTransform, 4f, 4f, 4f, 4f);
            var back = UiDraw.Tap(card, "Back", High, 4f, () => Tap(act.Back));
            TL((RectTransform)back.transform, 12f, 12f, 48f, 48f);
            UiDraw.Fill(UiDraw.Icon(back.transform, "Icon", "arrow_back", 24f, Primary).rectTransform);
            string state = profile == null
                ? Missing(L("state.accountSyncing", "Account Syncing"), L("state.accountUnavailable", "Account Unavailable"))
                : StreamSafePresentation.Enabled ? L("identity.privateProfile", "Private Profile")
                : profile.isGuest ? L("state.guestAccount", "Guest Account") : L("state.linkedAccount", "Linked Account");
            TL(UiDraw.Text(card, "Kicker", F("title.kickerTemplate", "ACCOUNT RECOVERY  <color=#{0}>•</color>  <color=#{1}>{2}</color>", ColorUtility.ToHtmlStringRGB(OnVariant), ColorUtility.ToHtmlStringRGB(Tertiary), state),
                UiFont.MonoMedium, CaptionDp, Primary, .05f).rectTransform, 72f, 15f, 600f, 14f);
            var heading = UiDraw.Text(card, "Heading", L("title.heading", "Account Security & Recovery"), UiFont.Heading, TitleDp, OnSurface, -.01f);
            float headingW = UiDraw.Width(heading) + 4f;
            TL(heading.rectTransform, 72f, 28f, headingW, 34f);

            float right = width - Pad * 2f - 12f;
            var fair = UiDraw.Box(card, "FairPlay", Lowest, 4f);
            var fairText = UiDraw.Text(fair.transform, "Text", L("title.fairplay", "Fairplay Active"), UiFont.MonoMedium, CaptionDp, Tertiary, .05f);
            float fw = 12f + 18f + 4f + UiDraw.Width(fairText) + 12f;
            UiDraw.Place(fair.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -36f), new Vector2(fw, 26f));
            UiDraw.Place(UiDraw.Icon(fair.transform, "Icon", "verified_user", 18f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(fairText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(34f, 0f), new Vector2(UiDraw.Width(fairText) + 2f, 14f));
            right -= fw + 8f;
            var session = UiDraw.Box(card, "Session", Lowest, 4f);
            var sLabel = UiDraw.Text(session.transform, "Label", L("title.sessionLabel", "Session:"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            var sValue = UiDraw.Text(session.transform, "Value", online ? L("title.authenticated", "Authenticated") : L("title.sessionOffline", "Offline"), UiFont.Mono, UiDp, online ? Primary : Secondary, .03f);
            float sw = 12f + 10f + 4f + UiDraw.Width(sLabel) + 4f + UiDraw.Width(sValue) + 12f;
            UiDraw.Place(session.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -36f), new Vector2(sw, 26f));
            UiDraw.Place(UiDraw.Dot(session.transform, "Dot", 10f, online ? Primary : Secondary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(10f, 10f));
            UiDraw.Place(sLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(26f, 0f), new Vector2(UiDraw.Width(sLabel) + 2f, 14f));
            UiDraw.Place(sValue.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f + UiDraw.Width(sLabel), 0f), new Vector2(UiDraw.Width(sValue) + 2f, 18f));
            if (72f + headingW + 16f + sw + 8f + fw <= width - Pad * 2f - 12f) return 72f;

            // Narrow page: the pills move under the heading, left-aligned with it.
            const float rowY = 72f + 13f;
            UiDraw.Place(session.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(72f, -rowY), new Vector2(sw, 26f));
            UiDraw.Place(fair.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(72f + sw + 8f, -rowY), new Vector2(fw, 26f));
            card.sizeDelta = new Vector2(card.sizeDelta.x, 72f + 34f);
            return 72f + 34f;
        }

        // ---------------------------------------------------------------- left column

        static float Identity(RectTransform page, float x, float y, float w, ProfileDto profile, string matchId, SupportHubActions act, bool walletUnavailable)
        {
            bool linked = profile != null && !profile.isGuest;
            bool streamSafe = StreamSafePresentation.Enabled;
            var card = Card(page, "Account", Mid, x, y, w, 100f);
            float iw = w - 40f, cy = 20f;
            Header(card, "badge", L("identity.header", "Callsign Identity"), Primary, cy, w);
            Badge(card, "Tier", profile == null ? Missing(L("state.syncing", "Syncing"), L("state.unavailable", "Unavailable")) : StreamSafePresentation.Enabled ? L("identity.privateProfile", "Private Profile") : F("identity.levelTemplate", "Level {0} Pilot", Mathf.Clamp(profile.level, 1, 20)), High, Primary, w - 20f, cy);
            cy += 18f + 12f;

            var brief = UiDraw.Box(card, "Dossier", Low, 8f);
            TL(brief.rectTransform, 20f, cy, iw, 72f);
            var avatar = UiDraw.Box(brief.transform, "Avatar", Highest, 8f);
            TL(avatar.rectTransform, 8f, 8f, 56f, 56f);
            UiDraw.Fill(UiDraw.Icon(avatar.transform, "Icon", "shield_person", 32f, Primary).rectTransform);
            var stripe = UiDraw.Box(avatar.transform, "Stripe", Primary);
            UiDraw.Place(stripe.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), Vector2.zero, new Vector2(48f, 4f));
            var alias = UiDraw.Text(brief.transform, "Alias", StreamSafePresentation.AccountAlias(profile?.alias ?? Missing(L("state.accountSyncing", "Account Syncing"), L("state.accountUnavailable", "Account Unavailable"))), UiFont.HeadingSemiBold, TitleDp, OnSurface);
            alias.overflowMode = TextOverflowModes.Ellipsis;
            TL(alias.rectTransform, 76f, 12f, iw - 88f, 26f);
            TL(UiDraw.Text(brief.transform, "Account", StreamSafePresentation.Enabled ? L("identity.privateAccountDetails", "Private Account Details") : profile == null ? Missing(L("identity.accountLoading", "Account data is loading."), L("identity.accountUnavailable", "Account data could not be loaded.")) : linked ? L("identity.linkedGooglePlay", "LINKED  •  GOOGLE PLAY GAMES") : L("identity.guestLocal", "GUEST  •  STORED ON THIS DEVICE"),
                UiFont.Body, CaptionDp, OnVariant, .01f).rectTransform, 76f, 40f, iw - 88f, 16f);
            cy += 72f + 12f;

            var link = UiDraw.Box(card, "LinkStatus", High, 8f);
            TL(link.rectTransform, 20f, cy, iw, 78f);
            TL(UiDraw.Icon(link.transform, "Icon", "sports_esports", 20f, Primary).rectTransform, 12f, 13f, 20f, 20f);
            TL(UiDraw.Text(link.transform, "Name", L("identity.googlePlayGames", "Google Play Games"), UiFont.Mono, UiDp, OnSurface, .03f).rectTransform, 38f, 12f, iw - 160f, 22f);
            string linkState = streamSafe ? L("identity.hiddenStatus", "Hidden")
                : profile == null ? Missing(L("state.syncing", "Syncing"), L("state.unknown", "Unknown"))
                : linked ? L("identity.linked", "Linked") : L("identity.notLinked", "Not Linked");
            Color linkStateTone = streamSafe ? OnVariant : linked ? Primary : Tertiary;
            var state = UiDraw.Text(link.transform, "State", linkState, UiFont.Mono, CaptionDp, linkStateTone, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -23f), new Vector2(UiDraw.Width(state) + 2f, 14f));
            UiDraw.Place(UiDraw.Dot(link.transform, "Dot", 8f, streamSafe ? OnVariant : profile == null ? Secondary : linked ? Primary : Tertiary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f - UiDraw.Width(state), -23f), new Vector2(8f, 8f));
            TL(UiDraw.Text(link.transform, "RecoveryLabel", L("identity.cloudRecovery", "Cloud Recovery:"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 12f, 40f, 160f, 14f);
            string recoveryStatus = streamSafe ? L("identity.hiddenStatus", "Hidden")
                : profile == null ? Missing(L("identity.recoveryChecking", "Checking…"), L("identity.recoveryUnavailable", "Status unavailable"))
                : linked ? L("identity.recoveryAfterReinstall", "Available after reinstall") : L("identity.recoveryDeviceOnly", "This device only");
            var recovery = UiDraw.Text(link.transform, "Recovery", recoveryStatus,
                UiFont.MonoMedium, CaptionDp, OnSurface, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(recovery.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -47f), new Vector2(iw - 180f, 14f));
            var bar = UiDraw.Box(link.transform, "Bar", Lowest, 3f);
            TL(bar.rectTransform, 12f, 60f, iw - 24f, 6f);
            if (linked && !streamSafe)
                UiDraw.Fill(UiDraw.Box(bar.transform, "Fill", Primary, 3f).rectTransform);
            cy += 78f + 12f;

            var notice = UiDraw.Box(card, "SecurityNotice", Lowest, 8f);
            TL(UiDraw.Icon(notice.transform, "Icon", "info", 18f, Tertiary).rectTransform, 8f, 10f, 18f, 18f);
            string securityNotice = streamSafe ? L("identity.streamSafeNotice", "Account link and recovery details are hidden while Stream-Safe Display is enabled.")
                : profile == null
                ? Missing(L("identity.securityChecking", "Checking whether this profile is linked for recovery."),
                    L("identity.securityUnavailable", "Recovery status could not be loaded, so this page cannot confirm whether this profile is linked. Retry before relying on recovery."))
                : linked
                ? L("identity.securityLinked", "This account is bound to Google Play Games. Sign in with the same Google Play identity after a reinstall or on a new phone to recover it. Accounts are never merged.")
                : L("identity.securityGuest", "Guest accounts live on this device only and are lost on uninstall or hardware handover. Link Google Play Games to make this profile recoverable.");
            var nt = Wrapped(notice.transform, "Text", $"<color=#{ColorUtility.ToHtmlStringRGB(Tertiary)}><b>{L("identity.securityNoticeLabel", "Security Notice:")}</b></color> " + securityNotice,
                UiFont.Body, CaptionDp, OnVariant, iw - 40f, 19.5f);
            TL(nt.rectTransform, 32f, 8f, iw - 40f, nt.preferredHeight);
            float nh = nt.preferredHeight + 16f;
            TL(notice.rectTransform, 20f, cy, iw, nh);
            cy += nh + 12f;

            // Unavailable: the row becomes a 48 px retry action instead of a passive status line.
            bool retry = (accountUnavailable || walletUnavailable) && act.RetryAccount != null;
            float sh = retry ? 48f : 46f;
            var sync = retry
                ? UiDraw.Tap(card, "Sync", Low, 8f, () => Tap(act.RetryAccount)).GetComponent<Image>()
                : UiDraw.Box(card, "Sync", Low, 8f);
            TL(sync.rectTransform, 20f, cy, iw, sh);
            string syncIcon = profile != null && !walletUnavailable ? "cloud_done" : accountUnavailable || walletUnavailable ? "warning" : "cloud_sync";
            UiDraw.Place(UiDraw.Icon(sync.transform, "Icon", syncIcon, 20f, accountUnavailable || walletUnavailable ? Tertiary : Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(20f, 20f));
            string syncTitle = accountUnavailable ? L("sync.accountUnavailable", "Account Service Unavailable") : walletUnavailable ? L("sync.creditsUnavailable", "Credits Unavailable") : profile == null ? L("sync.pending", "Sync Pending") : L("sync.nominal", "Sync Health Nominal");
            TL(UiDraw.Text(sync.transform, "Title", syncTitle, UiFont.MonoMedium, CaptionDp, OnSurface, .05f).rectTransform, 36f, 8f, iw - (retry ? 76f : 44f), 14f);
            string syncMessage = accountUnavailable
                ? retry ? L("sync.accountRetry", "Tap to retry. Reports and web deletion stay available.") : L("sync.accountUnavailableDetail", "Reports and web deletion stay available.")
                : walletUnavailable ? L("sync.creditsRetry", "Credits could not be loaded. Tap to retry.")
                : profile == null ? L("sync.waiting", "Waiting for the account service.") : L("sync.saved", "Profile, inventory and credits are saved to your account.");
            var syncBody = UiDraw.Text(sync.transform, "Body", syncMessage, UiFont.Body, CaptionDp, OnVariant, .01f);
            syncBody.overflowMode = TextOverflowModes.Ellipsis;
            TL(syncBody.rectTransform, 36f, 23f, iw - (retry ? 76f : 44f), 16f);
            if (retry)
                UiDraw.Place(UiDraw.Icon(sync.transform, "RetryIcon", "refresh", 20f, Primary).rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(20f, 20f));
            cy += sh + 12f + 4f;

            if (profile != null)
            {
                var recover = UiDraw.Tap(card, "RecoverAccount", PrimaryC, 8f, () => Tap(act.Recover));
                TL((RectTransform)recover.transform, 20f, cy, iw, 48f);
                UiDraw.Glow((RectTransform)recover.transform, UiDraw.Hex("#000000", .25f), 8f, 6f);
                IconLabel(recover.transform, "sync", 20f,
                    streamSafe ? L("identity.recoveryAction", "ACCOUNT RECOVERY") : L("identity.recoverAction", "Recover Linked Account"),
                    UiFont.HeadingSemiBold, TitleDp, OnPrimaryC, .025f);
                cy += 48f + 4f;
                float half = (iw - 4f) / 2f;
                var manage = UiDraw.Tap(card, "ManageAccount", High, 8f, () => Tap(act.AccountSettings));
                TL((RectTransform)manage.transform, 20f, cy, half, 48f);
                IconLabel(manage.transform, "manage_accounts", 18f, L("identity.manageAction", "Account & Email"), UiFont.Heading, CaptionDp, OnSurface, .08f, Tertiary);
                var second = UiDraw.Tap(card, linked ? "RevokeLink" : "LinkGooglePlay", High, 8f, () => Tap(linked ? act.Revoke : act.Link));
                TL((RectTransform)second.transform, 20f + half + 4f, cy, half, 48f);
                if (streamSafe) IconLabel(second.transform, "link", 18f, L("identity.manageLinkAction", "MANAGE ACCOUNT LINK"), UiFont.Heading, CaptionDp, OnVariant, .08f);
                else if (linked) IconLabel(second.transform, "link_off", 18f, L("identity.revokeAction", "Revoke Link"), UiFont.Heading, CaptionDp, Secondary, .08f);
                else IconLabel(second.transform, "add_link", 18f, L("identity.linkAction", "Link Google Play"), UiFont.Heading, CaptionDp, Primary, .08f);
                cy += 48f + 20f;
            }
            card.sizeDelta = new Vector2(w, cy);

            var node = Card(page, "MatchNode", Low, x, y + cy + Gap, w, 52f);
            UiDraw.Place(UiDraw.Icon(node, "Icon", "network_ping", 20f, OnVariant).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(UiDraw.Text(node, "Label", L("identity.lastOnlineMatch", "Last Online Match"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(200f, 14f));
            UiDraw.Place(UiDraw.Text(node, "Match", string.IsNullOrEmpty(matchId) ? L("identity.noMatchRecorded", "None Recorded") : F("identity.matchIdTemplate", "Match ID #{0}", ShortId(matchId)), UiFont.Mono, UiDp, Primary, .03f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(w * .5f, 18f));
            return cy + Gap + 52f;
        }

        // ---------------------------------------------------------------- centre column

        static readonly (string icon, string key, string label, string reason)[] Violations =
        {
            ("sports_martial_arts", "report.violation.disruptive", "Disruptive Play", "Disruptive play"),
            ("campaign", "report.violation.harassment", "Harassment", "Harassment"),
            ("cell_wifi", "report.violation.cheating", "Cheating / Exploit", "Cheating or exploit"),
            ("hourglass_disabled", "report.violation.inactivity", "Inactivity / AFK", "Inactivity / AFK"),
        };

        /// <summary>A caption on the left and a value on the right of one line; the value moves under
        /// the caption when both do not fit. Returns the height used.</summary>
        static float LabelAndValue(RectTransform card, string labelName, string label, string valueName, string value,
            UiFont valueFont, Color valueTone, float y, float iw)
        {
            var left = UiDraw.Text(card, labelName, label, UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            var right = UiDraw.Text(card, valueName, value, valueFont, CaptionDp, valueTone, .05f, TextAlignmentOptions.MidlineRight);
            float lw = UiDraw.Width(left) + 2f, rw = UiDraw.Width(right) + 2f;
            TL(left.rectTransform, 20f, y, Mathf.Min(lw, iw), 14f);
            if (lw + 12f + rw <= iw)
            {
                UiDraw.Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -y - 7f), new Vector2(rw, 14f));
                return 14f;
            }
            right.alignment = TextAlignmentOptions.MidlineLeft;
            TL(right.rectTransform, 20f, y + 18f, Mathf.Min(rw, iw), 14f);
            return 14f + 4f + 14f;
        }

        static float Report(RectTransform page, float x, float y, float w, TMP_InputField category, TMP_InputField detail,
            IReadOnlyList<RecentMatch.Entry> roster, int localTeam, SupportHubActions act, string keepReason)
        {
            var card = Card(page, "Report", Mid, x, y, w, 100f);
            float iw = w - 40f, cy = 20f;
            Header(card, "gavel", L("report.header", "Tactical Integrity"), Primary, cy, w);
            Badge(card, "Tolerance", L("report.zeroTolerance", "Zero Tolerance"), Highest, Tertiary, w - 20f, cy);
            cy += 18f + 12f;
            TL(UiDraw.Text(card, "Title", L("report.title", "Report Match Participant"), UiFont.HeadingSemiBold, TitleDp, OnSurface).rectTransform, 20f, cy, iw, 26f);
            var desc = Wrapped(card, "Context", L("report.context", "Reports are reviewed by the HOTDROP team together with the match record. Pick who, why and what happened."),
                UiFont.Body, CaptionDp, OnVariant, iw, 16f);
            TL(desc.rectTransform, 20f, cy + 28f, iw, desc.preferredHeight);
            cy += 28f + desc.preferredHeight + 12f;

            bool has = roster != null && roster.Count > 0;
            cy += LabelAndValue(card, "RosterLabel", L("report.rosterLabel", "Recent Engagement Roster"), "RosterWindow", L("report.lastOnlineMatch", "Last Online Match"),
                UiFont.MonoRegular, Primary, cy, iw) + 4f;
            if (!has)
            {
                var empty = UiDraw.Box(card, "NoRecentMatch", Low, 8f);
                UiDraw.Place(UiDraw.Text(empty.transform, "Title", L("report.noRecentMatch", "No Recent Online Match"), UiFont.HeadingSemiBold, CaptionDp, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -16f), new Vector2(iw - 16f, 16f));
                // Wraps on a narrow column instead of running past the box.
                var emptyDetail = Wrapped(empty.transform, "Detail", L("report.opensAfterMatch", "Reports open after your next online match"), UiFont.MonoMedium, CaptionDp, OnVariant, iw - 16f, 16f, .05f);
                emptyDetail.alignment = TextAlignmentOptions.Top;
                TL(emptyDetail.rectTransform, 8f, 26f, iw - 16f, emptyDetail.preferredHeight);
                float eh = Mathf.Max(48f, 26f + emptyDetail.preferredHeight + 6f);
                TL(empty.rectTransform, 20f, cy, iw, eh);
                cy += eh + 12f;
            }
            else
            {
                float cw = (iw - 8f) / 3f;
                var cards = new List<(Image tint, TextMeshProUGUI name)>();
                for (int i = 0; i < roster.Count; i++)
                {
                    var entry = roster[i];
                    int index = i;
                    var button = UiDraw.Tap(card, "Participant_" + i, High, 8f, null);
                    TL((RectTransform)button.transform, 20f + (i % 3) * (cw + 4f), cy + (i / 3) * 52f, cw, 48f);
                    var tint = UiDraw.Box(button.transform, "Selected", UIDesignTokens.WithAlpha(PrimaryC, .2f), 8f);
                    UiDraw.Fill(tint.rectTransform);
                    tint.enabled = false;
                    var name = UiDraw.Text(button.transform, "Name", StreamSafePresentation.PlayerName(entry.alias, entry.slot), UiFont.HeadingSemiBold, CaptionDp, OnSurface, 0f, TextAlignmentOptions.Midline);
                    name.overflowMode = TextOverflowModes.Ellipsis;
                    UiDraw.Place(name.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 8f), new Vector2(cw - 8f, 16f));
                    string side = localTeam < 0 ? F("report.slotTemplate", "Slot {0}", entry.slot + 1) : entry.team == localTeam
                        ? L("report.alliedWingman", "Allied Wingman") : L("report.coralOpponent", "Coral Opponent");
                    UiDraw.Place(UiDraw.Text(button.transform, "Side", side, UiFont.MonoMedium, CaptionDp, OnVariant, .05f, TextAlignmentOptions.Midline).rectTransform,
                        new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -9f), new Vector2(cw - 8f, 14f));
                    cards.Add((tint, name));
                    if (i == chosenParticipant) { tint.enabled = true; name.color = Primary; }
                    button.onClick.AddListener(() =>
                    {
                        GameAudio.Play2D(GameCue.UiTap);
                        chosenParticipant = index;
                        for (int k = 0; k < cards.Count; k++)
                        {
                            cards[k].tint.enabled = k == index;
                            cards[k].name.color = k == index ? Primary : OnSurface;
                        }
                        act.Choose?.Invoke(entry);
                    });
                }
                int rows = (roster.Count + 2) / 3;
                cy += rows * 52f - 4f + 12f;
            }

            TL(UiDraw.Text(card, "ViolationLabel", L("report.violationClass", "Violation Class"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 20f, cy, iw, 14f);
            cy += 18f;
            if (category != null)
            {
                category.text = keepReason ?? "";
                category.transform.SetParent(card, false);
                category.gameObject.SetActive(false); // the class pills fill it
            }
            float pw = (iw - 4f) / 2f;
            var pills = new List<(Image tint, TextMeshProUGUI icon, TextMeshProUGUI label)>();
            for (int i = 0; i < Violations.Length; i++)
            {
                var v = Violations[i];
                int index = i;
                var pill = UiDraw.Tap(card, "Violation_" + i, High, 8f, null);
                TL((RectTransform)pill.transform, 20f + (i % 2) * (pw + 4f), cy + (i / 2) * 52f, pw, 48f);
                var tint = UiDraw.Box(pill.transform, "Selected", UIDesignTokens.WithAlpha(PrimaryC, .2f), 8f);
                UiDraw.Fill(tint.rectTransform);
                tint.enabled = false;
                var icon = UiDraw.Icon(pill.transform, "Icon", v.icon, 18f, OnSurface);
                UiDraw.Place(icon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(18f, 18f));
                var label = UiDraw.Text(pill.transform, "Label", L(v.key, v.label), UiFont.Heading, CaptionDp, OnSurface, .08f);
                UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f, 0f), new Vector2(pw - 36f, 16f));
                pills.Add((tint, icon, label));
                if (!string.IsNullOrEmpty(keepReason) && v.reason == keepReason)
                {
                    tint.enabled = true;
                    icon.color = label.color = Primary;
                }
                pill.onClick.AddListener(() =>
                {
                    GameAudio.Play2D(GameCue.UiTap);
                    for (int k = 0; k < pills.Count; k++)
                    {
                        bool on = k == index;
                        pills[k].tint.enabled = on;
                        pills[k].icon.color = pills[k].label.color = on ? Primary : OnSurface;
                    }
                    if (category != null) category.text = v.reason;
                });
            }
            cy += 48f * 2f + 4f + 12f;

            int limit = detail != null && detail.characterLimit > 0 ? detail.characterLimit : 180;
            TL(UiDraw.Text(card, "LogLabel", L("report.incidentLog", "Flight Incident Log"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 20f, cy, iw * .6f, 14f);
            var counter = UiDraw.Text(card, "Counter", F("report.characterCount", "{0} / {1} Chars", detail?.text?.Length ?? 0, limit), UiFont.MonoMedium, CaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(counter.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -cy - 7f), new Vector2(iw * .4f, 14f));
            cy += 18f;
            if (detail != null)
            {
                detail.gameObject.SetActive(true);
                detail.transform.SetParent(card, false);
                var rt = (RectTransform)detail.transform;
                TL(rt, 20f, cy, iw, IncidentLogH);
                StyleTextArea(detail);
                if (counterSource != null && counterListener != null) counterSource.onValueChanged.RemoveListener(counterListener);
                counterSource = detail;
                counterListener = v => { if (counter != null) counter.text = F("report.characterCount", "{0} / {1} Chars", (v ?? "").Length, limit); };
                detail.onValueChanged.AddListener(counterListener);
            }
            cy += IncidentLogH + 12f + 4f;

            float bw = (iw - 4f) / 2f;
            var clear = UiDraw.Tap(card, "ClearReport", High, 8f, () =>
            {
                GameAudio.Play2D(GameCue.UiTap);
                if (detail != null) detail.text = "";
                if (category != null) category.text = "";
                foreach (var p in pills) { p.tint.enabled = false; p.icon.color = p.label.color = OnSurface; }
            });
            TL((RectTransform)clear.transform, 20f, cy, bw, 48f);
            UiDraw.Fill(UiDraw.Text(clear.transform, "Label", L("report.clear", "Clear"), UiFont.HeadingSemiBold, UiDp, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform);
            var submit = UiDraw.Tap(card, "Submit", Primary, 8f, () => Tap(act.Submit));
            TL((RectTransform)submit.transform, 20f + bw + 4f, cy, bw, 48f);
            UiDraw.Glow((RectTransform)submit.transform, UiDraw.Hex("#000000", .25f), 8f, 6f);
            IconLabel(submit.transform, "send", 18f, L("report.submit", "Transmit Report"), UiFont.HeadingSemiBold, UiDp, OnPrimary, 0f);
            cy += 48f + 4f;

            var policy = UiDraw.Box(card, "ReportNotice", Low, 8f);
            TL(UiDraw.Icon(policy.transform, "Icon", "verified", 18f, Primary).rectTransform, 8f, 10f, 18f, 18f);
            var pt = Wrapped(policy.transform, "Text", $"<color=#{ColorUtility.ToHtmlStringRGB(Primary)}><b>{L("report.policyLabel", "Integrity Policy:")}</b></color> {L("report.policyText", "Every report is paired with the selected match for human review. Repeated false reports are not acted on.")}",
                UiFont.Body, CaptionDp, OnVariant, iw - 40f, 15f);
            TL(pt.rectTransform, 32f, 8f, iw - 40f, pt.preferredHeight);
            float ph = pt.preferredHeight + 16f;
            TL(policy.rectTransform, 20f, cy, iw, ph);
            cy += ph + 20f;
            card.sizeDelta = new Vector2(w, cy);
            return cy;
        }

        static void StyleTextArea(TMP_InputField input)
        {
            var bg = input.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = UIRoundedSprites.RoundedRect(8f);
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 1f;
                bg.color = Lowest;
            }
            foreach (var outline in input.GetComponentsInChildren<Outline>(true)) outline.enabled = false;
            input.lineType = TMP_InputField.LineType.MultiLineSubmit;
            if (input.textViewport != null) UiDraw.Fill(input.textViewport, 8f, 8f, 8f, 8f);
            if (input.textComponent != null)
            {
                UIFonts.Use(input.textComponent, UiFont.Mono);
                input.textComponent.fontSize = UiMetrics.ScaleTextSize(UiDp) / UiDraw.TextUnitScale;
                input.textComponent.enableAutoSizing = false;
                input.textComponent.color = OnSurface;
                input.textComponent.alignment = TextAlignmentOptions.TopLeft;
                input.textComponent.textWrappingMode = TextWrappingModes.Normal;
                input.textComponent.characterSpacing = 3f;
            }
            if (input.placeholder is TextMeshProUGUI placeholder)
            {
                UIFonts.Use(placeholder, UiFont.Mono);
                placeholder.text = L("report.placeholder", "Describe abnormal trajectory, grief behavior, or communication breach...");
                placeholder.fontSize = UiMetrics.ScaleTextSize(UiDp) / UiDraw.TextUnitScale;
                placeholder.enableAutoSizing = false;
                placeholder.fontStyle = FontStyles.Normal;
                placeholder.color = UIDesignTokens.WithAlpha(UIDesignTokens.TextMuted, .7f);
                placeholder.alignment = TextAlignmentOptions.TopLeft;
                placeholder.textWrappingMode = TextWrappingModes.Normal;
                placeholder.characterSpacing = 3f;
            }
        }

        // ---------------------------------------------------------------- right column

        static float Compliance(RectTransform page, float x, float y, float w, SupportHubActions act)
        {
            var card = Card(page, "Assistance", Mid, x, y, w, 170f);
            float iw = w - 40f;
            Header(card, "terminal", L("compliance.header", "Compliance & Support"), Primary, 20f, w);
            var build = UiDraw.Text(card, "Build", F("build.label", "Build {0}", Application.version.ToUpperInvariant()), UiFont.MonoMedium, CaptionDp, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(build.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -29f), new Vector2(160f, 14f));
            var tiles = new (string name, string icon, Color tone, string titleKey, string title, string subKey, string sub, Action go)[]
            {
                ("Privacy", "policy", Primary, "privacy", "Privacy", "privacySub", "Data & Email", act.AccountSettings),
                ("WebDeletion", "description", Primary, "webDeletion", "Web Request", "webDeletionSub", "Deletion Page", act.WebDeletion),
                ("Profile", "support_agent", Primary, "profile", "Profile", "profileSub", "Your Pilot Record", act.Profile),
                ("Diagnostics", "bug_report", Tertiary, "diagnostics", "Diagnostics", "diagnosticsSub", "Send a Report", act.Diagnostics),
            };
            float tw = (iw - 4f) / 2f;
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                var tile = UiDraw.Tap(card, "Support_" + t.name, High, 8f, () => Tap(t.go));
                TL((RectTransform)tile.transform, 20f + (i % 2) * (tw + 4f), 50f + (i / 2) * 52f, tw, 48f);
                UiDraw.Place(UiDraw.Icon(tile.transform, "Icon", t.icon, 20f, t.tone).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(20f, 20f));
                TL(UiDraw.Text(tile.transform, "Title", L("compliance." + t.titleKey, t.title), UiFont.Heading, CaptionDp, OnSurface, .08f).rectTransform, 36f, 9f, tw - 40f, 16f);
                TL(UiDraw.Text(tile.transform, "Sub", L("compliance." + t.subKey, t.sub), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 36f, 25f, tw - 40f, 14f);
            }

            float py = y + 170f + Gap;
            var gate = Card(page, "Purge", UIDesignTokens.WithAlpha(SecondaryC, .3f), x, py, w, 100f);
            float cy = 20f;
            Header(gate, "warning", L("purge.header", "Destructive Purge Gate"), Secondary, cy, w, Secondary);
            Badge(gate, "Safety", L("purge.twoStep", "2-Step Safety"), Secondary, OnSecondary, w - 20f, cy);
            cy += 18f + 12f;
            TL(UiDraw.Text(gate, "Title", L("purge.title", "Permanent Account Purge"), UiFont.HeadingSemiBold, TitleDp, Secondary).rectTransform, 20f, cy, iw, 26f);
            TL(UiDraw.Text(gate, "Detail", L("purge.irreversible", "Irreversible deletion of your HOTDROP account:"), UiFont.Body, CaptionDp, OnVariant, .01f).rectTransform, 20f, cy + 30f, iw, 16f);
            cy += 30f + 16f + 12f;

            var ledger = UiDraw.Box(gate, "Ledger", Lowest, 8f);
            var rows = new (string icon, Color tone, string text)[]
            {
                ("close", Secondary, F("purge.deletedTemplate", "Deleted: {0}", L("purge.deletedItems", "Profile, progression, inventory, credits, reports & match history"))),
                ("lock_clock", OnVariant, F("purge.retainedTemplate", "Retained: {0}", L("purge.retainedItems", "Purchase records where the law requires them"))),
                ("history_toggle_off", Tertiary, L("purge.secondStep", "SECOND STEP: type DELETE to confirm. This cannot be undone")),
            };
            float ly = 8f;
            foreach (var r in rows)
            {
                var line = Wrapped(ledger.transform, "Line", r.text, UiFont.MonoMedium, CaptionDp, r.tone, iw - 44f, 14f, .05f);
                TL(UiDraw.Icon(ledger.transform, "Icon", r.icon, 16f, r.tone).rectTransform, 8f, ly, 16f, 16f);
                TL(line.rectTransform, 28f, ly + 1f, iw - 44f, line.preferredHeight);
                ly += Mathf.Max(16f, line.preferredHeight) + 4f;
            }
            float lh = ly + 4f;
            TL(ledger.rectTransform, 20f, cy, iw, lh);
            cy += lh + 12f;

            TL(UiDraw.Text(gate, "HoldLabel", L("purge.holdLabel", "Tactical Safety Release"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 20f, cy, iw * .6f, 14f);
            var holdStatus = UiDraw.Text(gate, "HoldStatus", F("purge.holdDuration", "Hold {0} Seconds", 3), UiFont.Mono, CaptionDp, Secondary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(holdStatus.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -cy - 7f), new Vector2(iw * .4f, 14f));
            cy += 18f;
            var hold = UiDraw.Tap(gate, "DeleteAccount", SecondaryC, 8f, null);
            TL((RectTransform)hold.transform, 20f, cy, iw, 52f);
            hold.gameObject.AddComponent<CanvasGroup>();
            hold.gameObject.AddComponent<RectMask2D>();
            var fill = UiDraw.Box(hold.transform, "Progress", UIDesignTokens.WithAlpha(Secondary, .4f));
            UiDraw.Place(fill.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(0f, 52f));
            var holdLabel = IconLabel(hold.transform, "fingerprint", 20f, L("purge.pressHold", "Press & Hold to Unlock"), UiFont.HeadingSemiBold, TitleDp, OnSecondaryC, .05f);
            var gateLogic = hold.gameObject.AddComponent<HoldGate>();
            gateLogic.Fill = fill.rectTransform; gateLogic.Width = iw;
            gateLogic.Changed = holding => holdStatus.text = holding ? L("purge.engaging", "Override engaging...") : F("purge.holdDuration", "Hold {0} Seconds", 3);
            gateLogic.Done = () => RevealPurge(FindOwner(gate));
            cy += 52f + 20f;
            gate.sizeDelta = new Vector2(w, cy);

            // Typed second step, shown once the hold completes.
            var stage = UiDraw.Box(gate, "StageTwo", High, 8f);
            var prompt = Wrapped(stage.transform, "Prompt", F("purge.confirmPrompt", "TYPE \"{0}\" TO CONFIRM — {1}", "DELETE",
                    L("purge.confirmDetails", "This deletes your profile, progression, inventory, credits and match history.")),
                UiFont.MonoMedium, CaptionDp, Secondary, iw - 16f, 14f, .05f);
            TL(prompt.rectTransform, 8f, 8f, iw - 16f, prompt.preferredHeight);
            float sy = 8f + prompt.preferredHeight + 6f;
            var typed = TypedInput(stage.transform);
            float purgeW = 96f, cancelW = 96f;
            TL((RectTransform)typed.transform, 8f, sy, iw - 16f - purgeW - cancelW - 8f, 48f);
            var cancel = UiDraw.Tap(stage.transform, "CancelPurge", Highest, 8f, null);
            TL((RectTransform)cancel.transform, iw - 8f - purgeW - 4f - cancelW, sy, cancelW, 48f);
            UiDraw.Fill(UiDraw.Text(cancel.transform, "Label", L("purge.cancel", "Cancel"), UiFont.HeadingSemiBold, UiDp, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform);
            var purge = UiDraw.Tap(stage.transform, "Purge", Secondary, 8f, () => Tap(act.Purge));
            TL((RectTransform)purge.transform, iw - 8f - purgeW, sy, purgeW, 48f);
            UiDraw.Fill(UiDraw.Text(purge.transform, "Label", L("purge.action", "Purge"), UiFont.HeadingSemiBold, TitleDp, OnSecondary, 0f, TextAlignmentOptions.Midline).rectTransform);
            var purgeGroup = purge.gameObject.AddComponent<CanvasGroup>();
            purge.interactable = false; purgeGroup.alpha = .3f;
            typed.onValueChanged.AddListener(v =>
            {
                bool ok = string.Equals((v ?? "").Trim(), "DELETE", StringComparison.OrdinalIgnoreCase);
                purge.interactable = ok; purgeGroup.alpha = ok ? 1f : .3f;
            });
            cancel.onClick.AddListener(() =>
            {
                GameAudio.Play2D(GameCue.UiTap);
                typed.text = "";
                stage.gameObject.SetActive(false);
                gate.sizeDelta = new Vector2(w, cy);
                gateLogic.ResetGate();
                holdStatus.text = F("purge.holdDuration", "Hold {0} Seconds", 3);
                SetCaption(holdLabel, L("purge.pressHold", "Press & Hold to Unlock"));
                hold.GetComponent<CanvasGroup>().alpha = 1f;
            });
            float sh = sy + 48f + 8f;
            TL(stage.rectTransform, 20f, cy - 20f + Gap, iw, sh);
            stage.gameObject.SetActive(false);
            return 170f + Gap + cy;
        }

        static GameObject FindOwner(RectTransform inside)
        {
            for (var t = inside.transform; t != null; t = t.parent)
                if (t.name == Root) return t.parent != null ? t.parent.gameObject : null;
            return null;
        }

        static TMP_InputField TypedInput(Transform parent)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "ConfirmDelete";
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 6;
            var bg = go.GetComponent<Image>();
            bg.sprite = UIRoundedSprites.RoundedRect(8f); bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f; bg.color = Lowest;
            if (input.textViewport != null) UiDraw.Fill(input.textViewport, 12f, 0f, 12f, 0f);
            UIFonts.Use(input.textComponent, UiFont.Mono);
            input.textComponent.fontSize = UiMetrics.ScaleTextSize(UiDp) / UiDraw.TextUnitScale; input.textComponent.color = Secondary;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft; input.textComponent.characterSpacing = 2f;
            if (input.placeholder is TextMeshProUGUI ph)
            {
                UIFonts.Use(ph, UiFont.Mono);
                ph.text = "DELETE"; ph.fontSize = UiMetrics.ScaleTextSize(UiDp) / UiDraw.TextUnitScale; ph.fontStyle = FontStyles.Normal;
                ph.color = OutlineVariant; ph.alignment = TextAlignmentOptions.MidlineLeft; ph.characterSpacing = 2f;
            }
            input.onValidateInput = (text, index, c) => char.ToUpperInvariant(c);
            return input;
        }

        // ---------------------------------------------------------------- page footer

        /// <summary>Builds the page footer and returns its height; the protocol line moves to a second
        /// row when it would run into the brand line.</summary>
        static float PageFooter(RectTransform page, float width, float y, ProfileDto profile, bool linked)
        {
            var foot = UiDraw.Box(page, "PageFooter", Lowest);
            TL(foot.rectTransform, 0f, y, width, 66f);
            var brand = UiDraw.Text(foot.transform, "Brand", "HOTDROP", UiFont.HeadingSemiBold, TitleDp, Primary);
            UiDraw.Place(brand.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -33f), new Vector2(UiDraw.Width(brand) + 2f, 26f));
            var arena = UiDraw.Text(foot.transform, "Arena", L("support.footer.arenaTagline", "3v3 Ultra-Wide Tactical Arena"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            float arenaRight = 28f + UiDraw.Width(brand) + UiDraw.Width(arena) + 2f;
            UiDraw.Place(arena.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(28f + UiDraw.Width(brand), -33f), new Vector2(UiDraw.Width(arena) + 2f, 14f));
            string account = profile == null ? Missing(L("state.syncing", "Syncing"), L("state.unavailable", "Unavailable"))
                : StreamSafePresentation.Enabled ? L("identity.privateProfile", "Private Profile")
                : linked ? L("state.linked", "Linked") : L("state.guest", "Guest");
            var right = UiDraw.Text(foot.transform, "Protocol",
                F("footer.protocolTemplate", "SECURE PROTOCOL ENGAGED      <color=#{0}>ACCOUNT: {1}</color>      BUILD {2}", ColorUtility.ToHtmlStringRGB(Primary), account, Application.version.ToUpperInvariant()),
                UiFont.MonoMedium, CaptionDp, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            float rightW = UiDraw.Width(right) + 2f;
            if (arenaRight + 24f + rightW <= width - 16f)
            {
                UiDraw.Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -33f), new Vector2(rightW, 14f));
                return 66f;
            }
            right.alignment = TextAlignmentOptions.MidlineLeft;
            UiDraw.Place(right.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -66f), new Vector2(Mathf.Min(rightW, width - 32f), 14f));
            foot.rectTransform.sizeDelta = new Vector2(width, 92f);
            return 92f;
        }

        // ---------------------------------------------------------------- helpers

        static void Tap(Action action)
        {
            GameAudio.Play2D(GameCue.UiTap);
            action?.Invoke();
        }

        static void Header(RectTransform card, string icon, string label, Color iconTone, float y, float w, Color? textTone = null)
        {
            TL(UiDraw.Icon(card, "HeaderIcon", icon, 18f, iconTone).rectTransform, 20f, y, 18f, 18f);
            TL(UiDraw.Text(card, "Heading", label, UiFont.Heading, CaptionDp, textTone ?? OnVariant, .08f).rectTransform, 42f, y + 2f, w * .6f, 14f);
        }

        static void Badge(RectTransform card, string name, string value, Color fill, Color tone, float right, float y)
        {
            var text = UiDraw.Text(card, name + "Text", value, UiFont.Mono, CaptionDp, tone, .05f, TextAlignmentOptions.Midline);
            float bw = UiDraw.Width(text) + 8f;
            var plate = UiDraw.Box(card, name, fill, 4f);
            plate.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
            UiDraw.Place(plate.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -y - 9f), new Vector2(bw, 18f));
            UiDraw.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -y - 9f), new Vector2(bw, 18f));
        }

        /// <summary>A centred icon + caption pair, the design's <c>flex items-center justify-center gap-4</c>.</summary>
        static TextMeshProUGUI IconLabel(Transform button, string icon, float iconPx, string caption, UiFont font, float px, Color tone, float tracking, Color? iconTone = null)
        {
            var group = UiDraw.Node(button, "Content");
            var label = UiDraw.Text(group, "Caption", caption, font, px, tone, tracking);
            float lw = UiDraw.Width(label) + 2f, total = iconPx + 4f + lw;
            // On a narrow column, step down the type roles (22 -> 17 -> 12 dp) until the pair fits
            // inside the button; never below the 12 dp caption floor.
            float room = ((RectTransform)button).sizeDelta.x - 24f;
            foreach (float smaller in new[] { UiDp, CaptionDp })
            {
                if (room <= 0f || total <= room || smaller >= px) continue;
                px = smaller;
                label.fontSize = UiMetrics.ScaleTextSize(px) / UiDraw.TextUnitScale;
                lw = UiDraw.Width(label) + 2f; total = iconPx + 4f + lw;
            }
            UiDraw.Place(group, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(total, iconPx));
            UiDraw.Place(UiDraw.Icon(group, "Icon", icon, iconPx, iconTone ?? tone).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(iconPx, iconPx));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(iconPx + 4f, 0f), new Vector2(lw, px * 1.4f));
            label.name = "Label";
            return label;
        }

        /// <summary>Changes an <see cref="IconLabel"/> caption and keeps the pair centred.</summary>
        static void SetCaption(TextMeshProUGUI label, string caption)
        {
            if (label == null) return;
            label.text = caption;
            var group = (RectTransform)label.transform.parent;
            float iconPx = group.sizeDelta.y, lw = UiDraw.Width(label) + 2f;
            group.sizeDelta = new Vector2(iconPx + 4f + lw, iconPx);
            label.rectTransform.sizeDelta = new Vector2(lw, label.rectTransform.sizeDelta.y);
        }

        static TextMeshProUGUI Wrapped(Transform parent, string name, string value, UiFont font, float px, Color color, float width, float lineHeight, float tracking = .01f)
        {
            var text = UiDraw.Text(parent, name, value, font, px, color, tracking, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = new Vector2(width, 100f);
            // CSS line-height in px → TMP line spacing (em/100) over the face's natural line height.
            float natural = text.font != null && text.font.faceInfo.pointSize > 0
                ? text.font.faceInfo.lineHeight / text.font.faceInfo.pointSize * px : px * 1.3f;
            text.lineSpacing = (lineHeight - natural) / px * 100f;
            text.ForceMeshUpdate();
            var size = text.GetPreferredValues(value, width, 0f);
            text.rectTransform.sizeDelta = new Vector2(width, size.y);
            return text;
        }

        static RawImage DotGrid(RectTransform parent, Color tone)
        {
            var image = UiDraw.Node(parent, "DotGrid").gameObject.AddComponent<RawImage>();
            image.texture = DotTexture();
            image.color = tone;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            UiDraw.Fill(rt);
            Canvas.ForceUpdateCanvases();
            var size = parent.sizeDelta;
            image.uvRect = new Rect(0f, 0f, Mathf.Max(1f, size.x / 16f), Mathf.Max(1f, size.y / 16f));
            return image;
        }

        static Texture2D dots;
        static Texture2D DotTexture()
        {
            if (dots != null) return dots;
            dots = new Texture2D(16, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[256];
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                // radial-gradient(#38d9ec 1px, transparent 1px): a 1 px radius dot at each cell centre.
                float d = new Vector2(x + .5f - 8f, y + .5f - 8f).magnitude;
                px[y * 16 + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(1.5f - d) * 255f));
            }
            dots.SetPixels32(px);
            dots.Apply(false, true);
            return dots;
        }

        static string ShortId(string id)
        {
            string s = id.Replace("-", "").ToUpperInvariant();
            return s.Length <= 6 ? s : s.Substring(0, 4) + "-" + s.Substring(s.Length - 1);
        }

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TL(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
    }

    /// <summary>The purge gate's deliberate 3-second press-and-hold; releasing early resets it.</summary>
    public sealed class HoldGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Seconds = 3f;
        public RectTransform Fill;
        public float Width;
        public Action Done;
        public Action<bool> Changed;
        float held;
        bool down, done;

        public void OnPointerDown(PointerEventData e)
        {
            if (done) return;
            down = true; held = 0f;
            Changed?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData e) => Abort();
        public void OnPointerExit(PointerEventData e) => Abort();

        public void ResetGate()
        {
            done = false; down = false; held = 0f; Apply();
        }

        void Abort()
        {
            if (done || !down) return;
            down = false; held = 0f; Apply();
            Changed?.Invoke(false);
        }

        void Update()
        {
            if (!down || done) return;
            held += Time.unscaledDeltaTime;
            Apply();
            if (held < Seconds) return;
            done = true; down = false;
            GameAudio.Play2D(GameCue.UiTap);
            Done?.Invoke();
        }

        void Apply()
        {
            if (Fill != null) Fill.sizeDelta = new Vector2(Width * Mathf.Clamp01(held / Seconds), Fill.sizeDelta.y);
        }
    }
}
