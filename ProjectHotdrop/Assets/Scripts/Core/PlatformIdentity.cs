using System;
using System.Threading.Tasks;

namespace Hotdrop.Core
{
    /// <summary>
    /// QA-026: the device's platform identity (Google Play Games). A Play Games integration
    /// sets <see cref="ServerAuthCodeProvider"/> at startup to return a one-time server auth
    /// code (or null if the player cancels). Until the publisher's Play Games client is
    /// configured, no provider exists and the Account screen says so instead of pretending.
    /// </summary>
    public static class PlatformIdentity
    {
        public static Func<Task<string>> ServerAuthCodeProvider { get; set; }

        public static bool Available => ServerAuthCodeProvider != null;

        public static async Task<string> RequestServerAuthCodeAsync()
        {
            if (ServerAuthCodeProvider == null) return null;
            try { return await ServerAuthCodeProvider(); }
            catch (Exception) { return null; }
        }
    }
}

