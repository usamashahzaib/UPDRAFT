using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>Dark-launch gate. No asset means disabled, including in editor and development builds.</summary>
    public sealed class ContractsMasteryFeature : ScriptableObject
    {
        [Tooltip("Enable only for a staging build after backend result-pipeline approval.")]
        public bool enabledForThisBuild;
        static ContractsMasteryFeature cached;
        public static bool Enabled
        {
            get
            {
                if (cached == null) cached = Resources.Load<ContractsMasteryFeature>("ContractsMasteryFeature");
                return cached != null && cached.enabledForThisBuild;
            }
        }
    }
}
