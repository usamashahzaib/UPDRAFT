using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>
    /// Amendment 1: one truthful "what happens next" used by both Home's goal card and the
    /// post-match results screen, resolved from real state. It never advertises a disabled
    /// contract, an invented account reward or a goal the player has already finished.
    /// </summary>
    public static class NextGoal
    {
        static string L(string key, string fallback) => Hotdrop.UI.UiDraw.L("home.goal." + key, fallback);

        /// <summary>Two-line goal text. Does not create components; call <see cref="EnsureOwner"/> first
        /// when the caller is willing to build the challenge runtime.</summary>
        public static string Describe(GameObject owner)
        {
            if (owner == null) return L("playMatch.title", "Play a Match") + "\n" + L("playMatch.detail", "Three minutes. Hold the zone.");
            if (PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0)
                return L("learnToLaunch.title", "Learn to Launch") + "\n" + L("learnToLaunch.detail", "Move, shoot and use your first pad.");

            var controller = owner.GetComponent<OfflineChallengeRuntime>()?.Controller;
            if (controller != null && controller.config != null && controller.config.entries != null)
                foreach (var entry in controller.config.entries)
                    if (controller.RecordForCurrentContext(entry.id)?.isCompleted != true)
                        return Hotdrop.UI.UiDraw.L("offlineLibrary.challenge." + entry.id + ".title", entry.title) + "\n" + L("challenge.detail", "Your next rooftop challenge.");

            return controller == null
                ? L("basicRoute.title", "Basic Route") + "\n" + L("basicRoute.detail", "Start your rooftop challenge run.")
                : L("rooftopsMastered.title", "Rooftops Mastered") + "\n" + L("rooftopsMastered.detail", "Beat your best in another Relay.");
        }

        /// <summary>Single-line form for the results screen: outcome, one contribution and the
        /// next reachable goal on the same screen (Amendment 1, first-run journey step 4).</summary>
        public static string Short(GameObject owner)
        {
            string text = Describe(owner);
            int newline = text.IndexOf('\n');
            if (newline < 0) return Hotdrop.UI.UiDraw.LFormat("home.goal.shortSingleLine", "Next: {0}", text);
            return Hotdrop.UI.UiDraw.LFormat("home.goal.shortTwoLine", "NEXT: {0} — {1}", text.Substring(0, newline), text.Substring(newline + 1));
        }

        /// <summary>
        /// Authored challenges completed out of the authored total, as a 0..1 fraction.
        /// Only the current context's records count (the same context <see cref="Describe"/>
        /// reads), and an empty or absent library reports 0 rather than a fabricated 100%.
        /// Pure so the Home bar and its tests cannot disagree.
        /// </summary>
        public static float Progress(GameObject owner)
        {
            if (owner == null) return 0f;
            var entries = owner.GetComponent<OfflineChallengeRuntime>()?.Controller?.config?.entries;
            if (entries == null || entries.Count == 0) return 0f;
            int done = 0;
            foreach (var entry in entries)
                if (owner.GetComponent<OfflineChallengeRuntime>().Controller.RecordForCurrentContext(entry.id)?.isCompleted == true)
                    done++;
            return Mathf.Clamp01((float)done / entries.Count);
        }
    }
}
