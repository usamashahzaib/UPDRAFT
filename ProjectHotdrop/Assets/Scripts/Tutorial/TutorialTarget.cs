using UnityEngine;

namespace Hotdrop.Tutorial
{
    /// <summary>
    /// Simple stationary target for tutorial shooting step.
    /// </summary>
    public class TutorialTarget : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] public float health = 100f;
        [SerializeField] GameObject destroyEffect;
        [SerializeField] AudioClip hitSound;
        [SerializeField] AudioClip destroySound;

        AudioSource audioSource;

        public System.Action OnDestroyed;
        public System.Func<bool> CanTakeDamage;
        bool destroyed;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void TakeDamage(float amount)
        {
            if (destroyed || amount <= 0 || (CanTakeDamage != null && !CanTakeDamage())) return;
            health -= amount;

            if (hitSound != null)
                audioSource.PlayOneShot(hitSound);

            if (health <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            if (destroyed) return;
            destroyed = true;
            if (destroySound != null)
                audioSource.PlayOneShot(destroySound);

            if (destroyEffect != null)
                Instantiate(destroyEffect, transform.position, transform.rotation);

            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Also take damage from collisions (bullets would use raycast)
        }
    }
}
