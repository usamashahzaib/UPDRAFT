using UnityEngine;

namespace Hotdrop.Player
{
    /// <summary>Spec 15 (Amendment v2.1): the two selectable body rigs. Cosmetic identity only.</summary>
    public enum BodyRig
    {
        Male = 0,
        Female = 1,
    }

    /// <summary>
    /// Spec 15 (Amendment v2.1): one capsule, one hitbox set and one animation set shared by
    /// every body rig. Both rigs resolve to the same instance; a rig never owns gameplay
    /// geometry of its own. The values mirror the shipped MovementConfig and the generated
    /// player prefabs, and BodyRigParityTests ties all three together so Gate G art
    /// replacement cannot drift them.
    /// </summary>
    public sealed class BodyRigGameplayProfile
    {
        internal BodyRigGameplayProfile()
        {
            CapsuleHeight = 1.8f;
            CapsuleRadius = 0.35f;
            CapsuleCenter = new Vector3(0f, CapsuleHeight * 0.5f, 0f);

            HitboxSetId = "avatar_hitboxes_v1";
            BodyHitboxName = BodyRigs.BodyHitboxName;
            HeadHitboxName = BodyRigs.HeadHitboxName;
            BodyHitboxOffset = CapsuleCenter;
            BodyHitboxExtents = new Vector3(0.33f, 0.85f, 0.33f);
            HeadHitboxOffset = new Vector3(0f, 1.62f, 0f);
            HeadHitboxRadius = 0.16f;

            AnimationSetId = "avatar_motion_v1";
        }

        /// <summary>Spec 5: 1.8 m high, 0.35 m radius - identical for Male and Female.</summary>
        public float CapsuleHeight { get; }
        public float CapsuleRadius { get; }
        public Vector3 CapsuleCenter { get; }

        /// <summary>Spec 7/21: the Fusion lag-compensated hit geometry both rigs use.</summary>
        public string HitboxSetId { get; }
        public string BodyHitboxName { get; }
        public string HeadHitboxName { get; }
        public Vector3 BodyHitboxOffset { get; }
        public Vector3 BodyHitboxExtents { get; }
        public Vector3 HeadHitboxOffset { get; }
        public float HeadHitboxRadius { get; }

        /// <summary>Spec 15: one animation set, retargeted across both rigs.</summary>
        public string AnimationSetId { get; }
    }

    /// <summary>
    /// Spec 15 (Amendment v2.1): the rig catalog and the one gameplay profile every rig shares.
    /// Ids are stable strings for preference storage and appearance screens; the default is
    /// used whenever no valid stored choice exists.
    /// </summary>
    public static class BodyRigs
    {
        public const BodyRig Default = BodyRig.Male;
        public const string MaleId = "rig_male";
        public const string FemaleId = "rig_female";
        public const string BodyHitboxName = "Body";
        public const string HeadHitboxName = "Head";

        public static readonly BodyRig[] All = { BodyRig.Male, BodyRig.Female };

        /// <summary>The single profile instance both rigs resolve to - never a per-rig copy.</summary>
        public static readonly BodyRigGameplayProfile SharedGameplay = new BodyRigGameplayProfile();

        /// <summary>Spec 15: cosmetic-only. Every rig returns the same shared gameplay profile.</summary>
        public static BodyRigGameplayProfile Gameplay(BodyRig rig) => SharedGameplay;

        public static string Id(BodyRig rig) => rig == BodyRig.Female ? FemaleId : MaleId;

        public static string DisplayName(BodyRig rig) => rig == BodyRig.Female ? "Female" : "Male";

        public static bool TryParse(string id, out BodyRig rig)
        {
            if (id == MaleId) { rig = BodyRig.Male; return true; }
            if (id == FemaleId) { rig = BodyRig.Female; return true; }
            rig = Default;
            return false;
        }

        /// <summary>Stored rig ids are untrusted: unknown or missing falls back to the default.</summary>
        public static BodyRig FromStoredId(string id) => TryParse(id, out var rig) ? rig : Default;
    }
}

