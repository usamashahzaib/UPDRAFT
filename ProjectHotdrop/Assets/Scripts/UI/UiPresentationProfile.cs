using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    public enum UiPresentationProfileId { StylizedFlight, GroundedTacticalFlight, Hybrid }

    /// <summary>Semantic UI and procedural-world colors for one player-selectable presentation.</summary>
    public sealed class UiPresentationPalette
    {
        public readonly Color Background, Surface, Selected, Structural, Disabled, Concrete;
        public readonly Color PrimaryAction, Ally, Enemy, Objective, Success, Warning;
        public readonly Color TextPrimary, TextSecondary, TextMuted;
        public readonly Color WorldCover, WorldEquipment, WorldFacade, WorldWindow, WorldSky, WorldAmbient, WorldSun;
        public readonly Color WorldConcrete, WorldStructure, WorldSafety;
        public readonly Color WorldZoneWest, WorldZoneCenter, WorldZoneEast;
        public readonly float WorldSunIntensity;
        public Color SurfaceSelected => Selected;
        public Color SurfaceStructural => Structural;

        // Derived semantic tones. Like SurfaceSelected and TextMuted they live on the
        // palette so a live profile switch can remap them with the rest of the semantic
        // palette; call sites must not re-derive them locally or the remap will miss
        // them and the previous profile's hue stays on screen.
        /// <summary>Pressed tint of the primary action role.</summary>
        public Color ActionPressed => Color.Lerp(PrimaryAction, Background, 0.28f);
        /// <summary>Pressed tint of the enemy/damage role.</summary>
        public Color HazardPressed => Color.Lerp(Enemy, Background, 0.28f);
        /// <summary>Half mix of the action and objective roles.</summary>
        public Color ActionObjectiveMix => Color.Lerp(PrimaryAction, Objective, 0.5f);
        /// <summary>Warning role dimmed toward the background, for armed/destructive gates.</summary>
        public Color WarningDim => Color.Lerp(Warning, Background, 0.55f);

        public UiPresentationPalette(int background, int surface, int selected, int structural, int disabled,
            int concrete, int action, int ally, int enemy, int objective, int success, int warning,
            int textPrimary, int textSecondary, int textMuted, int worldCover, int worldEquipment,
            int worldFacade, int worldWindow, int worldSky, int worldAmbient, int worldSun,
            int worldConcrete, int worldStructure, int worldSafety, int worldZoneWest,
            int worldZoneCenter, int worldZoneEast, float sunIntensity)
        {
            Background = UIDesignTokens.Rgb(background); Surface = UIDesignTokens.Rgb(surface);
            Selected = UIDesignTokens.Rgb(selected); Structural = UIDesignTokens.Rgb(structural);
            Disabled = UIDesignTokens.Rgb(disabled); Concrete = UIDesignTokens.Rgb(concrete);
            PrimaryAction = UIDesignTokens.Rgb(action); Ally = UIDesignTokens.Rgb(ally);
            Enemy = UIDesignTokens.Rgb(enemy); Objective = UIDesignTokens.Rgb(objective);
            Success = UIDesignTokens.Rgb(success); Warning = UIDesignTokens.Rgb(warning);
            TextPrimary = UIDesignTokens.Rgb(textPrimary); TextSecondary = UIDesignTokens.Rgb(textSecondary);
            // A's authored palette only specifies one secondary text token. When its
            // secondary and legacy muted inputs coincide, derive the muted role with the
            // same rule as UIDesignTokens.TextMuted so text hierarchy remains distinct.
            TextMuted = textMuted == textSecondary
                ? Color.Lerp(TextSecondary, Background, 0.28f)
                : UIDesignTokens.Rgb(textMuted);
            WorldCover = UIDesignTokens.Rgb(worldCover);
            WorldEquipment = UIDesignTokens.Rgb(worldEquipment); WorldFacade = UIDesignTokens.Rgb(worldFacade);
            WorldWindow = UIDesignTokens.Rgb(worldWindow); WorldSky = UIDesignTokens.Rgb(worldSky);
            WorldAmbient = UIDesignTokens.Rgb(worldAmbient); WorldSun = UIDesignTokens.Rgb(worldSun);
            WorldConcrete = UIDesignTokens.Rgb(worldConcrete); WorldStructure = UIDesignTokens.Rgb(worldStructure);
            WorldSafety = UIDesignTokens.Rgb(worldSafety);
            WorldZoneWest = UIDesignTokens.Rgb(worldZoneWest); WorldZoneCenter = UIDesignTokens.Rgb(worldZoneCenter);
            WorldZoneEast = UIDesignTokens.Rgb(worldZoneEast);
            WorldSunIntensity = sunIntensity;
        }

        public Color WorldZone(Hotdrop.Config.ZoneId zone) => zone switch
        {
            Hotdrop.Config.ZoneId.West => WorldZoneWest,
            Hotdrop.Config.ZoneId.East => WorldZoneEast,
            _ => WorldZoneCenter,
        };
    }

    /// <summary>
    /// A/B/C are local presentation preferences. Hybrid preserves the checked-in UI palette
    /// by default. Profile changes are cosmetic and do not change map, collision, or balance.
    /// </summary>
    public static class UiPresentationProfile
    {
        public const string PreferenceKey = "Hotdrop.UiPresentationProfile";
        static readonly UiPresentationPalette stylized = new UiPresentationPalette(
            0x0C1220, 0x17202E, 0x263444, 0x49627B, 0x536176, 0xC3B59E,
            0x3DE0C0, 0x4DA3FF, 0xFF5A66, 0xFFB84D, 0x7ED68A, 0xFFB45C,
            0xEDF2F7, 0x8A94A6, 0x8A94A6, 0x7A8D9D, 0x26384A, 0x68797A, 0xD6A96B,
            0x9CC5DC, 0x74899A, 0xFFD4A1, 0xC3B59E, 0x49627B, 0xE0A936,
            0xB6A06B, 0x84998F, 0x9F8D7D, 1.1f);
        static readonly UiPresentationPalette grounded = new UiPresentationPalette(
            0x121619, 0x20272B, 0x303A3D, 0x434B4B, 0x4E5858, 0x969993,
            0x54D2B3, 0x66A7D9, 0xD85D61, 0xE2B85F, 0x7FBE8B, 0xD2A94F,
            0xF0F0EA, 0xA6AEAD, 0x858D8C, 0x737B78, 0x303637, 0x4C5554, 0x788B90,
            0x8F9CA0, 0x717877, 0xD9D1BE, 0x969993, 0x434B4B, 0xD2A94F,
            0x9E9270, 0x788A84, 0x8B8B7E, 0.9f);
        static readonly UiPresentationPalette hybrid = new UiPresentationPalette(
            0x101827, 0x1B2940, 0x263A59, 0x49627B, 0x536176, 0xC3B59E,
            0x3DE0C0, 0x4DA3FF, 0xFF5A66, 0xFFB84D, 0x7ED68A, 0xFFB45C,
            0xF4F7FB, 0xBBC6D8, 0x7F8CA3, 0x7F8D92, 0x2E3D48, 0x5F7076, 0x92AFC1,
            0xA3BED0, 0x81939E, 0xE6D4B8, 0xB1B1A8, 0x46545E, 0xE0AD52,
            0xBCA36F, 0x82968E, 0xA28B80, 1f);

        public static UiPresentationProfileId Current
        {
            get
            {
                int raw = PlayerPrefs.GetInt(PreferenceKey, (int)UiPresentationProfileId.Hybrid);
                return raw >= 0 && raw <= (int)UiPresentationProfileId.Hybrid
                    ? (UiPresentationProfileId)raw : UiPresentationProfileId.Hybrid;
            }
        }

        public static UiPresentationPalette Palette => For(Current);
        public static event System.Action Changed;

        /// <summary>Loads the rooftop key art that belongs to the active presentation profile.</summary>
        public static Texture2D LoadRooftopArtwork()
        {
            var profileArt = Resources.Load<Texture2D>(Current switch
            {
                UiPresentationProfileId.StylizedFlight => "HomeRooftop_StylizedFlight",
                UiPresentationProfileId.GroundedTacticalFlight => "HomeRooftop_GroundedTacticalFlight",
                _ => "HomeRooftop_Hybrid",
            });
            return profileArt != null ? profileArt : Resources.Load<Texture2D>("HomeRooftop");
        }

        public static UiPresentationPalette For(UiPresentationProfileId id) => id switch
        {
            UiPresentationProfileId.StylizedFlight => stylized,
            UiPresentationProfileId.GroundedTacticalFlight => grounded,
            _ => hybrid,
        };

        public static void Select(UiPresentationProfileId id)
        {
            if ((int)id < 0 || (int)id > (int)UiPresentationProfileId.Hybrid || Current == id) return;
            var previous = Palette;
            PlayerPrefs.SetInt(PreferenceKey, (int)id);
            PlayerPrefs.Save();
            RecolorExistingGraphics(previous, Palette);
            RefreshRooftopArtwork();
            Changed?.Invoke();
        }

        static void RefreshRooftopArtwork()
        {
            var currentArtwork = LoadRooftopArtwork();
            foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (image == null) continue;
                var texture = image.texture;
                if (texture != null && IsRooftopArtwork(texture.name))
                    image.texture = currentArtwork;
            }
        }

        static bool IsRooftopArtwork(string textureName) => textureName == "HomeRooftop"
            || textureName == "HomeRooftop_StylizedFlight"
            || textureName == "HomeRooftop_GroundedTacticalFlight"
            || textureName == "HomeRooftop_Hybrid";

        static void RecolorExistingGraphics(UiPresentationPalette from, UiPresentationPalette to)
        {
            foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (label == null || string.IsNullOrEmpty(label.text)) continue;
                label.text = RemapRichTextPalette(label.text, from, to);
            }

            foreach (var graphic in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (graphic == null || !TryRemap(graphic.color, from, to, out var color)) continue;
                graphic.color = color;
            }
        }

        // TMP rich-text colors are independent of Graphic.color, so remap only exact
        // six-digit colors that belong to a semantic palette role. Image/art colors stay put.
        static string RemapRichTextPalette(string text, UiPresentationPalette from, UiPresentationPalette to)
        {
            const string marker = "<color=#";
            var cursor = 0;
            while ((cursor = text.IndexOf(marker, cursor, System.StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                int hexStart = cursor + marker.Length;
                int close = hexStart + 6;
                if (close >= text.Length || text[close] != '>')
                {
                    cursor = hexStart;
                    continue;
                }

                if (int.TryParse(text.Substring(hexStart, 6), System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture, out int rgb) &&
                    TryRemapRichTextColor(rgb, from, to, out var mapped))
                {
                    string replacement = ColorUtility.ToHtmlStringRGB(mapped);
                    text = text.Substring(0, hexStart) + replacement + text.Substring(close);
                    cursor = hexStart + replacement.Length + 1;
                }
                else cursor = close + 1;
            }

            return text;
        }

        /// <summary>Maps semantic palette tags on newly generated text to the active profile.</summary>
        public static string NormalizeRichText(string text) => string.IsNullOrEmpty(text)
            ? text
            : RemapRichTextPalette(text, Palette, Palette);

        static bool TryRemapRichTextColor(int rgb, UiPresentationPalette from, UiPresentationPalette to, out Color mapped)
        {
            if (TryRemap(UIDesignTokens.Rgb(rgb), from, to, out mapped)) return true;
            return TryLegacyHex(rgb, to, out mapped);
        }

        // Frozen literal hexes from the pre-token "screen 08" reference art that still appear as
        // inline UiDraw.Hex(...) calls (e.g. UiuxHud, TouchControlsView panel backgrounds/rings).
        // Unlike a semantic palette role, these never move when a new profile is authored, so they
        // are matched by value rather than by field; shared by rich text and Graphic.color remap.
        static bool TryLegacyHex(int rgb, UiPresentationPalette to, out Color mapped)
        {
            switch (rgb & 0xFFFFFF)
            {
                case 0x101827: case 0x060E1D: case 0x0C1220: case 0x121619: mapped = to.Background; return true;
                case 0x1B2940: case 0x0B1322: case 0x141C2B: case 0x17202E: case 0x20272B: mapped = to.Surface; return true;
                case 0x263A59: case 0x263444: case 0x303A3D: case 0x18202F: case 0x222A3A: case 0x2D3545:
                    mapped = to.Selected; return true;
                case 0x49627B: case 0x434B4B: case 0x859396: case 0x3C494B: mapped = to.Structural; return true;
                case 0x536176: case 0x4E5858: mapped = to.Disabled; return true;
                case 0xF4F7FB: case 0xDBE2F8: case 0xEDF2F7: case 0xF0F0EA: mapped = to.TextPrimary; return true;
                case 0xBBC6D8: case 0xBBC9CB: case 0x8A94A6: case 0xA6AEAD: mapped = to.TextSecondary; return true;
                case 0x7F8CA3: case 0x858D8C: mapped = to.TextMuted; return true;
                case 0x30D5E8: case 0x7CEFFF: case 0x38D9EC: case 0x3DE0C0: case 0x54D2B3: mapped = to.PrimaryAction; return true;
                case 0x4DA3FF: case 0x66A7D9: mapped = to.Ally; return true;
                case 0xFF6376: case 0xFFB2B7: case 0xFF5A66: case 0xD85D61: mapped = to.Enemy; return true;
                case 0xFFD166: case 0xFFDA88: case 0xEDC157: case 0xFFB84D: case 0xE2B85F: mapped = to.Objective; return true;
                case 0xFFB45C: case 0xD2A94F: mapped = to.Warning; return true;
                case 0x7ED68A: case 0x7FBE8B: mapped = to.Success; return true;
                case 0x930D2D: mapped = Color.Lerp(to.Enemy, to.Background, .65f); return true;
                case 0x67001B: mapped = Color.Lerp(to.Enemy, to.Background, .8f); return true;
                case 0xFF9DA4: mapped = Color.Lerp(to.Enemy, to.TextPrimary, .35f); return true;
                case 0x00363C: mapped = UIDesignTokens.TextOn(to.PrimaryAction); return true;
                case 0x3F2E00: mapped = UIDesignTokens.TextOn(to.Objective); return true;
                default: mapped = default; return false;
            }
        }

        static bool TryRemap(Color current, UiPresentationPalette from, UiPresentationPalette to, out Color mapped)
        {
            if (Match(current, from.Background)) mapped = to.Background;
            else if (Match(current, from.Surface)) mapped = to.Surface;
            else if (Match(current, from.Selected)) mapped = to.Selected;
            else if (Match(current, from.Structural)) mapped = to.Structural;
            else if (Match(current, from.Disabled)) mapped = to.Disabled;
            else if (Match(current, from.Concrete)) mapped = to.Concrete;
            else if (Match(current, from.PrimaryAction)) mapped = to.PrimaryAction;
            else if (Match(current, from.Ally)) mapped = to.Ally;
            else if (Match(current, from.Enemy)) mapped = to.Enemy;
            else if (Match(current, from.Objective)) mapped = to.Objective;
            else if (Match(current, from.Success)) mapped = to.Success;
            else if (Match(current, from.Warning)) mapped = to.Warning;
            else if (Match(current, from.TextPrimary)) mapped = to.TextPrimary;
            else if (Match(current, from.TextSecondary)) mapped = to.TextSecondary;
            else if (Match(current, from.TextMuted)) mapped = to.TextMuted;
            else if (Match(current, Color.Lerp(from.Background, from.Surface, .55f))) mapped = Color.Lerp(to.Background, to.Surface, .55f);
            else if (Match(current, Color.Lerp(from.Enemy, from.Background, .65f))) mapped = Color.Lerp(to.Enemy, to.Background, .65f);
            else if (Match(current, Color.Lerp(from.Enemy, from.Background, .8f))) mapped = Color.Lerp(to.Enemy, to.Background, .8f);
            else if (Match(current, Color.Lerp(from.Enemy, from.TextPrimary, .35f))) mapped = Color.Lerp(to.Enemy, to.TextPrimary, .35f);
            else if (Match(current, Color.Lerp(from.PrimaryAction, from.TextPrimary, .35f))) mapped = Color.Lerp(to.PrimaryAction, to.TextPrimary, .35f);
            else if (Match(current, Color.Lerp(from.Objective, from.TextPrimary, .35f))) mapped = Color.Lerp(to.Objective, to.TextPrimary, .35f);
            else if (Match(current, Color.Lerp(from.Selected, from.TextPrimary, .06f))) mapped = Color.Lerp(to.Selected, to.TextPrimary, .06f);
            else if (Match(current, UIDesignTokens.TextOn(from.PrimaryAction))) mapped = UIDesignTokens.TextOn(to.PrimaryAction);
            else if (Match(current, from.ActionPressed)) mapped = to.ActionPressed;
            else if (Match(current, from.HazardPressed)) mapped = to.HazardPressed;
            else if (Match(current, from.ActionObjectiveMix)) mapped = to.ActionObjectiveMix;
            else if (Match(current, from.WarningDim)) mapped = to.WarningDim;
            else if (TryLegacyHex(ToRgbInt(current), to, out mapped)) { mapped.a = current.a; return true; }
            else { mapped = current; return false; }
            mapped.a = current.a;
            return true;
        }

        static int ToRgbInt(Color c) =>
            (Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f) << 16) |
            (Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f) << 8) |
            Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);

        static bool Match(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.003f && Mathf.Abs(a.g - b.g) < 0.003f && Mathf.Abs(a.b - b.b) < 0.003f;
    }

    /// <summary>Local-only redaction for broadcast capture; never changes account or network identity.</summary>
    public static class StreamSafePresentation
    {
        public const string PreferenceKey = "StreamSafeMode";
        const string PlayerSeatTemplateKey = "streamSafe.playerSeatTemplate";
        const string PlayerSeatTemplateFallback = "PLAYER {0:00}";
        static bool preferenceLoaded;
        static bool preferenceEnabled;
        static string cachedSeatTemplateLocaleCode;
        static string cachedSeatTemplate;
        static readonly string[] cachedPlayerSeatNames = new string[99];
        static string failedSeatTemplateLocaleCode;
        static bool seatTemplateLookupFailed;
        static float nextSeatTemplateRetryAt;

        public static bool Enabled
        {
            get
            {
                if (!preferenceLoaded)
                {
                    preferenceEnabled = PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
                    preferenceLoaded = true;
                }
                return preferenceEnabled;
            }
        }
        public static event System.Action Changed;

        public static void SetEnabled(bool enabled)
        {
            preferenceEnabled = enabled;
            preferenceLoaded = true;
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string PlayerName(string value, int seatIndex)
        {
            if (!Enabled) return value ?? UiDraw.L("results.common.player", "Player");
            string template = LocalizedPlayerSeatTemplate(out bool cacheLocalizedLabel);
            if (template.IndexOf("{0", System.StringComparison.Ordinal) < 0)
                template = PlayerSeatTemplateFallback;
            int seatNumber = Mathf.Clamp(seatIndex + 1, 1, 99);
            int seatCacheIndex = seatNumber - 1;
            if (cacheLocalizedLabel && cachedPlayerSeatNames[seatCacheIndex] != null)
                return cachedPlayerSeatNames[seatCacheIndex];
            try
            {
                string formatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, template, seatNumber);
                if (formatted.IndexOf("{0", System.StringComparison.Ordinal) >= 0)
                    formatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, PlayerSeatTemplateFallback, seatNumber);
                if (cacheLocalizedLabel) cachedPlayerSeatNames[seatCacheIndex] = formatted;
            }
            catch (System.FormatException)
            {
                string fallback = string.Format(System.Globalization.CultureInfo.InvariantCulture, PlayerSeatTemplateFallback, seatNumber);
                if (cacheLocalizedLabel) cachedPlayerSeatNames[seatCacheIndex] = fallback;
                return fallback;
            }
            return cacheLocalizedLabel ? cachedPlayerSeatNames[seatCacheIndex] : string.Format(System.Globalization.CultureInfo.InvariantCulture, PlayerSeatTemplateFallback, seatNumber);
        }

        static string LocalizedPlayerSeatTemplate(out bool cacheResult)
        {
            cacheResult = false;
            Locale locale;
            try
            {
                if (!LocalizationSettings.InitializationOperation.IsDone)
                    return PlayerSeatTemplateFallback;
                locale = LocalizationSettings.SelectedLocale;
            }
            catch
            {
                return PlayerSeatTemplateFallback;
            }
            if (locale == null)
                return PlayerSeatTemplateFallback;
            string localeCode = locale.Identifier.Code;
            if (string.IsNullOrEmpty(localeCode))
                return PlayerSeatTemplateFallback;
            if (cachedSeatTemplate != null && string.Equals(localeCode, cachedSeatTemplateLocaleCode, System.StringComparison.Ordinal))
            {
                cacheResult = true;
                return cachedSeatTemplate;
            }
            if (seatTemplateLookupFailed && string.Equals(localeCode, failedSeatTemplateLocaleCode, System.StringComparison.Ordinal) &&
                Time.unscaledTime < nextSeatTemplateRetryAt)
                return PlayerSeatTemplateFallback;

            if (!UiDraw.TryL(PlayerSeatTemplateKey, PlayerSeatTemplateFallback, out string template))
            {
                seatTemplateLookupFailed = true;
                failedSeatTemplateLocaleCode = localeCode;
                nextSeatTemplateRetryAt = Time.unscaledTime + 5f;
                return PlayerSeatTemplateFallback;
            }

            seatTemplateLookupFailed = false;
            failedSeatTemplateLocaleCode = null;
            cachedSeatTemplateLocaleCode = localeCode;
            cachedSeatTemplate = template;
            System.Array.Clear(cachedPlayerSeatNames, 0, cachedPlayerSeatNames.Length);
            cacheResult = true;
            return cachedSeatTemplate;
        }

        public static string AccountAlias(string value) =>
            Enabled || value == null ? UiDraw.L("results.common.player", "Player") : value;

        public static string AccountProgress(string value) =>
            Enabled ? UiDraw.L("common.hidden", "Hidden") : value ?? "";
        public static string PrivateCode(string value) => Enabled ? "••••-••••" : value ?? "";
    }
}
