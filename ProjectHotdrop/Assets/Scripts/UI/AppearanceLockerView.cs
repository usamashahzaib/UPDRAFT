using System;
using System.Linq;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 15 locker, rebuilt from its UIUX HTML (CSS px) around the actual rendered, draggable
    /// operative preview. Only owned, server-confirmed cosmetics are listed; nothing is priced.
    /// </summary>
    public static class AppearanceLockerView
    {
        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleLargeDp;
        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color Mid => Color.Lerp(UIDesignTokens.Background, UIDesignTokens.SurfacePanel, .55f);
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Highest => UIDesignTokens.SurfaceDisabled;
        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color PrimaryC => UIDesignTokens.PrimaryAction;
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.TextMuted;
        static Color Ink => UIDesignTokens.TextOnAccent;

        const float Pad = 24f;

        public static void SetStatus(GameObject owner, string value)
        {
            var label = owner != null ? owner.transform.Find("AppearanceLockerVisual/Footer/Status/Message")?.GetComponent<TextMeshProUGUI>() : null;
            if (label != null) label.text = value ?? "";
        }

        public static void Show(GameObject owner, RawImage preview, InventoryItemDto[] items,
            string category, string equippedBody, string equippedWrap, string equippedBadge,
            int currentLevel, WrapUnlockDto[] wrapUnlocks, bool inventoryUnavailable,
            string state, Action back, Action changeRunner, Action<string> changeCategory,
            Action<InventoryItemDto> inspect, Action<InventoryItemDto> equip, bool techSpecsExpanded = false,
            Action retry = null)
        {
            if (owner == null) return;
            var old = owner.transform.Find("AppearanceLockerVisual");
            if (old != null)
            {
                // The live preview image is reparented into each build; keep it alive across rebuilds.
                if (preview != null && preview.transform.IsChildOf(old)) preview.transform.SetParent(owner.transform, false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject);
                else UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var root = UiDraw.Node(owner.transform, "AppearanceLockerVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Lowest).rectTransform);
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

            float y = 16f;
            y = Header(page, w, y) + 16f;
            float leftW = (w - 20f) * .585f, rightW = w - 20f - leftW, rightX = Pad + leftW + 20f;
            float ly = Stage(page, Pad, y, leftW, preview, equippedWrap) + 16f;
            ly = FairPlay(page, Pad, ly, leftW) + 16f;
            float ry = Library(page, rightX, y, rightW, items, category, equippedBody, equippedWrap, equippedBadge,
                currentLevel, wrapUnlocks, inventoryUnavailable, changeCategory, inspect, equip) + 16f;
            ry = Spec(page, rightX, ry, rightW, equippedBody, changeRunner, techSpecsExpanded, () =>
            {
                GameAudio.Play2D(GameCue.UiTap);
                Telemetry.Track("appearance_tech_specs_toggled", ("expanded", !techSpecsExpanded));
                Show(owner, preview, items, category, equippedBody, equippedWrap, equippedBadge,
                    currentLevel, wrapUnlocks, inventoryUnavailable, state, back, changeRunner,
                    changeCategory, inspect, equip, !techSpecsExpanded, retry);
            }) + 16f;
            page.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);
            page.anchoredPosition = Vector2.zero;
            Footer(root, width, state, back, inventoryUnavailable ? retry : null);
        }

        static float Header(RectTransform page, float w, float y)
        {
            var card = Card(page, "Introduction", Low, Pad, y, w, 72f);
            TL(UiDraw.Box(card, "Accent", PrimaryC, 3f).rectTransform, 16f, 20f, 6f, 32f);
            var title = UiDraw.Text(card, "Title", "APPEARANCE", UiFont.Heading, TitleDp, OnSurface, -.03f);
            float tw = UiDraw.Width(title);
            TL(title.rectTransform, 36f, 12f, tw + 4f, 30f);
            var chip = UiDraw.Text(card, "Registry", "COSMETIC ONLY", UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chip) + 12f;
            var chipPlate = UiDraw.Box(card, "RegistryPlate", High, 4f);
            chipPlate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            TL(chipPlate.rectTransform, 36f + tw + 8f, 17f, cw, 20f); TL(chip.rectTransform, 36f + tw + 8f, 17f, cw, 20f);
            TL(UiDraw.Icon(card, "Shield", "verified_user", 14f, Primary).rectTransform, 36f, 46f, 14f, 14f);
            TL(UiDraw.Text(card, "Rule", "Cosmetic only. Zero gameplay effect.", UiFont.Body, CaptionDp, OnVariant).rectTransform, 56f, 44f, w - 72f, 16f);
            return y + 72f;
        }

        static float Stage(RectTransform page, float x, float y, float w, RawImage preview, string equippedWrap)
        {
            float h = Mathf.Round(w * .78f);
            var card = Card(page, "Stage", Low, x, y, w, h);
            card.gameObject.AddComponent<RectMask2D>();
            var scenery = UiDraw.Node(card, "Rooftop").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(scenery.rectTransform);
            scenery.texture = Resources.Load<Texture2D>("HomeRooftop");
            float aspect = w / h;
            scenery.uvRect = new Rect(.5f - .5f * aspect / (16f / 9f), 0f, aspect / (16f / 9f), 1f);
            scenery.color = new Color(.8f, .84f, .9f, 1f);
            scenery.raycastTarget = false;
            UiDraw.VerticalGradient(card, "Shade", UIDesignTokens.WithAlpha(Lowest, .85f), UIDesignTokens.WithAlpha(Lowest, .15f), UIDesignTokens.WithAlpha(Lowest, .5f));
            // Turntable ring under the operative.
            var turntable = UiDraw.Ring(card, "Turntable", 300f, 2f, UIDesignTokens.WithAlpha(PrimaryC, .45f), 40);
            UiDraw.Place(turntable.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 96f), new Vector2(300f, 300f));
            turntable.rectTransform.localScale = new Vector3(1f, .28f, 1f);
            var previewHost = UiDraw.Node(card, "OperativePreview");
            UiDraw.Place(previewHost, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 80f), new Vector2(h * .62f, h * .78f));
            if (preview != null)
            {
                preview.transform.SetParent(previewHost, false);
                UiDraw.Fill(preview.rectTransform);
            }
            Chip(card, "StageName", "", "SWITCHYARD ROOFTOP", new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -24f), true);
            Chip(card, "Lighting", "wb_sunny", "PILOT PREVIEW", new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -24f), false);
            // Equipment plate.
            var plate = UiDraw.Box(card, "Equipped", UIDesignTokens.WithAlpha(Lowest, .85f), 8f);
            UiDraw.Place(plate.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 150f), new Vector2(Mathf.Min(w - 80f, 560f), 58f));
            TL(UiDraw.Text(plate.transform, "WeaponLabel", "CURRENT ARMED WEAPON", UiFont.Mono, CaptionDp, OnVariant, .1f).rectTransform, 18f, 8f, 260f, 14f);
            TL(UiDraw.Text(plate.transform, "Weapon", "ARC PRECISION RIFLE", UiFont.Heading, UiDp, Primary, -.02f).rectTransform, 18f, 24f, 320f, 28f);
            var wrapLabel = UiDraw.Text(plate.transform, "WrapLabel", "ACTIVE WRAP", UiFont.Mono, CaptionDp, OnVariant, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(wrapLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-18f, -15f), new Vector2(200f, 14f));
            var wrap = UiDraw.Text(plate.transform, "Wrap", string.IsNullOrEmpty(equippedWrap) ? "STANDARD" : CosmeticLook.DisplayName(equippedWrap).ToUpperInvariant(), UiFont.Mono, UiDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(wrap.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-18f, -37f), new Vector2(220f, 18f));
            Chip(card, "Drag", "360", "DRAG TO ROTATE", new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 28f), false);
            return y + h;
        }

        static void Chip(RectTransform card, string name, string icon, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, bool dot)
        {
            var plate = UiDraw.Box(card, name, UIDesignTokens.WithAlpha(Lowest, .8f), 8f);
            var label = UiDraw.Text(plate.transform, "Text", text, UiFont.Mono, CaptionDp, OnSurface, .1f);
            float lead = dot ? 26f : string.IsNullOrEmpty(icon) ? 12f : 36f;
            float pw = lead + UiDraw.Width(label) + 12f;
            UiDraw.Place(plate.rectTransform, anchor, pivot, pos, new Vector2(pw, 30f));
            if (dot) UiDraw.Place(UiDraw.Dot(plate.transform, "Dot", 8f, PrimaryC).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(8f, 8f));
            else if (!string.IsNullOrEmpty(icon))
                UiDraw.Place(UiDraw.Icon(plate.transform, "Icon", icon, 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(lead, 0f), new Vector2(UiDraw.Width(label) + 2f, 16f));
        }

        static float FairPlay(RectTransform page, float x, float y, float w)
        {
            var card = Card(page, "FairPlay", Low, x, y, w, 80f);
            var plate = UiDraw.Box(card, "IconPlate", High, 8f);
            UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(44f, 44f));
            UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", "balance", 24f, Primary).rectTransform);
            TL(UiDraw.Text(card, "Title", "FAIR PLAY", UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 76f, 14f, w - 260f, 24f);
            var body = UiDraw.Text(card, "Rule", "HOTDROP locker unlocks are earned through matches and mastery. Visual items grant no gameplay advantage.", UiFont.Body, CaptionDp, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            TL(body.rectTransform, 76f, 40f, w - 260f, 32f);
            var tag = UiDraw.Text(card, "Tag", $"<color=#{ColorUtility.ToHtmlStringRGB(Tertiary)}>NO\nLOOTBOXES</color>\nNO COMBAT\nBUFFS", UiFont.Mono, CaptionDp, OnVariant, .1f);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(110f, 60f));
            return y + 80f;
        }

        static float Library(RectTransform page, float x, float y, float w, InventoryItemDto[] items, string category,
            string equippedBody, string equippedWrap, string equippedBadge, int currentLevel,
            WrapUnlockDto[] wrapUnlocks, bool inventoryUnavailable, Action<string> changeCategory,
            Action<InventoryItemDto> inspect, Action<InventoryItemDto> equip)
        {
            string[] categories = { "Body Presentation", "Body Style", "Weapon Wrap", "Badge" };
            string[] tabNames = { "BODY", "STYLES", "WRAPS", "BADGES" };
            string[] captions = { "BODY PRES.", "BODY STYLE", "WEAPON WRAP", "BADGES" };
            string[] icons = { "accessibility_new", "checkroom", "colorize", "military_tech" };
            var tabs = Card(page, "Tabs", Lowest, x, y, w, 72f);
            float tw = (w - 8f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                string target = categories[i];
                bool on = category == target;
                var tab = UiDraw.Tap(tabs, "InventoryTab_" + tabNames[i], on ? High : new Color(0f, 0f, 0f, 0f), 8f, () => changeCategory?.Invoke(target));
                TL((RectTransform)tab.transform, 4f + i * tw, 4f, tw, 64f);
                UiDraw.Place(UiDraw.Icon(tab.transform, "Icon", icons[i], 20f, on ? Primary : OnSurface).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 12f), new Vector2(20f, 20f));
                UiDraw.Place(UiDraw.Text(tab.transform, "Label", captions[i], UiFont.Heading, CaptionDp, on ? Primary : OnSurface, .05f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -14f), new Vector2(tw - 8f, 16f));
            }
            float cy = y + 72f + 12f;
            var owned = (items ?? Array.Empty<InventoryItemDto>()).Where(i => i != null && Category(i.cosmeticId) == category).ToArray();
            int ownedAll = items?.Length ?? 0;
            var filter = Card(page, "Filter", Mid, x, cy, w, 40f);
            bool inventoryKnown = items != null && !inventoryUnavailable;
            var filterLabel = UiDraw.Text(filter, "Label", inventoryKnown ? "FILTER:" : "INVENTORY:", UiFont.Mono, CaptionDp, OnVariant, .1f);
            UiDraw.Place(filterLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(84f, 14f));
            float fx = 84f;
            if (!inventoryKnown)
            {
                var inventoryStatus = UiDraw.Text(filter, "InventoryStatus", inventoryUnavailable ? "UNAVAILABLE" : "SYNCING",
                    UiFont.Mono, CaptionDp, inventoryUnavailable ? Tertiary : OnVariant, .08f, TextAlignmentOptions.Midline);
                UiDraw.Place(inventoryStatus.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(150f, 24f));
            }
            else
                foreach (var (text, on) in new[] { ($"IN CATEGORY ({owned.Length})", true), ($"OWNED ({ownedAll})", false) })
                {
                    var t = UiDraw.Text(filter, "Filter", text, UiFont.Mono, CaptionDp, on ? Primary : OnVariant, .1f, TextAlignmentOptions.Midline);
                    float fw = UiDraw.Width(t) + 18f;
                    var p = UiDraw.Box(filter, "FilterPlate", on ? High : Low, 4f);
                    p.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
                    UiDraw.Place(p.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                    UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                    fx += fw + 8f;
                }
            UiDraw.Place(UiDraw.Text(filter, "Shop", "NO PRICES • EARNED ONLY", UiFont.Mono, CaptionDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, 0f), new Vector2(200f, 14f));
            cy += 40f + 12f;
            var list = UiDraw.Node(page, "Library");
            TL(list, x, cy, w, 10f);
            if (owned.Length == 0)
            {
                var empty = UiDraw.Box(list, "Empty", Low, 12f);
                bool wrapGuide = category == "Weapon Wrap" && inventoryKnown;
                float emptyHeight = wrapGuide ? 138f : 112f;
                TL(empty.rectTransform, 0f, 0f, w, emptyHeight);
                string title = inventoryUnavailable ? "INVENTORY UNAVAILABLE"
                    : items == null ? "LOADING INVENTORY"
                    : wrapGuide ? "HOW TO EARN A WEAPON WRAP" : "NO OWNED ITEMS IN THIS CATEGORY";
                TL(UiDraw.Text(empty.transform, "Title", title, UiFont.Heading, UiDp, OnSurface, .03f).rectTransform, 20f, 16f, w - 40f, 22f);
                string detailCopy = inventoryUnavailable ? "Your saved items could not be checked. Retry to confirm ownership."
                    : items == null ? "Checking your server-confirmed inventory…"
                    : wrapGuide ? "Earn wraps through eligible public-match progress."
                    : "Only owned, server-confirmed cosmetics appear here. Your standard pilot look is always available.";
                var detail = UiDraw.Text(empty.transform, "Detail", detailCopy, UiFont.Body, CaptionDp, OnVariant);
                detail.textWrappingMode = TextWrappingModes.Normal; detail.alignment = TextAlignmentOptions.TopLeft;
                TL(detail.rectTransform, 20f, 46f, w - 40f, wrapGuide ? 28f : 48f);
                if (wrapGuide)
                {
                    var milestones = (wrapUnlocks ?? Array.Empty<WrapUnlockDto>())
                        .Where(u => u != null).OrderBy(u => u.level).ToArray();
                    var next = currentLevel > 0 ? milestones.FirstOrDefault(u => u.level > currentLevel) : null;
                    string path = next != null
                        ? $"Next server-configured wrap: {WrapName(next.cosmeticId)} at level {next.level}."
                        : currentLevel <= 0 && milestones.Length > 0
                            ? "Server-configured milestones: " + string.Join(" · ", milestones.Select(u => $"{WrapName(u.cosmeticId)} — level {u.level}")) + "."
                            : milestones.Length == 0
                                ? "No weapon-wrap level rewards are configured right now."
                                : "You have reached the last configured wrap milestone. No later wrap reward is listed.";
                    var unlockPath = UiDraw.Text(empty.transform, "UnlockPath", path, UiFont.Mono, CaptionDp, Primary, .02f);
                    unlockPath.textWrappingMode = TextWrappingModes.Normal; unlockPath.alignment = TextAlignmentOptions.TopLeft;
                    TL(unlockPath.rectTransform, 20f, 80f, w - 40f, 46f);
                }
                cy += emptyHeight;
            }
            else
            {
                float ly = 0f;
                foreach (var item in owned.Take(4))
                {
                    string activeId = category == "Weapon Wrap" ? equippedWrap : category == "Badge" ? equippedBadge : equippedBody;
                    bool active = activeId == item.cosmeticId || (string.IsNullOrEmpty(activeId) && item.cosmeticId == "body_standard");
                    var row = UiDraw.Tap(list, "Item_" + item.id, Low, 12f, () => inspect?.Invoke(item), active ? UIDesignTokens.WithAlpha(PrimaryC, .35f) : (Color?)null);
                    TL((RectTransform)row.transform, 0f, ly, w, 116f);
                    var r = (RectTransform)row.transform;
                    var stateText = UiDraw.Text(r, "State", active ? "EQUIPPED" : "OWNED", UiFont.Mono, CaptionDp, active ? Ink : OnVariant, .05f, TextAlignmentOptions.Midline);
                    float sw = UiDraw.Width(stateText) + 10f;
                    var statePlate = UiDraw.Box(r, "StatePlate", active ? PrimaryC : Highest, 2f);
                    statePlate.transform.SetSiblingIndex(stateText.transform.GetSiblingIndex());
                    TL(statePlate.rectTransform, 14f, 14f, sw, 18f); TL(stateText.rectTransform, 14f, 14f, sw, 18f);
                    TL(UiDraw.Text(r, "Slot", $"SLOT  ·  {SlotName(category)}", UiFont.Mono, CaptionDp, OnVariant, .1f).rectTransform, 20f + sw, 15f, 220f, 16f);
                    var thumb = UiDraw.Box(r, "Thumb", Lowest, 4f);
                    TL(thumb.rectTransform, 14f, 42f, 64f, 60f);
                    UiDraw.Fill(UiDraw.Icon(thumb.transform, "Icon", category == "Weapon Wrap" ? "colorize" : category == "Badge" ? "military_tech" : "accessibility_new", 28f, Primary).rectTransform);
                    TL(UiDraw.Text(r, "Name", CosmeticLook.DisplayName(item.cosmeticId), UiFont.BodySemiBold, UiDp, OnSurface).rectTransform, 92f, 48f, w - 260f, 24f);
                    TL(UiDraw.Text(r, "Source", Source(item.acquiredVia), UiFont.Mono, CaptionDp, Primary, .05f).rectTransform, 92f, 74f, w - 260f, 16f);
                    if (active)
                    {
                        UiDraw.Place(UiDraw.Icon(r, "Check", "check_circle", 20f, Primary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -24f), new Vector2(20f, 20f));
                        UiDraw.Place(UiDraw.Text(r, "Active", "ACTIVE", UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                            new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -8f), new Vector2(100f, 16f));
                    }
                    else
                    {
                        var preview = UiDraw.Tap(r, "Preview", High, 6f, () => inspect?.Invoke(item));
                        UiDraw.Place((RectTransform)preview.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-104f, -10f), new Vector2(84f, 32f));
                        UiDraw.Fill(UiDraw.Text(preview.transform, "Label", "PREVIEW", UiFont.Mono, CaptionDp, Primary, .05f, TextAlignmentOptions.Midline).rectTransform);
                        var equipButton = UiDraw.Tap(r, "Equip", PrimaryC, 6f, () => equip?.Invoke(item));
                        UiDraw.Place((RectTransform)equipButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -10f), new Vector2(80f, 32f));
                        UiDraw.Fill(UiDraw.Text(equipButton.transform, "Label", "EQUIP", UiFont.Mono, CaptionDp, Ink, .05f, TextAlignmentOptions.Midline).rectTransform);
                    }
                    ly += 116f + 12f;
                }
                cy += ly - 12f;
            }
            return cy;
        }

        static float Spec(RectTransform page, float x, float y, float w, string equippedBody,
            Action changeRunner, bool expanded, Action toggleSpecs)
        {
            var card = Card(page, "Telemetry", Low, x, y, w, 10f);
            TL(UiDraw.Text(card, "Heading", expanded ? "TECHNICAL DETAILS" : "EQUIPPED APPEARANCE", UiFont.Heading, CaptionDp, Primary, .05f).rectTransform, 16f, 18f, w - 220f, 16f);
            var saved = UiDraw.Box(card, "Saved", Mid, 4f);
            var savedText = UiDraw.Text(saved.transform, "Text", "SAVED TO YOUR PROFILE", UiFont.Mono, CaptionDp, OnSurface, .1f);
            float svw = 10f + 6f + 6f + UiDraw.Width(savedText) + 10f;
            UiDraw.Place(saved.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -26f), new Vector2(svw, 22f));
            UiDraw.Place(UiDraw.Dot(saved.transform, "Dot", 6f, PrimaryC).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(6f, 6f));
            UiDraw.Place(savedText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f, 0f), new Vector2(UiDraw.Width(savedText) + 2f, 14f));
            string name = string.IsNullOrEmpty(equippedBody) ? "Standard Flight Utility" : CosmeticLook.DisplayName(equippedBody);
            TL(UiDraw.Text(card, "Name", name, UiFont.HeadingSemiBold, UiDp, OnSurface).rectTransform, 16f, 42f, w - 32f, 30f);
            if (expanded)
            {
                var stats = UiDraw.Box(card, "Stats", Mid, 8f);
                TL(stats.rectTransform, 16f, 116f, w - 32f, 64f);
                float sw = (w - 32f) / 3f;
                var entries = new[] { ("HITBOX", "UNCHANGED"), ("SPEED", "UNCHANGED"), ("SOUND", "UNCHANGED") };
                for (int i = 0; i < 3; i++)
                {
                    UiDraw.Place(UiDraw.Text(stats.transform, "StatLabel" + i, entries[i].Item1, UiFont.Mono, CaptionDp, OnVariant, .1f, TextAlignmentOptions.Midline).rectTransform,
                        new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(sw * (i + .5f), 10f), new Vector2(sw, 14f));
                    UiDraw.Place(UiDraw.Text(stats.transform, "StatValue" + i, entries[i].Item2, UiFont.Mono, UiDp, OnSurface, .05f, TextAlignmentOptions.Midline).rectTransform,
                        new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(sw * (i + .5f), -10f), new Vector2(sw, 18f));
                }
                var source = UiDraw.Box(card, "Source", Mid, 8f);
                TL(source.rectTransform, 16f, 188f, w - 32f, 32f);
                UiDraw.Place(UiDraw.Icon(source.transform, "Icon", "verified", 16f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
                UiDraw.Place(UiDraw.Text(source.transform, "Text", "Default look  ·  always owned", UiFont.Mono, CaptionDp, OnSurface, .05f).rectTransform,
                    new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(w - 80f, 16f));
            }
            var disclosure = UiDraw.Tap(card, "TechSpecsDisclosure", High, 8f, toggleSpecs);
            TL((RectTransform)disclosure.transform, 16f, expanded ? 228f : 116f, w - 32f, 48f);
            UiDraw.Fill(UiDraw.Text(disclosure.transform, "Label", expanded ? "HIDE TECHNICAL DETAILS" : "VIEW TECHNICAL DETAILS", UiFont.HeadingSemiBold, UiDp, Primary, .01f, TextAlignmentOptions.Midline).rectTransform, 12f, 4f, 12f, 4f);
            float actionY = expanded ? 288f : 176f;
            var button = UiDraw.Tap(card, "ChangeRunner", PrimaryC, 8f, () => { GameAudio.Play2D(GameCue.UiTap); changeRunner?.Invoke(); });
            TL((RectTransform)button.transform, 16f, actionY, w - 32f - 60f - 12f, 60f);
            var label = UiDraw.Text(button.transform, "Label", "CHANGE RUNNER", UiFont.HeadingSemiBold, UiDp, Ink, .02f);
            float lw = UiDraw.Width(label);
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", "check", 20f, Ink).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 12f) / 2f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(16f, 0f), new Vector2(lw + 2f, 28f));
            var focus = UiDraw.Box(card, "Focus", High, 8f);
            TL(focus.rectTransform, w - 16f - 60f, actionY, 60f, 60f);
            UiDraw.Fill(UiDraw.Icon(focus.transform, "Icon", "center_focus_strong", 22f, OnSurface).rectTransform);
            float height = actionY + 60f + 16f;
            card.sizeDelta = new Vector2(w, height);
            return y + height;
        }

        static void Footer(RectTransform root, float width, string state, Action back, Action retry)
        {
            var bar = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(UIDesignTokens.Background, .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 76f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .5f), 0f);
            var backButton = UiDraw.Tap(bar, "Back", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var backText = UiDraw.Text(backButton.transform, "Label", "BACK", UiFont.Heading, UiDp, OnSurface, .01f);
            float bw = 20f + 20f + 12f + UiDraw.Width(backText) + 24f;
            UiDraw.Place((RectTransform)backButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 52f));
            UiDraw.Place(UiDraw.Icon(backButton.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 28f));
            // An unavailable inventory keeps its retry in this sticky footer: the page covers the
            // shared menu rows, so a retry placed there would be hidden.
            float rightEdge = Pad;
            if (retry != null)
            {
                var again = UiDraw.Tap(bar, "Retry", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); retry(); });
                var againText = UiDraw.Text(again.transform, "Label", "RETRY", UiFont.Heading, UiDp, Primary, .01f);
                float rw = 20f + 20f + 12f + UiDraw.Width(againText) + 24f;
                UiDraw.Place((RectTransform)again.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-rightEdge, 0f), new Vector2(rw, 52f));
                UiDraw.Place(UiDraw.Icon(again.transform, "Icon", "refresh", 20f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
                UiDraw.Place(againText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(againText) + 2f, 28f));
                rightEdge += rw + 16f;
            }
            float statusX = Pad + bw + 16f;
            var status = UiDraw.Node(bar, "Status");
            UiDraw.Place(status, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(statusX, 0f),
                new Vector2(Mathf.Clamp(width - statusX - rightEdge, 0f, 800f), 20f));
            var msg = UiDraw.Text(status, "Message", state ?? "", UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            UiDraw.Fill(msg.rectTransform);
            msg.overflowMode = TextOverflowModes.Ellipsis;
        }

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TL(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

        static string SlotName(string category) => category switch
        {
            "Weapon Wrap" => "ARC RIFLE", "Badge" => "FLIGHT CARD", "Body Style" => "BODY STYLE", _ => "BODY SUIT"
        };

        static string Category(string id) => string.IsNullOrEmpty(id) || id == "body_standard"
            ? "Body Presentation" : id.StartsWith("body_", StringComparison.Ordinal) ? "Body Style"
            : id.StartsWith("wrap_", StringComparison.Ordinal) ? "Weapon Wrap"
            : id.StartsWith("badge_", StringComparison.Ordinal) ? "Badge" : "Body Presentation";
        static string WrapName(string id)
        {
            const string prefix = "Weapon wrap: ";
            string name = CosmeticLook.DisplayName(id);
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? name.Substring(prefix.Length) : name;
        }
        static string Source(string via) => via == "starter" ? "Default Issue  •  Starter Collection"
            : via == "unlock" ? "Match Unlock  •  Earned" : string.IsNullOrEmpty(via) ? "Source unavailable" : via;
    }
}
