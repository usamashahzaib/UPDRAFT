using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.Config;
using Hotdrop.Core;
using Hotdrop.UI;

namespace Hotdrop.Network
{
    /// <summary>
    /// Online journey (spec section 2 / 17): Home → matchmaking → server reservation → load →
    /// match → results → Home. Every wait shows what is happening, every failure says why,
    /// and Cancel always works. No shop, ads or rewards here.
    /// </summary>
    public class OnlinePlay : MonoBehaviour
    {
        static string L(string key, string fallback) => UiDraw.L("online." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("online." + key, fallback, args);
        const float ServerWaitSeconds = 60f;
        const float ReconnectPollSeconds = 2f;

        GameBootstrap game;
        GameObject panel;
        TextMeshProUGUI title, status;
        Button actionButton;
        TextMeshProUGUI actionLabel;
        bool cancelled;
        int flowGeneration;
        bool searching;
        // QA-015: true from the moment an enqueue is sent (or an existing ticket is followed)
        // until the match is joined, so Cancel/failure always withdraw the ticket — even while
        // the enqueue request is still in flight.
        bool queueRequested;
        bool queueTelemetryActive;
        bool queueMatched;
        float queueTelemetryStartedAt;
        int queueElapsedSeconds;
        int queueInitialElapsedSeconds;
        int partyLeaderQueueElapsedSeconds;
        string partyLeaderQueueRegion;
        bool partyLeaderQueueMatched;
        int recoveryGeneration = -1;
        bool queuedPracticeActive;
        GameObject queuedPracticeBanner;
        RectTransform queuedPracticeBannerRect;
        RectTransform queuedPracticeCopyRect;
        RectTransform queuedPracticeReturnRect;
        RectTransform queuedPracticeCancelRect;
        TextMeshProUGUI queuedPracticeCopyText;
        TextMeshProUGUI queuedPracticeReturnLabel;
        TextMeshProUGUI queuedPracticeCancelLabel;
        float queuedPracticeBannerDpPerUnit = -1f;
        float queuedPracticeBannerTextScale = -1f;
        NetSession session;
        PartyDto searchParty;

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
                return L("offlinePlaytest", "OFFLINE PLAYTEST  •  Online matches need a configured server (not in this build)");
            };
        }

        void LateUpdate()
        {
            if (queuedPracticeBanner == null) return;
            float dpPerUnit = UiMetrics.DpPerUnit;
            if (!Mathf.Approximately(dpPerUnit, queuedPracticeBannerDpPerUnit) ||
                !Mathf.Approximately(UiMetrics.TextScale, queuedPracticeBannerTextScale))
                FitQueuedPracticeBanner();
        }

        public void Begin(GameBootstrap bootstrap)
        {
            int generation = ++flowGeneration;
            cancelled = true;
            StopQueuedPractice();
            session?.Leave();
            session = null;
            recoveryGeneration = -1;
            game = bootstrap;
            cancelled = false;
            queueRequested = false;
            queueTelemetryActive = false;
            queueMatched = false;
            queuedPracticeActive = false;
            DestroyQueuedPracticeBanner();
            queueElapsedSeconds = 0;
            queueInitialElapsedSeconds = 0;
            partyLeaderQueueElapsedSeconds = 0;
            partyLeaderQueueRegion = null;
            partyLeaderQueueMatched = false;
            searchParty = null;
            Telemetry.CurrentMatchId = "";
            Telemetry.Region = "unknown";
            Telemetry.CurrentMatchHasBots = false;
            Telemetry.CurrentMatchIsBotSubstituted = false;
            Telemetry.CurrentMatchBotCount = 0;
            BuildPanel();
            panel.SetActive(true);
            Hotdrop.UI.UiScreenEnterMotion.Reopen(panel);
            var config = BackendConfig.Load();
            if (config.UsesDevDirectSession) _ = JoinDevAsync(config, generation);
            else _ = MatchmakeAsync(generation);
        }

        // ---------- Flows ----------

        bool IsCurrentFlow(int generation) => generation == flowGeneration && !cancelled;

        async Task JoinDevAsync(BackendConfig config, int generation)
        {
            Show(L("connecting", "Connecting"), F("joiningDevServerTemplate", "Joining development server \"{0}\"…", config.devDirectSession), L("cancel", "Cancel"), Cancel);
            string alias = PlayerPrefs.GetString("LocalAlias", "Player");
            await Join(config.devDirectSession, config.devDirectRegion, $"dev:A:{alias}", null, generation);
        }

        async Task MatchmakeAsync(int generation)
        {
            var api = BackendClient.Instance;
            Show(L("connecting", "Connecting"), L("signingIn", "Signing in…"), L("cancel", "Cancel"), Cancel);
            var auth = await api.EnsureSessionAsync();
            if (!IsCurrentFlow(generation)) return;
            if (!auth.Ok) { Failed(auth.Message, generation); return; }
            // Show the real party roster during search. Solo search remains a single named seat.
            var currentParty = await api.CurrentPartyAsync();
            if (!IsCurrentFlow(generation)) return;
            if (!currentParty.Ok && currentParty.Error != BackendError.NotFound)
            {
                Failed(F("partyStatusFailedTemplate", "Party status could not be checked. {0}", currentParty.Message), generation);
                return;
            }
            searchParty = currentParty.Ok ? currentParty.Value : null;

            // QA-014: party members never enqueue themselves — the leader's enqueue creates a
            // ticket for every member. Follow an existing ticket if there is one.
            var existing = await api.QueueStatusAsync();
            if (!IsCurrentFlow(generation)) return;
            if (existing.Ok && existing.Value != null &&
                (existing.Value.state == "maintenance" || existing.Value.state == "queue_paused"))
            {
                Failed(MaintenanceMessage(existing.Value.message, existing.Value.etaUtc, existing.Value.compensationNote), generation);
                return;
            }
            bool following = existing.Ok && existing.Value != null && existing.Value.state != "idle";
            int followedElapsedSeconds = following ? Mathf.Max(0, existing.Value.elapsedSeconds) : 0;
            bool followingMatched = following && existing.Value.state == "matched";
            queueRequested = true;
            if (!following)
            {
                var enqueue = await api.EnqueueAsync();
                if (!IsCurrentFlow(generation))
                {
                    // Cancel was tapped while the enqueue was in flight: its cancel request may have
                    // reached the server before this ticket existed, so withdraw it again now.
                    if (enqueue.Ok || enqueue.Error == BackendError.Conflict)
                    {
                        _ = api.CancelQueueAsync();
                        StartQueueTelemetry(following || enqueue.Error == BackendError.Conflict, 0);
                        EndQueueTelemetry("cancel_during_enqueue");
                    }
                    return;
                }
                if (enqueue.Status == 403) // party member: only the leader may queue the party
                {
                    if (!await WaitForPartyLeaderAsync(generation)) return;
                    following = true;
                    followedElapsedSeconds = partyLeaderQueueElapsedSeconds;
                    followingMatched = partyLeaderQueueMatched;
                    if (!string.IsNullOrEmpty(partyLeaderQueueRegion))
                        Telemetry.Region = partyLeaderQueueRegion;
                }
                else if (!enqueue.Ok && enqueue.Error != BackendError.Conflict) // 409 = already queued: keep waiting
                {
                    Failed(enqueue.Value?.error == "maintenance" ? MaintenanceMessage(enqueue.Value.message, enqueue.Value.etaUtc, enqueue.Value.compensationNote)
                        : enqueue.Value?.error == "matchmaking_disabled" ? L("matchmakingPaused", "Matchmaking is paused right now. Practice offline and try again soon.")
                        : enqueue.Value?.error == "party_not_ready" ? L("partyNotReady", "Everyone in your party must be Ready before the leader can queue.")
                        : enqueue.Message, generation);
                    return;
                }
            }

            searching = true;
            Telemetry.Track("matchmaking_started");
            if (following && !string.IsNullOrEmpty(partyLeaderQueueRegion))
                Telemetry.Region = partyLeaderQueueRegion;
            else if (following && existing.Value != null &&
                     (existing.Value.state == "searching" || existing.Value.state == "matched") &&
                     !string.IsNullOrEmpty(existing.Value.region))
                Telemetry.Region = existing.Value.region;
            StartQueueTelemetry(following, followedElapsedSeconds);
            queueMatched = followingMatched;
            int queueRetryDelay = 2;
            while (IsCurrentFlow(generation))
            {
                var queue = await api.QueueStatusAsync();
                if (!IsCurrentFlow(generation)) return;
                if (!queue.Ok)
                {
                    if (IsTransientConnectionError(queue.Error))
                    {
                        int delay = queueRetryDelay;
                        if (queuedPracticeActive) UpdateQueuedPracticeBanner(F("retryTemplate", "{0}\nRetrying in {1}s. Cancel or switch to offline practice at any time.", queue.Message, delay));
                        else if (!await WaitBeforeRetryAsync(L("queueStatus", "Queue Status"), queue.Message, delay, generation)) return;
                        queueRetryDelay = Mathf.Min(queueRetryDelay * 2, 30);
                        if (queuedPracticeActive) await Task.Delay(delay * 1000);
                        continue;
                    }
                    Failed(queue.Message, generation);
                    return;
                }
                queueRetryDelay = 2;
                if (queue.Value.state == "maintenance" || queue.Value.state == "queue_paused")
                {
                    Failed(MaintenanceMessage(queue.Value.message, queue.Value.etaUtc, queue.Value.compensationNote), generation);
                    return;
                }
                if (!string.IsNullOrEmpty(queue.Value.region)) Telemetry.Region = queue.Value.region;
                if (queue.Value.state == "matched")
                {
                    queueMatched = true;
                    break;
                }
                if (queue.Value.state == "idle") { Failed(L("leftQueue", "You left the queue."), generation); return; }
                if (queue.Value.state != "searching")
                {
                    Failed(L("unknownQueueStatus", "The queue returned an unknown status. Return Home and try again."), generation);
                    return;
                }
                int s = queue.Value.elapsedSeconds;
                queueElapsedSeconds = Mathf.Max(queueElapsedSeconds, s);
                if (queuedPracticeActive)
                {
                    UpdateQueuedPracticeBanner(F("searchingTemplate", "Searching {0}{1}\nFair matches first — empty seats fill automatically after a short wait.", $"{s / 60}:{s % 60:00}", string.IsNullOrEmpty(queue.Value.region) ? "" : F("regionSuffixTemplate", "  ·  Region {0}", queue.Value.region.ToUpperInvariant())));
                    await Task.Delay(2000);
                    continue;
                }
                // Keep offline practice reachable throughout queue search.
                string region = string.IsNullOrEmpty(queue.Value.region) ? "" : F("regionSuffixTemplate", "  ·  Region {0}", queue.Value.region.ToUpperInvariant());
                string body = F("searchingTemplate", "Searching {0}{1}\nFair matches first — empty seats fill automatically after a short wait.", $"{s / 60}:{s % 60:00}", region);
                Show(L("findingMatch", "FINDING A 3v3 MATCH"), body, L("cancel", "Cancel"), Cancel, L("practiceInstead", "Practice Instead"), PracticeInstead,
                    preserveMatchSearch: true);
                MatchSearchView.Show(panel, s, queue.Value.region, searchParty, api.PlayerId,
                    practiceAvailable: true,
                    cancel: () => { if (IsCurrentFlow(generation)) Cancel(); },
                    practice: () => { if (IsCurrentFlow(generation)) PracticeInstead(); });
                await Task.Delay(2000);
            }
            if (!IsCurrentFlow(generation)) return;
            searching = false;

            float deadline = Time.realtimeSinceStartup + ServerWaitSeconds;
            TicketDto ticket = null;
            while (IsCurrentFlow(generation) && Time.realtimeSinceStartup < deadline)
            {
                int waited = Mathf.RoundToInt(ServerWaitSeconds - (deadline - Time.realtimeSinceStartup));
                string reserving = F("reservingServerTemplate", "Reserving a match server… {0}s (up to {1:0}s)", waited, ServerWaitSeconds);
                if (queuedPracticeActive) UpdateQueuedPracticeBanner(reserving);
                else if (waited >= 20) Show(L("matchFound", "Match Found"), reserving, L("cancel", "Cancel"), Cancel, L("practiceInstead", "Practice Instead"), PracticeInstead);
                else Show(L("matchFound", "Match Found"), reserving, L("cancel", "Cancel"), Cancel);
                var t = await api.TicketAsync();
                if (!IsCurrentFlow(generation)) return;
                if (!t.Ok) { Failed(t.Message, generation); return; }
                if (t.Value.serverReady && !string.IsNullOrEmpty(t.Value.ticket)) { ticket = t.Value; break; }
                await Task.Delay(1500);
            }
            if (!IsCurrentFlow(generation)) return;
            if (ticket == null) { Failed(L("noMatchServer", "No match server became available. Nothing was lost — please queue again."), generation); return; }

            // A server match takes priority over the local drill. Stop it before ticket
            // handoff/join so offline state cannot overlap an online match.
            StopQueuedPractice();

            queueRequested = false; // reserved: the queue ticket is consumed
            queueTelemetryActive = false; // match_joining is the successful queue-funnel event
            // QA-027: online events carry the real match, region and bot context.
            Telemetry.CurrentMatchId = ticket.matchId ?? "";
            Telemetry.Region = string.IsNullOrEmpty(ticket.region) ? "unknown" : ticket.region;
            Telemetry.CurrentMatchHasBots = ticket.hasBots;
            Telemetry.Track("match_joining", ("match_id", ticket.matchId), ("has_bots", ticket.hasBots ? "1" : "0"));
            string loadingDetails = UiDraw.L("matchSearch.footer.venue", "SWITCHYARD · RELAY") + "\n" +
                (ticket.hasBots ? L("joiningBalanced", "Joining… roster is being balanced.") : L("joining", "Joining…"));
            Show(L("loadingMatch", "Loading Match"), loadingDetails, L("cancel", "Cancel"), Cancel);
            await Join(ticket.session, ticket.region, ticket.ticket, ticket.matchId, generation);
        }

        /// <summary>
        /// QA-014: a party member who presses Play waits here until the leader queues the party,
        /// then continues into the shared search automatically. Returns false if cancelled.
        /// </summary>
        async Task<bool> WaitForPartyLeaderAsync(int generation)
        {
            var api = BackendClient.Instance;
            int retryDelay = 2;
            while (IsCurrentFlow(generation))
            {
                Show(L("waitingForPartyLeader", "Waiting for Party Leader"), F("partyLeaderWaitTemplate", "Your party leader starts the search for everyone.\nYou'll join automatically. Checking again in {0}s.", retryDelay), L("cancel", "Cancel"), Cancel);
                await Task.Delay(retryDelay * 1000);
                if (!IsCurrentFlow(generation)) return false;
                var status = await api.QueueStatusAsync();
                if (!IsCurrentFlow(generation)) return false;
                if (status.Ok && status.Value != null && status.Value.state != "idle")
                {
                    partyLeaderQueueElapsedSeconds = Mathf.Max(0, status.Value.elapsedSeconds);
                    partyLeaderQueueRegion = status.Value.region;
                    partyLeaderQueueMatched = status.Value.state == "matched";
                    return true;
                }
                if (!status.Ok && IsTransientConnectionError(status.Error))
                {
                    int delay = retryDelay;
                    if (!await WaitBeforeRetryAsync(L("partyQueue", "Party Queue"), status.Message, delay, generation)) return false;
                    retryDelay = Mathf.Min(retryDelay * 2, 30);
                    continue;
                }
                if (!status.Ok)
                {
                    Failed(status.Message, generation);
                    return false;
                }
                retryDelay = 2;
            }
            return false;
        }

        static bool IsTransientConnectionError(BackendError error) =>
            error == BackendError.NoInternet || error == BackendError.ServerBusy || error == BackendError.ServerError;

        async Task<bool> WaitBeforeRetryAsync(string operation, string detail, int seconds, int generation)
        {
            string reason = string.IsNullOrWhiteSpace(detail) ? L("serverNoResponse", "The server did not respond.") : detail.Trim();
            Show(F("reconnectingOperationTemplate", "RECONNECTING — {0}", operation), F("retryTemplate", "{0}\nRetrying in {1}s. Cancel or switch to offline practice at any time.", reason, seconds),
                L("cancel", "Cancel"), Cancel, L("practiceInstead", "Practice Instead"), PracticeInstead);
            await Task.Delay(seconds * 1000);
            return IsCurrentFlow(generation);
        }

        async Task Join(string sessionName, string region, string ticket, string matchId, int generation)
        {
            var started = await NetSession.StartClient(sessionName, region, ticket);
            if (!IsCurrentFlow(generation)) { started?.Leave(); return; }
            session = started;
            if (session == null) { Failed(L("connectionCreateFailed", "Could not create a match connection."), generation); return; }
            if (session.FailureReason != null)
            {
                if (string.IsNullOrEmpty(matchId)) Failed(session.FailureReason, generation);
                else await Recover(matchId, session, session.FailureReason, generation);
                return;
            }

            BindPresenter(session, matchId, generation);
        }

        void BindPresenter(NetSession net, string matchId, int generation)
        {
            var presenter = net.gameObject.AddComponent<NetClientPresenter>();
            presenter.Bind(game, net, () =>
            {
                if (IsCurrentFlow(generation)) ReturnHome();
            });
            presenter.MatchId = matchId; // QA-017: results screen fetches this match's reward receipt
            presenter.OnRemovedForInactivity += () =>
            {
                if (!IsCurrentFlow(generation)) return;
                // Spec 18: removal, not a silent lockout. Leaving cleanly (ShutdownReason.Ok) does
                // not trigger reconnect, and the server refuses this seat for the rest of the match.
                Telemetry.Track("afk_removed", ("match_id", matchId ?? ""));
                game.touchInput.enabled = false;
                game.matchHUD.HideAllPanels();
                net.Leave();
                session = null;
                Show(L("removedForInactivity", "Removed for Inactivity"),
                    L("inactivityMessage", "You were idle for 45 seconds, so a squadmate took over your seat to keep the match fair.\nThis match won't count toward XP or rating."),
                    L("backHome", "Back to Home"), ReturnHome, L("practiceOffline", "Practice Offline"), () => { ReturnHome(); game.touchInput.enabled = true; game.StartOfflinePractice(); });
            };
            net.OnFailed += reason =>
            {
                if (!IsCurrentFlow(generation)) return;
                if (presenter.ShowingResults) return;
                if (string.IsNullOrEmpty(matchId)) Failed(reason, generation);
                else _ = Recover(matchId, net, reason, generation);
            };
            var joinedPanel = panel;
            Hotdrop.UI.UiScreenEnterMotion.Exit(joinedPanel, () =>
            {
                if (joinedPanel != null) joinedPanel.SetActive(false);
            });
            game.touchInput.enabled = true;
        }

        async Task Recover(string matchId, NetSession disconnected, string initialReason, int generation)
        {
            if (recoveryGeneration == generation || !IsCurrentFlow(generation)) return;
            recoveryGeneration = generation;
            session = disconnected;
            game.touchInput.enabled = false;
            Telemetry.Track("match_disconnect", ("match_id", matchId));
            var api = BackendClient.Instance;

            while (IsCurrentFlow(generation))
            {
                Show(L("reconnectingMatch", "Reconnecting to Match"), F("preservingMatchTemplate", "{0}\nYour avatar and match state are being preserved. Checking the match…", initialReason), L("cancel", "Cancel"), Cancel);
                var auth = await api.EnsureSessionAsync();
                if (!IsCurrentFlow(generation)) break;
                if (!auth.Ok)
                {
                    Show(L("reconnectingMatch", "Reconnecting to Match"), F("avatarReservedTemplate", "{0}\nYour avatar remains reserved. Retrying…", auth.Message), L("cancel", "Cancel"), Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    continue;
                }

                var allowed = await api.ReconnectAsync(matchId);
                if (!IsCurrentFlow(generation)) break;
                if (!allowed.Ok || allowed.Value == null)
                {
                    if (allowed.Error is BackendError.Invalid or BackendError.NotFound or BackendError.NotConfigured)
                    {
                        FinishRecovery(generation);
                        Failed(L("matchUnavailableNoReward", "This match is no longer available. No result or reward was created for a failed server."), generation);
                        return;
                    }
                    Show(L("reconnectingMatch", "Reconnecting to Match"), F("retryReconnectCheckTemplate", "{0}\nRetrying the secure reconnect check…", allowed.Message), L("cancel", "Cancel"), Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    if (!IsCurrentFlow(generation)) break;
                    continue;
                }
                if (!allowed.Value.reconnectAllowed)
                {
                    FinishRecovery(generation);
                    Failed(L("matchEnded", "The match has ended. Return Home to queue again."), generation);
                    return;
                }

                var ticket = await api.TicketAsync();
                if (!IsCurrentFlow(generation)) break;
                if (!ticket.Ok || ticket.Value == null || !ticket.Value.serverReady || string.IsNullOrEmpty(ticket.Value.ticket))
                {
                    string message = ticket.Ok ? L("serverRestarting", "The match server is restarting. Retrying…") : ticket.Message;
                    Show(L("reconnectingMatch", "Reconnecting to Match"), message, L("cancel", "Cancel"), Cancel);
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    if (!IsCurrentFlow(generation)) break;
                    continue;
                }

                if (disconnected != null)
                {
                    disconnected.Leave();
                    for (int i = 0; i < 50 && IsCurrentFlow(generation) && disconnected != null && disconnected.Runner != null && disconnected.Runner.IsRunning; i++)
                        await Task.Delay(100);
                await Task.Delay(250); // allow Unity to finish the old runner's shutdown callback
                }
                if (!IsCurrentFlow(generation)) break;

                var next = await NetSession.StartClient(ticket.Value.session, ticket.Value.region, ticket.Value.ticket);
                if (!IsCurrentFlow(generation)) { next?.Leave(); break; }
                if (next == null || next.FailureReason != null)
                {
                    string reason = next?.FailureReason ?? L("connectionCreateFailed", "Could not create a match connection.");
                    Show(L("reconnectingMatch", "Reconnecting to Match"), F("retryFreshTicketTemplate", "{0}\nRetrying with a fresh join ticket…", reason), L("cancel", "Cancel"), Cancel);
                    disconnected = next;
                    session = next;
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectPollSeconds));
                    if (!IsCurrentFlow(generation)) break;
                    continue;
                }

                session = next;
                FinishRecovery(generation);
                Telemetry.Track("match_reconnected", ("match_id", matchId));
                BindPresenter(next, matchId, generation);
                return;
            }

            FinishRecovery(generation);
        }

        void FinishRecovery(int generation)
        {
            if (recoveryGeneration != generation) return;
            recoveryGeneration = -1;
        }

        // ---------- UI ----------

        void Cancel()
        {
            cancelled = true;
            flowGeneration++;
            EndQueueTelemetry(queueMatched ? "cancel_after_match_found" : "user_cancel");
            if (searching || queueRequested) _ = BackendClient.Instance.CancelQueueAsync();
            searching = false;
            queueRequested = false;
            if (session != null) session.Leave();
            StopQueuedPractice();
            ReturnHome();
        }

        /// <summary>Run local practice while the existing matchmaking ticket keeps searching.</summary>
        void PracticeInstead()
        {
            if (queuedPracticeActive || cancelled || !queueRequested) return;
            queuedPracticeActive = true;
            MatchSearchView.Clear(panel);
            if (panel != null) panel.SetActive(false);
            game.StartOfflinePractice();
            ShowQueuedPracticeBanner(L("practiceQueueActive", "Your match search is still active."));
        }

        void ReturnToQueue()
        {
            StopQueuedPractice();
            if (cancelled) return;
            panel?.SetActive(true);
            Show(queueMatched ? L("matchFound", "Match Found") : L("findingMatch", "FINDING A 3v3 MATCH"),
                queueMatched ? L("matchServerPending", "Your match is found. Waiting for its server.") : L("practiceQueueActive", "Your match search is still active."),
                L("cancel", "Cancel"), Cancel);
        }

        void ShowQueuedPracticeBanner(string message)
        {
            DestroyQueuedPracticeBanner();
            int bannerGeneration = flowGeneration;
            var canvas = game?.matchHUD != null ? game.matchHUD.transform : null;
            if (canvas == null) return;
            queuedPracticeBannerRect = UiDraw.Node(canvas, "QueuedPracticeBanner");
            queuedPracticeBanner = queuedPracticeBannerRect.gameObject;
            var background = queuedPracticeBanner.AddComponent<Image>();
            background.color = UIDesignTokens.WithAlpha(UIDesignTokens.SurfacePanel, .96f);

            var copy = UiDraw.Text(queuedPracticeBannerRect, "Status", message, UiFont.Body,
                UIDesignTokens.BodyDp, UIDesignTokens.TextPrimary, 0f, TMPro.TextAlignmentOptions.MidlineLeft);
            copy.textWrappingMode = TextWrappingModes.Normal;
            queuedPracticeCopyText = copy;
            queuedPracticeCopyRect = copy.rectTransform;

            var returnButton = UiDraw.Tap(queuedPracticeBannerRect, "ReturnToQueue", UIDesignTokens.PrimaryAction, 8f, () =>
            {
                if (bannerGeneration != flowGeneration) return;
                GameAudio.Play2D(GameCue.UiTap);
                ReturnToQueue();
            });
            queuedPracticeReturnRect = (RectTransform)returnButton.transform;
            var returnLabel = UiDraw.Text(returnButton.transform, "Label", L("returnToQueue", "Return to Queue"),
                UiFont.Heading, UIDesignTokens.CaptionDp, UIDesignTokens.TextOn(UIDesignTokens.PrimaryAction), 0f,
                TMPro.TextAlignmentOptions.Center);
            returnLabel.textWrappingMode = TextWrappingModes.Normal;
            queuedPracticeReturnLabel = returnLabel;
            UiDraw.Fill(returnLabel.rectTransform);

            var cancelButton = UiDraw.Tap(queuedPracticeBannerRect, "CancelSearch", UIDesignTokens.SurfaceStructural, 8f, () =>
            {
                if (bannerGeneration != flowGeneration) return;
                GameAudio.Play2D(GameCue.UiTap);
                Cancel();
            });
            queuedPracticeCancelRect = (RectTransform)cancelButton.transform;
            var cancelLabel = UiDraw.Text(cancelButton.transform, "Label", L("cancel", "Cancel"), UiFont.Heading,
                UIDesignTokens.CaptionDp, UIDesignTokens.TextPrimary, 0f, TMPro.TextAlignmentOptions.Center);
            cancelLabel.textWrappingMode = TextWrappingModes.Normal;
            queuedPracticeCancelLabel = cancelLabel;
            UiDraw.Fill(cancelLabel.rectTransform);

            FitQueuedPracticeBanner();
        }

        void FitQueuedPracticeBanner()
        {
            if (queuedPracticeBannerRect == null || queuedPracticeCopyRect == null ||
                queuedPracticeReturnRect == null || queuedPracticeCancelRect == null) return;

            float returnWidth = Mathf.Max(132f, UiMetrics.MinTouchUnits);
            float cancelWidth = Mathf.Max(92f, UiMetrics.MinTouchUnits);
            float buttonGroupWidth = returnWidth + cancelWidth + 20f;
            float width = Mathf.Max(560f, 16f + 280f + 16f + buttonGroupWidth);

            // Keep the queue banner above the bottom-center armor/caption stack and
            // away from the top-center score/zone panels and crosshair.
            UiDraw.Place(queuedPracticeBannerRect, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 180f), new Vector2(width, UiMetrics.BarHeight(84f, 54f)));
            UiDraw.Place(queuedPracticeReturnRect, new Vector2(1f, .5f), new Vector2(1f, .5f),
                new Vector2(-(cancelWidth + 20f), 0f), new Vector2(returnWidth, 54f));
            UiDraw.Place(queuedPracticeCancelRect, new Vector2(1f, .5f), new Vector2(1f, .5f),
                new Vector2(-10f, 0f), new Vector2(cancelWidth, 54f));
            UiDraw.Fill(queuedPracticeCopyRect, 16f, 10f, buttonGroupWidth + 16f, 10f);
            if (queuedPracticeCopyText != null) queuedPracticeCopyText.fontSize = UiMetrics.ScaleTextSize(UIDesignTokens.BodyDp);
            if (queuedPracticeReturnLabel != null) queuedPracticeReturnLabel.fontSize = UiMetrics.ScaleTextSize(UIDesignTokens.CaptionDp);
            if (queuedPracticeCancelLabel != null) queuedPracticeCancelLabel.fontSize = UiMetrics.ScaleTextSize(UIDesignTokens.CaptionDp);
            UiFloor.Apply(queuedPracticeBannerRect);
            queuedPracticeBannerDpPerUnit = UiMetrics.DpPerUnit;
            queuedPracticeBannerTextScale = UiMetrics.TextScale;
        }

        void UpdateQueuedPracticeBanner(string message)
        {
            if (queuedPracticeBanner == null) ShowQueuedPracticeBanner(message);
            else
            {
                var copy = queuedPracticeBanner.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
                if (copy != null) copy.text = message;
            }
        }

        void StopQueuedPractice()
        {
            if (!queuedPracticeActive) { DestroyQueuedPracticeBanner(); return; }
            queuedPracticeActive = false;
            DestroyQueuedPracticeBanner();
            game?.matchController?.StopOfflinePractice();
            game?.matchHUD?.HideAllPanels();
            if (game?.touchInput != null) game.touchInput.enabled = false;
        }

        void DestroyQueuedPracticeBanner()
        {
            if (queuedPracticeBanner != null) Destroy(queuedPracticeBanner);
            queuedPracticeBanner = null;
            queuedPracticeBannerRect = null;
            queuedPracticeCopyRect = null;
            queuedPracticeReturnRect = null;
            queuedPracticeCancelRect = null;
            queuedPracticeCopyText = null;
            queuedPracticeReturnLabel = null;
            queuedPracticeCancelLabel = null;
            queuedPracticeBannerDpPerUnit = -1f;
            queuedPracticeBannerTextScale = -1f;
        }

        void Failed(string message, int generation)
        {
            if (!IsCurrentFlow(generation)) return;
            cancelled = true;
            flowGeneration++;
            // QA-015: a failure while queued must not leave a server ticket behind (the server
            // only cancels tickets still searching, so this is harmless after a match is found).
            if (searching || queueRequested) _ = BackendClient.Instance.CancelQueueAsync();
            EndQueueTelemetry(queueMatched ? "failure_after_match_found" : "queue_failure");
            queueRequested = false;
            searching = false;
            StopQueuedPractice();
            if (session != null) { session.Leave(); session = null; }
            game.touchInput.enabled = false;
            Show(L("cannotPlayOnline", "Can't Play Online"), message, L("backHome", "Back to Home"), ReturnHome, L("practiceOffline", "Practice Offline"), () => { ReturnHome(); game.touchInput.enabled = true; game.StartOfflinePractice(); });
            Telemetry.Track("online_failed", ("reason", message.Length > 120 ? message.Substring(0, 120) : message));
        }

        void EndQueueTelemetry(string reason)
        {
            if (!queueTelemetryActive) return;
            int elapsed = Mathf.Max(queueElapsedSeconds,
                queueInitialElapsedSeconds + Mathf.FloorToInt(Time.realtimeSinceStartup - queueTelemetryStartedAt));
            Telemetry.Track("queue_abandoned", ("reason", reason), ("elapsedSeconds", Mathf.Max(0, elapsed)));
            queueTelemetryActive = false;
        }

        void StartQueueTelemetry(bool followingExisting, int initialElapsedSeconds)
        {
            queueInitialElapsedSeconds = Mathf.Max(0, initialElapsedSeconds);
            queueElapsedSeconds = queueInitialElapsedSeconds;
            queueTelemetryStartedAt = Time.realtimeSinceStartup;
            queueTelemetryActive = true;
            queueMatched = false;
            Telemetry.Track("queue_started", ("followingExisting", followingExisting),
                ("elapsedSeconds", queueInitialElapsedSeconds));
        }

        static string MaintenanceMessage(string message, string etaUtc, string compensationNote)
            => BackendClient.QueueMaintenanceMessage(message, etaUtc, compensationNote);

        void ReturnHome()
        {
            cancelled = true;
            flowGeneration++;
            StopQueuedPractice();
            var closingPanel = panel;
            if (closingPanel != null)
                Hotdrop.UI.UiScreenEnterMotion.Exit(closingPanel, () =>
                {
                    if (closingPanel != null) closingPanel.SetActive(false);
                });
            session = null;
            Telemetry.CurrentMatchId = ""; Telemetry.Region = "offline"; Telemetry.CurrentMatchHasBots = false;
            TelemetryUploader.Kick(); // QA-027: deliver this match's events
            game.ReturnToHome();
        }

        void Show(string heading, string body, string action, UnityEngine.Events.UnityAction onAction,
            string secondary = null, UnityEngine.Events.UnityAction onSecondary = null, bool preserveMatchSearch = false)
        {
            int screenGeneration = flowGeneration;
            BuildPanel();
            panel.SetActive(true);
            Hotdrop.UI.UiScreenEnterMotion.Reopen(panel);
            if (!preserveMatchSearch) MatchSearchView.Clear(panel);
            title.text = heading;
            status.text = body;
            actionButton.gameObject.SetActive(action != null); // no action = a transient status, not a dead button
            actionLabel.text = action ?? "";
            actionButton.onClick.RemoveAllListeners();
            if (onAction != null) actionButton.onClick.AddListener(() =>
            {
                if (screenGeneration != flowGeneration) return;
                GameAudio.Play2D(GameCue.UiTap);
                onAction();
            });
            // QA-029: an optional second way out (e.g. practice instead of waiting longer).
            secondaryButton.gameObject.SetActive(secondary != null);
            secondaryLabel.text = secondary ?? "";
            secondaryButton.onClick.RemoveAllListeners();
            if (onSecondary != null) secondaryButton.onClick.AddListener(() =>
            {
                if (screenGeneration != flowGeneration) return;
                GameAudio.Play2D(GameCue.UiTap);
                onSecondary();
            });
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



