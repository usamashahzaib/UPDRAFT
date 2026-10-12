using UnityEngine;
using Hotdrop.Match;

namespace Hotdrop.Network
{
    /// <summary>
    /// Presentation-only avatar body (renderers, no colliders). Hit detection uses
    /// the prefab's Fusion hitboxes; cosmetics can never change them (spec 5).
    /// </summary>
    public static class NetAvatarVisual
    {
        // Spec 15 (Amendment v2.1): one hitbox set for both body rigs - never a per-rig name.
        public const string HeadHitboxName = Hotdrop.Player.BodyRigs.HeadHitboxName;
        public static readonly Vector3 MuzzleLocalPosition = new Vector3(0.28f, 1.15f, 0.65f);

        public static void Attach(NetPlayer player)
        {
            if (player.transform.Find("Visual") != null) return;
            if (Application.isBatchMode && player.Runner != null && player.Runner.IsServer) return; // headless server draws nothing
            var root = new GameObject("Visual").transform;
            root.SetParent(player.transform, false);
            bool friendly = MatchView.IsFriendly(player.Team);
            var material = Resources.Load<Material>(friendly ? "Materials/Friendly" : "Materials/Enemy");
            // Amendment 2: the same procedural humanoid the offline slice uses, with the
            // networked motion component so remote players are not frozen statues.
            var motion = root.gameObject.AddComponent<Hotdrop.Player.SportAvatarAnimation>();
            var parts = Hotdrop.Player.AvatarRigVisual.Build(root.gameObject, Hotdrop.Player.BodyRigs.Default,
                material, Resources.Load<Material>("Materials/Equipment"), includeGun: true);
            Hotdrop.Player.AvatarRigVisual.Wire(motion, parts);
            // QA-023: viewer-relative ally chevron + chest band / enemy diamond + shoulder
            // spikes so team never relies on color alone; collider-free, under "Visual".
            Hotdrop.Player.TeamCueVisual.Apply(root, friendly, material);
        }

        /// <summary>Amendment 3: rebuild the humanoid in the player's replicated body rig.
        /// Capsule, hitboxes and animation set are unchanged - appearance only.</summary>
        public static void ApplyRig(Transform visualRoot, Hotdrop.Player.BodyRig rig, bool friendly)
        {
            if (visualRoot == null) return;
            var material = Resources.Load<Material>(friendly ? "Materials/Friendly" : "Materials/Enemy");
            var motion = visualRoot.GetComponent<Hotdrop.Player.SportAvatarAnimation>();
            var parts = Hotdrop.Player.AvatarRigVisual.Rebuild(visualRoot.gameObject, rig, material,
                Resources.Load<Material>("Materials/Equipment"), includeGun: true);
            if (motion != null) Hotdrop.Player.AvatarRigVisual.Wire(motion, parts);
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    /// <summary>Pad disc, launch arrow and team symbol (friendly ring / enemy cross), relative to the viewer.</summary>
    public static class NetPadVisual
    {
        public static void Attach(NetPad pad)
        {
            if (Application.isBatchMode && pad.Runner != null && pad.Runner.IsServer) return;
            bool friendly = MatchView.IsFriendly(pad.Team);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(disc.GetComponent<Collider>());
            disc.name = "Disc";
            disc.GetComponent<MeshFilter>().sharedMesh = Hotdrop.Pads.LaunchPadBehaviour.SharedVisualMesh;
            disc.transform.SetParent(pad.transform, false);
            disc.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            disc.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>(friendly ? "Materials/Friendly" : "Materials/Enemy");

            Vector3 origin = pad.transform.position + Vector3.up * 0.12f;
            Vector3 dir = pad.Forward.sqrMagnitude > 0.01f ? pad.Forward.normalized : pad.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            Line(pad.transform, "Arrow", "Materials/Objective", origin - dir * 0.35f, origin + dir * 0.35f, origin + dir * 0.1f + right * 0.2f, origin + dir * 0.35f, origin + dir * 0.1f - right * 0.2f);
            if (friendly)
            {
                var points = new Vector3[25];
                for (int i = 0; i < 25; i++) { float a = i / 24f * Mathf.PI * 2f; points[i] = origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.62f; }
                Line(pad.transform, "Ring", "Materials/Friendly", points);
            }
            else
            {
                float d = 0.43f;
                Line(pad.transform, "Cross", "Materials/Enemy", origin + new Vector3(-d, 0, -d), origin + new Vector3(d, 0, d), origin,
                    origin + new Vector3(-d, 0, d), origin + new Vector3(d, 0, -d));
            }

            // Pads are spawned outside the SwitchyardGenerator hierarchy, so its
            // profile pass cannot reach them. Keep their viewer-relative team cues
            // aligned with the selected A/B/C palette, including live profile changes.
            var profileTint = pad.GetComponent<Hotdrop.Player.TeamProfileTint>();
            if (profileTint == null) profileTint = pad.gameObject.AddComponent<Hotdrop.Player.TeamProfileTint>();
            profileTint.SetFriendly(friendly);
        }

        static void Line(Transform parent, string name, string material, params Vector3[] points)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, true);
            line.useWorldSpace = true;
            line.sharedMaterial = Resources.Load<Material>(material);
            line.startWidth = line.endWidth = 0.07f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.positionCount = points.Length;
            line.SetPositions(points);
        }
    }
}



