using System;
using System.Collections.Generic;
using Hotdrop.Core;
using Hotdrop.Match;
using Hotdrop.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 11 results, rebuilt from its UIUX HTML (CSS px). The page scrolls under a sticky
    /// action bar, as the reference does. Every figure is the match's own: where the reference
    /// shows data this game does not record (ping, telemetry, pad-use attribution) the slot carries
    /// a real equivalent instead of an invented number.
    /// </summary>
    public static class ResultsView
    {
        static Color Lowest => UiDraw.Hex("#060e1d");
        static Color Low => UiDraw.Hex("#141c2b");
        static Color Mid => UiDraw.Hex("#18202f");
        static Color High => UiDraw.Hex("#222a3a");
        static Color Highest => UiDraw.Hex("#2d3545");
        static Color Primary => UiDraw.Hex("#7cefff");
        static Color Secondary => UiDraw.Hex("#ffb2b7");
        static Color Tertiary => UiDraw.Hex("#ffda88");
        static Color OnSurface => UiDraw.Hex("#dbe2f8");
        static Color OnVariant => UiDraw.Hex("#bbc9cb");
        static Color Outline => UiDraw.Hex("#859396");
        static Color OnPrimary => UiDraw.Hex("#00363c");

        const float Pad = 24f, Gap = 12f;

        public static void Hide(MatchHUD hud)
        {
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(root.gameObject);
            else UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        public static void SetReceipt(MatchHUD hud, string receipt, Color color)
        {
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null || string.IsNullOrWhiteSpace(receipt)) return;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "Receipt") { t.text = receipt; t.color = color; }
        }

        public static void Show(MatchHUD hud, bool win, bool draw, int ally, int enemy,
            IReadOnlyList<IMatchParticipant> players, IMatchParticipant local, string footer,
            string nextGoal, Action replay, Action home)
        {
            Hide(hud);
            var root = UiDraw.Node(hud.transform, "ResultsVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", UiDraw.Hex("#0b1322")).rectTransform);
            var photo = UiDraw.Node(root, "RooftopBackdrop").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(photo.rectTransform);
            photo.texture = Resources.Load<Texture2D>("HomeRooftop");
            photo.color = new Color(.6f, .62f, .66f, .25f);
            photo.raycastTarget = false;
            var surface = UiDraw.Hex("#0b1322");
            UiDraw.VerticalGradient(root, "Shade", surface, new Color(surface.r, surface.g, surface.b, .8f), new Color(surface.r, surface.g, surface.b, .9f));

            bool localPractice = !string.IsNullOrEmpty(footer) && footer.Contains("PRACTICE MATCH");
            float width = root.rect.width > 0f ? root.rect.width : hud.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;
            float contentW = width - Pad * 2f;

            // Scrolling page above an 80 px sticky action bar.
            var viewport = UiDraw.Node(root, "Viewport");
            UiDraw.Fill(viewport, 0f, 0f, 0f, 80f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var page = UiDraw.Node(viewport, "Page");
            // Top-left anchored with an explicit width: everything inside is placed from the top-left.
            page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page; scroll.horizontal = false; scroll.viewport = viewport;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            float y = 12f;
            y = Outcome(page, contentW, y, win, draw, ally, enemy, local, localPractice) + 20f;
            float colUnit = (contentW - 11f * Gap) / 12f;
            float leftW = colUnit * 4f + Gap * 3f, rightW = colUnit * 8f + Gap * 7f;
            float leftH = Contribution(page, leftW, y, local, nextGoal, localPractice);
            float rightH = Scoreboard(page, Pad + leftW + Gap, rightW, y, ally, enemy, players, local, localPractice);
            float progressionTop = y + rightH + Gap;
            float progH = Progression(page, Pad + leftW + Gap, rightW, progressionTop, footer, localPractice);
            y = Mathf.Max(y + leftH, progressionTop + progH) + 24f;
            page.sizeDelta = new Vector2(width, y);
            page.anchoredPosition = Vector2.zero;

            Actions(root, replay, home);
        }

        // ------------------------------------------------------------ outcome banner

        static float Outcome(RectTransform page, float w, float y, bool win, bool draw, int ally, int enemy, IMatchParticipant local, bool localPractice)
        {
            var box = Card(page, "Outcome", UiDraw.Hex("#141c2b", .9f), Pad, y, w, 88f);
            float x = 12f;
            var tag = UiDraw.Text(box, "Eyebrow", localPractice ? "PRACTICE MATCH // COMPLETE" : "MATCH COMPLETE // SERVER RECEIPT",
                UiFont.Heading, 12f, Primary, .1f);
            float tw = UiDraw.Width(tag);
            var tagPlate = UiDraw.Box(box, "EyebrowPlate", High, 4f);
            tagPlate.transform.SetSiblingIndex(tag.transform.GetSiblingIndex());
            TopLeft(tagPlate.rectTransform, x, 12f, tw + 8f, 18f);
            TopLeft(tag.rectTransform, x + 4f, 12f, tw + 2f, 18f);
            x += tw + 8f + 4f;
            foreach (var part in new[] { "•", "REGULATION 3V3 RELAY", "•", "SWITCHYARD ROOFTOP" })
            {
                var t = UiDraw.Text(box, "Meta", part, UiFont.MonoMedium, 11f, part == "•" ? Outline : OnVariant, .05f);
                float pw = UiDraw.Width(t);
                TopLeft(t.rectTransform, x, 12f, pw + 2f, 18f);
                x += pw + 4f;
            }
            var title = UiDraw.Text(box, "Title", draw ? "DRAW" : win ? "VICTORY" : "DEFEAT", UiFont.Heading, 40f,
                draw ? Tertiary : win ? Primary : Secondary, -.05f);
            float titleW = UiDraw.Width(title);
            TopLeft(title.rectTransform, 12f, 34f, titleW + 4f, 44f);
            // Score pill: ally TO enemy + how it ended.
            var pill = UiDraw.Box(box, "ScorePill", Mid, 8f);
            var a = UiDraw.Text(pill.transform, "Ally", ally.ToString(), UiFont.Mono, 18f, Primary, .02f);
            var to = UiDraw.Text(pill.transform, "To", "TO", UiFont.MonoMedium, 11f, Outline, .05f);
            var e = UiDraw.Text(pill.transform, "Enemy", enemy.ToString(), UiFont.Mono, 18f, Secondary, .02f);
            var how = UiDraw.Text(pill.transform, "How", "FINAL SCORE", UiFont.Heading, 12f, OnVariant, .08f);
            float px = 8f;
            foreach (var (t, gap) in new[] { (a, 4f), (to, 4f), (e, 8f), (how, 0f) })
            {
                float tw2 = UiDraw.Width(t);
                UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(px, 0f), new Vector2(tw2 + 2f, 22f));
                px += tw2 + gap;
            }
            TopLeft(pill.rectTransform, 12f + titleW + 16f, 44f, px + 8f, 30f);

            // Player card (right): name, the weapon actually carried, take-off glyph.
            var card = UiDraw.Box(box, "PlayerCard", High, 8f);
            string name = local?.DisplayName ?? "PLAYER";
            string gun = local is PlayerController pc && pc.CurrentWeapon != null ? pc.CurrentWeapon.displayName : "";
            var n = UiDraw.Text(card.transform, "Name", name, UiFont.BodyBold, 16f, OnSurface, .01f, TextAlignmentOptions.MidlineRight);
            var sub = UiDraw.Text(card.transform, "Spec", gun, UiFont.MonoMedium, 11f, Primary, .05f, TextAlignmentOptions.MidlineRight);
            float cw = Mathf.Max(UiDraw.Width(n), UiDraw.Width(sub)) + 12f + 12f + 40f + 12f;
            UiDraw.Place(card.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(cw, 56f));
            UiDraw.Place(n.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-64f, 9f), new Vector2(cw - 76f, 22f));
            UiDraw.Place(sub.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-64f, -11f), new Vector2(cw - 76f, 14f));
            var glyph = UiDraw.Box(card.transform, "Glyph", Mid, 8f);
            UiDraw.Place(glyph.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(glyph.transform, "Icon", "flight_takeoff", 24f, Primary).rectTransform);
            return y + 88f;
        }

        // ------------------------------------------------------------ contribution (left column)

        static float Contribution(RectTransform page, float w, float y, IMatchParticipant local, string nextGoal, bool localPractice)
        {
            var box = Card(page, "Contribution", Low, Pad, y, w, 100f);
            float inner = w - 24f;
            UiDraw.Place(UiDraw.Icon(box, "Icon", "verified_user", 18f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(12f, -21f), new Vector2(18f, 18f));
            TopLeft(UiDraw.Text(box, "Heading", localPractice ? "YOUR CONTRIBUTION" : "VERIFIED CONTRIBUTION", UiFont.Heading, 12f, OnSurface, .08f).rectTransform, 34f, 12f, 220f, 18f);
            var right = UiDraw.Text(box, "Impact", "DIRECT IMPACT", UiFont.MonoMedium, 11f, Primary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -21f), new Vector2(140f, 14f));

            int zone = Mathf.RoundToInt(local?.ObjectiveSeconds ?? 0f);
            var tiles = new (string label, string value, Color color, string sub)[]
            {
                ("OBJ HOLD", $"{zone / 60:00}:{zone % 60:00}", OnSurface, "Zone time"),
                ("COMBAT KD", $"{local?.Eliminations ?? 0} / {local?.Deaths ?? 0}", Primary, "Elims / deaths"),
                ("PAD CATAPULTS", (local?.PadLaunches ?? 0).ToString(), OnSurface, "Launches"),
            };
            float tileW = (inner - 2f * 8f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var tile = UiDraw.Box(box, "Tile" + i, Mid, 8f);
                TopLeft(tile.rectTransform, 12f + i * (tileW + 8f), 42f, tileW, 72f);
                TopLeft(UiDraw.Text(tile.transform, "Label", tiles[i].label, UiFont.Heading, 12f, OnVariant, .08f).rectTransform, 8f, 8f, tileW - 16f, 14f);
                TopLeft(UiDraw.Text(tile.transform, "Value", tiles[i].value, UiFont.Mono, 18f, tiles[i].color, .02f).rectTransform, 8f, 26f, tileW - 16f, 22f);
                TopLeft(UiDraw.Text(tile.transform, "Sub", tiles[i].sub, UiFont.Body, 12f, Outline, .01f).rectTransform, 8f, 50f, tileW - 16f, 16f);
            }
            float cy = 42f + 72f + 12f + 4f;
            var bests = local != null ? PersonalBests.Record(local) : "";
            if (!string.IsNullOrEmpty(bests))
                cy = Callout(box, inner, cy, "timer", Tertiary, "PERSONAL BEST", "[RECORD]", bests) + 4f;
            if (!string.IsNullOrWhiteSpace(nextGoal))
                cy = Callout(box, inner, cy, "flag", Primary, "NEXT GOAL", "", nextGoal.Replace("NEXT: ", "")) + 4f;
            var quality = UiDraw.Box(box, "Quality", Mid, 8f);
            TopLeft(quality.rectTransform, 12f, cy + 8f, inner, 26f);
            UiDraw.Place(UiDraw.Text(quality.transform, "Label", "MATCH MODE", UiFont.MonoMedium, 11f, Outline, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(160f, 14f));
                UiDraw.Place(UiDraw.Text(quality.transform, "Value", localPractice ? "PRACTICE MATCH" : "REGULATION 3V3", UiFont.Mono, 11f, Primary, .05f, TextAlignmentOptions.MidlineRight).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-8f, 0f), new Vector2(180f, 14f));
            float h = cy + 8f + 26f + 12f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        static float Callout(RectTransform box, float inner, float y, string icon, Color tone, string title, string tag, string body)
        {
            var c = UiDraw.Box(box, "Callout_" + title, UiDraw.Hex("#222a3a", .8f), 8f);
            UiDraw.Place(UiDraw.Icon(c.transform, "Icon", icon, 20f, tone).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(8f, -20f), new Vector2(20f, 20f));
            var t = UiDraw.Text(c.transform, "Title", title, UiFont.Heading, 12f, tone, .05f);
            TopLeft(t.rectTransform, 36f, 8f, UiDraw.Width(t) + 2f, 16f);
            if (!string.IsNullOrEmpty(tag))
                TopLeft(UiDraw.Text(c.transform, "Tag", tag, UiFont.MonoMedium, 11f, Outline, .05f).rectTransform, 36f + UiDraw.Width(t) + 4f, 8f, 80f, 16f);
            var text = UiDraw.Text(c.transform, "Body", body, UiFont.Body, 12f, OnVariant, .01f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            float bh = Mathf.Max(16f, text.GetPreferredValues(body, inner - 44f, 0f).y);
            TopLeft(text.rectTransform, 36f, 26f, inner - 44f, bh);
            float h = 26f + bh + 8f;
            TopLeft(c.rectTransform, 12f, y, inner, h);
            return y + h;
        }

        // ------------------------------------------------------------ scoreboard (right column)

        static float Scoreboard(RectTransform page, float x, float w, float y, int ally, int enemy,
            IReadOnlyList<IMatchParticipant> players, IMatchParticipant local, bool localPractice)
        {
            var box = Card(page, "Roster", Low, x, y, w, 100f);
            float inner = w - 24f, col = inner / 12f;
            bool localA = local == null || local.TeamId == 0;
            float cy = 12f;
            for (int group = 0; group < 2; group++)
            {
                int team = group == 0 ? (localA ? 0 : 1) : (localA ? 1 : 0);
                Color tone = group == 0 ? Primary : Secondary;
                if (group == 1)
                {
                    TopLeft(UiDraw.Box(box, "Divider", UiDraw.Hex("#3c494b", .4f)).rectTransform, 12f, cy + 4f, inner, 1f);
                    cy += 12f;
                }
                // Team header.
                TopLeft(UiDraw.Dot(box, "TeamDot_" + group, 10f, tone).rectTransform, 12f, cy + 8f, 10f, 10f);
                var teamName = UiDraw.Text(box, "TeamHeading_" + group, group == 0 ? "ALLY TEAM" : "RIVAL TEAM", UiFont.Heading, 20f, OnSurface);
                float tn = UiDraw.Width(teamName);
                TopLeft(teamName.rectTransform, 30f, cy, tn + 2f, 26f);
                var chipText = UiDraw.Text(box, "TeamPoints_" + group, (group == 0 ? "ALLIES // " : "OPPONENTS // ") + (group == 0 ? ally : enemy) + " PTS",
                    UiFont.Mono, 11f, tone, .05f, TextAlignmentOptions.Midline);
                float cw = UiDraw.Width(chipText) + 8f;
                var chip = UiDraw.Box(box, "TeamChip_" + group, Mid, 4f);
                chip.transform.SetSiblingIndex(chipText.transform.GetSiblingIndex());
                TopLeft(chip.rectTransform, 30f + tn + 8f, cy + 4f, cw, 18f);
                TopLeft(chipText.rectTransform, 30f + tn + 8f, cy + 4f, cw, 18f);
                var note = UiDraw.Text(box, "TeamNote_" + group, group == 0 ? (localPractice ? "MATCH RECORD" : "MATCH RECORD") : "EQUAL COMPETITION ROSTER",
                    UiFont.MonoMedium, 11f, Outline, .05f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(note.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -(cy + 13f)), new Vector2(260f, 14f));
                cy += 26f + 12f;
                if (group == 0)
                {
                    var labels = new (string text, float center, TextAlignmentOptions align)[]
                    {
                        ("OPERATIVE / ID", 0f, TextAlignmentOptions.MidlineLeft), ("ZONE HOLD", 5f, TextAlignmentOptions.Midline),
                        ("K / D", 7f, TextAlignmentOptions.Midline), ("PADS", 9f, TextAlignmentOptions.Midline), ("ROLE", 12f, TextAlignmentOptions.MidlineRight),
                    };
                    foreach (var l in labels) Cell(box, "Col_" + l.text, l.text, UiFont.Heading, 12f, Outline, .08f, l.align, col, l.center, cy + 4f, 22f);
                    cy += 22f + 4f;
                }
                int slot = 0;
                if (players != null)
                    foreach (var p in players)
                    {
                        if (p == null || p.TeamId != team) continue;
                        bool self = ReferenceEquals(p, local);
                        var row = UiDraw.Box(box, "Player_" + group + "_" + slot++, self ? High : Mid, 8f);
                        TopLeft(row.rectTransform, 12f, cy, inner, 52f);
                        var r = row.rectTransform;
                        float nameX = 8f;
                        if (self)
                        {
                            var badge = UiDraw.Text(r, "Badge", "YOU", UiFont.Heading, 12f, OnPrimary, .08f, TextAlignmentOptions.Midline);
                            float bw = UiDraw.Width(badge) + 12f;
                            var plate = UiDraw.Box(r, "BadgePlate", self ? Primary : Highest, 4f);
                            plate.transform.SetSiblingIndex(badge.transform.GetSiblingIndex());
                            UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, 0f), new Vector2(bw, 18f));
                            UiDraw.Place(badge.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, 0f), new Vector2(bw, 18f));
                            nameX += bw + 4f;
                        }
                        else
                        {
                            UiDraw.Place(UiDraw.Dot(r, "Dot", 8f, group == 0 ? UiDraw.Hex("#3c494b") : UiDraw.Hex("#ffb2b7", .5f)).rectTransform,
                                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, 0f), new Vector2(8f, 8f));
                            nameX += 12f;
                        }
                        string display = p.DisplayName;
                        var nm = UiDraw.Text(r, "Name", display + (!p.IsConnected ? "  (LEFT)" : ""), self ? UiFont.BodyBold : UiFont.BodySemiBold, 16f, OnSurface, .01f);
                        nm.overflowMode = TextOverflowModes.Ellipsis;
                        UiDraw.Place(nm.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, 7f), new Vector2(col * 4f - nameX, 22f));
                        UiDraw.Place(UiDraw.Text(r, "Sub", self ? "Your run" : group == 0 ? "Squad Ally" : "Rival", UiFont.MonoMedium, 11f, self ? Primary : Outline, .05f).rectTransform,
                            new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, -11f), new Vector2(col * 4f - nameX, 14f));
                        int held = Mathf.RoundToInt(p.ObjectiveSeconds);
                        RowCell(r, "Zone", $"{held / 60:00}:{held % 60:00}", UiFont.Mono, 14f, group == 0 || self ? OnSurface : OnVariant, TextAlignmentOptions.Midline, col, 5f);
                        RowCell(r, "KD", $"{p.Eliminations} / {p.Deaths}", UiFont.Mono, 14f, self ? Primary : group == 1 ? Secondary : OnVariant, TextAlignmentOptions.Midline, col, 7f);
                        RowCell(r, "Pads", p.PadLaunches.ToString(), UiFont.MonoMedium, 14f, OnSurface, TextAlignmentOptions.Midline, col, 9f);
                        RowCell(r, "Role", self ? "YOU" : group == 0 ? "ALLY" : "RIVAL", UiFont.MonoMedium, 11f, self ? Primary : OnVariant, TextAlignmentOptions.MidlineRight, col, 12f);
                        cy += 52f + 4f;
                    }
                cy += 8f;
            }
            float h = cy + 4f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        static void Cell(RectTransform box, string name, string text, UiFont font, float size, Color color, float tracking,
            TextAlignmentOptions align, float col, float center, float top, float height)
        {
            var t = UiDraw.Text(box, name, text, font, size, color, tracking, align);
            float x = 12f + 8f + (align == TextAlignmentOptions.MidlineLeft ? 0f : align == TextAlignmentOptions.MidlineRight ? center * col - 16f - 120f : center * col - 60f);
            TopLeft(t.rectTransform, x, top, 120f, height);
        }

        static void RowCell(RectTransform row, string name, string text, UiFont font, float size, Color color, TextAlignmentOptions align, float col, float center)
        {
            var t = UiDraw.Text(row, name, text, font, size, color, .03f, align);
            float x = align == TextAlignmentOptions.MidlineRight ? center * col - 8f - 120f : center * col - 60f;
            UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(120f, 18f));
        }

        // ------------------------------------------------------------ progression

        static float Progression(RectTransform page, float x, float w, float y, string footer, bool localPractice)
        {
            var box = Card(page, "Progression", Low, x, y, w, 100f);
            float inner = w - 24f;
            UiDraw.Place(UiDraw.Icon(box, "Icon", "military_tech", 18f, Tertiary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(12f, -21f), new Vector2(18f, 18f));
            TopLeft(UiDraw.Text(box, "Heading", localPractice ? "PRACTICE PROGRESSION" : "EARNED PROGRESSION BREAKDOWN", UiFont.Heading, 12f, OnSurface, .08f).rectTransform, 34f, 12f, 320f, 18f);
            var receipt = UiDraw.Text(box, "Receipt", footer ?? "", UiFont.Mono, 12f, Tertiary, .05f);
            receipt.textWrappingMode = TextWrappingModes.Normal;
            receipt.alignment = TextAlignmentOptions.TopLeft;
            float rh = Mathf.Max(16f, receipt.GetPreferredValues(receipt.text, inner, 0f).y);
            TopLeft(receipt.rectTransform, 12f, 42f, inner, rh);
            var note = UiDraw.Box(box, "SaveNote", Mid, 8f);
            TopLeft(note.rectTransform, 12f, 42f + rh + 12f, inner, 28f);
            UiDraw.Place(UiDraw.Icon(note.transform, "Lock", "lock", 14f, Outline).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(UiDraw.Text(note.transform, "Text", localPractice ? "No online XP, credits, contracts, or mastery are awarded in local practice."
                    : "Waiting for the authoritative match receipt.", UiFont.MonoMedium, 11f, Outline, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(28f, 0f), new Vector2(inner - 36f, 14f));
            float h = 42f + rh + 12f + 28f + 12f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        // ------------------------------------------------------------ sticky actions

        static void Actions(RectTransform root, Action replay, Action home)
        {
            var bar = UiDraw.Box(root, "Actions", UiDraw.Hex("#141c2b", .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 80f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UiDraw.Hex("#3c494b", .5f), 0f);
            var back = UiDraw.Tap(bar, "Home", High, 8f, () => home?.Invoke());
            var backText = UiDraw.Text(back.transform, "Caption", "Return Home", UiFont.BodySemiBold, 16f, OnSurface, .01f);
            float bw = 16f + 20f + 8f + UiDraw.Width(backText) + 16f;
            UiDraw.Place((RectTransform)back.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 48f));
            UiDraw.Place(UiDraw.Icon(back.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(44f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 22f));

            var play = UiDraw.Tap(bar, "Replay", UiDraw.Hex("#30d5e8"), 12f, () => replay?.Invoke());
            var playText = UiDraw.Text(play.transform, "Caption", "PLAY AGAIN", UiFont.Heading, 20f, UiDraw.Hex("#005862"), .02f);
            float pw = 40f + UiDraw.Width(playText) + 8f + 20f + 40f;
            UiDraw.Place((RectTransform)play.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(pw, 52f));
            UiDraw.Glow((RectTransform)play.transform, UiDraw.Hex("#7cefff", .3f), 12f, 16f);
            UiDraw.Place(playText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(UiDraw.Width(playText) + 2f, 26f));
            UiDraw.Place(UiDraw.Icon(play.transform, "Icon", "play_arrow", 20f, UiDraw.Hex("#005862")).rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-40f, 0f), new Vector2(20f, 20f));
            var ready = UiDraw.Text(bar, "ReadyStatus", "READY STATUS", UiFont.Heading, 12f, OnVariant, .08f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(ready.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad - pw - 16f, 0f), new Vector2(140f, 16f));
        }

        // ------------------------------------------------------------ helpers

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TopLeft(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TopLeft(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
    }
}
