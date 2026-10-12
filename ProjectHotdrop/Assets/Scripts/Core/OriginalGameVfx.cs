using UnityEngine;
using System.Collections.Generic;

namespace Hotdrop.Core
{
    public enum VfxType
    {
        MuzzleFlash,
        HitSparks,
        ConcreteImpact,
        PadArming,
        PadArmed,
        PadLaunch,
        PadLanding,
        Elimination
    }

    /// <summary>
    /// Procedurally generated, spec-compliant original visual effects (VFX).
    /// 100% project-original C# code using procedural particle systems, colors,
    /// and light bursts. Zero third-party asset dependencies (spec §15, §16).
    /// </summary>
    public static class OriginalGameVfx
    {
        static Hotdrop.Config.GraphicsConfig graphicsConfig;

        public static GameObject Create(VfxType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var go = new GameObject("VFX_" + type);
            go.transform.position = position;
            go.transform.rotation = rotation;
            if (parent != null) go.transform.SetParent(parent, true);

            var ps = go.AddComponent<ParticleSystem>();
            // A new system plays on awake; configuring duration while it plays asserts.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var colorOverLifetime = ps.colorOverLifetime;
            var sizeOverLifetime = ps.sizeOverLifetime;

            switch (type)
            {
                case VfxType.MuzzleFlash:
                    main.duration = 0.05f;
                    main.loop = false;
                    main.startLifetime = 0.08f;
                    main.startSpeed = 8f;
                    main.startSize = 0.25f;
                    main.startColor = new Color(1f, 0.85f, 0.3f, 1f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 15f;
                    break;

                case VfxType.HitSparks:
                    main.duration = 0.1f;
                    main.loop = false;
                    main.startLifetime = 0.15f;
                    main.startSpeed = 12f;
                    main.startSize = 0.15f;
                    main.startColor = new Color(1f, 0.6f, 0.1f, 1f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
                    shape.shapeType = ParticleSystemShapeType.Hemisphere;
                    shape.radius = 0.1f;
                    break;

                case VfxType.ConcreteImpact:
                    main.duration = 0.08f;
                    main.loop = false;
                    main.startLifetime = 0.2f;
                    main.startSpeed = 3.5f;
                    main.startSize = 0.07f;
                    main.startColor = new Color(0.72f, 0.67f, 0.57f, 0.8f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 28f;
                    shape.radius = 0.035f;
                    break;

                case VfxType.PadArming:
                    main.duration = 0.3f;
                    main.loop = false;
                    main.startLifetime = 0.4f;
                    main.startSpeed = 2f;
                    main.startSize = 0.3f;
                    main.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.rateOverTime = 30;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.8f;
                    break;

                case VfxType.PadArmed:
                    main.duration = 1f;
                    main.loop = true;
                    main.startLifetime = 0.6f;
                    main.startSpeed = 0.5f;
                    main.startSize = 0.2f;
                    main.startColor = new Color(0.1f, 0.9f, 0.4f, 0.7f);
                    emission.rateOverTime = 15;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.75f;
                    break;

                case VfxType.PadLaunch:
                    main.duration = 0.2f;
                    main.loop = false;
                    main.startLifetime = 0.3f;
                    main.startSpeed = 15f;
                    main.startSize = 0.4f;
                    main.startColor = new Color(0.3f, 1f, 0.5f, 0.9f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 25f;
                    break;

                case VfxType.PadLanding:
                    main.duration = 0.15f;
                    main.loop = false;
                    main.startLifetime = 0.25f;
                    main.startSpeed = 6f;
                    main.startSize = 0.35f;
                    main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.6f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
                    shape.shapeType = ParticleSystemShapeType.Hemisphere;
                    shape.radius = 0.5f;
                    break;

                case VfxType.Elimination:
                    main.duration = 0.4f;
                    main.loop = false;
                    main.startLifetime = 0.5f;
                    main.startSpeed = 5f;
                    main.startSize = 0.45f;
                    main.startColor = new Color(1f, 0.2f, 0.2f, 1f);
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.6f;
                    break;
            }

            ApplyPerformanceBudget(ps);

            // Balanced/Cinematic render HDR with bloom: push the emissive colours past the bloom
            // threshold so flashes glow. Performance has no HDR, so colours stay as authored.
            bool rich = Hotdrop.Map.SwitchyardAtmosphere.Quality > 0;
            float boost = rich ? HdrBoost(type) : 1f;
            var start = main.startColor.color;
            main.startColor = new Color(start.r * boost, start.g * boost, start.b * boost, start.a);

            // One shared additive soft-particle material (07_WORLD_ART); it used to be a new
            // Sprites/Default material per effect, which leaked and drew hard-edged squares.
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = SharedMaterial();
            if (rich) VfxFlash.Attach(go, type, start);

            ps.Play();
            return go;
        }

        static Material sharedMaterial;

        static Material SharedMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            sharedMaterial = Resources.Load<Material>("Materials/VfxAdditive");
            if (sharedMaterial == null) sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "VFX fallback" };
            return sharedMaterial;
        }

        static float HdrBoost(VfxType type) => type switch
        {
            VfxType.MuzzleFlash => 4f,
            VfxType.HitSparks => 3f,
            VfxType.PadLaunch => 3.5f,
            VfxType.Elimination => 3f,
            VfxType.PadLanding => 2f,
            _ => 1.5f,
        };

        static void ApplyPerformanceBudget(ParticleSystem system)
        {
            if (system == null || PlayerPrefs.GetInt(OfflinePauseMenu.GraphicsQualityKey, 0) != 0) return;
            if (graphicsConfig == null) graphicsConfig = Resources.Load<Hotdrop.Config.GraphicsConfig>("Config/GraphicsConfig");

            var main = system.main;
            int maxParticles = graphicsConfig != null ? graphicsConfig.performanceMaximumParticlesPerEffect : 256;
            float lifetimeScale = graphicsConfig != null ? graphicsConfig.performanceParticleLifetimeScale : 0.75f;
            float sizeScale = graphicsConfig != null ? graphicsConfig.performanceParticleSizeScale : 0.9f;
            main.maxParticles = Mathf.Clamp(maxParticles, 8, 2048);
            main.startLifetimeMultiplier *= Mathf.Clamp(lifetimeScale, 0.25f, 1f);
            main.startSizeMultiplier *= Mathf.Clamp(sizeScale, 0.25f, 1f);
        }
    }

    /// <summary>A short point-light pulse under an effect (Balanced/Cinematic only), fading out.</summary>
    public sealed class VfxFlash : MonoBehaviour
    {
        Light flash;
        float peak, life, age;

        public static void Attach(GameObject host, VfxType type, Color colour)
        {
            float range, intensity, seconds;
            switch (type)
            {
                case VfxType.MuzzleFlash: range = 4f; intensity = 3f; seconds = .06f; break;
                case VfxType.PadLaunch: range = 7f; intensity = 4f; seconds = .25f; break;
                case VfxType.Elimination: range = 6f; intensity = 4f; seconds = .3f; break;
                default: return;
            }
            var pulse = host.AddComponent<VfxFlash>();
            pulse.flash = host.AddComponent<Light>();
            pulse.flash.type = LightType.Point;
            pulse.flash.range = range;
            pulse.flash.color = colour;
            pulse.flash.shadows = LightShadows.None;
            pulse.flash.intensity = pulse.peak = intensity;
            pulse.life = seconds;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            if (flash != null) flash.intensity = peak * (1f - t) * (1f - t);
            if (t >= 1f) { if (flash != null) Destroy(flash); Destroy(this); }
        }
    }

    /// <summary>Local-only surface response for a predicted or offline pellet impact.</summary>
    public static class ShotImpactVfx
    {
        const int MaximumMarks = 32;
        const float MarkLifetimeSeconds = 12f;
        static readonly int GroundLayer = LayerMask.NameToLayer("Ground");
        static readonly int CoverLayer = LayerMask.NameToLayer("Cover");
        static readonly int SpawnBarrierLayer = LayerMask.NameToLayer("SpawnBarrier");
        static readonly int PadLayer = LayerMask.NameToLayer("Pad");
        static readonly Queue<GameObject> marks = new Queue<GameObject>(MaximumMarks);
        static Material concreteMarkMaterial, metalMarkMaterial;
        static Texture2D concreteMarkTexture, metalMarkTexture;

        public static bool TryShow(Collider collider, Vector3 point, Vector3 normal)
        {
            if (collider == null || (Application.isBatchMode && SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null))
                return false;

            VfxType effect;
            int layer = collider.gameObject.layer;
            if (layer == GroundLayer)
                effect = VfxType.ConcreteImpact;
            else if (layer == CoverLayer || layer == SpawnBarrierLayer || layer == PadLayer)
                effect = VfxType.HitSparks;
            else
                return false;

            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
            Vector3 surfaceNormal = normal.normalized;
            Vector3 surfacePoint = point + surfaceNormal * 0.015f;
            OriginalGameVfx.Create(effect, surfacePoint, Quaternion.FromToRotation(Vector3.up, surfaceNormal));
            CreateSurfaceMark(effect, surfacePoint, surfaceNormal);
            return true;
        }

        static void CreateSurfaceMark(VfxType type, Vector3 point, Vector3 normal)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            bool concrete = type == VfxType.ConcreteImpact;
            if (concrete)
            {
                if (concreteMarkMaterial == null)
                    concreteMarkMaterial = CreateMarkMaterial("Concrete chip", ref concreteMarkTexture, concrete: true, new Color(.32f, .29f, .24f, .78f));
            }
            else if (metalMarkMaterial == null)
                metalMarkMaterial = CreateMarkMaterial("Cladding scratch", ref metalMarkTexture, concrete: false, new Color(.18f, .2f, .21f, .85f));

            while (marks.Count >= MaximumMarks)
            {
                var oldest = marks.Dequeue();
                if (oldest != null) UnityEngine.Object.Destroy(oldest);
            }

            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = concrete ? "Impact mark — concrete" : "Impact mark — cladding";
            var collider = mark.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
            mark.transform.position = point + normal * 0.004f;
            float variant = MarkVariant(point);
            mark.transform.rotation = Quaternion.AngleAxis(variant * 360f, normal) *
                                      Quaternion.FromToRotation(Vector3.forward, normal);
            float size = Mathf.Lerp(0.14f, 0.24f, variant);
            float aspect = Mathf.Lerp(.8f, 1.15f, Mathf.Repeat(variant * 3.17f, 1f));
            mark.transform.localScale = concrete ? new Vector3(size, size * aspect, 1f)
                                                 : new Vector3(size * 1.35f, size * .36f, 1f);
            mark.GetComponent<Renderer>().sharedMaterial = concrete ? concreteMarkMaterial : metalMarkMaterial;
            marks.Enqueue(mark);
            UnityEngine.Object.Destroy(mark, MarkLifetimeSeconds);
        }

        static float MarkVariant(Vector3 point)
        {
            float value = Mathf.Sin(point.x * 12.9898f + point.y * 78.233f + point.z * 37.719f + Time.frameCount * 0.618f) * 43758.5453f;
            return Mathf.Repeat(Mathf.Abs(value), 1f);
        }

        static Material CreateMarkMaterial(string name, ref Texture2D texture, bool concrete, Color tint)
        {
            texture = CreateMarkTexture(name + " shape", concrete);
            var material = new Material(Shader.Find("Sprites/Default")) { name = name };
            material.mainTexture = texture;
            material.color = tint;
            return material;
        }

        static Texture2D CreateMarkTexture(string name, bool concrete)
        {
            const int resolution = 64;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float px = (x + .5f) / resolution * 2f - 1f;
                    float py = (y + .5f) / resolution * 2f - 1f;
                    float hash = Mathf.Abs(Mathf.Sin((x + 13.7f) * 127.1f + (y + 4.3f) * 311.7f) * 43758.5453f) % 1f;
                    float alpha;
                    if (concrete)
                    {
                        float angle = Mathf.Atan2(py, px);
                        float radius = Mathf.Sqrt(px * px + py * py);
                        float edge = .72f + .12f * Mathf.Sin(angle * 7f + .4f) + .07f * Mathf.Sin(angle * 13f + 1.2f);
                        float chipped = radius < edge && radius > .2f + hash * .13f ? 1f : 0f;
                        alpha = chipped * Mathf.SmoothStep(0f, .14f, edge - radius) * (.5f + hash * .5f);
                    }
                    else
                    {
                        float scratchWidth = .11f + hash * .1f;
                        float inside = Mathf.Abs(py + .06f * Mathf.Sin(px * 9f)) < scratchWidth && Mathf.Abs(px) < .78f ? 1f : 0f;
                        alpha = inside * Mathf.SmoothStep(.78f, .48f, Mathf.Abs(px)) * (.55f + hash * .45f);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            texture.Apply(false, true);
            return texture;
        }
    }
}
