using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Updraft.Network
{
    [Serializable] public class ServerTokenDto { public string token; public int expiresInSeconds; }
    [Serializable] public class ClaimedPlayerDto { public string playerId; public string team; public string alias; public string cosmetic; }
    [Serializable] public class ClaimDto { public string matchId; public string session; public string region; public bool hasBots; public List<ClaimedPlayerDto> players; }
    [Serializable] public class TicketCheckDto { public string playerId; public string matchId; public string team; public string alias; }
    [Serializable] public class ResultParticipantDto { public string PlayerId; public bool Won; public bool Eligible; public bool HadBotSubstitution; public string Team; public int Slot = -1; }
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

        readonly Updraft.Core.PendingResultStore outbox;

        public ServerBackend(string baseUrl, string serverKey, string serverId, Updraft.Core.PendingResultStore outbox = null)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.serverKey = serverKey;
            this.serverId = serverId;
            this.outbox = outbox;
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

        /// <summary>
        /// QA-017: results matter for rating and progression. Transient failures are retried a few
        /// times; if the backend is still unreachable the result goes to the durable outbox and is
        /// re-delivered later (<see cref="FlushOutboxAsync"/>). The backend records each participant
        /// once per match, so re-delivery never grants twice.
        /// </summary>
        public async Task<bool> SubmitResultAsync(string matchId, ResultDto result)
        {
            string json = JsonUtility.ToJson(result);
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var delivery = await TryDeliverAsync(matchId, json);
                if (delivery == Delivery.Delivered) return true;
                if (delivery == Delivery.Rejected) return false;
                await Task.Delay(1000 * (attempt + 1));
            }
            if (outbox != null)
            {
                outbox.Save(matchId, json);
                Debug.LogWarning($"[Server] result for {matchId} queued in the outbox for re-delivery");
            }
            return false;
        }

        /// <summary>Re-delivers every queued result once; keeps the ones that still fail transiently.</summary>
        public async Task FlushOutboxAsync()
        {
            if (outbox == null) return;
            foreach (var (matchId, json) in outbox.Pending())
            {
                var delivery = await TryDeliverAsync(matchId, json);
                if (delivery == Delivery.Transient) continue;
                outbox.Remove(matchId);
                Debug.Log($"[Server] outbox result {matchId}: {delivery}");
            }
        }

        enum Delivery { Delivered, Rejected, Transient }

        async Task<Delivery> TryDeliverAsync(string matchId, string json)
        {
            if (!await EnsureTokenAsync()) return Delivery.Transient;
            var (status, _) = await SendAsync("POST", $"/api/matches/{matchId}/result", json, authed: true);
            if (status == 200 || status == 409) return Delivery.Delivered; // 409: already recorded
            if (status == 401) { token = null; return Delivery.Transient; }
            if (status >= 400 && status < 500 && status != 408 && status != 429) return Delivery.Rejected; // malformed: retrying cannot help
            return Delivery.Transient;
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
