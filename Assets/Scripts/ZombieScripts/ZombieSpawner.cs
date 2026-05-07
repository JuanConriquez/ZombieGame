using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using ZombieGame.Combat;

/// <summary>
/// Attach to any GameObject in your scene to act as a zombie factory.
///
/// Usage – call from anywhere:
///   ZombieSpawner.Instance.Spawn(ZombieType.Runner, speed: 6f, health: 60f, position: spawnPoint.position);
///   ZombieSpawner.Instance.Spawn(ZombieType.Tank,   speed: 2f, health: 400f, position: transform.position);
///
/// You can also call SpawnWithDefaults(ZombieType) to use the per-type
/// default stats configured in the ZombieData ScriptableObjects.
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton (optional convenience – remove if you prefer DI / service locator)
    // ──────────────────────────────────────────────

    public static ZombieSpawner Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector – prefab slots
    // ──────────────────────────────────────────────

    [Header("Zombie Prefabs")]
    [Tooltip("Prefab for the Regular zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject regularPrefab;

    [Tooltip("Prefab for the Tank zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject tankPrefab;

    [Tooltip("Prefab for the Runner zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject runnerPrefab;

    [Tooltip("Prefab for the Thrower zombie. Must have a Zombie component.")]
    [SerializeField] private GameObject throwerPrefab;

    // ──────────────────────────────────────────────
    //  Inspector – default stat overrides per type
    // ──────────────────────────────────────────────

    [Header("Default Stats Per Type")]
    [SerializeField] private ZombieData regularData;
    [SerializeField] private ZombieData tankData;
    [SerializeField] private ZombieData runnerData;
    [SerializeField] private ZombieData throwerData;

    // ──────────────────────────────────────────────
    //  Inspector – gameplay
    // ──────────────────────────────────────────────

    [Header("Gameplay")]
    [Tooltip("The Transform zombies will chase. Assign your player here.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("If no valid NavMesh is found at the requested spawn position, " +
             "search within this radius for the nearest valid point.")]
    [SerializeField, Min(0.5f)] private float navMeshSampleRadius = 5f;

    [Tooltip("Optional parent Transform to keep the hierarchy tidy.")]
    [SerializeField] private Transform zombieContainer;

    [Header("Drops")]
    [Tooltip("Chance for a zombie kill to spawn an ammo box pickup.")]
    [SerializeField, Range(0f, 1f)] private float ammoDropChance = 0.15f;
    [Tooltip("Optional pickup prefab. If null, a simple fallback pickup object is created.")]
    [SerializeField] private GameObject ammoPickupPrefab;
    [Tooltip("Vertical offset applied to spawned pickups.")]
    [SerializeField, Min(0f)] private float ammoPickupHeightOffset = 0.35f;

    // ──────────────────────────────────────────────
    //  Events
    // ──────────────────────────────────────────────

    /// <summary>Fired every time a zombie is successfully spawned.</summary>
    public System.Action<Zombie> OnZombieSpawned;

    /// <summary>Fires every time any zombie dies (forwarded from the Zombie's OnDeath event).</summary>
    public System.Action<Zombie> OnZombieDied;

    // ══════════════════════════════════════════════
    //  Unity lifecycle
    // ══════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ══════════════════════════════════════════════
    //  Primary spawn API
    // ══════════════════════════════════════════════

    /// <summary>
    /// Spawn a zombie with fully explicit stats.
    /// </summary>
    /// <param name="type">Which zombie archetype to spawn.</param>
    /// <param name="speed">NavMesh movement speed.</param>
    /// <param name="health">Starting (and maximum) health.</param>
    /// <param name="position">World-space spawn position. Snapped to NavMesh automatically.</param>
    /// <returns>The Zombie component on the new instance, or null if spawn failed.</returns>
    public Zombie Spawn(ZombieType type, float speed, float health, Vector3 position)
    {
        // 1. Resolve prefab
        GameObject prefab = GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"[ZombieSpawner] No prefab assigned for ZombieType.{type}. " +
                           "Assign it in the Inspector.");
            return null;
        }

        // 2. Find a valid NavMesh position near the requested point
        if (!TryGetNavMeshPosition(position, out Vector3 navPosition))
        {
            Debug.LogWarning($"[ZombieSpawner] Could not find a NavMesh position near {position} " +
                             $"within radius {navMeshSampleRadius}. Zombie not spawned.");
            return null;
        }

        // 3. Instantiate
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

        // 4. Get & validate the Zombie component
        if (!go.TryGetComponent<Zombie>(out var zombie))
        {
            Debug.LogError($"[ZombieSpawner] Prefab '{prefab.name}' is missing a Zombie component. " +
                           "Destroying spawned object.");
            Destroy(go);
            return null;
        }

        // 5. Resolve chase target
        Transform chase = GetNearestPlayer(navPosition);
        if (chase == null)
        {
            Destroy(go);
            return null;
        }

        // 6. Initialise stats
        zombie.Initialize(type, speed, health, chase);

        // 7. Wire up death callback
        zombie.OnDeath += HandleZombieDeath;

        // 8. Notify listeners
        OnZombieSpawned?.Invoke(zombie);

        return zombie;
    }

    /// <summary>
    /// Spawn a zombie using the default stats defined in its ZombieData ScriptableObject.
    /// Useful for wave systems where you don't want to hard-code numbers.
    /// </summary>
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

    /// <summary>
    /// Convenience overload: spawn at a Transform's position.
    /// </summary>
    public Zombie Spawn(ZombieType type, float speed, float health, Transform spawnPoint)
        => Spawn(type, speed, health, spawnPoint.position);

    /// <summary>
    /// Convenience overload: spawn with defaults at a Transform's position.
    /// </summary>
    public Zombie SpawnWithDefaults(ZombieType type, Transform spawnPoint)
        => SpawnWithDefaults(type, spawnPoint.position);

    // ══════════════════════════════════════════════
    //  Internal helpers
    // ══════════════════════════════════════════════

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
        zombie.OnDeath -= HandleZombieDeath; // Unsubscribe to avoid leaks
        TrySpawnAmmoPickup(zombie.transform.position);
        OnZombieDied?.Invoke(zombie);
    }

    private void TrySpawnAmmoPickup(Vector3 zombiePosition)
    {
        if (ammoDropChance <= 0f || Random.value > ammoDropChance) return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer) return;

        Vector3 spawnPos = zombiePosition + Vector3.up * ammoPickupHeightOffset;
        if (ammoPickupPrefab != null)
        {
            GameObject pickup = Instantiate(ammoPickupPrefab, spawnPos, Quaternion.identity);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && pickup.TryGetComponent<NetworkObject>(out var netObj))
                netObj.Spawn();
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.name = "AmmoBoxPickup";
        fallback.transform.position = spawnPos;
        fallback.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);

        var collider = fallback.GetComponent<Collider>();
        collider.isTrigger = true;

        var rb = fallback.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        fallback.AddComponent<AmmoBoxPickup>();
    }

    // ──────────────────────────────────────────────
    //  Built-in fallback defaults (used when no ZombieData SO is assigned)
    // ──────────────────────────────────────────────

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
