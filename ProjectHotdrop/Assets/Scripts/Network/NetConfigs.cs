using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Network
{
    /// <summary>The same authoritative config assets the offline game uses (spec section 32).</summary>
    public static class NetConfigs
    {
        static MovementConfig movement;
        static PadConfig pad;
        static MapConfig map;
        static MatchRulesConfig rules;
        static ProgressionConfig progression;
        static WeaponDefinition[] weapons;

        public static MovementConfig Movement => movement ??= Resources.Load<MovementConfig>("Config/MovementConfig");
        public static PadConfig Pad => pad ??= Resources.Load<PadConfig>("Config/PadConfig");
        public static MapConfig Map => map ??= Resources.Load<MapConfig>("Config/MapConfig");
        public static MatchRulesConfig Rules => rules ??= Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
        // Spec 18: AFK windows live in config, never hardcoded on the server.
        public static ProgressionConfig Progression => progression ??= Resources.Load<ProgressionConfig>("Config/ProgressionConfig");

        public static WeaponDefinition[] Weapons => weapons ??= new[]
        {
            Resources.Load<WeaponDefinition>("Config/Weapon_ARC"),
            Resources.Load<WeaponDefinition>("Config/Weapon_ZIP"),
            Resources.Load<WeaponDefinition>("Config/Weapon_BRICK"),
        };
    }
}


