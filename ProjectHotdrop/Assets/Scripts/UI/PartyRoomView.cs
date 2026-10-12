using System;
using System.Linq;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 05 party room, rebuilt from its UIUX HTML (CSS px). The same room is shown before a
    /// party exists: your seat plus two open seats, with create and join in the code pill. Members,
    /// readiness and the code are the backend's; nothing here is sample data.
    /// </summary>
    public static class PartyRoomView
    {
        static Color Ink => UIDesignTokens.Background;
        static Color Panel => UIDesignTokens.SurfacePanel;
        static Color Edge => UIDesignTokens.SurfaceStructural;
        static Color Cyan => UIDesignTokens.PrimaryAction;
        static Color Gold => UIDesignTokens.ObjectiveGold;
        static Color Text1 => UIDesignTokens.TextPrimary;
        static Color Text2 => UIDesignTokens.TextSecondary;
        static Color Muted => UIDesignTokens.TextMuted;

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleMediumDp;

        const float CardW = 368f, CardH = 360f, CardGap = 24f;
        // Design bars (72 px header around the 44 px HOME, 77 px footer around 48 px actions, 38 px join
        // pill around 28 px field/JOIN) grow with the 48 dp floor so raised controls stay on screen.
        static float HeaderHeight => Mathf.Max(UiMetrics.BarHeight(72f, 44f), JoinPillHeight + 12f);
        static float FooterHeight => UiMetrics.BarHeight(77f, 48f);
        static float JoinPillHeight => UiMetrics.BarHeight(38f, 28f);
        static float IconButtonSize => Mathf.Max(24f, UiMetrics.MinTouchUnits);
        static GameObject queueOwner;
        static Button queueButton;
        static bool queueLeaderReady;
        static bool queueAvailable = true;
        static TextMeshProUGUI queueStatusText;
        static string queueReadyStatus;

        public static void Clear(GameObject owner)
        {
            if (owner == null) return;
            if (owner == queueOwner)
            {
                queueOwner = null;
                queueButton = null;
                queueLeaderReady = false;
                queueAvailable = true;
                queueStatusText = null;
                queueReadyStatus = null;
            }
            var old = owner.transform.Find("PartyRoomVisual");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
        }

        public static void SetQueueAvailability(GameObject owner, bool available, string unavailableStatus = null)
        {
            if (owner == null || owner != queueOwner) return;
            queueAvailable = available;
            ApplyQueueButtonState();
            if (queueLeaderReady && queueStatusText != null)
                queueStatusText.text = available ? queueReadyStatus : unavailableStatus ?? UiDraw.L("online.queueStatus", "Queue Status");
        }

        public static void SetStatus(GameObject owner, string message)
        {
            var visual = owner != null ? owner.transform.Find("PartyRoomVisual") : null;
            var label = visual != null ? visual.Find("Footer/RoomStatus")?.GetComponent<TextMeshProUGUI>() : null;
            if (label != null)
            {
                label.richText = false;
                label.text = message ?? "";
            }
        }

        /// <summary>No party yet: your seat, two open seats, join-by-code in the header, CREATE PARTY.</summary>
        public static TMP_InputField BuildEntry(GameObject owner, string alias, Action back, Action create, Action<string> join)
        {
            Clear(owner);
            if (owner == null) return null;
            var root = Frame(owner, back);
            var header = (RectTransform)root.Find("Header");
            // Code pill in its join form: an 8-character field and JOIN.
            var pill = UiDraw.Box(header, "PartyCode", Panel, 8f, Edge);
            UiDraw.Place(pill.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(330f, JoinPillHeight));
            UiDraw.Place(UiDraw.Text(pill.transform, "CodeHeading", UiDraw.L("partyRoom.pill.joinCodeLabel", "Join Code:"), UiFont.HeadingSemiBold, CaptionDp, Muted, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(80f, 16f));
            var input = CodeField(pill.transform);
            var joinButton = UiDraw.Tap(pill.transform, "JoinCode", UIDesignTokens.WithAlpha(Cyan, .15f), 6f, () => join?.Invoke(input.text), UIDesignTokens.WithAlpha(Cyan, .4f));
            UiDraw.Place((RectTransform)joinButton.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-5f, 0f), new Vector2(60f, 28f));
            UiDraw.Fill(UiDraw.Text(joinButton.transform, "Label", UiDraw.L("partyRoom.action.join", "Join"), UiFont.Heading, CaptionDp, Cyan, .05f, TextAlignmentOptions.Midline).rectTransform);

            var row = Row(root);
            Seat(row, 0, alias ?? UiDraw.L("partyRoom.seat.you", "You"), false, false, true, false, null);
            Seat(row, 1, null, false, false, false, false, null);
            Seat(row, 2, null, false, false, false, false, null);

            var footer = Footer(root, UiDraw.L("partyRoom.footer.notInParty", "Not in a Party"),
                UiDraw.L("partyRoom.footer.entryStatus", "Create a squad, or enter a friend's 8-character code."), false);
            Primary(footer, "CreateParty", "add", UiDraw.L("partyRoom.action.createParty", "Create Party"), create);
            InstallDensityReflow(root, hasParty: false);
            return input;
        }

        public static void Build(GameObject owner, PartyDto party, string playerId, bool isLeader, bool isReady,
            bool allReady, Action back, Action toggleReady, Action leave, Action refresh, Action queue,
            bool queueAvailable = true)
        {
            Clear(owner);
            if (owner == null || party == null) return;
            var root = Frame(owner, back);
            var header = (RectTransform)root.Find("Header");

            // Party code pill: label, the code split 4-4, copy and refresh.
            string code = party.code ?? "";
            string shown = StreamSafePresentation.Enabled ? StreamSafePresentation.PrivateCode(code)
                : code.Length == 8 ? code.Substring(0, 4) + "-" + code.Substring(4) : code;
            var pill = UiDraw.Box(header, "PartyCode", Panel, 8f, Edge);
            var heading = UiDraw.Text(pill.transform, "CodeHeading", UiDraw.L("partyRoom.pill.partyCodeLabel", "Party Code:"), UiFont.HeadingSemiBold, CaptionDp, Muted, .05f);
            var value = UiDraw.Text(pill.transform, "CodeValue", shown, UiFont.Mono, UiDp, Cyan, .1f);
            float hw = UiDraw.Width(heading), vw = UiDraw.Width(value);
            // Icon buttons are 24 px in the design and at least 48 dp to touch; the pill makes room.
            float ib = IconButtonSize;
            float pw = 14f + hw + 10f + vw + 10f + ib + 4f + ib + 10f;
            UiDraw.Place(pill.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(pw, UiMetrics.BarHeight(38f, 24f)));
            UiDraw.Place(heading.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f), new Vector2(hw + 2f, 16f));
            UiDraw.Place(value.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f + hw, 0f), new Vector2(vw + 2f, 22f));
            IconButton(pill.transform, "CopyCode", "content_copy", -(10f + ib + 4f), () => GUIUtility.systemCopyBuffer = code);
            IconButton(pill.transform, "RefreshParty", "refresh", -10f, refresh);

            var ordered = (party.members ?? Array.Empty<PartyMemberDto>())
                .OrderByDescending(m => m != null && string.Equals(m.playerId, party.leaderId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var row = Row(root);
            var memberAliases = new string[3];
            var aliasLabels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var member = i < ordered.Length ? ordered[i] : null;
                bool leader = member != null && string.Equals(member.playerId, party.leaderId, StringComparison.OrdinalIgnoreCase);
                bool self = member != null && string.Equals(member.playerId, playerId, StringComparison.OrdinalIgnoreCase);
                Seat(row, i, member?.alias, leader, member != null && member.isReady, self, member != null, self ? toggleReady : null);
                memberAliases[i] = member?.alias;
                aliasLabels[i] = row.Find("Member_" + i + "/Alias")?.GetComponent<TextMeshProUGUI>();
            }
            var privacyDisplay = root.gameObject.AddComponent<PrivacyDisplay>();
            privacyDisplay.Bind(value, code, aliasLabels, memberAliases);

            int members = party.memberIds?.Count ?? ordered.Length;
            int ready = party.readyIds?.Count ?? ordered.Count(m => m != null && m.isReady);
            var footer = Footer(root, string.Format(UiDraw.L("partyRoom.footer.readySummaryTemplate", "{0}/{1} Squad Ready"), ready, members),
                allReady && !queueAvailable ? UiDraw.L("home.status.queuePaused", "Queue Paused")
                : allReady ? UiDraw.L("partyRoom.footer.statusAllReady", "All members confirmed. Ready to initiate 3v3 search.")
                : isLeader ? UiDraw.L("partyRoom.footer.statusLeaderWaiting", "Waiting for squad members to ready up.")
                : isReady ? UiDraw.L("partyRoom.footer.statusReadyWaitingLeader", "Ready. The party leader starts the search.")
                : UiDraw.L("partyRoom.footer.statusNotReady", "Set yourself ready for the party leader."), allReady);
            var leaveButton = UiDraw.Tap(footer, "LeaveParty", Panel, 12f, leave, Edge);
            var leaveText = UiDraw.Text(leaveButton.transform, "Label", UiDraw.L("partyRoom.action.leaveParty", "Leave Party"), UiFont.Heading, CaptionDp, Text2, .05f);
            float lw = 20f + 16f + 8f + UiDraw.Width(leaveText) + 20f;
            UiDraw.Place((RectTransform)leaveButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(lw, 48f));
            UiDraw.Place(UiDraw.Icon(leaveButton.transform, "Icon", "logout", 16f, Text2).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(leaveText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(44f, 0f), new Vector2(UiDraw.Width(leaveText) + 2f, 16f));
            var summary = footer.Find("ReadySummary") as RectTransform;
            var status = footer.Find("RoomStatus") as RectTransform;
            summary.anchoredPosition = new Vector2(32f + lw + 16f, summary.anchoredPosition.y);
            status.anchoredPosition = new Vector2(32f + lw + 16f, status.anchoredPosition.y);
            if (isLeader)
            {
                queueOwner = owner;
                PartyRoomView.queueAvailable = queueAvailable;
                queueStatusText = status != null ? status.GetComponent<TextMeshProUGUI>() : null;
                queueReadyStatus = UiDraw.L("partyRoom.footer.statusAllReady", "All members confirmed. Ready to initiate 3v3 search.");
                queueButton = Primary(footer, "QueueParty", "play_arrow", allReady ? UiDraw.L("partyRoom.action.playRelay", "Play Relay (Start Search)") : UiDraw.L("partyRoom.action.waitingForSquad", "Waiting for Squad"), allReady ? queue : null);
                queueLeaderReady = allReady;
                ApplyQueueButtonState();
            }
            InstallDensityReflow(root, hasParty: true);
        }

        static void ApplyQueueButtonState()
        {
            if (queueButton == null) return;
            bool canQueue = queueLeaderReady && queueAvailable;
            queueButton.interactable = canQueue;
            var image = queueButton.GetComponent<Image>();
            if (image != null) image.color = canQueue ? Cyan : Panel;
            foreach (var label in queueButton.GetComponentsInChildren<TextMeshProUGUI>())
                label.color = canQueue ? Ink : Muted;
            var glow = queueButton.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(canQueue);
        }

        // ------------------------------------------------------------ shared frame

        sealed class DensityReflow : MonoBehaviour
        {
            internal bool hasParty;
            internal Vector3 lastScreen;
            internal float lastTextScale;
            internal bool initialized;

            void LateUpdate()
            {
                Vector3 screen = UiMetrics.CurrentScreen;
                if (!initialized || screen != lastScreen || !Mathf.Approximately(lastTextScale, UiMetrics.TextScale))
                    Reflow((RectTransform)transform, hasParty, this);
            }
        }

        sealed class PrivacyDisplay : MonoBehaviour
        {
            TextMeshProUGUI codeValue;
            string partyCode;
            TextMeshProUGUI[] aliasLabels;
            string[] aliases;

            void OnEnable()
            {
                StreamSafePresentation.Changed += Refresh;
                Refresh();
            }

            void OnDisable() => StreamSafePresentation.Changed -= Refresh;

            internal void Bind(TextMeshProUGUI codeLabel, string code, TextMeshProUGUI[] memberLabels, string[] memberAliases)
            {
                codeValue = codeLabel;
                partyCode = code ?? "";
                aliasLabels = memberLabels;
                aliases = memberAliases;
                Refresh();
            }

            void Refresh()
            {
                if (codeValue != null)
                    codeValue.text = StreamSafePresentation.Enabled ? StreamSafePresentation.PrivateCode(partyCode)
                        : partyCode.Length == 8 ? partyCode.Substring(0, 4) + "-" + partyCode.Substring(4) : partyCode;

                if (aliasLabels == null || aliases == null) return;
                for (int i = 0; i < aliasLabels.Length && i < aliases.Length; i++)
                {
                    if (aliasLabels[i] == null) continue;
                    aliasLabels[i].text = aliases[i] == null
                        ? UiDraw.L("partyRoom.seat.awaitingPlayer", "Awaiting Player")
                        : StreamSafePresentation.PlayerName(aliases[i], i);
                }
            }
        }

        static void InstallDensityReflow(RectTransform root, bool hasParty)
        {
            var watcher = root.gameObject.AddComponent<DensityReflow>();
            watcher.hasParty = hasParty;
            Reflow(root, hasParty, watcher);
        }

        static void Reflow(RectTransform root, bool hasParty, DensityReflow watcher)
        {
            if (root == null) return;
            var header = root.Find("Header") as RectTransform;
            if (header != null) header.sizeDelta = new Vector2(header.sizeDelta.x, HeaderHeight);
            var footer = root.Find("Footer") as RectTransform;
            if (footer != null) footer.sizeDelta = new Vector2(footer.sizeDelta.x, FooterHeight);
            if (root.Find("Header/HomeButton") is RectTransform home)
                home.sizeDelta = new Vector2(home.sizeDelta.x, Mathf.Max(44f, UiMetrics.MinTouchUnits));

            var pill = root.Find("Header/PartyCode") as RectTransform;
            if (pill != null && !hasParty)
            {
                float inputWidth = Mathf.Max(158f, UiMetrics.MinTouchUnits);
                float joinSize = Mathf.Max(60f, UiMetrics.MinTouchUnits);
                float controlHeight = Mathf.Max(28f, UiMetrics.MinTouchUnits);
                float inputX = 98f;
                float pillWidth = Mathf.Max(330f, inputX + inputWidth + 10f + joinSize + 10f);
                pill.sizeDelta = new Vector2(pillWidth, JoinPillHeight);
                if (pill.Find("CodeInput") is RectTransform input)
                {
                    input.anchoredPosition = new Vector2(inputX, 0f);
                    input.sizeDelta = new Vector2(inputWidth, controlHeight);
                }
                if (pill.Find("JoinCode") is RectTransform join)
                {
                    join.anchoredPosition = new Vector2(-5f, 0f);
                    join.sizeDelta = new Vector2(joinSize, controlHeight);
                }
            }
            else if (pill != null)
            {
                var heading = pill.Find("CodeHeading")?.GetComponent<TextMeshProUGUI>();
                var value = pill.Find("CodeValue")?.GetComponent<TextMeshProUGUI>();
                if (heading != null && value != null)
                {
                    float headingWidth = UiDraw.Width(heading);
                    float valueWidth = UiDraw.Width(value);
                    float button = IconButtonSize;
                    pill.sizeDelta = new Vector2(14f + headingWidth + 10f + valueWidth + 10f + button * 2f + 14f,
                        UiMetrics.BarHeight(38f, 24f));
                    heading.rectTransform.anchoredPosition = new Vector2(14f, 0f);
                    heading.rectTransform.sizeDelta = new Vector2(headingWidth + 2f, 16f);
                    value.rectTransform.anchoredPosition = new Vector2(24f + headingWidth, 0f);
                    value.rectTransform.sizeDelta = new Vector2(valueWidth + 2f, 22f);
                    PlaceIconButton(pill, "CopyCode", -(10f + button + 4f), button);
                    PlaceIconButton(pill, "RefreshParty", -10f, button);
                }
            }

            UiFloor.Apply(root);
            watcher.lastScreen = UiMetrics.CurrentScreen;
            watcher.lastTextScale = UiMetrics.TextScale;
            watcher.initialized = true;
        }

        static void PlaceIconButton(Transform parent, string name, float x, float size)
        {
            if (parent.Find(name) is RectTransform button)
            {
                button.anchorMin = new Vector2(1f, .5f);
                button.anchorMax = new Vector2(1f, .5f);
                button.pivot = new Vector2(1f, .5f);
                button.anchoredPosition = new Vector2(x, 0f);
                button.sizeDelta = new Vector2(size, size);
            }
        }

        static RectTransform Frame(GameObject owner, Action back)
        {
            var root = UiDraw.Node(owner.transform, "PartyRoomVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Ink).rectTransform);
            var photo = UiDraw.Node(root, "RooftopBackdrop").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(photo.rectTransform);
            photo.texture = UiPresentationProfile.LoadRooftopArtwork();
            photo.color = new Color(.75f, .75f, .76f, .35f);
            photo.raycastTarget = false;
            UiDraw.VerticalGradient(root, "Shade", Ink, new Color(Ink.r, Ink.g, Ink.b, .8f), new Color(Ink.r, Ink.g, Ink.b, .9f));
            var dots = UiDraw.Node(root, "LocationGrid").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(dots.rectTransform);
            dots.texture = DotTexture();
            dots.uvRect = new Rect(0, 0, 1600f / 32f, 720f / 32f);
            dots.color = new Color(Cyan.r, Cyan.g, Cyan.b, .15f);
            dots.raycastTarget = false;

            var header = UiDraw.Box(root, "Header", UIDesignTokens.WithAlpha(Ink, .8f)).rectTransform;
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1f);
            header.sizeDelta = new Vector2(0f, HeaderHeight); header.anchoredPosition = Vector2.zero;
            var rule = UiDraw.Box(header, "HeaderRule", UIDesignTokens.WithAlpha(Edge, .4f)).rectTransform;
            rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1f, 0f); rule.pivot = new Vector2(.5f, 0f); rule.sizeDelta = new Vector2(0f, 1f);
            var home = UiDraw.Tap(header, "HomeButton", Panel, 8f, back, Edge);
            var homeText = UiDraw.Text(home.transform, "Label", UiDraw.L("partyRoom.header.home", "Home"), UiFont.Heading, CaptionDp, Text1, .05f);
            float hw = 16f + 18f + 8f + UiDraw.Width(homeText) + 16f;
            UiDraw.Place((RectTransform)home.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(hw, 44f));
            UiDraw.Place(UiDraw.Icon(home.transform, "Icon", "arrow_back", 18f, Text1).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(homeText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(UiDraw.Width(homeText) + 2f, 16f));
            float x = 32f + hw + 16f;
            UiDraw.Place(UiDraw.Box(header, "Divider", UIDesignTokens.WithAlpha(Edge, .5f)).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(1f, 24f));
            x += 17f;
            var title = UiDraw.Text(header, "PartyTitle", UiDraw.L("partyRoom.header.title", "Party Room"), UiFont.Heading, TitleDp, Text1, -.025f);
            float tw = UiDraw.Width(title);
            UiDraw.Place(title.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 9f), new Vector2(tw + 2f, 28f));
            var chipText = UiDraw.Text(header, "SquadMax", UiDraw.L("partyRoom.header.squadMax", "3 Squad Max"), UiFont.Mono, CaptionDp, Cyan, 0f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chipText) + 18f;
            var chip = UiDraw.Box(header, "SquadMaxChip", UIDesignTokens.WithAlpha(Cyan, .15f), 4f, UIDesignTokens.WithAlpha(Cyan, .4f));
            chip.transform.SetSiblingIndex(chipText.transform.GetSiblingIndex());
            UiDraw.Place(chip.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x + tw + 8f, 9f), new Vector2(cw, 20f));
            UiDraw.Place(chipText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x + tw + 8f, 9f), new Vector2(cw, 20f));
            UiDraw.Place(UiDraw.Text(header, "PartySubtitle", UiDraw.L("partyRoom.header.subtitle", "Play together in 3v3 Relay matches"), UiFont.Body, CaptionDp, Text2).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, -13f), new Vector2(300f, 16f));
            return root;
        }

        static RectTransform Row(RectTransform root) =>
            UiDraw.Place(UiDraw.Node(root, "Seats"), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 4f),
                new Vector2(CardW * 3f + CardGap * 2f, CardH));

        static void Seat(RectTransform row, int index, string alias, bool leader, bool ready, bool self, bool present, Action toggleReady)
        {
            const float pad = 21f;
            bool open = alias == null;
            var shell = UiDraw.ArtSliced(row, "Member_" + index, self ? "prep_card_selected" : "prep_card", 24f);
            var card = UiDraw.Place(shell.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2((index - 1) * (CardW + CardGap), 0f), new Vector2(CardW, CardH));
            if (open) shell.color = new Color(1f, 1f, 1f, .6f);

            // Header: role with its glyph, readiness chip.
            string role = open ? UiDraw.L("partyRoom.seat.roleOpen", "Open Seat")
                : leader ? UiDraw.L("partyRoom.seat.roleLeader", "Party Leader")
                : !present ? UiDraw.L("partyRoom.seat.roleYourSeat", "Your Seat")
                : UiDraw.L("partyRoom.seat.roleMember", "Member");
            Color roleColor = leader ? Gold : Text2;
            UiDraw.Place(UiDraw.Icon(card, "RoleIcon", open ? "person_add" : leader ? "stars" : "person", 18f, leader ? Gold : Muted).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad, -32f), new Vector2(18f, 18f));
            UiDraw.Place(UiDraw.Text(card, "Role", role, UiFont.Heading, CaptionDp, roleColor, .05f).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(pad + 26f, -32f), new Vector2(200f, 16f));
            string badge = open ? UiDraw.L("partyRoom.seat.badgeOpen", "Open")
                : !present ? UiDraw.L("partyRoom.seat.badgeNotInParty", "Not in Party")
                : ready ? UiDraw.L("partyRoom.seat.badgeReady", "Ready") : UiDraw.L("partyRoom.seat.badgeNotReady", "Not Ready");
            var badgeText = UiDraw.Text(card, "Readiness", badge, UiFont.Mono, CaptionDp, ready ? Cyan : Text2, 0f, TextAlignmentOptions.Midline);
            float bw = UiDraw.Width(badgeText) + 18f;
            var badgePlate = UiDraw.Box(card, "ReadinessChip", ready ? UIDesignTokens.WithAlpha(Cyan, .2f) : Panel, 4f, ready ? UIDesignTokens.WithAlpha(Cyan, .5f) : UIDesignTokens.WithAlpha(Edge, .6f));
            badgePlate.transform.SetSiblingIndex(badgeText.transform.GetSiblingIndex());
            UiDraw.Place(badgePlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad, -32f), new Vector2(bw, 22f));
            UiDraw.Place(badgeText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-pad, -32f), new Vector2(bw, 22f));
            UiDraw.HRule(card, "TopRule", UIDesignTokens.WithAlpha(Edge, .4f), 56f, pad, pad);

            // Portrait: 112 px circle on the venue, ringed cyan for the leader.
            var ring = UiDraw.Dot(card, "Portrait", 112f, Ink);
            UiDraw.Place(ring.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -150f), new Vector2(112f, 112f));
            var mask = UiDraw.Dot(ring.transform, "Mask", 108f, Color.white);
            mask.rectTransform.anchoredPosition = Vector2.zero;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            if (!open)
            {
                var art = UiDraw.Node(mask.transform, "Rooftop").gameObject.AddComponent<RawImage>();
                UiDraw.Place(art.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(192f, 108f));
                art.texture = UiPresentationProfile.LoadRooftopArtwork();
                art.raycastTarget = false;
            }
            else UiDraw.Place(UiDraw.Icon(ring.transform, "Empty", "person_add", 40f, Edge).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40f, 40f));
            var outline = UiDraw.Ring(ring.transform, "Rim", 112f, leader || self ? 2f : 1f, leader || self ? UIDesignTokens.WithAlpha(Cyan, .6f) : Edge);
            outline.rectTransform.anchoredPosition = Vector2.zero;

            string visibleAlias = open ? UiDraw.L("partyRoom.seat.awaitingPlayer", "Awaiting Player") : StreamSafePresentation.PlayerName(alias, index);
            UiDraw.Place(UiDraw.Text(card, "Alias", visibleAlias, UiFont.Heading, UiDp, open ? Muted : Text1, -.025f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -232f), new Vector2(CardW - 2f * pad, 28f));
            string readyToLaunch = UiDraw.L("partyRoom.seat.readyToLaunch", "Ready to Launch");
            string seatDescription = open ? UiDraw.L("partyRoom.seat.descriptionOpen", "Share the party code to fill this seat")
                : self && !present ? UiDraw.L("partyRoom.seat.descriptionHostHint", "Create a party to host this room")
                : self ? UiDraw.L("partyRoom.seat.you", "You")
                : ready ? readyToLaunch : UiDraw.L("partyRoom.seat.awaitingReady", "Awaiting Ready");
            UiDraw.Place(UiDraw.Text(card, "SeatDescription", seatDescription,
                    UiFont.MonoRegular, CaptionDp, open ? Text2 : self ? Cyan : Text2, 0f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -256f), new Vector2(CardW - 2f * pad, 16f));

            // Footer: seat role and the seat's own action.
            UiDraw.Place(UiDraw.Box(card, "BottomRule", UIDesignTokens.WithAlpha(Edge, .4f)).rectTransform,
                new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, pad + 28f), new Vector2(CardW - 2f * pad, 1f));
            string footerRole = open ? UiDraw.L("partyRoom.seat.footerInviteOpen", "Invite Open")
                : !present ? UiDraw.L("partyRoom.seat.footerHostOnCreate", "Role: Host on Create")
                : leader ? UiDraw.L("partyRoom.seat.footerRoleHost", "Role: Host")
                : ready ? readyToLaunch : UiDraw.L("partyRoom.seat.footerSquadMember", "Squad Member");
            UiDraw.Place(UiDraw.Text(card, "FooterRole", footerRole,
                    UiFont.HeadingMedium, CaptionDp, Text2, .05f).rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(pad, pad + 8f), new Vector2(200f, 16f));
            if (self && toggleReady != null)
            {
                var toggle = UiDraw.Tap(card, "ToggleReady", new Color(0f, 0f, 0f, 0f), 4f, toggleReady);
                UiDraw.Place((RectTransform)toggle.transform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-pad + 6f, pad + 8f), new Vector2(130f, 32f));
                UiDraw.Place(UiDraw.Text(toggle.transform, "Label", ready ? UiDraw.L("partyRoom.seat.setNotReady", "Set Not Ready") : UiDraw.L("partyRoom.seat.setReady", "Set Ready"), UiFont.Mono, CaptionDp, Cyan, .05f, TextAlignmentOptions.MidlineRight).rectTransform,
                    new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-6f, 0f), new Vector2(124f, 16f));
            }
            else if (self)
                UiDraw.Place(UiDraw.Text(card, "You", UiDraw.L("partyRoom.seat.you", "You"), UiFont.MonoRegular, CaptionDp, Cyan, 0f, TextAlignmentOptions.MidlineRight).rectTransform,
                    new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-pad, pad + 8f), new Vector2(60f, 16f));
        }

        static RectTransform Footer(RectTransform root, string summary, string status, bool good)
        {
            var footer = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(Ink, .9f)).rectTransform;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1f, 0f); footer.pivot = new Vector2(.5f, 0f);
            footer.sizeDelta = new Vector2(0f, FooterHeight); footer.anchoredPosition = Vector2.zero;
            UiDraw.HRule(footer, "FooterRule", UIDesignTokens.WithAlpha(Edge, .4f), 0f);
            UiDraw.Place(UiDraw.Text(footer, "ReadySummary", summary, UiFont.Heading, CaptionDp, good ? Cyan : Text2, .05f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 9f), new Vector2(420f, 16f));
            var statusText = UiDraw.Text(footer, "RoomStatus", status, UiFont.Body, CaptionDp, Text2);
            statusText.richText = false;
            UiDraw.Place(statusText.rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, -9f), new Vector2(520f, 16f));
            return footer;
        }

        static Button Primary(RectTransform footer, string name, string icon, string caption, Action action)
        {
            var button = UiDraw.Tap(footer, name, Cyan, 12f, () => { GameAudio.Play2D(GameCue.UiTap); action?.Invoke(); });
            var label = UiDraw.Text(button.transform, "Label", caption, UiFont.Heading, UiDp, Ink, .05f);
            float w = 32f + 20f + 8f + UiDraw.Width(label) + 32f;
            UiDraw.Place((RectTransform)button.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-32f, 0f), new Vector2(w, 48f));
            UiDraw.Glow((RectTransform)button.transform, UIDesignTokens.WithAlpha(Cyan, .35f), 12f, 24f);
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", icon, 20f, Ink).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(60f, 0f), new Vector2(UiDraw.Width(label) + 2f, 20f));
            return button;
        }

        static void IconButton(Transform parent, string name, string icon, float x, Action action)
        {
            var b = UiDraw.Tap(parent, name, new Color(0f, 0f, 0f, 0f), 4f, () => { GameAudio.Play2D(GameCue.UiTap); action?.Invoke(); });
            UiDraw.Place((RectTransform)b.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(x, 0f), new Vector2(IconButtonSize, IconButtonSize));
            UiDraw.Fill(UiDraw.Icon(b.transform, "Icon", icon, 16f, Text2).rectTransform);
        }

        static TMP_InputField CodeField(Transform pill)
        {
            var box = UiDraw.Box(pill, "CodeInput", Ink, 6f, UIDesignTokens.WithAlpha(Edge, .6f));
            box.raycastTarget = true;
            UiDraw.Place(box.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(98f, 0f), new Vector2(158f, 28f));
            var area = UiDraw.Node(box.transform, "Text Area");
            UiDraw.Fill(area, 10f, 2f, 10f, 2f);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UiDraw.Text(area, "Placeholder", UiDraw.L("partyRoom.pill.joinCodePlaceholder", "ABCD-EFGH"), UiFont.Mono, UiDp, Muted, .1f);
            UiDraw.Fill(placeholder.rectTransform);
            var text = UiDraw.Text(area, "Text", "", UiFont.Mono, UiDp, Cyan, .1f);
            UiDraw.Fill(text.rectTransform);
            var input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 8;
            if (StreamSafePresentation.Enabled) input.contentType = TMP_InputField.ContentType.Password;
            input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            input.onValidateInput = (s, i, c) => char.ToUpperInvariant(c);
            input.caretColor = Cyan;
            return input;
        }

        static Texture2D dotTexture;

        static Texture2D DotTexture()
        {
            if (dotTexture != null) return dotTexture;
            dotTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            { name = "PartyLocationGrid", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = x >= 15 && x <= 16 && y >= 15 && y <= 16 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            dotTexture.SetPixels32(pixels); dotTexture.Apply(false, true);
            return dotTexture;
        }
    }
}
