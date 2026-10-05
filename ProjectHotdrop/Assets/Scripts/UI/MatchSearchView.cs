using System;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 06 search presentation, rebuilt from its UIUX HTML (CSS px). Time, region and squad
    /// come from the active queue; the blueprint map behind it is the reference's own SVG.
    /// </summary>
    public static class MatchSearchView
    {
        static readonly Color Ink = UiDraw.Hex("#101827");
        static readonly Color Panel = UiDraw.Hex("#1b2940");
        static readonly Color Edge = UiDraw.Hex("#49627b");
        static readonly Color Cyan = UiDraw.Hex("#30d5e8");
        static readonly Color Coral = UiDraw.Hex("#ff6376");
        static readonly Color Text1 = UiDraw.Hex("#f4f7fb");
        static readonly Color Text2 = UiDraw.Hex("#bbc6d8");
        static readonly Color Muted = UiDraw.Hex("#7f8ca3");
        static Texture2D dots;

        public static void Clear(GameObject owner)
        {
            var old = owner != null ? owner.transform.Find("MatchSearchVisual") : null;
            if (old == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject);
            else UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        public static void Show(GameObject owner, int elapsed, string region, PartyDto party, string playerId,
            bool practiceAvailable, Action cancel, Action practice)
        {
            Clear(owner);
            var root = UiDraw.Node(owner.transform, "MatchSearchVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Ink).rectTransform);
            var grid = UiDraw.Node(root, "LocationGrid").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(grid.rectTransform);
            grid.texture = Dots(); grid.uvRect = new Rect(0, 0, 1600f / 24f, 720f / 24f);
            grid.color = new Color(Cyan.r, Cyan.g, Cyan.b, .2f); grid.raycastTarget = false;
            // The reference's blueprint SVG is 1920x1080 drawn 'meet' into the frame at 35%.
            var map = UiDraw.Node(root, "Blueprint").gameObject.AddComponent<RawImage>();
            map.texture = UiDraw.Texture("search_map");
            map.color = new Color(1f, 1f, 1f, .35f);
            map.raycastTarget = false;
            var fit = map.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;

            // Header: live dot, mode and venue; region chip right.
            var header = UiDraw.Box(root, "Header", UiDraw.Hex("#101827", .8f)).rectTransform;
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1f);
            header.sizeDelta = new Vector2(0f, 58f); header.anchoredPosition = Vector2.zero;
            var rule = UiDraw.Box(header, "Rule", UiDraw.Hex("#49627b", .4f)).rectTransform;
            rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1f, 0f); rule.pivot = new Vector2(.5f, 0f); rule.sizeDelta = new Vector2(0f, 1f);
            UiDraw.Place(UiDraw.Dot(header, "Live", 10f, Cyan).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(10f, 10f));
            var state = UiDraw.Text(header, "State", "RELAY MATCHMAKING", UiFont.Heading, 12f, Cyan, .05f);
            float sw = UiDraw.Width(state);
            UiDraw.Place(state.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(54f, 0f), new Vector2(sw + 2f, 16f));
            UiDraw.Place(UiDraw.Text(header, "Sep", "•", UiFont.Body, 12f, Edge).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(66f + sw, 0f), new Vector2(10f, 16f));
            UiDraw.Place(UiDraw.Text(header, "Mode", "Switchyard 3v3 • Three Minute Round", UiFont.Body, 12f, Text2).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(84f + sw, 0f), new Vector2(320f, 16f));
            if (!string.IsNullOrWhiteSpace(region))
            {
                var chip = UiDraw.Box(header, "RegionChip", Panel, 8f, Edge);
                var rt = UiDraw.Text(chip.transform, "Region", region, UiFont.MonoRegular, 12f, Text2);
                float rw = 12f + 8f + 8f + UiDraw.Width(rt) + 12f;
                UiDraw.Place(chip.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(rw, 30f));
                UiDraw.Place(UiDraw.Dot(chip.transform, "Dot", 8f, Cyan).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(8f, 8f));
                UiDraw.Place(rt.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(28f, 0f), new Vector2(UiDraw.Width(rt) + 2f, 16f));
            }

            // Central stack (max-w-xl): rings + timer, title, line, squad, actions.
            int memberCount = Mathf.Clamp(party?.members?.Length ?? 1, 1, 3);
            var stack = UiDraw.Place(UiDraw.Node(root, "Stack"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -4f), new Vector2(576f, 522f));
            var radar = UiDraw.Place(UiDraw.Node(stack, "Radar"), new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -88f), new Vector2(176f, 176f));
            UiDraw.Fill(UiDraw.Ring(radar, "OuterScan", 176f, 1f, UiDraw.Hex("#49627b", .4f)).rectTransform);
            UiDraw.Place(UiDraw.Ring(radar, "MiddleScan", 144f, 1f, UiDraw.Hex("#30d5e8", .3f)).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(144f, 144f));
            UiDraw.Place(UiDraw.Ring(radar, "InnerScan", 112f, 1f, UiDraw.Hex("#30d5e8", .6f)).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(112f, 112f));
            var sweep = UiDraw.Ring(radar, "Sweep", 176f, 2f, Cyan);
            UiDraw.Fill(sweep.rectTransform);
            sweep.type = Image.Type.Filled; sweep.fillMethod = Image.FillMethod.Radial360;
            sweep.fillOrigin = (int)Image.Origin360.Top; sweep.fillAmount = .25f;
            sweep.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f - Time.unscaledTime * 120f);
            sweep.gameObject.AddComponent<SpinUi>().degreesPerSecond = -120f;
            UiDraw.Place(UiDraw.Text(radar, "SearchTime", $"{elapsed / 60:00}:{elapsed % 60:00}", UiFont.Mono, 30f, Text1, -.025f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 7f), new Vector2(160f, 36f));
            UiDraw.Place(UiDraw.Text(radar, "SearchTimeCaption", "SEARCH TIME", UiFont.HeadingSemiBold, 10f, Muted, .05f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -17f), new Vector2(160f, 14f));
            UiDraw.Place(UiDraw.Text(stack, "SearchingTitle", "FINDING A RELAY MATCH", UiFont.Heading, 24f, Text1, -.025f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -216f), new Vector2(576f, 32f));
            var explain = UiDraw.Text(stack, "SearchExplanation", string.IsNullOrEmpty(region)
                    ? "Connecting 6 competitive operatives for a fair 3v3 Relay match."
                    : $"Connecting 6 competitive operatives across {region}. Matching fair, low-latency lobbies.",
                UiFont.Body, 14f, Text2, 0f, TextAlignmentOptions.Midline);
            explain.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(explain.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -258f), new Vector2(448f, 40f));

            var squad = UiDraw.Box(stack, "Squad", UiDraw.Hex("#1b2940", .9f), 12f, Edge);
            var sq = UiDraw.Place(squad.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -302f), new Vector2(576f, 116f));
            UiDraw.Place(UiDraw.Text(sq, "SquadHeading", $"YOUR SQUAD ({memberCount}/3)", UiFont.Heading, 12f, Muted, .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(17f, -26f), new Vector2(240f, 16f));
            bool allReady = party?.members != null && Array.TrueForAll(party.members, m => m == null || m.isReady);
            UiDraw.Place(UiDraw.Text(sq, "ReadyHeading", party == null ? "SOLO QUEUE" : allReady ? "ALL MEMBERS READY" : "PARTY SEARCH", UiFont.MonoMedium, 12f, Cyan, 0f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-17f, -26f), new Vector2(240f, 16f));
            UiDraw.HRule(sq, "Rule", UiDraw.Hex("#49627b", .4f), 41f, 17f, 17f);
            float seatW = (576f - 34f - 24f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var member = party?.members != null && i < party.members.Length ? party.members[i] : null;
                bool self = member != null && string.Equals(member.playerId, playerId, StringComparison.OrdinalIgnoreCase);
                bool solo = party == null && i == 0;
                string alias = member?.alias ?? (solo ? "YOU" : "OPEN SEAT");
                bool filled = member != null || solo;
                var seat = UiDraw.Box(sq, "Seat_" + i, Ink, 8f, i == 0 ? UiDraw.Hex("#30d5e8", .4f) : Edge);
                UiDraw.Place(seat.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(17f + i * (seatW + 12f), -54f), new Vector2(seatW, 46f));
                UiDraw.Place(UiDraw.Dot(seat.transform, "Dot", 8f, filled ? Cyan : Edge).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(9f, 0f), new Vector2(8f, 8f));
                var name = UiDraw.Text(seat.transform, "Alias", alias, UiFont.Heading, 12f, filled ? Text1 : Muted);
                name.overflowMode = TextOverflowModes.Ellipsis;
                UiDraw.Place(name.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(25f, 7f), new Vector2(seatW - 34f, 16f));
                UiDraw.Place(UiDraw.Text(seat.transform, "Status", self || solo ? (party != null && i == 0 ? "HOST" : "YOU") : member == null ? "WAITING" : member.isReady ? "READY" : "NOT READY",
                        UiFont.MonoRegular, 10f, Muted).rectTransform,
                    new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(25f, -9f), new Vector2(seatW - 34f, 14f));
            }

            float ax = 0f;
            var actions = UiDraw.Place(UiDraw.Node(stack, "Actions"), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -442f), new Vector2(576f, 48f));
            float pw = practiceAvailable ? Measure("PRACTICE WHILE WAITING") : 0f, cw = Measure("CANCEL SEARCH");
            float total = pw + cw + (practiceAvailable ? 16f : 0f);
            ax = -total / 2f;
            if (practiceAvailable)
            {
                Action(actions, "PracticeWhileWaiting", "flight_takeoff", "PRACTICE WHILE WAITING", Panel, Edge, Text2, ax, pw, practice);
                ax += pw + 16f;
            }
            Action(actions, "CancelSearch", "close", "CANCEL SEARCH", Panel, UiDraw.Hex("#ff6376", .5f), Coral, ax, cw, cancel);

            // Footer: the fairness rule and the venue.
            var footer = UiDraw.Box(root, "Footer", UiDraw.Hex("#101827", .9f)).rectTransform;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1f, 0f); footer.pivot = new Vector2(.5f, 0f);
            footer.sizeDelta = new Vector2(0f, 42f); footer.anchoredPosition = Vector2.zero;
            UiDraw.HRule(footer, "Rule", UiDraw.Hex("#49627b", .4f), 0f);
            UiDraw.Place(UiDraw.Icon(footer, "Info", "info", 14f, Cyan).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(UiDraw.Text(footer, "Fairness", practiceAvailable ? "Honest latency matching active. Practice leaves the queue before starting." : "Honest latency matching active.",
                    UiFont.Body, 12f, Muted).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(54f, 0f), new Vector2(600f, 16f));
            UiDraw.Place(UiDraw.Text(footer, "Venue", "SWITCHYARD // RELAY", UiFont.MonoRegular, 12f, Muted, .05f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(300f, 16f));
        }

        static float Measure(string caption)
        {
            var probe = new GameObject("Probe", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            UIFonts.Use(probe, UiFont.Heading);
            probe.fontSize = 12f; probe.characterSpacing = 5f;
            float w = probe.GetPreferredValues(caption).x + 24f + 18f + 8f + 24f;
            UnityEngine.Object.Destroy(probe.gameObject);
            return w;
        }

        static void Action(RectTransform parent, string name, string icon, string caption, Color fill, Color border, Color ink, float x, float w, Action action)
        {
            var b = UiDraw.Tap(parent, name, fill, 12f, () => { GameAudio.Play2D(GameCue.UiTap); action?.Invoke(); }, border);
            UiDraw.Place((RectTransform)b.transform, new Vector2(.5f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(w, 48f));
            UiDraw.Place(UiDraw.Icon(b.transform, "Icon", icon, 18f, ink).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(UiDraw.Text(b.transform, "Label", caption, UiFont.Heading, 12f, ink, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(50f, 0f), new Vector2(w - 60f, 16f));
        }

        static Texture2D Dots()
        {
            if (dots != null) return dots;
            dots = new Texture2D(24, 24, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                    dots.SetPixel(x, y, x >= 11 && x <= 12 && y >= 11 && y <= 12 ? Color.white : new Color(0f, 0f, 0f, 0f));
            dots.Apply(false, true);
            return dots;
        }
    }
}
