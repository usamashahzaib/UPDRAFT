using UnityEngine;

namespace Updraft.Config
{
    /// <summary>
    /// Backend endpoint per environment (spec section 31: separate dev/stage/prod).
    /// Empty baseUrl means the build runs offline-only and says so; never point a
    /// release build at a development server. Contains no secrets.
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "Updraft/Config/Backend Config")]
    public class BackendConfig : ScriptableObject
    {
        public string environmentName = "offline";
        public string baseUrl = "";
        public float requestTimeoutSeconds = 10f;
        public int clientProtocolVersion = 1;

        [Header("Development builds only")]
        [Tooltip("Direct-join a dev dedicated server (started with -devSession NAME --insecure-dev), skipping matchmaking. Ignored in release builds.")]
        public string devDirectSession = "";
        [Tooltip("Photon region for the dev session, e.g. \"eu\". Empty = best region.")]
        public string devDirectRegion = "";

        public bool IsConfigured
        {
            get
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return false;
                // A store build must never ship an endpoint that only exists on the
                // developer's machine. The publisher supplies an HTTPS stage/prod URL.
                if (!Debug.isDebugBuild && string.Equals(environmentName, "dev", System.StringComparison.OrdinalIgnoreCase)) return false;
                if (!Debug.isDebugBuild && (!System.Uri.TryCreate(baseUrl, System.UriKind.Absolute, out var uri) || uri.Scheme != System.Uri.UriSchemeHttps)) return false;
                return true;
            }
        }
        public bool UsesDevDirectSession => Debug.isDebugBuild && !string.IsNullOrWhiteSpace(devDirectSession);

        public static BackendConfig Load() => Resources.Load<BackendConfig>("Config/BackendConfig");
    }
}
