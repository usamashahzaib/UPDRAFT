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
        static Color Lowest => UiDraw.Hex("#060e1d");
        static Color Low => UiDraw.Hex("#141c2b");
        static Color Mid => UiDraw.Hex("#18202f");
        static Color High => UiDraw.Hex("#222a3a");
        static Color Highest => UiDraw.Hex("#2d3545");
        static Color Primary => UiDraw.Hex("#7cefff");
        static Color PrimaryC => UiDraw.Hex("#30d5e8");
        static Color Tertiary => UiDraw.Hex("#ffda88");
        static Color OnSurface => UiDraw.Hex("#dbe2f8");
        static Color OnVariant => UiDraw.Hex("#bbc9cb");
        static Color Outline => UiDraw.Hex("#859396");
        static Color Ink => UiDraw.Hex("#101827");

        const float Pad = 24f;

        public static void SetStatus(GameObject owner, string value)
        {
            var label = owner != null ? owner.transform.Find("AppearanceLockerVisual/Footer/Status/Message")?.GetComponent<TextMeshProUGUI>() : null;
            if (label != null) label.text = value ?? "";
        }

        public static void Show(GameObject owner, RawImage preview, InventoryItemDto[] items,
            string category, string equippedBody, string equippedWrap, string equippedBadge,
            string state, Action back, Action changeRunner, Action<string> changeCategory,
            Action<InventoryItemDto> inspect, Action<InventoryItemDto> equip)
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

            float y = 16f;
            y = Header(page, w, y) + 16f;
            float leftW = (w - 20f) * .585f, rightW = w - 20f - leftW, rightX = Pad + leftW + 20f;
            float ly = Stage(page, Pad, y, leftW, preview, equippedWrap) + 16f;
            ly = FairPlay(page, Pad, ly, leftW) + 16f;
            float ry = Library(page, rightX, y, rightW, items, category, equippedBody, equippedWrap, equippedBadge, changeCategory, inspect, equip) + 16f;
            ry = Spec(page, rightX, ry, rightW, equippedBody, changeRunner) + 16f;
            page.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);
            page.anchoredPosition = Vector2.zero;
            Footer(root, state, back);
        }

        static float Header(RectTransform page, float w, float y)
        {
            var card = Card(page, "Introduction", Low, Pad, y, w, 72f);
            TL(UiDraw.Box(card, "Accent", PrimaryC, 3f).rectTransform, 16f, 20f, 6f, 32f);
            var title = UiDraw.Text(card, "Title", "APPEARANCE // TACTICAL LOCKER", UiFont.Heading, 24f, OnSurface, -.03f);
            float tw = UiDraw.Width(title);
            TL(title.rectTransform, 36f, 12f, tw + 4f, 30f);
            var chip = UiDraw.Text(card, "Registry", "COSMETIC ONLY", UiFont.Mono, 11f, Primary, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chip) + 12f;
            var chipPlate = UiDraw.Box(card, "RegistryPlate", High, 4f);
            chipPlate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            TL(chipPlate.rectTransform, 36f + tw + 8f, 17f, cw, 20f); TL(chip.rectTransform, 36f + tw + 8f, 17f, cw, 20f);
            TL(UiDraw.Icon(card, "Shield", "verified_user", 14f, Primary).rectTransform, 36f, 46f, 14f, 14f);
            TL(UiDraw.Text(card, "Rule", "Cosmetics strictly non-mechanical  •  100% identical silhouette, hitboxes, recoil recovery and sound.", UiFont.Body, 12f, OnVariant).rectTransform, 56f, 44f, w - 520f, 16f);
            float x = -16f;
            foreach (var (name, icon, label, value) in new[] { ("Acoustics", "graphic_eq", "ACOUSTICS: ", "STANDARDIZED"), ("Silhouette", "tune", "SILHOUETTE: ", "MATCHED 1:1") })
            {
                var plate = UiDraw.Box(card, name, Mid, 8f);
                var text = UiDraw.Text(plate.transform, "Text", label + $"<color=#7cefff>{value}</color>", UiFont.Mono, 11f, OnVariant, .1f);
                float pw = 12f + 16f + 6f + UiDraw.Width(text) + 12f;
                UiDraw.Place(plate.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(x, 0f), new Vector2(pw, 30f));
                UiDraw.Place(UiDraw.Icon(plate.transform, "Icon", icon, 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
                UiDraw.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(34f, 0f), new Vector2(UiDraw.Width(text) + 2f, 14f));
                x -= pw + 12f;
            }
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
            UiDraw.VerticalGradient(card, "Shade", UiDraw.Hex("#060e1d", .85f), UiDraw.Hex("#060e1d", .15f), UiDraw.Hex("#060e1d", .5f));
            // Turntable ring under the operative.
            var turntable = UiDraw.Ring(card, "Turntable", 300f, 2f, UiDraw.Hex("#30d5e8", .45f), 40);
            UiDraw.Place(turntable.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 96f), new Vector2(300f, 300f));
            turntable.rectTransform.localScale = new Vector3(1f, .28f, 1f);
            var previewHost = UiDraw.Node(card, "OperativePreview");
            UiDraw.Place(previewHost, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 80f), new Vector2(h * .62f, h * .78f));
            if (preview != null)
            {
                preview.transform.SetParent(previewHost, false);
                UiDraw.Fill(preview.rectTransform);
            }
            Chip(card, "StageName", "", "STAGE // SWITCHYARD ROOFTOP", new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(16f, -24f), true);
            Chip(card, "Lighting", "wb_sunny", "DAYLIGHT // OPERATIVE PREVIEW", new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -24f), false);
            // Equipment plate.
            var plate = UiDraw.Box(card, "Equipped", UiDraw.Hex("#060e1d", .85f), 8f);
            UiDraw.Place(plate.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 150f), new Vector2(Mathf.Min(w - 80f, 560f), 58f));
            TL(UiDraw.Text(plate.transform, "WeaponLabel", "CURRENT ARMED WEAPON", UiFont.Mono, 11f, OnVariant, .1f).rectTransform, 18f, 8f, 260f, 14f);
            TL(UiDraw.Text(plate.transform, "Weapon", "ARC PRECISION RIFLE", UiFont.Heading, 22f, Primary, -.02f).rectTransform, 18f, 24f, 320f, 28f);
            var wrapLabel = UiDraw.Text(plate.transform, "WrapLabel", "ACTIVE WRAP", UiFont.Mono, 11f, OnVariant, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(wrapLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-18f, -15f), new Vector2(200f, 14f));
            var wrap = UiDraw.Text(plate.transform, "Wrap", string.IsNullOrEmpty(equippedWrap) ? "STANDARD" : CosmeticLook.DisplayName(equippedWrap).ToUpperInvariant(), UiFont.Mono, 14f, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(wrap.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-18f, -37f), new Vector2(220f, 18f));
            Chip(card, "Drag", "360", "DRAG TO ROTATE OPERATIVE", new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 28f), false);
            return y + h;
        }

        static void Chip(RectTransform card, string name, string icon, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, bool dot)
        {
            var plate = UiDraw.Box(card, name, UiDraw.Hex("#060e1d", .8f), 8f);
            var label = UiDraw.Text(plate.transform, "Text", text, UiFont.Mono, 12f, OnSurface, .1f);
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
            TL(UiDraw.Text(card, "Title", "FAIR PLAY REQUISITIONS", UiFont.Heading, 18f, OnSurface, -.01f).rectTransform, 76f, 14f, w - 260f, 24f);
            var body = UiDraw.Text(card, "Rule", "HOTDROP locker unlocks are earned through matches and mastery. Visual items grant no gameplay advantage.", UiFont.Body, 12f, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            TL(body.rectTransform, 76f, 40f, w - 260f, 32f);
            var tag = UiDraw.Text(card, "Tag", "<color=#ffda88>NO\nLOOTBOXES</color>\nNO COMBAT\nBUFFS", UiFont.Mono, 11f, OnVariant, .1f);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(110f, 60f));
            return y + 80f;
        }

        static float Library(RectTransform page, float x, float y, float w, InventoryItemDto[] items, string category,
            string equippedBody, string equippedWrap, string equippedBadge, Action<string> changeCategory,
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
                UiDraw.Place(UiDraw.Text(tab.transform, "Label", captions[i], UiFont.Heading, 12f, on ? Primary : OnSurface, .05f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -14f), new Vector2(tw - 8f, 16f));
            }
            float cy = y + 72f + 12f;
            var owned = (items ?? Array.Empty<InventoryItemDto>()).Where(i => i != null && Category(i.cosmeticId) == category).ToArray();
            int ownedAll = items?.Length ?? 0;
            var filter = Card(page, "Filter", Mid, x, cy, w, 40f);
            UiDraw.Place(UiDraw.Text(filter, "Label", "FILTER:", UiFont.Mono, 11f, OnVariant, .1f).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(70f, 14f));
            float fx = 84f;
            foreach (var (text, on) in new[] { ($"IN CATEGORY ({owned.Length})", true), ($"OWNED ({ownedAll})", false) })
            {
                var t = UiDraw.Text(filter, "Filter", text, UiFont.Mono, 11f, on ? Primary : OnVariant, .1f, TextAlignmentOptions.Midline);
                float fw = UiDraw.Width(t) + 18f;
                var p = UiDraw.Box(filter, "FilterPlate", on ? High : Low, 4f);
                p.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
                UiDraw.Place(p.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(fx, 0f), new Vector2(fw, 24f));
                fx += fw + 8f;
            }
            UiDraw.Place(UiDraw.Text(filter, "Shop", "NO PRICES • EARNED ONLY", UiFont.Mono, 11f, Tertiary, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, 0f), new Vector2(200f, 14f));
            cy += 40f + 12f;
            var list = UiDraw.Node(page, "Library");
            TL(list, x, cy, w, 10f);
            if (owned.Length == 0)
            {
                var empty = UiDraw.Box(list, "Empty", Low, 12f);
                TL(empty.rectTransform, 0f, 0f, w, 112f);
                TL(UiDraw.Text(empty.transform, "Title", items == null ? "LOADING INVENTORY" : "NO OWNED ITEMS IN THIS CATEGORY", UiFont.Heading, 16f, OnSurface, .03f).rectTransform, 20f, 22f, w - 40f, 22f);
                var detail = UiDraw.Text(empty.transform, "Detail", items == null ? "Checking your account inventory…"
                    : "Only owned, server-confirmed cosmetics appear here. Your standard operative remains available.", UiFont.Body, 12f, OnVariant);
                detail.textWrappingMode = TextWrappingModes.Normal; detail.alignment = TextAlignmentOptions.TopLeft;
                TL(detail.rectTransform, 20f, 52f, w - 40f, 40f);
                cy += 112f;
            }
            else
            {
                float ly = 0f;
                foreach (var item in owned.Take(4))
                {
                    string activeId = category == "Weapon Wrap" ? equippedWrap : category == "Badge" ? equippedBadge : equippedBody;
                    bool active = activeId == item.cosmeticId || (string.IsNullOrEmpty(activeId) && item.cosmeticId == "body_standard");
                    var row = UiDraw.Tap(list, "Item_" + item.id, Low, 12f, () => inspect?.Invoke(item), active ? UiDraw.Hex("#30d5e8", .35f) : (Color?)null);
                    TL((RectTransform)row.transform, 0f, ly, w, 116f);
                    var r = (RectTransform)row.transform;
                    var stateText = UiDraw.Text(r, "State", active ? "EQUIPPED" : "OWNED", UiFont.Mono, 11f, active ? UiDraw.Hex("#00363c") : OnVariant, .05f, TextAlignmentOptions.Midline);
                    float sw = UiDraw.Width(stateText) + 10f;
                    var statePlate = UiDraw.Box(r, "StatePlate", active ? PrimaryC : Highest, 2f);
                    statePlate.transform.SetSiblingIndex(stateText.transform.GetSiblingIndex());
                    TL(statePlate.rectTransform, 14f, 14f, sw, 18f); TL(stateText.rectTransform, 14f, 14f, sw, 18f);
                    TL(UiDraw.Text(r, "Slot", $"SLOT // {SlotName(category)}", UiFont.Mono, 11f, OnVariant, .1f).rectTransform, 20f + sw, 15f, 220f, 16f);
                    var thumb = UiDraw.Box(r, "Thumb", Lowest, 4f);
                    TL(thumb.rectTransform, 14f, 42f, 64f, 60f);
                    UiDraw.Fill(UiDraw.Icon(thumb.transform, "Icon", category == "Weapon Wrap" ? "colorize" : category == "Badge" ? "military_tech" : "accessibility_new", 28f, Primary).rectTransform);
                    TL(UiDraw.Text(r, "Name", CosmeticLook.DisplayName(item.cosmeticId), UiFont.BodySemiBold, 18f, OnSurface).rectTransform, 92f, 48f, w - 260f, 24f);
                    TL(UiDraw.Text(r, "Source", Source(item.acquiredVia), UiFont.Mono, 12f, Primary, .05f).rectTransform, 92f, 74f, w - 260f, 16f);
                    if (active)
                    {
                        UiDraw.Place(UiDraw.Icon(r, "Check", "check_circle", 20f, Primary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -24f), new Vector2(20f, 20f));
                        UiDraw.Place(UiDraw.Text(r, "Active", "ACTIVE", UiFont.Mono, 12f, Primary, .1f, TextAlignmentOptions.MidlineRight).rectTransform,
                            new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -8f), new Vector2(100f, 16f));
                    }
                    else
                    {
                        var preview = UiDraw.Tap(r, "Preview", High, 6f, () => inspect?.Invoke(item));
                        UiDraw.Place((RectTransform)preview.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-104f, -10f), new Vector2(84f, 32f));
                        UiDraw.Fill(UiDraw.Text(preview.transform, "Label", "PREVIEW", UiFont.Mono, 11f, Primary, .05f, TextAlignmentOptions.Midline).rectTransform);
                        var equipButton = UiDraw.Tap(r, "Equip", PrimaryC, 6f, () => equip?.Invoke(item));
                        UiDraw.Place((RectTransform)equipButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, -10f), new Vector2(80f, 32f));
                        UiDraw.Fill(UiDraw.Text(equipButton.transform, "Label", "EQUIP", UiFont.Mono, 11f, Ink, .05f, TextAlignmentOptions.Midline).rectTransform);
                    }
                    ly += 116f + 12f;
                }
                cy += ly - 12f;
            }
            return cy;
        }

        static float Spec(RectTransform page, float x, float y, float w, string equippedBody, Action changeRunner)
        {
            var card = Card(page, "Telemetry", Low, x, y, w, 100f);
            TL(UiDraw.Text(card, "Heading", "TELEMETRY & ACQUISITION SPEC", UiFont.Heading, 12f, Primary, .05f).rectTransform, 16f, 18f, w - 220f, 16f);
            var saved = UiDraw.Box(card, "Saved", Mid, 4f);
            var savedText = UiDraw.Text(saved.transform, "Text", "SAVED TO OPERATIVE PROFILE", UiFont.Mono, 10f, OnSurface, .1f);
            float svw = 10f + 6f + 6f + UiDraw.Width(savedText) + 10f;
            UiDraw.Place(saved.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -26f), new Vector2(svw, 22f));
            UiDraw.Place(UiDraw.Dot(saved.transform, "Dot", 6f, PrimaryC).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(6f, 6f));
            UiDraw.Place(savedText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f, 0f), new Vector2(UiDraw.Width(savedText) + 2f, 14f));
            string name = string.IsNullOrEmpty(equippedBody) ? "Standard Flight Utility" : CosmeticLook.DisplayName(equippedBody);
            TL(UiDraw.Text(card, "Name", name, UiFont.HeadingSemiBold, 24f, OnSurface).rectTransform, 16f, 42f, w - 32f, 30f);
            var desc = UiDraw.Text(card, "Description", "Operative presentation only. Geometry and combat response remain matched across every cosmetic.", UiFont.Body, 12f, OnSurface);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            TL(desc.rectTransform, 16f, 76f, w - 32f, 32f);
            var stats = UiDraw.Box(card, "Stats", Mid, 8f);
            TL(stats.rectTransform, 16f, 116f, w - 32f, 64f);
            float sw = (w - 32f) / 3f;
            var entries = new[] { ("HITBOX DELTA", "0.00% (STABLE)"), ("VELOCITY BIAS", "0.00 M/S"), ("SOUND EMISSION", "MATCHED") };
            for (int i = 0; i < 3; i++)
            {
                UiDraw.Place(UiDraw.Text(stats.transform, "StatLabel" + i, entries[i].Item1, UiFont.Mono, 11f, OnVariant, .1f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(sw * (i + .5f), 10f), new Vector2(sw, 14f));
                UiDraw.Place(UiDraw.Text(stats.transform, "StatValue" + i, entries[i].Item2, UiFont.Mono, 14f, OnSurface, .05f, TextAlignmentOptions.Midline).rectTransform,
                    new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(sw * (i + .5f), -10f), new Vector2(sw, 18f));
            }
            var source = UiDraw.Box(card, "Source", Mid, 8f);
            TL(source.rectTransform, 16f, 188f, w - 32f, 32f);
            UiDraw.Place(UiDraw.Icon(source.transform, "Icon", "verified", 16f, Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(UiDraw.Text(source.transform, "Text", "Default Operational Requisition", UiFont.Mono, 12f, OnSurface, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(w - 80f, 16f));
            var button = UiDraw.Tap(card, "ChangeRunner", PrimaryC, 8f, () => { GameAudio.Play2D(GameCue.UiTap); changeRunner?.Invoke(); });
            TL((RectTransform)button.transform, 16f, 232f, w - 32f - 60f - 12f, 60f);
            var label = UiDraw.Text(button.transform, "Label", "CHANGE RUNNER", UiFont.HeadingSemiBold, 22f, UiDraw.Hex("#005862"), .02f);
            float lw = UiDraw.Width(label);
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", "check", 20f, UiDraw.Hex("#005862")).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 12f) / 2f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(16f, 0f), new Vector2(lw + 2f, 28f));
            var focus = UiDraw.Box(card, "Focus", High, 8f);
            TL(focus.rectTransform, w - 16f - 60f, 232f, 60f, 60f);
            UiDraw.Fill(UiDraw.Icon(focus.transform, "Icon", "center_focus_strong", 22f, OnSurface).rectTransform);
            card.sizeDelta = new Vector2(w, 308f);
            return y + 308f;
        }

        static void Footer(RectTransform root, string state, Action back)
        {
            var bar = UiDraw.Box(root, "Footer", UiDraw.Hex("#101827", .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 76f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UiDraw.Hex("#3c494b", .5f), 0f);
            var backButton = UiDraw.Tap(bar, "Back", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var backText = UiDraw.Text(backButton.transform, "Label", "BACK", UiFont.Heading, 22f, OnSurface, .01f);
            float bw = 20f + 20f + 12f + UiDraw.Width(backText) + 24f;
            UiDraw.Place((RectTransform)backButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 52f));
            UiDraw.Place(UiDraw.Icon(backButton.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 28f));
            var status = UiDraw.Node(bar, "Status");
            UiDraw.Place(status, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad + bw + 16f, 0f), new Vector2(800f, 20f));
            var msg = UiDraw.Text(status, "Message", state ?? "", UiFont.MonoMedium, 12f, OnVariant, .05f);
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
        static string Source(string via) => via == "starter" ? "Default Issue  •  Starter Collection"
            : via == "unlock" ? "Match Unlock  •  Earned" : string.IsNullOrEmpty(via) ? "Source unavailable" : via;
    }
}
