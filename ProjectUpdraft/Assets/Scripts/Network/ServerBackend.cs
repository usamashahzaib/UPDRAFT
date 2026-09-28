using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Updraft.Network
{
    [Serializable] public class ServerTokenDto { public string token; public int expiresInSeconds; }
    [Serializable] public class ClaimedPlayerDto { public string playerId; public string team; public string alias; }
    [Serializable] public class ClaimDto { public string matchId; public string session; public string region; public bool hasBots; public List<ClaimedPlayerDto> players; }
    [Serializable] public class TicketCheckDto { public string playerId; public string matchId; public string team; public string alias; }
    [Serializable] public class ResultParticipantDto { public string PlayerId; public bool Won; public bool Eligible; public bool HadBotSubstitution; public string Team; }
    [Serializable] public class ResultDto { public bool Draw; public List<ResultParticipantDto> Participants; public bool ServerFailed; public bool HasBots; }

    /// <summary>
    /// The dedicated server's side of the backend contract (spec section 21): claim a
    /// composed match, validate each joining player's ticket, report the result. The
    /// server key comes from the UPDRAFT_SERVER_KEY environment variable only; it is
    /// never serialized into the build or the repository.
    /// </summary>
    public class ServerBackend
    {
        readonly string baseUrl;
        readonly string serverKey;
        readonly string serverId;
        string token;
        DateTime tokenExpiresUtc;

        public ServerBackend(string baseUrl, string serverKey, string serverId)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.serverKey = serverKey;
            this.serverId = serverId;
        }

        public bool Configured => !string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(serverKey);

        async Task<bool> EnsureTokenAsync()
        {
            if (!string.IsNullOrEmpty(token) && DateTime.UtcNow < tokenExpiresUtc) return true;
            var (status, body) = await SendAsync("POST", "/api/auth/server-token", JsonUtility.ToJson(new ServerIdBody { ServerId = serverId }), authed: false);
            if (status != 200) { Debug.LogWarning($"[Server] server-token failed ({status})"); return false; }
            var dto = JsonUtility.FromJson<ServerTokenDto>(body);
            token = dto.token;
            tokenExpiresUtc = DateTime.UtcNow.AddSeconds(Mathf.Max(30, dto.expiresInSeconds - 60));
            return true;
        }

        [Serializable] class ServerIdBody { public string ServerId; }
        [Serializable] class TicketBody { public string MatchId; public string Ticket; }

        /// <summary>Null when there is nothing to host (204) or the pool is at capacity (503).</summary>
        public async Task<ClaimDto> ClaimAsync()
        {
            if (!await EnsureTokenAsync()) return null;
            var (status, body) = await SendAsync("POST", "/api/matches/claim", "{}", authed: true);
            return status == 200 ? JsonUtility.FromJson<ClaimDto>(body) : null;
        }

        public async Task<TicketCheckDto> ValidateTicketAsync(string matchId, string ticket)
        {
            if (!await EnsureTokenAsync()) return null;
            var (status, body) = await SendAsync("POST", "/api/matches/validate-ticket",
                JsonUtility.ToJson(new TicketBody { MatchId = matchId, Ticket = ticket }), authed: true);
            return status == 200 ? JsonUtility.FromJson<TicketCheckDto>(body) : null;
        }

        public async Task<bool> SubmitResultAsync(string matchId, ResultDto result)
        {
            // Results matter for rating and progression: retry transient failures a few times.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (await EnsureTokenAsync())
                {
                    var (status, _) = await SendAsync("POST", $"/api/matches/{matchId}/result", JsonUtility.ToJson(result), authed: true);
                    if (status == 200) return true;
                    if (status >= 400 && status < 500 && status != 401) return false;
                    if (status == 401) token = null;
                }
                await Task.Delay(1000 * (attempt + 1));
            }
            return false;
        }

        async Task<(long status, string body)> SendAsync(string method, string path, string json, bool authed)
        {
            using var request = new UnityWebRequest(baseUrl + path, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json ?? "")),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            if (authed) request.SetRequestHeader("Authorization", "Bearer " + token);
            else request.SetRequestHeader("X-Server-Key", serverKey);
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            return (request.responseCode, request.downloadHandler?.text ?? "");
        }
    }
}
