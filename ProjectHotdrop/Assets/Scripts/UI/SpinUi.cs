using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>Spins a UI element at a fixed rate in unscaled time (search sweep, scan rings).</summary>
    public sealed class SpinUi : MonoBehaviour
    {
        public float degreesPerSecond = 120f;
        void Update() => transform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * degreesPerSecond);
    }
}
