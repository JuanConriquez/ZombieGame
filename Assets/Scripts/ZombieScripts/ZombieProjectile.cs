using UnityEngine;
using ZombieGame.Combat;

[RequireComponent(typeof(Rigidbody))]
public class ZombieProjectile : MonoBehaviour
{
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float lifetime        = 4f;

    private float damage;
    private bool  hasHit;

    public void Launch(Vector3 targetPosition, float damageAmount)
    {
        damage = damageAmount;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = direction * projectileSpeed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

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