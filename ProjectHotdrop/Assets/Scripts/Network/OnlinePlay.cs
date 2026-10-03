using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.Network
{
    /// <summary>
    /// Online journey (spec section 2 / 17): Home → matchmaking → server reservation → load →
    /// match → results → Home. Every wait shows what is happening, every failure says why,
    /// and Cancel always works. No shop, ads or rewards here.
    /// </summary>
    public class OnlinePlay : MonoBehaviour
    {
        const float ServerWaitSeconds = 60f;
        const float ReconnectPollSeconds = 2f;
        const int LongWaitSeconds = 45; // QA-029: the bot-fill boundary; offer practice from here

        GameBootstrap game;
        GameObject panel;
        TextMeshProUGUI title, status;
        Button actionButton;
        TextMeshProUGUI actionLabel;
        bool cancelled;
        bool searching;
        // QA-015: true from the moment an enqueue is sent (or an existing ticket is followed)
        // until the match is joined, so Cancel/failure always withdraw the ticket — even while
        // the enqueue request is still in flight.
        bool queueRequested;
        bool recovering;
        NetSession session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            OnlineHooks.PlayOnline = game =>
            {
                var play = game.GetComponent<OnlinePlay>() ?? game.gameObject.AddComponent<OnlinePlay>();
                play.Begin(game);
            };
            OnlineHooks.UnavailableReason = () =>
            {
                var config = BackendConfig.Load();
                if (config != null && (config.IsConfigured || config.UsesDevDirectSession)) return null;
                return "OFFLINE PLAYTEST  •  Online matches need a configured server (not in this build)";
            };
        }

        public void Begin(GameBootstrap bootstrap)
        {
            game = bootstrap;
            cancelled = false;
            queueRequested = false;
            BuildPanel();
            panel.SetActive(true);
            var config = BackendConfig.Load();
            if (config.UsesDevDirectSession) _ = JoinDevAsync(config);
            else _ = MatchmakeAsync();
        }

        // ---------- Flows ----------

        async Task JoinDevAsync(BackendConfig config)
        {
            Show("CONNECTING", $"Joining development server \"{config.devDirectSession}\"…", "CANCEL", Cancel);
            string alias = PlayerPrefs.GetString("LocalAlias", "Player");
            await Join(config.devDirectSession, config.devDirectRegion, $"dev:A:{alias}", null);
        }

        async Task MatchmakeAsync()
        {
            var api = BackendClient.Instance;
            Show("CONNECTING", "Signing in…", "CANCEL", Cancel);
            var auth = await api.EnsureSessionAsync();
            if (cancelled) return;
            if (!auth.Ok) { Failed(auth.Message); return; }

            // QA-014: party members never enqueue themselves — the leader's enqueue creates a
            // ticket for every member. Follow an existing ticket if there is one.
            var existing = await api.QueueStatusAsync();
            if (cancelled) return;
            bool following = existing.Ok && existing.Value != null && existing.Value.state != "idle";
            queueRequested = true;
            if (!following)
            {
                var enqueue = await api.EnqueueAsync();
                if (cancelled)
                {
                    // Cancel was tapped while the enqueue was in flight: its cancel request may have
                    // reached the server before this ticket existed, so withdraw it again now.
                    if (enqueue.Ok || enqueue.Error == BackendError.Conflict) _ = api.CancelQueueAsync();
                    return;
                }
                if (enqueue.Status == 403) // party member: only the leader may queue the party
                {
                    if (!await WaitForPartyLeaderAsync()) return;
                }
                else if (!enqueue.Ok && enqueue.Error != BackendError.Conflict) // 409 = already queued: keep waiting
                {
                    Failed(enqueue.Value?.error == "matchmaking_disabled" ? "Matchmaking is paused right now. Practice offline and try again soon."
                        : enqueue.Value?.error == "party_not_ready" ? "Everyone in your party must be Ready before the leader can queue."
                        : enqueue.Message);
                    return;
                }
            }

            searching = true;
            Telemetry.Track("matchmaking_started");
            while (!cancelled)
            {
                var queue = await api.QueueStatusAsync();
                if (cancelled) return;
                if (!queue.Ok) { Failed(queue.Message); return; }
                if (queue.Value.state == "matched") break;
                if (queue.Value.state == "idle") { Failed("You left the queue."); return; }
                int s = queue.Value.elapsedSeconds;
                // QA-029: elapsed time, the region and — once the wait is long — a direct way to practice.
                string region = string.IsNullOrEmpty(queue.Value.region) ? "" : $"  ·  Region {queue.Value.region.ToUpperInvariant()}";
                string body = $"Searching {s / 60}:{s % 60:00}{region}\nFair matches first — empty seats fill with labeled bots after a short wait.";
                if (s >= LongWaitSeconds) Show("FINDING A 3v3 MATCH", body, "CANCEL", Cancel, "PRACTICE INSTEAD", PracticeInstead);
                else Show("FINDING A 3v3 MATCH", body, "CANCEL", Cancel);
                await Task.Delay(2000);
            }
            if (cancelled) return;
            searching = false;

            float deadline = Time.realtimeSinceStartup + ServerWaitSeconds;
            TicketDto ticket = null;
            while (!cancelled && Time.realtimeSinceStartup < deadline)
            {
                int waited = Mathf.RoundToInt(ServerWaitSeconds - (deadline - Time.realtimeSinceStartup));
                string reserving = $"Reserving a match server… {waited}s (up to {ServerWaitSeconds:0}s)";
                if (waited >= 20) Show("MATCH FOUND", reserving, "CANCEL", Cancel, "PRACTICE INSTEAD", PracticeInstead);
                else Show("MATCH FOUND", reserving, "CANCEL", Cancel);
                var t = await api.TicketAsync();
                if (!t.Ok) { Failed(t.Message); return; }
                if (t.Value.serverReady && !string.IsNullOrEmpty(t.Value.ticket)) { ticket = t.Value; break; }
                await Task.Delay(1500);
            }
            if (cancelled) return;
            if (ticket == null) { Failed("No match server became available. Nothing was lost — please queue again."); return; }

            queueRequested = false; // reserved: the queue ticket is consumed
            // QA-027: online events carry the real match, region and bot context.
            Telemetry.CurrentMatchId = ticket.matchId ?? "";
            Telemetry.Region = string.IsNullOrEmpty(ticket.region) ? "unknown" : ticket.region;
            Telemetry.CurrentMatchHasBots = ticket.hasBots;
            Telemetry.Track("match_joining", ("match_id", ticket.matchId), ("has_bots", ticket.hasBots ? "1" : "0"));
            Show("LOADING MATCH", ticket.hasBots ? "Joining… (some seats are labeled bots)" : "Joining…", "CANCEL", Cancel);
            await Join(ticket.session, ticket.region, ticket.ticket, ticket.matchId);
        }

        /// <summary>
        /// QA-014: a party member who presses Play waits here until the leader queues the party,
        /// then continues into the shared search automatically. Returns false if cancelled.
        /// </summary>
        async Task<bool> WaitForPartyLeaderAsync()
        {
            var api = BackendClient.Instance;
            while (!cancelled)
            {
                Show("WAITING FOR PARTY LEADER", "Your party leader starts the search for everyone.\nYou'll join the same match automatically.", "CANCEL", Cancel);
                await Task.Delay(2000);
                if (cancelled) return false;
                var status = await api.QueueStatusAsync();
                if (cancelled) return false;
                if (status.Ok && status.Value != null && status.Value.state != "idle") return true;
                if (!status.Ok && status.Error != BackendError.NoInternet && status.Error != BackendError.ServerBusy)
                {
                    Failed(status.Message);
                    return false;
                }
            }
            return false;
        }

        async Task Join(string sessionName, string region, string ticket, string matchId)
        {
            session = await NetSession.StartClient(sessionName, region, ticket);
            if (cancelled) { session?.Leave(); return; }
            if (session == null) { Failed("Could not create a match connection."); return; }
            if (session.FailureReason != null)
            {
                if (string.IsNullOrEmpty(matchId)) Failed(session.FailureReason);
                else await Recover(matchId, session, session.FailureReason);
                return;
            }

            BindPresenter(session, matchId);
        }

        void BindPresenter(NetSession net, string matchId)
        {
            var presenter = net.gameObject.AddComponent<NetClientPresenter>();
            presenter.Bind(game, net, ReturnHome);
            presenter.MatchId = matchId; // QA-017: results screen fetches this match's reward receipt
            presenter.OnRemovedForInactivity += () =>
            {
                // Spec 18: removal, not a silent lockout. Leaving cleanly (ShutdownReason.Ok) does
                // not trigger reconnect, and the server refuses this seat for the rest of the match.
                Telemetry.Track("afk_removed", ("match_id", matchId ?? ""));
                game.touchInput.enabled = false;
                game.matchHUD.HideAllPanels();
                net.Leave();
                session = null;
                Show("REMOVED FOR INACTIVITY",
                    "You were idle for 45 seconds, so a labeled bot took your seat to keep the match fair for your team.\nThis match won't count toward XP or rating.",
                    "BACK TO HOME", ReturnHome, "PRACTICE OFFLINE", () => { ReturnHome(); game.touchInput.enabled = true; game.StartOfflinePractice(); });
            };
            net.OnFailed += reason =>
            {
                if (presenter.ShowingResults) return;
                if (string.IsNullOrEmpty(matchId)) Failed(reason);
                else _ = Recover(matchId, net, reason);
            };
            panel.SetActive(false);
            game.touchInput.enabled = true;
        }

        async Task Recover(string matchId, NetSession disconnected, string initialReason)
        {
            if (recovering || cancelled) return;
            recovering = true;
            session = disconnected;
            game.touchInput.enabled = false;
            Telemetry.Track("match_disconnect", ("match_id", matchId));
            var api = BackendClient.Instance;

            while (!cancelled)
            {
                Show("RECONNECTING TO MATCH", $"{initialReason}\nYour avatar and match state are being preserved. Checking the match…", "CANCEL", Cancel);
                var auth = await api.EnsureSessionAsync();
                if (cancelled) break;
                if (!auth.Ok)
                {
                    Show("RECONNECTING TO MATCH", $"{auth.Message}\nYour avatar remains reserved. Retrying…", "CANCEL", Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    continue;
                }

                var allowed = await api.ReconnectAsync(matchId);
                if (cancelled) break;
                if (!allowed.Ok || allowed.Value == null)
                {
                    if (allowed.Error is BackendError.Invalid or BackendError.NotFound or BackendError.NotConfigured)
                    {
                        recovering = false;
                        Failed("This match is no longer available. No result or reward was created for a failed server.");
                        return;
                    }
                    Show("RECONNECTING TO MATCH", $"{allowed.Message}\nRetrying the secure reconnect check…", "CANCEL", Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    continue;
                }
                if (!allowed.Value.reconnectAllowed)
                {
                    recovering = false;
                    Failed("The match has ended. Return Home to queue again.");
                    return;
                }

                var ticket = await api.TicketAsync();
                if (cancelled) break;
                if (!ticket.Ok || ticket.Value == null || !ticket.Value.serverReady || string.IsNullOrEmpty(ticket.Value.ticket))
                {
                    string message = ticket.Ok ? "The match server is restarting. Retrying…" : ticket.Message;
                    Show("RECONNECTING TO MATCH", message, "CANCEL", Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    continue;
                }

                if (disconnected != null)
                {
                    disconnected.Leave();
                    for (int i = 0; i < 50 && disconnected != null && disconnected.Runner != null && disconnected.Runner.IsRunning; i++)
                        await Task.Delay(100);
                    await Task.Delay(250); // allow Unity to finish the old runner's shutdown callback
                }
                if (cancelled) break;

                var next = await NetSession.StartClient(ticket.Value.session, ticket.Value.region, ticket.Value.ticket);
                if (cancelled) { next?.Leave(); break; }
                if (next == null || next.FailureReason != null)
                {
                    string reason = next?.FailureReason ?? "Could not create a match connection.";
                    Show("RECONNECTING TO MATCH", $"{reason}\nRetrying with a fresh join ticket…", "CANCEL", Cancel);
                    disconnected = next;
                    session = next;
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    continue;
                }

                session = next;
                recovering = false;
                Telemetry.Track("match_reconnected", ("match_id", matchId));
                BindPresenter(next, matchId);
                return;
            }

            recovering = false;
        }

        // ---------- UI ----------

        void Cancel()
        {
            cancelled = true;
            if (searching || queueRequested) _ = BackendClient.Instance.CancelQueueAsync();
            searching = false;
            queueRequested = false;
            if (session != null) session.Leave();
            ReturnHome();
        }

        /// <summary>QA-029: leave the queue (withdrawing the ticket) and start offline practice in one tap.</summary>
        bool leavingForPractice;

        async void PracticeInstead()
        {
            if (leavingForPractice) return; // the search loop may redraw this button while the release is in flight
            leavingForPractice = true;
            try { await ReleaseQueueThenPractice(); }
            finally { leavingForPractice = false; }
        }

        async Task ReleaseQueueThenPractice()
        {
            if (queueRequested)
            {
                Show("LEAVING MATCH SEARCH", "Releasing your queue reservation before practice starts…", null, null);
                var release = await BackendClient.Instance.CancelQueueAsync();
                if (!release.Ok)
                {
                    if (release.Error == BackendError.Conflict)
                        Show("MATCH SERVER STARTING", "Your reservation is already starting. Continue joining the match, or cancel and return Home.", "CANCEL", Cancel);
                    else
                        Show("COULDN'T LEAVE QUEUE", $"{release.Message}\nPractice hasn't started. Try again or cancel.", "CANCEL", Cancel, "TRY AGAIN", PracticeInstead);
                    return;
                }
            }
            cancelled = true;
            searching = false;
            queueRequested = false;
            session?.Leave();
            ReturnHome();
            game.touchInput.enabled = true;
            game.StartOfflinePractice();
        }

        void Failed(string message)
        {
            // QA-015: a failure while queued must not leave a server ticket behind (the server
            // only cancels tickets still searching, so this is harmless after a match is found).
            if (searching || queueRequested) _ = BackendClient.Instance.CancelQueueAsync();
            queueRequested = false;
            searching = false;
            if (session != null) { session.Leave(); session = null; }
            game.touchInput.enabled = false;
            Show("CAN'T PLAY ONLINE", message, "BACK TO HOME", ReturnHome, "PRACTICE OFFLINE", () => { ReturnHome(); game.touchInput.enabled = true; game.StartOfflinePractice(); });
            Telemetry.Track("online_failed", ("reason", message.Length > 120 ? message.Substring(0, 120) : message));
        }

        void ReturnHome()
        {
            if (panel != null) panel.SetActive(false);
            session = null;
            Telemetry.CurrentMatchId = ""; Telemetry.Region = "offline"; Telemetry.CurrentMatchHasBots = false;
            TelemetryUploader.Kick(); // QA-027: deliver this match's events
            game.ReturnToHome();
        }

        void Show(string heading, string body, string action, UnityEngine.Events.UnityAction onAction,
            string secondary = null, UnityEngine.Events.UnityAction onSecondary = null)
        {
            BuildPanel();
            panel.SetActive(true);
            title.text = heading;
            status.text = body;
            actionButton.gameObject.SetActive(action != null); // no action = a transient status, not a dead button
            actionLabel.text = action ?? "";
            actionButton.onClick.RemoveAllListeners();
            if (onAction != null) actionButton.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); onAction(); });
            // QA-029: an optional second way out (e.g. practice instead of waiting longer).
            secondaryButton.gameObject.SetActive(secondary != null);
            secondaryLabel.text = secondary ?? "";
            secondaryButton.onClick.RemoveAllListeners();
            if (onSecondary != null) secondaryButton.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); onSecondary(); });
        }

        Button secondaryButton;
        TextMeshProUGUI secondaryLabel;

        void BuildPanel()
        {
            if (panel != null) return;
            // QA-019/020: density-aware layout (48 dp actions, ~12 dp text) like every other menu.
            var screen = Hotdrop.UI.MenuLayout.Create(game.matchHUD.transform, "OnlinePanel", "", null);
            panel = screen.Root;
            title = screen.Title;
            status = Hotdrop.UI.MenuLayout.AddText(screen.Content, "", 28, Hotdrop.UI.MatchHUD.SecondaryText, "Status");
            var actions = Hotdrop.UI.MenuLayout.Row(screen.Content, "Actions");
            actionButton = Hotdrop.UI.MenuLayout.AddButton(actions, "", null, "Action", 96);
            actionLabel = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            secondaryButton = Hotdrop.UI.MenuLayout.AddButton(actions, "", null, "SecondaryAction", 96);
            secondaryLabel = secondaryButton.GetComponentInChildren<TextMeshProUGUI>();
            secondaryButton.gameObject.SetActive(false);
        }
    }
}



