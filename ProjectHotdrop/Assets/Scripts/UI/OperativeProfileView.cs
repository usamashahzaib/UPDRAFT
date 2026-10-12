using System;
using System.Linq;
using Hotdrop.Config;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 16 operative profile, rebuilt from its UIUX HTML (CSS px) as a scrolling page with a
    /// sticky footer. Every figure is the account's or the local save's own; what the profile feed
    /// does not carry is shown as unavailable, never estimated.
    /// </summary>
    public static class OperativeProfileView
    {
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

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleLargeDp;
        const float Pad = 24f;

        public static void SetStatus(GameObject owner, string message)
        {
            var label = owner != null ? owner.transform.Find("OperativeProfileVisual/Footer/Status")?.GetComponent<TextMeshProUGUI>() : null;
            if (label != null) label.text = message ?? "";
        }

        public static void Show(GameObject owner, ProfileDto profile, WalletDto wallet,
            ProfileStatsDto stats, OfflineChallengeController challenges, string state,
            Action back, Action appearance, bool showDetails = false, Action retry = null)
        {
            if (owner == null) return;
            // A retry action means the load failed: placeholders say "unavailable", not "loading".
            loadFailed = retry != null;
            var previous = owner.transform.Find("OperativeProfileVisual");
            if (previous != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(previous.gameObject);
                else UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }
            var root = UiDraw.Node(owner.transform, "OperativeProfileVisual");
            var streamSafeRefresh = root.gameObject.AddComponent<OperativeProfileStreamSafeRefresh>();
            streamSafeRefresh.SetRefresh(() => Show(owner, profile, wallet, stats, challenges, state,
                back, appearance, showDetails, retry));
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

            float y = 20f;
            // Title block.
            TL(UiDraw.Dot(page, "KickerDot", 8f, PrimaryC).rectTransform, Pad, y + 4f, 8f, 8f);
            TL(UiDraw.Text(page, "Kicker", UiDraw.L("profile.kicker", "Your Pilot Record"), UiFont.Mono, CaptionDp, Primary, .1f).rectTransform, Pad + 16f, y, 400f, 16f);
            var title = UiDraw.Text(page, "Title", UiDraw.L("profile.title", "Pilot Profile"), UiFont.Heading, TitleDp, OnSurface, -.02f, TextAlignmentOptions.TopLeft);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = LineSpacing(title, 48f);
            TL(title.rectTransform, Pad, y + 16f, w * .62f, 96f);
            TL(UiDraw.Icon(page, "TaglineIcon", "verified", 14f, Primary).rectTransform, Pad, y + 119f, 14f, 14f);
            // Wrapped instead of left as single-line Overflow: at this box width the full line
            // ran under the Insignia/Calibration pills to its right (reproduced at 1920 and 2400).
            // The wrapped copy reaches two lines at this box width, so the box was widened past
            // the 22 dp single-line height (40) to the full two-line height (48), and the Identity/
            // Accreditation cards below were pushed down to match - a 40-height box under two 22 dp
            // lines let the second line draw into the "PILOT NAME" heading (reproduced at 1920 and
            // 2400, summary and expanded).
            var tagline = UiDraw.Text(page, "Tagline", UiDraw.L("profile.tagline", "Verified match record  •  Private rating  •  Progress that never resets"), UiFont.Body, UiDp, OnVariant, 0f, TextAlignmentOptions.TopLeft);
            tagline.textWrappingMode = TextWrappingModes.Normal;
            tagline.lineSpacing = LineSpacing(tagline, 22f);
            TL(tagline.rectTransform, Pad + 20f, y + 117f, w * .62f, 48f);
            float pillX = Pad + w * .672f;
            // Badge names are player-chosen from the catalog and not length-bounded like the static
            // Calibration pill; clamped to the remaining page width with Ellipsis so a long one
            // truncates at the right margin instead of drawing past the visible canvas (reproduced
            // at 1920 and 2400 with "badge_relay_captain_veteran").
            Pill(page, "Insignia", "military_tech", UiDraw.L("profile.insigniaLabel", "Badge: "), StreamSafePresentation.Enabled ? UiDraw.L("common.hidden", "Hidden") : profile == null || string.IsNullOrEmpty(profile.badgeId) ? UiDraw.L("profile.insigniaNone", "None") : CosmeticLook.ItemName(profile.badgeId), pillX, y + 89f, width - pillX - Pad);
            Pill(page, "Calibration", "history", "", UiDraw.L("profile.calibration", "Private Rating"), pillX, y + 123f);
            y += 168f;

            // Identity + accreditation.
            float leftW = (w - 16f) * .5855f, rightW = w - 16f - leftW;
            Identity(page, Pad, y, leftW, profile, wallet, showDetails);
            float accreditationHeight = Accreditation(page, Pad + leftW + 16f, y, rightW, profile, showDetails);
            y += Mathf.Max(CardH, accreditationHeight) + 28f;

            // Metrics.
            UiDraw.Place(UiDraw.Icon(page, "MetricsIcon", "monitoring", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(Pad, -y - 12f), new Vector2(20f, 20f));
            var mh = UiDraw.Text(page, "MetricsHeading", showDetails ? UiDraw.L("profile.metrics.headingDetails", "More Stats") : UiDraw.L("profile.metrics.headingSummary", "Match Summary"), UiFont.Heading, UiDp, OnSurface, -.01f);
            TL(mh.rectTransform, Pad + 28f, y, UiDraw.Width(mh) + 4f, 26f);
            if (!showDetails)
            {
                var summaryLabel = UiDraw.Text(page, "SummaryLabel", UiDraw.L("profile.metrics.summaryLabel", "MATCHES  ·  WIN RATE"), UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.Midline);
                float sw = UiDraw.Width(summaryLabel) + 12f;
                var summaryPlate = UiDraw.Box(page, "SummaryPlate", High, 4f);
                summaryPlate.transform.SetSiblingIndex(summaryLabel.transform.GetSiblingIndex());
                TL(summaryPlate.rectTransform, Pad + 36f + UiDraw.Width(mh), y + 4f, sw, 18f); TL(summaryLabel.rectTransform, Pad + 36f + UiDraw.Width(mh), y + 4f, sw, 18f);
            }
            var logs = UiDraw.Text(page, "Logs", UiDraw.L("profile.metrics.telemetry", "Match Record"), UiFont.MonoMedium, CaptionDp, OnSurface, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(logs.rectTransform, new Vector2(0f, 1f), new Vector2(1f, .5f), new Vector2(Pad + w, -y - 13f), new Vector2(320f, 16f));
            y += 38f;
            int matches = stats?.eligibleMatches ?? 0, wins = stats?.wins ?? 0, draws = stats?.draws ?? 0;
            string winRate = stats == null || matches == 0 ? "—" : $"{100f * wins / matches:0.0}%";
            string winsTitle = UiDraw.L("profile.metrics.wins.title", "Win Rate");
            string verifiedTag = UiDraw.L("profile.metrics.tag.verified", "Verified");
            var metrics = new (string id, string title, string icon, string value, Color tone, string sub, string left, string right)[]
            {
                ("Matches", UiDraw.L("profile.metrics.matches.title", "Eligible Matches"), "flag", stats == null ? "—" : matches.ToString(), OnSurface, UiDraw.L("profile.metrics.matches.sub", "For Progress"), verifiedTag, UiDraw.L("profile.metrics.tag.account", "Account")),
                ("Wins", winsTitle, "north_east", winRate, Primary,
                    showDetails ? stats == null ? Missing(UiDraw.L("profile.metrics.wins.waitingForRecord", "Waiting for Record"), UiDraw.L("profile.metrics.wins.recordUnavailable", "Record Unavailable")) : string.Format(UiDraw.L("profile.metrics.wins.winsDrawsTemplate", "{0} Wins / {1} Draws"), wins, draws) : winsTitle,
                    showDetails && stats != null ? string.Format(UiDraw.L("profile.metrics.wins.winsTemplate", "{0} Wins"), wins) : verifiedTag,
                    showDetails && stats != null ? string.Format(UiDraw.L("profile.metrics.wins.defeatsTemplate", "{0} Defeats"), Mathf.Max(0, matches - wins - draws)) : UiDraw.L("profile.metrics.tag.matches", "Matches")),
                ("Draws", UiDraw.L("profile.metrics.draws.title", "Drawn Matches"), "balance", stats == null ? "—" : draws.ToString(), OnSurface, UiDraw.L("profile.metrics.draws.sub", "Even Results"), verifiedTag, UiDraw.L("profile.metrics.tag.account", "Account")),
                ("Credits", UiDraw.L("profile.metrics.credits.title", "Earned Credits"), "toll", wallet == null ? "—" : wallet.credits.ToString("N0"), Tertiary, UiDraw.L("profile.metrics.credits.sub", "For Cosmetics"), UiDraw.L("profile.metrics.tag.earnedOnly", "Earned Only"), UiDraw.L("profile.metrics.tag.noPurchases", "No Purchases")),
                ("Privacy", UiDraw.L("profile.metrics.privacy.title", "Private Rating"), "visibility_off", UiDraw.L("common.hidden", "Hidden"), OnVariant, UiDraw.L("profile.metrics.privacy.sub", "Not Displayed"), UiDraw.L("profile.metrics.tag.noPublic", "Private"), UiDraw.L("profile.metrics.tag.shaming", "No Leaderboard")),
            };
            int visibleMetricCount = showDetails ? metrics.Length : 2;
            float mw = (w - (visibleMetricCount - 1) * 16f) / visibleMetricCount;
            for (int i = 0; i < visibleMetricCount; i++)
            {
                var m = metrics[i];
                var card = Card(page, "Metric_" + m.id, Mid, Pad + i * (mw + 16f), y, mw, 160f);
                var metricTitle = UiDraw.Text(card, "Title", m.title, UiFont.Heading, CaptionDp, OnVariant, .05f);
                FitPlainText(metricTitle, m.title, mw - 50f);
                TL(metricTitle.rectTransform, 14f, 16f, mw - 50f, 16f);
                UiDraw.Place(UiDraw.Icon(card, "Icon", m.icon, 18f, m.id == "Credits" ? Tertiary : Primary).rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -24f), new Vector2(18f, 18f));
                string metricValueCopy = StreamSafePresentation.AccountProgress(m.value);
                var metricValue = UiDraw.Text(card, "Value", metricValueCopy, UiFont.Heading, TitleDp, m.tone, -.03f);
                FitPlainText(metricValue, metricValueCopy, mw - 28f);
                TL(metricValue.rectTransform, 14f, 42f, mw - 28f, 48f);
                string sourceCopy = StreamSafePresentation.Enabled ? UiDraw.L("common.hidden", "Hidden") : m.sub;
                var source = UiDraw.Text(card, "Source", sourceCopy, UiFont.Mono, CaptionDp, OnVariant, .1f);
                FitPlainText(source, sourceCopy, mw - 28f);
                TL(source.rectTransform, 14f, 96f, mw - 28f, 14f);
                float tagWidth = Mathf.Max(1f, (mw - 28f) * .5f);
                string leftCopy = StreamSafePresentation.Enabled ? UiDraw.L("common.private", "Private") : m.left;
                var left = UiDraw.Text(card, "Left", leftCopy, UiFont.Mono, CaptionDp, Primary, .05f);
                FitPlainText(left, leftCopy, tagWidth);
                TL(left.rectTransform, 14f, 128f, tagWidth, 14f);
                string rightCopy = StreamSafePresentation.Enabled ? UiDraw.L("common.private", "Private") : m.right;
                var right = UiDraw.Text(card, "Right", rightCopy, UiFont.Mono, CaptionDp, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
                FitPlainText(right, rightCopy, tagWidth);
                UiDraw.Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -135f), new Vector2(tagWidth, 14f));
            }
            y += 160f + 24f;

            var detailsToggle = UiDraw.Tap(page, "DetailsToggle", High, 8f, () =>
            {
                GameAudio.Play2D(GameCue.UiTap);
                Telemetry.Track("profile_details_toggled", ("expanded", !showDetails));
                Show(owner, profile, wallet, stats, challenges, state, back, appearance, !showDetails, retry);
            });
            TL((RectTransform)detailsToggle.transform, Pad, y, 260f, 48f);
            var toggleText = UiDraw.Text(detailsToggle.transform, "Label", showDetails ? UiDraw.L("profile.toggle.showSummary", "Show Summary") : UiDraw.L("profile.toggle.showDetails", "More Details"), UiFont.HeadingSemiBold, UiDp, Primary, .01f, TextAlignmentOptions.Midline);
            UiDraw.Fill(toggleText.rectTransform, 8f, 4f, 8f, 4f);
            y += 64f;

            // Offline accomplishments + equipped identity.
            if (showDetails)
            {
                float halfW = (w - 20f) / 2f;
                float oh = Offline(page, Pad, y, halfW, challenges);
                float eh = Loadout(page, Pad + halfW + 20f, y, halfW, profile);
                y += Mathf.Max(oh, eh) + 24f;
                var hash = UiDraw.Text(page, "Record", UiDraw.L("profile.record", "YOUR PROGRESS  •  SAVED TO YOUR ACCOUNT"), UiFont.MonoMedium, CaptionDp, OnVariant, .1f);
                TL(UiDraw.Icon(page, "RecordIcon", "shield", 14f, Primary).rectTransform, Pad, y, 14f, 14f);
                TL(hash.rectTransform, Pad + 22f, y - 1f, w, 16f);
            }
            page.sizeDelta = new Vector2(width, y + 40f);
            page.anchoredPosition = Vector2.zero;
            Footer(root, width, state, back, appearance, retry);
        }

        static bool loadFailed;

        /// <summary>Stops one-line plain copy from painting into adjacent profile fields.</summary>
        static void FitPlainText(TMP_Text text, string value, float width)
        {
            if (text == null) return;
            string copy = value ?? "";
            text.text = UiDraw.Truncate(text, copy, Mathf.Max(1f, width));
        }

        /// <summary>Text for a value that has not arrived: still loading, or failed to load.</summary>
        static string Missing(string loading, string unavailable) => loadFailed ? unavailable : loading;

        /// <summary>A negative/zero <paramref name="maxWidth"/> leaves the pill sized to its content
        /// (the existing behavior); a positive one clamps it so a long value truncates at the
        /// pill's edge instead of drawing past it.</summary>
        static void Pill(RectTransform page, string name, string icon, string label, string value, float left, float y, float maxWidth = -1f)
        {
            var pill = UiDraw.Box(page, name, Low, 8f);
            bool noLabel = string.IsNullOrEmpty(label);
            var text = UiDraw.Text(pill.transform, "Text", label + $"<color=#{UIDesignTokens.Hex(Tertiary)}>{value}</color>", UiFont.Mono, CaptionDp, OnSurface, .1f);
            if (noLabel) text.text = $"<color=#{UIDesignTokens.Hex(Primary)}>{value}</color>";
            float pw = 12f + 14f + 6f + UiDraw.Width(text) + 12f;
            float textW = UiDraw.Width(text) + 2f;
            if (maxWidth > 0f && pw > maxWidth)
            {
                pw = maxWidth;
                textW = pw - 32f;
                // TMP's Ellipsis overflow mode was tried here first and found to render the whole
                // label blank in this project's batch PlayMode captures (reproduced with both this
                // rich-text label and a plain string, and on a pre-existing unrelated call site -
                // see UiDraw.Truncate); trimming the plain value and rebuilding the tag around it
                // avoids that render path entirely.
                // Truncate measures the plain value alone; the assembled "label + <color>value</color>"
                // string still rendered a few units wider than the sum of its measured parts
                // (reproduced with the long-badge fixture at 1920 and 2400), so a small safety
                // margin is reserved rather than trusting the two separate measurements to add up
                // exactly.
                const float measurementSlack = 16f;
                text.text = noLabel
                    ? $"<color=#{UIDesignTokens.Hex(Primary)}>{UiDraw.Truncate(text, value, textW - measurementSlack)}</color>"
                    : label + $"<color=#{UIDesignTokens.Hex(Tertiary)}>{UiDraw.Truncate(text, value, textW - UiDraw.Width(text, label) - measurementSlack)}</color>";
            }
            UiDraw.Place(pill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(left, -y), new Vector2(pw, 26f));
            UiDraw.Place(UiDraw.Icon(pill.transform, "Icon", icon, 14f, string.IsNullOrEmpty(label) ? Primary : Tertiary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(14f, 14f));
            UiDraw.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(textW, 14f));
        }

        static void Identity(RectTransform page, float x, float y, float w, ProfileDto profile, WalletDto wallet, bool showDetails)
        {
            var card = Card(page, "Identity", Low, x, y, w, CardH);
            TL(UiDraw.Icon(card, "Icon", "flight", 14f, Primary).rectTransform, 20f, 20f, 14f, 14f);
            TL(UiDraw.Text(card, "Heading", UiDraw.L("profile.identity.heading", "Pilot Name"), UiFont.Mono, CaptionDp, Primary, .1f).rectTransform, 40f, 19f, 300f, 16f);
            var avatar = UiDraw.Box(card, "Avatar", High, 12f);
            TL(avatar.rectTransform, 20f, 52f, 64f, 64f);
            UiDraw.Fill(UiDraw.Icon(avatar.transform, "Icon", "shield_person", 32f, Primary).rectTransform);
            string aliasCopy = StreamSafePresentation.AccountAlias(profile?.alias ?? Missing(UiDraw.L("profile.identity.aliasSyncing", "Account Syncing"), UiDraw.L("profile.identity.aliasUnavailable", "Account Unavailable")));
            var alias = UiDraw.Text(card, "Alias", aliasCopy, UiFont.Heading, TitleDp, OnSurface, -.03f);
            FitPlainText(alias, aliasCopy, w - 360f);
            TL(alias.rectTransform, 100f, 54f, w - 360f, 38f);
            string accountCopy = StreamSafePresentation.Enabled ? UiDraw.L("profile.identity.accountPrivate", "Private Account Details")
                : profile == null ? Missing(UiDraw.L("profile.identity.accountLoading", "Account data is loading."), UiDraw.L("profile.identity.accountFailed", "Account data could not be loaded."))
                : profile.isGuest ? UiDraw.L("profile.identity.accountGuest", "Guest account  •  stored on this device") : UiDraw.L("profile.identity.accountLinked", "Linked account  •  recovery available");
            var account = UiDraw.Text(card, "Account", accountCopy, UiFont.Mono, CaptionDp, OnVariant, .05f);
            FitPlainText(account, accountCopy, w - 360f);
            TL(account.rectTransform, 100f, 92f, w - 360f, 18f);
            if (showDetails)
            {
            // Previously pinned to the card's top-right corner (y=-66), which put it on the same
            // row as the Alias text above; a fixed-width value plate hid that, but widening the
            // plate for a high credit balance (e.g. 7 digits) made the two collide. Moved to its
            // own row in the card's unused middle area instead of re-overlapping another field.
            var seals = UiDraw.Text(card, "SealsLabel", UiDraw.L("profile.identity.sealsLabel", "Earned Credits:"), UiFont.Heading, CaptionDp, Tertiary, .05f);
            // The nominal 140-wide box undersized this copy at this size (the sibling value plate
            // below already positions off the real measured width via sealsValueX, so this never
            // visually collided, but the box itself was narrower than its own label).
            TL(seals.rectTransform, 100f, 132f, UiDraw.Width(seals) + 4f, 18f);
            string sealsValue = StreamSafePresentation.AccountProgress(wallet == null ? "—" : wallet.credits.ToString("N0") + " CR");
            var sealsValueText = UiDraw.Text(card, "Value", sealsValue, UiFont.Mono, UiDp, Tertiary, .05f, TextAlignmentOptions.Midline);
            float sealsValueX = 100f + UiDraw.Width(seals) + 16f;
            float maxSealsPlateWidth = Mathf.Max(32f, w - sealsValueX - 20f);
            FitPlainText(sealsValueText, sealsValue, Mathf.Max(1f, maxSealsPlateWidth - 24f));
            float sealsPlateWidth = Mathf.Min(maxSealsPlateWidth, Mathf.Max(Mathf.Min(92f, maxSealsPlateWidth), UiDraw.Width(sealsValueText) + 24f));
            var sealPlate = UiDraw.Box(card, "SealsPlate", Mid, 4f);
            sealPlate.transform.SetSiblingIndex(sealsValueText.transform.GetSiblingIndex());
            TL(sealPlate.rectTransform, sealsValueX, 128f, sealsPlateWidth, 26f);
            TL(sealsValueText.rectTransform, sealsValueX, 128f, sealsPlateWidth, 26f);
            }
            // Previously side-by-side, each given half the row: at this card's width, the full
            // copy of either label alone needs ~560-590 units, almost the card's entire usable
            // width, so no side-by-side split has room for both. Reproduced at 1920 and 2400;
            // stacked as two full-width lines instead, which both actually fit.
            var footRule = UiDraw.Box(card, "Rule", UIDesignTokens.BorderColor(.4f));
            UiDraw.Place(footRule.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 58f), new Vector2(w - 40f, 1f));
            UiDraw.Place(UiDraw.Dot(card, "PrivacyDot", 6f, PrimaryC).rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(20f, 40f), new Vector2(6f, 6f));
            float footLabelWidth = Mathf.Max(0f, w - 54f);
            string privacyCopy = UiDraw.L("profile.identity.publicIdentity", "Your Rating Stays Private");
            var footPrivacy = UiDraw.Text(card, "Privacy", privacyCopy, UiFont.MonoMedium, CaptionDp, OnSurface, .1f);
            FitPlainText(footPrivacy, privacyCopy, footLabelWidth);
            UiDraw.Place(footPrivacy.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(34f, 40f), new Vector2(footLabelWidth, 17f));
            string verifiedCopy = UiDraw.L("profile.identity.verified", "Match Results Are Verified");
            var footVerified = UiDraw.Text(card, "Verified", verifiedCopy, UiFont.MonoMedium, CaptionDp, Primary, .1f);
            FitPlainText(footVerified, verifiedCopy, footLabelWidth);
            UiDraw.Place(footVerified.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(34f, 20f), new Vector2(footLabelWidth, 17f));
        }

        static float Accreditation(RectTransform page, float x, float y, float w, ProfileDto profile, bool showDetails)
        {
            var card = Card(page, "Progression", Low, x, y, w, CardH);
            TL(UiDraw.Icon(card, "Icon", "trending_up", 14f, Primary).rectTransform, 20f, 20f, 14f, 14f);
            const float phaseWidth = 160f;
            float headingWidth = Mathf.Max(80f, w - 240f);
            string headingCopy = UiDraw.L("profile.accreditation.heading", "Level Progress");
            var accreditationHeading = UiDraw.Text(card, "Heading", headingCopy, UiFont.Heading, CaptionDp, Primary, .05f);
            // TextMeshPro's Overflow mode paints past the RectTransform, so reserve the phase
            // chip's width and trim the heading to the actual remaining width.
            accreditationHeading.text = UiDraw.Truncate(accreditationHeading, headingCopy, headingWidth - 4f);
            TL(accreditationHeading.rectTransform, 40f, 19f, headingWidth, 18f);
            string phaseCopy = UiDraw.L("profile.accreditation.phase", "Never Resets");
            var phase = UiDraw.Text(card, "Phase", phaseCopy, UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.MidlineRight);
            phase.text = UiDraw.Truncate(phase, phaseCopy, phaseWidth - 4f);
            UiDraw.Place(phase.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -27f), new Vector2(phaseWidth, 14f));
            int level = profile != null && !StreamSafePresentation.Enabled ? Mathf.Clamp(profile.level, 1, 20) : 0;
            float frac = level / 20f;
            var ring = UiDraw.Place(UiDraw.Node(card, "Ring"), new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -99f), new Vector2(104f, 104f));
            ring.pivot = new Vector2(0f, .5f);
            UiDraw.Fill(UiDraw.Ring(ring, "Track", 104f, 6f, Highest).rectTransform);
            var fill = UiDraw.Ring(ring, "Fill", 104f, 6f, PrimaryC);
            UiDraw.Fill(fill.rectTransform);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true; fill.fillAmount = frac;
            UiDraw.Place(UiDraw.Text(ring, "Caption", UiDraw.L("profile.accreditation.ringCaption", "Starter"), UiFont.Mono, CaptionDp, OnVariant, .05f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 20f), new Vector2(90f, 12f));
            UiDraw.Place(UiDraw.Text(ring, "Lvl", profile == null ? UiDraw.L("profile.accreditation.levelMissing", "LVL —") : StreamSafePresentation.Enabled ? UiDraw.L("common.private", "Private") : string.Format(UiDraw.L("profile.accreditation.levelTemplate", "Lvl {0}"), level), UiFont.Heading, UiDp, OnSurface, -.02f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 0f), new Vector2(90f, 26f));
            UiDraw.Place(UiDraw.Text(ring, "Pct", !showDetails || profile == null || StreamSafePresentation.Enabled ? "" : $"{frac * 100f:0.0}%", UiFont.Mono, CaptionDp, Primary, .05f, TextAlignmentOptions.Midline).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -20f), new Vector2(90f, 12f));
            var levelText = UiDraw.Text(card, "Level", profile == null ? UiDraw.L("profile.accreditation.levelLabelMissing", "LEVEL —") : StreamSafePresentation.Enabled ? UiDraw.L("profile.accreditation.levelHidden", "Level Hidden") : string.Format(UiDraw.L("profile.accreditation.levelLabelTemplate", "Level {0}<color=#{1}> / 20</color>"), level, UIDesignTokens.Hex(OnVariant)), UiFont.Heading, TitleDp, OnSurface, -.03f);
            TL(levelText.rectTransform, 144f, 64f, w - 164f, 34f);
            string xpCopy = !showDetails ? "" : profile == null ? Missing(UiDraw.L("profile.accreditation.xpWaiting", "Waiting for account progress"), UiDraw.L("profile.accreditation.xpUnavailable", "Account progress unavailable")) : StreamSafePresentation.Enabled ? UiDraw.L("profile.accreditation.xpHidden", "Progress Hidden") : string.Format(UiDraw.L("profile.accreditation.xpTemplate", "{0} Total XP"), profile.profileXp.ToString("N0"));
            var xp = UiDraw.Text(card, "XP", xpCopy, UiFont.Mono, UiDp, Primary, .05f);
            FitPlainText(xp, xpCopy, w - 164f);
            TL(xp.rectTransform, 144f, 102f, w - 164f, 18f);
            var bar = UiDraw.Box(card, "XPBar", Highest, 3f);
            TL(bar.rectTransform, 144f, 129f, w - 250f, 6f);
            var bf = UiDraw.Box(bar.transform, "Fill", PrimaryC, 3f);
            UiDraw.Place(bf.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2((w - 250f) * frac, 6f));
            var guarantee = UiDraw.Box(card, "Guarantee", Lowest, 8f);
            const float guaranteeTop = 164f;
            float guaranteeTextWidth = Mathf.Max(1f, w - 96f);
            UiDraw.Place(UiDraw.Icon(guarantee.transform, "Icon", "shield", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
            var gt = UiDraw.Text(guarantee.transform, "Rule", string.Format(UiDraw.L("profile.accreditation.guarantee", "<color=#{0}>Permanent progress:</color> Your starter level never resets between seasons. Match results are verified before they add progress."), UIDesignTokens.Hex(Primary)), UiFont.Body, UiDp, OnSurface, .03f);
            gt.textWrappingMode = TextWrappingModes.Normal;
            gt.lineSpacing = LineSpacing(gt, 22f);
            float guaranteeTextHeight = Mathf.Max(88f, gt.GetPreferredValues(gt.text, guaranteeTextWidth, 0f).y);
            float guaranteeHeight = Mathf.Max(104f, guaranteeTextHeight + 20f);
            TL(guarantee.rectTransform, 20f, guaranteeTop, w - 40f, guaranteeHeight);
            UiDraw.Place(gt.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(36f, 0f), new Vector2(guaranteeTextWidth, guaranteeTextHeight));
            float cardHeight = Mathf.Max(CardH, guaranteeTop + guaranteeHeight + 8f);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardHeight);
            return cardHeight;
        }

        static float Offline(RectTransform page, float x, float y, float w, OfflineChallengeController challenges)
        {
            var card = Card(page, "OfflineAccomplishments", Low, x, y, w, 100f);
            UiDraw.Place(UiDraw.Icon(card, "Icon", "explore", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -32f), new Vector2(20f, 20f));
            // The nominal 200-wide box undersized the rendered copy at this tracking/size
            // (reproduced at 1920 and 2400, where NoWrap + Overflow let the glyphs draw left into
            // the Heading's reserved width); measured and widened to the real text width instead.
            var tag = UiDraw.Text(card, "Tag", UiDraw.L("profile.offline.tag", "This Device Only"), UiFont.Mono, CaptionDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            float tagWidth = UiDraw.Width(tag) + 4f;
            UiDraw.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-20f, -32f), new Vector2(tagWidth, 14f));
            TL(UiDraw.Text(card, "Heading", UiDraw.L("profile.offline.heading", "Offline Achievements"), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 48f, 20f, Mathf.Max(80f, w - 48f - tagWidth - 20f), 26f);
            // A full sentence left in single-line NoWrap drew hundreds of units past its own card
            // (reproduced at 1920 and 2400); wrapped like the Fair Play body copy, with the card's
            // row layout shifted down to the copy's actual wrapped height instead of an assumed
            // one-line 20.
            var note = UiDraw.Text(card, "Note", UiDraw.L("profile.offline.note", "Solo training circuits and rooftop trials. Device-local medals; no online rewards."), UiFont.Body, UiDp, OnVariant);
            note.textWrappingMode = TextWrappingModes.Normal;
            note.lineSpacing = LineSpacing(note, 20f);
            float noteWidth = w - 40f;
            float noteHeight = Mathf.Max(20f, note.GetPreferredValues(note.text, noteWidth, 0f).y);
            TL(note.rectTransform, 20f, 52f, noteWidth, noteHeight);
            var savedLocal = challenges == null ? new LocalChallengeSave() : null;
            float cy = 52f + noteHeight + 10f;
            var icons = new[] { "military_tech", "apartment", "lock" };
            var names = new[]
            {
                UiDraw.L("profile.offline.chapterName.0", "First Flight"),
                UiDraw.L("profile.offline.chapterName.1", "Rooftop Runner"),
                UiDraw.L("profile.offline.chapterName.2", "Relay Captain"),
            };
            for (int i = 0; i < 3; i++)
            {
                var chapter = (ChallengeChapter)i;
                var authored = challenges?.config?.GetChallengesForChapter(chapter)
                    ?? OfflineChallengeConfig.GetAuthoredEntries().Where(e => e.chapter == chapter).ToList();
                int total = authored?.Count ?? 0;
                int completed = authored?.Count(e => challenges != null
                    ? challenges.RecordForCurrentContext(e.id)?.isCompleted == true
                    : savedLocal.Data.records.Any(r => r.challengeId == e.id && r.isCompleted && r.challengeVersion == e.version)) ?? 0;
                int pct = total > 0 ? Mathf.RoundToInt(100f * completed / total) : 0;
                var row = UiDraw.Box(card, "Chapter_" + i, Lowest, 8f);
                TL(row.rectTransform, 20f, cy, w - 40f, 76f);
                var plate = UiDraw.Box(row.transform, "IconPlate", High, 8f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(48f, 48f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", completed > 0 || i == 0 ? icons[i] == "lock" ? "flag" : icons[i] : "lock", 22f, completed == total && total > 0 ? Tertiary : Primary).rectTransform);
                var title = UiDraw.Text(row.transform, "Name", string.Format(UiDraw.L("profile.offline.chapterTemplate", "Chapter {0}: {1}"), i + 1, names[i]), UiFont.Heading, UiDp, OnSurface, -.01f);
                TL(title.rectTransform, 72f, 12f, UiDraw.Width(title) + 2f, 24f);
                var pctText = UiDraw.Text(row.transform, "Pct", $"{pct}%", UiFont.Mono, CaptionDp, UIDesignTokens.TextOn(Tertiary), 0f, TextAlignmentOptions.Midline);
                float pw = UiDraw.Width(pctText) + 10f;
                var pctPlate = UiDraw.Box(row.transform, "PctPlate", pct == 100 ? Tertiary : PrimaryC, 3f);
                pctPlate.transform.SetSiblingIndex(pctText.transform.GetSiblingIndex());
                TL(pctPlate.rectTransform, 76f + UiDraw.Width(title), 15f, pw, 18f); TL(pctText.rectTransform, 76f + UiDraw.Width(title), 15f, pw, 18f);
                TL(UiDraw.Text(row.transform, "Count", total == 0 ? UiDraw.L("profile.offline.notLoaded", "Not Loaded") : string.Format(UiDraw.L("profile.offline.completedTemplate", "{0}/{1} Completed"), completed, total), UiFont.Mono, CaptionDp, completed == total && total > 0 ? Tertiary : Primary, .05f).rectTransform, 72f, 38f, w - 160f, 14f);
                UiDraw.Place(UiDraw.Icon(row.transform, "State", completed == total && total > 0 ? "check_circle" : completed > 0 ? "sync" : "play_circle", 20f, Primary).rectTransform,
                    new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(20f, 20f));
                cy += 76f + 10f;
            }
            float h = cy + 10f;
            card.sizeDelta = new Vector2(w, h);
            return h;
        }

        static float Loadout(RectTransform page, float x, float y, float w, ProfileDto profile)
        {
            var card = Card(page, "SavedIdentity", Low, x, y, w, 100f);
            UiDraw.Place(UiDraw.Icon(card, "Icon", "auto_awesome", 20f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -32f), new Vector2(20f, 20f));
            TL(UiDraw.Text(card, "Heading", UiDraw.L("profile.loadout.heading", "Equipped Items"), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 48f, 20f, w - 68f, 26f);
            TL(UiDraw.Text(card, "Policy", UiDraw.L("profile.loadout.policy", "Cosmetics do not affect gameplay."), UiFont.Body, UiDp, OnVariant).rectTransform, 20f, 52f, w - 40f, 20f);
            string syncing = UiDraw.L("profile.loadout.syncing", "Syncing"), unavailable = UiDraw.L("profile.loadout.unavailable", "Unavailable");
            var slots = new[]
            {
                ("Body", "accessibility_new", UiDraw.L("profile.loadout.bodyLabel", "Body Style"), profile == null ? Missing(syncing, unavailable) : CosmeticLook.DisplayName(profile.bodyStyleId), UiDraw.L("profile.loadout.bodyDetail", "Body style")),
                ("Wrap", "colorize", UiDraw.L("profile.loadout.wrapLabel", "Weapon Wrap"), profile == null ? Missing(syncing, unavailable) : CosmeticLook.DisplayName(profile.weaponWrapId), UiDraw.L("profile.loadout.wrapDetail", "ARC rifle wrap")),
                ("Badge", "badge", UiDraw.L("profile.loadout.badgeLabel", "Profile Badge"), profile == null ? Missing(syncing, unavailable) : string.IsNullOrEmpty(profile.badgeId) ? UiDraw.L("profile.loadout.badgeNone", "None") : CosmeticLook.DisplayName(profile.badgeId), UiDraw.L("profile.loadout.badgeDetail", "Flight card badge")),
            };
            float cy = 82f;
            foreach (var s in slots)
            {
                var row = UiDraw.Box(card, s.Item1, Lowest, 8f);
                TL(row.rectTransform, 20f, cy, w - 40f, 76f);
                var plate = UiDraw.Box(row.transform, "IconPlate", High, 8f);
                UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(48f, 48f));
                UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", s.Item2, 24f, Primary).rectTransform);
                TL(UiDraw.Text(row.transform, "Label", s.Item3, UiFont.Heading, CaptionDp, OnVariant, .05f).rectTransform, 72f, 12f, w - 120f, 14f);
                // The catalog display name (e.g. "Weapon wrap: Midnight carbon fiber finish") can
                // run past this row's value width (reproduced at 1920 with the long-wrap fixture);
                // measured and trimmed like the equipped-wrap value elsewhere in this screen.
                var value = UiDraw.Text(row.transform, "Value", s.Item4, UiFont.BodySemiBold, UiDp, OnSurface);
                FitPlainText(value, s.Item4, w - 120f);
                TL(value.rectTransform, 72f, 28f, w - 120f, 26f);
                TL(UiDraw.Text(row.transform, "Detail", s.Item5, UiFont.Mono, CaptionDp, Primary, .05f).rectTransform, 72f, 54f, w - 120f, 14f);
                cy += 76f + 10f;
            }
            float h = cy + 10f;
            card.sizeDelta = new Vector2(w, h);
            return h;
        }

        static void Footer(RectTransform root, float width, string state, Action back, Action appearance, Action retry)
        {
            var bar = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(Lowest, .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 76f); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UIDesignTokens.BorderColor(.5f), 0f);
            var home = UiDraw.Tap(bar, "Back", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var homeText = UiDraw.Text(home.transform, "Label", UiDraw.L("profile.footer.home", "Return to Home"), UiFont.HeadingSemiBold, UiDp, OnSurface, .01f);
            float hw = 20f + 20f + 12f + UiDraw.Width(homeText) + 24f;
            UiDraw.Place((RectTransform)home.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(hw, 52f));
            UiDraw.Place(UiDraw.Icon(home.transform, "Icon", "home", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(homeText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(homeText) + 2f, 26f));
            var status = UiDraw.Text(bar, "Status", state ?? "", UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            status.overflowMode = TextOverflowModes.Ellipsis;
            var edit = UiDraw.Tap(bar, "Appearance", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); appearance?.Invoke(); });
            var editText = UiDraw.Text(edit.transform, "Label", UiDraw.L("profile.footer.appearance", "Edit Appearance"), UiFont.HeadingSemiBold, UiDp, Primary, .01f);
            float ew = 20f + 20f + 12f + UiDraw.Width(editText) + 24f;
            UiDraw.Place((RectTransform)edit.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(ew, 52f));
            UiDraw.Place(UiDraw.Icon(edit.transform, "Icon", "checkroom", 20f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(editText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(editText) + 2f, 26f));
            float rightEdge = Pad + ew + 16f;
            // A failed load keeps its retry in this sticky footer: the page covers the shared
            // menu rows, so a retry placed there would be hidden.
            if (retry != null) rightEdge += RetryButton(bar, rightEdge, retry) + 12f;
            float statusX = Pad + hw + 16f;
            UiDraw.Place(status.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(statusX, 0f),
                new Vector2(Mathf.Clamp(width - statusX - rightEdge, 0f, 500f), 16f));
        }

        /// <summary>Places a 52 px RETRY action <paramref name="rightInset"/> from the bar's right edge; returns its width.</summary>
        static float RetryButton(RectTransform bar, float rightInset, Action retry)
        {
            var again = UiDraw.Tap(bar, "Retry", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); retry(); });
            var label = UiDraw.Text(again.transform, "Label", UiDraw.L("common.retry", "Retry"), UiFont.HeadingSemiBold, UiDp, Primary, .01f);
            float rw = 20f + 20f + 12f + UiDraw.Width(label) + 24f;
            UiDraw.Place((RectTransform)again.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-rightInset, 0f), new Vector2(rw, 52f));
            UiDraw.Place(UiDraw.Icon(again.transform, "Icon", "refresh", 20f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(label) + 2f, 26f));
            return rw;
        }

        const float CardH = 272f;

        /// <summary>CSS line-height in px as TMP line spacing over the face's natural line height.</summary>
        static float LineSpacing(TMP_Text text, float lineHeight)
        {
            float px = text.fontSize;
            float natural = text.font != null && text.font.faceInfo.pointSize > 0
                ? text.font.faceInfo.lineHeight / text.font.faceInfo.pointSize * px : px * 1.3f;
            return (lineHeight - natural) / px * 100f;
        }

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TL(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
    }

    /// <summary>Rebuilds the profile from its existing snapshot when local privacy changes.</summary>
    internal sealed class OperativeProfileStreamSafeRefresh : MonoBehaviour
    {
        Action refresh;

        public void SetRefresh(Action action) => refresh = action;

        void OnEnable()
        {
            StreamSafePresentation.Changed -= OnChanged;
            StreamSafePresentation.Changed += OnChanged;
        }

        void OnDisable() => StreamSafePresentation.Changed -= OnChanged;
        void OnDestroy() => refresh = null;

        void OnChanged()
        {
            if (isActiveAndEnabled) refresh?.Invoke();
        }
    }
}
