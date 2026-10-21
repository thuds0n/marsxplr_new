# Combat and game mode

Lasers, quarry and score: `CombatSystemModern`, `RocketModern` and `GameModeSinglePlayerModern`, ported from `VehicleMe.js`, `Rocket.js`, `Vehicle.js` and `Game.js`.

## First slice: fire, hit, score (2026-10-21)

Replaced the placeholders: the hitscan `PlayerLaserModern` with its debug-line laser, the `QuarryTarget` cylinder, the impact-stub spheres, and the placeholder laser settings (`laserCooldown`, `laserRange`, `laserImpactForce`, `laserProjectileSpeed`, `laserProjectileGravity`, `autoSelectQuarry`, `quarryScore`).

Ported:

- `CombatSystemModern` (`VehicleMe.js`): the Fire1 conditions, auto (`fR`) and snipe (`fS`) shots from the buggy's `RideCam` (the original `ridePos`) 0.1 m below, aimed by the camera in first person or with Fire2/Snipe held, else by `ridePos`. Snipe when Snipe is held with firepower above 1, or always with firepower above 2. Cooldowns 0.25 s auto, 2 s snipe, 0.5 s snipe at firepower 3.
- `RocketModern` (`Rocket.js`): launch speed `laserSpeed × speedX` plus the vehicle's forward speed, lifetime `lerp(13, 5, laserSpeed × speedX × 0.003)` s, a ray swept from the last position each physics step, gravity, ricochet, launch-vehicle immunity, `OnLaserHit` broadcast, explosion force `350 + 300 × speedX` within 10 m, and the sparks prefab at the hit. Hits are processed nearest first; `RaycastAll` returned them unsorted.
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
- Weapons lock and lock-on firing (`fRl`, `fSl`, lead targeting). Offline there is no other vehicle to lock on to, as in the original single player without bots.
- Bots, ramming (`OnRam`), fatal laser hits (`lR`), score display in name tags, and `scoreTime`.

## Manual firing, sparks and RideCam (2026-10-21)

Firing by hand did nothing: no bolt, sparks or sound. `CombatSystemModern` fires only when the vehicle has a `ridePos` (as `VehicleMe.js` did), and the spawned buggy had none. The scripted test called `Fire` directly, so it missed this. Decoding the original `unity/` `Buggy.prefab` (binary) shows `VehicleData.ridePos` is the `RideCam` child of the prefab root at (0, 0.375, 0). `SinglePlayerPlayableBootstrap` now adds a `RideCam` child at that offset. This also moves the first-person camera, which had fallen back to 1.5 m above the root, down to the original eye point, with the buggy's nose in view.

The sparks' legacy `EllipsoidParticleEmitter`, `ParticleAnimator` and `ParticleRenderer` were dropped on import, so they drew nothing. `PlasmaSparksModern` rebuilds them as a `ParticleSystem` from the decoded values: a one-shot burst of 4 (auto) or 8 (snipe) particles, size 0.4–0.7, random velocity in a ±1.5 m/s horizontal box, 0.8 s or 1.3 s life, a 1 m/s² downward force, size growing as 3^t or 5^t, alpha 10, 180, 255, 180, 10 over life, stretched rendering (length scale 1.5, maximum size 1 or 2), with the Plasma Green or Plasma Red material. Each instance is destroyed after its particles and hit sound end, replacing the missing `SoundEffect.js` and the animator's autodestruct.

Other values decoded from the binary prefabs (trail widths, materials and clips match the Unity 6 imports; pitch was not checked there): the bolts' trails (Plasma Green, 0.5 → 0.2 m; Plasma Red, 2 → 1 m; 1 s) and their `LaserShot` sources (pitch 1.3 auto, 0.5 snipe), the sparks' `LaserHit` sources, and `Rocket.js` `explosion`, which is the matching sparks prefab (now confirmed, no longer an inference).

Verified (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init`): holding the left mouse button in first person fires; a green glow shows at the crosshair; bolts and sparks exist in the scene; no exceptions. The bolt and sparks sources were reported playing in the scripted test. The sound has not been confirmed by ear, and the look has not been compared with the Wine version.

Wing shadows: the wings do not cast shadows, and the original's did not either. `Buggy.prefab` enables shadow casting on the wing renderer, but its `GlassGlowing` shader (same in `unity/`) is transparent with no shadow-caster pass and no fallback, so no shadow was drawn.
