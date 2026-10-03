using System;

namespace Hotdrop.Core
{
    /// <summary>Player-facing connection result used before Home. These are deliberately
    /// concrete states, rather than a decorative spinner with invented progress.</summary>
    public enum StartupConnectionState
    {
        Normal,
        NoInternet,
        ServiceUnavailable,
        UpdateRequired,
        Maintenance,
        RecoveringLocalSave,
    }

    /// <summary>
    /// Seam between the offline game (Hotdrop.Runtime) and online play (Hotdrop.Network,
    /// which references Runtime, never the reverse). The network assembly installs these.
    /// </summary>
    public static class OnlineHooks
    {
        /// <summary>Starts the online journey from Home (matchmaking → server → match).</summary>
        public static Action<GameBootstrap> PlayOnline;

        /// <summary>Null when online play can start; otherwise the reason shown on Home.</summary>
        public static Func<string> UnavailableReason;

        /// <summary>Optional network implementation result for the startup shell. A null
        /// provider means the offline slice only knows the device reachability state.</summary>
        public static Func<StartupConnectionState> StartupState;

        /// <summary>True in a headless dedicated-server process: no Home, tutorial or offline match.</summary>
        public static bool IsDedicatedServer =>
            Array.IndexOf(Environment.GetCommandLineArgs(), "-updraftServer") >= 0;
    }
}


