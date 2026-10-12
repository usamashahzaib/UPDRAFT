using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Hotdrop.Config;

namespace Hotdrop.Core
{
    /// <summary>Spec section 17 explicit failure states — shown as text, never as an unexplained spinner.</summary>
    public enum BackendError
    {
        None, NotConfigured, NoInternet, AuthUnavailable, ServerBusy, UpdateRequired, Unauthorized, NotFound, Conflict, Invalid, ServerError,
    }

    public readonly struct BackendResult<T>
    {
        public readonly T Value;
        public readonly BackendError Error;
        public readonly long Status;
        public readonly string ServiceCode;
        public bool Ok => Error == BackendError.None;
        public BackendResult(T value, BackendError error, long status, string serviceCode = null)
        { Value = value; Error = error; Status = status; ServiceCode = serviceCode; }

        public string Message => BackendClient.Describe(Error, ServiceCode);
    }

    [Serializable] public class SessionDto { public string playerId; public string accessToken; public string refreshToken; public string accessExpiresAt; }
    [Serializable] public class RefreshDto { public string refreshToken; }
    [Serializable] public class ClientStatusDto { public string state; public int minClientProtocol; public string storeUrl; public string message; public string etaUtc; public string compensationNote; public bool maintenance; }
    [Serializable] public class QueueStatusDto { public string state; public int elapsedSeconds; public string matchId; public string region; public string message; public string etaUtc; public string compensationNote; }
    [Serializable] public class TicketDto { public string ticket; public string expiresAt; public string matchId; public string session; public string region; public string team; public bool hasBots; public bool serverReady; public string note; }
    [Serializable] public class ReconnectDto { public string matchId; public bool reconnectAllowed; }
    [Serializable] public class MatchReceiptDto { public bool recorded; public bool eligible; public int xpGained; public int creditsGained; public int level; public int? profileXpBefore; public int? profileXpAfter; public int xpPerLevel; public int maxLevel; public string[] unlocked; }
    [Serializable] public class PartyMemberDto { public string playerId; public string alias; public bool isReady; }
    [Serializable] public class PartyDto { public string code; public string leaderId; public List<string> memberIds; public List<string> readyIds; public PartyMemberDto[] members; }
    [Serializable] public class ProfileDto { public string id; public string alias; public int profileXp; public int level; public string equippedCosmeticId; public string linkedPlatformIdentity; public bool isGuest; public string bodyStyleId; public string weaponWrapId; public string badgeId; public string bodyRigId; }
    [Serializable] public class WalletDto { public long credits; }
    [Serializable] public class ProfileStatsDto { public int eligibleMatches; public int wins; public int draws; }
    [Serializable] public class ErrorDto { public string error; public string message; public string etaUtc; public string compensationNote; }
    [Serializable] public class InventoryItemDto { public long id; public string cosmeticId; public string acquiredVia; public int supportedBodyRigMask; }
    [Serializable] public class WrapUnlockDto { public int level; public string cosmeticId; }
    [Serializable] public class InventoryDto { public InventoryItemDto[] items; public WrapUnlockDto[] wrapUnlocks; }
    [Serializable] public class ReportRequestDto { public string reportedPlayerId; public string reason; public string matchId; public string reportedAlias; public int reportedSlot = -1; }
    [Serializable] public class LinkGooglePlayDto { public string serverAuthCode; }
    [Serializable] public class ContactEmailDto { public string email; }
    [Serializable] public class EquipDto { public string equipped; }
    [Serializable] public class SupportResultDto { public bool submitted; public bool requested; public bool deleted; }
    [Serializable] public class GoalTemplateDto { public string id; public int target; }
    [Serializable] public class GoalInstanceDto { public string id; public string templateId; public string status; public int progress; public int target; }
    [Serializable] public class MasteryProgressDto { public string trackId; public string weaponId; public int progress; }
    [Serializable] public class GoalsDto { public string catalogVersion; public bool unlocked; public int eligiblePublicMatches; public GoalTemplateDto[] templates; public GoalInstanceDto[] goals; public int seals; public bool rewardCatalogComplete; public MasteryProgressDto[] mastery; public int[] relayThresholds; public int[] routeThresholds; public int[] weaponThresholds; }

    /// <summary>
    /// HTTPS client for the single modular backend (spec sections 20/22). Ownership
    /// always comes from the session token, never from request bodies. The refresh
    /// token is kept in app-private preferences; no server secret ever lives here.
    /// </summary>
    public class BackendClient
    {
        const string RefreshKey = "Session.RefreshToken";
        const string PlayerKey = "Session.PlayerId";

        static BackendClient instance;
        public static BackendClient Instance => instance ??= new BackendClient(BackendConfig.Load());

        readonly BackendConfig config;
        string accessToken;
        DateTime accessExpiresUtc;
        public string PlayerId { get; private set; }

        public BackendClient(BackendConfig config)
        {
            this.config = config;
            PlayerId = PlayerPrefs.GetString(PlayerKey, "");
        }

        public bool IsConfigured => config != null && config.IsConfigured;
        public bool HasSession => !string.IsNullOrEmpty(accessToken) && DateTime.UtcNow < accessExpiresUtc;
        public bool HasSavedSession => !string.IsNullOrEmpty(PlayerPrefs.GetString(RefreshKey, ""));
        /// <summary>An absolute page on the configured backend (for browser hand-offs), or null offline.</summary>
        public string WebUrl(string path) => IsConfigured ? config.baseUrl.TrimEnd('/') + path : null;

        public static string Describe(BackendError error, string serviceCode = null)
        {
            string localizedCode = serviceCode switch
            {
                "already_in_party" => Hotdrop.UI.UiDraw.L("backend.code.alreadyInParty", "You're already in a party."),
                "party_full" => Hotdrop.UI.UiDraw.L("backend.code.partyFull", "That party is full."),
                "party_not_found" => Hotdrop.UI.UiDraw.L("backend.code.partyNotFound", "No party was found for that code."),
                "party_membership_not_found" => Hotdrop.UI.UiDraw.L("product.party.notInParty", "You are not in a party yet. Create one or join with a code."),
                "party_membership_required" => Hotdrop.UI.UiDraw.L("backend.code.partyMembershipRequired", "You are not a member of that party."),
                "party_leader_required" => Hotdrop.UI.UiDraw.L("backend.code.partyLeaderRequired", "Only the party leader can start the match search."),
                "profile_not_found" => Hotdrop.UI.UiDraw.L("backend.code.profileNotFound", "Your profile could not be found."),
                "cosmetic_not_owned" => Hotdrop.UI.UiDraw.L("backend.code.cosmeticNotOwned", "That cosmetic isn't in your owned inventory."),
                "party_unavailable" => Hotdrop.UI.UiDraw.L("backend.code.partyUnavailable", "Your party is no longer available."),
                "party_not_ready" => Hotdrop.UI.UiDraw.L("online.partyNotReady", "Everyone in your party must be Ready before the leader can queue."),
                "already_queued" => Hotdrop.UI.UiDraw.L("backend.code.alreadyQueued", "Your match search is already active."),
                "reservation_starting" => Hotdrop.UI.UiDraw.L("online.reservationStarting", "Your reservation is already starting. Continue joining the match, or cancel and return Home."),
                "matchmaking_disabled" => Hotdrop.UI.UiDraw.L("online.matchmakingPaused", "Matchmaking is paused right now. Practice offline and try again soon."),
                "maintenance" => Hotdrop.UI.UiDraw.L("online.matchmakingPaused", "Matchmaking is paused right now. Practice offline and try again soon."),
                "capacity" => Hotdrop.UI.UiDraw.L("backend.error.serverBusy", "Servers are full or paused. Try again shortly, or practice offline."),
                "google_play_games_not_configured" => Hotdrop.UI.UiDraw.L("backend.code.platformLinkUnavailable", "Google Play Games linking and recovery are unavailable in this build."),
                "server_auth_code_required" or "google_identity_verification_failed" => Hotdrop.UI.UiDraw.L("product.account.signInFailed", "Google Play Games sign-in could not be completed. Check the app setup or try again."),
                "no_linked_account" => Hotdrop.UI.UiDraw.L("product.account.noLinkedAccount", "No account is linked to this Google Play identity yet. Use LINK to protect this profile."),
                "google_identity_already_linked" => Hotdrop.UI.UiDraw.L("product.account.linkConflict", "That Google Play identity belongs to another account. Use RECOVER to switch to it; accounts are not merged."),
                "account_already_linked" => Hotdrop.UI.UiDraw.L("backend.code.accountAlreadyLinked", "This profile already has a different Google Play recovery link."),
                "valid_report_target_and_reason_required" => Hotdrop.UI.UiDraw.L("backend.code.reportTargetAndReasonRequired", "Choose a report target and a reason before submitting."),
                "reported_player_not_found" => Hotdrop.UI.UiDraw.L("backend.code.reportTargetUnavailable", "That player is no longer available in the match."),
                "cannot_report_self" => Hotdrop.UI.UiDraw.L("backend.code.cannotReportSelf", "You can't report your own account."),
                "valid_name_and_email_required" => Hotdrop.UI.UiDraw.L("backend.code.validNameEmailRequired", "Enter your name and a valid email address."),
                "reporter_not_in_match" => Hotdrop.UI.UiDraw.L("backend.code.reporterNotInMatch", "Only a participant in that match can file this report."),
                "valid_email_required" => Hotdrop.UI.UiDraw.L("product.account.invalidEmail", "Enter a valid email address."),
                "unknown_cosmetic_category" => Hotdrop.UI.UiDraw.L("backend.code.cosmeticCategoryUnavailable", "That cosmetic slot is not available."),
                "contracts_locked_or_invalid" => Hotdrop.UI.UiDraw.L("backend.code.contractUnavailable", "This contract can't be selected right now."),
                "purchases_disabled" => Hotdrop.UI.UiDraw.L("shop.status.conceptOnly", "Concept only. Purchases are unavailable in this build."),
                "invalid_purchase_request" => Hotdrop.UI.UiDraw.L("backend.code.purchaseRequestInvalid", "The purchase request could not be accepted."),
                "purchase_token_conflict" => Hotdrop.UI.UiDraw.L("backend.code.purchaseTokenConflict", "This purchase request can't be linked to this profile. Contact support if you think this is a mistake."),
                "linked_google_identity_required" => Hotdrop.UI.UiDraw.L("product.account.purchaseLinkRequired", "Link Google Play Games before making purchases."),
                _ => null,
            };
            if (!string.IsNullOrEmpty(localizedCode)) return localizedCode;

            return error switch
        {
            BackendError.None => "",
            BackendError.NotConfigured => Hotdrop.UI.UiDraw.L("backend.error.notConfigured", "Online play isn't available in this build."),
            BackendError.NoInternet => Hotdrop.UI.UiDraw.L("backend.error.noInternet", "No internet connection. Check your connection and try again."),
            BackendError.AuthUnavailable => Hotdrop.UI.UiDraw.L("backend.error.authUnavailable", "Sign-in service is down. Try again shortly."),
            BackendError.ServerBusy => Hotdrop.UI.UiDraw.L("backend.error.serverBusy", "Servers are full or paused. Try again shortly, or practice offline."),
            BackendError.UpdateRequired => Hotdrop.UI.UiDraw.L("backend.error.updateRequired", "An update is required to play online."),
            BackendError.Unauthorized => Hotdrop.UI.UiDraw.L("backend.error.unauthorized", "Your session expired. Reconnecting…"),
            BackendError.NotFound => Hotdrop.UI.UiDraw.L("backend.error.notFound", "Not found."),
            BackendError.Conflict => Hotdrop.UI.UiDraw.L("backend.error.conflict", "That action conflicts with your current state."),
            BackendError.Invalid => Hotdrop.UI.UiDraw.L("backend.error.invalid", "The request was rejected."),
            _ => Hotdrop.UI.UiDraw.L("backend.error.serverError", "Something went wrong on our side. Try again."),
        };
        }

        public static string QueueMaintenanceMessage(string message, string etaUtc, string compensationNote)
        {
            string result = string.IsNullOrWhiteSpace(message)
                ? Hotdrop.UI.UiDraw.L("online.maintenanceFallback", "Online matchmaking is under maintenance. You can keep browsing Home or practice offline.")
                : message.Trim();
            if (!string.IsNullOrWhiteSpace(etaUtc))
                result += Hotdrop.UI.UiDraw.LFormat("online.expectedUpdateTemplate", "\nExpected update: {0} UTC.", etaUtc.Trim());
            if (!string.IsNullOrWhiteSpace(compensationNote)) result += $"\n{compensationNote.Trim()}";
            return result;
        }

        // ---- Session ----

        public async Task<BackendResult<SessionDto>> EnsureSessionAsync()
        {
            if (!IsConfigured) return new BackendResult<SessionDto>(null, BackendError.NotConfigured, 0);
            if (HasSession) return new BackendResult<SessionDto>(null, BackendError.None, 200);

            string refresh = PlayerPrefs.GetString(RefreshKey, "");
            BackendResult<SessionDto> result;
            if (!string.IsNullOrEmpty(refresh))
            {
                result = await SendAsync<SessionDto>("POST", "/api/auth/refresh", JsonUtility.ToJson(new RefreshDto { refreshToken = refresh }), auth: false);
                if (result.Ok) { Store(result.Value); return result; }
                if (result.Error != BackendError.Unauthorized) return Map(result, BackendError.AuthUnavailable);
            }
            result = await SendAsync<SessionDto>("POST", "/api/auth/guest", "{}", auth: false);
            if (result.Ok) Store(result.Value);
            return result.Ok ? result : Map(result, BackendError.AuthUnavailable);
        }

        /// <summary>
        /// QA-026: recover the account already linked to this platform identity (reinstall or new
        /// device). Replaces the local session only on success; NotFound means nothing is linked.
        /// </summary>
        public async Task<BackendResult<SessionDto>> PlatformSignInAsync(string serverAuthCode)
        {
            if (!IsConfigured) return new BackendResult<SessionDto>(null, BackendError.NotConfigured, 0);
            var result = await SendAsync<SessionDto>("POST", "/api/auth/platform-signin", JsonUtility.ToJson(new LinkGooglePlayDto { serverAuthCode = serverAuthCode }), auth: false);
            if (result.Ok && result.Value != null) Store(result.Value);
            return result;
        }

        static BackendResult<SessionDto> Map(BackendResult<SessionDto> r, BackendError fallback) =>
            r.Error is BackendError.NoInternet or BackendError.UpdateRequired ? r : new BackendResult<SessionDto>(null, fallback, r.Status, r.ServiceCode);

        void Store(SessionDto session)
        {
            accessToken = session.accessToken;
            accessExpiresUtc = DateTime.TryParse(session.accessExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var at)
                ? at.ToUniversalTime().AddSeconds(-30) : DateTime.UtcNow.AddMinutes(10);
            PlayerId = session.playerId;
            PlayerPrefs.SetString(RefreshKey, session.refreshToken);
            PlayerPrefs.SetString(PlayerKey, session.playerId);
            PlayerPrefs.Save();
        }

        // ---- Game APIs ----

        public Task<BackendResult<ProfileDto>> GetProfileAsync() => AuthedAsync<ProfileDto>("GET", "/api/profile/");
        public Task<BackendResult<WalletDto>> GetWalletAsync() => AuthedAsync<WalletDto>("GET", "/api/profile/wallet");
        public Task<BackendResult<ProfileStatsDto>> GetProfileStatsAsync() => AuthedAsync<ProfileStatsDto>("GET", "/api/profile/stats");
        public Task<BackendResult<InventoryDto>> GetInventoryAsync() => AuthedAsync<InventoryDto>("GET", "/api/profile/inventory");
        public Task<BackendResult<EquipDto>> EquipAsync(string cosmeticId) =>
            AuthedAsync<EquipDto>("POST", "/api/profile/equip/" + UnityWebRequest.EscapeURL(cosmeticId), "{}");
        public Task<BackendResult<ErrorDto>> EnqueueAsync() => AuthedAsync<ErrorDto>("POST", "/api/matchmaking/enqueue", "{}");
        public Task<BackendResult<ErrorDto>> CancelQueueAsync() => AuthedAsync<ErrorDto>("POST", "/api/matchmaking/cancel", "{}");
        public Task<BackendResult<QueueStatusDto>> QueueStatusAsync() => AuthedAsync<QueueStatusDto>("GET", "/api/matchmaking/status");
        public Task<BackendResult<ClientStatusDto>> ClientStatusAsync() => IsConfigured
            ? SendAsync<ClientStatusDto>("GET", "/api/client/status", null, auth: false)
            : Task.FromResult(new BackendResult<ClientStatusDto>(null, BackendError.NotConfigured, 0));
        public Task<BackendResult<TicketDto>> TicketAsync() => AuthedAsync<TicketDto>("GET", "/api/matchmaking/ticket");
        public Task<BackendResult<MatchReceiptDto>> MatchReceiptAsync(string matchId) =>
            AuthedAsync<MatchReceiptDto>("GET", $"/api/matches/{UnityWebRequest.EscapeURL(matchId)}/receipt");
        public Task<BackendResult<ReconnectDto>> ReconnectAsync(string matchId) =>
            AuthedAsync<ReconnectDto>("POST", $"/api/matches/{UnityWebRequest.EscapeURL(matchId)}/reconnect", "{}");
        public Task<BackendResult<GoalsDto>> GoalsAsync() => AuthedAsync<GoalsDto>("GET", "/api/goals/");
        public Task<BackendResult<GoalInstanceDto>> SelectContractAsync(string templateId) =>
            AuthedAsync<GoalInstanceDto>("POST", "/api/goals/contracts/" + UnityWebRequest.EscapeURL(templateId), "{}");
        public Task<BackendResult<PartyDto>> CreatePartyAsync() => AuthedAsync<PartyDto>("POST", "/api/party/create", "{}");
        public Task<BackendResult<PartyDto>> JoinPartyAsync(string code) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/join", "{}");
        public Task<BackendResult<PartyDto>> LeavePartyAsync(string code) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/leave", "{}");
        public Task<BackendResult<PartyDto>> SetReadyAsync(string code, bool ready) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/ready", ready ? "true" : "false");
        public Task<BackendResult<PartyDto>> CurrentPartyAsync() => AuthedAsync<PartyDto>("GET", "/api/party/current");
        /// <summary>QA-025: report someone from a match by what was visible in-app (their alias and slot).</summary>
        public Task<BackendResult<ErrorDto>> ReportInMatchAsync(string matchId, string alias, int slot, string reason) =>
            AuthedAsync<ErrorDto>("POST", "/api/support/report", JsonUtility.ToJson(new ReportRequestDto { matchId = matchId, reportedAlias = alias, reportedSlot = slot, reason = reason }));
        public Task<BackendResult<ErrorDto>> ReportAsync(string reportedPlayerId, string reason) =>
            AuthedAsync<ErrorDto>("POST", "/api/support/report", JsonUtility.ToJson(new ReportRequestDto { reportedPlayerId = reportedPlayerId, reason = reason }));
        public Task<BackendResult<SupportResultDto>> RequestDeletionAsync() =>
            AuthedAsync<SupportResultDto>("POST", "/api/support/account-deletion", "{}");
        public Task<BackendResult<SupportResultDto>> DeleteAccountAsync() =>
            AuthedAsync<SupportResultDto>("DELETE", "/api/support/account");
        /// <summary>QA-028: a contact email lets the public web form verify and complete account deletion.</summary>
        public Task<BackendResult<ErrorDto>> SetContactEmailAsync(string email) =>
            AuthedAsync<ErrorDto>("PUT", "/api/support/contact-email", JsonUtility.ToJson(new ContactEmailDto { email = email }));
        public Task<BackendResult<ErrorDto>> RemoveContactEmailAsync() => AuthedAsync<ErrorDto>("DELETE", "/api/support/contact-email");
        public Task<BackendResult<ErrorDto>> RevokeGooglePlayLinkAsync() =>
            AuthedAsync<ErrorDto>("DELETE", "/api/auth/google-play");
        public Task<BackendResult<ErrorDto>> LinkGooglePlayAsync(string serverAuthCode) =>
            AuthedAsync<ErrorDto>("POST", "/api/auth/link-google-play", JsonUtility.ToJson(new LinkGooglePlayDto { serverAuthCode = serverAuthCode }));

        public void ForgetLocalSession()
        {
            accessToken = null;
            accessExpiresUtc = DateTime.MinValue;
            PlayerId = "";
            PlayerPrefs.DeleteKey(RefreshKey);
            PlayerPrefs.DeleteKey(PlayerKey);
            PlayerPrefs.Save();
        }

        /// <summary>Uploads queued client-owned events in batches; offline match facts stay local.</summary>
        public async Task<int> UploadTelemetryAsync()
        {
            if (!IsConfigured) return 0;
            var lines = Telemetry.ReadQueued();
            if (lines.Length == 0) return 0;
            var keep = new List<string>();
            var batch = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("\"source\":\"offline_sim\"")) { keep.Add(line); continue; }
                batch.Add(line);
            }
            int sent = 0;
            for (int i = 0; i < batch.Count; i += 50)
            {
                int n = Math.Min(50, batch.Count - i);
                string body = "[" + string.Join(",", batch.GetRange(i, n)) + "]";
                var result = await AuthedAsync<ErrorDto>("POST", "/api/analytics/events/batch", body);
                if (!result.Ok) { keep.AddRange(batch.GetRange(i, batch.Count - i)); break; }
                sent += n;
            }
            Telemetry.ReplaceQueue(keep);
            return sent;
        }

        // ---- Transport ----

        async Task<BackendResult<T>> AuthedAsync<T>(string method, string path, string body = null) where T : class
        {
            var session = await EnsureSessionAsync();
            if (!session.Ok) return new BackendResult<T>(null, session.Error, session.Status, session.ServiceCode);
            var result = await SendAsync<T>(method, path, body, auth: true);
            if (result.Error != BackendError.Unauthorized) return result;
            accessToken = null; // expired or revoked: refresh once and retry
            session = await EnsureSessionAsync();
            return session.Ok ? await SendAsync<T>(method, path, body, auth: true) : new BackendResult<T>(null, session.Error, session.Status, session.ServiceCode);
        }

        async Task<BackendResult<T>> SendAsync<T>(string method, string path, string body, bool auth) where T : class
        {
            long status = 0;
            try
            {
                using var request = new UnityWebRequest(config.baseUrl.TrimEnd('/') + path, method);
                request.downloadHandler = new DownloadHandlerBuffer();
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.SetRequestHeader("X-Client-Protocol", config.clientProtocolVersion.ToString());
                request.SetRequestHeader("X-Client-Platform", Application.platform switch
                {
                    RuntimePlatform.Android => "android",
                    RuntimePlatform.IPhonePlayer => "ios",
                    _ => "other",
                });
                if (auth && accessToken != null) request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                request.timeout = Mathf.CeilToInt(config.requestTimeoutSeconds);

                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                status = request.responseCode;
                if (request.result == UnityWebRequest.Result.ConnectionError)
                    return new BackendResult<T>(null, BackendError.NoInternet, status);
                if (request.result == UnityWebRequest.Result.DataProcessingError)
                    return new BackendResult<T>(null, BackendError.ServerError, status);

                BackendError error = status switch
                {
                    >= 200 and < 300 => BackendError.None,
                    401 => BackendError.Unauthorized,
                    404 => BackendError.NotFound,
                    409 => BackendError.Conflict,
                    426 => BackendError.UpdateRequired,
                    429 or 503 => BackendError.ServerBusy,
                    >= 400 and < 500 => BackendError.Invalid,
                    _ => BackendError.ServerError,
                };
                string text = request.downloadHandler.text;
                T value = null;
                if (!string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
                {
                    try { value = JsonUtility.FromJson<T>(text); }
                    catch (Exception e) { Debug.LogWarning($"Backend response parse failed for {path}: {e.Message}"); }
                }

                // Every client endpoint used here returns a JSON object on success. A 2xx
                // with an empty, malformed, or wrong-shaped body is not a confirmed result:
                // treating it as success can tell players a party/action completed when the
                // client has no authoritative state to render.
                if (error == BackendError.None && value == null)
                {
                    Debug.LogWarning($"Backend returned no readable {typeof(T).Name} response for {method} {path}.");
                    return new BackendResult<T>(null, BackendError.ServerError, status);
                }
                string serviceCode = null;
                if (error != BackendError.None && !string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
                {
                    try { serviceCode = JsonUtility.FromJson<ErrorDto>(text)?.error; }
                    catch (Exception e) { Debug.LogWarning($"Backend error code parse failed for {path}: {e.Message}"); }
                }
                return new BackendResult<T>(value, error, status, serviceCode);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Backend request failed for {method} {path}: {e.Message}");
                return new BackendResult<T>(null, BackendError.ServerError, status);
            }
        }
    }
}


