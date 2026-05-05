using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// COD-style wave manager. Sits on top of ZombieSpawner and drives round
/// progression, zombie-count scaling, health scaling, and type unlocks.
///
/// ── Setup ──────────────────────────────────────────────────────────────────
///  1. Add this component to any GameObject in your scene.
///  2. Assign at least one Transform in Spawn Points via the Inspector.
///  3. Enable Auto Start or call BeginWaves() from another script.
///
/// ── How it works ───────────────────────────────────────────────────────────
///  Idle → Countdown → Spawning → Active → (next wave) → …
///  A wave does NOT end until every zombie spawned that round has died.
///
/// ── Multiplayer ────────────────────────────────────────────────────────────
///  Extends NetworkBehaviour. Wave state and counts are synced to all clients
///  via NetworkVariables so any HUD script can read them. Spawn logic only
///  runs on the server. If you run without Netcode (offline), Start() detects
///  this and wires up a local fallback automatically.
/// </summary>
public class WaveManager : NetworkBehaviour
{
    // ──────────────────────────────────────────────
    //  State machine
    // ──────────────────────────────────────────────

    public enum WaveState { Idle, Countdown, Spawning, Active }

    // ──────────────────────────────────────────────
    //  Inspector — Spawn Points
    // ──────────────────────────────────────────────

    [Header("Spawn Points")]
    [Tooltip("World positions where zombies can appear. Assign multiple for variety — " +
             "one is picked at random per zombie.")]
    [SerializeField] private Transform[] spawnPoints;

    // ──────────────────────────────────────────────
    //  Inspector — Timing
    // ──────────────────────────────────────────────

    [Header("Wave Timing")]
    [Tooltip("Seconds between waves (the 'get ready' countdown).")]
    [SerializeField, Min(1f)] private float secondsBetweenWaves = 10f;

    [Tooltip("Seconds between each zombie spawning within a wave. " +
             "Lower = more intense pressure.")]
    [SerializeField, Min(0f)] private float spawnInterval = 0.8f;

    [Tooltip("Start waves automatically when the game begins.")]
    [SerializeField] private bool autoStart = true;

    // ──────────────────────────────────────────────
    //  Inspector — Zombie Count Scaling
    // ──────────────────────────────────────────────

    [Header("Zombie Count Scaling")]
    [Tooltip("Number of zombies on wave 1.")]
    [SerializeField, Min(1)] private int baseZombieCount = 8;

    [Tooltip("Extra zombies added each wave.")]
    [SerializeField, Min(0)] private int zombiesAddedPerWave = 2;

    [Tooltip("Hard cap on zombies per wave. Set to 0 for no cap.")]
    [SerializeField, Min(0)] private int maxZombiesPerWave = 60;

    // ──────────────────────────────────────────────
    //  Inspector — Health Scaling
    // ──────────────────────────────────────────────

    [Header("Health Scaling")]
    [Tooltip("Each wave, zombie health is multiplied by this value raised to (wave-1).\n" +
             "1.15 = +15% per wave compounding. Wave 5 ≈ 1.75×, wave 10 ≈ 4×.")]
    [SerializeField, Range(1f, 2f)] private float healthScalePerWave = 1.15f;

    // ──────────────────────────────────────────────
    //  Inspector — Type Unlocks
    // ──────────────────────────────────────────────

    [Header("Zombie Type Unlock Waves")]
    [Tooltip("Wave at which Runner zombies start appearing.")]
    [SerializeField, Min(1)] private int runnerUnlockWave  = 3;

    [Tooltip("Wave at which Thrower zombies start appearing.")]
    [SerializeField, Min(1)] private int throwerUnlockWave = 6;

    [Tooltip("Wave at which Tank zombies start appearing.")]
    [SerializeField, Min(1)] private int tankUnlockWave    = 10;

    [Tooltip("Maximum fraction of the wave that can be Tanks (0–0.5). " +
             "Prevents the player being swamped by bullet-sponges.")]
    [SerializeField, Range(0f, 0.5f)] private float maxTankFraction = 0.15f;

    // ──────────────────────────────────────────────
    //  NetworkVariables — readable by all clients
    // ──────────────────────────────────────────────

    private readonly NetworkVariable<int>       _currentWave      = new(0);
    private readonly NetworkVariable<int>       _aliveZombies     = new(0);
    private readonly NetworkVariable<int>       _totalThisWave    = new(0);
    private readonly NetworkVariable<WaveState> _waveState        = new(WaveState.Idle);
    private readonly NetworkVariable<int>       _countdownSeconds = new(0);

    // ──────────────────────────────────────────────
    //  Public read-only accessors
    // ──────────────────────────────────────────────

    public int       CurrentWave      => _currentWave.Value;
    public int       AliveZombies     => _aliveZombies.Value;
    public int       TotalThisWave    => _totalThisWave.Value;
    public WaveState State            => _waveState.Value;
    public int       CountdownSeconds => _countdownSeconds.Value;

    // ──────────────────────────────────────────────
    //  Events (fire on all clients via NetworkVariable callbacks)
    // ──────────────────────────────────────────────

    /// <summary>Fired when a new wave begins. Arg: wave number (1-based).</summary>
    public event Action<int> OnWaveStarted;

    /// <summary>Fired when all zombies in a wave have been killed. Arg: wave number.</summary>
    public event Action<int> OnWaveCompleted;

    /// <summary>Fired every second during the between-wave countdown. Arg: seconds remaining.</summary>
    public event Action<int> OnCountdownTick;

    /// <summary>Fired whenever the alive/total zombie count changes. Args: alive, total.</summary>
    public event Action<int, int> OnZombieCountChanged;

    // ──────────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────────

    private Coroutine _waveCoroutine;
    private bool      _offlineMode; // true when running without Netcode

    // ══════════════════════════════════════════════
    //  Unity / Netcode lifecycle
    // ══════════════════════════════════════════════

    private void Start()
    {
        // Offline fallback: if no NetworkManager is active, run as a plain MonoBehaviour.
        bool netcodeActive = NetworkManager.Singleton != null;
        if (!netcodeActive)
        {
            _offlineMode = true;
            WireUpSpawner();
            if (autoStart) BeginWavesOffline();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to NetworkVariable changes so every client fires the same events.
        _waveState.OnValueChanged        += (_, next) => { /* state changes drive events below */  };
        _currentWave.OnValueChanged      += (_, next) => { if (next > 0) OnWaveStarted?.Invoke(next); };
        _aliveZombies.OnValueChanged     += (_, _)    => OnZombieCountChanged?.Invoke(_aliveZombies.Value, _totalThisWave.Value);
        _countdownSeconds.OnValueChanged += (_, next) => OnCountdownTick?.Invoke(next);

        if (!IsServer) return;

        WireUpSpawner();
        if (autoStart) BeginWaves();
    }

    public override void OnNetworkDespawn()
    {
        UnwireSpawner();
    }

    public override void OnDestroy()
    {
        if (_offlineMode) UnwireSpawner();
    }

    // ══════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════

    /// <summary>
    /// Begin the wave sequence from wave 1. Call this instead of relying on
    /// Auto Start if you want waves to begin after a lobby countdown, etc.
    /// Server / offline only — clients cannot call this directly.
    /// </summary>
    public void BeginWaves()
    {
        if (!IsAuthority()) return;

        if (GetState() != WaveState.Idle)
        {
            Debug.LogWarning("[WaveManager] BeginWaves() called but waves are already running.");
            return;
        }

        StartNextWave();
    }

    /// <summary>
    /// Stop all wave activity immediately. Server / offline only.
    /// </summary>
    public void StopWaves()
    {
        if (!IsAuthority()) return;
        if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
        SetState(WaveState.Idle);
    }

    // ══════════════════════════════════════════════
    //  Wave coroutine (server / offline)
    // ══════════════════════════════════════════════

    private void StartNextWave()
    {
        if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
        _waveCoroutine = StartCoroutine(RunWave());
    }

    private IEnumerator RunWave()
    {
        // ── Phase 1: Countdown ─────────────────────────────────────────────
        SetState(WaveState.Countdown);
        float remaining = secondsBetweenWaves;

        while (remaining > 0f)
        {
            SetCountdown(Mathf.CeilToInt(remaining));
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        SetCountdown(0);

        // ── Phase 2: Advance wave and build spawn list ─────────────────────
        int wave = IncrementWave();
        List<SpawnEntry> spawnList = BuildSpawnList(wave);

        SetAlive(spawnList.Count);
        SetTotal(spawnList.Count);

        // ── Phase 3: Drip-feed spawns ──────────────────────────────────────
        SetState(WaveState.Spawning);
        OnWaveStarted?.Invoke(wave);  // Also fires on server

        foreach (SpawnEntry entry in spawnList)
        {
            Vector3 pos = PickSpawnPoint();
            ZombieSpawner.Instance.Spawn(entry.Type, entry.Speed, entry.Health, pos);
            yield return new WaitForSeconds(spawnInterval);
        }

        // ── Phase 4: Active — wait for HandleZombieDied() to finish the wave
        SetState(WaveState.Active);
    }

    private void HandleZombieDied(Zombie zombie)
    {
        if (!IsAuthority()) return;

        int newAlive = Mathf.Max(0, GetAlive() - 1);
        SetAlive(newAlive);

        if (newAlive <= 0 && GetState() == WaveState.Active)
        {
            OnWaveCompleted?.Invoke(GetWave()); // fires on server
            NotifyWaveCompletedClientRpc(GetWave());
            StartNextWave();
        }
    }

    // Relay WaveCompleted to non-server clients (NetworkVariable change doesn't cover this event).
    [Rpc(SendTo.NotServer)]
    private void NotifyWaveCompletedClientRpc(int wave)
    {
        OnWaveCompleted?.Invoke(wave);
    }

    // ══════════════════════════════════════════════
    //  Spawn list builder
    // ══════════════════════════════════════════════

    private struct SpawnEntry
    {
        public ZombieType Type;
        public float      Health;
        public float      Speed;
    }

    /// <summary>
    /// Build a randomised, shuffled list of zombies to spawn this wave.
    /// </summary>
    private List<SpawnEntry> BuildSpawnList(int wave)
    {
        int   count       = GetZombieCount(wave);
        float healthMult  = GetHealthMultiplier(wave);
        int   maxTanks    = Mathf.Max(1, Mathf.RoundToInt(count * maxTankFraction));
        int   tanksUsed   = 0;

        var list = new List<SpawnEntry>(count);

        for (int i = 0; i < count; i++)
        {
            bool tanksCapped = tanksUsed >= maxTanks;
            ZombieType type  = PickZombieType(wave, tanksCapped);
            if (type == ZombieType.Tank) tanksUsed++;

            // Health scales exponentially. Speed gets a gentle nudge every 5 waves, capped at +50%.
            float baseHealth    = BaseHealth(type);
            float baseSpeed     = BaseSpeed(type);
            float scaledHealth  = baseHealth * healthMult;
            float scaledSpeed   = Mathf.Min(baseSpeed * (1f + (wave / 5) * 0.05f), baseSpeed * 1.5f);

            list.Add(new SpawnEntry { Type = type, Health = scaledHealth, Speed = scaledSpeed });
        }

        Shuffle(list);
        return list;
    }

    // ══════════════════════════════════════════════
    //  Scaling formulas
    // ══════════════════════════════════════════════

    /// <summary>
    /// Total zombies this wave.
    /// Wave 1 = baseZombieCount, each subsequent wave adds zombiesAddedPerWave.
    /// Example with defaults: wave 1→8, wave 5→16, wave 10→26.
    /// </summary>
    private int GetZombieCount(int wave)
    {
        int count = baseZombieCount + (wave - 1) * zombiesAddedPerWave;
        if (maxZombiesPerWave > 0)
            count = Mathf.Min(count, maxZombiesPerWave);
        return count;
    }

    /// <summary>
    /// Compound health multiplier. With the default 1.15:
    ///   Wave 1 → 1.00×  Wave 5 → 1.75×  Wave 10 → 4.05×  Wave 20 → 16.4×
    /// </summary>
    private float GetHealthMultiplier(int wave)
    {
        return Mathf.Pow(healthScalePerWave, wave - 1);
    }

    // ── Type selection ──────────────────────────────────────────────────────

    /// <summary>
    /// Pick a zombie type using a weighted random draw.
    /// Weights ramp up gradually after each type's unlock wave so the
    /// transition feels natural rather than abrupt.
    /// </summary>
    private ZombieType PickZombieType(int wave, bool tanksCapped)
    {
        // Each non-Regular weight ramps up by a small amount per wave after unlock.
        float runnerW  = wave >= runnerUnlockWave  ? Mathf.Min(0.25f, (wave - runnerUnlockWave  + 1) * 0.04f) : 0f;
        float throwerW = wave >= throwerUnlockWave ? Mathf.Min(0.20f, (wave - throwerUnlockWave + 1) * 0.03f) : 0f;
        float tankW    = (wave >= tankUnlockWave && !tanksCapped) ? Mathf.Min(0.15f, (wave - tankUnlockWave + 1) * 0.02f) : 0f;

        // Regular fills the remainder, never dropping below 40% of the pool.
        float regularW = Mathf.Max(0.40f, 1f - runnerW - throwerW - tankW);
        float total    = regularW + runnerW + throwerW + tankW;
        float roll     = Random.value * total;

        if (roll < regularW)             return ZombieType.Regular;
        roll -= regularW;
        if (roll < runnerW)              return ZombieType.Runner;
        roll -= runnerW;
        if (roll < throwerW)             return ZombieType.Thrower;
        return ZombieType.Tank;
    }

    // ── Stat baselines (must match ZombieSpawner's built-in fallbacks) ──────

    private static float BaseHealth(ZombieType t) => t switch
    {
        ZombieType.Regular => 100f,
        ZombieType.Tank    => 500f,
        ZombieType.Runner  =>  60f,
        ZombieType.Thrower =>  80f,
        _                  => 100f
    };

    private static float BaseSpeed(ZombieType t) => t switch
    {
        ZombieType.Regular => 3.5f,
        ZombieType.Tank    => 2.0f,
        ZombieType.Runner  => 7.0f,
        ZombieType.Thrower => 3.0f,
        _                  => 3.5f
    };

    // ══════════════════════════════════════════════
    //  Spawn point selection
    // ══════════════════════════════════════════════

    private Vector3 PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No spawn points assigned — spawning at world origin.");
            return Vector3.zero;
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }

    // ══════════════════════════════════════════════
    //  Netcode / offline abstraction helpers
    //  These let the wave logic stay the same whether
    //  Netcode is running or not.
    // ══════════════════════════════════════════════

    private bool IsAuthority()
    {
        if (_offlineMode) return true;
        return IsServer;
    }

    private void SetState(WaveState s)
    {
        if (_offlineMode) { _localState = s; return; }
        _waveState.Value = s;
    }

    private WaveState GetState()
    {
        return _offlineMode ? _localState : _waveState.Value;
    }

    private void SetAlive(int v)
    {
        if (_offlineMode) { _localAlive = v; OnZombieCountChanged?.Invoke(v, _localTotal); return; }
        _aliveZombies.Value = v;
    }

    private int GetAlive()
    {
        return _offlineMode ? _localAlive : _aliveZombies.Value;
    }

    private void SetTotal(int v)
    {
        if (_offlineMode) { _localTotal = v; return; }
        _totalThisWave.Value = v;
    }

    private void SetCountdown(int v)
    {
        if (_offlineMode) { OnCountdownTick?.Invoke(v); return; }
        _countdownSeconds.Value = v;
    }

    private int IncrementWave()
    {
        if (_offlineMode) { return ++_localWave; }
        _currentWave.Value++;
        OnWaveStarted?.Invoke(_currentWave.Value);
        return _currentWave.Value;
    }

    private int GetWave()
    {
        return _offlineMode ? _localWave : _currentWave.Value;
    }

    // Offline-only mirror state (not synced, single-player only).
    private WaveState _localState = WaveState.Idle;
    private int       _localWave;
    private int       _localAlive;
    private int       _localTotal;

    // Offline entry point.
    private void BeginWavesOffline()
    {
        if (_localState != WaveState.Idle) return;
        StartNextWave();
    }

    private void WireUpSpawner()
    {
        if (ZombieSpawner.Instance == null)
        {
            Debug.LogError("[WaveManager] ZombieSpawner.Instance not found. " +
                           "Make sure a ZombieSpawner is in the scene.");
            return;
        }
        ZombieSpawner.Instance.OnZombieDied += HandleZombieDied;
    }

    private void UnwireSpawner()
    {
        if (ZombieSpawner.Instance != null)
            ZombieSpawner.Instance.OnZombieDied -= HandleZombieDied;
    }

    // ══════════════════════════════════════════════
    //  Utility
    // ══════════════════════════════════════════════

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
