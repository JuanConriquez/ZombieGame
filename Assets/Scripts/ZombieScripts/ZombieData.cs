using UnityEngine;

/// <summary>
/// ScriptableObject that stores the default stats for one zombie type.
/// Create an asset per type: Assets > Create > Zombies > Zombie Data
///
/// Assign these assets in the ZombieSpawner Inspector slots so designers
/// can tune balance without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Zombies/Zombie Data", fileName = "ZombieData_New")]
public class ZombieData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Which archetype this data describes. Informational only.")]
    public ZombieType Type;

    [Header("Default Stats")]
    [Tooltip("Default NavMesh movement speed.")]
    [Min(0.1f)] public float DefaultSpeed  = 3.5f;

    [Tooltip("Default maximum health.")]
    [Min(1f)]   public float DefaultHealth = 100f;

    [Header("Combat")]
    [Tooltip("Base melee / projectile damage.")]
    [Min(0f)]   public float AttackDamage  = 10f;

    [Tooltip("Distance at which this zombie can attack.")]
    [Min(0.5f)] public float AttackRange   = 2f;

    [Tooltip("Seconds between attacks.")]
    [Min(0.1f)] public float AttackCooldown = 1.5f;

    [Header("Thrower Only")]
    [Tooltip("Preferred engagement distance for Thrower type (ignored for others).")]
    [Min(1f)]   public float PreferredThrowDistance = 10f;
}
