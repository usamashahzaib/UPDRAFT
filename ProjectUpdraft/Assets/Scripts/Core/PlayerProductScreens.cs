using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Updraft.UI;

namespace Updraft.Core
{
    /// <summary>Backend-backed account, party, cosmetic and support screens for the product shell.</summary>
    public static class PlayerProductScreens
    {
        static GameObject panel;
        static GameObject home;
        static GameBootstrap game;
        static TextMeshProUGUI status;
        static TMP_InputField codeInput;
        static TMP_InputField playerInput;
        static TMP_InputField reasonInput;

        public static void Open(GameBootstrap owner, GameObject homePanel, string page)
        {
            game = owner;
            home = homePanel;
            if (panel != null) UnityEngine.Object.Destroy(panel);
            panel = new GameObject("PlayerProductScreen", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(game.matchHUD.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.063f, 0.094f, 0.153f, 0.985f);
            Text(page.ToUpperInvariant(), new Vector2(0, 335), 48, MatchHUD.MainText);
            Button("BACK", new Vector2(0, -365), 360, 68, () => Close());
            status = Text("Connecting securely…", new Vector2(0, -275), 21, MatchHUD.SecondaryText);

            switch (page)
            {
                case "Party": OpenParty(); break;
                case "Profile": OpenProfile(); break;
                case "Appearance": OpenAppearance(); break;
                case "Account": OpenAccount(); break;
                case "Support": OpenSupport(); break;
            }
        }

        static async void OpenParty()
        {
            var auth = await BackendClient.Instance.EnsureSessionAsync();
            if (!auth.Ok) { SetStatus(auth.Message); Retry("REFRESH PARTY", OpenParty); return; }
            Label("Invite code", new Vector2(-330, 235));
            codeInput = Input("ABCDEFGH", new Vector2(-160, 185), 420, 62, 8);
            Button("CREATE PARTY", new Vector2(420, 205), 330, 62, async () =>
            {
                SetStatus("Creating party…");
                var result = await BackendClient.Instance.CreatePartyAsync();
                SetStatus(result.Ok ? "Party created." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            });
            Button("JOIN CODE", new Vector2(420, 125), 330, 62, async () =>
            {
                string code = (codeInput.text ?? "").Trim().ToUpperInvariant();
                if (code.Length != 8) { SetStatus("Enter the complete 8-character party code."); return; }
                SetStatus("Joining party…");
                var result = await BackendClient.Instance.JoinPartyAsync(code);
                SetStatus(result.Ok ? "Joined party." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            });
            Button("REFRESH", new Vector2(420, 45), 330, 62, RefreshParty);
            // Compile-repair (data desk, 2026-09-28): RefreshParty is async void so it can
            // serve as a button handler; fire-and-forget here — no await on void.
            RefreshParty();
        }

        static async void RefreshParty()
        {
            SetStatus("Refreshing party…");
            var result = await BackendClient.Instance.CurrentPartyAsync();
            if (result.Ok && result.Value != null) { RenderParty(result.Value); SetStatus("Party status is current."); }
            else SetStatus(result.Error == BackendError.NotFound ? "You are not in a party yet. Create one or join with a code." : result.Message);
        }

        static void RenderParty(PartyDto party)
        {
            ClearRows();
            if (party == null) return;
            bool isLeader = string.Equals(party.leaderId, BackendClient.Instance.PlayerId, StringComparison.OrdinalIgnoreCase);
            bool isReady = party.readyIds != null && party.readyIds.Contains(BackendClient.Instance.PlayerId, StringComparer.OrdinalIgnoreCase);
            int memberCount = party.memberIds?.Count ?? 0;
            int readyCount = party.readyIds?.Count ?? 0;
            Text("CODE  " + party.code, new Vector2(0, 100), 38, MatchHUD.Objective);
            Text($"Members {memberCount}/3     Ready {readyCount}/{memberCount}     {(isLeader ? "LEADER" : "MEMBER")}", new Vector2(0, 40), 24, MatchHUD.MainText);
            string memberSummary = string.Join("   ·   ", (party.members ?? Array.Empty<PartyMemberDto>()).Select(m => $"{m.alias}{(m.isReady ? " ✓" : " · not ready")}"));
            Text("Members: " + memberSummary, new Vector2(0, -15), 20, MatchHUD.SecondaryText);
            Button(isReady ? "SET NOT READY" : "READY", new Vector2(-260, -105), 340, 64, async () =>
            {
                SetStatus("Updating readiness…");
                var update = await BackendClient.Instance.SetReadyAsync(party.code, !isReady);
                if (update.Ok) RenderParty(update.Value);
                SetStatus(update.Ok ? "Readiness updated." : update.Message);
            });
            Button("LEAVE PARTY", new Vector2(260, -105), 340, 64, async () =>
            {
                SetStatus("Leaving party…");
                var leave = await BackendClient.Instance.LeavePartyAsync(party.code);
                SetStatus(leave.Ok ? "Left party. The leader role transfers to the next member." : leave.Message);
                if (leave.Ok) ClearRows();
            });
            bool allReady = memberCount > 0 && readyCount == memberCount;
            if (isLeader && allReady)
                Button("QUEUE PARTY", new Vector2(0, -195), 420, 68, () => OnlineHooks.PlayOnline?.Invoke(game));
            else if (isLeader)
                Text("The leader can queue when every member is ready.", new Vector2(0, -195), 19, MatchHUD.SecondaryText);
        }

        static async void OpenProfile()
        {
            SetStatus("Loading your profile…");
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); Retry("RETRY PROFILE", OpenProfile); return; }
            var wallet = await BackendClient.Instance.GetWalletAsync();
            if (!wallet.Ok) { SetStatus(wallet.Message); Retry("RETRY PROFILE", OpenProfile); return; }
            Text(profile.Value.alias, new Vector2(0, 210), 36, MatchHUD.Friendly);
            Text($"Level {profile.Value.level}     XP {profile.Value.profileXp}     Credits {wallet.Value?.credits ?? 0}", new Vector2(0, 150), 25, MatchHUD.MainText);
            Text("All weapons stay unlocked. Cosmetic ownership never changes gameplay power.", new Vector2(0, 95), 18, MatchHUD.SecondaryText);
            SetStatus("Profile loaded from your signed-in account.");
        }

        static async void OpenAppearance()
        {
            SetStatus("Loading your cosmetic inventory…");
            var result = await BackendClient.Instance.GetInventoryAsync();
            if (!result.Ok || result.Value == null) { SetStatus(result.Message); Retry("RETRY INVENTORY", OpenAppearance); return; }
            Text("Select an owned cosmetic to equip. Preview rendering is not available in this build.", new Vector2(0, 245), 18, MatchHUD.SecondaryText);
            int row = 0;
            foreach (var item in result.Value.items ?? Array.Empty<InventoryItemDto>())
            {
                int y = 170 - row * 72;
                string cosmeticId = item.cosmeticId;
                Text(cosmeticId + "  ·  " + item.acquiredVia, new Vector2(-160, y), 21, MatchHUD.MainText);
                Button("EQUIP", new Vector2(380, y), 220, 54, async () =>
                {
                    SetStatus("Saving appearance…");
                    var equipped = await BackendClient.Instance.EquipAsync(cosmeticId);
                    SetStatus(equipped.Ok ? "Appearance saved to your account." : equipped.Message);
                });
                row++;
            }
            if (row == 0) SetStatus("Inventory is empty. Retry when the service is available.");
            else SetStatus("Owned cosmetics only. Equip changes appearance, never power.");
        }

        static async void OpenAccount()
        {
            SetStatus("Loading account recovery status…");
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); Retry("RETRY ACCOUNT", OpenAccount); return; }
            Text(profile.Value.isGuest ? "GUEST ACCOUNT" : "RECOVERABLE ACCOUNT", new Vector2(0, 220), 30, MatchHUD.Friendly);
            Text("Player: " + profile.Value.alias, new Vector2(0, 170), 21, MatchHUD.MainText);
            Text(profile.Value.isGuest
                ? "This guest profile is not recoverable after uninstall. Google Play Games sign-in is not configured in this client build."
                : "Google Play Games is linked. Sign in again after reinstall to recover this profile.", new Vector2(0, 90), 20, MatchHUD.SecondaryText);
            if (!profile.Value.isGuest)
                Button("REVOKE GOOGLE PLAY LINK", new Vector2(0, -30), 490, 64, async () =>
                {
                    SetStatus("Removing the recovery link…");
                    var revoke = await BackendClient.Instance.RevokeGooglePlayLinkAsync();
                    SetStatus(revoke.Ok ? "Link removed. This account is now guest-only and will not recover after reinstall." : revoke.Message);
                });
            else
                Text("Linking is unavailable until the publisher's Play Games client setup is supplied.", new Vector2(0, -25), 18, MatchHUD.Objective);
        }

        static void OpenSupport()
        {
            Label("Reported player ID", new Vector2(0, 235));
            playerInput = Input("player account ID", new Vector2(0, 185), 760, 58, 40);
            Label("Reason", new Vector2(0, 110));
            reasonInput = Input("cheating, harassment, or other", new Vector2(0, 60), 760, 58, 180);
            Button("SUBMIT REPORT", new Vector2(0, -30), 420, 64, async () =>
            {
                string id = (playerInput.text ?? "").Trim();
                string reason = (reasonInput.text ?? "").Trim();
                if (!Guid.TryParse(id, out _) || reason.Length < 3) { SetStatus("Enter a valid player ID and a short report reason."); return; }
                SetStatus("Submitting report…");
                var report = await BackendClient.Instance.ReportAsync(id, reason);
                SetStatus(report.Ok ? "Report submitted for review." : report.Message);
            });
            Button("DELETE ACCOUNT…", new Vector2(0, -120), 500, 64, () => ConfirmDelete());
            Text("Deletion removes profile, inventory, credits, reports and match history. Purchase records are retained where required.", new Vector2(0, -215), 17, MatchHUD.SecondaryText);
        }

        static void ConfirmDelete()
        {
            SetStatus("Tap DELETE ACCOUNT again to confirm. This cannot be undone.");
            ClearRows();
            Button("DELETE ACCOUNT", new Vector2(0, 15), 430, 68, async () =>
            {
                SetStatus("Deleting account data…");
                var deleted = await BackendClient.Instance.DeleteAccountAsync();
                if (!deleted.Ok) { SetStatus(deleted.Message); return; }
                BackendClient.Instance.ForgetLocalSession();
                ClearRows();
                SetStatus("Account deleted. Purchase records are retained where required.");
            });
            Button("CANCEL", new Vector2(0, -70), 320, 60, OpenSupport);
        }

        // Compile-repair (data desk, 2026-09-28): the shared Button helper takes
        // UnityEventTools-compatible UnityAction; adapt the Action here instead of changing
        // every call site. FLAG FOR OWNER REVIEW — cosmetic edit only.
        static void Retry(string label, Action retry) => Button(label, new Vector2(0, 0), 360, 62, () => retry());

        static void Close()
        {
            if (panel != null) UnityEngine.Object.Destroy(panel);
            panel = null;
            if (home != null) home.SetActive(true);
        }

        static void ClearRows()
        {
            if (panel == null) return;
            foreach (Transform child in panel.transform)
                if (child.gameObject != (status != null ? status.gameObject : null) && child.name.StartsWith("Row_", StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(child.gameObject);
        }

        static void SetStatus(string value) { if (status != null) status.text = value ?? "Something went wrong. Try again."; }
        static void Label(string value, Vector2 position) => Text(value, position, 18, MatchHUD.SecondaryText);

        static TextMeshProUGUI Text(string value, Vector2 position, float size, Color color)
        {
            var obj = new GameObject("Row_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(panel.transform, false);
            var rt = obj.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(1450, size + 24);
            var text = obj.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false;
            return text;
        }

        static TMP_InputField Input(string placeholder, Vector2 position, float width, float height, int maxLength)
        {
            var root = new GameObject("Row_Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(panel.transform, false);
            var rt = root.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(width, height);
            root.GetComponent<Image>().color = new Color(0.106f, 0.161f, 0.251f);
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(root.transform, false);
            var textRt = textObject.GetComponent<RectTransform>(); textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.offsetMin = new Vector2(12, 4); textRt.offsetMax = new Vector2(-12, -4);
            var text = textObject.GetComponent<TextMeshProUGUI>(); text.fontSize = 20; text.color = MatchHUD.MainText; text.text = ""; text.raycastTarget = false;
            var input = root.GetComponent<TMP_InputField>(); input.textComponent = text; input.targetGraphic = root.GetComponent<Image>(); input.characterLimit = maxLength; input.lineType = TMP_InputField.LineType.SingleLine;
            input.placeholder = Text(placeholder, Vector2.zero, 18, MatchHUD.SecondaryText); input.placeholder.transform.SetParent(root.transform, false);
            var placeholderRect = input.placeholder.rectTransform; placeholderRect.anchorMin = Vector2.zero; placeholderRect.anchorMax = Vector2.one; placeholderRect.offsetMin = new Vector2(12, 4); placeholderRect.offsetMax = new Vector2(-12, -4);
            return input;
        }

        static void Button(string caption, Vector2 position, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject("Row_Button", typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(panel.transform, false);
            var rt = obj.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(width, height);
            var image = obj.GetComponent<Image>(); image.color = new Color(0.106f, 0.161f, 0.251f);
            var button = obj.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); action(); });
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(obj.transform, false);
            var labelRt = label.GetComponent<RectTransform>(); labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one; labelRt.sizeDelta = Vector2.zero;
            var text = label.GetComponent<TextMeshProUGUI>(); text.text = caption; text.fontSize = 21; text.alignment = TextAlignmentOptions.Center; text.color = MatchHUD.Friendly; text.raycastTarget = false;
        }
    }
}
