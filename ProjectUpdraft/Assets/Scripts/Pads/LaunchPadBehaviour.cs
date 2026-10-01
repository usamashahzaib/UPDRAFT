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
    public class LaunchPadBehaviour : MonoBehaviour, Updraft.UI.IPingablePad
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
        bool ownerTicks;
        MaterialPropertyBlock colorBlock;

        /// <summary>Every pad object in the scene (alive or finishing its removal effect).</summary>
        public static readonly System.Collections.Generic.List<LaunchPadBehaviour> All = new System.Collections.Generic.List<LaunchPadBehaviour>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        /// <summary>Armed or arming pads not owned by <paramref name="ownerId"/> (the 6-pad arena cap).</summary>
        public static int ActiveCountExcludingOwner(int ownerId)
        {
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var pad = All[i].padData;
                if (pad != null && pad.OwnerId != ownerId && (pad.State == PadState.Arming || pad.State == PadState.Armed)) count++;
            }
            return count;
        }

        /// <summary>Removes every pad immediately (match teardown / rematch).</summary>
        public static void DestroyAll()
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var pad = All[i];
                All.RemoveAt(i);
                if (pad != null) Destroy(pad.gameObject);
            }
        }

        public LaunchPad PadData => padData;
        bool Updraft.UI.IPingablePad.Usable => padData != null && padData.CanBeUsed;
        Vector3 Updraft.UI.IPingablePad.PadPosition => transform.position;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.priority = 60;
            audioSource.minDistance = 2; audioSource.maxDistance = 45;
            if (deploySound == null) deploySound = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.PadDeploy);
            if (armSound == null) armSound = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.PadArm);
            if (launchSound == null) launchSound = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.PadLaunch);
            if (destroySound == null) destroySound = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.PadDestroy);
        }

        public void Initialize(LaunchPad pad, bool tickedByOwner = false)
        {
            ownerTicks = tickedByOwner;
            padData = pad;
            spawnTime = Time.time;
            var arrowObject = new GameObject("Launch direction");
            arrowObject.transform.SetParent(transform, false);
            var arrow = arrowObject.AddComponent<LineRenderer>();
            arrow.sharedMaterial = Resources.Load<Material>("Materials/Objective");
            arrow.startWidth = arrow.endWidth = 0.06f;
            arrow.positionCount = 5;
            arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Vector3 direction = pad.ForwardDirection.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            Vector3 origin = pad.Position + Vector3.up * 0.23f;
            arrow.SetPosition(0, origin - direction * 0.35f);
            arrow.SetPosition(1, origin + direction * 0.35f);
            arrow.SetPosition(2, origin + direction * 0.1f + right * 0.2f);
            arrow.SetPosition(3, origin + direction * 0.35f);
            arrow.SetPosition(4, origin + direction * 0.1f - right * 0.2f);
            if (padRenderer == null) padRenderer = GetComponent<Renderer>();
            if (padRenderer == null)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Pad disc"; disc.layer = LayerMask.NameToLayer("Pad");
                disc.transform.SetParent(transform, false);
                disc.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
                padRenderer = disc.GetComponent<Renderer>();
            }
            if (friendlyMaterial == null) friendlyMaterial = Resources.Load<Material>("Materials/Friendly");
            if (enemyMaterial == null) enemyMaterial = Resources.Load<Material>("Materials/Enemy");

            // Unity's cylinder primitive has a CapsuleCollider. Nonuniform
            // scaling leaves a tall spherical collider around a thin disc,
            // putting the activation ray's origin inside it (no ray hit).
            foreach (var capsule in GetComponentsInChildren<CapsuleCollider>())
            {
                capsule.enabled = false;
                var box = capsule.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1, 2, 1);
                Destroy(capsule);
            }

            // Team colour is relative to the viewer: your team's pads are always
            // friendly-coloured, whichever side you play on.
            bool friendly = IsFriendlyToViewer(pad.TeamId);
            if (padRenderer != null)
            {
                // Shared material + property block: no per-pad material instance to leak.
                padRenderer.sharedMaterial = friendly ? friendlyMaterial : enemyMaterial;
                if (padRenderer.sharedMaterial != null) baseColor = padRenderer.sharedMaterial.color;
                colorBlock = new MaterialPropertyBlock();
            }
            BuildTeamSymbol(pad, friendly);

            // Show arming effect
            if (armingEffect != null)
                armingEffect.SetActive(true);
            if (armedEffect != null)
                armedEffect.SetActive(false);

            if (deploySound != null) audioSource.PlayOneShot(deploySound, Updraft.Core.GameAudio.SfxVolume); // QA-022
            audioSource.PlayOneShot(Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.PadCharging), 0.7f * Updraft.Core.GameAudio.SfxVolume);
        }

        Color baseColor = Color.white;
        bool reportedDestroyed;
        PadState lastState = PadState.Arming;

        public static bool IsFriendlyToViewer(int teamId)
        {
            var local = Updraft.Match.MatchView.Current?.LocalParticipant;
            return teamId == (local != null ? local.TeamId : 0);
        }

        // Colour is never the only signal: friendly pads carry a ring, enemy pads a
        // hazard cross, both visible from above and at an angle.
        void BuildTeamSymbol(LaunchPad pad, bool friendly)
        {
            var symbol = new GameObject(friendly ? "Friendly ring" : "Enemy cross").AddComponent<LineRenderer>();
            symbol.transform.SetParent(transform, false);
            symbol.sharedMaterial = Resources.Load<Material>(friendly ? "Materials/Friendly" : "Materials/Enemy");
            symbol.startWidth = symbol.endWidth = 0.07f;
            symbol.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            symbol.useWorldSpace = true;
            Vector3 c = pad.Position + Vector3.up * 0.2f;
            float r = 0.62f;
            if (friendly)
            {
                symbol.loop = true;
                symbol.positionCount = 24;
                for (int i = 0; i < 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    symbol.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r);
                }
            }
            else
            {
                float d = r * 0.7f;
                symbol.positionCount = 5;
                symbol.SetPosition(0, c + new Vector3(-d, 0, -d));
                symbol.SetPosition(1, c + new Vector3(d, 0, d));
                symbol.SetPosition(2, c);
                symbol.SetPosition(3, c + new Vector3(-d, 0, d));
                symbol.SetPosition(4, c + new Vector3(d, 0, -d));
            }
        }

        void Update()
        {
            if (padData == null) return;
            if (!ownerTicks) padData.Tick(Time.deltaTime);

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
            if (isArmed && lastState == PadState.Arming && armSound != null) audioSource.PlayOneShot(armSound, Updraft.Core.GameAudio.SfxVolume);
            lastState = padData.State;
            // Arming pads read as dim and blinking; armed pads at full colour.
            if (padRenderer != null && colorBlock != null)
            {
                Color color = isArming ? baseColor * (0.45f + 0.25f * Mathf.PingPong(Time.time * 6f, 1f)) : baseColor;
                colorBlock.SetColor("_BaseColor", color);
                colorBlock.SetColor("_Color", color);
                padRenderer.SetPropertyBlock(colorBlock);
            }

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
            if (launchSound != null)
                audioSource.PlayOneShot(launchSound, Updraft.Core.GameAudio.SfxVolume);
        }

        public void ApplyDamage(float amount)
        {
            padData?.ApplyDamage(amount);
            if (padData?.State == PadState.Destroyed && !reportedDestroyed)
            {
                reportedDestroyed = true;
                Updraft.Core.Telemetry.TrackOfflineMatchFact("pad_destroyed", ("padTeam", padData.TeamId),
                    ("ageSeconds", Time.time - spawnTime));
                if (destroySound != null)
                    audioSource.PlayOneShot(destroySound, Updraft.Core.GameAudio.SfxVolume);
                if (destroyEffect != null)
                {
                    Instantiate(destroyEffect, transform.position, transform.rotation);
                }
            }
        }

        bool removing;

        void DestroyPad()
        {
            if (removing) return;
            removing = true;
            if (armedEffect != null)
                armedEffect.SetActive(false);
            if (armingEffect != null)
                armingEffect.SetActive(false);
            // Gone for gameplay at once: nothing can stand on, shoot or overlap a removed pad.
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

            Destroy(gameObject, 0.5f); // allow effects to play
        }
    }
}
