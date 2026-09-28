using System;

namespace Updraft.Core
{
    /// <summary>
    /// Seam between the offline game (Updraft.Runtime) and online play (Updraft.Network,
    /// which references Runtime, never the reverse). The network assembly installs these.
    /// </summary>
    public static class OnlineHooks
    {
        /// <summary>Starts the online journey from Home (matchmaking → server → match).</summary>
        public static Action<GameBootstrap> PlayOnline;

        /// <summary>Null when online play can start; otherwise the reason shown on Home.</summary>
        public static Func<string> UnavailableReason;

        /// <summary>True in a headless dedicated-server process: no Home, tutorial or offline match.</summary>
        public static bool IsDedicatedServer =>
            Array.IndexOf(Environment.GetCommandLineArgs(), "-updraftServer") >= 0;
    }
}
