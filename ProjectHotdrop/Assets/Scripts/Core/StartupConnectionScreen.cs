using System;
using Hotdrop.UI;
using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>Brief, actionable first screen. It never simulates a connection progress bar:
    /// every non-normal state explains what happened and retains local play when possible.</summary>
    public sealed class StartupConnectionScreen : MonoBehaviour
    {
        GameObject panel;
        GameBootstrap game;
        bool advanced;
        float normalAdvanceAt;

        public void Show(GameBootstrap bootstrap)
        {
            game = bootstrap;
            advanced = false;
            var state = ResolveState();
            if (panel == null) Build(state);
            else { panel.SetActive(true); Refresh(state); }

            // Normal launches should feel immediate but the branded connection moment remains
            // available for a screenshot and does not block automated or keyboard-free starts.
            normalAdvanceAt = state == StartupConnectionState.Normal ? Time.unscaledTime + 1.1f : -1f;
        }

        void Update()
        {
            if (!advanced && normalAdvanceAt > 0f && Time.unscaledTime >= normalAdvanceAt) Continue();
        }

        public void Retry()
        {
            if (panel != null) Destroy(panel);
            panel = null;
            Show(game);
        }

        public void Continue()
        {
            if (advanced || game == null) return;
            advanced = true;
            panel?.SetActive(false);
            game.ContinueFromStartup();
        }

        public void PlayOffline()
        {
            if (advanced || game == null) return;
            advanced = true;
            panel?.SetActive(false);
            game.StartOfflinePractice();
        }

        StartupConnectionState ResolveState()
        {
            var supplied = OnlineHooks.StartupState?.Invoke();
            if (supplied.HasValue) return supplied.Value;
            return Application.internetReachability == NetworkReachability.NotReachable
                ? StartupConnectionState.NoInternet : StartupConnectionState.Normal;
        }

        void Build(StartupConnectionState state)
        {
            var screen = MenuLayout.Create(game.matchHUD.transform, "Startup", "HOTDROP", null);
            panel = screen.Root;
            MenuLayout.AddText(screen.Content, "AIR ROUTE", 20, UIDesignTokens.ActionCyan, "RouteLabel");
            MenuLayout.AddText(screen.Content, "◯  ─────  ◇  ─────  ◯", 32, UIDesignTokens.TextPrimary, "RouteMotif");
            MenuLayout.AddText(screen.Content, "Three players. One team. Three minutes.", 22, UIDesignTokens.TextSecondary, "Tagline");
            MenuLayout.AddButton(screen.Content, "CONTINUE", Continue, "Continue", 108);
            MenuLayout.AddButton(screen.Content, "PLAY OFFLINE", PlayOffline, "Offline", 84);
            MenuLayout.AddButton(screen.Content, "RETRY CONNECTION", Retry, "Retry", 64);
            Refresh(state);
        }

        void Refresh(StartupConnectionState state)
        {
            var status = panel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            // MenuScreen keeps the status line under Nav_Status; finding it by path avoids a
            // second reference surviving a rebuild.
            var statusTransform = panel.transform.Find("Nav_Status");
            if (statusTransform != null) status = statusTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (status == null) return;
            status.text = state switch
            {
                StartupConnectionState.NoInternet => "NO INTERNET — local Offline play is ready.",
                StartupConnectionState.ServiceUnavailable => "SERVICE UNAVAILABLE — retry or play Offline.",
                StartupConnectionState.UpdateRequired => "UPDATE REQUIRED FOR ONLINE PLAY — Offline remains available.",
                StartupConnectionState.Maintenance => "SERVICE MAINTENANCE — local Offline play is ready.",
                StartupConnectionState.RecoveringLocalSave => "RECOVERING LOCAL SAVE — your saved training progress is kept safe.",
                _ => "ROUTE READY — checking only what this build can actually use.",
            };
        }
    }
}
