using UnityEngine;
using Updraft.Pads;
using Updraft.Config;
using Updraft.Player;

namespace Updraft.Pads
{
    /// <summary>
    /// MonoBehaviour wrapper for LaunchPad simulation logic.
    /// Handles visual representation, arming effects, lifetime, and activation.
    /// </summary>
    public class LaunchPadBehaviour : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] GameObject armingEffect;
        [SerializeField] GameObject armedEffect;
        [SerializeField] GameObject destroyEffect;
        [SerializeField] Renderer padRenderer;
        [SerializeField] Material friendlyMaterial;
        [SerializeField] Material enemyMaterial;

        [Header("Audio")]
        [SerializeField] AudioClip deploySound;
        [SerializeField] AudioClip armSound;
        [SerializeField] AudioClip launchSound;
        [SerializeField] AudioClip destroySound;

        LaunchPad padData;
        AudioSource audioSource;
        float spawnTime;

        public LaunchPad PadData => padData;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }

        public void Initialize(LaunchPad pad)
        {
            padData = pad;
            spawnTime = Time.time;

            // Set team material
            if (padRenderer != null)
            {
                padRenderer.material = pad.TeamId == 0 ? friendlyMaterial : enemyMaterial;
            }

            // Show arming effect
            if (armingEffect != null)
                armingEffect.SetActive(true);
            if (armedEffect != null)
                armedEffect.SetActive(false);

            // Play deploy sound
            if (deploySound != null)
                audioSource.PlayOneShot(deploySound);
        }

        void Update()
        {
            if (padData == null) return;

            // State transitions (arming -> armed -> destroyed/expired) are owned by
            // LaunchPad.Tick(), driven from PlayerPadManager.Tick() on the owner's
            // PlayerController; this MonoBehaviour only mirrors that state visually.

            // Update visuals based on state
            UpdateVisuals();

            // Check for expiry
            if (padData.State == PadState.Expired || padData.State == PadState.Destroyed)
            {
                DestroyPad();
            }
        }

        void UpdateVisuals()
        {
            if (padData == null) return;

            bool isArming = padData.State == PadState.Arming;
            bool isArmed = padData.State == PadState.Armed;

            if (armingEffect != null)
                armingEffect.SetActive(isArming);
            if (armedEffect != null)
                armedEffect.SetActive(isArmed);

            // Pulse when armed
            if (isArmed && armedEffect != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.1f;
                armedEffect.transform.localScale = Vector3.one * pulse;
            }
        }

        public void NotifyActivated()
        {
            if (armSound != null)
                audioSource.PlayOneShot(armSound);
            if (launchSound != null)
                audioSource.PlayOneShot(launchSound);
        }

        public void ApplyDamage(float amount)
        {
            padData?.ApplyDamage(amount);
            if (padData?.State == PadState.Destroyed)
            {
                if (destroySound != null)
                    audioSource.PlayOneShot(destroySound);
                if (destroyEffect != null)
                {
                    Instantiate(destroyEffect, transform.position, transform.rotation);
                }
            }
        }

        void DestroyPad()
        {
            if (armedEffect != null)
                armedEffect.SetActive(false);
            if (armingEffect != null)
                armingEffect.SetActive(false);

            Destroy(gameObject, 0.5f); // allow effects to play
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if player stands on pad
            var player = other.GetComponent<PlayerController>();
            if (player != null && padData?.State == PadState.Armed)
            {
                // Player can launch by jumping - handled in PlayerController
            }
        }
    }
}