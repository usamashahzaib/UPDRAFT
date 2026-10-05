using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Hotdrop.UI
{
    /// <summary>The UIUX type families. Files live in Resources/Fonts (SIL OFL / Apache-2.0).</summary>
    public enum UiFont
    {
        HeadingMedium,   // Space Grotesk 500
        HeadingSemiBold, // Space Grotesk 600
        Heading,         // Space Grotesk 700 - display, section headers, label-caps
        Body,            // Hanken Grotesk 400
        BodySemiBold,    // Hanken Grotesk 600
        BodyBold,        // Hanken Grotesk 700
        MonoRegular,     // JetBrains Mono 400
        MonoMedium,      // JetBrains Mono 500
        Mono,            // JetBrains Mono 700 - data, timers, codes
        Icon,            // Material Symbols Outlined
        IconFilled,      // Material Symbols filled
    }

    /// <summary>
    /// Builds dynamic SDF font assets from the shipped TTFs on first use, so no baked atlas has to
    /// be regenerated when copy changes. Missing glyphs fall back to the TMP default font.
    /// </summary>
    public static class UIFonts
    {
        static readonly Dictionary<UiFont, TMP_FontAsset> cache = new Dictionary<UiFont, TMP_FontAsset>();
        static TMP_FontAsset shippedDefault;

        /// <summary>The TMP default before the UIUX body face replaced it (LiberationSans).</summary>
        static TMP_FontAsset ShippedDefault => shippedDefault != null ? shippedDefault : TMP_Settings.defaultFontAsset;

        /// <summary>
        /// Every label created without an explicit family gets DESIGN.md's operational face
        /// (Hanken Grotesk) instead of LiberationSans, so no screen falls back to the stock font.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallBodyAsDefault()
        {
            if (shippedDefault == null) shippedDefault = TMP_Settings.defaultFontAsset;
            var body = Get(UiFont.Body);
            if (body != null && body != shippedDefault) TMP_Settings.defaultFontAsset = body;
        }

        /// <summary>Puts the shipped default back (the editor calls this on leaving Play Mode).</summary>
        public static void RestoreShippedDefault()
        {
            if (shippedDefault != null) TMP_Settings.defaultFontAsset = shippedDefault;
        }

        static string FileFor(UiFont font) => font switch
        {
            UiFont.HeadingMedium => "SpaceGrotesk-Medium",
            UiFont.HeadingSemiBold => "SpaceGrotesk-SemiBold",
            UiFont.Heading => "SpaceGrotesk-Bold",
            UiFont.Body => "HankenGrotesk-Regular",
            UiFont.BodySemiBold => "HankenGrotesk-SemiBold",
            UiFont.BodyBold => "HankenGrotesk-Bold",
            UiFont.MonoRegular => "JetBrainsMono-Regular",
            UiFont.MonoMedium => "JetBrainsMono-Medium",
            UiFont.Mono => "JetBrainsMono-Bold",
            UiFont.Icon => "MaterialSymbols-Outlined",
            _ => "MaterialSymbols-Filled",
        };

        /// <summary>The font asset for a family, or the TMP default if the file is missing.</summary>
        public static TMP_FontAsset Get(UiFont font)
        {
            if (cache.TryGetValue(font, out var asset) && asset != null) return asset;
            asset = null;
            var source = Resources.Load<Font>("Fonts/" + FileFor(font));
            if (source != null)
            {
                asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                if (asset != null)
                {
                    asset.name = FileFor(font);
                    asset.hideFlags = HideFlags.DontSave;
                    var fallback = ShippedDefault;
                    if (fallback != null && font != UiFont.Icon && font != UiFont.IconFilled)
                        asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
                }
            }
            if (asset == null) asset = ShippedDefault;
            cache[font] = asset;
            return asset;
        }

        /// <summary>The family DESIGN.md assigns to each text role.</summary>
        public static UiFont ForRole(UiTextRole role) => role switch
        {
            UiTextRole.DisplayXLarge => UiFont.Heading,
            UiTextRole.DisplayLarge => UiFont.Heading,
            UiTextRole.TitleLarge => UiFont.Heading,
            UiTextRole.TitleMedium => UiFont.Heading,
            UiTextRole.BodyLarge => UiFont.BodySemiBold,
            UiTextRole.Body => UiFont.Body,
            UiTextRole.BodySmall => UiFont.Body,
            UiTextRole.Caption => UiFont.Heading,
            UiTextRole.DataLarge => UiFont.Mono,
            UiTextRole.DataMedium => UiFont.Mono,
            _ => UiFont.MonoMedium,
        };

        /// <summary>Sets a label's family and strips faux-bold, since the weights are real files.</summary>
        public static TMP_Text Use(TMP_Text text, UiFont font)
        {
            if (text == null) return null;
            text.font = Get(font);
            text.fontStyle &= ~FontStyles.Bold;
            return text;
        }
    }
}
