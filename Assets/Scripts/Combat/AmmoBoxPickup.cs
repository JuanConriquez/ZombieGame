using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Pickup that refills player reserve ammo when a player moves nearby.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmmoBoxPickup : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float autoPickupRadius = 1.75f;
        [SerializeField, Min(1f)] private float lifetimeSeconds = 15f;

        private bool _consumed;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Start()
        {
            Destroy(gameObject, lifetimeSeconds);
        }

        private void Update()
        {
            if (_consumed) return;
            Collider[] nearby = Physics.OverlapSphere(transform.position, autoPickupRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < nearby.Length; i++)
            {
                TryPickup(nearby[i]);
                if (_consumed) return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickup(other);
        }

        private void TryPickup(Collider other)
        {
            if (_consumed || other == null) return;

            WeaponController weaponController = other.GetComponentInParent<WeaponController>();
            if (weaponController == null) return;

            _consumed = true;
            weaponController.GrantAmmoBox();
            Destroy(gameObject);
        }
    }
}
