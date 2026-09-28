using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Updraft.Config;
using Updraft.Core;

namespace Updraft.Network
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

        GameBootstrap game;
        GameObject panel;
        TextMeshProUGUI title, status;
        Button actionButton;
        TextMeshProUGUI actionLabel;
        bool cancelled;
        bool searching;
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

            var enqueue = await api.EnqueueAsync();
            if (cancelled) return;
            if (!enqueue.Ok && enqueue.Error != BackendError.Conflict) // 409 = already queued: keep waiting
            {
                Failed(enqueue.Value?.error == "matchmaking_disabled" ? "Matchmaking is paused right now. Practice offline and try again soon." : enqueue.Message);
                return;
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
                Show("FINDING A 3v3 MATCH", $"Searching {s / 60}:{s % 60:00}\nFair matches first — empty seats fill with labeled bots after a short wait.", "CANCEL", Cancel);
                await Task.Delay(2000);
            }
            if (cancelled) return;
            searching = false;

            float deadline = Time.realtimeSinceStartup + ServerWaitSeconds;
            TicketDto ticket = null;
            while (!cancelled && Time.realtimeSinceStartup < deadline)
            {
                Show("MATCH FOUND", "Reserving a match server…", "CANCEL", Cancel);
                var t = await api.TicketAsync();
                if (!t.Ok) { Failed(t.Message); return; }
                if (t.Value.serverReady && !string.IsNullOrEmpty(t.Value.ticket)) { ticket = t.Value; break; }
                await Task.Delay(1500);
            }
            if (cancelled) return;
            if (ticket == null) { Failed("No match server became available. Nothing was lost — please queue again."); return; }

            Telemetry.Track("match_joining", ("match_id", ticket.matchId), ("has_bots", ticket.hasBots ? "1" : "0"));
            Show("LOADING MATCH", ticket.hasBots ? "Joining… (some seats are labeled bots)" : "Joining…", "CANCEL", Cancel);
            await Join(ticket.session, ticket.region, ticket.ticket, ticket.matchId);
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
            if (searching) _ = BackendClient.Instance.CancelQueueAsync();
            searching = false;
            if (session != null) session.Leave();
            ReturnHome();
        }

        void Failed(string message)
        {
            searching = false;
            if (session != null) { session.Leave(); session = null; }
            game.touchInput.enabled = false;
            Show("CAN'T PLAY ONLINE", message, "BACK TO HOME", ReturnHome);
            Telemetry.Track("online_failed", ("reason", message.Length > 120 ? message.Substring(0, 120) : message));
        }

        void ReturnHome()
        {
            if (panel != null) panel.SetActive(false);
            session = null;
            game.ReturnToHome();
        }

        void Show(string heading, string body, string action, UnityEngine.Events.UnityAction onAction)
        {
            BuildPanel();
            panel.SetActive(true);
            title.text = heading;
            status.text = body;
            actionLabel.text = action;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => { GameAudio.Play2D(GameCue.UiTap); onAction(); });
        }

        void BuildPanel()
        {
            if (panel != null) return;
            panel = new GameObject("OnlinePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(game.matchHUD.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.063f, 0.094f, 0.153f, 0.98f);
            title = Updraft.UI.Ui.Text("Title", panel.transform, "", 56, Updraft.UI.MatchHUD.MainText);
            title.rectTransform.anchoredPosition = new Vector2(0, 170);
            title.rectTransform.sizeDelta = new Vector2(1200, 80);
            status = Updraft.UI.Ui.Text("Status", panel.transform, "", 28, Updraft.UI.MatchHUD.SecondaryText);
            status.rectTransform.anchoredPosition = new Vector2(0, 40);
            status.rectTransform.sizeDelta = new Vector2(1200, 160);

            var buttonObj = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);
            var brt = buttonObj.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(0, -150); brt.sizeDelta = new Vector2(480, 90);
            var image = buttonObj.GetComponent<Image>(); image.color = new Color(0.106f, 0.161f, 0.251f);
            actionButton = buttonObj.GetComponent<Button>(); actionButton.targetGraphic = image;
            actionLabel = Updraft.UI.Ui.Text("Label", buttonObj.transform, "", 28, Updraft.UI.MatchHUD.Friendly);
            actionLabel.rectTransform.sizeDelta = new Vector2(460, 80);
        }
    }
}
