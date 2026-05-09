using UnityEngine;
using UnityEngine.AI;
using ZombieGame.Combat;
using Unity.Netcode;

public enum ZombieType
{
    Regular,   // Balanced – average speed and health
    Tank,      // Slow but very high health; hits hard
    Runner,    // Fast, low health; charges the player
    Thrower    // Ranged attacker; keeps distance and lobs projectiles
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{

    public ZombieType ZombieType { get; private set; }

    [Header("Runtime Stats (read-only in Inspector)")]
    [SerializeField, Min(0)] private float maxHealth = 100f;
    [SerializeField, Min(0)] private float currentHealth;
    [SerializeField, Min(0)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0)] private float attackDamage = 10f;
    [SerializeField, Min(0)] private float attackRange = 2f;
    [SerializeField, Min(0)] private float attackCooldown = 1.5f;

    [Header("Thrower Settings")]
    [Tooltip("Prefab launched by Thrower zombies.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("How far a Thrower tries to stay from the target.")]
    [SerializeField] private float preferredThrowDistance = 10f;

    private NavMeshAgent agent;
    private Transform target;
    private float attackTimer;
    private bool isDead;
    private float retargetTimer;
    private const float RetargetInterval = 2f;

    private Animator animator;

    public System.Action<float, float> OnDamaged;

    public System.Action<Zombie> OnDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

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

        agent.speed = moveSpeed;

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

        if (animator != null && agent.enabled)
        {
         animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private void ChaseAndMelee(float distance)
    {
        agent.SetDestination(target.position);
        Debug.Log($"[Zombie] distance to player: {distance} attackRange: {attackRange} timer: {attackTimer}");
        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            PerformMeleeAttack();
        }
    }

    private void RunnerBehaviour(float distance)
    {
        agent.SetDestination(target.position);

        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            PerformMeleeAttack();
        }
    }

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

    private void PerformMeleeAttack()
    {
        attackTimer = 0f;

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

            OnMeleeAttack();
        }

    private void PerformRangedAttack()
    {
        attackTimer = 0f;

        if (projectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            if (proj.TryGetComponent<ZombieProjectile>(out var projectile))
            {
                projectile.Launch(target.position, attackDamage);
            }
        }

        OnRangedAttack();
    }

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

    protected virtual void OnMeleeAttack() { }

    protected virtual void OnRangedAttack() { }

    protected virtual void OnDeathBehaviour() { }

    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool  IsDead        => isDead;

    public int  TeamId  => 1;

    public bool IsAlive => !isDead;

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

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (agent.enabled && newTarget != null)
            agent.SetDestination(newTarget.position);
    }
}