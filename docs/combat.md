# Combat and game mode

Lasers, quarry and score: `CombatSystemModern`, `RocketModern` and `GameModeSinglePlayerModern`, ported from `VehicleMe.js`, `Rocket.js`, `Vehicle.js` and `Game.js`.

## First slice: fire, hit, score (2026-10-21)

Replaced the placeholders: the hitscan `PlayerLaserModern` with its debug-line laser, the `QuarryTarget` cylinder, the impact-stub spheres, and the placeholder laser settings (`laserCooldown`, `laserRange`, `laserImpactForce`, `laserProjectileSpeed`, `laserProjectileGravity`, `autoSelectQuarry`, `quarryScore`).

Ported:

- `CombatSystemModern` (`VehicleMe.js`): the Fire1 conditions, auto (`fR`) and snipe (`fS`) shots from the buggy's `RideCam` (the original `ridePos`) 0.1 m below, aimed by the camera in first person or with Fire2/Snipe held, else by `ridePos`. Snipe when Snipe is held with firepower above 1, or always with firepower above 2. Cooldowns 0.25 s auto, 2 s snipe, 0.5 s snipe at firepower 3.
- `RocketModern` (`Rocket.js`): launch speed `laserSpeed × speedX` (see the snipe audit below for the vehicle-speed term), lifetime `lerp(13, 5, laserSpeed × speedX × 0.003)` s, a ray swept from the last position each physics step, gravity, ricochet, launch-vehicle immunity, `OnLaserHit` broadcast, explosion force `350 + 300 × speedX` within 10 m, and the sparks prefab at the hit. Hits are processed nearest first; `RaycastAll` returned them unsorted.
- `GameModeSinglePlayerModern` (`Game.js` `eSI`, `Vehicle.js` `sQ`, `iS`, `dS`): someone is always quarry, so the player alone "Defaulted to Quarry"; a quarry's tag scores +1 with a 3 s guard per target; tagging the quarry makes the shooter quarry; tagging a non-quarry as a non-quarry scores −1. Quarry state (`isIt`, `lastTag`, `score`, `isResponding`) is on `VehicleStateModern`. The HUD shows DST only when the player is not quarry, as in `Vehicle.js`.
- Settings: `laserSpeed` 180, `laserGrav` 0, `laserRico` 0, `lasersFatal` and `lasersOptHit` off (`Settings.js` defaults).

Values read from the binary `LaserAuto.prefab` and `LaserSnipe.prefab` (Unity 2.x serialisation with a type tree; parsed by hand): `speedX` 1 (auto) and 10 (snipe); `mask` Default and LaserHit (`0x1001`); `maskOpt` adds LaserOpt (`0x3001`). The `explosion` reference did not decode cleanly; each prefab's only external prefab is `PlasmaSparks.prefab` or `PlasmaSparksSnipe.prefab`, so those are used (inference). The prefabs are bound on `Game` by the editor, like the existing HUD assets.

Verification (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init` into Fort Boxy): `tools/compile-check.sh` passes. `Tools -> MarsXPLR -> Run Laser Test` (`Editor/LaserTest.cs`, log `Unity6/Logs/laser-test.txt`) fires through `CombatSystemModern.Fire`, because synthetic key presses do not reach the Game view:

- the player reports `isIt=1`, quarry is `Player`;
- auto and snipe shots hit the Fort Boxy floor (`cube`) and spawn one sparks prefab each;
- three auto shots at a temporary kinematic target vehicle 25 m ahead hit it: score 0 → 1, then 1 → 1 for a second hit 1.1 s later (3 s guard), then 1 → 2 after 3.2 s;
- no exceptions during the run.

Not verified or not ported:

- Unity warns about the missing `Rocket.js` and `SoundEffect.js` scripts on each bolt and sparks instance.
- No comparison with the Wine version yet (bolt speed and look, impact effect, sound).
- Weapons lock and lock-on firing: ported on 2026-10-23 (see below).
- Bots, ramming (`OnRam`), fatal laser hits (`lR`), score display in name tags, and `scoreTime`.

## Manual firing, sparks and RideCam (2026-10-21)

Firing by hand did nothing: no bolt, sparks or sound. `CombatSystemModern` fires only when the vehicle has a `ridePos` (as `VehicleMe.js` did), and the spawned buggy had none. The scripted test called `Fire` directly, so it missed this. Decoding the original `unity/` `Buggy.prefab` (binary) shows `VehicleData.ridePos` is the `RideCam` child of the prefab root at (0, 0.375, 0). `SinglePlayerPlayableBootstrap` now adds a `RideCam` child at that offset. This also moves the first-person camera, which had fallen back to 1.5 m above the root, down to the original eye point, with the buggy's nose in view.

The sparks' legacy `EllipsoidParticleEmitter`, `ParticleAnimator` and `ParticleRenderer` were dropped on import, so they drew nothing. `PlasmaSparksModern` rebuilds them as a `ParticleSystem` from the decoded values: a one-shot burst of 4 (auto) or 8 (snipe) particles, size 0.4–0.7, random velocity in a ±1.5 m/s horizontal box, 0.8 s or 1.3 s life, a 1 m/s² downward force, size growing as 3^t or 5^t, alpha 10, 180, 255, 180, 10 over life, stretched rendering (length scale 1.5, maximum size 1 or 2), with the Plasma Green or Plasma Red material. Each instance is destroyed after its particles and hit sound end, replacing the missing `SoundEffect.js` and the animator's autodestruct.

Other values decoded from the binary prefabs (trail widths, materials and clips match the Unity 6 imports; pitch was not checked there): the bolts' trails (Plasma Green, 0.5 → 0.2 m; Plasma Red, 2 → 1 m; 1 s) and their `LaserShot` sources (pitch 1.3 auto, 0.5 snipe), the sparks' `LaserHit` sources, and `Rocket.js` `explosion`, which is the matching sparks prefab (now confirmed, no longer an inference).

Verified (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init`): holding the left mouse button in first person fires; a green glow shows at the crosshair; bolts and sparks exist in the scene; no exceptions. The bolt and sparks sources were reported playing in the scripted test. The sound has not been confirmed by ear, and the look has not been compared with the Wine version.

Wing shadows: the wings do not cast shadows, and the original's did not either. `Buggy.prefab` enables shadow casting on the wing renderer, but its `GlassGlowing` shader (same in `unity/`) is transparent with no shadow-caster pass and no fallback, so no shadow was drawn.

## Impact sound and buggy shadows (2026-10-21)

Impact sound: in the Wine version hits are not heard, while the port played them at full volume. The original `LaserHit.mp3` and `LaserShot.mp3` were imported as 3D sounds (`3D: 1` in the `unity/` import settings), so the sparks' hit sound faded with distance on the original rolloff curve (rolloff factor 0.5: about 48% at 3.2 m, 7% at 25.6 m, under 1% at 400 m). Unity 6 dropped that import flag and left the sparks' and the snipe bolt's sources 2D (`spatialBlend` 0). `PlasmaSparksModern` and `CombatSystemModern` now set `spatialBlend` to 1. That alone left the hits loud: the sparks' imported rolloff curves keep their keys in metres (0–500), but Unity 6 reads the curve's time as a fraction of `maxDistance`, so the curve stayed near 1 at any real distance (the auto bolt's curve was imported correctly, 0–1). `PlasmaSparksModern.Make3D` rescales such curves to 0–1. Verified with `Run Laser Test` (Unity `6000.3.26f1`, macOS `27.0.1`, from `Init`): the sparks' gain is 0.08–0.10 at 20–23 m and 0.02–0.03 at 100 m, which matches the original curve. Not yet compared by ear with the Wine version.

Buggy shadows, checked in code (Unity `6000.3.26f1`):

- Wings: no shadow, as in the original (see above).
- Axles: `Buggy Axel.prefab` casts and receives shadows in both `unity/` and Unity 6, and `BuggyPresentationModern` instantiates it without changing that. Its `Vehicle Chrome` material uses `Legacy Shaders/Reflective/Parallax Specular`, the same shader as the body, which gets its shadow-caster pass from its fallback chain. Nothing in the port disables the axle shadows, and they were confirmed visible by eye on 2026-10-21.

## Snipe shot audit against the source (2026-10-22)

Line-by-line comparison of `VehicleMe.js` firing, `Vehicle.js` `fS()`, `Rocket.js`, the `LaserSnipe` and `PlasmaSparksSnipe` prefabs and `Settings.js`/`Game.js` with `CombatSystemModern` and `RocketModern`.

Matches the source: spawn at `ridePos.position − 0.1 × vehicle up`; aim from the main camera in ride mode or with Fire2/Snipe held, else `ridePos.rotation` (the source sends `rang.eulerAngles` and rebuilds it with `Quaternion.Euler`, the same rotation); snipe when Snipe is held with firepower above 1, or always above 2; cooldown 2 s, or 0.5 s at firepower 3; speed `laserSpeed × 10` = 1800 m/s; gravity `laserGrav × speedX × dt × 20` off the vertical velocity each physics step (`Time.deltaTime` is the fixed step inside `FixedUpdate`); ricochet formula; lifetime `lerp(13, 5, 5.4)` = 5 s; a ray from the previous position to the current one each physics step, masks `0x1001` / `0x3001`; launch-vehicle immunity; explosion force and sparks at the hit. The bolt prefab's rigidbody has no gravity, no damping and no collider, so only `Rocket.js` moves it.

Differences found and fixed:

- Inherited speed: `Rocket.js` adds `launchVehicle.velocity` (forward component), a `Vehicle.js` field that only `VehicleNet.js` writes, for remote vehicles. The firer's own vehicle has no `VehicleNet`, so on the firing (authoritative) instance the term was always 0. The port used the rigidbody's velocity, making the player's bolts up to the buggy's speed faster. Now 0.
- Several hits on one step: `Rocket.js` handles every `RaycastAll` hit on the step's ray, even after one stops the bolt (at 36 m a step, a snipe can spark on two objects at once). The port stopped at the first. It now handles all of them, nearest first.
- Hit count: the port's 16-hit `RaycastNonAlloc` buffer could drop hits; it is now `RaycastAll`.
- Chat: the port would not fire while chatting; the source's fire condition has no such check. Removed.

Engine differences, with their Unity 6 equivalents:

- `RaycastAll` order: undefined in Unity 2.x's PhysX; the port sorts by distance so the result is repeatable.
- Triggers: Unity 2.x rays hit triggers; the port passes `QueryTriggerInteraction.Collide` (the project's `queriesHitTriggers` is also on).
- `rigidbody.velocity` is `linearVelocity`.
- Fixed step: the original `TimeManager` stores 0.002 s, and `Settings.js` sets 0.02 s (or 0.025 s at low render levels) at run time, as `Settings.cs` does; the test ran at 0.02 s.
- `laserSpeed` is an `int` in `Settings.js` and a `float` in `Settings.cs`; equal for the default 180, and only a server setting could differ.

Not ported: lag extrapolation (`lag`), which only applied on remote instances.

Verification (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init` into Fort Boxy): `tools/compile-check.sh` passes. `Tools -> MarsXPLR -> Run Snipe Trajectory Test` (`Editor/SnipeTrajectoryTest.cs`, log `Unity6/Logs/snipe-trajectory-test.txt`) passed:

- launch speed 1800.00 m/s;
- a level shot at boxes with faces 298 m and 310 m away, both within step 9 (288–324 m): the bolt stops at 310.00 m and spawns 2 sparks, as `Rocket.js` would;
- a 45° shot with `laserGrav` 1: 548 samples on whole physics steps, worst height error 0.001 m against `y = v0·dt·k − 4·dt·k(k+1)/2`;
- destroyed after 5.01 s (source 5 s).

The editor must be in front for the test to run. Not compared by eye with the Wine version.

Wine comparison follow-up (2026-10-22, same editor and macOS): two subtle differences were reported, the bolt slightly right of the crosshair centre and a flat-looking snipe trail. `Run Snipe Trajectory Test` now also fires a first-person shot as `CombatSystemModern.Update` does. The camera sits exactly on `ridePos`, and the bolt is 0.1 px right of and 1 px below the screen centre at 36 m. The path is centred, so the offset is not in the aim or spawn code. The bolt is only its `TrailRenderer` (no mesh), which is camera-facing (`alignment` View, width 2 → 1 m, Stretch). Both differences are therefore in how the trail ribbon is built and textured. Unity 2.x's trail generation is not documented and cannot be read from the source, so this remains open. The original crosshair is drawn at `centre − cursorOffset` (16, 16) with a 64 px texture whose mark is centred at 32 px, while the port centres it; the effect of the original GUI skin on that placement is unconfirmed.

Crosshair offset (2026-10-23, Unity `6000.3.26f1`, macOS 27.0 / Darwin 27.0.0): the port drew the 64 px crosshair with `GUI.Label` in a 64 px rect. Unity 6's built-in game label style pads 3 px top and bottom (none at the sides) and aligns upper-left, so the texture was shrunk to 58 px and its mark sat 3 px left of the screen centre, where the bolt actually goes (measured within 1 px of the centre). That explains the bolt looking slightly right of the crosshair. `Game.cs` now uses `GUI.DrawTexture`, which places the mark exactly at the centre at its full 64 px. The editor compiles with no console errors; not yet compared by eye with Wine. The original drew at `centre − cursorOffset` (16, 16) with the Unity 2.x label style, whose padding is unknown, so the crosshair's size in Wine should be checked by eye.

Snipe trail look (2026-10-23, Unity `6000.3.26f1`, macOS 27.0 / Darwin 27.0.0): the bolt's only visual is the prefab's `TrailRenderer`, and its settings carry over from the original: 1 s, 2 → 1 m wide, 3 m minimum vertex distance, no caps, a 5-key alpha fade, no shadows, Stretch UVs, Plasma Red on `Legacy Shaders/Particles/Additive` (64 px soft round glow, `_TintColor` 0.5 grey). Unity 6 builds it camera-facing (`View`). From the eye 0.1 m above the bolt's line, a camera-facing ribbon is a wide horizontal band, which matches the flat look reported. How Unity 2.x oriented trail ribbons is not documented and cannot be read from `unity/`, so the reported twist in Wine is not explained yet. Rendering a test frame through the Unity MCP failed: Play mode does not advance while the editor is in the background, and a hand-built trail draws nothing until its own update runs. Next step: a Wine reference (screenshot or clip from first person and from the side) to compare with.

Snipe trail frames (2026-10-23, Unity `6000.3.26f1`, macOS 27.0 / Darwin 27.0.0): captured through the Unity MCP by pausing straight after a snipe shot, stepping five frames and rendering cameras placed around the trail's own points. Play mode only advances with the editor in front. From the side, the trail is a soft red glowing band about 1–2 m thick that reads as a cylinder. From behind and above, the part near the shooter has faded out (the alpha gradient is near zero at the old end), and only the far part shows as a thin streak. From the firing eye, 0.1 m above the line, the camera-facing strip is seen almost edge-on. So the flat look is specific to looking along the bolt. Accepted as it is (2026-10-23); no Wine comparison planned.

## Weapons lock and lock-on shots (2026-10-23)

Ported from `VehicleMe.js` (weapons locking and firing), `Vehicle.js` (`fRl`, `fSl`, `laserLock` in `OnPrefsUpdated`) and `Rocket.js` (auto-targeting):

- `LaserLockModern`: every vehicle's invisible `LaserLock` trigger sphere (from `Vehicle.prefab`: radius 0.5, layer 14 `LaserLock`), scaled to `(laserLock + camOffset × 0.1) × 10` (6, a 3 m radius, at the default 0.4) and off when the setting is 0. The port's visible green placeholder sphere for it is removed; the original drew nothing.
- `CombatSystemModern`: each frame, when `laserLocking` is on, a ray along the vehicle's nose from `(laserLock + camOffset × 0.1) × 15` m ahead (9 m by default), layer 14 only, infinite range; it locks on when the shooter or the vehicle hit is quarry. While locked the gun fires on its own, with no button, from the usual muzzle. A lock-on shot is a snipe at firepower 3, or at firepower 2 when the target is over 500 m away or faster than √500 ≈ 22.4 m/s. Cooldowns are unchanged.
- `RocketModern`: a lock-on bolt waits at the muzzle for 5 physics steps, samples the target's velocity before and after, then steps the target's position forward (with that acceleration) until the bolt could reach it within the elapsed time, or 10 s, and aims there. This is `Rocket.js`'s local-vehicle branch, the only one offline.
- `LegacyLaserSightController` swaps the green and red aimers on that lock instead of its own placeholder quarry ray.

The aimer and the lock follow the vehicle's nose, not the crosshair. In first person the camera turns with the mouse (`CameraVehicle.js` adds mouse rotation to `ridePos`), but nothing turns `ridePos`, `LaserAim` or the lock ray, and the aimer particles are pushed forwards in the vehicle's local space. Only manual shots follow the crosshair.

Verified (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init`): `tools/compile-check.sh` passes. `Tools -> MarsXPLR -> Run Lock-on Test` (`Editor/LockOnTest.cs`, log `Unity6/Logs/lock-on-test.txt`) holds the player 200 m up and puts a target vehicle on its nose line 150 m ahead, moving sideways at 10 m/s. It passed: no lock with nothing ahead; lock and an automatic auto shot 0.02 s after the target appeared; the bolt led the target by 3.20° (about 0.83 s × 10 m/s across 150 m) and hit it, scoring 0 → 1; at 30 m/s the lock-on shot was a snipe. `Run Snipe Trajectory Test` still passes. `Run Laser Test` still scores tags correctly; its floor shots went elsewhere because the earlier tests had left the buggy moved and tilted. Offline there is still nothing to lock on to until bots are ported, as in the original single player without bots.
