using Unity.Netcode;
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Place on a pickup with a trigger collider. Walking into it tops up weapon reserve ammo (per slot, at least reserveAmmoStart from each WeaponData).
    /// networked: only the owning client requests pickup; server refills ammo and despawns this NetworkObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmmoBoxPickup : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            WeaponController wc = other.GetComponentInParent<WeaponController>();
            if (wc == null) return;

            bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (online)
            {
                if (!wc.IsOwner) return;
                ulong pickupId = 0UL;
                if (TryGetComponent<NetworkObject>(out var nob) && nob.IsSpawned)
                    pickupId = nob.NetworkObjectId;
                wc.RequestCollectAmmoPickupServerRpc(pickupId);
            }
            else
            {
                wc.GrantAmmoBox();
                Destroy(gameObject);
            }
        }
    }
}
