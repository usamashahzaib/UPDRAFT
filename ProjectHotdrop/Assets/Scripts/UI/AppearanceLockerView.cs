using System;
using System.Collections.Generic;
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
            var title = UiDraw.Text(card, "Title", UiDraw.L("appearance.title", "Appearance"), UiFont.Heading, TitleDp, OnSurface, -.03f);
            var chip = UiDraw.Text(card, "Registry", UiDraw.L("appearance.registry", "Cosmetic Only"), UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.Midline);
            float cw = Mathf.Min(w * .42f, UiDraw.Width(chip) + 12f);
            float chipX = Mathf.Max(36f, w - cw - 16f);
            float titleW = Mathf.Max(1f, chipX - 36f - 8f);
            title.text = UiDraw.Truncate(title, title.text, titleW);
            TL(title.rectTransform, 36f, 12f, titleW, 30f);
            var chipPlate = UiDraw.Box(card, "RegistryPlate", High, 4f);
            chipPlate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            chip.text = UiDraw.Truncate(chip, chip.text, Mathf.Max(1f, cw - 12f));
            TL(chipPlate.rectTransform, chipX, 17f, cw, 20f); TL(chip.rectTransform, chipX, 17f, cw, 20f);
            TL(UiDraw.Icon(card, "Shield", "verified_user", 14f, Primary).rectTransform, 36f, 46f, 14f, 14f);
            TL(UiDraw.Text(card, "Rule", UiDraw.L("appearance.rule", "Cosmetic only. Zero gameplay effect."), UiFont.Body, CaptionDp, OnVariant).rectTransform, 56f, 44f, w - 72f, 16f);
            return y + 72f;
        }

        static float Stage(RectTransform page, float x, float y, float w, RawImage preview, string equippedWrap)
        {
            float h = Mathf.Round(w * .78f);
            var card = Card(page, "Stage", Low, x, y, w, h);
            card.gameObject.AddComponent<RectMask2D>();
            var scenery = UiDraw.Node(card, "Rooftop").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(scenery.rectTransform);
            scenery.texture = UiPresentationProfile.LoadRooftopArtwork();
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
            Chip(card, "StageName", "", UiDraw.L("appearance.stage.name", "Switchyard Rooftop"), new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -24f), true);
            Chip(card, "Lighting", "wb_sunny", UiDraw.L("appearance.stage.lighting", "Pilot Preview"), new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -24f), false);
            // Equipment plate.
            var plate = UiDraw.Box(card, "Equipped", UIDesignTokens.WithAlpha(Lowest, .85f), 8f);
            float plateW = Mathf.Min(w - 80f, 560f);
            UiDraw.Place(plate.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 150f), new Vector2(plateW, 58f));
            float columnW = Mathf.Max(1f, (plateW - 54f) * .5f);
            var weaponLabel = UiDraw.Text(plate.transform, "WeaponLabel", UiDraw.L("appearance.stage.weaponLabel", "Current Armed Weapon"), UiFont.Mono, CaptionDp, OnVariant, .1f);
            weaponLabel.text = UiDraw.Truncate(weaponLabel, weaponLabel.text, columnW);
            TL(weaponLabel.rectTransform, 18f, 8f, columnW, 14f);
            var weapon = UiDraw.Text(plate.transform, "Weapon", UiDraw.L("appearance.stage.weaponValue", "Arc Precision Rifle"), UiFont.Heading, UiDp, Primary, -.02f);
            weapon.text = UiDraw.Truncate(weapon, weapon.text, columnW);
            TL(weapon.rectTransform, 18f, 24f, columnW, 28f);
            var wrapLabel = UiDraw.Text(plate.transform, "WrapLabel", UiDraw.L("appearance.stage.wrapLabel", "Active Wrap"), UiFont.Mono, CaptionDp, OnVariant, .1f, TextAlignmentOptions.MidlineRight);
            float wrapX = 36f + columnW;
            wrapLabel.text = UiDraw.Truncate(wrapLabel, wrapLabel.text, columnW);
            TL(wrapLabel.rectTransform, wrapX, 8f, columnW, 14f);
            var wrap = UiDraw.Text(plate.transform, "Wrap", string.IsNullOrEmpty(equippedWrap) ? UiDraw.L("appearance.stage.wrapStandard", "Standard") : CosmeticLook.DisplayName(equippedWrap), UiFont.Mono, UiDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            wrap.text = UiDraw.Truncate(wrap, wrap.text, columnW);
            TL(wrap.rectTransform, wrapX, 24f, columnW, 28f);
            Chip(card, "Drag", "360", UiDraw.L("appearance.stage.drag", "Drag to Rotate"), new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 28f), false);
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
            TL(UiDraw.Text(card, "Title", UiDraw.L("appearance.fairPlay.title", "Fair Play"), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 76f, 14f, w - 260f, 24f);
            var body = UiDraw.Text(card, "Rule", UiDraw.L("appearance.fairPlay.body", "HOTDROP locker unlocks are earned through matches and mastery. Visual items grant no gameplay advantage."), UiFont.Body, CaptionDp, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            TL(body.rectTransform, 76f, 40f, w - 260f, 32f);
            var tag = UiDraw.Text(card, "Tag", string.Format(UiDraw.L("appearance.fairPlay.tag", "<color=#{0}>NO\nLOOTBOXES</color>\nNO COMBAT\nBUFFS"), ColorUtility.ToHtmlStringRGB(Tertiary)), UiFont.Mono, CaptionDp, OnVariant, .1f);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(130f, 60f));
            return y + 80f;
        }

        static float Library(RectTransform page, float x, float y, float w, InventoryItemDto[] items, string category,
            string equippedBody, string equippedWrap, string equippedBadge, int currentLevel,
            WrapUnlockDto[] wrapUnlocks, bool inventoryUnavailable, Action<string> changeCategory,
            Action<InventoryItemDto> inspect, Action<InventoryItemDto> equip)
        {
            string[] categories = { "Body Presentation", "Body Style", "Weapon Wrap", "Badge" };
            string[] tabNames = { "BODY", "STYLES", "WRAPS", "BADGES" };
            string[] captions =
            {
                UiDraw.L("appearance.tab.bodyPres", "Body Pres."),
                UiDraw.L("appearance.tab.bodyStyle", "Body Style"),
                UiDraw.L("appearance.tab.weaponWrap", "Weapon Wrap"),
                UiDraw.L("appearance.tab.badges", "Badges"),
            };
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
                // At the narrow end of the right column (reproduced at 1920, 4 tabs) the full
                // caption is wider than its own tw-8 column and, left in the default Overflow
                // mode, drew into the next tab's caption. TMP's Ellipsis overflow mode was tried
                // here first and found to render the whole caption blank in this project's batch
                // PlayMode captures (see UiDraw.Truncate), so the caption is measured and trimmed
                // before it is ever shown instead.
                var caption = UiDraw.Text(tab.transform, "Label", captions[i], UiFont.Heading, CaptionDp, on ? Primary : OnSurface, .05f, TextAlignmentOptions.Midline);
                caption.text = UiDraw.Truncate(caption, captions[i], tw - 8f);
                UiDraw.Place(caption.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -14f), new Vector2(tw - 8f, 16f));
            }
            float cy = y + 72f + 12f;
            var owned = (items ?? Array.Empty<InventoryItemDto>()).Where(i => i != null && Category(i.cosmeticId) == category).ToArray();
            int ownedAll = items?.Length ?? 0;
            var filter = Card(page, "Filter", Mid, x, cy, w, 40f);
            var filterRowElements = new List<RectTransform>();
            bool inventoryKnown = items != null && !inventoryUnavailable;
            // The nominal 84-wide box undersized "INVENTORY:" (reproduced at 1920 and 2400,
            // NoWrap + Overflow let it draw into the first filter chip at fx); measured and the
            // chip start (fx) derived from the real label width instead of a fixed offset.
            var filterLabel = UiDraw.Text(filter, "Label", inventoryKnown ? UiDraw.L("appearance.filter.filterLabel", "Filter:") : UiDraw.L("appearance.filter.inventoryLabel", "Inventory:"), UiFont.Mono, CaptionDp, OnVariant, .1f);
            float filterLabelWidth = UiDraw.Width(filterLabel) + 4f;
            UiDraw.Place(filterLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(filterLabelWidth, 14f));
            filterRowElements.Add(filterLabel.rectTransform);
            float fx = 14f + filterLabelWidth + 6f;
            if (!inventoryKnown)
            {
                var inventoryStatus = UiDraw.Text(filter, "InventoryStatus", inventoryUnavailable ? UiDraw.L("appearance.filter.unavailable", "Unavailable") : UiDraw.L("appearance.filter.syncing", "Syncing"),
                    UiFont.Mono, CaptionDp, inventoryUnavailable ? Tertiary : OnVariant, .08f, TextAlignmentOptions.Midline);
                UiDraw.Place(inventoryStatus.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(150f, 24f));
                filterRowElements.Add(inventoryStatus.rectTransform);
            }
            else
                foreach (var (text, on) in new[] { (string.Format(UiDraw.L("appearance.filter.inCategoryTemplate", "In Category ({0})"), owned.Length), true), (string.Format(UiDraw.L("appearance.filter.ownedTemplate", "Owned ({0})"), ownedAll), false) })
                {
                    var t = UiDraw.Text(filter, "Filter", text, UiFont.Mono, CaptionDp, on ? Primary : OnVariant, .1f, TextAlignmentOptions.Midline);
                    float fw = UiDraw.Width(t) + 18f;
                    var p = UiDraw.Box(filter, "FilterPlate", on ? High : Low, 4f);
                    p.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
                    UiDraw.Place(p.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                    UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                    filterRowElements.Add(p.rectTransform);
                    filterRowElements.Add(t.rectTransform);
                    fx += fw + 8f;
                }
            // The nominal 200-wide box undersized this copy (reproduced at 1920 and 2400, drawing
            // well past its own box with NoWrap + Overflow); measured up front so both the actual
            // placed width and the row-wrap threshold below reflect the real text.
            var shop = UiDraw.Text(filter, "Shop", UiDraw.L("appearance.filter.shopNotice", "NO PRICES • EARNED ONLY"), UiFont.Mono, CaptionDp, Tertiary, .1f);
            float shopWidth = UiDraw.Width(shop) + 4f;
            bool wrapShopNotice = fx > w - (shopWidth + 28f);
            float filterHeight = wrapShopNotice ? 64f : 40f;
            if (wrapShopNotice)
            {
                filter.sizeDelta = new Vector2(filter.sizeDelta.x, filterHeight);
                foreach (RectTransform rowElement in filterRowElements)
                    rowElement.anchoredPosition += Vector2.up * 14f;
            }
            shop.alignment = wrapShopNotice ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            UiDraw.Place(shop.rectTransform,
                wrapShopNotice ? new Vector2(0f, .5f) : new Vector2(1f, .5f),
                wrapShopNotice ? new Vector2(0f, .5f) : new Vector2(1f, .5f),
                wrapShopNotice ? new Vector2(14f, -16f) : new Vector2(-14f, 0f),
                new Vector2(shopWidth, 14f));
            cy += filterHeight + 12f;
            var list = UiDraw.Node(page, "Library");
            TL(list, x, cy, w, 10f);
            if (owned.Length == 0)
            {
                var empty = UiDraw.Box(list, "Empty", Low, 12f);
                bool wrapGuide = category == "Weapon Wrap" && inventoryKnown;
                float emptyHeight = wrapGuide ? 138f : 112f;
                TL(empty.rectTransform, 0f, 0f, w, emptyHeight);
                string title = inventoryUnavailable ? UiDraw.L("appearance.library.inventoryUnavailable", "Inventory Unavailable")
                    : items == null ? UiDraw.L("appearance.library.loadingInventory", "Loading Inventory")
                    : wrapGuide ? UiDraw.L("appearance.library.howToEarnWrap", "How to earn a weapon wrap") : UiDraw.L("appearance.library.noOwnedItems", "No owned items in this category");
                TL(UiDraw.Text(empty.transform, "Title", title, UiFont.Heading, UiDp, OnSurface, .03f).rectTransform, 20f, 16f, w - 40f, 22f);
                string detailCopy = inventoryUnavailable ? UiDraw.L("appearance.library.detailUnavailable", "Your saved items could not be checked. Retry to confirm ownership.")
                    : items == null ? UiDraw.L("appearance.library.detailChecking", "Checking your server-confirmed inventory…")
                    : wrapGuide ? UiDraw.L("appearance.library.detailWrapGuide", "Earn wraps through eligible public-match progress.")
                    : UiDraw.L("appearance.library.detailDefault", "Only owned, server-confirmed cosmetics appear here. Your standard pilot look is always available.");
                var detail = UiDraw.Text(empty.transform, "Detail", detailCopy, UiFont.Body, CaptionDp, OnVariant);
                detail.textWrappingMode = TextWrappingModes.Normal; detail.alignment = TextAlignmentOptions.TopLeft;
                TL(detail.rectTransform, 20f, 46f, w - 40f, wrapGuide ? 28f : 48f);
                if (wrapGuide)
                {
                    var milestones = (wrapUnlocks ?? Array.Empty<WrapUnlockDto>())
                        .Where(u => u != null).OrderBy(u => u.level).ToArray();
                    var next = currentLevel > 0 ? milestones.FirstOrDefault(u => u.level > currentLevel) : null;
                    string path = next != null
                        ? string.Format(UiDraw.L("appearance.library.nextWrapTemplate", "Next server-configured wrap: {0} at level {1}."), WrapName(next.cosmeticId), next.level)
                        : currentLevel <= 0 && milestones.Length > 0
                            ? UiDraw.L("appearance.library.milestonesPrefix", "Server-configured milestones: ") + string.Join(" · ", milestones.Select(u => string.Format(UiDraw.L("appearance.library.milestoneTemplate", "{0} — level {1}"), WrapName(u.cosmeticId), u.level))) + "."
                            : milestones.Length == 0
                                ? UiDraw.L("appearance.library.noMilestones", "No weapon-wrap level rewards are configured right now.")
                                : UiDraw.L("appearance.library.allMilestonesReached", "You have reached the last configured wrap milestone. No later wrap reward is listed.");
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
                    var stateText = UiDraw.Text(r, "State", active ? UiDraw.L("appearance.item.equipped", "Equipped") : UiDraw.L("appearance.item.owned", "Owned"), UiFont.Mono, CaptionDp, active ? Ink : OnVariant, .05f, TextAlignmentOptions.Midline);
                    float sw = UiDraw.Width(stateText) + 10f;
                    var statePlate = UiDraw.Box(r, "StatePlate", active ? PrimaryC : Highest, 2f);
                    statePlate.transform.SetSiblingIndex(stateText.transform.GetSiblingIndex());
                    TL(statePlate.rectTransform, 14f, 14f, sw, 18f); TL(stateText.rectTransform, 14f, 14f, sw, 18f);
                    // The nominal 220-wide box undersized this template at this tracking/size
                    // (reproduced at 1920 and 2400); measured instead of assumed.
                    var slotText = UiDraw.Text(r, "Slot", string.Format(UiDraw.L("appearance.item.slotTemplate", "SLOT  ·  {0}"), SlotName(category)), UiFont.Mono, CaptionDp, OnVariant, .1f);
                    TL(slotText.rectTransform, 20f + sw, 15f, UiDraw.Width(slotText) + 4f, 16f);
                    var thumb = UiDraw.Box(r, "Thumb", Lowest, 4f);
                    TL(thumb.rectTransform, 14f, 42f, 64f, 60f);
                    UiDraw.Fill(UiDraw.Icon(thumb.transform, "Icon", category == "Weapon Wrap" ? "colorize" : category == "Badge" ? "military_tech" : "accessibility_new", 28f, Primary).rectTransform);
                    // Owned items whose longer catalog name actually reaches this width (reproduced
                    // with "Weapon wrap: Midnight carbon fiber finish"): at w-260 the box's own
                    // right edge already lands 20 units past the right-anchored Preview button's
                    // left edge, and the default Overflow mode let the glyphs run further still,
                    // into PREVIEW/EQUIP or the ACTIVE label. Narrowed to end before Preview, and
                    // measured/trimmed up front instead of using TMP's Ellipsis overflow mode,
                    // which was tried here first and found to render the whole label blank in this
                    // project's batch PlayMode captures (see UiDraw.Truncate).
                    float nameW = w - 300f;
                    string nameValue = CosmeticLook.DisplayName(item.cosmeticId);
                    var nameText = UiDraw.Text(r, "Name", nameValue, UiFont.BodySemiBold, UiDp, OnSurface);
                    nameText.text = UiDraw.Truncate(nameText, nameValue, nameW);
                    TL(nameText.rectTransform, 92f, 48f, nameW, 24f);
                    string sourceValue = Source(item.acquiredVia);
                    var sourceText = UiDraw.Text(r, "Source", sourceValue, UiFont.Mono, CaptionDp, Primary, .05f);
                    sourceText.text = UiDraw.Truncate(sourceText, sourceValue, nameW);
                    TL(sourceText.rectTransform, 92f, 74f, nameW, 16f);
                    if (active)
                    {
                        UiDraw.Place(UiDraw.Icon(r, "Check", "check_circle", 20f, Primary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -24f), new Vector2(20f, 20f));
                        UiDraw.Place(UiDraw.Text(r, "Active", UiDraw.L("appearance.item.active", "Active"), UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                            new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -8f), new Vector2(100f, 16f));
                    }
                    else
                    {
                        var preview = UiDraw.Tap(r, "Preview", High, 6f, () => inspect?.Invoke(item));
                        var previewLabel = UiDraw.Text(preview.transform, "Label", UiDraw.L("appearance.item.preview", "Preview"), UiFont.Mono, CaptionDp, Primary, .05f, TextAlignmentOptions.Midline);
                        // The nominal 84-wide button undersized "PREVIEW" at this tracking/size
                        // (reproduced at 1920 and 2400); the pivot stays right-anchored at -104, so
                        // widening only grows the button leftward, away from Equip.
                        float previewWidth = Mathf.Max(84f, UiDraw.Width(previewLabel) + 24f);
                        UiDraw.Place((RectTransform)preview.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-104f, -10f), new Vector2(previewWidth, 32f));
                        UiDraw.Fill(previewLabel.rectTransform);
                        var equipButton = UiDraw.Tap(r, "Equip", PrimaryC, 6f, () => equip?.Invoke(item));
                        UiDraw.Place((RectTransform)equipButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -10f), new Vector2(80f, 32f));
                        UiDraw.Fill(UiDraw.Text(equipButton.transform, "Label", UiDraw.L("appearance.item.equip", "Equip"), UiFont.Mono, CaptionDp, Ink, .05f, TextAlignmentOptions.Midline).rectTransform);
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
            TL(UiDraw.Text(card, "Heading", expanded ? UiDraw.L("appearance.spec.headingExpanded", "Technical Details") : UiDraw.L("appearance.spec.headingCollapsed", "Equipped Appearance"), UiFont.Heading, CaptionDp, Primary, .05f).rectTransform, 16f, 18f, w - 220f, 16f);
            var saved = UiDraw.Box(card, "Saved", Mid, 4f);
            var savedText = UiDraw.Text(saved.transform, "Text", UiDraw.L("appearance.spec.saved", "Saved to Your Profile"), UiFont.Mono, CaptionDp, OnSurface, .1f);
            float svw = 10f + 6f + 6f + UiDraw.Width(savedText) + 10f;
            UiDraw.Place(saved.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -26f), new Vector2(svw, 22f));
            UiDraw.Place(UiDraw.Dot(saved.transform, "Dot", 6f, PrimaryC).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(6f, 6f));
            UiDraw.Place(savedText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f, 0f), new Vector2(UiDraw.Width(savedText) + 2f, 14f));
            string name = string.IsNullOrEmpty(equippedBody) ? UiDraw.L("appearance.spec.nameDefault", "Standard Flight Utility") : CosmeticLook.DisplayName(equippedBody);
            TL(UiDraw.Text(card, "Name", name, UiFont.HeadingSemiBold, UiDp, OnSurface).rectTransform, 16f, 42f, w - 32f, 30f);
            if (expanded)
            {
                var stats = UiDraw.Box(card, "Stats", Mid, 8f);
                TL(stats.rectTransform, 16f, 116f, w - 32f, 64f);
                float sw = (w - 32f) / 3f;
                string unchanged = UiDraw.L("appearance.spec.stat.unchanged", "Unchanged");
                var entries = new[]
                {
                    (UiDraw.L("appearance.spec.stat.hitbox", "Hitbox"), unchanged),
                    (UiDraw.L("appearance.spec.stat.speed", "Speed"), unchanged),
                    (UiDraw.L("appearance.spec.stat.sound", "Sound"), unchanged),
                };
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
                UiDraw.Place(UiDraw.Text(source.transform, "Text", UiDraw.L("appearance.spec.source", "Default look  ·  always owned"), UiFont.Mono, CaptionDp, OnSurface, .05f).rectTransform,
                    new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(w - 80f, 16f));
            }
            var disclosure = UiDraw.Tap(card, "TechSpecsDisclosure", High, 8f, toggleSpecs);
            TL((RectTransform)disclosure.transform, 16f, expanded ? 228f : 116f, w - 32f, 48f);
            UiDraw.Fill(UiDraw.Text(disclosure.transform, "Label", expanded ? UiDraw.L("appearance.spec.hide", "Hide Technical Details") : UiDraw.L("appearance.spec.view", "View Technical Details"), UiFont.HeadingSemiBold, UiDp, Primary, .01f, TextAlignmentOptions.Midline).rectTransform, 12f, 4f, 12f, 4f);
            float actionY = expanded ? 288f : 176f;
            var button = UiDraw.Tap(card, "ChangeRunner", PrimaryC, 8f, () => { GameAudio.Play2D(GameCue.UiTap); changeRunner?.Invoke(); });
            TL((RectTransform)button.transform, 16f, actionY, w - 32f - 60f - 12f, 60f);
            var label = UiDraw.Text(button.transform, "Label", UiDraw.L("appearance.spec.changeRunner", "Change Runner"), UiFont.HeadingSemiBold, UiDp, Ink, .02f);
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
            var backText = UiDraw.Text(backButton.transform, "Label", UiDraw.L("appearance.footer.back", "Back"), UiFont.Heading, UiDp, OnSurface, .01f);
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
                var againText = UiDraw.Text(again.transform, "Label", UiDraw.L("common.retry", "Retry"), UiFont.Heading, UiDp, Primary, .01f);
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
            "Weapon Wrap" => UiDraw.L("appearance.slot.arcRifle", "Arc Rifle"),
            "Badge" => UiDraw.L("appearance.slot.flightCard", "Flight Card"),
            "Body Style" => UiDraw.L("appearance.slot.bodyStyle", "Body Style"),
            _ => UiDraw.L("appearance.slot.bodySuit", "Body Suit"),
        };

        static string Category(string id) => string.IsNullOrEmpty(id) || id == "body_standard"
            ? "Body Presentation" : id.StartsWith("body_", StringComparison.Ordinal) ? "Body Style"
            : id.StartsWith("wrap_", StringComparison.Ordinal) ? "Weapon Wrap"
            : id.StartsWith("badge_", StringComparison.Ordinal) ? "Badge" : "Body Presentation";
        static string WrapName(string id)
            => CosmeticLook.ItemName(id);
        static string Source(string via) => via == "starter" ? UiDraw.L("appearance.source.starter", "Default Issue  •  Starter Collection")
            : via == "unlock" ? UiDraw.L("appearance.source.unlock", "Match Unlock  •  Earned") : string.IsNullOrEmpty(via) ? UiDraw.L("appearance.source.unavailable", "Source unavailable") : via;
    }
}
