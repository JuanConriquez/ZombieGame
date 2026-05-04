using UnityEngine;

namespace ZombieGame.Combat
{
    public interface IDamageable
    {
        int TeamId { get; }
        bool IsAlive { get; }
        void ApplyDamage(float amount, GameObject source, Vector3 hitPoint, Vector3 hitNormal);
    }
}
