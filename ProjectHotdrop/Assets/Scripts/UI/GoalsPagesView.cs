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
        static readonly Color Lowest = UiDraw.Hex("#060e1d");
        static readonly Color Low = UiDraw.Hex("#141c2b");
        static readonly Color Mid = UiDraw.Hex("#18202f");
        static readonly Color High = UiDraw.Hex("#222a3a");
        static readonly Color Highest = UiDraw.Hex("#2d3545");
        static readonly Color Primary = UiDraw.Hex("#7cefff");
        static readonly Color PrimaryC = UiDraw.Hex("#30d5e8");
        static readonly Color Tertiary = UiDraw.Hex("#ffda88");
        static readonly Color Gold = UiDraw.Hex("#ffd166");
        static readonly Color Bronze = UiDraw.Hex("#d39b6a");
        static readonly Color OnSurface = UiDraw.Hex("#dbe2f8");
        static readonly Color OnVariant = UiDraw.Hex("#bbc9cb");
        static readonly Color Outline = UiDraw.Hex("#859396");
        static readonly Color OnPrimary = UiDraw.Hex("#00363c");

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
            UiDraw.Fill(UiDraw.Box(root, "Base", UiDraw.Hex("#0b1322")).rectTransform);
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
            var kicker = UiDraw.Text(head, "Kicker", "PROTOCOL // SPEC-09", UiFont.Heading, 12f, Primary, .08f);
            TL(kicker.rectTransform, 86f, 24f, UiDraw.Width(kicker) + 2f, 16f);
            TL(UiDraw.Text(head, "Net", "•  AIR-GROUND RELAY NET", UiFont.MonoMedium, 11f, OnVariant, .1f).rectTransform, 86f + UiDraw.Width(kicker) + 8f, 24f, 220f, 16f);
            var title = UiDraw.Text(head, "Title", "CONTRACTS // ROTATING OBJECTIVE\nDOCTRINE", UiFont.Heading, 28f, OnSurface, -.03f);
            title.lineSpacing = -14f;
            title.alignment = TextAlignmentOptions.TopLeft;
            TL(title.rectTransform, 86f, 44f, 520f, 80f);
            float rx = w * .5f + 40f;
            var seals = UiDraw.Box(head, "Seals", UiDraw.Hex("#222a3a", .8f), 8f);
            TL(seals.rectTransform, rx, 18f, 390f, 52f);
            var sealIcon = UiDraw.Dot(seals.transform, "Icon", 32f, UiDraw.Hex("#ffd166", .2f));
            UiDraw.Place(sealIcon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(32f, 32f));
            UiDraw.Fill(UiDraw.Icon(sealIcon.transform, "Glyph", "military_tech", 18f, Gold).rectTransform);
            var count = UiDraw.Text(seals.transform, "Count", preview ? "—" : data.seals.ToString(), UiFont.Mono, 16f, Gold);
            TL(count.rectTransform, 64f, 8f, UiDraw.Width(count) + 2f, 20f);
            TL(UiDraw.Text(seals.transform, "Label", "FLIGHT SEALS", UiFont.Heading, 12f, OnSurface, .08f).rectTransform, 64f + UiDraw.Width(count) + 8f, 10f, 160f, 16f);
            TL(UiDraw.Text(seals.transform, "Sub", "Earned Progression  •  Never Purchasable", UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 64f, 28f, 310f, 16f);
            int matches = Mathf.Min(2, data.eligiblePublicMatches);
            var lockPill = UiDraw.Box(head, "Unlock", UiDraw.Hex("#222a3a", .8f), 8f);
            var lockText = UiDraw.Text(lockPill.transform, "Text", preview ? "Verified progression unavailable in this build"
                : $"Matches {matches}/2 Complete ({(data.unlocked ? "Unlocked" : "Locked")})", UiFont.MonoMedium, 11f, OnVariant, .05f);
            float lw = 16f + 14f + 8f + UiDraw.Width(lockText) + 16f;
            TL(lockPill.rectTransform, rx, 80f, lw, 32f);
            UiDraw.Place(UiDraw.Icon(lockPill.transform, "Icon", data.unlocked && !preview ? "lock_open" : "lock", 14f, OnVariant).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(lockText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(38f, 0f), new Vector2(UiDraw.Width(lockText) + 2f, 14f));
            y += 136f + 24f;

            // Rules strip.
            var rules = Card(page, "Rules", Low, Pad, y, w, 46f, 12f);
            UiDraw.Place(UiDraw.Icon(rules, "Info", "info", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(16f, 16f));
            var rulesText = UiDraw.Text(rules, "Text", preview
                    ? "Contracts advance only from verified public matches. That pipeline is not enabled in this build, so no progress, seals or claims are shown."
                    : data.unlocked
                        ? "Select one contract to advance at any time. Progress never expires, resets, or degrades on switch. Switching pauses progression until reactivated."
                        : $"Complete {Mathf.Max(0, 2 - data.eligiblePublicMatches)} more eligible public match(es) to unlock Contracts.",
                UiFont.Body, 12f, OnVariant, .01f);
            rulesText.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(rulesText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(w - 76f, 34f));
            y += 46f + 28f;

            // Section header.
            TL(UiDraw.Dot(page, "SectionDot", 8f, Primary).rectTransform, Pad, y + 8f, 8f, 8f);
            TL(UiDraw.Text(page, "Section", "DIRECTIVES & FIELD OBJECTIVES", UiFont.Heading, 18f, OnSurface, .01f).rectTransform, Pad + 16f, y, 400f, 24f);
            var templates = data.templates ?? Array.Empty<GoalTemplateDto>();
            var instances = data.goals ?? Array.Empty<GoalInstanceDto>();
            int engaged = instances.Count(g => g != null && g.status == "active");
            var activeLabel = UiDraw.Text(page, "Active", preview ? "ACTIVE PROTOCOL: UNAVAILABLE"
                : $"ACTIVE PROTOCOL: [{engaged} OF 3 ENGAGED]", UiFont.MonoMedium, 11f, OnSurface, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(activeLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 12f), new Vector2(320f, 16f));
            y += 24f + 20f;

            // Three directive cards.
            string[] ids = { "finish_together", "hold_the_relay", "build_a_team_route" };
            float cw = (w - 2f * 24f) / 3f, cardH = 410f;
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
            TL(UiDraw.Text(page, "ExchangeTitle", "FLIGHT SEAL EXCHANGE", UiFont.Heading, 20f, OnSurface, .01f).rectTransform, Pad + 32f, y, 400f, 26f);
            TL(UiDraw.Text(page, "ExchangeSubtitle", "Static open requisition catalog  •  Earned cosmetic choices, never purchasable", UiFont.Body, 12f, OnVariant).rectTransform, Pad, y + 28f, 600f, 16f);
            var policy = UiDraw.Box(page, "ExchangePolicy", Low, 8f);
            var policyText = UiDraw.Text(policy.transform, "Text", "No Timers  •  No Rotations  •  No Duplicates", UiFont.MonoMedium, 11f, OnSurface, .1f);
            float pw = 16f + 16f + 8f + UiDraw.Width(policyText) + 16f;
            UiDraw.Place(policy.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 16f), new Vector2(pw, 32f));
            UiDraw.Place(UiDraw.Icon(policy.transform, "Icon", "verified", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(policyText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(UiDraw.Width(policyText) + 2f, 14f));
            y += 60f;
            var exchange = Card(page, "Exchange", Low, Pad, y, w, 110f, 12f);
            bool catalog = data.rewardCatalogComplete && !preview;
            UiDraw.Place(UiDraw.Icon(exchange, "Icon", catalog ? "checkroom" : "inventory_2", 32f, catalog ? Primary : Outline).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(28f, 0f), new Vector2(32f, 32f));
            TL(UiDraw.Text(exchange, "Catalog", catalog ? "COSMETIC CATALOG AVAILABLE" : "COSMETIC EXCHANGE UNAVAILABLE", UiFont.Heading, 16f, catalog ? Primary : OnSurface, .05f).rectTransform, 80f, 28f, w - 120f, 22f);
            var catalogBody = UiDraw.Text(exchange, "Body", catalog ? "Open Appearance to review owned items and equip earned cosmetics."
                : "A complete licensed reward catalog is required before seals can be exchanged. Nothing is purchasable, now or later.",
                UiFont.Body, 12f, OnVariant);
            catalogBody.textWrappingMode = TextWrappingModes.Normal;
            TL(catalogBody.rectTransform, 80f, 54f, w - 120f, 34f);
            y += 110f + 24f;

            // Integrity standard.
            var integrity = Card(page, "Integrity", Lowest, Pad, y, w, 82f, 12f);
            var scale = UiDraw.Box(integrity, "IconPlate", High, 8f);
            UiDraw.Place(scale.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(44f, 44f));
            UiDraw.Fill(UiDraw.Icon(scale.transform, "Icon", "balance", 22f, Primary).rectTransform);
            TL(UiDraw.Text(integrity, "Title", "COMPETITIVE INTEGRITY STANDARD", UiFont.Heading, 12f, OnSurface, .08f).rectTransform, 84f, 18f, w - 110f, 16f);
            var integrityBody = UiDraw.Text(integrity, "Body", "Cosmetics are strictly visual. They do not alter weapon recoil, projectile speed, hitboxes, jump pad thrust, or mobility. Flight Seals can only be earned through match objectives.",
                UiFont.Body, 12f, OnVariant);
            integrityBody.textWrappingMode = TextWrappingModes.Normal;
            TL(integrityBody.rectTransform, 84f, 36f, w - 110f, 34f);
            return y + 82f;
        }

        static void Directive(RectTransform page, float x, float y, float w, float h, int index, string id, int progress, int target,
            bool active, bool done, bool paused, bool ready, bool actionable, bool preview, Action<string> select)
        {
            var card = Card(page, "Directive_" + index, active ? UiDraw.Hex("#18202f") : Low, x, y, w, h, 12f);
            if (active)
            {
                UiDraw.Glow(card, UiDraw.Hex("#30d5e8", .2f), 12f, 16f);
                var tabText = UiDraw.Text(page, "CurrentTab", ready ? "CURRENT TARGET // READY" : "CURRENT TARGET // ENGAGED", UiFont.Heading, 12f, OnPrimary, .08f, TextAlignmentOptions.Midline);
                float tw = UiDraw.Width(tabText) + 24f;
                var tab = UiDraw.Box(page, "CurrentTabPlate", PrimaryC, 11f);
                tab.transform.SetSiblingIndex(tabText.transform.GetSiblingIndex());
                TL(tab.rectTransform, x + 20f, y - 11f, tw, 22f);
                TL(tabText.rectTransform, x + 20f, y - 11f, tw, 22f);
            }
            float inner = w - 44f;
            TL(UiDraw.Text(card, "Number", $"DIRECTIVE // {index + 1:00}{(active ? " [ENGAGED]" : "")}", UiFont.MonoMedium, 11f, active ? Primary : Outline, .1f).rectTransform, 22f, 26f, inner - 120f, 16f);
            string state = preview ? "LOCKED" : done ? "COMPLETE" : ready ? "100% READY" : active ? $"ENGAGED ({Pct(progress, target)}%)" : paused ? $"PAUSED ({Pct(progress, target)}%)" : $"STANDBY ({Pct(progress, target)}%)";
            var stateText = UiDraw.Text(card, "State", state, UiFont.MonoMedium, 11f, ready ? Gold : OnVariant, .1f, TextAlignmentOptions.Midline);
            float sw = UiDraw.Width(stateText) + 18f;
            var statePlate = UiDraw.Box(card, "StatePlate", ready ? UiDraw.Hex("#ffd166", .15f) : High, 4f);
            statePlate.transform.SetSiblingIndex(stateText.transform.GetSiblingIndex());
            UiDraw.Place(statePlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -34f), new Vector2(sw, 22f));
            UiDraw.Place(stateText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -34f), new Vector2(sw, 22f));
            TL(UiDraw.Text(card, "Name", Name(id).ToUpperInvariant(), UiFont.Heading, 18f, OnSurface, -.01f).rectTransform, 22f, 44f, inner, 26f);
            var desc = UiDraw.Text(card, "Description", Description(id), UiFont.Body, 14f, OnSurface);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 22f, 84f, inner, 44f);
            // Progress box.
            var meter = UiDraw.Box(card, "Progress", Lowest, 8f);
            TL(meter.rectTransform, 22f, 136f, inner, 60f);
            TL(UiDraw.Text(meter.transform, "Caption", Metric(id).ToUpperInvariant(), UiFont.Heading, 12f, active ? Primary : OnSurface, .08f).rectTransform, 14f, 14f, inner - 150f, 16f);
            var countText = UiDraw.Text(meter.transform, "Count", preview ? $"— / {target} {Unit(id)}"
                : $"{progress} / {target} {Unit(id)}", UiFont.Mono, 14f, active ? Primary : OnSurface, .03f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(countText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -22f), new Vector2(170f, 18f));
            var track = UiDraw.Box(meter.transform, "Track", Highest, 3f);
            TL(track.rectTransform, 14f, 40f, inner - 28f, 6f);
            var fill = UiDraw.Box(track.transform, "Fill", active ? PrimaryC : Outline, 3f);
            UiDraw.Place(fill.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((inner - 28f) * Mathf.Clamp01(target > 0 ? progress / (float)target : 0f), 6f));
            // Condition box.
            var cond = UiDraw.Box(card, "Condition", Lowest, 8f);
            TL(cond.rectTransform, 22f, 208f, inner, 76f);
            var condText = UiDraw.Text(cond.transform, "Text", "<color=#dbe2f8><b>Condition:</b></color> " + Condition(id).Replace("Condition: ", ""), UiFont.Body, 12f, OnVariant);
            condText.textWrappingMode = TextWrappingModes.Normal; condText.alignment = TextAlignmentOptions.TopLeft;
            TL(condText.rectTransform, 12f, 12f, inner - 24f, 52f);
            // Yield row.
            TL(UiDraw.Text(card, "YieldLabel", ready ? "GUARANTEED TOKEN:" : "YIELD:", UiFont.MonoMedium, 11f, ready ? Primary : Outline, .1f).rectTransform, 22f, h - 104f, 160f, 16f);
            var yieldText = UiDraw.Text(card, "Yield", "+1 Flight Seal", UiFont.Mono, 14f, Gold, .03f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(yieldText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -h + 96f), new Vector2(140f, 18f));
            UiDraw.Place(UiDraw.Icon(card, "YieldIcon", "military_tech", 16f, Gold).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f - UiDraw.Width(yieldText) - 6f, -h + 96f), new Vector2(16f, 16f));
            // Action.
            string caption = preview ? "Progression Unavailable" : done ? "Completed" : ready ? "Seal Awarded On Verification" : active ? "Current Directive" : paused ? "Resume Directive" : "Switch to Directive";
            bool can = actionable && !active && !done;
            var button = UiDraw.Tap(card, "Select", ready ? Gold : High, 8f, can ? (Action)(() => select?.Invoke(id)) : null);
            button.interactable = can;
            UiDraw.Place((RectTransform)button.transform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 22f), new Vector2(inner, 52f));
            var label = UiDraw.Text(button.transform, "Label", caption, UiFont.BodySemiBold, 16f, ready ? UiDraw.Hex("#3f2e00") : can ? OnSurface : Outline, .01f);
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
            var chip1 = UiDraw.Text(head, "Immutable", "IMMUTABLE RECORD", UiFont.MonoMedium, 11f, Primary, .1f);
            float c1 = UiDraw.Width(chip1);
            var chip1Plate = UiDraw.Box(head, "ImmutablePlate", UiDraw.Hex("#30d5e8", .1f), 4f, UiDraw.Hex("#30d5e8", .4f));
            chip1Plate.transform.SetSiblingIndex(chip1.transform.GetSiblingIndex());
            TL(chip1Plate.rectTransform, hx, 24f, c1 + 34f, 18f);
            UiDraw.Place(UiDraw.Icon(chip1Plate.transform, "Icon", "verified", 12f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(12f, 12f));
            TL(chip1.rectTransform, hx + 26f, 25f, c1 + 2f, 16f);
            hx += c1 + 34f + 8f;
            var mid = UiDraw.Text(head, "Archive", "// ARCHIVAL CALIBRATION LOG", UiFont.MonoMedium, 11f, Outline, .1f);
            TL(mid.rectTransform, hx, 25f, UiDraw.Width(mid) + 2f, 16f);
            hx += UiDraw.Width(mid) + 8f;
            var chip2 = UiDraw.Text(head, "Wipes", "ZERO SEASON WIPES", UiFont.MonoMedium, 11f, Gold, .1f, TextAlignmentOptions.Midline);
            float c2 = UiDraw.Width(chip2) + 12f;
            var chip2Plate = UiDraw.Box(head, "WipesPlate", UiDraw.Hex("#ffd166", .15f), 4f);
            chip2Plate.transform.SetSiblingIndex(chip2.transform.GetSiblingIndex());
            TL(chip2Plate.rectTransform, hx, 24f, c2, 18f);
            TL(chip2.rectTransform, hx, 24f, c2, 18f);
            // h1 + inline subtitle span, wrapping inside the left column exactly like the HTML flow.
            var title = UiDraw.Text(head, "Title", "FLIGHT-LOG MASTERY <size=20><color=#7cefff><cspace=1>// PERMANENT OPERATIVE RECORD</cspace></color></size>",
                UiFont.Heading, 40f, OnSurface, -.02f, TextAlignmentOptions.BottomLeft);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = 8f;
            TL(title.rectTransform, 24f, 44f, w * .58f, 92f);
            var body = UiDraw.Text(head, "Body", "Permanent milestone tracking. Achievements reflect verified competitive execution. Cosmetics never change weapon power, trajectory physics, or movement speeds.",
                UiFont.Body, 14f, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            body.lineSpacing = 4f;
            TL(body.rectTransform, 24f, 138f, w * .58f, 44f);
            var insignia = UiDraw.Box(head, "Insignia", UiDraw.Hex("#222a3a", .8f), 12f);
            UiDraw.Place(insignia.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, 0f), new Vector2(428f, 72f));
            var iconBox = UiDraw.Box(insignia.transform, "IconPlate", Highest, 8f);
            UiDraw.Place(iconBox.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(iconBox.transform, "Icon", "alt_route", 22f, Primary).rectTransform);
            TL(UiDraw.Text(insignia.transform, "Kicker", "ACTIVE BROADCAST INSIGNIA", UiFont.MonoMedium, 10f, OnVariant, .1f).rectTransform, 64f, 10f, 300f, 14f);
            TL(UiDraw.Text(insignia.transform, "Name", preview ? "None equipped" : "Equip from the Trophy Rack", UiFont.BodySemiBold, 14f, OnSurface).rectTransform, 64f, 24f, 300f, 18f);
            TL(UiDraw.Text(insignia.transform, "Where", "Live across Home HUD, Match Intro & Flight Card", UiFont.MonoMedium, 10f, OnVariant, .05f).rectTransform, 64f, 44f, 310f, 14f);
            y += HeadH + 24f;

            float leftW = (w - 24f) * .655f, rightW = w - 24f - leftW;
            int[] relay = Thresholds(data.relayThresholds, 60, 300, 1200);
            int[] route = Thresholds(data.routeThresholds, 3, 12, 40);
            int[] weapon = Thresholds(data.weaponThresholds, 5, 20, 60);
            float ly = y;
            ly = TrackCard(page, Pad, ly, leftW, "01", "OBJECTIVE INTEGRITY", "RELAY SPECIALIST", "track_changes", Primary,
                "Cumulative valid uncontested seconds holding active Relay nodes under tournament rules.", "ACTIVE ACCUMULATION",
                Find(data, "relay_specialist", ""), relay, "s", "Relay Anchor I", "Relay Core Keeper", "Apex Relay Sentinel",
                "Only authoritative uncontested active Relay hold time advances this record. Flying through or contested deadlocks do not tick seconds.", preview) + 20f;
            ly = TrackCard(page, Pad, ly, leftW, "02", "TRAVERSAL LOGISTICS", "ROUTE BUILDER", "alt_route", Tertiary,
                "Distinct qualifying human teammate launches deployed through your kinetic jump vectors.", "LAUNCHES CONFIRMED",
                Find(data, "route_builder", ""), route, " Teammates", "Route Scout", "Route Architect", "Sky Bridge Marshal",
                "Verified human teammates who step on your armed pad and launch. Self-launches, repeated bounces and bots do not advance.", preview) + 20f;
            int best = 0; string bestWeapon = "ARC";
            foreach (var wid in new[] { "ARC", "ZIP", "BRICK" })
            { int p = Find(data, "weapon_familiarity", wid); if (p > best) { best = p; bestWeapon = wid; } }
            ly = TrackCard(page, Pad, ly, leftW, "03", "BALLISTIC PROFICIENCY", "WEAPON FAMILIARITY", "my_location", UiDraw.Hex("#ffb2b7"),
                $"Verified public matches with {bestWeapon} equipped for the majority of deployment time. Cosmetics provide zero recoil or damage shifts.", "MATCH QUOTA",
                best, weapon, " Matches", "Marksman Cadre", "Ballistic Veteran", "Precision Machined Wrap",
                "Competitive Integrity Guarantee: earned wraps are visually distinct but keep identical bloom, velocity, cadence and hitboxes.", preview) + 20f;

            float ry = y;
            ry = TrophyRack(page, Pad + leftW + 24f, ry, rightW, data, relay, route, weapon, preview) + 20f;
            var pact = Card(page, "Pact", Low, Pad + leftW + 24f, ry, rightW, 170f, 12f);
            UiDraw.Place(UiDraw.Icon(pact, "Icon", "workspace_premium", 18f, Gold).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -26f), new Vector2(18f, 18f));
            TL(UiDraw.Text(pact, "Title", "THE PERMANENCE PACT", UiFont.Heading, 14f, Gold, .03f).rectTransform, 42f, 16f, rightW - 60f, 20f);
            var pactBody = UiDraw.Text(pact, "Body", "HOTDROP mastery badges are non-expiring records of skill. They are completely separate from seasonal passes or store promotions.", UiFont.Body, 12f, OnSurface);
            pactBody.textWrappingMode = TextWrappingModes.Normal; pactBody.alignment = TextAlignmentOptions.TopLeft;
            TL(pactBody.rectTransform, 16f, 46f, rightW - 32f, 70f);
            TL(UiDraw.Icon(pact, "Check", "check", 14f, Primary).rectTransform, 16f, 136f, 14f, 14f);
            TL(UiDraw.Text(pact, "Note", "Cosmetics carry zero gameplay or collision advantages.", UiFont.Body, 11f, OnVariant).rectTransform, 36f, 134f, rightW - 52f, 18f);
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
            TL(UiDraw.Text(card, "Kicker", $"TRACK {number}  •  {kicker}", UiFont.MonoMedium, 10f, OnSurface, .1f).rectTransform, 62f, 18f, 320f, 14f);
            TL(UiDraw.Text(card, "Title", name, UiFont.Heading, 24f, OnSurface, -.03f).rectTransform, 62f, 32f, 360f, 30f);
            var desc = UiDraw.Text(card, "Description", description, UiFont.Body, 12f, OnVariant);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 62f, 66f, w * .6f, 36f);
            int gold = tiers[2];
            var value = UiDraw.Box(card, "Value", Mid, 8f);
            UiDraw.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(160f, 52f));
            UiDraw.Place(UiDraw.Text(value.transform, "Label", valueLabel, UiFont.MonoMedium, 10f, OnVariant, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -14f), new Vector2(150f, 14f));
            UiDraw.Place(UiDraw.Text(value.transform, "Count", preview ? "— / " + gold.ToString("N0") + unit : $"<color=#7cefff><size=16>{progress}</size></color> / {gold:N0}{unit}", UiFont.Mono, 12f, OnSurface, .03f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -34f), new Vector2(150f, 20f));
            // Gradient bar and tick labels.
            float bw = w - 32f;
            var track = UiDraw.Box(card, "Track", Highest, 4f);
            TL(track.rectTransform, 16f, 112f, bw, 8f);
            var grad = UiDraw.HorizontalGradient(track.transform, "Fill", PrimaryC, UiDraw.Hex("#7cefff"), Gold);
            UiDraw.Place(grad.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(bw * Mathf.Clamp01(preview ? 0f : progress / (float)gold), 8f));
            int pct = Mathf.RoundToInt(100f * Mathf.Clamp01(progress / (float)gold));
            var ticks = new[] { ($"0{unit.Trim().Substring(0, Math.Min(1, unit.Trim().Length)).ToLowerInvariant()} INIT", 0f, TextAlignmentOptions.MidlineLeft),
                ($"BRONZE ({tiers[0]})", .3f, TextAlignmentOptions.Midline), ($"SILVER ({tiers[1]})", .6f, TextAlignmentOptions.Midline),
                (preview ? $"GOLD ({gold:N0}) [—]" : $"GOLD ({gold:N0}) [{pct}%]", 1f, TextAlignmentOptions.MidlineRight) };
            foreach (var (text, at, align) in ticks)
            {
                var t = UiDraw.Text(card, "Tick", text, UiFont.MonoMedium, 10f, at >= 1f ? Gold : OnVariant, .1f, align);
                float tx = 16f + bw * at - (align == TextAlignmentOptions.MidlineRight ? 200f : align == TextAlignmentOptions.Midline ? 100f : 0f);
                TL(t.rectTransform, tx, 126f, 200f, 14f);
            }
            // Tier tiles.
            float tileW = (bw - 2f * 12f) / 3f;
            var names = new[] { bronzeName, silverName, goldName };
            var tones = new[] { Bronze, OnSurface, Gold };
            for (int i = 0; i < 3; i++)
            {
                bool earned = !preview && progress >= tiers[i];
                var tile = UiDraw.Box(card, "Tier_" + i, earned ? Mid : UiDraw.Hex("#18202f", .6f), 8f, earned && i == 1 ? UiDraw.Hex("#7cefff", .3f) : (Color?)null);
                TL(tile.rectTransform, 16f + i * (tileW + 12f), 152f, tileW, 106f);
                UiDraw.Place(UiDraw.Icon(tile.transform, "Icon", "military_tech", 12f, tones[i]).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(10f, -16f), new Vector2(12f, 12f));
                TL(UiDraw.Text(tile.transform, "Tier", new[] { "BRONZE TIER", "SILVER TIER", "GOLD TIER" }[i], UiFont.MonoMedium, 10f, tones[i], .1f).rectTransform, 26f, 9f, tileW - 90f, 14f);
                var status = UiDraw.Text(tile.transform, "Need", earned ? $"{tiers[i]} / {tiers[i]}" : preview ? "LOCKED" : $"{progress} / {tiers[i]}", UiFont.MonoMedium, 10f, earned ? Primary : OnVariant, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(status.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-10f, -16f), new Vector2(90f, 14f));
                TL(UiDraw.Text(tile.transform, "Name", names[i], UiFont.BodySemiBold, 14f, OnSurface).rectTransform, 10f, 30f, tileW - 20f, 20f);
                TL(UiDraw.Text(tile.transform, "Detail", $"Awarded at {tiers[i]:N0}{unit}.", UiFont.Body, 11f, OnVariant).rectTransform, 10f, 52f, tileW - 20f, 16f);
                var foot = UiDraw.Text(tile.transform, "State", earned ? "UNLOCKED" : preview ? "PROGRESSION UNAVAILABLE" : $"{Mathf.Max(0, tiers[i] - progress)}{unit.ToLowerInvariant()} remaining", UiFont.MonoMedium, 10f, earned ? Primary : OnVariant, .1f, TextAlignmentOptions.MidlineRight);
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
            TL(ruleBox.rectTransform, 16f, 270f, bw, 44f);
            UiDraw.Place(UiDraw.Icon(ruleBox.transform, "Icon", "shield", 12f, OnVariant).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(12f, 12f));
            var ruleText = UiDraw.Text(ruleBox.transform, "Text", "<color=#dbe2f8><b>Strict Rule:</b></color> " + rule, UiFont.Body, 11f, OnVariant);
            ruleText.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(ruleText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(bw - 44f, 36f));
            return y + 330f;
        }

        static float TrophyRack(RectTransform page, float x, float y, float w, GoalsDto data, int[] relay, int[] route, int[] weapon, bool preview)
        {
            var entries = new (string name, string tier, string req, string icon, bool earned)[]
            {
                ("Relay Anchor", "Bronze", $"{relay[0]}s Hold", "anchor", !preview && Find(data, "relay_specialist", "") >= relay[0]),
                ("Relay Core Keeper", "Silver", $"{relay[1]}s Hold", "track_changes", !preview && Find(data, "relay_specialist", "") >= relay[1]),
                ("Route Architect", "Silver", $"{route[1]} Launches", "alt_route", !preview && Find(data, "route_builder", "") >= route[1]),
                ("Vector Marksman", "Silver", $"{weapon[1]} Matches", "my_location", !preview && new[] { "ARC", "ZIP", "BRICK" }.Any(wd => Find(data, "weapon_familiarity", wd) >= weapon[1])),
                ("Apex Relay Sentinel", "Gold", $"{relay[2]:N0}s Required", "lock", !preview && Find(data, "relay_specialist", "") >= relay[2]),
            };
            int unlocked = entries.Count(e => e.earned);
            float h = 108f + entries.Length * 62f + 16f;
            var card = Card(page, "TrophyRack", Low, x, y, w, h, 12f);
            TL(UiDraw.Text(card, "Kicker", "TROPHY RACK", UiFont.Heading, 11f, Primary, .08f).rectTransform, 16f, 18f, 200f, 14f);
            TL(UiDraw.Text(card, "Title", "Mastery Insignias", UiFont.BodySemiBold, 16f, OnSurface).rectTransform, 16f, 36f, 220f, 22f);
            var count = UiDraw.Text(card, "Count", $"{unlocked} / {entries.Length} UNLOCKED", UiFont.MonoMedium, 10f, OnVariant, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(count) + 14f;
            var countPlate = UiDraw.Box(card, "CountPlate", Mid, 4f);
            countPlate.transform.SetSiblingIndex(count.transform.GetSiblingIndex());
            UiDraw.Place(countPlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -40f), new Vector2(cw, 18f));
            UiDraw.Place(count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -40f), new Vector2(cw, 18f));
            var desc = UiDraw.Text(card, "Body", "Equip an authoritative seal to project your verified flight specialization across match telemetry.", UiFont.Body, 11f, OnVariant);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 16f, 66f, w - 32f, 32f);
            float ey = 108f;
            foreach (var e in entries)
            {
                var row = UiDraw.Box(card, "Badge_" + e.name, e.earned ? Mid : UiDraw.Hex("#18202f", .5f), 8f);
                TL(row.rectTransform, 16f, ey, w - 32f, 54f);
                var plate = UiDraw.Box(row.transform, "IconPlate", e.earned ? Highest : UiDraw.Hex("#2d3545", .5f), 6f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(36f, 36f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", e.earned ? e.icon : "lock", 18f, e.earned ? OnSurface : Outline).rectTransform);
                TL(UiDraw.Text(row.transform, "Name", e.name, UiFont.BodySemiBold, 14f, e.earned ? OnSurface : Outline).rectTransform, 56f, 9f, w - 180f, 18f);
                TL(UiDraw.Text(row.transform, "Req", $"{e.tier} // {e.req}", UiFont.MonoMedium, 10f, Outline, .1f).rectTransform, 56f, 29f, w - 180f, 14f);
                var state = UiDraw.Text(row.transform, "State", e.earned ? "EARNED" : "LOCKED", UiFont.MonoMedium, 10f, e.earned ? Primary : Outline, .1f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(state.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(80f, 14f));
                ey += 62f;
            }
            return y + h;
        }

        // ------------------------------------------------------------ footer

        static void Footer(RectTransform root, bool mastery, bool preview, Action back, Action other)
        {
            var bar = UiDraw.Box(root, "Footer", UiDraw.Hex("#141c2b", .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 88f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UiDraw.Hex("#3c494b", .5f), 0f);
            var backButton = UiDraw.Tap(bar, "Back", High, 12f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var backText = UiDraw.Text(backButton.transform, "Label", mastery ? "RETURN TO HANGAR" : "Return to Lobby", UiFont.HeadingSemiBold, 16f, OnSurface, .01f);
            float bw = 24f + 18f + 10f + UiDraw.Width(backText) + 24f;
            UiDraw.Place((RectTransform)backButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 52f));
            UiDraw.Place(UiDraw.Icon(backButton.transform, "Icon", "arrow_back", 18f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 22f));
            var integrity = UiDraw.Text(bar, "Integrity", preview ? "PROGRESSION PIPELINE  •  NOT ENABLED IN THIS BUILD" : "VERIFIED PUBLIC MATCH RECORD  •  COSMETIC REWARDS ONLY",
                UiFont.MonoMedium, 11f, OnVariant, .1f, TextAlignmentOptions.Midline);
            UiDraw.Place(integrity.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(520f, 16f));
            var otherButton = UiDraw.Tap(bar, "Other", High, 12f, () => { GameAudio.Play2D(GameCue.UiTap); other?.Invoke(); });
            var otherText = UiDraw.Text(otherButton.transform, "Label", mastery ? "FLIGHT CONTRACTS" : "View Flight Mastery", UiFont.HeadingSemiBold, 16f, Primary, .01f);
            float ow = 24f + 18f + 10f + UiDraw.Width(otherText) + 24f;
            UiDraw.Place((RectTransform)otherButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(ow, 52f));
            UiDraw.Place(UiDraw.Icon(otherButton.transform, "Icon", mastery ? "description" : "terminal", 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(otherText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(otherText) + 2f, 22f));
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

        static string Unit(string id) => id switch { "finish_together" => "Matches", "hold_the_relay" => "SEC", "build_a_team_route" => "Teammates", _ => "" };

        static int Find(GoalsDto data, string track, string weapon) => (data.mastery ?? Array.Empty<MasteryProgressDto>())
            .FirstOrDefault(x => x != null && x.trackId == track && x.weaponId == weapon)?.progress ?? 0;

        static string Name(string id) => id switch { "finish_together" => "Finish Together",
            "hold_the_relay" => "Hold the Relay", "build_a_team_route" => "Build a Team Route", _ => id ?? "Directive" };
        static string Metric(string id) => id switch { "finish_together" => "Match completion",
            "hold_the_relay" => "Relay presence", "build_a_team_route" => "Pad trajectories", _ => "Progress" };
        static string Description(string id) => id switch {
            "finish_together" => "Complete eligible public matches with a registered team unit.",
            "hold_the_relay" => "Earn valid uncontested objective occupation across eligible matches.",
            "build_a_team_route" => "Two distinct human teammates launch using your tactical pads in eligible matches.",
            _ => "Advance this directive in eligible public matches." };
        static string Condition(string id) => id switch {
            "finish_together" => "Condition: Both regulation and overtime public matches qualify. Solo queue fill or pre-formed parties count equally.",
            "hold_the_relay" => "Condition: Contested zones and airborne passes score 0 seconds. Standing grounded inside the active Relay node counts.",
            "build_a_team_route" => "Condition: Teammates must launch from your armed pad. Self-launches and simulated bots do not qualify.",
            _ => "Condition: Verified eligible match results only." };
    }
}
