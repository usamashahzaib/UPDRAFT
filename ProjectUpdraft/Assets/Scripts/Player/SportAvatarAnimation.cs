using UnityEngine;

namespace Updraft.Player
{
    /// <summary>Original prototype utility-sport rig; cosmetic motion never changes the capsule.</summary>
    public class SportAvatarAnimation : MonoBehaviour
    {
        public Transform leftLeg, rightLeg;
        Vector3 previousPosition;
        float phase;
        void OnEnable() { previousPosition = transform.position; }
        void LateUpdate()
        {
            float speed = Vector3.Distance(transform.position, previousPosition) / Mathf.Max(Time.deltaTime, 0.001f);
            previousPosition = transform.position;
            phase += Time.deltaTime * Mathf.Clamp(speed, 0, 6) * 2.5f;
            float swing = Mathf.Sin(phase) * Mathf.Clamp01(speed / 3f) * 22f;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
        }
    }
}
