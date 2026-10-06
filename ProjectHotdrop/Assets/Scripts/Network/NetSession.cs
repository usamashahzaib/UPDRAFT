using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

namespace Hotdrop.Network
{
    /// <summary>One player the server expects: from the backend claim, or a dev-mode identity.</summary>
    public class ExpectedPlayer
    {
        public string AccountId;
        public int Team;
        public string Alias;
        public string Cosmetic; // QA-024: equipped cosmetic from the backend claim (presentation only)
    }

    /// <summary>
    /// Owns one Fusion runner (spec section 21: Photon Fusion 2 Server mode). The server
    /// admits only players whose join ticket the backend confirms, gives each account a
    /// fixed slot on its matched team, keeps the avatar through disconnects (reconnect
    /// until match end), and fills empty slots with labeled bots. Clients send input only.
    /// </summary>
    public class NetSession : MonoBehaviour, INetworkRunnerCallbacks
    {
        public const int MatchSize = 6;
        public static NetSession Current { get; private set; }

        public NetworkRunner Runner { get; private set; }
        public bool IsServer { get; private set; }
        public string MatchId { get; private set; }
        public string FailureReason { get; private set; }
        public event Action<string> OnFailed;
        public event Action OnShutdownComplete;

        // ---- Server state ----
        ServerBackend backend;
        bool insecureDev;
        readonly List<ExpectedPlayer> expected = new List<ExpectedPlayer>();
        readonly Dictionary<string, ExpectedPlayer> admittedByTicket = new Dictionary<string, ExpectedPlayer>();
        readonly HashSet<string> pendingTickets = new HashSet<string>();
        readonly HashSet<string> consumedTickets = new HashSet<string>();
        NetMatch match;
        public NetMatch Match => match;
        public IReadOnlyList<ExpectedPlayer> Expected => expected;

        // ---- Client input sampling (set by NetClientPresenter) ----
        public Func<NetInput> InputSource;

        public static async Task<NetSession> StartServer(string session, string region, string matchId, IEnumerable<ExpectedPlayer> players,
            ServerBackend serverBackend, bool allowInsecureDevTickets)
        {
            var s = Create("NetServer");
            s.IsServer = true;
            s.MatchId = matchId;
            s.backend = serverBackend;
            s.insecureDev = allowInsecureDevTickets;
            s.expected.AddRange(players);
            var result = await s.StartRunner(GameMode.Server, session, region, null);
            if (!result.Ok) { s.Fail($"server start failed: {result.ShutdownReason}"); return s; }
            s.SpawnMatch();
            return s;
        }

        public static async Task<NetSession> StartClient(string session, string region, string ticket)
        {
            var s = Create("NetClient");
            var result = await s.StartRunner(GameMode.Client, session, region, Encoding.UTF8.GetBytes(ticket ?? ""));
            if (!result.Ok) s.Fail(Describe(result.ShutdownReason));
            return s;
        }

        static NetSession Create(string name)
        {
            var go = new GameObject(name);
            var s = go.AddComponent<NetSession>();
            s.Runner = go.AddComponent<NetworkRunner>();
            s.Runner.AddCallbacks(s);
            Current = s;
            return s;
        }

        // Default Fusion UDP port for Server mode. Fixed (rather than Fusion's default
        // ephemeral OS-assigned port) so ops can open one known port in a firewall/security
        // list instead of reading it from a log line per run. Override with HOTDROP_SERVER_PORT.
        const ushort DefaultServerPort = 27015;

        async Task<StartGameResult> StartRunner(GameMode mode, string session, string region, byte[] token)
        {
            Runner.ProvideInput = mode == GameMode.Client;
            var settings = PhotonAppSettings.Global.AppSettings.GetCopy();
            if (!string.IsNullOrEmpty(region)) settings.FixedRegion = region;
            var args = new StartGameArgs
            {
                GameMode = mode,
                SessionName = session,
                PlayerCount = MatchSize,
                IsVisible = false,          // matchmaking is the backend's job, never the Photon lobby
                IsOpen = true,
                ConnectionToken = token,
                CustomPhotonAppSettings = settings,
                EnableClientSessionCreation = false,
            };
            if (mode == GameMode.Server)
            {
                ushort port = ushort.TryParse(Environment.GetEnvironmentVariable("HOTDROP_SERVER_PORT"), out var p) ? p : DefaultServerPort;
                args.Address = NetAddress.Any(port);
            }
            return await Runner.StartGame(args);
        }

        public static string Describe(ShutdownReason reason) => reason switch
        {
            ShutdownReason.GameNotFound => "The match server is not available. Please queue again.",
            ShutdownReason.GameIsFull => "That match is already full.",
            ShutdownReason.ConnectionRefused => "The match server refused this connection (ticket expired or invalid).",
            ShutdownReason.PhotonCloudTimeout or ShutdownReason.ConnectionTimeout => "Connection timed out. Check your network and try again.",
            ShutdownReason.InvalidAuthentication or ShutdownReason.CustomAuthenticationFailed => "Online services rejected this build's credentials.",
            ShutdownReason.MaxCcuReached => "Online play is at capacity right now. Try again shortly.",
            ShutdownReason.IncompatibleConfiguration => "This app version cannot join that match. Please update.",
            _ => $"Disconnected ({reason}).",
        };

        void Fail(string reason)
        {
            FailureReason = reason;
            Debug.LogWarning($"[Net] {reason}");
            OnFailed?.Invoke(reason);
        }

        public async void Leave()
        {
            if (this == null) return; // already torn down (QA-016)
            if (Runner != null && Runner.IsRunning) await Runner.Shutdown();
            else if (this != null) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // ================= Server =================

        void SpawnMatch()
        {
            var prefab = Resources.Load<NetworkObject>("NetPrefabs/NetMatch");
            var obj = Runner.Spawn(prefab, Vector3.zero, Quaternion.identity, PlayerRef.None, (r, o) =>
            {
                var m = o.GetComponent<NetMatch>();
                int humans = 0;
                foreach (var p in expected) if (!string.IsNullOrEmpty(p.AccountId)) humans++;
                m.ExpectedHumans = humans;
            });
            match = obj.GetComponent<NetMatch>();
            // Bot-filled slots exist from the start so a 5-human match never waits (spec 14).
            for (int slot = 0; slot < MatchSize; slot++)
                if (!SlotReservedForHuman(slot)) SpawnAvatar(slot, PlayerRef.None, null, bot: true);
        }

        int TeamOfSlot(int slot) => slot < 3 ? 0 : 1;

        bool SlotReservedForHuman(int slot)
        {
            int team = TeamOfSlot(slot);
            int index = slot % 3, seen = 0;
            foreach (var p in expected)
                if (p.Team == team && seen++ == index) return true;
            return false;
        }

        int SlotFor(ExpectedPlayer player)
        {
            int index = 0;
            foreach (var p in expected)
            {
                if (p == player) return player.Team * 3 + index;
                if (p.Team == player.Team) index++;
            }
            return -1;
        }

        NetPlayer SpawnAvatar(int slot, PlayerRef owner, ExpectedPlayer identity, bool bot)
        {
            var prefab = Resources.Load<NetworkObject>("NetPrefabs/NetPlayer");
            var exit = NetConfigs.Map.SpawnExit(TeamOfSlot(slot), slot % 3);
            var obj = Runner.Spawn(prefab, exit.position, exit.rotation, owner, (r, o) =>
            {
                var p = o.GetComponent<NetPlayer>();
                p.Slot = slot;
                p.Team = TeamOfSlot(slot);
                p.BotControlled = bot;
                p.Connected = !bot;
                p.AccountId = identity?.AccountId ?? "";
                p.Alias = bot ? OfflineAlias.Next(slot * 7919 + (MatchId ?? "").GetHashCode()) : identity?.Alias ?? $"Player{slot + 1}";
                // C19: snapshot the human alias before any bot substitution can relabel it.
                if (!bot) p.HumanAliasAtJoin = p.Alias;
                p.Cosmetic = bot ? "" : identity?.Cosmetic ?? "";
            });
            if (owner != PlayerRef.None) Runner.SetPlayerObject(owner, obj);
            return obj.GetComponent<NetPlayer>();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            if (!IsServer) return;
            string ticket = token != null ? Encoding.UTF8.GetString(token) : "";
            if (string.IsNullOrEmpty(ticket) || pendingTickets.Contains(ticket) ||
                admittedByTicket.ContainsKey(ticket) || consumedTickets.Contains(ticket))
            { request.Refuse(); return; }

            if (backend == null || !backend.Configured)
            {
                // Local development only (explicit --insecure-dev flag): "dev:<team>:<alias>".
                var dev = insecureDev ? ParseDevTicket(ticket) : null;
                if (dev == null) { request.Refuse(); return; }
                if (HasConnectedIdentity(runner, dev)) { request.Refuse(); return; }
                admittedByTicket[ticket] = dev;
                request.Accept();
                return;
            }

            pendingTickets.Add(ticket);
            request.Waiting();
            ValidateAsync(request, ticket);
        }

        async void ValidateAsync(NetworkRunnerCallbackArgs.ConnectRequest request, string ticket)
        {
            try
            {
                var check = await backend.ValidateTicketAsync(MatchId, ticket);
                ExpectedPlayer player = null;
                if (check != null && string.Equals(check.matchId, MatchId, StringComparison.OrdinalIgnoreCase))
                    foreach (var p in expected)
                        if (string.Equals(p.AccountId, check.playerId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(check.team, p.Team == 0 ? "A" : "B", StringComparison.Ordinal))
                        { player = p; break; }

                // The reservation fixes account, team and slot. A second live connection
                // cannot take over the same input authority; a disconnected account can.
                if (player == null || HasConnectedIdentity(Runner, player) || Match?.Phase == Hotdrop.Match.MatchPhase.Results)
                { request.Refuse(); return; }
                admittedByTicket[ticket] = player;
                request.Accept();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Ticket validation failed closed: {e.GetType().Name}");
                request.Refuse();
            }
            finally { pendingTickets.Remove(ticket); }
        }

        static bool HasConnectedIdentity(NetworkRunner runner, ExpectedPlayer identity)
        {
            foreach (var player in NetPlayer.All)
                if (player.Runner == runner && player.Connected &&
                    string.Equals(player.AccountId.ToString(), identity.AccountId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        ExpectedPlayer ParseDevTicket(string ticket)
        {
            var parts = ticket.Split(':');
            if (parts.Length != 3 || parts[0] != "dev") return null;
            // Dev tickets claim the next unused human slot on the requested team.
            int team = parts[1] == "B" ? 1 : 0;
            string alias = parts[2].Length > 20 ? parts[2].Substring(0, 20) : parts[2];
            foreach (var p in expected)
                if (p.Team == team && p.Alias == alias) return p;
            foreach (var p in expected)
                if (p.Team == team && string.IsNullOrEmpty(p.Alias))
                {
                    p.Alias = alias;
                    return p;
                }
            return null;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!IsServer) return;
            var token = runner.GetPlayerConnectionToken(player);
            string ticket = token != null ? Encoding.UTF8.GetString(token) : "";
            if (!admittedByTicket.TryGetValue(ticket, out var identity) || !consumedTickets.Add(ticket))
            { runner.Disconnect(player); return; }
            admittedByTicket.Remove(ticket);
            if (Match?.Phase == Hotdrop.Match.MatchPhase.Results) { runner.Disconnect(player); return; }

            int slot = SlotFor(identity);
            if (slot < 0) { runner.Disconnect(player); return; }
            // Reconnect: the same account resumes its own avatar (spec 10).
            foreach (var existing in NetPlayer.All)
            {
                if (existing.Runner != runner || existing.Slot != slot) continue;
                // Spec 18: an AFK removal is final for this match — the labeled bot keeps the seat.
                if (existing.Connected || existing.AfkRemoved) { runner.Disconnect(player); return; }
                existing.Object.AssignInputAuthority(player);
                runner.SetPlayerObject(player, existing.Object);
                if (identity != null && !string.IsNullOrWhiteSpace(identity.Alias))
                {
                    existing.Alias = identity.Alias;
                    // C19: a resuming human replaces the bot label; re-snapshot the real alias.
                    existing.HumanAliasAtJoin = identity.Alias;
                }
                existing.Connected = true;
                existing.BotControlled = false;
                existing.DisconnectGrace = default;
                return;
            }
            SpawnAvatar(slot, player, identity, bot: false);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!IsServer) return;
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != runner || p.Object.InputAuthority != player) continue;
                p.Connected = false;
                // Spec 10: a labeled bot takes over after 5 s; the player may still reconnect.
                p.DisconnectGrace = TickTimer.CreateFromSeconds(runner, 5f);
                p.Object.RemoveInputAuthority();
            }
        }

        // ================= Client =================

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (InputSource != null) input.Set(InputSource());
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (!IsServer && shutdownReason != ShutdownReason.Ok && FailureReason == null) Fail(Describe(shutdownReason));
            OnShutdownComplete?.Invoke();
            if (this != null) Destroy(gameObject);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Fail($"Lost connection to the match server ({reason}).");
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Fail($"Could not reach the match server ({reason}).");

        // ================= Unused callbacks =================

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}


