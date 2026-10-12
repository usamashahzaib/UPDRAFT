using System;
using System.Linq;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screens 12 (Contracts) and 13 (Flight-Log Mastery), rebuilt from their UIUX HTML (CSS px) as
    /// scrolling pages over a sticky footer. Progress, seals and thresholds come from the backend's
    /// GoalsDto; when verified progression is not enabled in this build the same page shows its
    /// honest locked state. Never invents rewards or progress.
    /// </summary>
    public static class GoalsPagesView
    {
        static string L(string key, string fallback) => UiDraw.L("goals." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("goals." + key, fallback, args);

        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color Mid => Color.Lerp(UIDesignTokens.Background, UIDesignTokens.SurfacePanel, .55f);
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Highest => UIDesignTokens.SurfaceDisabled;
        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color PrimaryC => UIDesignTokens.PrimaryAction;
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color Gold => UIDesignTokens.ObjectiveGold;
        static Color SecondaryAccent => UiPresentationProfile.Palette.ActionObjectiveMix;
        static Color Bronze => UiDraw.Hex("#d39b6a");
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.TextMuted;
        static Color OnPrimary => UIDesignTokens.TextOnAccent;

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleLargeDp;
        const float Pad = 40f;

        public static void Clear(GameObject owner)
        {
            var old = owner != null ? owner.transform.Find("GoalsVisual") : null;
            if (old == null) return;
            old.gameObject.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject);
            else UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        sealed class Resize : MonoBehaviour
        {
            internal GameObject owner;
            internal GoalsDto data;
            internal Action back, other;
            internal Action<string> select;
            internal bool mastery, preview;
            internal float builtWidth;

            internal void RefreshIfNeeded()
            {
                if (owner == null || !gameObject.activeInHierarchy) return;
                float width = ((RectTransform)transform).rect.width;
                if (width > 0f && Mathf.Abs(width - builtWidth) > 4f)
                    Build(owner, data, back, other, mastery, preview, select);
            }

            void LateUpdate() => RefreshIfNeeded();
        }

        // Also used by synchronous screenshot rendering, which changes the canvas size without
        // running a LateUpdate between the resize and the drawn frame.
        public static void RefreshVisible(Canvas canvas)
        {
            if (canvas == null) return;
            foreach (var page in canvas.GetComponentsInChildren<Resize>()) page.RefreshIfNeeded();
        }

        /// <summary>Reachable page when server-verified progression is not enabled in this build.</summary>
        public static void Unavailable(GameObject owner, bool mastery, Action back, Action other)
        {
            if (mastery) Build(owner, new GoalsDto(), back, other, true, true, null);
            else Build(owner, new GoalsDto(), back, other, false, true, null);
        }

        /// <summary>Truthful retry surface when the feature is enabled but its server data failed to load.</summary>
        public static void LoadFailed(GameObject owner, bool mastery, Action back, Action retry)
        {
            Clear(owner);
            if (owner == null) return;
            var root = UiDraw.Node(owner.transform, "GoalsVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Lowest).rectTransform);
            float width = root.rect.width > 0f ? root.rect.width : 1280f;
            float cardWidth = Mathf.Min(520f, Mathf.Max(280f, width - Pad * 2f));
            var card = UiDraw.Box(root, "LoadFailure", Low, 12f);
            UiDraw.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(cardWidth, 172f));
            var title = UiDraw.Text(card.transform, "Title", mastery ? L("mastery.unavailableTitle", "Mastery Progress Unavailable") : L("contracts.unavailableTitle", "Contracts Unavailable"),
                UiFont.Heading, UiDp, OnSurface, .02f, TextAlignmentOptions.Midline);
            UiDraw.Place(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -30f), new Vector2(cardWidth - 60f, 28f));
            var detail = UiDraw.Text(card.transform, "Detail", L("loadFailure.detail", "We couldn't load verified progress. No progress or rewards were changed."),
                UiFont.Body, UiDp, OnVariant, .01f, TextAlignmentOptions.Midline);
            detail.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(detail.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(cardWidth - 60f, 52f));
            Footer(root, mastery, false, back, null, retry, dataUnavailable: true);
        }

        public static void Contracts(GameObject owner, GoalsDto data, Action back, Action mastery, Action<string> select) =>
            Build(owner, data, back, mastery, false, false, select);

        public static void Mastery(GameObject owner, GoalsDto data, Action back, Action contracts) =>
            Build(owner, data, back, contracts, true, false, null);

        static void Build(GameObject owner, GoalsDto data, Action back, Action other, bool mastery, bool preview, Action<string> select)
        {
            Clear(owner);
            if (owner == null || data == null) return;
            var root = UiDraw.Node(owner.transform, "GoalsVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Lowest).rectTransform);
            Canvas.ForceUpdateCanvases();
            float width = root.rect.width > 0f ? root.rect.width : owner.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;
            float contentW = width - Pad * 2f;

            var viewport = UiDraw.Node(root, "Viewport");
            UiDraw.Fill(viewport, 0f, 0f, 0f, 88f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var page = UiDraw.Node(viewport, "Page");
            page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;

            float y = mastery ? MasteryPage(page, contentW, data, preview) : ContractsPage(page, contentW, data, preview, select);
            page.sizeDelta = new Vector2(width, y + 24f);
            page.anchoredPosition = Vector2.zero;
            Footer(root, mastery, preview, back, other);
            var resize = root.gameObject.AddComponent<Resize>();
            resize.owner = owner; resize.data = data; resize.back = back; resize.other = other;
            resize.select = select; resize.mastery = mastery; resize.preview = preview;
            resize.builtWidth = width;
        }

        // ------------------------------------------------------------ contracts

        static float ContractsPage(RectTransform page, float w, GoalsDto data, bool preview, Action<string> select)
        {
            float y = 24f;
            // Doctrine header.
            var head = Card(page, "Doctrine", Low, Pad, y, w, 136f, 16f);
            var badge = UiDraw.Box(head, "Shield", High, 8f);
            TL(badge.rectTransform, 26f, 46f, 44f, 44f);
            UiDraw.Fill(UiDraw.Icon(badge.transform, "Icon", "verified_user", 24f, Primary).rectTransform);
            var kicker = UiDraw.Text(head, "Kicker", L("contracts.kicker", "Contracts"), UiFont.Heading, CaptionDp, Primary, .08f);
            TL(kicker.rectTransform, 86f, 24f, UiDraw.Width(kicker) + 2f, 16f);
            TL(UiDraw.Text(head, "Net", L("contracts.eligibleMatches", "•  ELIGIBLE PUBLIC MATCHES"), UiFont.MonoMedium, CaptionDp, OnVariant, .1f).rectTransform, 86f + UiDraw.Width(kicker) + 8f, 24f, 220f, 16f);
            var title = UiDraw.Text(head, "Title", L("contracts.heroTitle", "PLAY MATCHES.\nEARN FLIGHT SEALS."), UiFont.Heading, TitleDp, OnSurface, -.03f);
            title.alignment = TextAlignmentOptions.TopLeft;
            TL(title.rectTransform, 86f, 44f, 520f, 80f);
            float rx = w * .5f + 40f;
            var seals = UiDraw.Box(head, "Seals", UIDesignTokens.WithAlpha(High, .8f), 8f);
            TL(seals.rectTransform, rx, 18f, 390f, 52f);
            var sealIcon = UiDraw.Dot(seals.transform, "Icon", 32f, UIDesignTokens.WithAlpha(Gold, .2f));
            UiDraw.Place(sealIcon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(32f, 32f));
            UiDraw.Fill(UiDraw.Icon(sealIcon.transform, "Glyph", "military_tech", 18f, Gold).rectTransform);
            var count = UiDraw.Text(seals.transform, "Count", preview ? "—" : data.seals.ToString(), UiFont.Mono, UiDp, Gold);
            TL(count.rectTransform, 64f, 8f, UiDraw.Width(count) + 2f, 20f);
            TL(UiDraw.Text(seals.transform, "Label", L("contracts.flightSeals", "Flight Seals"), UiFont.Heading, CaptionDp, OnSurface, .08f).rectTransform, 64f + UiDraw.Width(count) + 8f, 10f, 160f, 16f);
            TL(UiDraw.Text(seals.transform, "Sub", L("contracts.sealsEarnedOnly", "Earned in matches  •  Never sold"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 64f, 28f, 310f, 16f);
            int matches = Mathf.Min(2, data.eligiblePublicMatches);
            var lockPill = UiDraw.Box(head, "Unlock", UIDesignTokens.WithAlpha(High, .8f), 8f);
            var lockText = UiDraw.Text(lockPill.transform, "Text", preview ? L("contracts.progressionUnavailable", "Verified match progress unavailable")
                : F("contracts.unlockState", "Eligible matches: {0}/2 ({1})", matches, data.unlocked ? L("state.unlocked", "Unlocked") : L("state.locked", "Locked")), UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            float lw = 16f + 14f + 8f + UiDraw.Width(lockText) + 16f;
            TL(lockPill.rectTransform, rx, 80f, lw, 32f);
            UiDraw.Place(UiDraw.Icon(lockPill.transform, "Icon", data.unlocked && !preview ? "lock_open" : "lock", 14f, OnVariant).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(lockText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(38f, 0f), new Vector2(UiDraw.Width(lockText) + 2f, 14f));
            y += 136f + 24f;

            // Rules strip.
            var rules = Card(page, "Rules", Low, Pad, y, w, 54f, 12f);
            UiDraw.Place(UiDraw.Icon(rules, "Info", "info", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(16f, 16f));
            var rulesText = UiDraw.Text(rules, "Text", preview
                    ? L("contracts.previewRules", "Contracts advance only after verified public matches. They are unavailable right now, so progress and rewards are not shown.")
                    : data.unlocked
                        ? L("contracts.activeRules", "Choose one contract at a time. Progress never expires or resets. Switching pauses a contract until you choose it again.")
                        : F("contracts.lockedRules", "Eligible matches left to unlock Contracts: {0}.", Mathf.Max(0, 2 - data.eligiblePublicMatches)),
                UiFont.Body, UiDp, OnVariant, .01f);
            rulesText.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(rulesText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(w - 76f, 42f));
            y += 54f + 28f;

            // Section header.
            TL(UiDraw.Dot(page, "SectionDot", 8f, Primary).rectTransform, Pad, y + 8f, 8f, 8f);
            TL(UiDraw.Text(page, "Section", L("contracts.section", "Your Contracts"), UiFont.Heading, UiDp, OnSurface, .01f).rectTransform, Pad + 16f, y, 400f, 24f);
            var templates = data.templates ?? Array.Empty<GoalTemplateDto>();
            var instances = data.goals ?? Array.Empty<GoalInstanceDto>();
            int engaged = instances.Count(g => g != null && g.status == "active");
            var activeLabel = UiDraw.Text(page, "Active", preview ? L("contracts.activeUnavailable", "Active Contract Unavailable")
                : F("contracts.activeCount", "Active Contract: {0}/1", engaged), UiFont.MonoMedium, CaptionDp, OnSurface, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(activeLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 12f), new Vector2(320f, 16f));
            y += 24f + 20f;

            // Three directive cards.
            string[] ids = { "finish_together", "hold_the_relay", "build_a_team_route" };
            float cw = (w - 2f * 24f) / 3f, cardH = 462f;
            for (int i = 0; i < 3; i++)
            {
                var template = templates.FirstOrDefault(t => t != null && t.id == ids[i]) ?? (i < templates.Length ? templates[i] : null);
                string id = template?.id ?? ids[i];
                var instance = instances.FirstOrDefault(g => g != null && g.templateId == id);
                int target = instance?.target ?? template?.target ?? DefaultTarget(id);
                int progress = instance?.progress ?? 0;
                bool active = instance != null && instance.status == "active";
                bool done = instance != null && instance.status == "completed";
                bool paused = instance != null && instance.status == "paused";
                bool ready = active && progress >= target && target > 0;
                Directive(page, Pad + i * (cw + 24f), y, cw, cardH, i, id, progress, target, active, done, paused, ready,
                    data.unlocked && !preview, preview, select);
            }
            y += cardH + 40f;

            // Flight seal exchange.
            UiDraw.Place(UiDraw.Icon(page, "ExchangeIcon", "deployed_code", 22f, Gold).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(Pad, -y - 12f), new Vector2(22f, 22f));
            TL(UiDraw.Text(page, "ExchangeTitle", L("contracts.exchangeTitle", "Use Flight Seals"), UiFont.Heading, UiDp, OnSurface, .01f).rectTransform, Pad + 32f, y, 400f, 26f);
            TL(UiDraw.Text(page, "ExchangeSubtitle", L("contracts.exchangeSubtitle", "Use earned seals for cosmetics  •  Never sold for cash"), UiFont.Body, UiDp, OnVariant).rectTransform, Pad, y + 28f, 600f, 16f);
            var policy = UiDraw.Box(page, "ExchangePolicy", Low, 8f);
            var policyText = UiDraw.Text(policy.transform, "Text", L("contracts.exchangePolicy", "No Timers  •  No Rotations  •  No Duplicates"), UiFont.MonoMedium, CaptionDp, OnSurface, .1f);
            float pw = 16f + 16f + 8f + UiDraw.Width(policyText) + 16f;
            UiDraw.Place(policy.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 16f), new Vector2(pw, 32f));
            UiDraw.Place(UiDraw.Icon(policy.transform, "Icon", "verified", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(policyText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(UiDraw.Width(policyText) + 2f, 14f));
            y += 60f;
            var exchange = Card(page, "Exchange", Low, Pad, y, w, 110f, 12f);
            bool catalog = data.rewardCatalogComplete && !preview;
            UiDraw.Place(UiDraw.Icon(exchange, "Icon", catalog ? "checkroom" : "inventory_2", 32f, catalog ? Primary : Outline).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(28f, 0f), new Vector2(32f, 32f));
            TL(UiDraw.Text(exchange, "Catalog", catalog ? L("contracts.catalogAvailable", "Cosmetics Available") : L("contracts.exchangeUnavailable", "Rewards Unavailable"), UiFont.Heading, UiDp, catalog ? Primary : OnSurface, .05f).rectTransform, 80f, 28f, w - 120f, 22f);
            var catalogBody = UiDraw.Text(exchange, "Body", catalog ? L("contracts.catalogBody", "Open Appearance to review owned items and equip earned cosmetics.")
                : L("contracts.exchangeBlockedBody", "Spending Flight Seals is not available yet. No items can be purchased."),
                UiFont.Body, UiDp, OnVariant);
            catalogBody.textWrappingMode = TextWrappingModes.Normal;
            TL(catalogBody.rectTransform, 80f, 54f, w - 120f, 34f);
            y += 110f + 24f;

            // Integrity standard.
            var integrity = Card(page, "Integrity", Lowest, Pad, y, w, 82f, 12f);
            var scale = UiDraw.Box(integrity, "IconPlate", High, 8f);
            UiDraw.Place(scale.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(44f, 44f));
            UiDraw.Fill(UiDraw.Icon(scale.transform, "Icon", "balance", 22f, Primary).rectTransform);
            TL(UiDraw.Text(integrity, "Title", L("contracts.integrityTitle", "Cosmetics Never Change Gameplay"), UiFont.Heading, CaptionDp, OnSurface, .08f).rectTransform, 84f, 18f, w - 110f, 16f);
            var integrityBody = UiDraw.Text(integrity, "Body", L("contracts.integrityBody", "Cosmetics only change appearance. They do not change recoil, bullet speed, hitboxes, jump-pad force, or movement. Earn Flight Seals only through match objectives."),
                UiFont.Body, UiDp, OnVariant);
            integrityBody.textWrappingMode = TextWrappingModes.Normal;
            TL(integrityBody.rectTransform, 84f, 36f, w - 110f, 34f);
            return y + 82f;
        }

        static void Directive(RectTransform page, float x, float y, float w, float h, int index, string id, int progress, int target,
            bool active, bool done, bool paused, bool ready, bool actionable, bool preview, Action<string> select)
        {
            var card = Card(page, "Directive_" + index, active ? Mid : Low, x, y, w, h, 12f);
            if (active)
            {
                UiDraw.Glow(card, UIDesignTokens.WithAlpha(PrimaryC, .2f), 12f, 16f);
                var tabText = UiDraw.Text(page, "CurrentTab", ready ? L("contracts.activeReady", "ACTIVE  ·  READY") : L("contracts.active", "Active"), UiFont.Heading, CaptionDp, OnPrimary, .08f, TextAlignmentOptions.Midline);
                float tw = UiDraw.Width(tabText) + 24f;
                var tab = UiDraw.Box(page, "CurrentTabPlate", PrimaryC, 11f);
                tab.transform.SetSiblingIndex(tabText.transform.GetSiblingIndex());
                TL(tab.rectTransform, x + 20f, y - 11f, tw, 22f);
                TL(tabText.rectTransform, x + 20f, y - 11f, tw, 22f);
            }
            float inner = w - 44f;
            TL(UiDraw.Text(card, "Number", F("contracts.number", "Contract {0}", index + 1), UiFont.MonoMedium, CaptionDp, active ? Primary : Outline, .1f).rectTransform, 22f, 26f, inner - 120f, 16f);
            int percent = Pct(progress, target);
            string state = preview ? L("state.locked", "LOCKED") : done ? L("state.complete", "Complete") : ready ? L("state.ready100", "100% Ready") : active ? F("state.inProgress", "In Progress ({0}%)", percent) : paused ? F("state.paused", "Paused ({0}%)", percent) : F("state.available", "Available ({0}%)", percent);
            var stateText = UiDraw.Text(card, "State", state, UiFont.MonoMedium, CaptionDp, ready ? Gold : OnVariant, .1f, TextAlignmentOptions.Midline);
            float sw = UiDraw.Width(stateText) + 18f;
            var statePlate = UiDraw.Box(card, "StatePlate", ready ? UIDesignTokens.WithAlpha(Gold, .15f) : High, 4f);
            statePlate.transform.SetSiblingIndex(stateText.transform.GetSiblingIndex());
            UiDraw.Place(statePlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -34f), new Vector2(sw, 22f));
            UiDraw.Place(stateText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -34f), new Vector2(sw, 22f));
            TL(UiDraw.Text(card, "Name", Name(id), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 22f, 44f, inner, 26f);
            var desc = UiDraw.Text(card, "Description", Description(id), UiFont.Body, UiDp, OnSurface);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 22f, 84f, inner, 44f);
            // Progress box.
            var meter = UiDraw.Box(card, "Progress", Lowest, 8f);
            TL(meter.rectTransform, 22f, 136f, inner, 60f);
            const float meterInset = 14f, countWidth = 170f, meterGap = 12f;
            float metricWidth = Mathf.Max(1f, inner - meterInset * 2f - countWidth - meterGap);
            var metric = UiDraw.Text(meter.transform, "Caption", Metric(id), UiFont.Heading, CaptionDp, active ? Primary : OnSurface, .08f);
            metric.text = UiDraw.Truncate(metric, metric.text, metricWidth);
            TL(metric.rectTransform, meterInset, 14f, metricWidth, 16f);
            var countText = UiDraw.Text(meter.transform, "Count", F("contracts.progressCount", "{0} / {1} {2}", preview ? "—" : progress, target, Unit(id)), UiFont.Mono, UiDp, active ? Primary : OnSurface, .03f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(countText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-meterInset, -22f), new Vector2(countWidth, 18f));
            var track = UiDraw.Box(meter.transform, "Track", Highest, 3f);
            TL(track.rectTransform, 14f, 40f, inner - 28f, 6f);
            var fill = UiDraw.Box(track.transform, "Fill", active ? PrimaryC : Outline, 3f);
            UiDraw.Place(fill.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((inner - 28f) * Mathf.Clamp01(target > 0 ? progress / (float)target : 0f), 6f));
            // Condition box.
            var cond = UiDraw.Box(card, "Condition", Lowest, 8f);
            const float conditionBoxHeight = 136f;
            TL(cond.rectTransform, 22f, 208f, inner, conditionBoxHeight);
            var condText = UiDraw.Text(cond.transform, "Text", $"<color=#{ColorUtility.ToHtmlStringRGB(OnSurface)}><b>{L("contracts.conditionLabel", "Do this:")}</b></color> " + Condition(id), UiFont.Body, UiDp, OnVariant);
            condText.textWrappingMode = TextWrappingModes.Normal; condText.alignment = TextAlignmentOptions.TopLeft;
            TL(condText.rectTransform, 12f, 12f, inner - 24f, conditionBoxHeight - 24f);
            // Yield row.
            TL(UiDraw.Text(card, "YieldLabel", ready ? L("contracts.guaranteedToken", "Reward:") : L("contracts.yield", "Reward:"), UiFont.MonoMedium, CaptionDp, ready ? Primary : Outline, .1f).rectTransform, 22f, h - 104f, 160f, 16f);
            var yieldText = UiDraw.Text(card, "Yield", L("contracts.flightSealYield", "+1 Flight Seal"), UiFont.Mono, UiDp, Gold, .03f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(yieldText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -h + 96f), new Vector2(140f, 18f));
            UiDraw.Place(UiDraw.Icon(card, "YieldIcon", "military_tech", 16f, Gold).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f - UiDraw.Width(yieldText) - 6f, -h + 96f), new Vector2(16f, 16f));
            // Action.
            string caption = preview ? L("contracts.progressionUnavailableAction", "Progress unavailable") : done ? L("contracts.completedAction", "Completed") : ready ? L("contracts.verifiedAwardAction", "Seal awarded after verification") : active ? L("contracts.currentAction", "Current Contract") : paused ? L("contracts.resumeAction", "Resume Contract") : L("contracts.switchAction", "Switch to Contract");
            bool can = actionable && !active && !done;
            var button = UiDraw.Tap(card, "Select", ready ? Gold : High, 8f, can ? (Action)(() => select?.Invoke(id)) : null);
            button.interactable = can;
            UiDraw.Place((RectTransform)button.transform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 22f), new Vector2(inner, 52f));
            var label = UiDraw.Text(button.transform, "Label", caption, UiFont.BodySemiBold, UiDp, ready ? OnPrimary : can ? OnSurface : Outline, .01f);
            float lw = UiDraw.Width(label);
            string icon = ready ? "stars" : done ? "check_circle" : preview ? "lock" : active ? "radio_button_checked" : "sync_alt";
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", icon, 18f, label.color).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 8f) / 2f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(13f, 0f), new Vector2(lw + 2f, 22f));
        }

        // ------------------------------------------------------------ mastery

        static float MasteryPage(RectTransform page, float w, GoalsDto data, bool preview)
        {
            float y = 24f;
            const float HeadH = 194f;
            var head = Card(page, "Record", Low, Pad, y, w, HeadH, 12f);
            float hx = 24f;
            var chip1 = UiDraw.Text(head, "Immutable", L("mastery.immutableRecord", "Verified Progress"), UiFont.MonoMedium, CaptionDp, Primary, .1f);
            float c1 = UiDraw.Width(chip1);
            var chip1Plate = UiDraw.Box(head, "ImmutablePlate", UIDesignTokens.WithAlpha(PrimaryC, .1f), 4f, UIDesignTokens.WithAlpha(PrimaryC, .4f));
            chip1Plate.transform.SetSiblingIndex(chip1.transform.GetSiblingIndex());
            TL(chip1Plate.rectTransform, hx, 24f, c1 + 34f, 18f);
            UiDraw.Place(UiDraw.Icon(chip1Plate.transform, "Icon", "verified", 12f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(12f, 12f));
            TL(chip1.rectTransform, hx + 26f, 25f, c1 + 2f, 16f);
            hx += c1 + 34f + 8f;
            var mid = UiDraw.Text(head, "Archive", L("mastery.archiveLog", "Lifetime Progress"), UiFont.MonoMedium, CaptionDp, Outline, .1f);
            TL(mid.rectTransform, hx, 25f, UiDraw.Width(mid) + 2f, 16f);
            hx += UiDraw.Width(mid) + 8f;
            var chip2 = UiDraw.Text(head, "Wipes", L("mastery.zeroSeasonWipes", "No Season Reset"), UiFont.MonoMedium, CaptionDp, Gold, .1f, TextAlignmentOptions.Midline);
            float c2 = UiDraw.Width(chip2) + 12f;
            var chip2Plate = UiDraw.Box(head, "WipesPlate", UIDesignTokens.WithAlpha(Gold, .15f), 4f);
            chip2Plate.transform.SetSiblingIndex(chip2.transform.GetSiblingIndex());
            TL(chip2Plate.rectTransform, hx, 24f, c2, 18f);
            TL(chip2.rectTransform, hx, 24f, c2, 18f);
            // h1 + inline subtitle span, wrapping inside the left column exactly like the HTML flow.
            var title = UiDraw.Text(head, "Title", $"{L("mastery.title", "Flight Mastery")} <color=#{ColorUtility.ToHtmlStringRGB(Primary)}><cspace=1>{L("mastery.permanentRecord", "Never Expires")}</cspace></color>",
                UiFont.Heading, TitleDp, OnSurface, -.02f, TextAlignmentOptions.BottomLeft);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = 8f;
            TL(title.rectTransform, 24f, 44f, w * .58f, 92f);
            var body = UiDraw.Text(head, "Body", L("mastery.body", "These badges track skill from verified matches and never expire. Cosmetics never change weapon power, jump-pad force, or movement speed."),
                UiFont.Body, UiDp, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            body.lineSpacing = 4f;
            TL(body.rectTransform, 24f, 138f, w * .58f, 44f);
            var insignia = UiDraw.Box(head, "Insignia", UIDesignTokens.WithAlpha(High, .8f), 12f);
            UiDraw.Place(insignia.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, 0f), new Vector2(428f, 72f));
            var iconBox = UiDraw.Box(insignia.transform, "IconPlate", Highest, 8f);
            UiDraw.Place(iconBox.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(iconBox.transform, "Icon", "alt_route", 22f, Primary).rectTransform);
            TL(UiDraw.Text(insignia.transform, "Kicker", L("mastery.activeInsignia", "Equipped Badge"), UiFont.MonoMedium, CaptionDp, OnVariant, .1f).rectTransform, 64f, 10f, 300f, 14f);
            TL(UiDraw.Text(insignia.transform, "Name", preview ? L("mastery.noneEquipped", "None equipped") : L("mastery.equipFromRack", "Badges are listed below"), UiFont.BodySemiBold, UiDp, OnSurface).rectTransform, 64f, 24f, 300f, 18f);
            TL(UiDraw.Text(insignia.transform, "Where", L("mastery.broadcastPlacements", "Shown on Home, match intro & profile"), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 64f, 44f, 310f, 14f);
            y += HeadH + 24f;

            float leftW = (w - 24f) * .655f, rightW = w - 24f - leftW;
            int[] relay = Thresholds(data.relayThresholds, 60, 300, 1200);
            int[] route = Thresholds(data.routeThresholds, 3, 12, 40);
            int[] weapon = Thresholds(data.weaponThresholds, 5, 20, 60);
            float ly = y;
            ly = TrackCard(page, Pad, ly, leftW, "01", L("mastery.track.relay.kicker", "Relay Time"), L("mastery.track.relay.title", "Relay Control"), "track_changes", Primary,
                L("mastery.track.relay.description", "Seconds spent holding the active Relay zone while no opponent contests it. Only verified matches count."), L("mastery.track.relay.valueLabel", "Seconds Held"),
                Find(data, "relay_specialist", ""), relay, L("unit.secondsShort", "s"), L("mastery.track.relay.bronze", "Relay Anchor I"), L("mastery.track.relay.silver", "Relay Core Keeper"), L("mastery.track.relay.gold", "Apex Relay Sentinel"),
                L("mastery.track.relay.rule", "Only verified time holding an uncontested Relay zone counts. Moving through or fighting over the zone does not count."), preview) + 20f;
            ly = TrackCard(page, Pad, ly, leftW, "02", L("mastery.track.route.kicker", "Teammate Pad Launches"), L("mastery.track.route.title", "Teammate Launches"), "alt_route", Tertiary,
                L("mastery.track.route.description", "Counts different human teammates who launch from your pad in verified matches."), L("mastery.track.route.valueLabel", "Teammate Launches"),
                Find(data, "route_builder", ""), route, L("unit.teammates", " Teammates"), L("mastery.track.route.bronze", "Route Scout"), L("mastery.track.route.silver", "Route Architect"), L("mastery.track.route.gold", "Sky Bridge Marshal"),
                L("mastery.track.route.rule", "A human teammate must use your pad and launch. Your own launches, repeat launches by the same teammate, and bots do not count."), preview) + 20f;
            int best = 0; string bestWeapon = "ARC";
            foreach (var wid in new[] { "ARC", "ZIP", "BRICK" })
            { int p = Find(data, "weapon_familiarity", wid); if (p > best) { best = p; bestWeapon = wid; } }
            ly = TrackCard(page, Pad, ly, leftW, "03", L("mastery.track.weapon.kicker", "Weapon Matches"), L("mastery.track.weapon.title", "Weapon Practice"), "my_location", SecondaryAccent,
                F("mastery.track.weapon.description", "A public match counts when {0} is equipped for most of the match. Wraps never change recoil or damage.", bestWeapon), L("mastery.track.weapon.valueLabel", "Matches"),
                best, weapon, L("unit.matches", " Matches"), L("mastery.track.weapon.bronze", "Marksman Cadre"), L("mastery.track.weapon.silver", "Ballistic Veteran"), L("mastery.track.weapon.gold", "Precision Machined Wrap"),
                L("mastery.track.weapon.rule", "Weapon wraps only change appearance. They do not change bullet spread, speed, fire rate, or hitboxes."), preview) + 20f;

            float ry = y;
            ry = TrophyRack(page, Pad + leftW + 24f, ry, rightW, data, relay, route, weapon, preview) + 20f;
            var pact = Card(page, "Pact", Low, Pad + leftW + 24f, ry, rightW, 170f, 12f);
            UiDraw.Place(UiDraw.Icon(pact, "Icon", "workspace_premium", 18f, Gold).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -26f), new Vector2(18f, 18f));
            TL(UiDraw.Text(pact, "Title", L("mastery.permanenceTitle", "Mastery Badges Never Expire"), UiFont.Heading, UiDp, Gold, .03f).rectTransform, 42f, 16f, rightW - 60f, 20f);
            var pactBody = UiDraw.Text(pact, "Body", L("mastery.permanenceBody", "Badges record skill in verified matches. They never expire and are not tied to seasons or store offers."), UiFont.Body, UiDp, OnSurface);
            pactBody.textWrappingMode = TextWrappingModes.Normal; pactBody.alignment = TextAlignmentOptions.TopLeft;
            TL(pactBody.rectTransform, 16f, 46f, rightW - 32f, 70f);
            TL(UiDraw.Icon(pact, "Check", "check", 14f, Primary).rectTransform, 16f, 136f, 14f, 14f);
            var pactNote = UiDraw.Text(pact, "Note", L("mastery.permanenceNote", "Cosmetics never change gameplay or collision."), UiFont.Body, UiDp, OnVariant);
            pactNote.textWrappingMode = TextWrappingModes.Normal;
            pactNote.alignment = TextAlignmentOptions.TopLeft;
            TL(pactNote.rectTransform, 36f, 134f, rightW - 52f, 24f);
            ry += 170f + 20f;
            return Mathf.Max(ly, ry);
        }

        static float TrackCard(RectTransform page, float x, float y, float w, string number, string kicker, string name, string icon, Color tone,
            string description, string valueLabel, int progress, int[] tiers, string unit, string bronzeName, string silverName, string goldName,
            string rule, bool preview)
        {
            var card = Card(page, "Track_" + number, Low, x, y, w, 330f, 12f);
            var iconPlate = UiDraw.Box(card, "IconPlate", new Color(tone.r, tone.g, tone.b, .15f), 8f);
            TL(iconPlate.rectTransform, 16f, 18f, 36f, 36f);
            UiDraw.Fill(UiDraw.Icon(iconPlate.transform, "Icon", icon, 20f, tone).rectTransform);
            TL(UiDraw.Text(card, "Kicker", F("mastery.track.kickerTemplate", "TRACK {0}  •  {1}", number, kicker), UiFont.MonoMedium, CaptionDp, OnSurface, .1f).rectTransform, 62f, 18f, 320f, 14f);
            TL(UiDraw.Text(card, "Title", name, UiFont.Heading, TitleDp, OnSurface, -.03f).rectTransform, 62f, 32f, 360f, 30f);
            var desc = UiDraw.Text(card, "Description", description, UiFont.Body, UiDp, OnVariant);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            // Labels are dp-sized, so on a dense phone this can take three lines; measure it and
            // move everything below down instead of letting it run into the progress scale.
            float descW = w * .6f;
            float descH = Mathf.Max(36f, desc.GetPreferredValues(desc.text, descW, 0f).y);
            TL(desc.rectTransform, 62f, 66f, descW, descH);
            float o = Mathf.Max(0f, 66f + descH + 8f - 112f);
            card.sizeDelta = new Vector2(card.sizeDelta.x, 330f + o);
            int gold = tiers[2];
            var value = UiDraw.Box(card, "Value", Mid, 8f);
            UiDraw.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(160f, 52f));
            UiDraw.Place(UiDraw.Text(value.transform, "Label", valueLabel, UiFont.MonoMedium, CaptionDp, OnVariant, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -14f), new Vector2(150f, 14f));
            UiDraw.Place(UiDraw.Text(value.transform, "Count", preview ? F("mastery.countPreview", "— / {0}{1}", gold.ToString("N0"), unit) : F("mastery.count", "<color=#{0}>{1}</color> / {2}{3}", ColorUtility.ToHtmlStringRGB(Primary), progress, gold.ToString("N0"), unit), UiFont.Mono, UiDp, OnSurface, .03f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -34f), new Vector2(150f, 20f));
            // Gradient bar and tick labels.
            float bw = w - 32f;
            var track = UiDraw.Box(card, "Track", Highest, 4f);
            TL(track.rectTransform, 16f, 112f + o, bw, 8f);
            var grad = UiDraw.HorizontalGradient(track.transform, "Fill", PrimaryC, Primary, Gold);
            UiDraw.Place(grad.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(bw * Mathf.Clamp01(preview ? 0f : progress / (float)gold), 8f));
            int pct = Mathf.RoundToInt(100f * Mathf.Clamp01(progress / (float)gold));
            var ticks = new[] { (F("mastery.tick.initial", "0{0} Start", unit.Trim().Substring(0, Math.Min(1, unit.Trim().Length)).ToLowerInvariant()), 0f, TextAlignmentOptions.MidlineLeft),
                (F("mastery.tick.bronze", "{0} ({1})", L("mastery.tier.bronze", "Bronze"), tiers[0]), .3f, TextAlignmentOptions.Midline), (F("mastery.tick.silver", "{0} ({1})", L("mastery.tier.silver", "Silver"), tiers[1]), .6f, TextAlignmentOptions.Midline),
                (preview ? F("mastery.tick.goldPreview", "{0} ({1}) [—]", L("mastery.tier.gold", "Gold"), gold.ToString("N0")) : F("mastery.tick.gold", "{0} ({1}) [{2}%]", L("mastery.tier.gold", "Gold"), gold.ToString("N0"), pct), 1f, TextAlignmentOptions.MidlineRight) };
            foreach (var (text, at, align) in ticks)
            {
                var t = UiDraw.Text(card, "Tick", text, UiFont.MonoMedium, CaptionDp, at >= 1f ? Gold : OnVariant, .1f, align);
                float tx = 16f + bw * at - (align == TextAlignmentOptions.MidlineRight ? 200f : align == TextAlignmentOptions.Midline ? 100f : 0f);
                TL(t.rectTransform, tx, 126f + o, 200f, 14f);
            }
            // Tier tiles.
            float tileW = (bw - 2f * 12f) / 3f;
            var names = new[] { bronzeName, silverName, goldName };
            var tones = new[] { Bronze, OnSurface, Gold };
            for (int i = 0; i < 3; i++)
            {
                bool earned = !preview && progress >= tiers[i];
                var tile = UiDraw.Box(card, "Tier_" + i, earned ? Mid : UIDesignTokens.WithAlpha(Mid, .6f), 8f, earned && i == 1 ? UIDesignTokens.WithAlpha(Primary, .3f) : (Color?)null);
                TL(tile.rectTransform, 16f + i * (tileW + 12f), 152f + o, tileW, 106f);
                UiDraw.Place(UiDraw.Icon(tile.transform, "Icon", "military_tech", 12f, tones[i]).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(10f, -16f), new Vector2(12f, 12f));
                TL(UiDraw.Text(tile.transform, "Tier", new[] { L("mastery.tier.bronzeFull", "Bronze Tier"), L("mastery.tier.silverFull", "Silver Tier"), L("mastery.tier.goldFull", "Gold Tier") }[i], UiFont.MonoMedium, CaptionDp, tones[i], .1f).rectTransform, 26f, 9f, tileW - 90f, 14f);
                var status = UiDraw.Text(tile.transform, "Need", earned ? F("mastery.tier.count", "{0} / {0}", tiers[i]) : preview ? L("state.locked", "LOCKED") : F("mastery.tier.countProgress", "{0} / {1}", progress, tiers[i]), UiFont.MonoMedium, CaptionDp, earned ? Primary : OnVariant, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(status.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -16f), new Vector2(90f, 14f));
                TL(UiDraw.Text(tile.transform, "Name", names[i], UiFont.BodySemiBold, UiDp, OnSurface).rectTransform, 10f, 30f, tileW - 20f, 22f);
                TL(UiDraw.Text(tile.transform, "Detail", F("mastery.awardedAt", "Awarded at {0}{1}.", tiers[i].ToString("N0"), unit), UiFont.MonoMedium, CaptionDp, OnVariant).rectTransform, 10f, 52f, tileW - 20f, 16f);
                var foot = UiDraw.Text(tile.transform, "State", earned ? L("state.unlockedUpper", "Unlocked") : preview ? L("state.progressionUnavailable", "Progress Unavailable") : F("mastery.remaining", "{0}{1} remaining", Mathf.Max(0, tiers[i] - progress), unit.ToLowerInvariant()), UiFont.MonoMedium, CaptionDp, earned ? Primary : OnVariant, .1f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(foot.rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-10f, 16f), new Vector2(tileW - 20f, 14f));
                if (!earned)
                {
                    var bar = UiDraw.Box(tile.transform, "Bar", Highest, 2f);
                    TL(bar.rectTransform, 10f, 74f, tileW - 20f, 4f);
                    var bf = UiDraw.Box(bar.transform, "Fill", Gold, 2f);
                    UiDraw.Place(bf.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((tileW - 20f) * Mathf.Clamp01(preview ? 0f : progress / (float)tiers[i]), 4f));
                }
            }
            // Strict rule.
            var ruleBox = UiDraw.Box(card, "Rule", Lowest, 8f);
            TL(ruleBox.rectTransform, 16f, 270f + o, bw, 44f);
            UiDraw.Place(UiDraw.Icon(ruleBox.transform, "Icon", "shield", 12f, OnVariant).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(12f, 12f));
            var ruleText = UiDraw.Text(ruleBox.transform, "Text", $"<color=#{ColorUtility.ToHtmlStringRGB(OnSurface)}><b>{L("mastery.strictRule", "Counts only:")}</b></color> " + rule, UiFont.Body, UiDp, OnVariant);
            ruleText.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(ruleText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(bw - 44f, 36f));
            return y + 330f + o;
        }

        static float TrophyRack(RectTransform page, float x, float y, float w, GoalsDto data, int[] relay, int[] route, int[] weapon, bool preview)
        {
            var entries = new (string nameKey, string name, string tierKey, string tier, string req, string icon, bool earned)[]
            {
                ("mastery.trophy.relayAnchor", "Relay Anchor", "mastery.tier.bronze", "Bronze", F("mastery.req.secondsHold", "Hold for {0}s", relay[0]), "anchor", !preview && Find(data, "relay_specialist", "") >= relay[0]),
                ("mastery.trophy.relayKeeper", "Relay Core Keeper", "mastery.tier.silver", "Silver", F("mastery.req.secondsHold", "{0}s Hold", relay[1]), "track_changes", !preview && Find(data, "relay_specialist", "") >= relay[1]),
                ("mastery.trophy.routeArchitect", "Route Architect", "mastery.tier.silver", "Silver", F("mastery.req.launches", "{0} Launches", route[1]), "alt_route", !preview && Find(data, "route_builder", "") >= route[1]),
                ("mastery.trophy.vectorMarksman", "Vector Marksman", "mastery.tier.silver", "Silver", F("mastery.req.matches", "{0} Matches", weapon[1]), "my_location", !preview && new[] { "ARC", "ZIP", "BRICK" }.Any(wd => Find(data, "weapon_familiarity", wd) >= weapon[1])),
                ("mastery.trophy.apexSentinel", "Apex Relay Sentinel", "mastery.tier.gold", "Gold", F("mastery.req.secondsRequired", "{0}s needed", relay[2].ToString("N0")), "lock", !preview && Find(data, "relay_specialist", "") >= relay[2]),
            };
            int unlocked = entries.Count(e => e.earned);
            float h = 108f + entries.Length * 62f + 16f;
            var card = Card(page, "TrophyRack", Low, x, y, w, h, 12f);
            TL(UiDraw.Text(card, "Kicker", L("mastery.trophyRack", "Badge Collection"), UiFont.Heading, CaptionDp, Primary, .08f).rectTransform, 16f, 18f, 200f, 14f);
            TL(UiDraw.Text(card, "Title", L("mastery.insignias", "Mastery Badges"), UiFont.BodySemiBold, UiDp, OnSurface).rectTransform, 16f, 36f, 220f, 22f);
            var count = UiDraw.Text(card, "Count", F("mastery.unlockedCount", "{0} / {1} Unlocked", unlocked, entries.Length), UiFont.MonoMedium, CaptionDp, OnVariant, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(count) + 14f;
            var countPlate = UiDraw.Box(card, "CountPlate", Mid, 4f);
            countPlate.transform.SetSiblingIndex(count.transform.GetSiblingIndex());
            UiDraw.Place(countPlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -40f), new Vector2(cw, 18f));
            UiDraw.Place(count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -40f), new Vector2(cw, 18f));
            var desc = UiDraw.Text(card, "Body", L("mastery.trophyBody", "Badges show skills from verified matches."), UiFont.Body, UiDp, OnVariant);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 16f, 66f, w - 32f, 32f);
            float ey = 108f;
            foreach (var e in entries)
            {
                var row = UiDraw.Box(card, "Badge_" + e.name, e.earned ? Mid : UIDesignTokens.WithAlpha(Mid, .5f), 8f);
                TL(row.rectTransform, 16f, ey, w - 32f, 54f);
                var plate = UiDraw.Box(row.transform, "IconPlate", e.earned ? Highest : UIDesignTokens.WithAlpha(Highest, .5f), 6f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(36f, 36f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", e.earned ? e.icon : "lock", 18f, e.earned ? OnSurface : Outline).rectTransform);
                TL(UiDraw.Text(row.transform, "Name", L(e.nameKey, e.name), UiFont.BodySemiBold, UiDp, e.earned ? OnSurface : Outline).rectTransform, 56f, 9f, w - 180f, 22f);
                TL(UiDraw.Text(row.transform, "Req", F("mastery.trophyReq", "{0}  ·  {1}", L(e.tierKey, e.tier), e.req), UiFont.MonoMedium, CaptionDp, Outline, .1f).rectTransform, 56f, 29f, w - 180f, 14f);
                var state = UiDraw.Text(row.transform, "State", e.earned ? L("state.earned", "Earned") : L("state.locked", "LOCKED"), UiFont.MonoMedium, CaptionDp, e.earned ? Primary : OnVariant, .1f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(state.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(80f, 14f));
                ey += 62f;
            }
            return y + h;
        }

        // ------------------------------------------------------------ footer

        static void Footer(RectTransform root, bool mastery, bool preview, Action back, Action other,
            Action retry = null, bool dataUnavailable = false)
        {
            var bar = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(Low, .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 88f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .5f), 0f);
            var backButton = UiDraw.Tap(bar, "Back", High, 12f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var backText = UiDraw.Text(backButton.transform, "Label", mastery ? L("footer.returnHangar", "Back") : L("footer.returnLobby", "Back"), UiFont.HeadingSemiBold, UiDp, OnSurface, .01f);
            float bw = 24f + 18f + 10f + UiDraw.Width(backText) + 24f;
            UiDraw.Place((RectTransform)backButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 52f));
            UiDraw.Place(UiDraw.Icon(backButton.transform, "Icon", "arrow_back", 18f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 22f));
            var integrity = UiDraw.Text(bar, "Integrity", dataUnavailable ? L("footer.serverUnavailable", "Match Progress Unavailable")
                : preview ? L("footer.previewPipeline", "Match progress is not available yet") : L("footer.verifiedRecord", "VERIFIED MATCH RESULTS  •  COSMETIC REWARDS ONLY"),
                UiFont.MonoMedium, CaptionDp, OnVariant, .1f, TextAlignmentOptions.Midline);
            float statusWidth = Mathf.Clamp(root.rect.width - bw - Pad * 2f - 180f, 120f, 520f);
            integrity.overflowMode = TextOverflowModes.Ellipsis;
            UiDraw.Place(integrity.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(statusWidth, 16f));
            if (retry != null)
            {
                var retryButton = UiDraw.Tap(bar, "Retry", High, 12f, () => { GameAudio.Play2D(GameCue.UiTap); retry(); });
                var retryText = UiDraw.Text(retryButton.transform, "Label", L("footer.retry", "Retry"), UiFont.HeadingSemiBold, UiDp, Primary, .01f);
                float rw = 24f + 18f + 10f + UiDraw.Width(retryText) + 24f;
                UiDraw.Place((RectTransform)retryButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(rw, 52f));
                UiDraw.Place(UiDraw.Icon(retryButton.transform, "Icon", "refresh", 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
                UiDraw.Place(retryText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(retryText) + 2f, 22f));
            }
            else
            {
                var otherButton = UiDraw.Tap(bar, "Other", High, 12f, () => { GameAudio.Play2D(GameCue.UiTap); other?.Invoke(); });
                var otherText = UiDraw.Text(otherButton.transform, "Label", mastery ? L("footer.contracts", "View Contracts") : L("footer.mastery", "View Mastery"), UiFont.HeadingSemiBold, UiDp, Primary, .01f);
                float ow = 24f + 18f + 10f + UiDraw.Width(otherText) + 24f;
                UiDraw.Place((RectTransform)otherButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(ow, 52f));
                UiDraw.Place(UiDraw.Icon(otherButton.transform, "Icon", mastery ? "description" : "terminal", 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
                UiDraw.Place(otherText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(otherText) + 2f, 22f));
            }
        }

        // ------------------------------------------------------------ helpers and copy

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h, float radius)
        {
            var box = UiDraw.Box(parent, name, fill, radius);
            TL(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

        static int[] Thresholds(int[] supplied, int bronze, int silver, int gold) =>
            supplied != null && supplied.Length >= 3 ? supplied : new[] { bronze, silver, gold };

        static int Pct(int progress, int target) => target > 0 ? Mathf.RoundToInt(100f * Mathf.Clamp01(progress / (float)target)) : 0;

        static int DefaultTarget(string id) => id switch { "finish_together" => 2, "hold_the_relay" => 45, "build_a_team_route" => 2, _ => 1 };

        static string Unit(string id) => id switch { "finish_together" => L("unit.matches", "Matches"), "hold_the_relay" => L("unit.seconds", "sec"), "build_a_team_route" => L("unit.teammates", "Teammates"), _ => "" };

        static int Find(GoalsDto data, string track, string weapon) => (data.mastery ?? Array.Empty<MasteryProgressDto>())
            .FirstOrDefault(x => x != null && x.trackId == track && x.weaponId == weapon)?.progress ?? 0;

        static string Name(string id) => id switch { "finish_together" => L("contract.finishTogether.name", "Finish Together"),
            "hold_the_relay" => L("contract.holdRelay.name", "Hold the Relay"), "build_a_team_route" => L("contract.buildRoute.name", "Build a Team Route"), _ => id ?? L("contract.genericName", "Contract") };
        static string Metric(string id) => id switch { "finish_together" => L("contract.finishTogether.metric", "Matches completed"),
            "hold_the_relay" => L("contract.holdRelay.metric", "Relay time"), "build_a_team_route" => L("contract.buildRoute.metric", "Teammate launches"), _ => L("contract.genericMetric", "Progress") };
        static string Description(string id) => id switch {
            "finish_together" => L("contract.finishTogether.description", "Complete eligible public matches. Solo queue and parties count."),
            "hold_the_relay" => L("contract.holdRelay.description", "Hold the active Relay zone while it is not contested."),
            "build_a_team_route" => L("contract.buildRoute.description", "Get two different human teammates to launch from your pad in eligible matches."),
            _ => L("contract.genericDescription", "Advance this contract in eligible public matches.") };
        static string Condition(string id) => id switch {
            "finish_together" => L("contract.finishTogether.condition", "Public matches count whether you queue alone or with a party. Overtime also counts."),
            "hold_the_relay" => L("contract.holdRelay.condition", "Contested zones and airborne passes do not count. You must stand inside the active Relay zone."),
            "build_a_team_route" => L("contract.buildRoute.condition", "Human teammates must launch from your jump pad. Your own launches and bots do not count."),
            _ => L("contract.genericCondition", "Verified eligible match results only.") };
    }
}
