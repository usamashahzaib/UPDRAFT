using UnityEngine;
using Hotdrop.Config;
using Hotdrop.UI;

namespace Hotdrop.Camera
{
    /// <summary>
    /// Spec section 8: Third-person shoulder camera.
    /// 55° FOV, 3.2m follow distance, 0.45m shoulder offset, upper-torso pivot,
    /// sphere-cast wall clip prevention, subtle shake (disableable), no combat cinematics.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public GraphicsConfig graphicsConfig;

        [Header("Camera Settings")]
        [SerializeField] float fieldOfView = 55f;
        [SerializeField] float followDistance = 3.2f;
        [SerializeField] float shoulderOffset = 0.45f;
        [SerializeField] float verticalOffset = 1.4f; // upper-torso pivot height from feet
        [SerializeField] float pitchMin = -30f;
        [SerializeField] float pitchMax = 60f;
        [SerializeField] float sphereCastRadius = 0.3f;
        [SerializeField] LayerMask obstructionMask;

        [Header("Smoothing")]
        [SerializeField] float positionLerpSpeed = 15f;
        [SerializeField] float rotationLerpSpeed = 20f;

        [Header("Shake")]
        [SerializeField] bool shakeEnabled = true;
        [SerializeField] float shakeMagnitude = 0.1f;
        [SerializeField] float shakeDuration = 0.15f;

        Transform target;
        float currentYaw;
        float currentPitch;
        float targetYaw;
        float targetPitch;

        Vector3 currentVelocity;
        float shakeTimer;
        Vector3 shakeOffset;
        readonly RaycastHit[] aimHits = new RaycastHit[64];

        public Vector3 AimDirection => transform.forward;
        public Vector3 AimPosition => transform.position;
        public bool IsShaking => shakeTimer > 0f;

        public const string ShakePrefKey = "CameraShakeEnabled";

        void Awake()
        {
            // QA-021: restore the saved shake preference (default on) so gameplay matches the settings UI.
            shakeEnabled = PlayerPrefs.GetInt(ShakePrefKey, 1) != 0;

            if (obstructionMask.value == 0)
                obstructionMask = LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");
            UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
            if (cam != null)
                cam.fieldOfView = fieldOfView;

            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            // Initialize angles from current transform
            Vector3 euler = transform.eulerAngles;
            currentYaw = targetYaw = euler.y;
            currentPitch = targetPitch = euler.x;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                // Initialize behind target
                Vector3 dir = target.forward;
                dir.y = 0;
                dir.Normalize();
                targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                targetPitch = 15f; // slight downward angle
            }
        }

        /// <summary>Face the direction the target faces immediately (spawns/respawns), without a swing.</summary>
        public void SnapBehindTarget()
        {
            if (target == null) return;
            SetTarget(target);
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }

        public void AddYawPitch(float yawDelta, float pitchDelta)
        {
            targetYaw += yawDelta;
            targetPitch = Mathf.Clamp(targetPitch + pitchDelta, pitchMin, pitchMax);
        }

        /// <summary>Apply weapon camera kick unless the player has requested reduced motion.</summary>
        public void AddRecoilPitch(float pitchDelta)
        {
            if (UIMotionSettings.ReduceMotionEnabled) return;
            AddYawPitch(0f, pitchDelta);
        }

        public void TriggerShake(float magnitudeMultiplier = 1f)
        {
            if (!shakeEnabled || UIMotionSettings.ReduceMotionEnabled) return;
            shakeTimer = shakeDuration;
            shakeOffset = Random.insideUnitSphere * shakeMagnitude * magnitudeMultiplier;
            shakeOffset.y *= 0.5f; // less vertical shake
        }

        public void SetShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
            PlayerPrefs.SetInt(ShakePrefKey, enabled ? 1 : 0);
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Update shake
            if (UIMotionSettings.ReduceMotionEnabled)
            {
                shakeTimer = 0f;
                shakeOffset = Vector3.zero;
            }
            else if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float t = shakeTimer / shakeDuration;
                shakeOffset *= t; // fade out
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            // Smooth angles
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationLerpSpeed * Time.deltaTime);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationLerpSpeed * Time.deltaTime);

            // Calculate desired position
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 head = target.position + Vector3.up * verticalOffset;
            int mask = obstructionMask.value;
            // QA-004: the shoulder offset is itself swept so the pivot never starts inside a wall.
            Vector3 pivot = ClampToClearSpace(head, head + rotation * (Vector3.right * shoulderOffset), sphereCastRadius, mask);
            Vector3 desiredPosition = ClampToClearSpace(pivot, pivot - rotation * Vector3.forward * followDistance, sphereCastRadius, mask);

            // Smooth position, then a final corrective sweep so smoothing/shake can never carry
            // the camera through a blocker (thin walls, corners, moving cover).
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition + shakeOffset, ref currentVelocity, 1f / positionLerpSpeed);
            transform.position = ClampToClearSpace(pivot, smoothed, sphereCastRadius, mask);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationLerpSpeed * Time.deltaTime);
        }

        const float CollisionSkin = 0.05f;

        /// <summary>
        /// QA-004: moves a sphere of <paramref name="radius"/> from <paramref name="from"/> toward
        /// <paramref name="to"/> and returns the furthest centre that stays on the near side of any
        /// blocker. (The previous version offset hit.point further along the cast — into the wall.)
        /// </summary>
        public static Vector3 ClampToClearSpace(Vector3 from, Vector3 to, float radius, int mask)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 1e-4f) return to;
            dir /= dist;

            // SphereCast ignores colliders the sphere already overlaps at its origin; fall back to
            // a ray in that case so a pivot squeezed against geometry still cannot pass through it.
            if (Physics.CheckSphere(from, radius, mask, QueryTriggerInteraction.Ignore))
            {
                if (Physics.Raycast(from, dir, out RaycastHit rayHit, dist, mask, QueryTriggerInteraction.Ignore))
                    return from + dir * Mathf.Max(0f, rayHit.distance - CollisionSkin);
                return to;
            }
            if (Physics.SphereCast(from, radius, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
                return from + dir * Mathf.Max(0f, hit.distance - CollisionSkin);
            return to;
        }

        /// <summary>
        /// Get the aim point in world space for shooting (spec: "aim point from camera, shot traced muzzle→point").
        /// Returns point at max range or hit point.
        /// </summary>
        public Vector3 GetAimPoint(float maxRange, LayerMask aimMask)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            int count = Physics.RaycastNonAlloc(ray, aimHits, maxRange, aimMask, QueryTriggerInteraction.Ignore);
            float nearest = maxRange;
            Vector3 point = ray.GetPoint(maxRange);
            var owner = target != null ? target.GetComponent<Hotdrop.Player.PlayerController>() : null;
            for (int i = 0; i < count; i++)
            {
                var player = aimHits[i].collider.GetComponentInParent<Hotdrop.Player.PlayerController>();
                if (player != null && (player == owner || (owner != null && player.TeamId == owner.TeamId))) continue;
                if (aimHits[i].distance >= nearest) continue;
                nearest = aimHits[i].distance; point = aimHits[i].point;
            }
            return point;
        }

        /// <summary>
        /// Check if a shot from muzzle to aim point is blocked by geometry (spec: "walls can block even when camera sees over").
        /// </summary>
        public bool IsShotBlocked(Vector3 muzzlePosition, Vector3 aimPoint, LayerMask blockMask, out Vector3 blockPoint)
        {
            Vector3 direction = aimPoint - muzzlePosition;
            float distance = direction.magnitude;
            if (distance < 0.01f)
            {
                blockPoint = muzzlePosition;
                return false;
            }

            if (Physics.Raycast(muzzlePosition, direction.normalized, out RaycastHit hit, distance, blockMask, QueryTriggerInteraction.Ignore))
            {
                blockPoint = hit.point;
                return true;
            }

            blockPoint = aimPoint;
            return false;
        }
    }
}



