using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Updraft.Config;

namespace Updraft.Network
{
    /// <summary>
    /// Server-owned launch pad (spec section 6): arms after 0.75 s, lasts 18 s,
    /// 60 HP, destroyable only by enemies, usable by anyone, outlives its owner.
    /// </summary>
    public class NetPad : NetworkBehaviour, Updraft.UI.IPingablePad
    {
        public static readonly List<NetPad> All = new List<NetPad>();

        [Networked] public int OwnerSlot { get; set; }
        [Networked] public int Team { get; set; }
        [Networked] public Vector3 Forward { get; set; }
        [Networked] public float HitPoints { get; set; }
        [Networked] public TickTimer ArmTimer { get; set; }
        [Networked] public TickTimer LifeTimer { get; set; }
        [Networked] public NetworkBool Removed { get; set; }

        PadConfig P => NetConfigs.Pad;

        public bool IsArmed => !Removed && ArmTimer.Expired(Runner) && !LifeTimer.Expired(Runner);
        public bool IsActive => !Removed && !LifeTimer.Expired(Runner);
        public Vector3 LaunchVelocity => Forward.normalized * P.launchHorizontalVelocity + Vector3.up * P.launchUpwardVelocity;

        bool Updraft.UI.IPingablePad.Usable => IsArmed;
        Vector3 Updraft.UI.IPingablePad.PadPosition => transform.position;

        public void Init(int ownerSlot, int team, Vector3 forward)
        {
            OwnerSlot = ownerSlot;
            Team = team;
            Forward = forward;
            HitPoints = P.health;
            ArmTimer = TickTimer.CreateFromSeconds(Runner, P.armingSeconds);
            LifeTimer = TickTimer.CreateFromSeconds(Runner, P.lifetimeSeconds);
        }

        public override void Spawned()
        {
            All.Add(this);
            NetPadVisual.Attach(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => All.Remove(this);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (Removed || LifeTimer.Expired(Runner)) Runner.Despawn(Object);
        }

        public void NotifyUsed(NetPlayer user) => NetMatch.For(Runner)?.RecordPadUse(this, user);

        /// <summary>Enemy fire only (spec: friendly fire can't destroy pads).</summary>
        public void ServerDamage(float amount, int attackerTeam)
        {
            if (Removed || attackerTeam == Team) return;
            HitPoints -= amount;
            if (HitPoints <= 0f) { Removed = true; NetMatch.For(Runner)?.RecordPadDestroyed(this); }
        }

        public static NetPad FindUsableUnder(NetworkRunner runner, Vector3 feet, PadConfig config)
        {
            foreach (var pad in All)
            {
                if (pad.Runner != runner || !pad.IsArmed) continue;
                Vector3 d = pad.transform.position - feet;
                if (new Vector2(d.x, d.z).magnitude <= config.diameter * 0.5f && Mathf.Abs(d.y) < 0.5f) return pad;
            }
            return null;
        }

        public static int ActiveCountExcludingOwner(NetworkRunner runner, int ownerSlot)
        {
            int count = 0;
            foreach (var pad in All)
                if (pad.Runner == runner && pad.IsActive && pad.OwnerSlot != ownerSlot) count++;
            return count;
        }

        public static NetPad ActiveOwnedBy(NetworkRunner runner, int ownerSlot)
        {
            foreach (var pad in All)
                if (pad.Runner == runner && pad.IsActive && pad.OwnerSlot == ownerSlot) return pad;
            return null;
        }
    }
}
