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
        public Action Back, Recover, Link, Revoke, AccountSettings, WebDeletion, Profile, Diagnostics, Submit, Purge;
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
        static Color Surface => UiDraw.Hex("#0b1322");
        static Color Lowest => UiDraw.Hex("#060e1d");
        static Color Low => UiDraw.Hex("#141c2b");
        static Color Mid => UiDraw.Hex("#18202f");
        static Color High => UiDraw.Hex("#222a3a");
        static Color Highest => UiDraw.Hex("#2d3545");
        static Color Primary => UiDraw.Hex("#7cefff");
        static Color PrimaryC => UiDraw.Hex("#30d5e8");
        static Color OnPrimary => UiDraw.Hex("#00363c");
        static Color OnPrimaryC => UiDraw.Hex("#005862");
        static Color Secondary => UiDraw.Hex("#ffb2b7");
        static Color SecondaryC => UiDraw.Hex("#930d2d");
        static Color OnSecondary => UiDraw.Hex("#67001b");
        static Color OnSecondaryC => UiDraw.Hex("#ff9da4");
        static Color Tertiary => UiDraw.Hex("#ffda88");
        static Color OnSurface => UiDraw.Hex("#dbe2f8");
        static Color OnVariant => UiDraw.Hex("#bbc9cb");
        static Color OutlineVariant => UiDraw.Hex("#3c494b");

        const float Pad = 24f, Gap = 12f, BarH = 64f;
        const string Root = "SupportHubVisual";

        static UnityEngine.Events.UnityAction<string> counterListener;
        static TMP_InputField counterSource;

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
            if (status != null) status.text = "GATE OPEN";
            SetCaption(gate.Find("DeleteAccount/Content/Label")?.GetComponent<TextMeshProUGUI>(), "SAFETY RELEASE ACTIVE");
            var hold = gate.Find("DeleteAccount")?.GetComponent<CanvasGroup>();
            if (hold != null) hold.alpha = .5f;
        }

        public static void Show(GameObject owner, TMP_InputField category, TMP_InputField detail,
            string matchId, IReadOnlyList<RecentMatch.Entry> roster, int localTeam,
            ProfileDto profile, WalletDto wallet, bool online, SupportHubActions act)
        {
            Clear(owner);
            if (owner == null) return;
            act ??= new SupportHubActions();
            var root = UiDraw.Node(owner.transform, Root);
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Surface).rectTransform);
            float width = root.rect.width > 0f ? root.rect.width : owner.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;

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
            Title(page, width, profile, online, act);
            float colW = (width - Pad * 2f - Gap * 2f) / 3f, top = 72f + 20f;
            float lh = Identity(page, Pad, top, colW, profile, matchId, act);
            float mh = Report(page, Pad + colW + Gap, top, colW, category, detail, roster, localTeam, act);
            float rh = Compliance(page, Pad + (colW + Gap) * 2f, top, colW, act);
            float end = top + Mathf.Max(lh, mh, rh + 110f) + 32f;
            PageFooter(page, width, end, profile, linked);
            page.sizeDelta = new Vector2(width, end + 66f);
            page.anchoredPosition = Vector2.zero;

            TopBar(root, width, profile, wallet, online, act);
            var toast = UiDraw.Box(root, "Toast", High, 8f, UiDraw.Hex("#30d5e8", .45f));
            UiDraw.Place(toast.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 20f), new Vector2(400f, 44f));
            UiDraw.Place(UiDraw.Icon(toast.transform, "Icon", "info", 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
            var toastText = UiDraw.Text(toast.transform, "Text", "", UiFont.MonoMedium, 12f, OnSurface, .03f);
            toastText.overflowMode = TextOverflowModes.Ellipsis;
            UiDraw.Place(toastText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(340f, 20f));
            toast.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- top bar

        static void TopBar(RectTransform root, float width, ProfileDto profile, WalletDto wallet, bool online, SupportHubActions act)
        {
            var bar = UiDraw.Box(root, "TopBar", UiDraw.Hex("#141c2b", .9f)).rectTransform;
            bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(.5f, 1f);
            bar.sizeDelta = new Vector2(0f, BarH); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Shadow", UiDraw.Hex("#000000", .25f), -BarH);

            var back = UiDraw.Tap(bar, "Back", High, 4f, () => Tap(act.Back));
            UiDraw.Place((RectTransform)back.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(48f, 48f));
            UiDraw.Fill(UiDraw.Icon(back.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform);

            var brand = UiDraw.Text(bar, "Brand", "HOTDROP", UiFont.HeadingSemiBold, 20f, Primary, .05f);
            var sub = UiDraw.Text(bar, "Build", "AVIONICS V" + Application.version.ToUpperInvariant(), UiFont.MonoMedium, 11f, OnVariant, .05f);
            TL(brand.rectTransform, 76f, 11f, UiDraw.Width(brand) + 4f, 24f);
            TL(sub.rectTransform, 76f, 36f, UiDraw.Width(sub) + 4f, 14f);
            float chipX = 76f + Mathf.Max(UiDraw.Width(brand), UiDraw.Width(sub)) + 16f;
            var chip = UiDraw.Box(bar, "SystemChip", High, 4f);
            var chipText = UiDraw.Text(chip.transform, "Text", online ? "[SYS // ONLINE // ACCOUNT]" : "[SYS // OFFLINE // ACCOUNT]", UiFont.MonoMedium, 11f, OnSurface, .1f);
            float cw = 8f + 8f + 8f + UiDraw.Width(chipText) + 8f;
            UiDraw.Place(chip.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(chipX, 0f), new Vector2(cw, 22f));
            UiDraw.Place(UiDraw.Dot(chip.transform, "Dot", 8f, online ? Primary : Secondary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(8f, 8f));
            UiDraw.Place(chipText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(UiDraw.Width(chipText) + 2f, 14f));

            // Centre navigation: each tab opens the real page it names.
            var tabs = new (string label, Action go)[]
            {
                ("OPERATIONS", act.Back),
                ("ARMORY", () => act.Navigate?.Invoke("Appearance")),
                ("TELEMETRY", () => act.Navigate?.Invoke("Profile")),
                ("PASS", () => act.Navigate?.Invoke("Contracts")),
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
                    ? UiDraw.Text(item.transform, "Label", t.label, UiFont.BodyBold, 14f, OnPrimaryC, 0f, TextAlignmentOptions.Midline)
                    : UiDraw.Text(item.transform, "Label", t.label, UiFont.Heading, 12f, OnVariant, .08f, TextAlignmentOptions.Midline);
                float iw = UiDraw.Width(label) + 24f;
                UiDraw.Fill(label.rectTransform);
                navItems.Add(((RectTransform)item.transform, iw));
                UiDraw.Place((RectTransform)item.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nx, 0f), new Vector2(iw, 48f));
                nx += iw + 4f;
            }
            UiDraw.Place(nav.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(nx, 36f));
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
            var opText = UiDraw.Text(op.transform, "Text", "<color=#bbc9cb>OP:</color> " + StreamSafePresentation.AccountAlias(profile?.alias ?? "SYNCING").ToUpperInvariant(), UiFont.MonoMedium, 11f, Primary, .05f);
            float ow = UiDraw.Width(opText) + 16f;
            UiDraw.Place(op.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(rx, 0f), new Vector2(ow, 26f));
            UiDraw.Fill(opText.rectTransform, 8f, 0f, 8f, 0f);
            rx -= ow + 12f;
            var credits = UiDraw.Box(bar, "Credits", High, 4f);
            var crText = UiDraw.Text(credits.transform, "Value", StreamSafePresentation.AccountProgress(wallet == null ? "—" : wallet.credits.ToString("N0")), UiFont.Mono, 14f, Tertiary, .03f);
            float crw = 8f + 18f + 4f + UiDraw.Width(crText) + 8f;
            UiDraw.Place(credits.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(rx, 0f), new Vector2(crw, 26f));
            UiDraw.Place(UiDraw.Icon(credits.transform, "Icon", "toll", 18f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(crText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f, 0f), new Vector2(UiDraw.Width(crText) + 2f, 18f));
        }

        // ---------------------------------------------------------------- title

        static void Title(RectTransform page, float width, ProfileDto profile, bool online, SupportHubActions act)
        {
            var card = Card(page, "Title", Mid, Pad, 0f, width - Pad * 2f, 72f);
            var grid = DotGrid(card, UiDraw.Hex("#38d9ec", .2f));
            UiDraw.Fill(grid.rectTransform, 4f, 4f, 4f, 4f);
            var back = UiDraw.Tap(card, "Back", High, 4f, () => Tap(act.Back));
            TL((RectTransform)back.transform, 12f, 12f, 48f, 48f);
            UiDraw.Fill(UiDraw.Icon(back.transform, "Icon", "arrow_back", 24f, Primary).rectTransform);
            string state = profile == null ? "ACCOUNT SYNCING" : profile.isGuest ? "GUEST ACCOUNT" : "LINKED ACCOUNT";
            TL(UiDraw.Text(card, "Kicker", $"ACCOUNT // NODE: AUTH-RECOVERY  <color=#bbc9cb>•</color>  <color=#ffda88>{state}</color>",
                UiFont.MonoMedium, 11f, Primary, .05f).rectTransform, 72f, 15f, 600f, 14f);
            TL(UiDraw.Text(card, "Heading", "ACCOUNT // SECURITY, RECOVERY & INTEGRITY", UiFont.Heading, 28f, OnSurface, -.01f).rectTransform, 72f, 28f, width - 560f, 34f);

            float right = width - Pad * 2f - 12f;
            var fair = UiDraw.Box(card, "FairPlay", Lowest, 4f);
            var fairText = UiDraw.Text(fair.transform, "Text", "FAIRPLAY ACTIVE", UiFont.MonoMedium, 11f, Tertiary, .05f);
            float fw = 12f + 18f + 4f + UiDraw.Width(fairText) + 12f;
            UiDraw.Place(fair.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -36f), new Vector2(fw, 26f));
            UiDraw.Place(UiDraw.Icon(fair.transform, "Icon", "verified_user", 18f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(fairText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(34f, 0f), new Vector2(UiDraw.Width(fairText) + 2f, 14f));
            right -= fw + 8f;
            var session = UiDraw.Box(card, "Session", Lowest, 4f);
            var sLabel = UiDraw.Text(session.transform, "Label", "SESSION:", UiFont.MonoMedium, 11f, OnVariant, .05f);
            var sValue = UiDraw.Text(session.transform, "Value", online ? "AUTHENTICATED" : "OFFLINE", UiFont.Mono, 14f, online ? Primary : Secondary, .03f);
            float sw = 12f + 10f + 4f + UiDraw.Width(sLabel) + 4f + UiDraw.Width(sValue) + 12f;
            UiDraw.Place(session.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(right, -36f), new Vector2(sw, 26f));
            UiDraw.Place(UiDraw.Dot(session.transform, "Dot", 10f, online ? Primary : Secondary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(10f, 10f));
            UiDraw.Place(sLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(26f, 0f), new Vector2(UiDraw.Width(sLabel) + 2f, 14f));
            UiDraw.Place(sValue.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f + UiDraw.Width(sLabel), 0f), new Vector2(UiDraw.Width(sValue) + 2f, 18f));
        }

        // ---------------------------------------------------------------- left column

        static float Identity(RectTransform page, float x, float y, float w, ProfileDto profile, string matchId, SupportHubActions act)
        {
            bool linked = profile != null && !profile.isGuest;
            var card = Card(page, "Account", Mid, x, y, w, 100f);
            float iw = w - 40f, cy = 20f;
            Header(card, "badge", "CALLSIGN IDENTITY", Primary, cy, w);
            Badge(card, "Tier", profile == null ? "SYNCING" : StreamSafePresentation.Enabled ? "PRIVATE PROFILE" : $"LEVEL {Mathf.Clamp(profile.level, 1, 20)} PILOT", High, Primary, w - 20f, cy);
            cy += 18f + 12f;

            var brief = UiDraw.Box(card, "Dossier", Low, 8f);
            TL(brief.rectTransform, 20f, cy, iw, 72f);
            var avatar = UiDraw.Box(brief.transform, "Avatar", Highest, 8f);
            TL(avatar.rectTransform, 8f, 8f, 56f, 56f);
            UiDraw.Fill(UiDraw.Icon(avatar.transform, "Icon", "shield_person", 32f, Primary).rectTransform);
            var stripe = UiDraw.Box(avatar.transform, "Stripe", Primary);
            UiDraw.Place(stripe.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), Vector2.zero, new Vector2(48f, 4f));
            var alias = UiDraw.Text(brief.transform, "Alias", StreamSafePresentation.AccountAlias(profile?.alias ?? "ACCOUNT SYNCING").ToUpperInvariant(), UiFont.HeadingSemiBold, 20f, OnSurface);
            alias.overflowMode = TextOverflowModes.Ellipsis;
            TL(alias.rectTransform, 76f, 12f, iw - 88f, 26f);
            TL(UiDraw.Text(brief.transform, "Account", StreamSafePresentation.Enabled ? "PRIVATE ACCOUNT DETAILS" : profile == null ? "Account data is loading." : linked ? "LINKED  •  GOOGLE PLAY GAMES" : "GUEST  •  STORED ON THIS DEVICE",
                UiFont.Body, 12f, OnVariant, .01f).rectTransform, 76f, 40f, iw - 88f, 16f);
            cy += 72f + 12f;

            var link = UiDraw.Box(card, "LinkStatus", High, 8f);
            TL(link.rectTransform, 20f, cy, iw, 78f);
            TL(UiDraw.Icon(link.transform, "Icon", "sports_esports", 20f, Primary).rectTransform, 12f, 13f, 20f, 20f);
            TL(UiDraw.Text(link.transform, "Name", "GOOGLE PLAY GAMES", UiFont.Mono, 14f, OnSurface, .03f).rectTransform, 38f, 12f, iw - 160f, 22f);
            var state = UiDraw.Text(link.transform, "State", profile == null ? "SYNCING" : linked ? "LINKED" : "NOT LINKED", UiFont.Mono, 11f, linked ? Primary : Tertiary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -23f), new Vector2(UiDraw.Width(state) + 2f, 14f));
            UiDraw.Place(UiDraw.Dot(link.transform, "Dot", 8f, linked ? Primary : Tertiary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f - UiDraw.Width(state), -23f), new Vector2(8f, 8f));
            TL(UiDraw.Text(link.transform, "RecoveryLabel", "CLOUD RECOVERY:", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 12f, 40f, 160f, 14f);
            var recovery = UiDraw.Text(link.transform, "Recovery", profile == null ? "Checking…" : linked ? "Available after reinstall" : "This device only",
                UiFont.MonoMedium, 11f, OnSurface, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(recovery.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -47f), new Vector2(iw - 180f, 14f));
            var bar = UiDraw.Box(link.transform, "Bar", Lowest, 3f);
            TL(bar.rectTransform, 12f, 60f, iw - 24f, 6f);
            if (linked)
                UiDraw.Fill(UiDraw.Box(bar.transform, "Fill", Primary, 3f).rectTransform);
            cy += 78f + 12f;

            var notice = UiDraw.Box(card, "SecurityNotice", Lowest, 8f);
            TL(UiDraw.Icon(notice.transform, "Icon", "info", 18f, Tertiary).rectTransform, 8f, 10f, 18f, 18f);
            var nt = Wrapped(notice.transform, "Text", "<color=#ffda88><b>SECURITY NOTICE:</b></color> " + (linked
                ? "This account is bound to Google Play Games. Sign in with the same Google Play identity after a reinstall or on a new phone to recover it. Accounts are never merged."
                : "Guest accounts live on this device only and are lost on uninstall or hardware handover. Link Google Play Games to make this profile recoverable."),
                UiFont.Body, 12f, OnVariant, iw - 40f, 19.5f);
            TL(nt.rectTransform, 32f, 8f, iw - 40f, nt.preferredHeight);
            float nh = nt.preferredHeight + 16f;
            TL(notice.rectTransform, 20f, cy, iw, nh);
            cy += nh + 12f;

            var sync = UiDraw.Box(card, "Sync", Low, 8f);
            TL(sync.rectTransform, 20f, cy, iw, 46f);
            UiDraw.Place(UiDraw.Icon(sync.transform, "Icon", profile == null ? "cloud_sync" : "cloud_done", 20f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(20f, 20f));
            TL(UiDraw.Text(sync.transform, "Title", profile == null ? "SYNC PENDING" : "SYNC HEALTH NOMINAL", UiFont.MonoMedium, 11f, OnSurface, .05f).rectTransform, 36f, 8f, iw - 44f, 14f);
            var syncBody = UiDraw.Text(sync.transform, "Body", profile == null ? "Waiting for the account service." : "Profile, inventory and credits are saved to your account.", UiFont.Body, 12f, OnVariant, .01f);
            syncBody.overflowMode = TextOverflowModes.Ellipsis;
            TL(syncBody.rectTransform, 36f, 23f, iw - 44f, 16f);
            cy += 46f + 12f + 4f;

            var recover = UiDraw.Tap(card, "RecoverAccount", PrimaryC, 8f, () => Tap(act.Recover));
            TL((RectTransform)recover.transform, 20f, cy, iw, 48f);
            UiDraw.Glow((RectTransform)recover.transform, UiDraw.Hex("#000000", .25f), 8f, 6f);
            IconLabel(recover.transform, "sync", 20f, "RECOVER LINKED ACCOUNT", UiFont.HeadingSemiBold, 20f, OnPrimaryC, .025f);
            cy += 48f + 4f;
            float half = (iw - 4f) / 2f;
            var manage = UiDraw.Tap(card, "ManageAccount", High, 8f, () => Tap(act.AccountSettings));
            TL((RectTransform)manage.transform, 20f, cy, half, 48f);
            IconLabel(manage.transform, "manage_accounts", 18f, "ACCOUNT & EMAIL", UiFont.Heading, 12f, OnSurface, .08f, Tertiary);
            var second = UiDraw.Tap(card, linked ? "RevokeLink" : "LinkGooglePlay", High, 8f, () => Tap(linked ? act.Revoke : act.Link));
            TL((RectTransform)second.transform, 20f + half + 4f, cy, half, 48f);
            if (linked) IconLabel(second.transform, "link_off", 18f, "REVOKE LINK", UiFont.Heading, 12f, Secondary, .08f);
            else IconLabel(second.transform, "add_link", 18f, "LINK GOOGLE PLAY", UiFont.Heading, 12f, Primary, .08f);
            cy += 48f + 20f;
            card.sizeDelta = new Vector2(w, cy);

            var node = Card(page, "MatchNode", Low, x, y + cy + Gap, w, 52f);
            UiDraw.Place(UiDraw.Icon(node, "Icon", "network_ping", 20f, OnVariant).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(UiDraw.Text(node, "Label", "LAST ONLINE MATCH", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(200f, 14f));
            UiDraw.Place(UiDraw.Text(node, "Match", string.IsNullOrEmpty(matchId) ? "NONE RECORDED" : "MATCH_ID #" + ShortId(matchId), UiFont.Mono, 14f, Primary, .03f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(w * .5f, 18f));
            return cy + Gap + 52f;
        }

        // ---------------------------------------------------------------- centre column

        static readonly (string icon, string label, string reason)[] Violations =
        {
            ("sports_martial_arts", "DISRUPTIVE PLAY", "Disruptive play"),
            ("campaign", "HARASSMENT", "Harassment"),
            ("cell_wifi", "CHEATING / EXPLOIT", "Cheating or exploit"),
            ("hourglass_disabled", "INACTIVITY / AFK", "Inactivity / AFK"),
        };

        static float Report(RectTransform page, float x, float y, float w, TMP_InputField category, TMP_InputField detail,
            IReadOnlyList<RecentMatch.Entry> roster, int localTeam, SupportHubActions act)
        {
            var card = Card(page, "Report", Mid, x, y, w, 100f);
            float iw = w - 40f, cy = 20f;
            Header(card, "gavel", "TACTICAL INTEGRITY", Primary, cy, w);
            Badge(card, "Tolerance", "ZERO TOLERANCE", Highest, Tertiary, w - 20f, cy);
            cy += 18f + 12f;
            TL(UiDraw.Text(card, "Title", "REPORT MATCH PARTICIPANT", UiFont.HeadingSemiBold, 20f, OnSurface).rectTransform, 20f, cy, iw, 26f);
            var desc = Wrapped(card, "Context", "Reports are reviewed by the HOTDROP team together with the match record. Pick who, why and what happened.",
                UiFont.Body, 12f, OnVariant, iw, 16f);
            TL(desc.rectTransform, 20f, cy + 28f, iw, desc.preferredHeight);
            cy += 28f + desc.preferredHeight + 12f;

            bool has = roster != null && roster.Count > 0;
            TL(UiDraw.Text(card, "RosterLabel", "RECENT ENGAGEMENT ROSTER", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 20f, cy, iw * .6f, 14f);
            UiDraw.Place(UiDraw.Text(card, "RosterWindow", "LAST ONLINE MATCH", UiFont.MonoRegular, 11f, Primary, .05f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -cy - 7f), new Vector2(iw * .4f, 14f));
            cy += 14f + 4f;
            if (!has)
            {
                var empty = UiDraw.Box(card, "NoRecentMatch", Low, 8f);
                TL(empty.rectTransform, 20f, cy, iw, 48f);
                UiDraw.Place(UiDraw.Text(empty.transform, "Title", "NO RECENT ONLINE MATCH", UiFont.HeadingSemiBold, 12f, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 8f), new Vector2(iw, 16f));
                UiDraw.Place(UiDraw.Text(empty.transform, "Detail", "REPORTS OPEN AFTER YOUR NEXT ONLINE MATCH", UiFont.MonoMedium, 10f, OnVariant, .05f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -9f), new Vector2(iw, 14f));
                cy += 48f + 12f;
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
                    var tint = UiDraw.Box(button.transform, "Selected", UiDraw.Hex("#30d5e8", .2f), 8f);
                    UiDraw.Fill(tint.rectTransform);
                    tint.enabled = false;
                    var name = UiDraw.Text(button.transform, "Name", StreamSafePresentation.PlayerName(entry.alias ?? "PLAYER", i), UiFont.HeadingSemiBold, 12f, OnSurface, 0f, TextAlignmentOptions.Midline);
                    name.overflowMode = TextOverflowModes.Ellipsis;
                    UiDraw.Place(name.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 8f), new Vector2(cw - 8f, 16f));
                    string side = localTeam < 0 ? $"SLOT {entry.slot + 1}" : entry.team == localTeam ? "ALLIED WINGMAN" : "CORAL OPPONENT";
                    UiDraw.Place(UiDraw.Text(button.transform, "Side", side, UiFont.MonoMedium, 10f, OnVariant, .05f, TextAlignmentOptions.Midline).rectTransform,
                        new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -9f), new Vector2(cw - 8f, 14f));
                    cards.Add((tint, name));
                    button.onClick.AddListener(() =>
                    {
                        GameAudio.Play2D(GameCue.UiTap);
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

            TL(UiDraw.Text(card, "ViolationLabel", "VIOLATION CLASS", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 20f, cy, iw, 14f);
            cy += 18f;
            if (category != null)
            {
                category.text = "";
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
                var tint = UiDraw.Box(pill.transform, "Selected", UiDraw.Hex("#30d5e8", .2f), 8f);
                UiDraw.Fill(tint.rectTransform);
                tint.enabled = false;
                var icon = UiDraw.Icon(pill.transform, "Icon", v.icon, 18f, OnSurface);
                UiDraw.Place(icon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(18f, 18f));
                var label = UiDraw.Text(pill.transform, "Label", v.label, UiFont.Heading, 12f, OnSurface, .08f);
                UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(30f, 0f), new Vector2(pw - 36f, 16f));
                pills.Add((tint, icon, label));
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
            TL(UiDraw.Text(card, "LogLabel", "FLIGHT INCIDENT LOG", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 20f, cy, iw * .6f, 14f);
            var counter = UiDraw.Text(card, "Counter", $"{detail?.text?.Length ?? 0} / {limit} CHARS", UiFont.MonoMedium, 11f, Primary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(counter.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -cy - 7f), new Vector2(iw * .4f, 14f));
            cy += 18f;
            if (detail != null)
            {
                detail.gameObject.SetActive(true);
                detail.transform.SetParent(card, false);
                var rt = (RectTransform)detail.transform;
                TL(rt, 20f, cy, iw, 72f);
                StyleTextArea(detail);
                if (counterSource != null && counterListener != null) counterSource.onValueChanged.RemoveListener(counterListener);
                counterSource = detail;
                counterListener = v => { if (counter != null) counter.text = $"{(v ?? "").Length} / {limit} CHARS"; };
                detail.onValueChanged.AddListener(counterListener);
            }
            cy += 72f + 12f + 4f;

            float bw = (iw - 4f) / 2f;
            var clear = UiDraw.Tap(card, "ClearReport", High, 8f, () =>
            {
                GameAudio.Play2D(GameCue.UiTap);
                if (detail != null) detail.text = "";
                if (category != null) category.text = "";
                foreach (var p in pills) { p.tint.enabled = false; p.icon.color = p.label.color = OnSurface; }
            });
            TL((RectTransform)clear.transform, 20f, cy, bw, 48f);
            UiDraw.Fill(UiDraw.Text(clear.transform, "Label", "CLEAR", UiFont.HeadingSemiBold, 14f, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform);
            var submit = UiDraw.Tap(card, "Submit", Primary, 8f, () => Tap(act.Submit));
            TL((RectTransform)submit.transform, 20f + bw + 4f, cy, bw, 48f);
            UiDraw.Glow((RectTransform)submit.transform, UiDraw.Hex("#000000", .25f), 8f, 6f);
            IconLabel(submit.transform, "send", 18f, "TRANSMIT REPORT", UiFont.HeadingSemiBold, 14f, OnPrimary, 0f);
            cy += 48f + 4f;

            var policy = UiDraw.Box(card, "ReportNotice", Low, 8f);
            TL(UiDraw.Icon(policy.transform, "Icon", "verified", 18f, Primary).rectTransform, 8f, 10f, 18f, 18f);
            var pt = Wrapped(policy.transform, "Text", "<color=#7cefff><b>INTEGRITY POLICY:</b></color> Every report is paired with the selected match for human review. Repeated false reports are not acted on.",
                UiFont.Body, 12f, OnVariant, iw - 40f, 15f);
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
                input.textComponent.fontSize = 14f;
                input.textComponent.enableAutoSizing = false;
                input.textComponent.color = OnSurface;
                input.textComponent.alignment = TextAlignmentOptions.TopLeft;
                input.textComponent.textWrappingMode = TextWrappingModes.Normal;
                input.textComponent.characterSpacing = 3f;
            }
            if (input.placeholder is TextMeshProUGUI placeholder)
            {
                UIFonts.Use(placeholder, UiFont.Mono);
                placeholder.text = "Describe abnormal trajectory, grief behavior, or communication breach...";
                placeholder.fontSize = 14f;
                placeholder.enableAutoSizing = false;
                placeholder.fontStyle = FontStyles.Normal;
                placeholder.color = UiDraw.Hex("#859396", .7f);
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
            Header(card, "terminal", "COMPLIANCE & SUPPORT", Primary, 20f, w);
            var build = UiDraw.Text(card, "Build", "BUILD " + Application.version.ToUpperInvariant(), UiFont.MonoMedium, 11f, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(build.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -29f), new Vector2(160f, 14f));
            var tiles = new (string name, string icon, Color tone, string title, string sub, Action go)[]
            {
                ("Privacy", "policy", Primary, "PRIVACY", "DATA & EMAIL", act.AccountSettings),
                ("WebDeletion", "description", Primary, "WEB REQUEST", "DELETION PAGE", act.WebDeletion),
                ("Profile", "support_agent", Primary, "OPERATIVE", "PERSONAL RECORD", act.Profile),
                ("Diagnostics", "bug_report", Tertiary, "DIAGNOSTICS", "SYS TELEMETRY", act.Diagnostics),
            };
            float tw = (iw - 4f) / 2f;
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                var tile = UiDraw.Tap(card, "Support_" + t.name, High, 8f, () => Tap(t.go));
                TL((RectTransform)tile.transform, 20f + (i % 2) * (tw + 4f), 50f + (i / 2) * 52f, tw, 48f);
                UiDraw.Place(UiDraw.Icon(tile.transform, "Icon", t.icon, 20f, t.tone).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(20f, 20f));
                TL(UiDraw.Text(tile.transform, "Title", t.title, UiFont.Heading, 12f, OnSurface, .08f).rectTransform, 36f, 9f, tw - 40f, 16f);
                TL(UiDraw.Text(tile.transform, "Sub", t.sub, UiFont.MonoMedium, 10f, OnVariant, .05f).rectTransform, 36f, 25f, tw - 40f, 14f);
            }

            float py = y + 170f + Gap;
            var gate = Card(page, "Purge", UiDraw.Hex("#930d2d", .3f), x, py, w, 100f);
            float cy = 20f;
            Header(gate, "warning", "DESTRUCTIVE PURGE GATE", Secondary, cy, w, Secondary);
            Badge(gate, "Safety", "2-STEP SAFETY", Secondary, OnSecondary, w - 20f, cy);
            cy += 18f + 12f;
            TL(UiDraw.Text(gate, "Title", "PERMANENT ACCOUNT PURGE", UiFont.HeadingSemiBold, 20f, Secondary).rectTransform, 20f, cy, iw, 26f);
            TL(UiDraw.Text(gate, "Detail", "Irreversible deletion of your HOTDROP account:", UiFont.Body, 12f, OnVariant, .01f).rectTransform, 20f, cy + 30f, iw, 16f);
            cy += 30f + 16f + 12f;

            var ledger = UiDraw.Box(gate, "Ledger", Lowest, 8f);
            var rows = new (string icon, Color tone, string text)[]
            {
                ("close", Secondary, "DELETED: Profile, progression, inventory, credits, reports & match history"),
                ("lock_clock", OnVariant, "RETAINED: Purchase records where the law requires them"),
                ("history_toggle_off", Tertiary, "SECOND STEP: type DELETE to confirm. This cannot be undone"),
            };
            float ly = 8f;
            foreach (var r in rows)
            {
                var line = Wrapped(ledger.transform, "Line", r.text, UiFont.MonoMedium, 11f, r.tone, iw - 44f, 14f, .05f);
                TL(UiDraw.Icon(ledger.transform, "Icon", r.icon, 16f, r.tone).rectTransform, 8f, ly, 16f, 16f);
                TL(line.rectTransform, 28f, ly + 1f, iw - 44f, line.preferredHeight);
                ly += Mathf.Max(16f, line.preferredHeight) + 4f;
            }
            float lh = ly + 4f;
            TL(ledger.rectTransform, 20f, cy, iw, lh);
            cy += lh + 12f;

            TL(UiDraw.Text(gate, "HoldLabel", "TACTICAL SAFETY RELEASE", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 20f, cy, iw * .6f, 14f);
            var holdStatus = UiDraw.Text(gate, "HoldStatus", "HOLD 3 SECONDS", UiFont.Mono, 11f, Secondary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(holdStatus.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -cy - 7f), new Vector2(iw * .4f, 14f));
            cy += 18f;
            var hold = UiDraw.Tap(gate, "DeleteAccount", SecondaryC, 8f, null);
            TL((RectTransform)hold.transform, 20f, cy, iw, 52f);
            hold.gameObject.AddComponent<CanvasGroup>();
            hold.gameObject.AddComponent<RectMask2D>();
            var fill = UiDraw.Box(hold.transform, "Progress", UiDraw.Hex("#ffb2b7", .4f));
            UiDraw.Place(fill.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(0f, 52f));
            var holdLabel = IconLabel(hold.transform, "fingerprint", 20f, "PRESS & HOLD TO UNLOCK", UiFont.HeadingSemiBold, 20f, OnSecondaryC, .05f);
            var gateLogic = hold.gameObject.AddComponent<HoldGate>();
            gateLogic.Fill = fill.rectTransform; gateLogic.Width = iw;
            gateLogic.Changed = holding => holdStatus.text = holding ? "OVERRIDE ENGAGING..." : "HOLD 3 SECONDS";
            gateLogic.Done = () => RevealPurge(FindOwner(gate));
            cy += 52f + 20f;
            gate.sizeDelta = new Vector2(w, cy);

            // Typed second step, shown once the hold completes.
            var stage = UiDraw.Box(gate, "StageTwo", High, 8f);
            var prompt = Wrapped(stage.transform, "Prompt", "TYPE \"DELETE\" TO CONFIRM — This deletes your profile, progression, inventory, credits and match history.",
                UiFont.MonoMedium, 11f, Secondary, iw - 16f, 14f, .05f);
            TL(prompt.rectTransform, 8f, 8f, iw - 16f, prompt.preferredHeight);
            float sy = 8f + prompt.preferredHeight + 6f;
            var typed = TypedInput(stage.transform);
            float purgeW = 96f, cancelW = 96f;
            TL((RectTransform)typed.transform, 8f, sy, iw - 16f - purgeW - cancelW - 8f, 48f);
            var cancel = UiDraw.Tap(stage.transform, "CancelPurge", Highest, 8f, null);
            TL((RectTransform)cancel.transform, iw - 8f - purgeW - 4f - cancelW, sy, cancelW, 48f);
            UiDraw.Fill(UiDraw.Text(cancel.transform, "Label", "CANCEL", UiFont.HeadingSemiBold, 14f, OnSurface, 0f, TextAlignmentOptions.Midline).rectTransform);
            var purge = UiDraw.Tap(stage.transform, "Purge", Secondary, 8f, () => Tap(act.Purge));
            TL((RectTransform)purge.transform, iw - 8f - purgeW, sy, purgeW, 48f);
            UiDraw.Fill(UiDraw.Text(purge.transform, "Label", "PURGE", UiFont.HeadingSemiBold, 20f, OnSecondary, 0f, TextAlignmentOptions.Midline).rectTransform);
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
                holdStatus.text = "HOLD 3 SECONDS";
                SetCaption(holdLabel, "PRESS & HOLD TO UNLOCK");
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
            input.textComponent.fontSize = 18f; input.textComponent.color = Secondary;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft; input.textComponent.characterSpacing = 2f;
            if (input.placeholder is TextMeshProUGUI ph)
            {
                UIFonts.Use(ph, UiFont.Mono);
                ph.text = "DELETE"; ph.fontSize = 18f; ph.fontStyle = FontStyles.Normal;
                ph.color = OutlineVariant; ph.alignment = TextAlignmentOptions.MidlineLeft; ph.characterSpacing = 2f;
            }
            input.onValidateInput = (text, index, c) => char.ToUpperInvariant(c);
            return input;
        }

        // ---------------------------------------------------------------- page footer

        static void PageFooter(RectTransform page, float width, float y, ProfileDto profile, bool linked)
        {
            var foot = UiDraw.Box(page, "PageFooter", Lowest);
            TL(foot.rectTransform, 0f, y, width, 66f);
            var brand = UiDraw.Text(foot.transform, "Brand", "HOTDROP", UiFont.HeadingSemiBold, 20f, Primary);
            UiDraw.Place(brand.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(UiDraw.Width(brand) + 2f, 26f));
            UiDraw.Place(UiDraw.Text(foot.transform, "Arena", "3V3 ULTRA-WIDE TACTICAL ARENA", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(28f + UiDraw.Width(brand), 0f), new Vector2(320f, 14f));
            string account = profile == null ? "SYNCING" : linked ? "LINKED" : "GUEST";
            var right = UiDraw.Text(foot.transform, "Protocol",
                $"SECURE PROTOCOL ENGAGED      <color=#7cefff>ACCOUNT: {account}</color>      BUILD {Application.version.ToUpperInvariant()}",
                UiFont.MonoMedium, 11f, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(right.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(width * .6f, 14f));
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
            TL(UiDraw.Text(card, "Heading", label, UiFont.Heading, 12f, textTone ?? OnVariant, .08f).rectTransform, 42f, y + 2f, w * .6f, 14f);
        }

        static void Badge(RectTransform card, string name, string value, Color fill, Color tone, float right, float y)
        {
            var text = UiDraw.Text(card, name + "Text", value, UiFont.Mono, 11f, tone, .05f, TextAlignmentOptions.Midline);
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
