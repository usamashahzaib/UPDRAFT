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

        // Skinned operator (07_WORLD_ART): when the rig carries an Animator, real clips replace
        // the procedural swing. Same inputs, same call sites; still presentation-only.
        Animator animator;
        Transform animatedRoot, spine, chest;
        float legYaw;
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int MoveDirId = Animator.StringToHash("MoveDir");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int ReloadingId = Animator.StringToHash("Reloading");
        static readonly int FireId = Animator.StringToHash("Fire");
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
        public void PulseFire()
        {
            firePulse = 1f;
            if (ResolveAnimator() != null) animator.SetTrigger(FireId);
        }

        Animator ResolveAnimator()
        {
            if (animator != null && animator.transform.IsChildOf(transform)) return animator;
            animator = GetComponentInChildren<Animator>();
            animatedRoot = animator != null ? animator.transform : null;
            spine = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Spine) : null;
            chest = animator != null && animator.isHuman
                ? (animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest)) : null;
            legYaw = 0f;
            return animator;
        }

        /// <summary>
        /// Clips follow measured motion: speed picks idle/walk/jog/sprint, a backpedal plays the
        /// run in reverse, and a strafe turns the legs (up to 55 degrees) toward the movement while
        /// the spine turns back so the chest and weapon stay on the aim line.
        /// </summary>
        void DriveAnimator(Vector3 velocity, float dt)
        {
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            float speed = Mathf.Min(flat.magnitude, 8f);
            float direction = 1f, targetYaw = 0f;
            if (speed > .4f)
            {
                var local = transform.InverseTransformDirection(flat);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(angle) > 100f) { direction = -1f; angle -= 180f * Mathf.Sign(angle); }
                targetYaw = Mathf.Clamp(angle, -55f, 55f);
            }
            animator.SetFloat(SpeedId, speed, .08f, dt);
            animator.SetFloat(MoveDirId, direction);
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(ReloadingId, reloading);
            legYaw = Mathf.MoveTowardsAngle(legYaw, targetYaw, 300f * dt);
            if (animatedRoot != null) animatedRoot.localRotation = Quaternion.Euler(0f, legYaw, 0f);
            var up = transform.up;
            if (spine != null) spine.rotation = Quaternion.AngleAxis(-legYaw * .5f, up) * spine.rotation;
            if (chest != null && chest != spine) chest.rotation = Quaternion.AngleAxis(-legYaw * .5f, up) * chest.rotation;
        }

        void OnEnable()
        {
            previousPosition = transform.position;
            firePulse = 0f;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            var velocity = (transform.position - previousPosition) / dt;
            float speed = velocity.magnitude;
            previousPosition = transform.position;
            if (ResolveAnimator() != null && animator.isActiveAndEnabled)
            {
                DriveAnimator(velocity, dt);
                return;
            }
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

