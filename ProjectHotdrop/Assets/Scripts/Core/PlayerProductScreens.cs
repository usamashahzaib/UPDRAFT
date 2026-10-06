using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
            if (panel != null)
            {
                var replacedPanel = panel;
                panel = null;
                UiScreenEnterMotion.Exit(replacedPanel, () => Discard(replacedPanel));
            }
            // QA-019/020: density-aware scrolling layout; BACK and status live outside the page
            // rows (top bar / bottom line), so clearing rows can never remove the exit (QA-002).
            var screen = MenuLayout.Create(parent, "PlayerProductScreen", page.ToUpperInvariant(), () => Close());
            panel = screen.Root;
            navBack = screen.Back;
            status = screen.Status;
            content = screen.Content;
            rowTarget = null;
            SetStatus("Loading…");

            switch (page)
            {
                case "Party": OpenParty(); break;
                case "Profile": OpenProfile(); break;
                case "Appearance": OpenAppearance(); break;
                case "Account": OpenAccount(); break;
                case "Support": OpenSupport(); break;
                case "Contracts": OpenContracts(); break;
                case "Mastery": OpenMastery(); break;
                case "Shop": ShopConceptView.Show(panel, Close); SetStatus("Concept only. Purchases are unavailable in this build."); break;
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
            codeInput = PartyRoomView.BuildEntry(panel, "YOU", () => Close(), async () =>
            {
                SetStatus("Creating party…");
                int version = screenVersion;
                var result = await BackendClient.Instance.CreatePartyAsync();
                if (Stale(version)) return;
                SetStatus(result.Ok ? "Party created." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            }, async entered =>
            {
                string code = (entered ?? "").Trim().Replace("-", "").ToUpperInvariant();
                if (code.Length != 8) { SetStatus("Enter the complete 8-character party code."); return; }
                SetStatus("Joining party…");
                int version = screenVersion;
                var result = await BackendClient.Instance.JoinPartyAsync(code);
                if (Stale(version)) return;
                SetStatus(result.Ok ? "Joined party." : result.Message);
                if (result.Ok) RenderParty(result.Value);
            });
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
            Action toggleReady = async () =>
            {
                SetStatus("Updating readiness…");
                int version = screenVersion;
                var update = await BackendClient.Instance.SetReadyAsync(party.code, !isReady);
                if (Stale(version)) return;
                if (update.Ok) RenderParty(update.Value);
                SetStatus(update.Ok ? "Readiness updated." : update.Message);
            };
            Action leaveParty = async () =>
            {
                SetStatus("Leaving party…");
                int version = screenVersion;
                var leave = await BackendClient.Instance.LeavePartyAsync(party.code);
                if (Stale(version)) return;
                if (leave.Ok) { ShowPartyEntry(); Telemetry.PartySize = 1; } // QA-002 / QA-027
                SetStatus(leave.Ok ? "Left party. The leader role transfers to the next member." : leave.Message);
            };
            bool allReady = memberCount > 0 && readyCount == memberCount;
            PartyRoomView.Build(panel, party, BackendClient.Instance.PlayerId, isLeader, isReady, allReady,
                () => Close(), toggleReady, leaveParty, RefreshParty,
                () => { var owner = game; Close(); OnlineHooks.PlayOnline?.Invoke(owner); });
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
            bool refreshFailed = false;
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
                if (party.Ok && party.Value != null)
                {
                    RenderParty(party.Value);
                    if (refreshFailed) SetStatus("Party status is current.");
                    refreshFailed = false;
                }
                else if (party.Error == BackendError.NotFound) { ShowPartyEntry(); SetStatus("You are no longer in a party."); return; }
                else if (!refreshFailed)
                {
                    // Keep the last known room visible but do not present it as current.
                    refreshFailed = true;
                    SetStatus("Party status could not refresh; showing the last known members and readiness. Retrying automatically.");
                }
            }
        }

        static async void OpenProfile()
        {
            ClearRows(); // drop any RETRY button from a previous failed load
            ShowProfile(null, null, null);
            SetStatus("Loading your profile…");
            int version = screenVersion;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); ShowProfile(null, null, null, OpenProfile); return; }
            var wallet = await BackendClient.Instance.GetWalletAsync();
            if (Stale(version)) return;
            if (!wallet.Ok) { SetStatus("Credits unavailable: " + wallet.Message); ShowProfile(profile.Value, null, null, OpenProfile); return; }
            var stats = await BackendClient.Instance.GetProfileStatsAsync();
            if (Stale(version)) return;
            // A brand-new account has no match-stats record yet; that is a normal empty
            // state, not a failure, and must read like one rather than a raw backend error.
            if (!stats.Ok && stats.Error == BackendError.NotFound)
            { SetStatus("Profile loaded."); ShowProfile(profile.Value, wallet.Value, new ProfileStatsDto()); return; }
            if (!stats.Ok || stats.Value == null) { SetStatus("Match record unavailable: " + stats.Message); ShowProfile(profile.Value, wallet.Value, null, OpenProfile); return; }
            SetStatus("Profile loaded.");
            ShowProfile(profile.Value, wallet.Value, stats.Value);
        }

        static void ShowProfile(ProfileDto profile, WalletDto wallet, ProfileStatsDto stats, Action retry = null)
        {
            var challenges = GameBootstrap.Instance?.GetComponent<OfflineChallengeRuntime>()?.Controller;
            OperativeProfileView.Show(panel, profile, wallet, stats, challenges,
                status != null ? status.text : "", Close,
                () => Open(panel.transform.parent, home, "Appearance"), retry: retry);
        }

        // These pages are reachable only through the local dark-launch gate. They render server state
        // verbatim; they never derive progress from client telemetry or offer a claim button for seals.
        static async void OpenContracts()
        {
            if (!ContractsMasteryFeature.Enabled)
            {
                ClearRows();
                GoalsPagesView.Unavailable(panel, false, Close,
                    () => Open(panel.transform.parent, home, "Mastery"));
                SetStatus("Contracts preview. Verified progression is unavailable in this build.");
                return;
            }
            ClearRows(); SetStatus("Loading Contracts…"); int version = screenVersion;
            var result = await BackendClient.Instance.GoalsAsync();
            if (Stale(version)) return;
            if (!result.Ok || result.Value == null) { SetStatus(result.Message); Retry("RETRY CONTRACTS", OpenContracts); return; }
            var goals = result.Value;
            GoalsPagesView.Contracts(panel, goals, Close, () => Open(panel.transform.parent, home, "Mastery"),
                async id =>
                {
                    SetStatus("Switching contract…"); int selectVersion = screenVersion;
                    var selected = await BackendClient.Instance.SelectContractAsync(id);
                    if (Stale(selectVersion)) return;
                    if (selected.Ok) OpenContracts(); else SetStatus(selected.Message);
                });
            SetStatus(goals.unlocked ? "Contract progress is verified by the backend."
                : "Contracts unlock after two eligible public matches.");
        }

        static async void OpenMastery()
        {
            if (!ContractsMasteryFeature.Enabled)
            {
                ClearRows();
                GoalsPagesView.Unavailable(panel, true, Close,
                    () => Open(panel.transform.parent, home, "Contracts"));
                SetStatus("Mastery preview. Verified progression is unavailable in this build.");
                return;
            }
            ClearRows(); SetStatus("Loading Mastery…"); int version = screenVersion;
            var result = await BackendClient.Instance.GoalsAsync();
            if (Stale(version)) return;
            if (!result.Ok || result.Value == null) { SetStatus(result.Message); Retry("RETRY MASTERY", OpenMastery); return; }
            var goals = result.Value;
            GoalsPagesView.Mastery(panel, goals, Close, () => Open(panel.transform.parent, home, "Contracts"));
            SetStatus("Permanent mastery progress is verified by the backend.");
        }

        static async void OpenAppearance()
        {
            ClosePreview();
            ClearRows();
            BuildLocalAppearance();
            RenderLocker(null, CosmeticLook.LocalEquipped, CosmeticLook.LocalWrap, CosmeticLook.LocalBadge);
            SetStatus("Loading your cosmetic inventory…");
            int version = screenVersion;
            var result = await BackendClient.Instance.GetInventoryAsync();
            if (Stale(version)) return;
            if (!result.Ok || result.Value == null)
            {
                SetStatus(result.Message);
                RenderLocker(null, CosmeticLook.LocalEquipped, CosmeticLook.LocalWrap,
                    CosmeticLook.LocalBadge, inventoryUnavailable: true);
                Retry("RETRY INVENTORY", OpenAppearance);
                return;
            }
            // QA-024: a live preview of the selected item, the currently equipped item marked, and
            // the equipped choice cached locally so the player's own avatar shows it in play.
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            // Amendment 3: each slot is read and cached on its own so a body style, weapon
            // wrap and badge can be equipped together.
            string equippedBody = profile.Ok && profile.Value != null
                ? (profile.Value.bodyStyleId ?? profile.Value.equippedCosmeticId ?? "") : CosmeticLook.LocalEquipped;
            string equippedWrap = profile.Ok && profile.Value != null ? (profile.Value.weaponWrapId ?? "") : CosmeticLook.LocalWrap;
            string equippedBadge = profile.Ok && profile.Value != null ? (profile.Value.badgeId ?? "") : CosmeticLook.LocalBadge;
            if (profile.Ok && profile.Value != null)
            {
                CosmeticLook.LocalEquipped = equippedBody;
                CosmeticLook.LocalWrap = equippedWrap;
                CosmeticLook.LocalBadge = equippedBadge;
            }
            ShowPreview(CosmeticLook.LocalComposite);
            var items = result.Value.items ?? Array.Empty<InventoryItemDto>();
            int currentLevel = profile.Ok && profile.Value != null ? Mathf.Max(1, profile.Value.level) : 0;
            SetStatus(!profile.Ok || profile.Value == null
                ? "Inventory loaded, but account equipment and level status could not be checked. Displayed equipment uses the last locally saved choice."
                : items.Length == 0 ? "No cosmetics are owned yet. Your standard pilot look remains available."
                : "Owned items shown. Locked cosmetics are unavailable; no shop prices are shown.");
            RenderLocker(items, equippedBody, equippedWrap, equippedBadge, currentLevel,
                result.Value.wrapUnlocks ?? Array.Empty<WrapUnlockDto>());
        }

        static string appearanceCategory = "Body Presentation";

        static void RenderLocker(InventoryItemDto[] items, string equippedBody, string equippedWrap,
            string equippedBadge, int currentLevel = 1, WrapUnlockDto[] wrapUnlocks = null,
            bool inventoryUnavailable = false)
        {
            AppearanceLockerView.Show(panel, previewImage, items, appearanceCategory, equippedBody,
                equippedWrap, equippedBadge, currentLevel, wrapUnlocks, inventoryUnavailable,
                status != null ? status.text : "", Close,
                () =>
                {
                    var owner = GameBootstrap.Instance;
                    if (owner == null) { SetStatus("Runner selection is unavailable outside the game."); return; }
                    Close(); owner.GetComponent<OfflineHomeScreen>()?.ShowRigChoice(false);
                },
                category =>
                {
                    appearanceCategory = category;
                    RenderLocker(items, equippedBody, equippedWrap, equippedBadge, currentLevel, wrapUnlocks, inventoryUnavailable);
                },
                item =>
                {
                    string id = item.cosmeticId;
                    ShowPreview(appearanceCategory == "Weapon Wrap"
                        ? equippedBody + "|" + id + "|" + equippedBadge
                        : appearanceCategory == "Badge"
                            ? equippedBody + "|" + equippedWrap + "|" + id
                            : id + "|" + equippedWrap + "|" + equippedBadge);
                    SetStatus($"Previewing {CosmeticLook.DisplayName(id)}. Source: {UnlockSource(item.acquiredVia)}.");
                },
                async item =>
                {
                    string id = item.cosmeticId;
                    SetStatus("Saving appearance…"); int equipVersion = screenVersion;
                    var result = await BackendClient.Instance.EquipAsync(id);
                    if (Stale(equipVersion)) return;
                    if (!result.Ok) { SetStatus(result.Message); return; }
                    if (appearanceCategory == "Weapon Wrap") CosmeticLook.LocalWrap = id;
                    else if (appearanceCategory == "Badge") CosmeticLook.LocalBadge = id;
                    else CosmeticLook.LocalEquipped = id;
                    OpenAppearance();
                },
                retry: OpenAppearance); // shown only while the inventory is unavailable
        }

        static void RenderAppearanceInventory(InventoryItemDto[] items, string equippedBody, string equippedWrap, string equippedBadge)
        {
            Transform inventoryParent = appearanceInventoryHost != null ? appearanceInventoryHost : content;
            var previous = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in inventoryParent)
                if (child.name.StartsWith("Row_Inventory", StringComparison.Ordinal)) previous.Add(child.gameObject);
            foreach (var child in previous) Discard(child);

            var tabs = MenuLayout.Row(inventoryParent, "Row_InventoryTabs");
            foreach (var entry in new[] { ("BODY", "Body Presentation"), ("STYLES", "Body Style"),
                ("WRAPS", "Weapon Wrap"), ("BADGES", "Badge") })
            {
                string category = entry.Item2;
                MenuLayout.AddButton(tabs, entry.Item1, () =>
                {
                    appearanceCategory = category;
                    RenderAppearanceInventory(items, equippedBody, equippedWrap, equippedBadge);
                }, "InventoryTab_" + entry.Item1, UiMetrics.MinTouchUnits,
                    variant: appearanceCategory == category ? ButtonVariant.Primary : ButtonVariant.Secondary);
            }
            var categoryItems = items.Where(i => CosmeticCategory(i.cosmeticId) == appearanceCategory).ToArray();
            if (categoryItems.Length == 0)
            {
                var empty = MenuLayout.Card(inventoryParent, "Row_InventoryEmpty");
                MenuLayout.AddText(empty, appearanceCategory.ToUpperInvariant(), 20, MatchHUD.Friendly);
                MenuLayout.AddText(empty, "No owned items in this category yet.", 19, MatchHUD.SecondaryText);
                return;
            }
            foreach (var item in categoryItems)
            {
                string cosmeticId = item.cosmeticId;
                string category = appearanceCategory;
                string equippedInSlot = category == "Weapon Wrap" ? equippedWrap : category == "Badge" ? equippedBadge : equippedBody;
                bool isEquipped = cosmeticId == equippedInSlot;
                var card = MenuLayout.Card(inventoryParent, "Row_InventoryItem_" + item.id);
                MenuLayout.AddText(card, (isEquipped ? "EQUIPPED  ·  " : "OWNED  ·  ") + category.ToUpperInvariant(), 19,
                    isEquipped ? MatchHUD.Friendly : MatchHUD.SecondaryText);
                MenuLayout.AddText(card, CosmeticLook.DisplayName(cosmeticId), 25, MatchHUD.MainText);
                MenuLayout.AddText(card, "Source: " + UnlockSource(item.acquiredVia), 18, MatchHUD.SecondaryText);
                var actions = MenuLayout.Row(card, "InventoryActions");
                MenuLayout.AddButton(actions, "PREVIEW", () =>
                {
                    ShowPreview(category == "Weapon Wrap" ? equippedBody + "|" + cosmeticId + "|" + equippedBadge
                        : category == "Badge" ? equippedBody + "|" + equippedWrap + "|" + cosmeticId
                        : cosmeticId + "|" + equippedWrap + "|" + equippedBadge);
                    SetStatus($"Previewing {CosmeticLook.DisplayName(cosmeticId)}. Unlock source: {UnlockSource(item.acquiredVia)}.");
                });
                if (!isEquipped)
                    MenuLayout.AddButton(actions, "EQUIP", async () =>
                    {
                        SetStatus("Saving appearance…");
                        int equipVersion = screenVersion;
                        var equipped = await BackendClient.Instance.EquipAsync(cosmeticId);
                        if (Stale(equipVersion)) return;
                        if (equipped.Ok)
                        {
                            if (category == "Weapon Wrap") CosmeticLook.LocalWrap = cosmeticId;
                            else if (category == "Badge") CosmeticLook.LocalBadge = cosmeticId;
                            else CosmeticLook.LocalEquipped = cosmeticId;
                            SetStatus($"Saved: {CosmeticLook.DisplayName(cosmeticId)}.");
                            OpenAppearance();
                        }
                        else SetStatus(equipped.Message);
                    }, variant: ButtonVariant.Primary);
            }
        }

        static void BuildLocalAppearance()
        {
            var hero = MenuLayout.Row(content, "Row_AppearanceHero");
            hero.GetComponent<LayoutElement>().preferredHeight = 500f;
            hero.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            appearancePreviewHost = hero;
            var information = MenuLayout.Card(hero, "AppearanceInformation", 500f);
            appearanceInventoryHost = information;
            MenuLayout.AddText(information, "YOUR RUNNER", 19, MatchHUD.Friendly);
            MenuLayout.AddText(information, "Drag to inspect · cosmetics never change power.", 19, MatchHUD.SecondaryText);
            MenuLayout.AddText(information, "OWNED EQUIPMENT", 22, MatchHUD.Friendly);
            ShowPreview(CosmeticLook.LocalComposite);
            MenuLayout.AddButton(content, "CHANGE RUNNER", () =>
            {
                var owner = GameBootstrap.Instance;
                if (owner == null) { SetStatus("Runner selection is not available outside the game."); return; }
                Close();
                owner.GetComponent<OfflineHomeScreen>()?.ShowRigChoice(false);
            }, "Row_AppearanceRunnerChoice", 84);
        }

        static async void OpenAccount()
        {
            ClearRows();
            SetStatus("Loading account recovery status…");
            int version = screenVersion;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            if (!profile.Ok || profile.Value == null) { SetStatus(profile.Message); Retry("RETRY ACCOUNT", OpenAccount); return; }
            Text(AccountStateLabel(profile.Value.isGuest), new Vector2(0, 220), 30, MatchHUD.Friendly);
            Text("Player: " + StreamSafePresentation.AccountAlias(profile.Value.alias), new Vector2(0, 170), 21, MatchHUD.MainText);
            Text(profile.Value.isGuest
                ? "Guest profiles are only on this device. Link Google Play Games to recover them after reinstall or on a new phone."
                : "Google Play Games is linked. Sign in again after reinstall to recover this profile.", new Vector2(0, 90), 20, MatchHUD.SecondaryText);
            if (!profile.Value.isGuest)
                Button("REVOKE GOOGLE PLAY LINK", new Vector2(0, -30), 490, 64, () => RevokeGooglePlayLink(null));
            else if (PlatformIdentity.Available)
            {
                // QA-026: link this guest now, or recover an account already linked elsewhere.
                BeginRow();
                Button("LINK GOOGLE PLAY", Vector2.zero, 0, 0, () => LinkGooglePlay(OpenAccount));
                Button("RECOVER LINKED ACCOUNT", Vector2.zero, 0, 0, () => RecoverLinkedAccount(OpenAccount));
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
            // QA-025: report someone from the last online match by the identity that was visible
            // in-game. The match context goes with the report; no account identifier is shown.
            // The view fills these two from its violation pills and incident log.
            reasonInput = Input("Harassment, cheating, or inappropriate name", Vector2.zero, 760, 58, 80);
            playerInput = Input("What happened?", Vector2.zero, 760, 58, 180);
            DrawSupport(null, null);
            SetStatus("");
            LoadSupportAccount();
        }

        static async void LoadSupportAccount()
        {
            int version = screenVersion;
            var profile = await BackendClient.Instance.GetProfileAsync();
            if (Stale(version)) return;
            if (!profile.Ok || profile.Value == null)
            {
                // Say the account could not be loaded instead of leaving the page "syncing";
                // reports, compliance and web deletion remain usable without it.
                DrawSupport(null, null, accountUnavailable: true);
                SetStatus(profile.Message);
                return;
            }
            var wallet = await BackendClient.Instance.GetWalletAsync();
            if (Stale(version)) return;
            DrawSupport(profile.Value, wallet.Ok ? wallet.Value : null, walletUnavailable: !wallet.Ok || wallet.Value == null);
            if (!wallet.Ok || wallet.Value == null)
                SetStatus("Credits could not be loaded. Retry account details to refresh.");
        }

        static void RetrySupportAccount()
        {
            DrawSupport(null, null);
            SetStatus("Loading account recovery status…");
            LoadSupportAccount();
        }

        static void DrawSupport(ProfileDto profile, WalletDto wallet, bool accountUnavailable = false, bool walletUnavailable = false)
        {
            reportTarget = null;
            bool hasRecent = RecentMatch.TryGet(out string recentMatchId, out var recentPlayers);
            var client = BackendClient.Instance;
            var actions = new SupportHubActions
            {
                Back = Close,
                Navigate = page => Open(panel.transform.parent, home, page),
                Recover = () => RecoverLinkedAccount(OpenSupport),
                Link = () => LinkGooglePlay(OpenSupport),
                Revoke = () => RevokeGooglePlayLink(OpenSupport),
                AccountSettings = () => Open(panel.transform.parent, home, "Account"),
                Profile = () => Open(panel.transform.parent, home, "Profile"),
                WebDeletion = () =>
                {
                    // Spec section 31: the same deletion request page players can reach without the app.
                    string url = client.WebUrl("/api/support/account-deletion");
                    if (string.IsNullOrEmpty(url)) { SetStatus("The web request page needs the online service, which this build does not have."); return; }
                    Application.OpenURL(url);
                    SetStatus("Opened the account-deletion request page in your browser.");
                },
                Diagnostics = () => SetStatus($"Build {Application.version}  •  {(client.IsConfigured ? "online service configured" : "offline build")}  •  {(client.HasSession ? "signed in" : "not signed in")}  •  {Application.platform}"),
                Choose = chosen => { reportTarget = chosen; SetStatus(ReportTargetMessage(chosen)); },
                Submit = () => SubmitReport(hasRecent, recentMatchId),
                Purge = ExecuteDelete,
                RetryAccount = RetrySupportAccount,
            };
            SupportHubView.Show(panel, reasonInput, playerInput, hasRecent ? recentMatchId : null, recentPlayers,
                RecentMatch.LocalTeam(), profile, wallet, client.HasSession, actions, accountUnavailable, walletUnavailable);
        }

        static async void SubmitReport(bool hasRecent, string matchId)
        {
            string detail = (playerInput != null ? playerInput.text ?? "" : "").Trim();
            string reason = (reasonInput != null ? reasonInput.text ?? "" : "").Trim();
            if (reportTarget == null) { SetStatus(hasRecent ? "Tap the participant you want to report." : "A recent online match is needed to select a participant."); return; }
            if (reason.Length < 3) { SetStatus("Choose a violation class for this report."); return; }
            SetStatus("Submitting report…");
            int version = screenVersion;
            var report = await BackendClient.Instance.ReportInMatchAsync(matchId, reportTarget.alias, reportTarget.slot,
                string.IsNullOrEmpty(detail) ? reason : reason + ": " + detail);
            if (Stale(version)) return;
            SetStatus(report.Ok ? "Report received. Thank you." : report.Message);
        }

        /// <summary>Step 2 of deletion: the typed confirmation (the 3-second hold opens it in the page).</summary>
        static void ConfirmDelete()
        {
            SupportHubView.RevealPurge(panel);
            SetStatus("Step 2 of 2 — type DELETE to confirm permanent deletion. This cannot be undone.");
        }

        static async void ExecuteDelete()
        {
            SetStatus("Deleting account data…");
            int version = screenVersion;
            var deleted = await BackendClient.Instance.DeleteAccountAsync();
            // Forget the local session even if the player already left this screen.
            if (deleted.Ok) BackendClient.Instance.ForgetLocalSession();
            if (Stale(version)) return;
            if (!deleted.Ok) { SetStatus(deleted.Message); return; }
            SupportHubView.Clear(panel);
            ClearRows();
            SetStatus("Account deleted. Purchase records are retained where required. Use BACK to return Home.");
        }

        // QA-026: link this guest now, or recover an account already linked elsewhere. Shared by
        // the Account and Support pages; reopen redraws the calling page after a change.
        static async void LinkGooglePlay(Action reopen)
        {
            if (!PlatformIdentity.Available) { SetStatus("Google Play Games is not set up in this build yet, so this profile cannot be linked."); return; }
            SetStatus("Opening Google Play Games…");
            int version = screenVersion;
            string code = await PlatformIdentity.RequestServerAuthCodeAsync();
            if (Stale(version)) return;
            if (string.IsNullOrEmpty(code)) { SetStatus("Google Play Games sign-in was cancelled."); return; }
            var link = await BackendClient.Instance.LinkGooglePlayAsync(code);
            if (Stale(version)) return;
            if (link.Ok) reopen?.Invoke();
            SetStatus(link.Ok ? "Linked. This account can now be recovered after reinstall."
                : link.Error == BackendError.Conflict ? "That Google Play identity belongs to another account. Use RECOVER to switch to it; accounts are not merged."
                : link.Message);
        }

        static async void RecoverLinkedAccount(Action reopen)
        {
            if (!PlatformIdentity.Available) { SetStatus("Google Play Games is not set up in this build yet, so linked accounts cannot be recovered here."); return; }
            SetStatus("Recovering your linked account…");
            int version = screenVersion;
            string code = await PlatformIdentity.RequestServerAuthCodeAsync();
            if (Stale(version)) return;
            if (string.IsNullOrEmpty(code)) { SetStatus("Google Play Games sign-in was cancelled."); return; }
            var signIn = await BackendClient.Instance.PlatformSignInAsync(code);
            if (Stale(version)) return;
            if (signIn.Ok) { RecentMatch.Forget(); reopen?.Invoke(); SetStatus("Recovered linked account. Its saved profile, inventory and credits are now active."); }
            else SetStatus(signIn.Error == BackendError.NotFound
                ? "No account is linked to this Google Play identity yet. Use LINK to protect this profile."
                : signIn.Message);
        }

        static async void RevokeGooglePlayLink(Action reopen)
        {
            SetStatus("Removing the recovery link…");
            int version = screenVersion;
            var revoke = await BackendClient.Instance.RevokeGooglePlayLinkAsync();
            if (Stale(version)) return;
            if (revoke.Ok) reopen?.Invoke();
            SetStatus(revoke.Ok ? "Link removed. This account is now guest-only and will not recover after reinstall." : revoke.Message);
        }

        // Compile-repair (data desk, 2026-09-28): the shared Button helper takes
        // UnityEventTools-compatible UnityAction; adapt the Action here instead of changing
        // every call site. FLAG FOR OWNER REVIEW — cosmetic edit only.
        static void Retry(string label, Action retry)
        {
            ClearRows(); // one retry action at a time; keep BACK/status untouched
            var notice = MenuLayout.Card(content, "Row_LoadFailure");
            MenuLayout.AddText(notice, "PAGE UNAVAILABLE", 24, MatchHUD.Objective);
            MenuLayout.AddText(notice, status != null ? status.text : "Unable to load this page.", 20, MatchHUD.SecondaryText);
            MenuLayout.AddButton(content, label, () => retry(), "Row_Retry", variant: ButtonVariant.Primary);
        }

        // ---- QA-024: Appearance preview (render texture of a mannequin wearing the item) ----
        static GameObject previewStage;
        static Transform previewMannequin;
        static RenderTexture previewTexture;
        static RawImage previewImage;
        static RectTransform appearancePreviewHost;
        static RectTransform appearanceInventoryHost;

        static void ShowPreview(string cosmeticId)
        {
            if (previewStage == null)
            {
                previewStage = new GameObject("AppearancePreviewStage");
                previewStage.transform.position = new Vector3(0, -800f, 0); // isolated from Home preview stages
                previewMannequin = CosmeticLook.BuildMannequin(previewStage.transform);
                previewMannequin.localRotation = Quaternion.Euler(0, 200f, 0);
                var light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(previewStage.transform, false);
                light.type = LightType.Point; light.range = 8f; light.intensity = 3f;
                light.transform.localPosition = new Vector3(-1.5f, 3f, -2f);
                previewTexture = new RenderTexture(512, 512, 16);
                var cam = new GameObject("PreviewCamera", typeof(UnityEngine.Camera)).GetComponent<UnityEngine.Camera>();
                cam.transform.SetParent(previewStage.transform, false);
                cam.transform.localPosition = new Vector3(0, 1.05f, -2.6f);
                cam.fieldOfView = 40f; cam.targetTexture = previewTexture;
                cam.nearClipPlane = 0.05f; cam.farClipPlane = 30f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
                cam.cullingMask = ~0;
            }
            CosmeticLook.Apply(previewMannequin, cosmeticId);
            if (previewImage == null && content != null)
            {
                var obj = new GameObject("Row_Preview", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                obj.transform.SetParent(appearancePreviewHost != null ? appearancePreviewHost : content, false);
                obj.transform.SetAsFirstSibling();
                obj.GetComponent<Image>().color = UIDesignTokens.SurfacePanel;
                obj.GetComponent<Image>().raycastTarget = false;
                previewImage = new GameObject("PreviewImage", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                previewImage.transform.SetParent(obj.transform, false);
                var imageRect = previewImage.rectTransform;
                imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.sizeDelta = new Vector2(400f, 400f);
                previewImage.texture = previewTexture;
                // The render is an actual match rig and gun, not a static asset. Keeping this
                // surface interactive lets the player inspect the selected appearance.
                previewImage.raycastTarget = true;
                var drag = previewImage.gameObject.AddComponent<AppearancePreviewDrag>();
                drag.Target = previewMannequin;
                var element = obj.GetComponent<LayoutElement>();
                element.preferredHeight = 500; element.minHeight = 400;
                element.preferredWidth = 420; element.minWidth = 300;
                element.flexibleWidth = 0;
            }
        }

        static void ClosePreview()
        {
            if (previewStage != null) Discard(previewStage);
            if (previewTexture != null) { previewTexture.Release(); Discard(previewTexture); }
            previewStage = null; previewMannequin = null; previewTexture = null; previewImage = null;
            appearancePreviewHost = null;
            appearanceInventoryHost = null;
        }

        static void Close()
        {
            ClosePreview();
            screenVersion++;
            navBack = null;
            status = null;
            var closingPanel = panel;
            panel = null;
            if (home != null) home.SetActive(true);
            if (closingPanel != null)
                UiScreenEnterMotion.Exit(closingPanel, () => Discard(closingPanel));
        }

        /// <summary>True when the screen that issued a request has since been closed or replaced.</summary>
        static bool Stale(int version) => version != screenVersion || panel == null;

        static string CosmeticCategory(string cosmeticId)
        {
            if (string.IsNullOrEmpty(cosmeticId) || cosmeticId == "body_standard") return "Body Presentation";
            if (cosmeticId.StartsWith("body_", StringComparison.Ordinal)) return "Body Style";
            if (cosmeticId.StartsWith("wrap_", StringComparison.Ordinal)) return "Weapon Wrap";
            if (cosmeticId.StartsWith("badge_", StringComparison.Ordinal)) return "Badge";
            return "Body Presentation";
        }

        static string AccountStateLabel(bool isGuest) => isGuest ? "GUEST ACCOUNT" : "LINKED ACCOUNT";

        static string ReportTargetMessage(RecentMatch.Entry target) =>
            target == null ? "Choose a participant to report." : $"Reporting {StreamSafePresentation.PlayerName(target.alias, target.slot)}, slot {target.slot + 1}. Add a category and submit.";

        static string UnlockSource(string acquiredVia)
        {
            if (string.Equals(acquiredVia, "starter", StringComparison.OrdinalIgnoreCase)) return "Starter collection";
            if (string.Equals(acquiredVia, "unlock", StringComparison.OrdinalIgnoreCase)) return "Eligible match unlock";
            // Purchase inventory remains displayable for existing owners, but the gated shop
            // stays disconnected and never invents a price or availability.
            if (!string.IsNullOrEmpty(acquiredVia) && acquiredVia.StartsWith("purchase:", StringComparison.OrdinalIgnoreCase)) return "Verified purchase";
            return string.IsNullOrEmpty(acquiredVia) ? "Unavailable" : acquiredVia;
        }

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

        static void SetStatus(string value)
        {
            string message = value ?? "Something went wrong. Try again.";
            if (status != null) status.text = message;
            PartyRoomView.SetStatus(panel, message);
            AppearanceLockerView.SetStatus(panel, message);
            OperativeProfileView.SetStatus(panel, message);
            SupportHubView.SetStatus(panel, message);
        }
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

    /// <summary>Small, input-only affordance for inspecting the real appearance preview rig.</summary>
    public sealed class AppearancePreviewDrag : MonoBehaviour, IDragHandler
    {
        public Transform Target { get; set; }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            Target.Rotate(0f, -eventData.delta.x * 0.45f, 0f, Space.Self);
        }
    }
}


