using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.UI;

namespace Hotdrop.Core
{
    /// <summary>Backend-backed account, party, cosmetic and support screens for the product shell.</summary>
    public static class PlayerProductScreens
    {
        static GameObject panel;
        static GameObject home;
        static GameObject navBack;
        static GameBootstrap game;
        static TextMeshProUGUI status;
        static TMP_InputField codeInput;
        static TMP_InputField playerInput;
        static TMP_InputField reasonInput;
        static RecentMatch.Entry reportTarget;
        // QA-002: bumped on every Open/Close so a reply that lands after the player left
        // (or switched to another page) is dropped instead of drawing on the wrong screen.
        static int screenVersion;

        public static void Open(GameBootstrap owner, GameObject homePanel, string page)
        {
            game = owner;
            Open(game.matchHUD.transform, homePanel, page);
        }

        /// <summary>Builds the page under <paramref name="parent"/>; separated from the bootstrap for tests.</summary>
        public static void Open(Transform parent, GameObject homePanel, string page)
        {
            home = homePanel;
            screenVersion++;
            ClosePreview();
            if (panel != null) Discard(panel);
            // QA-019/020: density-aware scrolling layout; BACK and status live outside the page
            // rows (top bar / bottom line), so clearing rows can never remove the exit (QA-002).
            var screen = MenuLayout.Create(parent, "PlayerProductScreen", page.ToUpperInvariant(), () => Close());
            panel = screen.Root;
            navBack = screen.Back;
            status = screen.Status;
            content = screen.Content;
            rowTarget = null;
            SetStatus("Connecting securely…");

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
            int version = screenVersion;
            var auth = await BackendClient.Instance.EnsureSessionAsync();
            if (Stale(version)) return;
            if (!auth.Ok) { SetStatus(auth.Message); Retry("REFRESH PARTY", OpenParty); return; }
            ShowPartyEntry();
            RefreshParty();
        }

        /// <summary>Create/join/refresh controls; also restored after leaving a party (QA-002).</summary>
        static void ShowPartyEntry()
        {
            ClearRows();
            Label("Invite code", new Vector2(-330, 235));
            codeInput = Input("ABCDEFGH", new Vector2(-160, 185), 420, 62, 8);
            BeginRow();
            Button("CREATE PARTY", new Vector2(420, 205), 330, 62, async () =>
            {
                SetStatus("Creating party…");
                int version = screenVersion;
                var result = await BackendClient.Instance.CreatePartyAsync();
                if (Stale(version)) return;
                SetStatus(result.Ok ? "Party created." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            });
            Button("JOIN CODE", new Vector2(420, 125), 330, 62, async () =>
            {
                string code = (codeInput.text ?? "").Trim().ToUpperInvariant();
                if (code.Length != 8) { SetStatus("Enter the complete 8-character party code."); return; }
                SetStatus("Joining party…");
                int version = screenVersion;
                var result = await BackendClient.Instance.JoinPartyAsync(code);
                if (Stale(version)) return;
                SetStatus(result.Ok ? "Joined party." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            });
            Button("REFRESH", new Vector2(420, 45), 330, 62, RefreshParty);
            EndRow();
        }

        static async void RefreshParty()
        {
            SetStatus("Refreshing party…");
            int version = screenVersion;
            var result = await BackendClient.Instance.CurrentPartyAsync();
            if (Stale(version)) return;
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
            Telemetry.PartySize = Mathf.Max(1, memberCount); // QA-027: events carry the real party size
            Text("CODE  " + party.code, new Vector2(0, 100), 38, MatchHUD.Objective);
            Text($"Members {memberCount}/3     Ready {readyCount}/{memberCount}     {(isLeader ? "LEADER" : "MEMBER")}", new Vector2(0, 40), 24, MatchHUD.MainText);
            string memberSummary = string.Join("   ·   ", (party.members ?? Array.Empty<PartyMemberDto>()).Select(m => $"{m.alias}{(m.isReady ? " ✓" : " · not ready")}"));
            Text("Members: " + memberSummary, new Vector2(0, -15), 20, MatchHUD.SecondaryText);
            BeginRow();
            Button(isReady ? "SET NOT READY" : "READY", new Vector2(-260, -105), 340, 64, async () =>
            {
                SetStatus("Updating readiness…");
                int version = screenVersion;
                var update = await BackendClient.Instance.SetReadyAsync(party.code, !isReady);
                if (Stale(version)) return;
                if (update.Ok) RenderParty(update.Value);
                SetStatus(update.Ok ? "Readiness updated." : update.Message);
            });
            Button("LEAVE PARTY", new Vector2(260, -105), 340, 64, async () =>
            {
                SetStatus("Leaving party…");
                int version = screenVersion;
                var leave = await BackendClient.Instance.LeavePartyAsync(party.code);
                if (Stale(version)) return;
                if (leave.Ok) { ShowPartyEntry(); Telemetry.PartySize = 1; } // QA-002 / QA-027
                SetStatus(leave.Ok ? "Left party. The leader role transfers to the next member." : leave.Message);
            });
            EndRow();
            bool allReady = memberCount > 0 && readyCount == memberCount;
            BeginRow();
            if (isLeader && allReady)
                Button("QUEUE PARTY", new Vector2(-230, -195), 420, 68, () => { var owner = game; Close(); OnlineHooks.PlayOnline?.Invoke(owner); });
            else if (isLeader)
                Text("The leader can queue when every member is ready.", new Vector2(-230, -195), 19, MatchHUD.SecondaryText);
            else
                Text(isReady ? "Ready — you'll join automatically when the leader queues." : "Tap READY so the leader can queue the party.",
                    new Vector2(-230, -195), 19, MatchHUD.SecondaryText);
            // QA-014: REFRESH stays available while in a party (it used to be cleared with the rows).
            Button("REFRESH", new Vector2(330, -195), 260, 64, RefreshParty);
            EndRow();
            WatchParty();
        }

        static int watchedVersion = -1;

        /// <summary>
        /// QA-014: while the party screen is open, keep membership/readiness current and, once
        /// the leader has queued the party, carry every member into the same search.
        /// </summary>
        static async void WatchParty()
        {
            int version = screenVersion;
            if (watchedVersion == version) return; // one watcher per opened screen
            watchedVersion = version;
            while (!Stale(version))
            {
                await Task.Delay(3000);
                if (Stale(version)) return;
                var queue = await BackendClient.Instance.QueueStatusAsync();
                if (Stale(version)) return;
                if (queue.Ok && queue.Value != null && queue.Value.state == "searching")
                {
                    var owner = game;
                    Close();
                    OnlineHooks.PlayOnline?.Invoke(owner);
                    return;
                }
                var party = await BackendClient.Instance.CurrentPartyAsync();
                if (Stale(version)) return;
                if (party.Ok && party.Value != null) RenderParty(party.Value);
                else if (party.Error == BackendError.NotFound) { ShowPartyEntry(); SetStatus("You are no longer in a party."); return; }
            }
        }

        static async void OpenProfile()
        {
            ClearRows(); // drop any RETRY button from a previous failed load
            SetStatus("Loading your profile…");
            int version = screenVersion;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); Retry("RETRY PROFILE", OpenProfile); return; }
            var wallet = await BackendClient.Instance.GetWalletAsync();
            if (Stale(version)) return;
            if (!wallet.Ok) { SetStatus(wallet.Message); Retry("RETRY PROFILE", OpenProfile); return; }
            Text(profile.Value.alias, new Vector2(0, 210), 36, MatchHUD.Friendly);
            Text($"Level {profile.Value.level}     XP {profile.Value.profileXp}     Credits {wallet.Value?.credits ?? 0}", new Vector2(0, 150), 25, MatchHUD.MainText);
            Text("All weapons stay unlocked. Cosmetic ownership never changes gameplay power.", new Vector2(0, 95), 18, MatchHUD.SecondaryText);
            SetStatus("Profile loaded from your signed-in account.");
        }

        static async void OpenAppearance()
        {
            ClearRows();
            SetStatus("Loading your cosmetic inventory…");
            int version = screenVersion;
            var result = await BackendClient.Instance.GetInventoryAsync();
            if (Stale(version)) return;
            if (!result.Ok || result.Value == null) { SetStatus(result.Message); Retry("RETRY INVENTORY", OpenAppearance); return; }
            // QA-024: a live preview of the selected item, the currently equipped item marked, and
            // the equipped choice cached locally so the player's own avatar shows it in play.
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            string equippedId = profile.Ok && profile.Value != null ? profile.Value.equippedCosmeticId : CosmeticLook.LocalEquipped;
            if (profile.Ok && profile.Value != null) CosmeticLook.LocalEquipped = profile.Value.equippedCosmeticId;
            ShowPreview(equippedId);
            Text("Tap an item to preview it, then EQUIP. Cosmetics change appearance, never power.", Vector2.zero, 20, MatchHUD.SecondaryText);
            int row = 0;
            foreach (var item in result.Value.items ?? Array.Empty<InventoryItemDto>())
            {
                string cosmeticId = item.cosmeticId;
                bool isEquipped = cosmeticId == equippedId;
                BeginRow();
                Button((isEquipped ? "✓ " : "") + CosmeticLook.DisplayName(cosmeticId), Vector2.zero, 0, 0, () =>
                {
                    ShowPreview(cosmeticId);
                    SetStatus($"Previewing {CosmeticLook.DisplayName(cosmeticId)}.");
                });
                Button(isEquipped ? "EQUIPPED" : "EQUIP", Vector2.zero, 0, 0, async () =>
                {
                    SetStatus("Saving appearance…");
                    int equipVersion = screenVersion;
                    var equipped = await BackendClient.Instance.EquipAsync(cosmeticId);
                    if (equipped.Ok) CosmeticLook.LocalEquipped = cosmeticId;
                    if (Stale(equipVersion)) return;
                    SetStatus(equipped.Ok ? $"{CosmeticLook.DisplayName(cosmeticId)} equipped — teammates and opponents will see it." : equipped.Message);
                    if (equipped.Ok) OpenAppearance();
                });
                EndRow();
                row++;
            }
            if (row == 0) SetStatus("Inventory is empty. Retry when the service is available.");
            else SetStatus("Owned cosmetics only. Equip changes appearance, never power.");
        }

        static async void OpenAccount()
        {
            ClearRows();
            SetStatus("Loading account recovery status…");
            int version = screenVersion;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); Retry("RETRY ACCOUNT", OpenAccount); return; }
            Text(profile.Value.isGuest ? "GUEST ACCOUNT" : "RECOVERABLE ACCOUNT", new Vector2(0, 220), 30, MatchHUD.Friendly);
            Text("Player: " + profile.Value.alias, new Vector2(0, 170), 21, MatchHUD.MainText);
            Text(profile.Value.isGuest
                ? "Guest profiles are only on this device. Link Google Play Games to recover them after reinstall or on a new phone."
                : "Google Play Games is linked. Sign in again after reinstall to recover this profile.", new Vector2(0, 90), 20, MatchHUD.SecondaryText);
            if (!profile.Value.isGuest)
                Button("REVOKE GOOGLE PLAY LINK", new Vector2(0, -30), 490, 64, async () =>
                {
                    SetStatus("Removing the recovery link…");
                    int revokeVersion = screenVersion;
                    var revoke = await BackendClient.Instance.RevokeGooglePlayLinkAsync();
                    if (Stale(revokeVersion)) return;
                    SetStatus(revoke.Ok ? "Link removed. This account is now guest-only and will not recover after reinstall." : revoke.Message);
                });
            else if (PlatformIdentity.Available)
            {
                // QA-026: link this guest now, or recover an account already linked elsewhere.
                BeginRow();
                Button("LINK GOOGLE PLAY", Vector2.zero, 0, 0, async () =>
                {
                    SetStatus("Opening Google Play Games…");
                    int linkVersion = screenVersion;
                    string code = await PlatformIdentity.RequestServerAuthCodeAsync();
                    if (Stale(linkVersion)) return;
                    if (string.IsNullOrEmpty(code)) { SetStatus("Google Play Games sign-in was cancelled."); return; }
                    var link = await BackendClient.Instance.LinkGooglePlayAsync(code);
                    if (Stale(linkVersion)) return;
                    if (link.Ok) OpenAccount();
                    SetStatus(link.Ok ? "Linked. This account can now be recovered after reinstall."
                        : link.Error == BackendError.Conflict ? "That Google Play identity already belongs to another account — use RECOVER to switch to it."
                        : link.Message);
                });
                Button("RECOVER LINKED ACCOUNT", Vector2.zero, 0, 0, async () =>
                {
                    SetStatus("Recovering your linked account…");
                    int recoverVersion = screenVersion;
                    string code = await PlatformIdentity.RequestServerAuthCodeAsync();
                    if (Stale(recoverVersion)) return;
                    if (string.IsNullOrEmpty(code)) { SetStatus("Google Play Games sign-in was cancelled."); return; }
                    var signIn = await BackendClient.Instance.PlatformSignInAsync(code);
                    if (Stale(recoverVersion)) return;
                    if (signIn.Ok) { RecentMatch.Forget(); OpenAccount(); SetStatus("Recovered your linked account — profile, inventory and credits restored."); }
                    else SetStatus(signIn.Error == BackendError.NotFound
                        ? "No account is linked to this Google Play identity yet. Use LINK to protect this profile."
                        : signIn.Message);
                });
                EndRow();
            }
            else
                Text("Google Play Games is not set up in this build yet, so this guest profile cannot be linked or recovered. Keep the app installed to keep your progress.",
                    new Vector2(0, -25), 20, MatchHUD.Objective);

            // QA-028: an optional contact email lets you request deletion from the web if you lose
            // this device. Only a hash is stored; it is never shown to other players.
            Text("Account deletion email (optional) — lets you delete this account from the web if you lose this phone.",
                Vector2.zero, 20, MatchHUD.SecondaryText);
            var emailInput = Input("you@example.com", Vector2.zero, 0, 0, 254);
            emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            BeginRow();
            Button("SAVE EMAIL", Vector2.zero, 0, 0, async () =>
            {
                string email = (emailInput.text ?? "").Trim();
                if (email.Length < 5 || !email.Contains("@")) { SetStatus("Enter a valid email address."); return; }
                SetStatus("Saving…");
                int emailVersion = screenVersion;
                var saved = await BackendClient.Instance.SetContactEmailAsync(email);
                if (Stale(emailVersion)) return;
                SetStatus(saved.Ok ? "Saved. Web deletion requests for this email will now reach this account." : saved.Message);
            });
            Button("REMOVE EMAIL", Vector2.zero, 0, 0, async () =>
            {
                SetStatus("Removing…");
                int emailVersion = screenVersion;
                var removed = await BackendClient.Instance.RemoveContactEmailAsync();
                if (Stale(emailVersion)) return;
                SetStatus(removed.Ok ? "Removed." : removed.Message);
            });
            EndRow();
        }

        static void OpenSupport()
        {
            ClearRows(); // this page can replace ConfirmDelete — never stack rows
            // QA-025: report someone from your last online match by picking their name; the
            // match context goes with the report. A raw player ID remains an advanced fallback.
            reportTarget = null;
            bool hasRecent = RecentMatch.TryGet(out string recentMatchId, out var recentPlayers);
            if (hasRecent)
            {
                Label("Report a player from your last online match — tap their name", Vector2.zero);
                BeginRow();
                int inRow = 0;
                foreach (var entry in recentPlayers)
                {
                    var chosen = entry;
                    Button(chosen.alias, Vector2.zero, 0, 0, () =>
                    {
                        reportTarget = chosen;
                        SetStatus($"Reporting {chosen.alias}. Add a reason and submit.");
                    });
                    if (++inRow % 3 == 0) { EndRow(); BeginRow(); }
                }
                EndRow();
            }
            else Label("Play an online match to report someone you played with, or enter their player ID below.", Vector2.zero);
            Label("Reason", new Vector2(0, 110));
            reasonInput = Input("cheating, harassment, or other", new Vector2(0, 60), 760, 58, 180);
            Label("Or report by player ID (advanced)", new Vector2(0, 235));
            playerInput = Input("player account ID", new Vector2(0, 185), 760, 58, 40);
            Button("SUBMIT REPORT", new Vector2(0, -30), 420, 64, async () =>
            {
                string id = (playerInput.text ?? "").Trim();
                string reason = (reasonInput.text ?? "").Trim();
                if (reason.Length < 3) { SetStatus("Add a short reason (at least 3 characters)."); return; }
                bool byId = Guid.TryParse(id, out _);
                if (!byId && reportTarget == null) { SetStatus(hasRecent ? "Tap the player you want to report, or enter their player ID." : "Enter a valid player ID."); return; }
                SetStatus("Submitting report…");
                int version = screenVersion;
                var report = byId ? await BackendClient.Instance.ReportAsync(id, reason)
                    : await BackendClient.Instance.ReportInMatchAsync(recentMatchId, reportTarget.alias, reportTarget.slot, reason);
                if (Stale(version)) return;
                SetStatus(report.Ok ? "Report submitted for review. Thank you." : report.Message);
            });
            Button("DELETE ACCOUNT…", new Vector2(0, -120), 500, 64, () => ConfirmDelete());
            Text("Deletion removes profile, inventory, credits, reports and match history. Purchase records are retained where required.", new Vector2(0, -215), 17, MatchHUD.SecondaryText);
        }

        static void ConfirmDelete()
        {
            SetStatus("Tap DELETE ACCOUNT again to confirm. This cannot be undone.");
            ClearRows();
            BeginRow();
            Button("DELETE ACCOUNT", new Vector2(0, 15), 430, 68, async () =>
            {
                SetStatus("Deleting account data…");
                int version = screenVersion;
                var deleted = await BackendClient.Instance.DeleteAccountAsync();
                // Forget the local session even if the player already left this screen.
                if (deleted.Ok) BackendClient.Instance.ForgetLocalSession();
                if (Stale(version)) return;
                if (!deleted.Ok) { SetStatus(deleted.Message); return; }
                ClearRows();
                SetStatus("Account deleted. Purchase records are retained where required. Use BACK to return Home.");
            });
            Button("CANCEL", new Vector2(0, -70), 320, 60, OpenSupport);
            EndRow();
        }

        // Compile-repair (data desk, 2026-09-28): the shared Button helper takes
        // UnityEventTools-compatible UnityAction; adapt the Action here instead of changing
        // every call site. FLAG FOR OWNER REVIEW — cosmetic edit only.
        static void Retry(string label, Action retry)
        {
            ClearRows(); // one retry action at a time; keep BACK/status untouched
            Button(label, new Vector2(0, 0), 360, 62, () => retry());
        }

        // ---- QA-024: Appearance preview (render texture of a mannequin wearing the item) ----
        static GameObject previewStage;
        static Transform previewMannequin;
        static RenderTexture previewTexture;
        static RawImage previewImage;

        static void ShowPreview(string cosmeticId)
        {
            if (previewStage == null)
            {
                previewStage = new GameObject("AppearancePreviewStage");
                previewStage.transform.position = new Vector3(0, -500f, 0); // far below the arena
                previewMannequin = CosmeticLook.BuildMannequin(previewStage.transform);
                previewMannequin.localRotation = Quaternion.Euler(0, 200f, 0);
                var light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(previewStage.transform, false);
                light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, 150, 0);
                previewTexture = new RenderTexture(512, 512, 16);
                var cam = new GameObject("PreviewCamera", typeof(UnityEngine.Camera)).GetComponent<UnityEngine.Camera>();
                cam.transform.SetParent(previewStage.transform, false);
                cam.transform.localPosition = new Vector3(0, 1.05f, -2.6f);
                cam.fieldOfView = 40f; cam.targetTexture = previewTexture;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.106f, 0.161f, 0.251f);
                cam.cullingMask = ~0;
            }
            CosmeticLook.Apply(previewMannequin, cosmeticId);
            if (previewImage == null && content != null)
            {
                var obj = new GameObject("Row_Preview", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter), typeof(LayoutElement));
                obj.transform.SetParent(content, false);
                obj.transform.SetAsFirstSibling();
                previewImage = obj.GetComponent<RawImage>();
                previewImage.texture = previewTexture; previewImage.raycastTarget = false;
                var element = obj.GetComponent<LayoutElement>(); element.preferredHeight = 260; element.minHeight = 200;
                obj.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }
        }

        static void ClosePreview()
        {
            if (previewStage != null) Discard(previewStage);
            if (previewTexture != null) { previewTexture.Release(); Discard(previewTexture); }
            previewStage = null; previewMannequin = null; previewTexture = null; previewImage = null;
        }

        static void Close()
        {
            ClosePreview();
            screenVersion++;
            navBack = null;
            status = null;
            if (panel != null) Discard(panel);
            panel = null;
            if (home != null) home.SetActive(true);
        }

        /// <summary>True when the screen that issued a request has since been closed or replaced.</summary>
        static bool Stale(int version) => version != screenVersion || panel == null;

        /// <summary>Removes page content. Nav_* objects (title, BACK) and the status line always survive.</summary>
        static void ClearRows()
        {
            if (panel == null || content == null) return;
            rowTarget = null;
            var rows = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in content)
                if (child.name.StartsWith("Row_", StringComparison.Ordinal))
                    rows.Add(child.gameObject);
            foreach (var row in rows) Discard(row);
        }

        static RectTransform content;
        static RectTransform rowTarget;

        /// <summary>Following controls sit side by side on one row until <see cref="EndRow"/>.</summary>
        static void BeginRow() => rowTarget = MenuLayout.Row(content);
        static void EndRow() => rowTarget = null;
        static Transform Target => rowTarget != null ? rowTarget : content;

        static void Discard(UnityEngine.Object obj)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        static void SetStatus(string value) { if (status != null) status.text = value ?? "Something went wrong. Try again."; }
        // Positions are ignored: the density-aware layout flows every element into the scroll
        // column (QA-019/020). Signatures are kept so the page code reads as before.
        static void Label(string value, Vector2 position) => Text(value, position, 20, MatchHUD.SecondaryText);

        static TextMeshProUGUI Text(string value, Vector2 position, float size, Color color, string objectName = "Row_Text") =>
            MenuLayout.AddText(Target, value, size, color, objectName);

        static TMP_InputField Input(string placeholder, Vector2 position, float width, float height, int maxLength) =>
            MenuLayout.AddInput(Target, placeholder, maxLength);

        static GameObject Button(string caption, Vector2 position, float width, float height, UnityEngine.Events.UnityAction action, string objectName = "Row_Button") =>
            MenuLayout.AddButton(Target, caption, action, objectName).gameObject;
    }
}


