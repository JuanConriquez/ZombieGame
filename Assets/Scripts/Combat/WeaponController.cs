using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Drives gunplay for a player: input, swap, fire, reload, recoil/spread,
    /// muzzle flash, tracers, and damage application. Server-authoritative when
    /// running under NetworkManager; falls back to local-only if not networked.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class WeaponController : NetworkBehaviour
    {

       //adding sounds to the gun script itself  
    public AudioSource gunAudioSource;
    public AudioClip pistolSound;
    public AudioClip shotgunSound;
    public AudioClip rifleSound;
    public AudioClip reloadSound;
    public float gunshotVolume = 1f;


        [Header("Loadout (1, 2, 3 keys)")]
        public WeaponData[] loadout = new WeaponData[3];
        public int startingIndex = 0;

        [Header("References")]
        [Tooltip("Where bullets/tracers originate. If null, a child 'Muzzle' is created in front of the player.")]
        public Transform muzzle;
        [Tooltip("Camera the local player uses (for shake). If null, Camera.main is used.")]
        public Camera playerCamera;

        [Header("Hit Filtering")]
        public LayerMask hittableMask = ~0;
        [Tooltip("Tag treated as 'self' to ignore on raycasts.")]
        public string ownerTag = "";

        [Header("Input")]
        [Tooltip("Show HUD for this player object. Leave enabled for the current single-player scene setup.")]
        public bool showHud = true;
        public KeyCode reloadKey = KeyCode.R;
        public KeyCode slot1 = KeyCode.Alpha1;
        public KeyCode slot2 = KeyCode.Alpha2;
        public KeyCode slot3 = KeyCode.Alpha3;
        public int fireMouseButton = 0;

        // Runtime state
        readonly List<WeaponRuntime> _runtime = new List<WeaponRuntime>();
        int _currentIndex = 0;
        float _nextFireTime;
        float _currentSpreadDeg;
        bool _isReloading;
        float _reloadEndsAt;

        Health _health;
        WeaponHUD _hud;
        CameraShake _shake;
        MuzzleFlash _muzzleFlash;
        SimplePlayerGunVisuals _visuals;

        WeaponRuntime Current => _runtime[_currentIndex];
        public int CurrentWeaponIndex => _currentIndex;
        public WeaponData ActiveWeaponData => _runtime.Count == 0 ? null : Current.Data;

        void Awake()
        {
            _health = GetComponent<Health>();

            if (gunAudioSource == null)
            {
            gunAudioSource = GetComponent<AudioSource>();

            if (gunAudioSource == null)
            gunAudioSource = gameObject.AddComponent<AudioSource>();
            }
            gunAudioSource.playOnAwake = false;
            gunAudioSource.spatialBlend = 1f;

            if (muzzle == null)
            {
                var go = new GameObject("Muzzle");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.0f, 0.6f);
                muzzle = go.transform;
            }

            _muzzleFlash = muzzle.GetComponent<MuzzleFlash>();
            if (_muzzleFlash == null) _muzzleFlash = muzzle.gameObject.AddComponent<MuzzleFlash>();

            BuildRuntime();

            _visuals = GetComponent<SimplePlayerGunVisuals>();
            if (_visuals == null) _visuals = gameObject.AddComponent<SimplePlayerGunVisuals>();
        }

        void PlayGunshotSound(WeaponData data)
{
    if (gunAudioSource == null || data == null) return;

    AudioClip clip = null;

    switch (data.kind)
    {
        case WeaponKind.Shotgun:
            clip = shotgunSound;
            break;

        case WeaponKind.AssaultRifle:
            clip = rifleSound;
            break;

        default:
            clip = pistolSound;
            break;
    }

    if (clip != null)
        gunAudioSource.PlayOneShot(clip, gunshotVolume);
    }


        void Start()
        {
            if (showHud)
            {
                _hud = gameObject.AddComponent<WeaponHUD>();
                RefreshHud();
            }

            // Camera shake only needs to exist for whichever camera is assigned here.
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera != null)
            {
                _shake = playerCamera.GetComponent<CameraShake>();
                if (_shake == null) _shake = playerCamera.gameObject.AddComponent<CameraShake>();
            }
        }

        bool IsLocalAuthority()
        {
            // Treat the editor as "owner" when not running networked yet.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return true;
            return IsOwner;
        }

        void BuildRuntime()
        {
            _runtime.Clear();
            if (loadout == null || loadout.Length == 0)
            {
                Debug.LogWarning($"{name}: WeaponController has no loadout assigned.");
                return;
            }
            for (int i = 0; i < loadout.Length; i++)
            {
                var data = loadout[i];
                if (data == null) continue;
                _runtime.Add(new WeaponRuntime
                {
                    Data = data,
                    Magazine = data.magazineSize,
                    Reserve = data.reserveAmmoStart
                });
            }
            _currentIndex = Mathf.Clamp(startingIndex, 0, Mathf.Max(0, _runtime.Count - 1));
        }

        void Update()
        {
            if (!IsLocalAuthority()) return;
            if (_runtime.Count == 0) return;
            if (!_health.IsAlive) return;

            HandleSwap();
            HandleReload();
            HandleFire();

            // Spread bleed-off
            _currentSpreadDeg = Mathf.Max(
                Current.Data.baseSpreadDeg,
                _currentSpreadDeg - Current.Data.spreadRecoverPerSec * Time.deltaTime);

            UpdateHud();
        }

        void HandleSwap()
        {
            int target = _currentIndex;
            if (Input.GetKeyDown(slot1) && _runtime.Count > 0) target = 0;
            else if (Input.GetKeyDown(slot2) && _runtime.Count > 1) target = 1;
            else if (Input.GetKeyDown(slot3) && _runtime.Count > 2) target = 2;

            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel > 0.05f) target = (_currentIndex + 1) % _runtime.Count;
            else if (wheel < -0.05f) target = (_currentIndex - 1 + _runtime.Count) % _runtime.Count;

            if (target != _currentIndex)
            {
                _currentIndex = target;
                _isReloading = false;
                _currentSpreadDeg = Current.Data.baseSpreadDeg;
                if (_visuals != null) _visuals.RefreshNow();
                RefreshHud();
            }
        }

        void HandleReload()
        {
            if (_isReloading)
            {
                if (Time.time >= _reloadEndsAt) FinishReload();
                return;
            }

            bool wantReload = Input.GetKeyDown(reloadKey)
                              || (Current.Magazine == 0 && Current.Reserve > 0
                                  && Input.GetMouseButtonDown(fireMouseButton));
            if (wantReload && Current.Magazine < Current.Data.magazineSize && Current.Reserve > 0)
                StartReload();
        }

        void StartReload()
        {
            _isReloading = true;
            _reloadEndsAt = Time.time + Current.Data.reloadSeconds;

            if (gunAudioSource != null && reloadSound != null)
        gunAudioSource.PlayOneShot(reloadSound, gunshotVolume);
        }

        void FinishReload()
        {
            int needed = Current.Data.magazineSize - Current.Magazine;
            int taken = Mathf.Min(needed, Current.Reserve);
            Current.Magazine += taken;
            Current.Reserve -= taken;
            _isReloading = false;
            RefreshHud();
        }

        void HandleFire()
        {
            if (_isReloading) return;
            var data = Current.Data;
            bool pressed = data.automatic
                ? Input.GetMouseButton(fireMouseButton)
                : Input.GetMouseButtonDown(fireMouseButton);
            if (!pressed) return;
            if (Time.time < _nextFireTime) return;
            if (Current.Magazine <= 0) return;

            FireOnce();
        }

        void FireOnce()
        {
            var data = Current.Data;
            _nextFireTime = Time.time + 1f / Mathf.Max(0.01f, data.fireRate);
            Current.Magazine -= 1;

            Vector3 origin = muzzle.position;
            Vector3 forward = transform.forward; // top-down player aims along forward via PlayerMovement

            // Per-shot recoil bump
            _currentSpreadDeg = Mathf.Min(data.maxSpreadDeg, _currentSpreadDeg + data.spreadPerShotDeg);
            if (_shake != null) _shake.AddKick(data.cameraKick);

            // Local visuals immediately for responsiveness.
            DoLocalShotVisuals(origin, forward, data);

            // Resolve hits locally for instant feedback; ask server to apply damage.
            for (int p = 0; p < Mathf.Max(1, data.pelletsPerShot); p++)
            {
                Vector3 dir = ApplySpread(forward, _currentSpreadDeg);
                Vector3 endPoint = origin + dir * data.range;

                if (TryGetFirstNonSelfHit(origin, dir, data.range, out RaycastHit hit))
                {
                    endPoint = hit.point;

                    var dmg = hit.collider.GetComponentInParent<IDamageable>();

                    // ── DEBUG ──────────────────────────────────────────────
                    Debug.Log($"[Gun] Ray hit: '{hit.collider.gameObject.name}' " +
                              $"(layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) " +
                              $"| IDamageable found: {dmg != null}" +
                              (dmg != null ? $" | type: {dmg.GetType().Name}" : " ← no IDamageable on this object or its parents"));
                    // ──────────────────────────────────────────────────────

                    if (dmg != null)
                    {
                        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                        {
                            var netObj = (dmg as Component)?.GetComponentInParent<NetworkObject>();
                            if (netObj != null)
                                RequestDamageRpc(netObj.NetworkObjectId, data.damagePerPellet, hit.point, hit.normal);
                        }
                        else
                        {
                            dmg.ApplyDamage(data.damagePerPellet, gameObject, hit.point, hit.normal);
                        }
                    }
                }
                else
                {
                    // ── DEBUG ──────────────────────────────────────────────
                    Debug.Log($"[Gun] Ray from {origin} dir {dir} — hit NOTHING within {data.range}m " +
                              $"(hittableMask: {hittableMask.value})");
                    // ──────────────────────────────────────────────────────
                }

                BulletTracer.Instance.Spawn(origin, endPoint, data.tracerColor, data.tracerSeconds, data.tracerWidth);
            }

            // Mirror visuals to other clients.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                BroadcastShotRpc(origin, forward, _currentSpreadDeg, _currentIndex);

            RefreshHud();
        }

        bool IsSelf(Collider c)
        {
            if (c == null) return false;
            if (c.transform.IsChildOf(transform)) return true;
            if (!string.IsNullOrEmpty(ownerTag) && c.CompareTag(ownerTag)) return true;
            return false;
        }

        Vector3 ApplySpread(Vector3 forward, float spreadDeg)
        {
            if (spreadDeg <= 0.001f) return forward;
            float yaw = Random.Range(-spreadDeg, spreadDeg);
            float pitch = Random.Range(-spreadDeg * 0.5f, spreadDeg * 0.5f);
            return Quaternion.AngleAxis(yaw, Vector3.up) *
                   Quaternion.AngleAxis(pitch, transform.right) *
                   forward;
        }

        void DoLocalShotVisuals(Vector3 origin, Vector3 forward, WeaponData data)
        {
            PlayGunshotSound(data);


            if (_muzzleFlash != null)
                _muzzleFlash.Flash(data.muzzleLightColor, data.muzzleLightIntensity, data.muzzleLightRange, data.muzzleLightSeconds);
            if (_hud != null) _hud.PulseCrosshair(Mathf.InverseLerp(data.baseSpreadDeg, data.maxSpreadDeg, _currentSpreadDeg));
        }

        // ---- Networking ----

        [Rpc(SendTo.Server)]
        void RequestDamageRpc(ulong targetNetId, float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var nob)) return;
            var dmg = nob.GetComponent<IDamageable>();
            if (dmg == null) dmg = nob.GetComponentInChildren<IDamageable>();
            if (dmg == null) return;
            dmg.ApplyDamage(damage, gameObject, hitPoint, hitNormal);
        }

        /// <summary>
        /// Offline / local: refill reserves. networked: server only (use RequestCollectAmmoPickupServerRpc from clients).
        /// </summary>
        public void GrantAmmoBox()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer) return;
            ApplyAmmoBoxRefill();
            RefreshHud();
        }

        [Rpc(SendTo.Server)]
        public void RequestCollectAmmoPickupServerRpc(ulong pickupNetworkObjectId)
        {
            ApplyAmmoBoxRefill();
            RefreshHud();

            if (pickupNetworkObjectId == 0UL) return;
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pickupNetworkObjectId, out NetworkObject pickup)) return;
            pickup.Despawn(true);
        }

        void ApplyAmmoBoxRefill()
        {
            for (int i = 0; i < _runtime.Count; i++)
            {
                WeaponRuntime slot = _runtime[i];
                if (slot.Data == null) continue;
                slot.Reserve = Mathf.Max(slot.Reserve, slot.Data.reserveAmmoStart);
            }
        }

        [Rpc(SendTo.NotMe)]
        void BroadcastShotRpc(Vector3 origin, Vector3 forward, float spreadDeg, int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= _runtime.Count) return;
            var data = _runtime[weaponIndex].Data;
            if (data == null) return;

            if (_muzzleFlash != null)
                _muzzleFlash.Flash(data.muzzleLightColor, data.muzzleLightIntensity, data.muzzleLightRange, data.muzzleLightSeconds);

            for (int p = 0; p < Mathf.Max(1, data.pelletsPerShot); p++)
            {
                Vector3 dir = ApplySpread(forward, spreadDeg);
                Vector3 endPoint = origin + dir * data.range;
                if (TryGetFirstNonSelfHit(origin, dir, data.range, out RaycastHit hit))
                    endPoint = hit.point;
                BulletTracer.Instance.Spawn(origin, endPoint, data.tracerColor, data.tracerSeconds, data.tracerWidth);
            }
        }

        // ---- HUD glue ----

        void RefreshHud()
        {
            if (_hud == null) return;
            _hud.SetWeaponName(Current.Data.displayName);
            _hud.SetAmmo(Current.Magazine, Current.Reserve);
            UpdateWeaponSlots();
        }

        void UpdateHud()
        {
            if (_hud == null) return;
            if (_isReloading)
            {
                float t = 1f - Mathf.Clamp01((_reloadEndsAt - Time.time) / Mathf.Max(0.01f, Current.Data.reloadSeconds));
                _hud.SetReloadProgress(true, t);
            }
            else
            {
                _hud.SetReloadProgress(false, 0f);
            }
            _hud.SetAmmo(Current.Magazine, Current.Reserve);
            UpdateWeaponSlots();
        }

        void UpdateWeaponSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i >= _runtime.Count || _runtime[i].Data == null)
                {
                    _hud.ClearWeaponSlot(i);
                    continue;
                }

                var weapon = _runtime[i];
                _hud.SetWeaponSlot(i, weapon.Data.displayName, weapon.Magazine, weapon.Reserve, i == _currentIndex);
            }
        }

        bool TryGetFirstNonSelfHit(Vector3 origin, Vector3 dir, float range, out RaycastHit firstHit)
        {
            firstHit = default;

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, range, hittableMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                if (IsSelf(hits[i].collider)) continue;

                firstHit = hits[i];
                return true;
            }

            return false;
        }

        public bool TryGetWeaponSlotInfo(int index, out WeaponData data, out int magazine, out int reserve)
        {
            data = null;
            magazine = 0;
            reserve = 0;

            if (index < 0 || index >= _runtime.Count) return false;

            var slot = _runtime[index];
            data = slot.Data;
            magazine = slot.Magazine;
            reserve = slot.Reserve;
            return data != null;
        }

        class WeaponRuntime
        {
            public WeaponData Data;
            public int Magazine;
            public int Reserve;
        }
    }
}
