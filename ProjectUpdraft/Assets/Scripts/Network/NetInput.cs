using Fusion;
using UnityEngine;

namespace Updraft.Network
{
    public enum NetButton { Fire = 0, Jump = 1, Pad = 2, Reload = 3, Ping = 4 }

    /// <summary>
    /// Spec section 21: clients send input only. The server validates and owns
    /// every outcome; nothing here (damage, score, position) is trusted.
    /// Look is absolute yaw/pitch so a dropped input packet never accumulates error.
    /// </summary>
    public struct NetInput : INetworkInput
    {
        public Vector2 Move;
        public float Yaw;
        public float Pitch;
        // World point selected by the client camera. The server still traces the
        // shot from its own muzzle and resolves all hits; this preserves TPP
        // camera-to-muzzle parallax without trusting a client hit result.
        public Vector3 AimPoint;
        public NetworkButtons Buttons;
        public byte WeaponChoice; // 0 = no change, 1..3 = ARC/ZIP/BRICK (only honoured while dead/preparing)

        public bool IsDown(NetButton b) => Buttons.IsSet((int)b);
        public void Set(NetButton b, bool down) => Buttons.Set((int)b, down);

        /// <summary>Server-side validation: clamp everything a modified client could inflate.</summary>
        public void Sanitize()
        {
            if (float.IsNaN(Move.x) || float.IsNaN(Move.y)) Move = Vector2.zero;
            Move = Vector2.ClampMagnitude(Move, 1f);
            if (float.IsNaN(Yaw) || float.IsInfinity(Yaw)) Yaw = 0f;
            Yaw = Mathf.Repeat(Yaw, 360f);
            if (float.IsNaN(Pitch) || float.IsInfinity(Pitch)) Pitch = 0f;
            Pitch = Mathf.Clamp(Pitch, -80f, 80f);
            if (float.IsNaN(AimPoint.x) || float.IsInfinity(AimPoint.x) ||
                float.IsNaN(AimPoint.y) || float.IsInfinity(AimPoint.y) ||
                float.IsNaN(AimPoint.z) || float.IsInfinity(AimPoint.z))
                AimPoint = Vector3.zero;
            if (WeaponChoice > 3) WeaponChoice = 0;
        }
    }
}
