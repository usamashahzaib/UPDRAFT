using Hotdrop.Config;

namespace Hotdrop.Weapons
{
    /// <summary>
    /// Spec section 7: magazine, cadence and reload for the equipped weapon.
    /// Pure logic shared by humans and bots. Cadence carries the sub-frame
    /// remainder so the long-run fire rate matches the configured RPM at any
    /// frame rate, while an idle trigger never banks shots (a fresh press fires
    /// as soon as one full interval has passed since the previous shot).
    /// Reload blocks firing; an empty magazine reloads automatically.
    /// </summary>
    public class WeaponState
    {
        const float Epsilon = 1e-5f;

        public WeaponDefinition Weapon { get; private set; }
        public int Ammo { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadRemaining { get; private set; }
        public int MagazineSize => Weapon != null ? Weapon.magazineSize : 0;
        public bool CanFire => Weapon != null && !IsReloading && Ammo > 0;

        // Seconds until the next shot may leave the barrel; <= 0 means ready.
        float _cooldown;

        /// <summary>Equip (or re-equip) a weapon with a full magazine and no pending reload.</summary>
        public void Equip(WeaponDefinition weapon)
        {
            Weapon = weapon;
            Ammo = weapon != null ? weapon.magazineSize : 0;
            IsReloading = false;
            ReloadRemaining = 0f;
            _cooldown = 0f;
        }

        /// <summary>Tutorial scripting only: force the magazine count.</summary>
        public void SetAmmo(int amount)
        {
            Ammo = System.Math.Max(0, System.Math.Min(amount, MagazineSize));
        }

        /// <summary>Manual reload. Returns true when a reload actually started.</summary>
        public bool RequestReload()
        {
            if (Weapon == null || IsReloading || Ammo >= Weapon.magazineSize) return false;
            StartReload();
            return true;
        }

        /// <summary>
        /// Advance by <paramref name="deltaTime"/> with the trigger held or not.
        /// Returns how many shots were fired during this step (0..n; n &gt; 1 only
        /// when a frame is longer than the fire interval).
        /// </summary>
        public int Tick(float deltaTime, bool triggerHeld)
        {
            if (Weapon == null) return 0;

            if (IsReloading)
            {
                ReloadRemaining -= deltaTime;
                if (ReloadRemaining > Epsilon) return 0;
                // Reload finished inside this step: the rest of the step can fire.
                deltaTime = -ReloadRemaining;
                IsReloading = false;
                ReloadRemaining = 0f;
                Ammo = Weapon.magazineSize;
                _cooldown = 0f;
            }

            _cooldown -= deltaTime;
            if (!triggerHeld)
            {
                if (_cooldown < 0f) _cooldown = 0f; // no banking while idle
                return 0;
            }

            int shots = 0;
            float interval = Weapon.SecondsPerShot;
            while (_cooldown <= Epsilon && Ammo > 0)
            {
                Ammo--;
                shots++;
                _cooldown += interval;
            }
            if (_cooldown < 0f) _cooldown = 0f;

            if (Ammo == 0) StartReload(); // auto-reload (spec default)
            return shots;
        }

        void StartReload()
        {
            IsReloading = true;
            ReloadRemaining = Weapon.reloadSeconds;
        }
    }
}


