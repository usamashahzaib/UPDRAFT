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

        GameBootstrap game;
        GameObject panel;
        TextMeshProUGUI title, status;
        Button actionButton;
        TextMeshProUGUI actionLabel;
        bool cancelled;
        bool searching;
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
            await Join(config.devDirectSession, config.devDirectRegion, $"dev:A:{alias}");
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
            await Join(ticket.session, ticket.region, ticket.ticket);
        }

        async Task Join(string sessionName, string region, string ticket)
        {
            session = await NetSession.StartClient(sessionName, region, ticket);
            if (cancelled) { session.Leave(); return; }
            if (session.FailureReason != null) { Failed(session.FailureReason); return; }

            var presenter = session.gameObject.AddComponent<NetClientPresenter>();
            presenter.Bind(game, session, ReturnHome);
            session.OnFailed += reason => { if (!presenter.ShowingResults) Failed(reason); };
            panel.SetActive(false);
            game.touchInput.enabled = true;
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
