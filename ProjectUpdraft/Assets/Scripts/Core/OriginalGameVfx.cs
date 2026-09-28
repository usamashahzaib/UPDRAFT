using UnityEngine;

namespace Updraft.Core
{
    public enum VfxType
    {
        MuzzleFlash,
        HitSparks,
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
        public static GameObject Create(VfxType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var go = new GameObject("VFX_" + type);
            go.transform.position = position;
            go.transform.rotation = rotation;
            if (parent != null) go.transform.SetParent(parent, true);

            var ps = go.AddComponent<ParticleSystem>();
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

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = new Material(Shader.Find("Sprites/Default"));

            ps.Play();
            return go;
        }
    }
}
