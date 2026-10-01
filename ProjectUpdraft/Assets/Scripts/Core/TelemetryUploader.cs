using UnityEngine;

namespace Updraft.Core
{
    /// <summary>
    /// QA-027: the local analytics queue is actually delivered to the backend intake — at
    /// startup, whenever the player returns Home and every few minutes on Home. Offline-study
    /// facts stay on the device (BackendClient.UploadTelemetryAsync keeps them) and a failed
    /// upload keeps the queue for the next attempt. Never blocks or breaks gameplay.
    /// </summary>
    public static class TelemetryUploader
    {
        const float MinSecondsBetweenUploads = 120f;
        static bool uploading;
        static float lastAttempt = -9999f;

        public static async void Kick(bool force = false)
        {
            if (uploading) return;
            if (!force && Time.realtimeSinceStartup - lastAttempt < MinSecondsBetweenUploads) return;
            var api = BackendClient.Instance;
            if (api == null || !api.IsConfigured) return;
            uploading = true;
            lastAttempt = Time.realtimeSinceStartup;
            try
            {
                var auth = await api.EnsureSessionAsync();
                if (auth.Ok) await api.UploadTelemetryAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Telemetry upload failed: " + e.Message);
            }
            finally { uploading = false; }
        }
    }
}
