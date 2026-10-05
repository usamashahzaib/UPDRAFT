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
        /// <summary>Two-line goal text. Does not create components; call <see cref="EnsureOwner"/> first
        /// when the caller is willing to build the challenge runtime.</summary>
        public static string Describe(GameObject owner)
        {
            if (owner == null) return "PLAY A MATCH\nThree minutes. Hold the zone.";
            if (PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0)
                return "LEARN TO LAUNCH\nMove, shoot and use your first pad.";

            var controller = owner.GetComponent<OfflineChallengeRuntime>()?.Controller;
            if (controller != null && controller.config != null && controller.config.entries != null)
                foreach (var entry in controller.config.entries)
                    if (controller.RecordForCurrentContext(entry.id)?.isCompleted != true)
                        return entry.title.ToUpperInvariant() + "\nYour next rooftop challenge.";

            return controller == null
                ? "BASIC ROUTE\nStart your rooftop challenge run."
                : "ROOFTOPS MASTERED\nBeat your best in another Relay.";
        }

        /// <summary>Single-line form for the results screen: outcome, one contribution and the
        /// next reachable goal on the same screen (Amendment 1, first-run journey step 4).</summary>
        public static string Short(GameObject owner)
        {
            string text = Describe(owner);
            int newline = text.IndexOf('\n');
            if (newline < 0) return "NEXT: " + text;
            return "NEXT: " + text.Substring(0, newline) + " — " + text.Substring(newline + 1);
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
