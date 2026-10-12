using System;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 07 loadout surface, rebuilt from its UIUX HTML (numbers are the reference's CSS px).
    /// Weapon data, teammates and countdown are the match's own; nothing here is a placeholder.
    /// </summary>
    public static class PreparationView
    {
        const float PreparationCaptionDp = UIDesignTokens.CaptionDp;
        const float PreparationValueDp = UIDesignTokens.BodyLargeDp;
        const float PreparationEmphasisDp = UIDesignTokens.TitleMediumDp;
        static Color Ink => UIDesignTokens.TextOnAccent;
        static Color Panel => UIDesignTokens.SurfacePanel;
        static Color Edge => UIDesignTokens.SurfaceStructural;
        static Color Cyan => UIDesignTokens.PrimaryAction;
        static Color Gold => UIDesignTokens.ObjectiveGold;
        static Color Text1 => UIDesignTokens.TextPrimary;
        static Color Text2 => UIDesignTokens.TextSecondary;
        static Color Muted => UIDesignTokens.TextMuted;

        static string L(string key, string fallback) => UiDraw.L("preparation." + key, fallback);
        static string Format(string key, string fallback, params object[] args) =>
            UiDraw.LFormat("preparation." + key, fallback, args);

        const float CardWidth = 368f, CardHeight = 425f, CardGap = 24f;
        // 73 px design bar around the 48 px CONFIRM; grows with the 48 dp floor on dense phones.
        static float FooterHeight => UiMetrics.BarHeight(73f, 48f);

        sealed class DensityReflow : MonoBehaviour
        {
            internal Vector3 lastScreen;
            internal float lastWidth;
            internal float lastTextScale;
            internal bool initialized;

            void OnEnable() => StreamSafePresentation.Changed += OnStreamSafeChanged;

            void OnDisable() => StreamSafePresentation.Changed -= OnStreamSafeChanged;

            void OnStreamSafeChanged()
            {
                if (initialized) Rebuild();
            }

            void LateUpdate()
            {
                Vector3 screen = UiMetrics.CurrentScreen;
                var root = (RectTransform)transform;
                float width = root.rect.width;
                bool textScaleChanged = !Mathf.Approximately(UiMetrics.TextScale, lastTextScale);
                if (initialized && screen == lastScreen && Mathf.Abs(width - lastWidth) <= 4f && !textScaleChanged)
                    return;

                // Preparation contains measured labels and widths. Recreate them when display
                // metrics or larger text changes instead of only resizing the footer floor.
                // Show preserves the active weapon choice and countdown and suppresses a second
                // phase-entry animation while replacing an existing root.
                Rebuild();
            }

            void Rebuild()
            {
                var hud = GetComponentInParent<MatchHUD>();
                if (hud == null || lastWeapons == null) return;
                Show(hud, lastWeapons, lastSelected, lastSeconds, lastChoose);
            }
        }

        static void ReserveTouchClusterSpace(float designPx)
        {
            var input = UnityEngine.Object.FindFirstObjectByType<Hotdrop.Input.TouchInputManager>();
            if (input != null) input.SetReservedBottom(designPx);
        }

        static WeaponDefinition[] lastWeapons;
        static Action<int> lastChoose;
        static float lastSeconds;
        static int lastSelected;

        public static void Hide(MatchHUD hud)
        {
            var root = hud != null ? hud.transform.Find("PreparationVisual") : null;
            if (root == null) return;
            var reflow = root.GetComponent<DensityReflow>();
            if (reflow != null) reflow.enabled = false;
            ReserveTouchClusterSpace(0f);
            var rootObject = root.gameObject;
            UiScreenEnterMotion.Exit(rootObject, () => DestroyRoot(rootObject));
        }

        static void DestroyRoot(GameObject root)
        {
            if (root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }

        public static void Show(MatchHUD hud, WeaponDefinition[] weapons, int selected, float seconds, Action<int> choose)
        {
            if (hud == null || weapons == null || weapons.Length == 0) return;
            // Rebuild immediately for an in-phase weapon selection change. Only the first
            // preparation surface gets screen-entry motion, so tapping a card cannot replay it.
            var previous = hud.transform.Find("PreparationVisual");
            bool enteringPhase = previous == null;
            if (previous != null)
            {
                // Destroy is deferred in play mode. Give the outgoing root a different name so
                // SetCountdown below resolves the newly built surface during this same frame.
                previous.name = "PreparationVisual_Destroying";
                DestroyRoot(previous.gameObject);
            }
            lastWeapons = weapons; lastChoose = choose; lastSeconds = seconds; lastSelected = selected;
            var root = UiDraw.Node(hud.transform, "PreparationVisual");
            if (enteringPhase) root.gameObject.AddComponent<UiScreenEnterMotion>();
            UiDraw.Fill(root);
            // Backdrop: venue at 65% under #101827 -> /70 -> /85 (bottom to top).
            UiDraw.Backdrop(root, .65f, Ink, new Color(Ink.r, Ink.g, Ink.b, .7f), new Color(Ink.r, Ink.g, Ink.b, .85f));
            BuildHeader(root, seconds);
            BuildSafety(root);
            var row = UiDraw.Place(UiDraw.Node(root, "Weapons"), new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -362f),
                new Vector2(CardWidth * 3f + CardGap * 2f, CardHeight));
            int count = Mathf.Min(3, weapons.Length);
            for (int i = 0; i < count; i++)
                WeaponCard(row, weapons[i], i, i == selected, choose, (i - (count - 1) / 2f) * (CardWidth + CardGap));
            BuildFooter(root, selected, choose);
            var reflow = root.gameObject.AddComponent<DensityReflow>();
            Reflow(root, reflow);
            SetCountdown(hud, seconds);
        }

        static void Reflow(RectTransform root, DensityReflow watcher)
        {
            if (root == null) return;
            if (root.Find("Footer") is RectTransform footer)
                footer.sizeDelta = new Vector2(footer.sizeDelta.x, FooterHeight);
            ReserveTouchClusterSpace(FooterHeight);
            UiFloor.Apply(root);
            watcher.lastScreen = UiMetrics.CurrentScreen;
            watcher.lastWidth = root.rect.width;
            watcher.lastTextScale = UiMetrics.TextScale;
            watcher.initialized = true;
        }

        static void BuildHeader(RectTransform root, float seconds)
        {
            var header = UiDraw.Box(root, "Header", UIDesignTokens.WithAlpha(UIDesignTokens.Background, .85f)).rectTransform;
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1f);
            header.sizeDelta = new Vector2(0f, 77f); header.anchoredPosition = Vector2.zero;
            var rule = UiDraw.Box(header, "HeaderRule", UIDesignTokens.WithAlpha(Edge, .4f)).rectTransform;
            rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1f, 0f); rule.pivot = new Vector2(.5f, 0f);
            rule.sizeDelta = new Vector2(0f, 1f);

            string zone = ZoneLabel(MatchView.Current != null ? MatchView.Current.ScoringZone : ZoneId.Center);
            UiDraw.Place(UiDraw.Text(header, "Kicker", L("header.kicker", "RELAY  ·  SWITCHYARD"), UiFont.Heading, PreparationCaptionDp, Cyan, .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(32f, -26f), new Vector2(300f, 16f));
            var objective = UiDraw.Text(header, "Objective", Format("header.objectiveTemplate", "Opening Objective: {0}",
                $"<color=#{ColorUtility.ToHtmlStringRGB(Gold)}>{zone}</color>"), UiFont.Heading, PreparationValueDp, Text1, -.025f);
            float objW = UiDraw.Width(objective);
            UiDraw.Place(objective.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(32f, -48f), new Vector2(objW + 2f, 28f));
            UiDraw.Place(UiDraw.Box(header, "Divider", UIDesignTokens.WithAlpha(Edge, .5f)).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(32f + objW + 16f, -40f), new Vector2(1f, 24f));
            var instruction = UiDraw.Text(header, "Instruction", L("header.instruction", "Equip weapon for this life. Changeable on respawn."), UiFont.Body, PreparationCaptionDp, Text2);
            float instW = UiDraw.Width(instruction);
            UiDraw.Place(instruction.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(32f + objW + 33f, -40f), new Vector2(instW + 2f, 16f));
            float leftW = objW + 33f + instW;

            // Teammate chips (right), from the live roster.
            float x = -32f, rightW = 0f;
            var view = MatchView.Current;
            var local = view?.LocalParticipant;
            if (view != null && local != null)
            {
                var mates = new System.Collections.Generic.List<IMatchParticipant>();
                foreach (var p in view.Participants)
                    if (p != null && !ReferenceEquals(p, local) && p.TeamId == local.TeamId) mates.Add(p);
                for (int i = mates.Count - 1; i >= 0; i--)
                {
                    var p = mates[i];
                    var chip = UiDraw.Box(header, "Mate" + i, Panel, 8f, Edge);
                    var name = UiDraw.Text(chip.transform, "Name", p.DisplayName, UiFont.Heading, PreparationCaptionDp, Text1);
                    string gun = p is PlayerController pc && pc.CurrentWeapon != null ? WeaponShortName(pc.CurrentWeapon) : "";
                    var weapon = UiDraw.Text(chip.transform, "Weapon", gun, UiFont.MonoRegular, PreparationCaptionDp, Muted);
                    float nw = UiDraw.Width(name), ww = gun.Length > 0 ? UiDraw.Width(weapon) : 0f;
                    float w = 12f + 8f + 8f + nw + (ww > 0f ? 8f + ww : 0f) + 12f + 2f;
                    UiDraw.Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(x, -40f), new Vector2(w, 30f));
                    UiDraw.Place(UiDraw.Dot(chip.transform, "Dot", 8f, Cyan).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(13f, 0f), new Vector2(8f, 8f));
                    UiDraw.Place(name.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(29f, 0f), new Vector2(nw + 2f, 16f));
                    UiDraw.Place(weapon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(37f + nw, 0f), new Vector2(ww + 2f, 14f));
                    x -= w + 12f; rightW += w + 12f;
                }
            }

            // Countdown, placed in the free space as justify-between places it.
            var caption = UiDraw.Text(header, "CountdownCaption", L("countdown.caption", "Match Starts In"), UiFont.Heading, PreparationCaptionDp, Gold, .05f);
            var count = UiDraw.Text(header, "Countdown", Format("countdown.secondsTemplate", "{0:00}s", Mathf.Max(0, Mathf.CeilToInt(seconds))), UiFont.Mono, PreparationEmphasisDp, Gold);
            float cw = UiDraw.Width(caption), nwid = UiDraw.Width(count, "00s");
            float boxW = 20f + cw + 12f + nwid + 20f + 4f;
            var box = UiDraw.Box(header, "CountdownPanel", Panel, 12f);
            var frame = UiDraw.Box(box.transform, "Frame", Gold);
            frame.sprite = UIRoundedSprites.RoundedOutline(12f, 2f); frame.type = Image.Type.Sliced;
            UiDraw.Fill(frame.rectTransform);
            UiDraw.Glow(box.rectTransform, UIDesignTokens.WithAlpha(Gold, .2f), 12f, 15f);
            float canvasW = root.rect.width > 0f ? root.rect.width : 1280f;
            float chipsW = Mathf.Max(0f, rightW - 12f);
            float gap = Mathf.Max(16f, (canvasW - 64f - leftW - boxW - chipsW) / 2f);
            float boxX = Mathf.Min(32f + leftW + gap, canvasW - 32f - chipsW - 16f - boxW);
            UiDraw.Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(boxX, -40f), new Vector2(boxW, 48f));
            caption.transform.SetParent(box.transform, false);
            count.transform.SetParent(box.transform, false);
            count.name = "Countdown";
            UiDraw.Place(caption.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f, 0f), new Vector2(cw + 2f, 16f));
            UiDraw.Place(count.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(22f + cw + 12f, 0f), new Vector2(nwid + 4f, 32f));
        }

        static void BuildSafety(RectTransform root)
        {
            var text = UiDraw.Text(root, "SafetyText", L("safety.notice", "SPAWN DOORS LOCKED • DAMAGE DISABLED • PADS SAFE"), UiFont.HeadingMedium, PreparationCaptionDp, Text2, .05f);
            float w = UiDraw.Width(text);
            var pill = UiDraw.Box(root, "SafetyNotice", UIDesignTokens.WithAlpha(Panel, .9f), 15f, Edge);
            UiDraw.Place(pill.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -111f), new Vector2(16f + 8f + 8f + w + 16f + 2f, 30f));
            UiDraw.Place(UiDraw.Dot(pill.transform, "Dot", 8f, Gold).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(17f, 0f), new Vector2(8f, 8f));
            text.transform.SetParent(pill.transform, false);
            UiDraw.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(33f, 0f), new Vector2(w + 2f, 16f));
        }

        static void BuildFooter(RectTransform root, int selected, Action<int> choose)
        {
            var footer = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(UIDesignTokens.Background, .9f)).rectTransform;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1f, 0f); footer.pivot = new Vector2(.5f, 0f);
            footer.sizeDelta = new Vector2(0f, FooterHeight); footer.anchoredPosition = Vector2.zero;
            UiDraw.HRule(footer, "FooterRule", UIDesignTokens.WithAlpha(Edge, .4f), 0f);
            UiDraw.Place(UiDraw.Icon(footer, "Verified", "verified", 14f, Cyan).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(UiDraw.Text(footer, "UnlockNote", L("footer.unlockNote", "All 3 weapons fully unlocked from day one • Cosmetics never change weapon power or spread."), UiFont.Body, PreparationCaptionDp, Text2).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(54f, 0f), new Vector2(700f, 16f));
            // Equipping is immediate; CONFIRM re-applies the current choice and the match starts itself.
            var label = UiDraw.Text(footer, "Text", Format("footer.confirmAutoTemplate", "CONFIRM LOADOUT (AUTO IN {0:00}s)", 8), UiFont.Heading, PreparationCaptionDp, Ink, .05f, TextAlignmentOptions.Midline);
            float w = UiDraw.Width(label, Format("footer.confirmAutoTemplate", "CONFIRM LOADOUT (AUTO IN {0:00}s)", 0)) + 64f;
            var confirm = UiDraw.Tap(footer, "Confirm", Cyan, 12f, () => choose?.Invoke(selected));
            UiDraw.Place((RectTransform)confirm.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(w, 48f));
            label.transform.SetParent(confirm.transform, false);
            UiDraw.Fill(label.rectTransform);
        }

        public static void SetCountdown(MatchHUD hud, float seconds)
        {
            var root = hud != null ? hud.transform.Find("PreparationVisual") : null;
            if (root == null) return;
            lastSeconds = seconds;
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            var label = root.Find("Header/CountdownPanel/Countdown")?.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = Format("countdown.secondsTemplate", "{0:00}s", s);
            var confirm = root.Find("Footer/Confirm/Text")?.GetComponent<TextMeshProUGUI>();
            if (confirm != null) confirm.text = Format("footer.confirmAutoTemplate", "CONFIRM LOADOUT (AUTO IN {0:00}s)", s);
        }

        /// <summary>Selection changes restyle three cards and the confirm action, so the view is rebuilt.</summary>
        public static void SetSelection(MatchHUD hud, int selected)
        {
            var root = hud != null ? hud.transform.Find("PreparationVisual") : null;
            if (root == null || lastWeapons == null) return;
            Show(hud, lastWeapons, selected, lastSeconds, lastChoose);
        }

        static string WeaponKey(WeaponDefinition weapon, string field) =>
            "weapon." + weapon.id.ToString().ToLowerInvariant() + "." + field;

        internal static string WeaponName(WeaponDefinition weapon)
        {
            string fallback = string.IsNullOrEmpty(weapon.displayName) ? weapon.id.ToString().ToUpperInvariant() : weapon.displayName;
            return L(WeaponKey(weapon, "name"), fallback);
        }

        internal static string WeaponShortName(WeaponDefinition weapon) =>
            L(WeaponKey(weapon, "shortName"), string.IsNullOrEmpty(weapon.displayName) ? weapon.id.ToString().ToUpperInvariant() : weapon.displayName.Split(' ')[0]);

        internal static string ZoneLabel(ZoneId zone) => zone switch
        {
            ZoneId.West => L("zone.west", "West Relay"),
            ZoneId.East => L("zone.east", "East Relay"),
            _ => L("zone.center", "Center Relay"),
        };

        internal static string ZoneShortLabel(ZoneId zone) => zone switch
        {
            ZoneId.West => L("zone.westShort", "West"),
            ZoneId.East => L("zone.eastShort", "East"),
            _ => L("zone.centerShort", "Center"),
        };

        static string FireBehavior(WeaponDefinition w) => w.pelletsPerShot > 1
            ? Format("behavior.pelletSpreadTemplate", "{0}-Pellet Spread", w.pelletsPerShot)
            : w.roundsPerMinute >= 700f ? L("behavior.rapidFire", "Rapid Fire") : L("behavior.precisionAuto", "Precision Auto");

        internal static string Role(WeaponDefinition w) => w.id == WeaponId.Arc ? L(WeaponKey(w, "role"), "BALANCED • MID RANGE")
            : w.id == WeaponId.Zip ? L(WeaponKey(w, "role"), "FAST • CLOSE RANGE") : L(WeaponKey(w, "role"), "HEAVY • VERY CLOSE");

        static string Ammo(WeaponDefinition weapon) => weapon.pelletsPerShot > 1
            ? Format("ammo.shellsTemplate", "{0} Shells", weapon.magazineSize)
            : Format("ammo.roundsTemplate", "{0} Rounds", weapon.magazineSize);

        static string LearningTip(WeaponDefinition weapon) =>
            Format("card.learningTipTemplate", "<color=#{0}><b>Learning Tip:</b></color> {1}",
                ColorUtility.ToHtmlStringRGB(Text1), L(WeaponKey(weapon, "learningTip"), weapon.roleDescription ?? string.Empty));

        static void WeaponCard(RectTransform row, WeaponDefinition weapon, int index, bool selected, Action<int> choose, float x)
        {
            const float pad = 22f;
            var shell = UiDraw.ArtSliced(row, "Weapon_" + index, selected ? "prep_card_selected" : "prep_card", 24f);
            var card = UiDraw.Place(shell.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(x, 0f), new Vector2(CardWidth, CardHeight));
            Color accent = selected ? Cyan : Text2;

            // Header row.
            if (selected)
            {
                var badgeText = UiDraw.Text(card, "Badge", L("card.selected", "Selected"), UiFont.Heading, PreparationCaptionDp, Cyan, .05f, TextAlignmentOptions.Midline);
                float bw = UiDraw.Width(badgeText) + 22f;
                var chip = UiDraw.Box(card, "BadgeChip", UIDesignTokens.WithAlpha(Cyan, .2f), 4f, UIDesignTokens.WithAlpha(Cyan, .5f));
                chip.transform.SetSiblingIndex(badgeText.transform.GetSiblingIndex());
                UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -33f), new Vector2(bw, 22f));
                UiDraw.Place(badgeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -33f), new Vector2(bw, 16f));
            }
            else UiDraw.Place(UiDraw.Text(card, "Badge", L("card.unlocked", "Unlocked"), UiFont.Heading, PreparationCaptionDp, Text2, .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -33f), new Vector2(120f, 16f));
            UiDraw.Place(UiDraw.Text(card, "Ammo", Ammo(weapon), UiFont.Mono, PreparationCaptionDp,
                    selected ? Cyan : Text2, 0f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad, -33f), new Vector2(140f, 16f));
            UiDraw.HRule(card, "TopRule", UIDesignTokens.WithAlpha(Edge, .4f), 56f, pad, pad);

            // Silhouette, name, role.
            string art = weapon.id == WeaponId.Arc ? "WeaponPortraits/weapon_arc"
                : weapon.id == WeaponId.Zip ? "WeaponPortraits/weapon_zip" : "WeaponPortraits/weapon_brick";
            var icon = UiDraw.Art(card, "WeaponSilhouette", art, Vector2.zero);
            icon.color = accent;
            var portraitSize = selected ? new Vector2(192f, 96f) : new Vector2(160f, 80f);
            UiDraw.Place(icon.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -121f), portraitSize);
            UiDraw.FitRawImage(icon, portraitSize);
            UiDraw.Place(UiDraw.Text(card, "Name", WeaponName(weapon), UiFont.Heading, PreparationEmphasisDp, Text1, -.025f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -187f), new Vector2(CardWidth - 2f * pad, 28f));
            UiDraw.Place(UiDraw.Text(card, "Role", Role(weapon), UiFont.HeadingSemiBold, PreparationCaptionDp, accent, .05f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -209f), new Vector2(CardWidth - 2f * pad, 16f));

            // Specs: fire behaviour, full-damage band with its bar, learning tip.
            UiDraw.HRule(card, "StatsRule", UIDesignTokens.WithAlpha(Edge, .4f), 233f, pad, pad);
            Row(card, "Fire", "Fire Behavior", FireBehavior(weapon), Text1, false, 254f);
            Row(card, "Range", "Optimal Distance", $"0m – {weapon.fullDamageDistance:0}m", selected ? Cyan : Text2, true, 278f);
            var track = UiDraw.Box(card, "RangeTrack", Ink, 3f);
            UiDraw.Place(track.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -297f), new Vector2(CardWidth - 2f * pad, 6f));
            var fill = UiDraw.Box(track.transform, "Fill", selected ? Cyan : UIDesignTokens.WithAlpha(Cyan, .6f), 3f);
            float frac = Mathf.Clamp01(weapon.maxRange > 0f ? weapon.fullDamageDistance / weapon.maxRange : .5f);
            UiDraw.Place(fill.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((CardWidth - 2f * pad) * frac, 6f));
            var tip = UiDraw.Text(card, "Tip", LearningTip(weapon), UiFont.Body, PreparationCaptionDp, Text2);
            tip.textWrappingMode = TextWrappingModes.Normal;
            tip.alignment = TextAlignmentOptions.TopLeft;
            tip.lineSpacing = 4f;
            UiDraw.Place(tip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(pad, -312f), new Vector2(CardWidth - 2f * pad, 40f));

            // Action.
            var button = UiDraw.Tap(card, "Choose", selected ? Cyan : Panel, 8f, () => choose?.Invoke(index), selected ? (Color?)null : Edge);
            UiDraw.Place((RectTransform)button.transform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, pad), new Vector2(CardWidth - 2f * pad, 44f));
            var label = UiDraw.Text(button.transform, "Label", selected ? L("action.confirmedEquipped", "Confirmed Equipped")
                    : Format("action.equipTemplate", "Equip {0}", L(WeaponKey(weapon, "equipName"), weapon.id == WeaponId.Zip ? "ZIP SMG" : WeaponShortName(weapon))),
                UiFont.Heading, PreparationCaptionDp, selected ? Ink : Text1, .05f, TextAlignmentOptions.Midline);
            UiDraw.Fill(label.rectTransform);
            if (selected)
            {
                float lw = UiDraw.Width(label);
                label.rectTransform.offsetMin = new Vector2(20f, 0f);
                UiDraw.Place(UiDraw.Icon(button.transform, "Check", "check", 16f, Ink).rectTransform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-lw / 2f - 2f, 0f), new Vector2(16f, 16f));
            }
        }

        static void Row(RectTransform card, string name, string label, string value, Color valueColor, bool bold, float y)
        {
            string key = name == "Fire" ? "row.fireBehavior" : "row.optimalDistance";
            UiDraw.Place(UiDraw.Text(card, name + "Label", L(key, label), UiFont.Body, PreparationCaptionDp, Muted).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(22f, -y), new Vector2(160f, 16f));
            UiDraw.Place(UiDraw.Text(card, name + "Value", value, bold ? UiFont.Mono : UiFont.MonoRegular, PreparationCaptionDp, valueColor, 0f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-22f, -y), new Vector2(200f, 16f));
        }
    }
}
