using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Updraft.Config;

namespace Updraft.Core
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
        public bool Ok => Error == BackendError.None;
        public BackendResult(T value, BackendError error, long status) { Value = value; Error = error; Status = status; }

        public string Message => BackendClient.Describe(Error);
    }

    [Serializable] public class SessionDto { public string playerId; public string accessToken; public string refreshToken; public string accessExpiresAt; }
    [Serializable] public class RefreshDto { public string refreshToken; }
    [Serializable] public class QueueStatusDto { public string state; public int elapsedSeconds; public string matchId; }
    [Serializable] public class TicketDto { public string ticket; public string expiresAt; public string matchId; public string session; public string region; public string team; public bool hasBots; public bool serverReady; public string note; }
    [Serializable] public class PartyDto { public string code; public string leaderId; public List<string> memberIds; public List<string> readyIds; }
    [Serializable] public class ProfileDto { public string id; public string alias; public int profileXp; public int level; public string equippedCosmeticId; public bool isGuest; }
    [Serializable] public class WalletDto { public int credits; }
    [Serializable] public class ErrorDto { public string error; }

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

        public static string Describe(BackendError error) => error switch
        {
            BackendError.None => "",
            BackendError.NotConfigured => "Online play isn't available in this build.",
            BackendError.NoInternet => "No internet connection. Check your connection and try again.",
            BackendError.AuthUnavailable => "Sign-in service is down. Try again shortly.",
            BackendError.ServerBusy => "Servers are full or paused. Try again shortly, or practice offline.",
            BackendError.UpdateRequired => "An update is required to play online.",
            BackendError.Unauthorized => "Your session expired. Reconnecting…",
            BackendError.NotFound => "Not found.",
            BackendError.Conflict => "That action conflicts with your current state.",
            BackendError.Invalid => "The request was rejected.",
            _ => "Something went wrong on our side. Try again.",
        };

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

        static BackendResult<SessionDto> Map(BackendResult<SessionDto> r, BackendError fallback) =>
            r.Error is BackendError.NoInternet or BackendError.UpdateRequired ? r : new BackendResult<SessionDto>(null, fallback, r.Status);

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
        public Task<BackendResult<ErrorDto>> EnqueueAsync() => AuthedAsync<ErrorDto>("POST", "/api/matchmaking/enqueue", "{}");
        public Task<BackendResult<ErrorDto>> CancelQueueAsync() => AuthedAsync<ErrorDto>("POST", "/api/matchmaking/cancel", "{}");
        public Task<BackendResult<QueueStatusDto>> QueueStatusAsync() => AuthedAsync<QueueStatusDto>("GET", "/api/matchmaking/status");
        public Task<BackendResult<TicketDto>> TicketAsync() => AuthedAsync<TicketDto>("GET", "/api/matchmaking/ticket");
        public Task<BackendResult<PartyDto>> CreatePartyAsync() => AuthedAsync<PartyDto>("POST", "/api/party/create", "{}");
        public Task<BackendResult<PartyDto>> JoinPartyAsync(string code) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/join", "{}");
        public Task<BackendResult<PartyDto>> LeavePartyAsync(string code) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/leave", "{}");
        public Task<BackendResult<PartyDto>> SetReadyAsync(string code, bool ready) => AuthedAsync<PartyDto>("POST", $"/api/party/{UnityWebRequest.EscapeURL(code)}/ready", ready ? "true" : "false");
        public Task<BackendResult<PartyDto>> CurrentPartyAsync() => AuthedAsync<PartyDto>("GET", "/api/party/current");

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
            if (!session.Ok) return new BackendResult<T>(null, session.Error, session.Status);
            var result = await SendAsync<T>(method, path, body, auth: true);
            if (result.Error != BackendError.Unauthorized) return result;
            accessToken = null; // expired or revoked: refresh once and retry
            session = await EnsureSessionAsync();
            return session.Ok ? await SendAsync<T>(method, path, body, auth: true) : new BackendResult<T>(null, session.Error, session.Status);
        }

        async Task<BackendResult<T>> SendAsync<T>(string method, string path, string body, bool auth) where T : class
        {
            using var request = new UnityWebRequest(config.baseUrl.TrimEnd('/') + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("X-Client-Protocol", config.clientProtocolVersion.ToString());
            if (auth && accessToken != null) request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = Mathf.CeilToInt(config.requestTimeoutSeconds);

            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            long status = request.responseCode;
            if (request.result == UnityWebRequest.Result.ConnectionError) return new BackendResult<T>(null, BackendError.NoInternet, status);
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
            if (error != BackendError.None) return new BackendResult<T>(null, error, status);
            string text = request.downloadHandler.text;
            T value = null;
            if (!string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
            {
                try { value = JsonUtility.FromJson<T>(text); }
                catch (Exception e) { Debug.LogWarning($"Backend response parse failed for {path}: {e.Message}"); }
            }
            return new BackendResult<T>(value, BackendError.None, status);
        }
    }
}
