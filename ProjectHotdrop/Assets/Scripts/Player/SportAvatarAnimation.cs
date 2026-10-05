using UnityEngine;

namespace Hotdrop.Player
{
    /// <summary>
    /// Original prototype utility-sport rig; cosmetic motion never changes the capsule.
    /// Amendment 2: grounded humanoid motion for the shared avatar - the leg cycle and
    /// counter-swinging arms read movement, an idle sway keeps a
    /// standing character alive, and fire/reload give the combat pose real state instead of a
    /// frozen statue. Every target is optional so network and preview avatars can wire only
    /// the parts they have. Rotation only: the root never moves and no collider is touched,
    /// so motion cannot change the capsule, hitboxes or reach.
    /// </summary>
    public class SportAvatarAnimation : MonoBehaviour
    {
        public Transform leftLeg, rightLeg;
        public Transform leftArm, rightArm;
        public Transform torso;

        [Header("Motion")]
        [SerializeField] float maxLegSwingDegrees = 22f;
        [SerializeField] float armSwingRatio = 0.7f;

        Vector3 previousPosition;
        float phase;
        float firePulse;
        bool reloading;
        bool grounded = true;

        /// <summary>Drives the combat pose. Safe to call every frame or never.</summary>
        public void SetActionState(bool isReloading, bool hasGroundContact)
        {
            reloading = isReloading;
            grounded = hasGroundContact;
        }

        /// <summary>A short right-arm kick + torso dip for a fired shot.</summary>
        public void PulseFire() => firePulse = 1f;

        void OnEnable()
        {
            previousPosition = transform.position;
            firePulse = 0f;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            float speed = Vector3.Distance(transform.position, previousPosition) / dt;
            previousPosition = transform.position;
            speed = Mathf.Clamp(speed, 0f, 8f);

            phase += dt * Mathf.Clamp(speed, 0f, 6f) * 2.5f;
            float drive = Mathf.Clamp01(speed / 3f);
            float swing = Mathf.Sin(phase) * maxLegSwingDegrees * drive;
            float airborne = grounded ? 0f : 1f;

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            // Arms counter-swing against the legs; in the air they settle into a braced pose.
            firePulse = Mathf.MoveTowards(firePulse, 0f, dt * 6f);
            float armSwing = swing * armSwingRatio;
            float armPitchL = -48f - armSwing * .25f - airborne * 8f;
            float armPitchR = -58f + armSwing * .25f - airborne * 8f - firePulse * 14f;
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(armPitchL, 0f, 24f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(armPitchR, 0f, -8f);

            if (reloading && leftArm != null)
            {
                // A visible magazine action reads at a glance even when the HUD is busy.
                float beat = Mathf.Sin(Time.time * 7f) * 7f;
                leftArm.localRotation = Quaternion.Euler(-48f + beat, 0f, 14f);
            }

            if (torso != null)
            {
                // Package 1 explicitly excludes automatic leaning. Keep only a tiny fired-shot
                // response and idle life in the presentation rig; neither affects movement or aim.
                float lean = firePulse * 1.6f;
                float idleSway = speed < 0.5f ? Mathf.Sin(Time.time * 1.6f) * 0.8f : 0f;
                torso.localRotation = Quaternion.Euler(lean, idleSway, 0f);
            }
        }
    }
}

