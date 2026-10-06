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
        public readonly Color WorldZoneWest, WorldZoneCenter, WorldZoneEast;
        public readonly float WorldSunIntensity;
        public Color SurfaceSelected => Selected;
        public Color SurfaceStructural => Structural;
        public Color WorldSafety => Objective;
        public Color WorldConcrete => Concrete;
        public Color WorldStructure => Structural;

        public UiPresentationPalette(int background, int surface, int selected, int structural, int disabled,
            int concrete, int action, int ally, int enemy, int objective, int success, int warning,
            int textPrimary, int textSecondary, int textMuted, int worldCover, int worldEquipment,
            int worldFacade, int worldWindow, int worldSky, int worldAmbient, int worldSun,
            int worldZoneWest, int worldZoneCenter, int worldZoneEast, float sunIntensity)
        {
            Background = UIDesignTokens.Rgb(background); Surface = UIDesignTokens.Rgb(surface);
            Selected = UIDesignTokens.Rgb(selected); Structural = UIDesignTokens.Rgb(structural);
            Disabled = UIDesignTokens.Rgb(disabled); Concrete = UIDesignTokens.Rgb(concrete);
            PrimaryAction = UIDesignTokens.Rgb(action); Ally = UIDesignTokens.Rgb(ally);
            Enemy = UIDesignTokens.Rgb(enemy); Objective = UIDesignTokens.Rgb(objective);
            Success = UIDesignTokens.Rgb(success); Warning = UIDesignTokens.Rgb(warning);
            TextPrimary = UIDesignTokens.Rgb(textPrimary); TextSecondary = UIDesignTokens.Rgb(textSecondary);
            TextMuted = UIDesignTokens.Rgb(textMuted); WorldCover = UIDesignTokens.Rgb(worldCover);
            WorldEquipment = UIDesignTokens.Rgb(worldEquipment); WorldFacade = UIDesignTokens.Rgb(worldFacade);
            WorldWindow = UIDesignTokens.Rgb(worldWindow); WorldSky = UIDesignTokens.Rgb(worldSky);
            WorldAmbient = UIDesignTokens.Rgb(worldAmbient); WorldSun = UIDesignTokens.Rgb(worldSun);
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
            0xEDF2F7, 0xBBC6D8, 0x8A94A6, 0x7A8D9D, 0x26384A, 0x68797A, 0xD6A96B,
            0x9CC5DC, 0x74899A, 0xFFD4A1, 0xB6A06B, 0x84998F, 0x9F8D7D, 1.1f);
        static readonly UiPresentationPalette grounded = new UiPresentationPalette(
            0x121619, 0x20272B, 0x303A3D, 0x434B4B, 0x4E5858, 0x969993,
            0x54D2B3, 0x66A7D9, 0xD85D61, 0xE2B85F, 0x7FBE8B, 0xD2A94F,
            0xF0F0EA, 0xA6AEAD, 0x858D8C, 0x737B78, 0x303637, 0x4C5554, 0x788B90,
            0x8F9CA0, 0x717877, 0xD9D1BE, 0x9E9270, 0x788A84, 0x8B8B7E, 0.9f);
        static readonly UiPresentationPalette hybrid = new UiPresentationPalette(
            0x101827, 0x1B2940, 0x263A59, 0x49627B, 0x536176, 0xC3B59E,
            0x3DE0C0, 0x4DA3FF, 0xFF5A66, 0xFFB84D, 0x7ED68A, 0xFFB45C,
            0xF4F7FB, 0xBBC6D8, 0x7F8CA3, 0x7F8D92, 0x2E3D48, 0x5F7076, 0x92AFC1,
            0xA3BED0, 0x81939E, 0xE6D4B8, 0xBCA36F, 0x82968E, 0xA28B80, 1f);

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
            Changed?.Invoke();
        }

        static void RecolorExistingGraphics(UiPresentationPalette from, UiPresentationPalette to)
        {
            foreach (var graphic in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (graphic == null || !TryRemap(graphic.color, from, to, out var color)) continue;
                graphic.color = color;
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
            else if (Match(current, Color.Lerp(from.Enemy, from.Background, .65f))) mapped = Color.Lerp(to.Enemy, to.Background, .65f);
            else if (Match(current, Color.Lerp(from.Enemy, from.Background, .8f))) mapped = Color.Lerp(to.Enemy, to.Background, .8f);
            else if (Match(current, Color.Lerp(from.Enemy, from.TextPrimary, .35f))) mapped = Color.Lerp(to.Enemy, to.TextPrimary, .35f);
            else { mapped = current; return false; }
            mapped.a = current.a;
            return true;
        }

        static bool Match(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.003f && Mathf.Abs(a.g - b.g) < 0.003f && Mathf.Abs(a.b - b.b) < 0.003f;
    }

    /// <summary>Local-only redaction for broadcast capture; never changes account or network identity.</summary>
    public static class StreamSafePresentation
    {
        public const string PreferenceKey = "StreamSafeMode";
        public static bool Enabled => PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
        public static event System.Action Changed;

        public static void SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string PlayerName(string value, int seatIndex)
        {
            if (!Enabled) return value ?? "PLAYER";
            return $"PLAYER {Mathf.Clamp(seatIndex + 1, 1, 99):00}";
        }

        public static string AccountAlias(string value) => Enabled ? "PLAYER" : value ?? "PLAYER";
        public static string AccountProgress(string value) => Enabled ? "HIDDEN" : value ?? "";
        public static string PrivateCode(string value) => Enabled ? "••••-••••" : value ?? "";
    }
}
