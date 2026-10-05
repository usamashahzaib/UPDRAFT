using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Hotdrop.Match;

namespace Hotdrop.Network
{
    /// <summary>
    /// Headless dedicated-server loop (spec sections 20-21, 29). Start the player build with
    /// <c>-updraftServer</c>. Pool mode: claim a composed match from the backend, host it,
    /// report the result, repeat. Configuration comes from the command line / environment:
    ///   -backendUrl URL   (or HOTDROP_BACKEND_URL)
    ///   HOTDROP_SERVER_KEY (environment only; never on the command line or in the build)
    ///   -serverId ID      (defaults to machine name + process id)
    ///   -region CODE      (Photon region; defaults to the claim's region)
    /// Local development without a backend: <c>-devSession NAME -devHumans N --insecure-dev</c>
    /// admits clients presenting "dev:A|B:alias" tickets. Never use that flag in production.
    /// </summary>
    public static class DedicatedServer
    {
        public static bool IsServerProcess { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!HasFlag("-updraftServer")) return;
            IsServerProcess = true;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            AudioListener.volume = 0f;
            Run();
        }

        static async void Run()
        {
            string devSession = Arg("-devSession");
            if (!string.IsNullOrEmpty(devSession))
            {
                bool insecure = HasFlag("--insecure-dev");
                if (!insecure) { Debug.LogError("[Server] -devSession requires --insecure-dev (no backend ticket checks)."); Quit(2); return; }
                int humans = Mathf.Clamp(int.TryParse(Arg("-devHumans"), out var h) ? h : 1, 1, NetSession.MatchSize);
                while (true)
                {
                    var players = new List<ExpectedPlayer>();
                    for (int i = 0; i < humans; i++) players.Add(new ExpectedPlayer { AccountId = $"dev-{i}", Team = i % 2 });
                    Debug.Log($"[Server] dev session '{devSession}' for {humans} human(s)");
                    await HostAsync(devSession, Arg("-region"), devSession, players, null, insecure);
                }
            }

            string baseUrl = Arg("-backendUrl") ?? Environment.GetEnvironmentVariable("HOTDROP_BACKEND_URL");
            string key = Environment.GetEnvironmentVariable("HOTDROP_SERVER_KEY");
            string serverId = Arg("-serverId") ?? $"{Environment.MachineName}-{System.Diagnostics.Process.GetCurrentProcess().Id}";
            string outboxDir = Environment.GetEnvironmentVariable("HOTDROP_OUTBOX_DIR")
                ?? System.IO.Path.Combine(Application.persistentDataPath, "result_outbox");
            var backend = new ServerBackend(baseUrl ?? "", key ?? "", serverId, new Hotdrop.Core.PendingResultStore(outboxDir));
            if (!backend.Configured)
            {
                Debug.LogError("[Server] Set -backendUrl (or HOTDROP_BACKEND_URL) and HOTDROP_SERVER_KEY, or use the dev flags.");
                Quit(2);
                return;
            }
            Debug.Log($"[Server] {serverId} polling {baseUrl} for matches");
            float nextOutboxFlush = 0f;
            while (true)
            {
                // QA-017: undelivered results from earlier matches (or an earlier process) are
                // re-sent between matches until the backend accepts them.
                if (Time.realtimeSinceStartup >= nextOutboxFlush)
                {
                    await backend.FlushOutboxAsync();
                    nextOutboxFlush = Time.realtimeSinceStartup + 30f;
                }
                var claim = await backend.ClaimAsync();
                if (claim == null) { await Task.Delay(2000); continue; }
                var players = new List<ExpectedPlayer>();
                foreach (var p in claim.players)
                    players.Add(new ExpectedPlayer { AccountId = p.playerId, Team = p.team == "B" ? 1 : 0, Alias = p.alias, Cosmetic = p.cosmetic });
                Debug.Log($"[Server] hosting match {claim.matchId} ({players.Count} human(s), bots={claim.hasBots})");
                await HostAsync(claim.session, Arg("-region") ?? claim.region, claim.matchId, players, backend, false);
            }
        }

        static async Task HostAsync(string session, string region, string matchId, List<ExpectedPlayer> players, ServerBackend backend, bool insecure)
        {
            var net = await NetSession.StartServer(session, region, matchId, players, backend, insecure);
            if (net == null || net.Match == null)
            {
                Debug.LogError($"[Server] could not start session {session}: {net?.FailureReason}");
                if (backend != null) await backend.SubmitResultAsync(matchId, FailedResult(players));
                if (net != null) net.Leave();
                await Task.Delay(5000);
                return;
            }

            var match = net.Match;
            while (net != null && net.Runner != null && net.Runner.IsRunning && match.Phase != MatchPhase.Results)
                await Task.Delay(250);

            if (backend != null)
            {
                var result = match.Cancelled ? FailedResult(players) : BuildResult(match);
                bool ok = await backend.SubmitResultAsync(matchId, result);
                Debug.Log($"[Server] match {matchId} result submitted: {ok}");
            }
            foreach (var e in match.ServerPadEvents)
                Debug.Log($"[ServerEvent] {e.name} slot={e.slot} team={e.team} padTeam={e.padTeam} t={e.matchTime:0.0}");

            // Let clients read the results screen before the session closes.
            await Task.Delay(12000);
            if (net != null) net.Leave();
            await Task.Delay(1000);
        }

        static ResultDto BuildResult(NetMatch match)
        {
            var outcome = match.Outcome;
            bool draw = outcome == MatchOutcome.Draw;
            var result = new ResultDto { Draw = draw, HasBots = match.HasBots, Participants = new List<ResultParticipantDto>() };
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != match.Runner || string.IsNullOrEmpty(p.AccountId.ToString())) continue;
                bool won = !draw && ((outcome == MatchOutcome.TeamAWins && p.Team == 0) || (outcome == MatchOutcome.TeamBWins && p.Team == 1));
                result.Participants.Add(new ResultParticipantDto
                {
                    PlayerId = p.AccountId.ToString(),
                    Won = won,
                    Eligible = match.IsEligible(p),
                    HadBotSubstitution = p.HadBotSubstitution,
                    // C04: the server's own bounded facts (eligible players only; the backend
                    // re-checks eligibility before advancing any progress).
                    ProgressFacts = match.IsEligible(p) ? BuildProgressFacts(match, p) : null,
                    Team = p.Team == 0 ? "A" : "B",
                    Slot = p.Slot, // QA-025: lets players report someone by what they saw in-match
                });
            }
            return result;
        }

        /// <summary>
        /// C04: bounded per-player facts a client can never fabricate. Occupation time counts
        /// only for the sole occupying team; a human-teammate pad use is attributed to the
        /// pad's owner only when a different, real human teammate used it; the weapon is
        /// whichever the player actually held longest.
        /// </summary>
        static ProgressFactsDto BuildProgressFacts(NetMatch match, NetPlayer owner)
        {
            var uses = new List<PadTeammateUseDto>();
            if (!string.IsNullOrEmpty(owner.AccountId))
            {
                var seen = new HashSet<string>();
                foreach (var e in match.ServerPadEvents)
                {
                    if (e.name != "pad_used" || e.ownerSlot != owner.Slot || e.slot == owner.Slot || e.team != owner.Team) continue;
                    var user = NetPlayer.All.Find(x => x.Runner == match.Runner && x.Slot == e.slot);
                    if (user == null || user.BotControlled || string.IsNullOrEmpty(user.AccountId)) continue;
                    if (seen.Add(user.AccountId))
                        uses.Add(new PadTeammateUseDto { PadOwnerPlayerId = owner.AccountId, TeammatePlayerId = user.AccountId, TeammateIsHuman = true });
                }
            }
            return new ProgressFactsDto
            {
                ValidUncontestedOccupationSeconds = Mathf.RoundToInt(owner.UncontestedZoneSeconds),
                MajorityEquippedWeapon = owner.MajorityWeaponId(),
                HumanTeammatePadUses = uses.ToArray(),
            };
        }

        /// <summary>Spec 4/19: a cancelled or crashed match grants nothing and never changes rating.</summary>
        static ResultDto FailedResult(List<ExpectedPlayer> players)
        {
            var result = new ResultDto { ServerFailed = true, Participants = new List<ResultParticipantDto>() };
            foreach (var p in players)
                result.Participants.Add(new ResultParticipantDto { PlayerId = p.AccountId, Eligible = false, Team = p.Team == 0 ? "A" : "B" });
            return result;
        }

        static bool HasFlag(string flag) => Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static void Quit(int code)
        {
#if UNITY_EDITOR
            Debug.LogError($"[Server] exit {code}");
#else
            Application.Quit(code);
#endif
        }
    }
}



