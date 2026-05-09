using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using ZombieGame.Combat;

public class ZombieSpawner : MonoBehaviour
{

    public static ZombieSpawner Instance { get; private set; }

    [Header("Zombie Prefabs")]
    [Tooltip("Prefab for the Regular zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject regularPrefab;

    [Tooltip("Prefab for the Tank zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject tankPrefab;

    [Tooltip("Prefab for the Runner zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject runnerPrefab;

    [Tooltip("Prefab for the Thrower zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject throwerPrefab;

    [Header("Default Stats Per Type")]
    [SerializeField] private ZombieData regularData;
    [SerializeField] private ZombieData tankData;
    [SerializeField] private ZombieData runnerData;
    [SerializeField] private ZombieData throwerData;

    [Header("Gameplay")]
    [Tooltip("The Transform zombies will chase. Assign your player here.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("If no valid NavMesh is found at the requested spawn position, " +
             "search within this radius for the nearest valid point.")]
    [SerializeField, Min(0.5f)] private float navMeshSampleRadius = 5f;

    [Tooltip("Optional parent Transform to keep the hierarchy tidy.")]
    [SerializeField] private Transform zombieContainer;

    [Header("Drops")]
    [Tooltip("Chance (0–1) for an ammo pickup to spawn when a zombie dies.")]
    [SerializeField, Range(0f, 1f)] private float ammoDropChance = 0.15f;
    [Tooltip("Optional pickup prefab: trigger collider, AmmoBoxPickup, NetworkObject when using Netcode.")]
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField, Min(0f)] private float ammoPickupHeightOffset = 0.35f;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform.position;
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }

    public Zombie Spawn(ZombieType type, float speed, float health)
    {
        return Spawn(type, speed, health, GetRandomSpawnPoint());
    }

    public System.Action<Zombie> OnZombieSpawned;

    public System.Action<Zombie> OnZombieDied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <param name="type">Which zombie archetype to spawn.</param>
    /// <param name="speed">NavMesh movement speed.</param>
    /// <param name="health">Starting (and maximum) health.</param>
    /// <param name="position">World-space spawn position. Snapped to NavMesh automatically.</param>
    /// <returns>The Zombie component on the new instance, or null if spawn failed.</returns>
    public Zombie Spawn(ZombieType type, float speed, float health, Vector3 position)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"[ZombieSpawner] No prefab assigned for ZombieType.{type}. " +
                           "Assign it in the Inspector.");
            return null;
        }

        if (!TryGetNavMeshPosition(position, out Vector3 navPosition))
        {
            Debug.LogWarning($"[ZombieSpawner] Could not find a NavMesh position near {position} " +
                             $"within radius {navMeshSampleRadius}. Zombie not spawned.");
            return null;
        }

        Transform parent = zombieContainer != null ? zombieContainer : transform;
        GameObject go;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            go = Instantiate(prefab, navPosition, Quaternion.identity, parent);
            go.GetComponent<NetworkObject>().Spawn();
        }
        else if (NetworkManager.Singleton == null)
        {
            go = Instantiate(prefab, navPosition, Quaternion.identity, parent);
        }
        else
        {
            Debug.LogWarning("[ZombieSpawner] Client attempted to spawn a zombie. Only server can spawn");
            return null;
        }
        go.name = $"{type}_Zombie";

        if (!go.TryGetComponent<Zombie>(out var zombie))
        {
            Debug.LogError($"[ZombieSpawner] Prefab '{prefab.name}' is missing a Zombie component. " +
                           "Destroying spawned object.");
            Destroy(go);
            return null;
        }

        Transform chase = GetNearestPlayer(navPosition);
        if (chase == null)
        {
            Destroy(go);
            return null;
        }

        zombie.Initialize(type, speed, health, chase);

        zombie.OnDeath += HandleZombieDeath;

        OnZombieSpawned?.Invoke(zombie);

        return zombie;
    }

    /// <param name="type">Which zombie archetype to spawn.</param>
    /// <param name="position">World-space spawn position.</param>
    /// <returns>The Zombie component, or null if spawn failed.</returns>
    public Zombie SpawnWithDefaults(ZombieType type, Vector3 position)
    {
        ZombieData data = GetData(type);

        if (data == null)
        {
            Debug.LogWarning($"[ZombieSpawner] No ZombieData assigned for ZombieType.{type}. " +
                             "Falling back to built-in defaults.");
            return Spawn(type, GetBuiltInSpeed(type), GetBuiltInHealth(type), position);
        }

        return Spawn(type, data.DefaultSpeed, data.DefaultHealth, position);
    }

    public Zombie Spawn(ZombieType type, float speed, float health, Transform spawnPoint)
        => Spawn(type, speed, health, spawnPoint.position);

    public Zombie SpawnWithDefaults(ZombieType type, Transform spawnPoint)
        => SpawnWithDefaults(type, spawnPoint.position);

    private GameObject GetPrefab(ZombieType type) => type switch
    {
        ZombieType.Regular  => regularPrefab,
        ZombieType.Tank     => tankPrefab,
        ZombieType.Runner   => runnerPrefab,
        ZombieType.Thrower  => throwerPrefab,
        _                   => null
    };

    private ZombieData GetData(ZombieType type) => type switch
    {
        ZombieType.Regular  => regularData,
        ZombieType.Tank     => tankData,
        ZombieType.Runner   => runnerData,
        ZombieType.Thrower  => throwerData,
        _                   => null
    };

    private bool TryGetNavMeshPosition(Vector3 requestedPos, out Vector3 result)
    {
        if (NavMesh.SamplePosition(requestedPos, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = requestedPos;
        return false;
    }

    private void HandleZombieDeath(Zombie zombie)
    {
        zombie.OnDeath -= HandleZombieDeath;
        TrySpawnAmmoPickup(zombie.transform.position);
        OnZombieDied?.Invoke(zombie);
    }

    private void TrySpawnAmmoPickup(Vector3 zombiePosition)
    {
        if (ammoDropChance <= 0f || Random.value > ammoDropChance) return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        Vector3 spawnPos = zombiePosition + Vector3.up * ammoPickupHeightOffset;

        if (ammoPickupPrefab != null)
        {
            GameObject pickup = Instantiate(ammoPickupPrefab, spawnPos, Quaternion.identity);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && pickup.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                netObj.Spawn();
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.name = "AmmoBoxPickup";
        fallback.transform.position = spawnPos;
        fallback.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
        fallback.GetComponent<Collider>().isTrigger = true;
        Rigidbody rb = fallback.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        fallback.AddComponent<AmmoBoxPickup>();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = fallback.AddComponent<NetworkObject>();
            netObj.Spawn();
        }
    }

    private Transform GetNearestPlayer(Vector3 fromPosition)
    {
        if (playerTarget != null) return playerTarget;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(fromPosition, p.transform.position);
            if(dist < nearestDist)
            {
                nearestDist = dist;
                nearest = p.transform;
            }
        }
        if(nearest == null)
        {
            Debug.LogWarning("[ZombieSpawner] No GameObjects tagged 'Player' found Zombie has no target.");
        }
        return nearest;
    }


    private static float GetBuiltInSpeed(ZombieType type) => type switch
    {
        ZombieType.Regular => 3.5f,
        ZombieType.Tank    => 2.0f,
        ZombieType.Runner  => 7.0f,
        ZombieType.Thrower => 3.0f,
        _                  => 3.5f
    };

    private static float GetBuiltInHealth(ZombieType type) => type switch
    {
        ZombieType.Regular => 100f,
        ZombieType.Tank    => 500f,
        ZombieType.Runner  => 60f,
        ZombieType.Thrower => 80f,
        _                  => 100f
    };
}
