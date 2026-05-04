using System;
using Unity.Netcode;
using UnityEngine;

namespace ZombieGame.Combat
{
    public class Health : NetworkBehaviour, IDamageable
    {
        [Header("Identity")]
        [Tooltip("0 = Players (friendly to each other if FriendlyFire is off), 1 = Zombies, etc.")]
        public int teamId = 0;

        [Header("Stats")]
        public float maxHealth = 100f;

        [Header("Rules")]
        [Tooltip("If false, same-team damage is ignored entirely.")]
        public bool friendlyFireEnabled = true;
        [Tooltip("Multiplier applied when friendly fire is enabled and the source shares the team.")]
        [Range(0f, 1f)] public float friendlyFireMultiplier = 0.5f;

        public event Action<float, float> OnHealthChanged;
        public event Action<GameObject> OnDied;

        float _offlineHp;
        readonly NetworkVariable<float> _hp = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int TeamId => teamId;
        public bool IsAlive => CurrentHealth > 0f;
        public float CurrentHealth => IsSpawned ? _hp.Value : _offlineHp;
        public float MaxHealth => maxHealth;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _hp.Value = maxHealth;
            _hp.OnValueChanged += HandleHpChanged;
            OnHealthChanged?.Invoke(_hp.Value, maxHealth);
        }

        public override void OnNetworkDespawn()
        {
            _hp.OnValueChanged -= HandleHpChanged;
        }

        void Awake()
        {
            // Do not write NetworkVariables before Netcode has spawned this behaviour.
            // Offline play uses the default value until damage is applied.
            _offlineHp = maxHealth;
        }

        void HandleHpChanged(float prev, float next)
        {
            OnHealthChanged?.Invoke(next, maxHealth);
            if (prev > 0f && next <= 0f) OnDied?.Invoke(gameObject);
        }

        public void ApplyDamage(float amount, GameObject source, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive || amount <= 0f) return;

            int sourceTeam = -1;
            if (source != null)
            {
                var srcHealth = source.GetComponentInParent<Health>();
                if (srcHealth != null) sourceTeam = srcHealth.teamId;
            }

            if (sourceTeam == teamId)
            {
                if (!friendlyFireEnabled) return;
                amount *= friendlyFireMultiplier;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (IsServer) ApplyDamageInternal(amount);
                else ApplyDamageServerRpc(amount);
            }
            else
            {
                ApplyDamageInternal(amount);
            }
        }

        [Rpc(SendTo.Server)]
        void ApplyDamageServerRpc(float amount)
        {
            ApplyDamageInternal(amount);
        }

        void ApplyDamageInternal(float amount)
        {
            if (IsSpawned)
            {
                _hp.Value = Mathf.Max(0f, _hp.Value - amount);
            }
            else
            {
                float prev = _offlineHp;
                _offlineHp = Mathf.Max(0f, _offlineHp - amount);
                HandleHpChanged(prev, _offlineHp);
            }
        }

        public void ServerHeal(float amount)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer) return;
            _hp.Value = Mathf.Min(maxHealth, _hp.Value + Mathf.Max(0f, amount));
        }
    }
}
