using UnityEngine;
using ZombieGame.Combat;

/// <summary>
/// Projectile for Thrower zombies.
/// Fires in a straight line toward the target — designed for top down games.
/// Attach to your projectile prefab. Call Launch() after Instantiate.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ZombieProjectile : MonoBehaviour
{
    [SerializeField] private float projectileSpeed = 6f;  // slow enough to dodge
    [SerializeField] private float lifetime        = 4f;  // destroys if it misses

    private float damage;
    private bool  hasHit;

    /// <summary>
    /// Fire the projectile in a straight line toward a world-space target position.
    /// </summary>
    public void Launch(Vector3 targetPosition, float damageAmount)
    {
        damage = damageAmount;

        // Flat straight line — keep Y the same so it doesn't arc up or fall down
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;  // no gravity so it stays flat
        rb.linearVelocity = direction * projectileSpeed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Don't collide with other zombies
        if (other.TryGetComponent<Zombie>(out _)) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            hasHit = true;
            Vector3 hitPoint  = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - other.transform.position).normalized;
            damageable.ApplyDamage(damage, gameObject, hitPoint, hitNormal);
            Destroy(gameObject);
        }
    }
}