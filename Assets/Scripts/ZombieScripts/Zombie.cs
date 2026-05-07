using UnityEngine;
using UnityEngine.AI;
using ZombieGame.Combat;
using Unity.Netcode;

/// <summary>
/// The four zombie archetypes available in the game.
/// </summary>
public enum ZombieType
{
    Regular,   // Balanced – average speed and health
    Tank,      // Slow but very high health; hits hard
    Runner,    // Fast, low health; charges the player
    Thrower    // Ranged attacker; keeps distance and lobs projectiles
}

/// <summary>
/// Core zombie behaviour.
/// Attach to a GameObject that also has a NavMeshAgent component.
/// All per-type values (speed, health, attack) are set at spawn-time
/// by ZombieSpawner — you can also tweak them at runtime if needed.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    // ──────────────────────────────────────────────
    //  Identity
    // ──────────────────────────────────────────────

    /// <summary>Which archetype this zombie was spawned as.</summary>
    public ZombieType ZombieType { get; private set; }

    // ──────────────────────────────────────────────
    //  Stats (set via Initialize)
    // ──────────────────────────────────────────────

    [Header("Runtime Stats (read-only in Inspector)")]
    [SerializeField, Min(0)] private float maxHealth = 100f;
    [SerializeField, Min(0)] private float currentHealth;
    [SerializeField, Min(0)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0)] private float attackDamage = 10f;
    [SerializeField, Min(0)] private float attackRange = 2f;
    [SerializeField, Min(0)] private float attackCooldown = 1.5f;

    // ──────────────────────────────────────────────
    //  Thrower-specific
    // ──────────────────────────────────────────────

    [Header("Thrower Settings")]
    [Tooltip("Prefab launched by Thrower zombies.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("How far a Thrower tries to stay from the target.")]
    [SerializeField] private float preferredThrowDistance = 10f;

    // ──────────────────────────────────────────────
    //  Internal state
    // ──────────────────────────────────────────────

    private NavMeshAgent agent;
    private Transform target;          // Usually the player
    private float attackTimer;
    private bool isDead;
    private float retargetTimer;
    private const float RetargetInterval = 2f;

    // ──────────────────────────────────────────────
    //  Events — subscribe in other systems as needed
    // ──────────────────────────────────────────────

    /// <summary>Fired when the zombie takes any damage.  Args: (damageAmount, remainingHealth)</summary>
    public System.Action<float, float> OnDamaged;

    /// <summary>Fired once when health reaches 0.</summary>
    public System.Action<Zombie> OnDeath;

    // ══════════════════════════════════════════════
    //  Initialisation
    // ══════════════════════════════════════════════

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Called by ZombieSpawner immediately after Instantiate.
    /// Configures all stats and assigns the chase target.
    /// </summary>
    /// <param name="type">Zombie archetype.</param>
    /// <param name="speed">Movement speed (applied to the NavMeshAgent).</param>
    /// <param name="health">Maximum (and starting) health.</param>
    /// <param name="chaseTarget">Transform the zombie will navigate toward (usually the player).</param>
    public void Initialize(ZombieType type, float speed, float health, Transform chaseTarget)
    {
        ZombieType   = type;
        moveSpeed    = speed;
        maxHealth    = health;
        currentHealth = health;
        target       = chaseTarget;
        isDead       = false;

        // Push speed into the NavMeshAgent immediately
        agent.speed = moveSpeed;

        // Per-type stat overrides – tweak these to balance your game
        switch (ZombieType)
        {
            case ZombieType.Regular:
                attackDamage  = 10f;
                attackRange   = 4f;
                attackCooldown = 1.5f;
                agent.stoppingDistance = 1.5f;
                break;

            case ZombieType.Tank:
                attackDamage  = 25f;
                attackRange   = 4.5f;
                attackCooldown = 2.5f;
                agent.stoppingDistance = 2f;
                break;

            case ZombieType.Runner:
                attackDamage  = 8f;
                attackRange   = 3.5f;
                attackCooldown = 0.8f;
                agent.stoppingDistance = 1f;
                break;

            case ZombieType.Thrower:
                attackDamage   = 12f;
                attackRange    = preferredThrowDistance;
                attackCooldown  = 2f;
                agent.stoppingDistance = 0f; // ThrowerBehaviour controls distance manually
                break;
        }
    }

    // ══════════════════════════════════════════════
    //  Main Loop
    // ══════════════════════════════════════════════

    private void Update()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer) return;
        if (isDead) return;

        attackTimer += Time.deltaTime;

        retargetTimer += Time.deltaTime;
        if (retargetTimer >= RetargetInterval)
        {
            retargetTimer = 0f;
            UpdateNearestTarget();
        }

        if (target == null)
        {
            UpdateNearestTarget();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        switch (ZombieType)
        {
            case ZombieType.Regular:
            case ZombieType.Tank:
                ChaseAndMelee(distanceToTarget);
                break;

            case ZombieType.Runner:
                RunnerBehaviour(distanceToTarget);
                break;

            case ZombieType.Thrower:
                ThrowerBehaviour(distanceToTarget);
                break;
        }
    }

    // ──────────────────────────────────────────────
    //  Behaviour helpers
    // ──────────────────────────────────────────────

    /// <summary>Regular / Tank: walk straight at the player, melee on arrival.</summary>
    private void ChaseAndMelee(float distance)
    {
        agent.SetDestination(target.position);
        Debug.Log($"[Zombie] distance to player: {distance} attackRange: {attackRange} timer: {attackTimer}");
        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            PerformMeleeAttack();
        }
    }

    /// <summary>Runner: same as melee but sprints; no special evasion.</summary>
    private void RunnerBehaviour(float distance)
    {
        agent.SetDestination(target.position);

        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            PerformMeleeAttack();
        }
    }

    /// <summary>
    /// Thrower: maintain preferred throw distance, lob projectiles when in range.
    /// If the player closes in, the Thrower backs up.
    /// </summary>
    private void ThrowerBehaviour(float distance)
    {
        if (distance < preferredThrowDistance * 0.4f)
        {
            // Player too close — retreat
            Vector3 retreatDir = (transform.position - target.position).normalized;
            agent.SetDestination(transform.position + retreatDir * 3f);
        }
        else
        {
            // Walk toward player until within throw range
            agent.SetDestination(target.position);
        }

        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            PerformRangedAttack();
        }
    }

    // ──────────────────────────────────────────────
    //  Attack execution
    // ──────────────────────────────────────────────

    private void PerformMeleeAttack()
    {
        attackTimer = 0f;

        // Check the target and its parents for IDamageable
        // because Health might be on the root player, not a child object
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Vector3 hitNormal = (target.position - transform.position).normalized;
            damageable.ApplyDamage(attackDamage, gameObject, target.position, hitNormal);
            Debug.Log($"[Zombie] Damage applied to {target.name}");
        }
        else
        {
            Debug.LogWarning($"[Zombie] No IDamageable found on {target.name} or its parents!");
        }

            // Hook: override or subscribe to add animations, sounds, particles
            OnMeleeAttack();
        }

    private void PerformRangedAttack()
    {
        attackTimer = 0f;

        if (projectilePrefab != null)
        {
            // Spawn above the zombie's centre and aim at the target
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // Assumes your projectile has a ZombieProjectile component
            if (proj.TryGetComponent<ZombieProjectile>(out var projectile))
            {
                projectile.Launch(target.position, attackDamage);
            }
        }

        OnRangedAttack();
    }

    // ══════════════════════════════════════════════
    //  Health / Damage
    // ══════════════════════════════════════════════

    /// <summary>Apply damage to this zombie. Returns true if the hit was lethal.</summary>
    public bool TakeDamage(float damage)
    {
        if (isDead) return false;

        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        OnDamaged?.Invoke(damage, currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    /// <summary>Instantly kill the zombie (e.g. triggered by a hazard).</summary>
    public void InstantKill()
    {
        if (!isDead) Die();
    }

    private void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled   = false;

        OnDeath?.Invoke(this);
        OnDeathBehaviour();

        Destroy(gameObject);
    }

    // ══════════════════════════════════════════════
    //  Overridable hooks (subclass or extend as needed)
    // ══════════════════════════════════════════════

    /// <summary>Called every time this zombie performs a melee attack.</summary>
    protected virtual void OnMeleeAttack() { }

    /// <summary>Called every time this zombie performs a ranged attack.</summary>
    protected virtual void OnRangedAttack() { }

    /// <summary>Called when health hits zero, before the GameObject is destroyed.</summary>
    protected virtual void OnDeathBehaviour() { }

    // ══════════════════════════════════════════════
    //  Public accessors
    // ══════════════════════════════════════════════

    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool  IsDead        => isDead;

    // ══════════════════════════════════════════════
    //  IDamageable (ZombieGame.Combat)
    //  Makes this zombie a valid target for WeaponController's bullet raycasts.
    // ══════════════════════════════════════════════

    /// <summary>Team 1 = enemies. Prevents friendly-fire between zombies.</summary>
    public int  TeamId  => 1;

    /// <summary>Required by IDamageable; mirrors the internal isDead flag.</summary>
    public bool IsAlive => !isDead;

    /// <summary>
    /// Called by WeaponController when a bullet hits this zombie.
    /// Bridges the combat system's damage pipeline into the zombie's own health logic.
    /// hitPoint and hitNormal are available here if you want to add hit reactions,
    /// directional ragdoll forces, or headshot detection later.
    /// </summary>
    public void ApplyDamage(float amount, GameObject source, Vector3 hitPoint, Vector3 hitNormal)
    {
        TakeDamage(amount);
    }
    private void UpdateNearestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if(dist < nearestDist)
            {
                nearestDist = dist;
                nearest = p.transform;
            }
        }
        if(nearest != null)
        {
            target = nearest;
        }

    }

    /// <summary>Swap the chase target at runtime (e.g. player dies, new target assigned).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (agent.enabled && newTarget != null)
            agent.SetDestination(newTarget.position);
    }
}