using UnityEngine;
using Updraft.Config;

namespace Updraft.Camera
{
    /// <summary>
    /// Spec section 8: Third-person shoulder camera.
    /// 55° FOV, 3.2m follow distance, 0.45m shoulder offset, upper-torso pivot,
    /// sphere-cast wall clip prevention, subtle shake (disableable), no combat cinematics.
    /// </summary>
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

        public Vector3 AimDirection => transform.forward;
        public Vector3 AimPosition => transform.position;
        public bool IsShaking => shakeTimer > 0f;

        void Awake()
        {
            UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
            if (cam != null)
                cam.fieldOfView = fieldOfView;

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
                Vector3 dir = -target.forward;
                dir.y = 0;
                dir.Normalize();
                targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                targetPitch = 15f; // slight downward angle
            }
        }

        public void AddYawPitch(float yawDelta, float pitchDelta)
        {
            targetYaw += yawDelta;
            targetPitch = Mathf.Clamp(targetPitch + pitchDelta, pitchMin, pitchMax);
        }

        public void TriggerShake(float magnitudeMultiplier = 1f)
        {
            if (!shakeEnabled) return;
            shakeTimer = shakeDuration;
            shakeOffset = Random.insideUnitSphere * shakeMagnitude * magnitudeMultiplier;
            shakeOffset.y *= 0.5f; // less vertical shake
        }

        public void SetShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
            PlayerPrefs.SetInt("CameraShakeEnabled", enabled ? 1 : 0);
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Update shake
            if (shakeTimer > 0f)
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
            Vector3 targetPosition = target.position + Vector3.up * verticalOffset;
            targetPosition += rotation * (Vector3.right * shoulderOffset); // shoulder offset
            Vector3 desiredPosition = targetPosition - rotation * Vector3.forward * followDistance;

            // Obstruction check (sphere cast from target to desired position)
            Vector3 direction = desiredPosition - targetPosition;
            float distance = direction.magnitude;
            if (distance > 0.01f)
            {
                RaycastHit hit;
                if (Physics.SphereCast(targetPosition, sphereCastRadius, direction.normalized, out hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    desiredPosition = hit.point + direction.normalized * sphereCastRadius;
                }
            }

            // Smooth position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition + shakeOffset, ref currentVelocity, 1f / positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationLerpSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Get the aim point in world space for shooting (spec: "aim point from camera, shot traced muzzle→point").
        /// Returns point at max range or hit point.
        /// </summary>
        public Vector3 GetAimPoint(float maxRange, LayerMask aimMask)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, aimMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }
            return ray.GetPoint(maxRange);
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