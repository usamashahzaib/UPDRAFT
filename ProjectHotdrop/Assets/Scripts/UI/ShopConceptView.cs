using System;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>Isolated visual concept. No billing, offers, prices or entitlements are exposed.</summary>
    public static class ShopConceptView
    {
        static string L(string key, string fallback) => UiDraw.L("shop." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("shop." + key, fallback, args);
        static Color Ink => UIDesignTokens.Background;
        static Color Card => UIDesignTokens.SurfacePanel;
        static Color Raised => UIDesignTokens.SurfaceSelected;
        static Color Cyan => UIDesignTokens.PrimaryAction;
        static Color Muted => UIDesignTokens.TextSecondary;

        public static void Show(GameObject owner, Action back)
        {
            if (owner == null) return;
            var old = owner.transform.Find("ShopConceptVisual");
            if (old != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject);
                else UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var root = Rect(owner.transform, "ShopConceptVisual", 0, 0, 1, 1);
            Fill(root, Ink);
            var header = Rect(root, "Header", 0, .925f, 1, 1);
            Fill(header, UIDesignTokens.Background);
            Label(header, "Brand", L("brand", "HOTDROP"), .025f, .16f, .18f, .84f, 24, Color.white);
            Label(header, "Page", L("header.title", "Cosmetics"), .2f, .15f, .7f, .85f, 21, Cyan);
            Label(header, "State", L("header.preview", "Preview"), .75f, .15f, .975f, .85f,
                13, Muted, TextAlignmentOptions.MidlineRight);
            var guard = Panel(root, "Guard", 0, .837f, 1, .915f, Raised);
            Label(guard, "Text", L("guard", "Preview  ·  Purchases are unavailable in this build."),
                .03f, .13f, .97f, .87f, 16, Color.white);
            Label(root, "Kicker", L("kicker", "Permanent Cosmetics"), .025f, .79f,
                .95f, .83f, 14, Cyan);
            Label(root, "Title", L("title", "Own it forever. No duplicates."), .025f,
                .727f, .975f, .79f, 29, Color.white);
            var principles = Panel(root, "Principles", .025f, .674f, .975f, .722f, Card);
            Label(principles, "Text", L("principles", "100% COSMETIC ONLY     ·     ZERO RANDOMNESS     ·     NO TIMERS     ·     NO POWER MODIFIERS"),
                .025f, .1f, .975f, .9f, 14, Cyan);
            string[] titles =
            {
                L("bundle.signal.title", "Signal Bundle"), L("bundle.foundry.title", "Foundry Bundle"),
                L("bundle.skyline.title", "Skyline Bundle")
            };
            string[] themes =
            {
                L("bundle.signal.theme", "Tactical Recon"), L("bundle.foundry.theme", "Heavy Utility"),
                L("bundle.skyline.theme", "Aero Monarch")
            };
            string[][] contents =
            {
                new[] { L("bundle.signal.item.suit", "Flight suit concept"), L("bundle.signal.item.wrap", "ARC rifle wrap concept"), L("bundle.signal.item.badge", "Pilot badge concept") },
                new[] { L("bundle.foundry.item.chassis", "Industrial chassis concept"), L("bundle.foundry.item.wrap", "ZIP SMG wrap concept"), L("bundle.foundry.item.pad", "Launch pad FX concept") },
                new[] { L("bundle.skyline.item.suit", "Apex flight suit concept"), L("bundle.skyline.item.wrap", "BRICK wrap concept"), L("bundle.skyline.item.effects", "Jump pad FX concept") }
            };
            for (int i = 0; i < 3; i++)
            {
                float left = .025f + i * .324f;
                var card = Panel(root, "Bundle_" + i, left, .168f, left + .306f, .658f, Card);
                var art = Rect(card, "ConceptArt", .015f, .55f, .985f, .95f)
                    .gameObject.AddComponent<RawImage>();
                art.texture = UiPresentationProfile.LoadRooftopArtwork();
                art.color = i == 1 ? new Color(.82f, .58f, .39f, .85f)
                    : i == 2 ? new Color(.35f, .7f, .9f, .9f) : Color.white;
                art.raycastTarget = false;
                Label(card, "Spec", F("conceptTemplate", "Concept {0}", i + 1), .04f, .93f,
                    .95f, .99f, 12, Cyan);
                Label(card, "Theme", themes[i], .04f, .46f, .95f, .54f, 13, Muted);
                Label(card, "Name", titles[i], .04f, .36f, .95f, .46f, 25, Color.white);
                Label(card, "Includes", L("includes", "Concept Assets"), .04f, .31f, .95f, .36f,
                    13, Cyan);
                for (int j = 0; j < 3; j++)
                    Label(card, "Item_" + j, F("itemTemplate", "•  {0}", contents[i][j]), .04f,
                        .24f - j * .06f, .95f, .31f - j * .06f, 13, Muted);
                var unavailable = Panel(card, "Unavailable", .04f, .018f, .96f, .095f, Raised);
                Label(unavailable, "Caption", L("unavailable", "Unavailable in This Build"), .02f, .02f, .98f,
                    .98f, 15, Muted, TextAlignmentOptions.Center);
            }
            var transparency = Panel(root, "Transparency", .025f, .095f, .975f, .153f, Card);
            Label(transparency, "Policy", L("transparency", "Transparency: billing products, localized prices, ownership and restore are shown only when backed by the store."),
                .025f, .1f, .975f, .9f, 14, Muted);
            var footer = Rect(root, "Footer", 0, 0, 1, .08f);
            Fill(footer, UIDesignTokens.Background);
            Button(footer, "Back", L("backHome", "Return Home"), .025f, .17f, .21f, .83f,
                Raised, Color.white, back);
            Label(footer, "State", L("footer.state", "CONCEPT ONLY  ·  NO PURCHASE ACTIONS"), .25f, .18f,
                .975f, .82f, 13, Muted);
        }
        static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        static RectTransform Panel(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var rect = Rect(parent, name, x0, y0, x1, y1);
            var image = Fill(rect, color); image.sprite = UIRoundedSprites.RoundedRect(9);
            image.type = Image.Type.Sliced; return rect;
        }
        static void Label(Transform parent, string name, string value, float x0, float y0, float x1, float y1,
            float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false); label.rectTransform.anchorMin = new Vector2(x0, y0);
            label.rectTransform.anchorMax = new Vector2(x1, y1);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = value; label.fontSize = UiMetrics.ScaleTextSize(size);
            label.color = color == Color.white ? UIDesignTokens.TextPrimary : color;
            label.alignment = align;
            label.enableAutoSizing = true; label.fontSizeMin = UiMetrics.ScaleTextSize(10); label.fontSizeMax = UiMetrics.ScaleTextSize(size);
            label.enableWordWrapping = false; label.raycastTarget = false;
        }
        static void Button(Transform parent, string name, string caption, float x0, float y0, float x1, float y1,
            Color background, Color foreground, Action action)
        {
            var rect = Panel(parent, name, x0, y0, x1, y1, background);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            button.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); action?.Invoke(); });
            Label(rect, "Caption", caption, .02f, .02f, .98f, .98f, 15, foreground,
                TextAlignmentOptions.Center);
        }
    }
}
