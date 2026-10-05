using System;
using System.Linq;
using Hotdrop.Config;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 16 operative profile, rebuilt from its UIUX HTML (CSS px) as a scrolling page with a
    /// sticky footer. Every figure is the account's or the local save's own; what the profile feed
    /// does not carry is shown as unavailable, never estimated.
    /// </summary>
    public static class OperativeProfileView
    {
        static readonly Color Lowest = UiDraw.Hex("#060e1d");
        static readonly Color Low = UiDraw.Hex("#141c2b");
        static readonly Color Mid = UiDraw.Hex("#18202f");
        static readonly Color High = UiDraw.Hex("#222a3a");
        static readonly Color Highest = UiDraw.Hex("#2d3545");
        static readonly Color Primary = UiDraw.Hex("#7cefff");
        static readonly Color PrimaryC = UiDraw.Hex("#30d5e8");
        static readonly Color Tertiary = UiDraw.Hex("#ffda88");
        static readonly Color OnSurface = UiDraw.Hex("#dbe2f8");
        static readonly Color OnVariant = UiDraw.Hex("#bbc9cb");
        static readonly Color Outline = UiDraw.Hex("#859396");

        const float Pad = 24f;

        public static void SetStatus(GameObject owner, string message)
        {
            var label = owner != null ? owner.transform.Find("OperativeProfileVisual/Footer/Status")?.GetComponent<TextMeshProUGUI>() : null;
            if (label != null) label.text = message ?? "";
        }

        public static void Show(GameObject owner, ProfileDto profile, WalletDto wallet,
            ProfileStatsDto stats, OfflineChallengeController challenges, string state,
            Action back, Action appearance)
        {
            if (owner == null) return;
            var previous = owner.transform.Find("OperativeProfileVisual");
            if (previous != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(previous.gameObject);
                else UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }
            var root = UiDraw.Node(owner.transform, "OperativeProfileVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", UiDraw.Hex("#0b1322")).rectTransform);
            float width = root.rect.width > 0f ? root.rect.width : owner.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;
            float w = width - Pad * 2f;
            var viewport = UiDraw.Node(root, "Viewport");
            UiDraw.Fill(viewport, 0f, 0f, 0f, 76f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var page = UiDraw.Node(viewport, "Page");
            page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;

            float y = 20f;
            // Title block.
            TL(UiDraw.Dot(page, "KickerDot", 8f, PrimaryC).rectTransform, Pad, y + 4f, 8f, 8f);
            TL(UiDraw.Text(page, "Kicker", "FLIGHT DOSSIER  //  NON-DECAYING RECORD", UiFont.Mono, 11f, Primary, .1f).rectTransform, Pad + 16f, y, 400f, 16f);
            var title = UiDraw.Text(page, "Title", "OPERATIVE PROFILE <color=#30d5e8>//</color> PERSONAL IMPROVEMENT", UiFont.Heading, 40f, OnSurface, -.02f, TextAlignmentOptions.TopLeft);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = LineSpacing(title, 48f);
            TL(title.rectTransform, Pad, y + 16f, w * .62f, 96f);
            TL(UiDraw.Icon(page, "TaglineIcon", "verified", 14f, Primary).rectTransform, Pad, y + 119f, 14f, 14f);
            TL(UiDraw.Text(page, "Tagline", "Authoritative Skill Ledger  •  Zero Public Rating Shaming  •  Permanent Onboarding Track", UiFont.Body, 14f, OnVariant).rectTransform, Pad + 20f, y + 117f, w * .62f, 18f);
            float pillX = Pad + w * .672f;
            Pill(page, "Insignia", "military_tech", "INSIGNIA: ", profile == null || string.IsNullOrEmpty(profile.badgeId) ? "NONE" : CosmeticLook.DisplayName(profile.badgeId).ToUpperInvariant(), pillX, y + 89f);
            Pill(page, "Calibration", "history", "", "PERSISTENT CALIBRATION", pillX, y + 123f);
            y += 160f;

            // Identity + accreditation.
            float leftW = (w - 16f) * .5855f, rightW = w - 16f - leftW;
            Identity(page, Pad, y, leftW, profile, wallet);
            Accreditation(page, Pad + leftW + 16f, y, rightW, profile);
            y += CardH + 28f;

            // Metrics.
            UiDraw.Place(UiDraw.Icon(page, "MetricsIcon", "monitoring", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(Pad, -y - 12f), new Vector2(20f, 20f));
            var mh = UiDraw.Text(page, "MetricsHeading", "CORE PERSONAL IMPROVEMENT METRICS", UiFont.Heading, 20f, OnSurface, -.01f);
            TL(mh.rectTransform, Pad + 28f, y, UiDraw.Width(mh) + 4f, 26f);
            var drift = UiDraw.Text(page, "Drift", "CONSTRUCTIVE DRIFT", UiFont.Mono, 10f, Primary, .1f, TextAlignmentOptions.Midline);
            float dw = UiDraw.Width(drift) + 12f;
            var driftPlate = UiDraw.Box(page, "DriftPlate", High, 4f);
            driftPlate.transform.SetSiblingIndex(drift.transform.GetSiblingIndex());
            TL(driftPlate.rectTransform, Pad + 36f + UiDraw.Width(mh), y + 4f, dw, 18f); TL(drift.rectTransform, Pad + 36f + UiDraw.Width(mh), y + 4f, dw, 18f);
            var logs = UiDraw.Text(page, "Logs", "VERIFIED ACCOUNT TELEMETRY", UiFont.MonoMedium, 11f, OnSurface, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(logs.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 13f), new Vector2(320f, 16f));
            y += 38f;
            int matches = stats?.eligibleMatches ?? 0, wins = stats?.wins ?? 0, draws = stats?.draws ?? 0;
            string winRate = stats == null || matches == 0 ? "—" : $"{100f * wins / matches:0.0}%";
            var metrics = new (string id, string title, string icon, string value, Color tone, string sub, string left, string right)[]
            {
                ("Matches", "PUBLIC MATCHES", "flag", stats == null ? "—" : matches.ToString(), OnSurface, "ELIGIBLE SESSIONS", "VERIFIED", "ACCOUNT"),
                ("Wins", "POSITIVE TRAJECTORY", "north_east", winRate, Primary, stats == null ? "WAITING FOR RECORD" : $"{wins} WINS / {draws} DRAWS", stats == null ? "" : $"{wins} POSITIVE", stats == null ? "" : $"{Mathf.Max(0, matches - wins - draws)} DEFEATS"),
                ("Draws", "DRAWN MATCHES", "balance", stats == null ? "—" : draws.ToString(), OnSurface, "EVEN RESULTS", "VERIFIED", "ACCOUNT"),
                ("Credits", "EARNED CREDITS", "toll", wallet == null ? "—" : wallet.credits.ToString("N0"), Tertiary, "COSMETIC CURRENCY", "EARNED ONLY", "NO PURCHASES"),
                ("Privacy", "PRIVATE RATING", "visibility_off", "HIDDEN", Outline, "NOT DISPLAYED", "NO PUBLIC", "SHAMING"),
            };
            float mw = (w - 4f * 16f) / 5f;
            for (int i = 0; i < metrics.Length; i++)
            {
                var m = metrics[i];
                var card = Card(page, "Metric_" + m.id, Mid, Pad + i * (mw + 16f), y, mw, 160f);
                TL(UiDraw.Text(card, "Title", m.title, UiFont.Heading, 12f, OnVariant, .05f).rectTransform, 14f, 16f, mw - 50f, 16f);
                UiDraw.Place(UiDraw.Icon(card, "Icon", m.icon, 18f, m.id == "Credits" ? Tertiary : Primary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -24f), new Vector2(18f, 18f));
                TL(UiDraw.Text(card, "Value", m.value, UiFont.Heading, 40f, m.tone, -.03f).rectTransform, 14f, 42f, mw - 28f, 48f);
                TL(UiDraw.Text(card, "Source", m.sub, UiFont.Mono, 11f, OnVariant, .1f).rectTransform, 14f, 96f, mw - 28f, 14f);
                TL(UiDraw.Text(card, "Left", m.left, UiFont.Mono, 11f, Primary, .05f).rectTransform, 14f, 128f, mw * .5f, 14f);
                var right = UiDraw.Text(card, "Right", m.right, UiFont.Mono, 11f, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -135f), new Vector2(mw * .5f, 14f));
            }
            y += 160f + 24f;

            // Offline accomplishments + equipped identity.
            float halfW = (w - 20f) / 2f;
            float oh = Offline(page, Pad, y, halfW, challenges);
            float eh = Loadout(page, Pad + halfW + 20f, y, halfW, profile);
            y += Mathf.Max(oh, eh) + 24f;
            var hash = UiDraw.Text(page, "Record", "HOTDROP PERSONAL GROWTH RECORD  •  STORED WITH YOUR ACCOUNT", UiFont.MonoMedium, 11f, OnVariant, .1f);
            TL(UiDraw.Icon(page, "RecordIcon", "shield", 14f, Primary).rectTransform, Pad, y, 14f, 14f);
            TL(hash.rectTransform, Pad + 22f, y - 1f, w, 16f);
            page.sizeDelta = new Vector2(width, y + 40f);
            page.anchoredPosition = Vector2.zero;
            Footer(root, state, back, appearance);
        }

        static void Pill(RectTransform page, string name, string icon, string label, string value, float left, float y)
        {
            var pill = UiDraw.Box(page, name, Low, 8f);
            var text = UiDraw.Text(pill.transform, "Text", label + $"<color=#ffda88>{value}</color>", UiFont.Mono, 11f, OnSurface, .1f);
            if (string.IsNullOrEmpty(label)) text.text = $"<color=#7cefff>{value}</color>";
            float pw = 12f + 14f + 6f + UiDraw.Width(text) + 12f;
            UiDraw.Place(pill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(left, -y), new Vector2(pw, 26f));
            UiDraw.Place(UiDraw.Icon(pill.transform, "Icon", icon, 14f, string.IsNullOrEmpty(label) ? Primary : Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(UiDraw.Width(text) + 2f, 14f));
        }

        static void Identity(RectTransform page, float x, float y, float w, ProfileDto profile, WalletDto wallet)
        {
            var card = Card(page, "Identity", Low, x, y, w, CardH);
            TL(UiDraw.Icon(card, "Icon", "flight", 14f, Primary).rectTransform, 20f, 20f, 14f, 14f);
            TL(UiDraw.Text(card, "Heading", "AVIONIC ID  //  PUBLIC IDENTITY", UiFont.Mono, 11f, Primary, .1f).rectTransform, 40f, 19f, 300f, 16f);
            var avatar = UiDraw.Box(card, "Avatar", High, 12f);
            TL(avatar.rectTransform, 20f, 52f, 64f, 64f);
            UiDraw.Fill(UiDraw.Icon(avatar.transform, "Icon", "shield_person", 32f, Primary).rectTransform);
            if (profile != null)
            {
                var lvl = UiDraw.Dot(card, "LevelDot", 22f, PrimaryC);
                TL(lvl.rectTransform, 70f, 104f, 22f, 22f);
                UiDraw.Fill(UiDraw.Text(lvl.transform, "Level", Mathf.Clamp(profile.level, 1, 20).ToString(), UiFont.Mono, 10f, UiDraw.Hex("#00363c"), 0f, TextAlignmentOptions.Midline).rectTransform);
            }
            TL(UiDraw.Text(card, "Alias", profile?.alias?.ToUpperInvariant() ?? "ACCOUNT SYNCING", UiFont.Heading, 28f, OnSurface, -.03f).rectTransform, 100f, 54f, w - 360f, 34f);
            TL(UiDraw.Text(card, "Account", profile == null ? "Account data is loading." : profile.isGuest ? "Guest account  •  stored on this device" : "Linked account  •  recovery available",
                UiFont.Mono, 11f, OnVariant, .05f).rectTransform, 100f, 92f, w - 360f, 16f);
            var seals = UiDraw.Text(card, "SealsLabel", "EARNED CREDITS:", UiFont.Heading, 12f, Tertiary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(seals.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-120f, -66f), new Vector2(140f, 16f));
            var sealPlate = UiDraw.Box(card, "SealsPlate", Mid, 4f);
            UiDraw.Place(sealPlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -66f), new Vector2(92f, 26f));
            UiDraw.Fill(UiDraw.Text(sealPlate.transform, "Value", wallet == null ? "—" : wallet.credits.ToString("N0") + " CR", UiFont.Mono, 14f, Tertiary, .05f, TextAlignmentOptions.Midline).rectTransform);
            var footRule = UiDraw.Box(card, "Rule", UiDraw.Hex("#3c494b", .4f));
            UiDraw.Place(footRule.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 44f), new Vector2(w - 40f, 1f));
            UiDraw.Place(UiDraw.Dot(card, "PrivacyDot", 6f, PrimaryC).rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(20f, 24f), new Vector2(6f, 6f));
            UiDraw.Place(UiDraw.Text(card, "Privacy", "PUBLIC IDENTITY: ACTIVE COMPETITIVE LEDGER", UiFont.MonoMedium, 11f, OnSurface, .1f).rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(34f, 24f), new Vector2(w * .6f, 14f));
            UiDraw.Place(UiDraw.Text(card, "Verified", "ZERO MMR LEAK  •  ROOFTOP SPORT VERIFIED", UiFont.MonoMedium, 11f, Primary, .1f, TextAlignmentOptions.MidlineRight).rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-20f, 24f), new Vector2(w * .45f, 14f));
        }

        static void Accreditation(RectTransform page, float x, float y, float w, ProfileDto profile)
        {
            var card = Card(page, "Progression", Low, x, y, w, CardH);
            TL(UiDraw.Icon(card, "Icon", "trending_up", 14f, Primary).rectTransform, 20f, 20f, 14f, 14f);
            TL(UiDraw.Text(card, "Heading", "PERMANENT ACCREDITATION TRACK", UiFont.Heading, 12f, Primary, .05f).rectTransform, 40f, 19f, 300f, 16f);
            var phase = UiDraw.Text(card, "Phase", "ONBOARDING", UiFont.Mono, 11f, Primary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(phase.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -27f), new Vector2(140f, 14f));
            int level = profile != null ? Mathf.Clamp(profile.level, 1, 20) : 0;
            float frac = level / 20f;
            var ring = UiDraw.Place(UiDraw.Node(card, "Ring"), new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -99f), new Vector2(104f, 104f));
            ring.pivot = new Vector2(0f, .5f);
            UiDraw.Fill(UiDraw.Ring(ring, "Track", 104f, 6f, Highest).rectTransform);
            var fill = UiDraw.Ring(ring, "Fill", 104f, 6f, PrimaryC);
            UiDraw.Fill(fill.rectTransform);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true; fill.fillAmount = frac;
            UiDraw.Place(UiDraw.Text(ring, "Caption", "ONBOARD", UiFont.Mono, 9f, OnVariant, .05f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 20f), new Vector2(90f, 12f));
            UiDraw.Place(UiDraw.Text(ring, "Lvl", profile == null ? "LVL —" : $"LVL {level}", UiFont.Heading, 20f, OnSurface, -.02f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 0f), new Vector2(90f, 26f));
            UiDraw.Place(UiDraw.Text(ring, "Pct", profile == null ? "" : $"{frac * 100f:0.0}%", UiFont.Mono, 10f, Primary, .05f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -20f), new Vector2(90f, 12f));
            var levelText = UiDraw.Text(card, "Level", profile == null ? "LEVEL —" : $"LEVEL {level}<size=16><color=#bbc9cb> / 20</color></size>", UiFont.Heading, 28f, OnSurface, -.03f);
            TL(levelText.rectTransform, 144f, 64f, w - 164f, 34f);
            TL(UiDraw.Text(card, "XP", profile == null ? "Waiting for account progress" : $"{profile.profileXp:N0} TOTAL PROFILE XP", UiFont.Mono, 14f, Primary, .05f).rectTransform, 144f, 102f, w - 164f, 18f);
            var bar = UiDraw.Box(card, "XPBar", Highest, 3f);
            TL(bar.rectTransform, 144f, 129f, w - 250f, 6f);
            var bf = UiDraw.Box(bar.transform, "Fill", PrimaryC, 3f);
            UiDraw.Place(bf.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((w - 250f) * frac, 6f));
            var guarantee = UiDraw.Box(card, "Guarantee", Lowest, 8f);
            TL(guarantee.rectTransform, 20f, 164f, w - 40f, 69f);
            UiDraw.Place(UiDraw.Icon(guarantee.transform, "Icon", "shield", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
            var gt = UiDraw.Text(guarantee.transform, "Rule", "<color=#7cefff>Permanent Guarantee:</color> Onboarding level never resets or decays between seasonal deployments. Match results are verified before they are recorded.", UiFont.MonoRegular, 11f, OnSurface, .03f);
            gt.textWrappingMode = TextWrappingModes.Normal;
            gt.lineSpacing = LineSpacing(gt, 18f);
            UiDraw.Place(gt.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(w - 96f, 56f));
        }

        static float Offline(RectTransform page, float x, float y, float w, OfflineChallengeController challenges)
        {
            var card = Card(page, "OfflineAccomplishments", Low, x, y, w, 100f);
            UiDraw.Place(UiDraw.Icon(card, "Icon", "explore", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -32f), new Vector2(20f, 20f));
            TL(UiDraw.Text(card, "Heading", "OFFLINE TRAVEL ACCOMPLISHMENTS", UiFont.Heading, 20f, OnSurface, -.01f).rectTransform, 48f, 20f, w - 220f, 26f);
            var tag = UiDraw.Text(card, "Tag", "EXPEDITION DOSSIER", UiFont.Mono, 11f, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -32f), new Vector2(200f, 14f));
            TL(UiDraw.Text(card, "Note", "Solo training circuits and rooftop trials. Device-local medals; no online rewards.", UiFont.Body, 12f, OnVariant).rectTransform, 20f, 52f, w - 40f, 16f);
            var savedLocal = challenges == null ? new LocalChallengeSave() : null;
            float cy = 82f;
            var icons = new[] { "military_tech", "apartment", "lock" };
            var names = new[] { "FIRST FLIGHT", "ROOFTOP OPERATOR", "RELAY CAPTAIN" };
            for (int i = 0; i < 3; i++)
            {
                var chapter = (ChallengeChapter)i;
                var authored = challenges?.config?.GetChallengesForChapter(chapter)
                    ?? OfflineChallengeConfig.GetAuthoredEntries().Where(e => e.chapter == chapter).ToList();
                int total = authored?.Count ?? 0;
                int completed = authored?.Count(e => challenges != null
                    ? challenges.RecordForCurrentContext(e.id)?.isCompleted == true
                    : savedLocal.Data.records.Any(r => r.challengeId == e.id && r.isCompleted && r.challengeVersion == e.version)) ?? 0;
                int pct = total > 0 ? Mathf.RoundToInt(100f * completed / total) : 0;
                var row = UiDraw.Box(card, "Chapter_" + i, Lowest, 8f);
                TL(row.rectTransform, 20f, cy, w - 40f, 76f);
                var plate = UiDraw.Box(row.transform, "IconPlate", High, 8f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(48f, 48f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", completed > 0 || i == 0 ? icons[i] == "lock" ? "flag" : icons[i] : "lock", 22f, completed == total && total > 0 ? Tertiary : Primary).rectTransform);
                var title = UiDraw.Text(row.transform, "Name", $"CHAPTER {i + 1}: {names[i]}", UiFont.Heading, 18f, OnSurface, -.01f);
                TL(title.rectTransform, 72f, 12f, UiDraw.Width(title) + 2f, 24f);
                var pctText = UiDraw.Text(row.transform, "Pct", $"{pct}%", UiFont.Mono, 10f, UiDraw.Hex("#3f2e00"), 0f, TextAlignmentOptions.Midline);
                float pw = UiDraw.Width(pctText) + 10f;
                var pctPlate = UiDraw.Box(row.transform, "PctPlate", pct == 100 ? Tertiary : PrimaryC, 3f);
                pctPlate.transform.SetSiblingIndex(pctText.transform.GetSiblingIndex());
                TL(pctPlate.rectTransform, 76f + UiDraw.Width(title), 15f, pw, 18f); TL(pctText.rectTransform, 76f + UiDraw.Width(title), 15f, pw, 18f);
                TL(UiDraw.Text(row.transform, "Count", total == 0 ? "NOT LOADED" : $"{completed}/{total} Completed", UiFont.Mono, 11f, completed == total && total > 0 ? Tertiary : Primary, .05f).rectTransform, 72f, 38f, w - 160f, 14f);
                UiDraw.Place(UiDraw.Icon(row.transform, "State", completed == total && total > 0 ? "check_circle" : completed > 0 ? "sync" : "play_circle", 20f, Primary).rectTransform,
                    new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(20f, 20f));
                cy += 76f + 10f;
            }
            float h = cy + 10f;
            card.sizeDelta = new Vector2(w, h);
            return h;
        }

        static float Loadout(RectTransform page, float x, float y, float w, ProfileDto profile)
        {
            var card = Card(page, "SavedIdentity", Low, x, y, w, 100f);
            UiDraw.Place(UiDraw.Icon(card, "Icon", "auto_awesome", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -32f), new Vector2(20f, 20f));
            TL(UiDraw.Text(card, "Heading", "EQUIPPED LOADOUT & VISUAL IDENTITY", UiFont.Heading, 20f, OnSurface, -.01f).rectTransform, 48f, 20f, w - 68f, 26f);
            TL(UiDraw.Text(card, "Policy", "Cosmetic identity does not alter combat physics.", UiFont.Body, 12f, OnVariant).rectTransform, 20f, 52f, w - 40f, 16f);
            var slots = new[]
            {
                ("Body", "accessibility_new", "ACTIVE CHASSIS RIG", profile == null ? "Syncing" : CosmeticLook.DisplayName(profile.bodyStyleId), "Body style"),
                ("Wrap", "colorize", "PRIMARY COSMETIC WRAP", profile == null ? "Syncing" : CosmeticLook.DisplayName(profile.weaponWrapId), "ARC rifle wrap"),
                ("Badge", "badge", "PUBLIC DISPLAY BADGE", profile == null ? "Syncing" : string.IsNullOrEmpty(profile.badgeId) ? "None" : CosmeticLook.DisplayName(profile.badgeId), "Flight card badge"),
            };
            float cy = 82f;
            foreach (var s in slots)
            {
                var row = UiDraw.Box(card, s.Item1, Lowest, 8f);
                TL(row.rectTransform, 20f, cy, w - 40f, 76f);
                var plate = UiDraw.Box(row.transform, "IconPlate", High, 8f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(48f, 48f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", s.Item2, 24f, Primary).rectTransform);
                TL(UiDraw.Text(row.transform, "Label", s.Item3, UiFont.Heading, 11f, OnVariant, .05f).rectTransform, 72f, 12f, w - 120f, 14f);
                TL(UiDraw.Text(row.transform, "Value", s.Item4, UiFont.BodySemiBold, 20f, OnSurface).rectTransform, 72f, 28f, w - 120f, 26f);
                TL(UiDraw.Text(row.transform, "Detail", s.Item5, UiFont.Mono, 11f, Primary, .05f).rectTransform, 72f, 54f, w - 120f, 14f);
                cy += 76f + 10f;
            }
            float h = cy + 10f;
            card.sizeDelta = new Vector2(w, h);
            return h;
        }

        static void Footer(RectTransform root, string state, Action back, Action appearance)
        {
            var bar = UiDraw.Box(root, "Footer", UiDraw.Hex("#101827", .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 76f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UiDraw.Hex("#3c494b", .5f), 0f);
            var home = UiDraw.Tap(bar, "Back", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var homeText = UiDraw.Text(home.transform, "Label", "RETURN TO HOME", UiFont.HeadingSemiBold, 20f, OnSurface, .01f);
            float hw = 20f + 20f + 12f + UiDraw.Width(homeText) + 24f;
            UiDraw.Place((RectTransform)home.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(hw, 52f));
            UiDraw.Place(UiDraw.Icon(home.transform, "Icon", "home", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(homeText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(homeText) + 2f, 26f));
            var status = UiDraw.Text(bar, "Status", state ?? "", UiFont.MonoMedium, 12f, OnVariant, .05f);
            status.overflowMode = TextOverflowModes.Ellipsis;
            UiDraw.Place(status.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad + hw + 16f, 0f), new Vector2(500f, 16f));
            var edit = UiDraw.Tap(bar, "Appearance", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); appearance?.Invoke(); });
            var editText = UiDraw.Text(edit.transform, "Label", "EDIT APPEARANCE", UiFont.HeadingSemiBold, 20f, Primary, .01f);
            float ew = 20f + 20f + 12f + UiDraw.Width(editText) + 24f;
            UiDraw.Place((RectTransform)edit.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(ew, 52f));
            UiDraw.Place(UiDraw.Icon(edit.transform, "Icon", "checkroom", 20f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(editText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(editText) + 2f, 26f));
        }

        const float CardH = 252f;

        /// <summary>CSS line-height in px as TMP line spacing over the face's natural line height.</summary>
        static float LineSpacing(TMP_Text text, float lineHeight)
        {
            float px = text.fontSize;
            float natural = text.font != null && text.font.faceInfo.pointSize > 0
                ? text.font.faceInfo.lineHeight / text.font.faceInfo.pointSize * px : px * 1.3f;
            return (lineHeight - natural) / px * 100f;
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
}
