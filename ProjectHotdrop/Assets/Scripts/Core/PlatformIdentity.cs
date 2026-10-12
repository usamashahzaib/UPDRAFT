using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotdrop.Core
{
    public enum PlatformAuthCodeState { Ready, Cancelled, Unavailable, Failed }

    public readonly struct PlatformAuthCodeResult
    {
        public readonly string Code;
        public readonly PlatformAuthCodeState State;
        public PlatformAuthCodeResult(string code, PlatformAuthCodeState state)
        { Code = code; State = state; }
    }

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

        public static async Task<PlatformAuthCodeResult> RequestServerAuthCodeAsync()
        {
            var provider = ServerAuthCodeProvider;
            if (provider == null) return new PlatformAuthCodeResult(null, PlatformAuthCodeState.Unavailable);
            try
            {
                string code = await provider();
                return string.IsNullOrWhiteSpace(code)
                    ? new PlatformAuthCodeResult(null, PlatformAuthCodeState.Cancelled)
                    : new PlatformAuthCodeResult(code, PlatformAuthCodeState.Ready);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Google Play Games sign-in failed: {exception.Message}");
                return new PlatformAuthCodeResult(null, PlatformAuthCodeState.Failed);
            }
        }
    }
}

