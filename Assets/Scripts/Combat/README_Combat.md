# Combat / Gunplay Module

Drop-in gunplay for the top-down zombie shooter. Built-in render pipeline, Unity 6, Netcode for GameObjects 2.x.

## Files
- `WeaponData.cs` – ScriptableObject (per-gun stats: damage, fireRate, mag, recoil, light, tracer)
- `WeaponController.cs` – Player input, swap, fire, reload, recoil, network RPCs
- `Health.cs` – Networked HP with team + friendly-fire toggle
- `IDamageable.cs` – Damage interface
- `MuzzleFlash.cs` – Brief light pop at gun tip
- `PlayerVisionLight.cs` – Persistent dim spotlight in front of player
- `BulletTracer.cs` – Pooled `LineRenderer` tracer
- `CameraShake.cs` – Layered camera kick that does not fight `PlayerMovement`
- `WeaponHUD.cs` – Auto-built canvas: weapon name, ammo, reload bar, crosshair

## Setup (5 minutes)

### 1. Create the three weapons
In the Project window: **Right-click → Create → ZombieGame → Weapon Data**. Make 3 assets and tune as desired.

Suggested starting values:

| Field                | Pistol | Shotgun | Assault Rifle |
|----------------------|--------|---------|---------------|
| `kind`               | Pistol | Shotgun | AssaultRifle  |
| `damagePerPellet`    | 24     | 12      | 16            |
| `pelletsPerShot`     | 1      | 8       | 1             |
| `fireRate` (rps)     | 4      | 1.2     | 9             |
| `automatic`          | false  | false   | true          |
| `magazineSize`       | 12     | 6       | 30            |
| `reserveAmmoStart`   | 60     | 36      | 120           |
| `reloadSeconds`      | 1.2    | 2.2     | 1.8           |
| `baseSpreadDeg`      | 0.6    | 4.5     | 0.8           |
| `spreadPerShotDeg`   | 1.4    | 0.6     | 1.1           |
| `maxSpreadDeg`       | 6      | 8       | 7             |
| `spreadRecoverPerSec`| 8      | 6       | 6             |
| `cameraKick`         | 0.18   | 0.55    | 0.22          |
| `range`              | 45     | 22      | 70            |

### 2. Player object
On the existing **player** GameObject:

1. Add **`Health`** component
   - `teamId = 0` (players)
   - `friendlyFireEnabled = true` (per the README's "calling out attacks are essential")
   - `friendlyFireMultiplier = 0.5` (tunable)
2. Add **`WeaponController`**
   - Assign the 3 `WeaponData` assets to the **Loadout** array
   - Optional: set **Muzzle** to a child Transform in front of the player (auto-created if empty)
3. Add **`PlayerVisionLight`** (auto-creates the spotlight child)

### 3. Zombies / boss
On each enemy root:
- Add **`Health`** with `teamId = 1`
- Add a `NetworkObject` if you want server-authoritative damage (recommended)
- Make sure they have a `Collider`

### 4. Camera
The existing `PlayerMovement` writes the camera position every frame. `CameraShake` is added automatically by `WeaponController` to the local player's camera and runs in `LateUpdate`, so the shake is layered on top without being overwritten.

### 5. Layers (optional but recommended)
Put the player on a **Player** layer and uncheck it on `WeaponController.hittableMask`, or set `ownerTag` to your player tag. The controller already filters out colliders that are children of the shooter.

## Controls
- **LMB** – Fire (auto-fire on AR)
- **R** – Reload
- **1 / 2 / 3** – Swap weapons (Mouse Wheel also cycles)

## Lighting model
- `PlayerVisionLight` is a dim spotlight that gives a small forward-facing visibility cone at all times.
- `MuzzleFlash` pops a bright point light for ~50 ms on every shot — this is the "glimpse ahead from bullets" effect for the dark map.
- Ambient lighting itself is left to Jorge's lighting/shaders pass.

## Friendly fire
`Health.ApplyDamage` checks the source's `Health.teamId` against its own:
- Same team + `friendlyFireEnabled = false` → no damage.
- Same team + `friendlyFireEnabled = true` → damage scaled by `friendlyFireMultiplier`.
- Different team → full damage.

## Networking
- Server-authoritative health via `NetworkVariable<float>`.
- Local owner does instant client-side raycast for responsive tracers/flash.
- `RequestDamageRpc(SendTo.Server)` applies real damage.
- `BroadcastShotRpc(SendTo.NotMe)` mirrors muzzle flash + tracers to remote clients.
- All paths fall back to local-only when `NetworkManager` is not running, so editor playtests work without starting a host.
