using UnityEngine;

namespace ZombieGame.Combat
{
    public enum WeaponKind { Pistol, Shotgun, AssaultRifle }

    [CreateAssetMenu(fileName = "Weapon_New", menuName = "ZombieGame/Weapon Data", order = 0)]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Pistol";
        public WeaponKind kind = WeaponKind.Pistol;

        [Header("Ballistics")]
        [Tooltip("Damage per pellet/bullet on a hit.")]
        public float damagePerPellet = 18f;
        [Tooltip("Pellets fired per trigger pull. 1 for pistol/AR, 6-10 for shotgun.")]
        public int pelletsPerShot = 1;
        [Tooltip("Max effective range in meters.")]
        public float range = 60f;
        [Tooltip("Rounds per second.")]
        public float fireRate = 4f;
        public bool automatic = false;

        [Header("Ammo")]
        public int magazineSize = 12;
        public int reserveAmmoStart = 60;
        public float reloadSeconds = 1.4f;

        [Header("Recoil / Spread (degrees)")]
        [Tooltip("Resting hipfire spread.")]
        public float baseSpreadDeg = 0.6f;
        [Tooltip("Spread added per shot.")]
        public float spreadPerShotDeg = 1.5f;
        [Tooltip("Hard cap on spread.")]
        public float maxSpreadDeg = 8f;
        [Tooltip("Spread bleed-off per second when not firing.")]
        public float spreadRecoverPerSec = 6f;

        [Header("Camera Kick")]
        [Tooltip("World-space camera shake magnitude per shot.")]
        public float cameraKick = 0.18f;
        [Tooltip("How fast the camera returns to rest.")]
        public float cameraKickRecover = 14f;

        [Header("Muzzle Flash Light")]
        public float muzzleLightIntensity = 6f;
        public float muzzleLightRange = 14f;
        public float muzzleLightSeconds = 0.06f;
        public Color muzzleLightColor = new Color(1f, 0.85f, 0.55f, 1f);

        [Header("Tracer")]
        public Color tracerColor = new Color(1f, 0.9f, 0.5f, 1f);
        public float tracerSeconds = 0.04f;
        public float tracerWidth = 0.05f;
    }
}
