using UnityEngine;

namespace Updraft.Core
{
    /// <summary>
    /// Spec section 25 pad validation study: two counterbalanced versions of the
    /// same build, with and without pads. A facilitator sets the version per
    /// participant session from the hidden study panel on Home (long-press the
    /// title); it is recorded as the telemetry cohort. Players are never
    /// rewarded for choosing either version.
    /// </summary>
    public static class StudySettings
    {
        public const string PadsOn = "study_pads_on";
        public const string PadsOff = "study_pads_off";

        public static bool PadsEnabled => Telemetry.Cohort != PadsOff;
        public static bool IncentivizedTester => Telemetry.IncentivizedTester;

        public static void SetCohort(string cohort)
        {
            PlayerPrefs.SetString(Telemetry.CohortKey, cohort);
            PlayerPrefs.Save();
            Telemetry.Track("study_cohort_set", ("cohort", cohort));
        }

        public static void SetIncentivizedTester(bool incentivized)
        {
            PlayerPrefs.SetInt(Telemetry.IncentivizedTesterKey, incentivized ? 1 : 0);
            PlayerPrefs.Save();
            Telemetry.Track("study_incentivized_set", ("incentivized", incentivized));
        }
    }
}
