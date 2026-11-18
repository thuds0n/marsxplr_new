# Combat and game mode

Lasers, quarry and score: `CombatSystemModern`, `RocketModern` and `GameModeSinglePlayerModern`, ported from `VehicleMe.js`, `Rocket.js`, `Vehicle.js` and `Game.js`.

## First slice: fire, hit, score (2026-10-21)

Replaced the placeholders: the hitscan `PlayerLaserModern` with its debug-line laser, the `QuarryTarget` cylinder, the impact-stub spheres, and the placeholder laser settings (`laserCooldown`, `laserRange`, `laserImpactForce`, `laserProjectileSpeed`, `laserProjectileGravity`, `autoSelectQuarry`, `quarryScore`).

Ported:

- `CombatSystemModern` (`VehicleMe.js`): the Fire1 conditions, auto (`fR`) and snipe (`fS`) shots from the buggy's `RideCam` (the original `ridePos`) 0.1 m below, aimed by the camera in first person or with Fire2/Snipe held, else by `ridePos`. Snipe when Snipe is held with firepower above 1, or always with firepower above 2. Cooldowns 0.25 s auto, 2 s snipe, 0.5 s snipe at firepower 3.
- `RocketModern` (`Rocket.js`): launch speed `laserSpeed × speedX` (see the snipe audit below for the vehicle-speed term), lifetime `lerp(13, 5, laserSpeed × speedX × 0.003)` s, a ray swept from the last position each physics step, gravity, ricochet, launch-vehicle immunity, `OnLaserHit` broadcast, explosion force `350 + 300 × speedX` within 10 m, and the sparks prefab at the hit. Hits are processed nearest first; `RaycastAll` returned them unsorted.
- `GameModeSinglePlayerModern` (`Game.js` `eSI`, `Vehicle.js` `sQ`, `iS`, `dS`): someone is always quarry (corrected 2026-11-17: the original creates the host as quarry with no message, and "Defaulted to Quarry" appears only after a bot is axed or a player leaves); a quarry's tag scores +1 with a 3 s guard per target; tagging the quarry makes the shooter quarry; tagging a non-quarry as a non-quarry scores −1. Quarry state (`isIt`, `lastTag`, `score`, `isResponding`) is on `VehicleStateModern`. The HUD shows DST only when the player is not quarry, as in `Vehicle.js`.
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

Verified (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init`): `tools/compile-check.sh` passes. `Tools -> MarsXPLR -> Run Lock-on Test` (`Editor/LockOnTest.cs`, log `Unity6/Logs/lock-on-test.txt`) holds the player 200 m up and puts a target vehicle on its nose line 150 m ahead, moving sideways at 10 m/s. It passed: no lock with nothing ahead; lock and an automatic auto shot 0.02 s after the target appeared; the bolt led the target by 3.20° (about 0.83 s × 10 m/s across 150 m) and hit it, scoring 0 → 1; at 30 m/s the lock-on shot was a snipe. `Run Snipe Trajectory Test` still passes. `Run Laser Test` still scores tags correctly; its floor shots went elsewhere because the earlier tests had left the buggy moved and tilted. Bots (below) are now the offline lock-on targets.

## Bots (2026-10-23)

Ported from `VehicleBot.js`, `Game.js` (`addBot`, `axeBot`, `iV`) and `Vehicle.js` (bot set-up and markers):

- `Game.addBot()` announces "<player> added a bot" (or "bot n"), waits 2 s, and spawns "<player>'s bot[ n]" at the base like any vehicle, up to 10. `Game.axeBot()` removes the newest bot with "<player> removed the last bot" or "bot n"; the game mode then makes the player quarry if nobody is.
- `SinglePlayerPlayableBootstrap` now spawns the player and bots through one `SpawnVehicle`. A bot gets the same buggy and physics, `VehicleBotModern` instead of keyboard input, a `LaserLock` sphere, the `Marker` minimap marker swapped for `Marker Quarry` while it is quarry (`VehicleMarkersModern`), the player's colours, no aimers, and a fresh 3 s tag guard. `VehicleStateModern.isBot` marks it. `CombatSystemModern` now reads keyboard and mouse input only on the player's vehicle, as `VehicleMe.js` existed only there.
- `VehicleBotModern`: every 2 s (its timer is an `int`, as in the source) it picks an enemy: the nearest vehicle while it is quarry, otherwise the quarry. The original's "Rabbit Hunt" branch is always taken. While quarry it hides: full throttle with random steering every frame. Otherwise it pursues: steering by the heading error / 20, clamped to ±0.6, or ±1 within 15 m, with a 0.1 dead zone, at full throttle with wings closed. The hovercraft, tank and jet branches are ported too, for when those vehicles are. Once a second, with line of sight past layer 8 (Lightmapped), it fires a lock-on auto shot (`fRl`) at its enemy, regardless of `lasersAllowed` and firepower, as in the source. Below −300 m it returns to the base with "<bot> fell off the planet". `botsCanDrive` and `botsCanFire` (default on) are in `Settings`.
- `Game.unity` binds `objectMarker` and `objectMarkerQuarry` (edited as text: three lines).

The original's Add Bot and Axe Bot buttons are in the Server Settings panel, ported on 2026-10-23 (see [ui.md](ui.md)); `Tools -> MarsXPLR -> Add Bot` and `Axe Bot` remain as editor shortcuts. Not ported: bot name tags (`Vehicle.js` `OnGUI`) and the materialisation effect (`Entry Effect.prefab`, which still uses `EntryPoint.js` and legacy particles).

Verified (Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode from `Init`): `tools/compile-check.sh` passes, and the editor console shows no errors. `Tools -> MarsXPLR -> Run Bot Test` (`Editor/BotTest.cs`, log `Unity6/Logs/bot-test.txt`) passed: no bot before 2 s; "Player's bot" spawned with bot controls, no aimer and a `LaserLock`; it pursued the player (82.6 m to 48.6 m), fired 3 lock-on shots and tagged the quarry 3.9 s after spawning, becoming quarry; it then drove on at full throttle; axing it removed it and the player became quarry again. A first run failed the tag check: the bot closed to 9.7 m and fired 19 lock-on shots in 20 s without a tag. Their sparks were around the player's buggy near the base, so they probably struck the floor short of the buggy's low centre, which is where the source aims as well. That is not confirmed. A later manual Add Bot tagged the player normally. Bots have not been played against by hand.

## Official-build laser measurements (2026-11-02)

Code check (Unity `6000.3.27f1`, macOS `27.2` / Darwin 27.2.0): `RocketModern` matches `Rocket.js`'s local-vehicle branch, which is the only one offline (bots and the player are all `networkView.isMine` on the host): it waits `Time.fixedDeltaTime * 5`, samples the target's rigidbody velocity before and after, and steps the prediction in `fixedDeltaTime`. The remote branch (lines 35 and 39) is networking-only and not ported. `Settings.cs` sets `fixedDeltaTime` to 0.02 above render level 3 and 0.025 otherwise, as `Settings.js` does, so the wait is 0.10 s at Fantastic. `CombatSystemModern`'s lock ray, lock-on fire rule and snipe choice match `VehicleMe.js`, and `fRl`/`fSl` match `Vehicle.js`. No difference found, so no code change. `tools/compile-check.sh` passes and the editor recompiles with no console errors.

`Editor/BotAccuracyLog.cs` (`Tools -> MarsXPLR -> Start/Stop Bot Accuracy Log`, log `Unity6/Logs/bot-accuracy.txt`) now also logs the player's own lock-on shots: delay from firing to launch, range, target speed, lead angle at launch, hit or miss and launch-to-impact time, with means at the end. Bot shots are logged as before (where each ends, closest pass, player tags).

Manual plan for the official build (`~/Applications/Sikarugir/Mars Explorer 2.22.app`), not yet run:

1. Run `tools/whirld-server.py`. Start the game at Fantastic quality. Host → Use Custom World → `http://127.0.0.1:8765/FortBoxy.utw`.
2. In Server Settings, leave laser lock and firepower at their defaults; add one bot.
3. Bot accuracy: switch to first person, park on open floor near the base and hold the handbrake (Command). For 60 s, count bot shots, how many tag you, and where the misses land (short on the floor, beside, over). Repeat once driving straight at full throttle across the bot's line.
4. Player homing: become quarry's hunter (or quarry), hold the bot on the nose in first person so the aimer turns red and the gun fires by itself. For 10 lock-on shots note: hit or miss, roughly how far the bolt flies before it turns towards the target (visible delay at the muzzle), and how long from the shot to the sparks (count frames in a screen recording if possible; macOS screen recording of the Wine window is fine).
5. Headlight: at night-side or in the castle's shade, first person and chase view, take a screenshot of the buggy's headlight pool on the floor at standstill, and one from the side.

Record the date, the game window size and the frame rate shown, and send the counts and screenshots. The port side is then run with the same steps in Play mode from `Init` with the log above; results go here.

First official-build session (2026-11-02, Mars Explorer 2.22 in the Sikarugir wrapper, macOS `27.2`, Fantastic, Fort Boxy from the local server, 5 min screen recording at 3024×1964, game reporting 21–22 FPS): bots were added (up to four) and tagged the player repeatedly ("…'s bot Got …"); bot fire was then switched off (Server Settings → Bots → Can Fire) because their lock-on shots kept tagging the player. Not noted at the time (added 2026-11-19): Fort Boxy switches on "L Hits Rematerialize" (`lsrh:1`), so each hit more than 10 m from the base should also have returned the player to the base. Whether that happened was not recorded; check it in the next official-build session. No automatic lock-on shot was seen. This is expected rather than a missing setting: there is no separate switch, `Settings.js` turns weapons lock on whenever the vehicle's lock slider is above 0 (the default), but the lock only fires when the player or the target is quarry and the target's 3 m `LaserLock` sphere sits on the nose line, which is hard to hold on Fort Boxy at 21 FPS. No lead, hit-rate or time-to-impact figures could be taken from this session. Qualitative result: in the official build bots hit a parked or slow player often, which supports the source's 5-step prediction; the port's earlier bot test (19 lock-on shots in 20 s without a tag at 9.7 m) did not match that at first; see the port run below.

Port run (2026-11-02, Unity `6000.3.27f1` editor, macOS `27.2` / Darwin 27.2.0, Play mode from `Init`, Fort Boxy, render level 6, fixed step 0.02 s, `BotAccuracyLog`): a first 78 s run looked like the old failure (87 bot shots, none on the player), but the player was not parked: its throttle input read 1 with nobody at the keyboard and it drove off at 35 m/s, so the bot was shooting at a fast, accelerating target. The source of that stuck input was not traced. Rerun with the player held still (kinematic, the equivalent of the handbrake) and the bot placed 60 m away, for 67 s: 72 bot shots, 43 on the player (60%), 2 on the floor, 5 elsewhere, 14 ended with no sparks; the player was tagged 13 times, about every 3 s once the 3 s tag guard allows. Bolts left within 0.1° of the line to the target. That matches the official build, where the bots tagged a slow player so often that bot fire was switched off. So the earlier "19 shots, no tag" result was most likely the same kind of moving-target run, not a targeting fault. The player's own lock-on shots in the same run (6, at the bot 47–60 m away moving 5–12 m/s): launched 0.10–0.11 s after firing (5 steps of 0.02 s, as `Rocket.js` waits), lead 0.56–1.62°, 4 hits, 1.31–1.57 s from launch to impact. No official-build figures exist for those yet, because the lock never fired in the recorded session. No code change was needed for the lasers.

## Laser-hit rematerialise and quarry colour (2026-11-02)

- `VehicleLaserResetModern` (on every vehicle) ports `Vehicle.js` `OnLaserHit` and `lR`: with `lasersFatal` ("L Hits Rematerialize", off by default in `Settings.js` but switched on by Fort Boxy's own settings, see below) on, a hit more than 10 m from the base freezes the vehicle, and 2 s later it is put back at the base; a second reset within 3 s is ignored. Not ported: the materialisation effect at both ends (`Game.js` `mE`, `Entry Effect.prefab`).
- `VehicleColourModern` now fades the quarry's body and accent to `Game.js` `vehicleIsItColor` (0.1289, 0.3922, 0) and `vehicleIsItAccent` (0.0644, 0.1961, 0), read with UnityPy from `Game` in the shipped 2.22 `level2`, whenever there is more than one vehicle; `GameModeSinglePlayerModern.SetQuarry` asks every vehicle to recolour, as `sQ` does. Corrected 2026-11-17: `Vehicle.js` `Start()` recolours every vehicle when one joins, so the quarry turns green as soon as a bot arrives; the port now does the same.
- Ramming (`OnRam`) is ported with RORBs (below).

Verified (Unity `6000.3.27f1`, macOS `27.2`, Play mode from `Init`): `tools/compile-check.sh` passes. With one bot, `lasersFatal` on and the player frozen 57 m from the base, the bot tagged the player twice in 20 s, became quarry and faded to green (body (0.151, 0.361, 0.050) after a few seconds, accent (0.067, 0.172, 0.030)), while the player kept its own blue; the frozen player was released and ended up beside the base, which only the reset does. No new console errors. Not yet compared on screen with the original.

## RORBs and ramming (2026-11-02)

Corrected 2026-11-17: ramming does not need RORBs. `VehicleLocal.js` calls `OnRam` on any body contact, and on any overlap of the vehicle's triggers (its `LaserLock` sphere included), with the quarry's rigidbody; see the section below. RORBs ("RORBs Enabled" in Server Settings, `ramoSpheres`, off by default; on, it is 0.5 with a Size slider from 0.001 to 1) add a larger sphere that does the same. Each vehicle then carries the `ORB` prefab (`ramoSphereObj`), grown in `FixedUpdate` towards `ramoSpheres × 15 + camOffset` (9.5 at the defaults). A vehicle whose sphere touches the quarry's rigidbody becomes quarry (`OnRam` → `sQ` mode 1, "… rammed the Quarry"), with the same 3 s guards as laser tags. ORB values, read with UnityPy from the shipped 2.22 `sharedassets3`: layer 13, trigger `SphereCollider` radius 0.5, `RamoSphere` `tagColor` (0.035, 0.565, 1, 0.278) and `ramColor` (0, 1, 0, 0.227), child `Sphere` with the built-in sphere mesh and the `RamoShield` material (`ForceField` shader).

Ported in `VehicleRamoSphereModern` (on every vehicle), with the setting and slider in `Settings.showDialogServer()` and the shield material through `Resources/RamoSphereAssets.asset`. `ForceField.shader` did not compile in Unity 6 (`texture` is a reserved word, and Unity 2.x's `TRANSFORM_UV` no longer exists); both are fixed, and the dead fixed-function `SetTexture` lines are removed. Not ported: XORBs (`zorbBall`, the solid rolling sphere, its green `ramColor` and the impact flash that only a solid sphere receives).

Verified (Unity `6000.3.27f1`, macOS `27.2`, Play mode from `Init`): `tools/compile-check.sh` passes and `ForceField` compiles with no errors. With RORBs on, one bot with firing off, and the player frozen as quarry 57 m from the base, both spheres grew to 9.49 and the bot took over as quarry 17 s in, which only ramming could do with its fire off; the shield draws as a faint blue bubble around each buggy. No console errors. Not yet compared on screen with the original.

## Parity fixes from the script audit (2026-11-17)

From [parity-audit.md](parity-audit.md). Static only: `tools/compile-check.sh` passes against Unity `6000.3.27f1` on macOS `27.2.1`; not yet compiled or played in the editor, and none of the scripted tests has been rerun.

- World settings: `World` reads the world's `message` and `marsxplr` parameters and `Game.ApplyWorldParams()` applies them once the player has spawned, as `Game.js` `Init()` does. `Settings.applyServerString()` ports `sSS` for the settings the port has. On Fort Boxy (`camo:1;lsrh:1;`) this turns on "L Hits Rematerialize" and `hideNames`, and shows "Welcome to Fort Boxy!". So `lasersFatal` is off by default but on in the reference world.
- Contact ramming: the new `VehicleLocalModern` (on every vehicle) ports `VehicleLocal.js` `OnCollisionStay` and `OnTriggerStay` into `Vehicle.js` `OnRam`. A vehicle that touches the quarry, or whose `LaserLock` sphere (3 m radius at the default) overlaps the quarry's body, becomes quarry with "… rammed the Quarry" and the usual 3 s guards. `VehicleRamoSphereModern` now calls the same `OnRam`. Unity 2.x had no layer collision matrix; the Unity 6 project's matrix is all-on, so the trigger overlaps should fire as they did. Not yet seen in Play mode.
- Terrain penetration and bounds: `VehicleLocalModern` also ports the `VehicleLocal.js` sweep (previous position to current, `terrainMask`, sit back on the surface unless the step was over √500 m) and, for the player, the `VehicleMe.js` checks: below −300 m back to the base with "… fell off the planet", and speed above 500 m/s halved. The `FallReset` stand-in is removed.
- Bots take the player's colours (`Vehicle.js` `Start()`), and no longer re-randomise or overwrite the saved colour.
- Start of game: the player's vehicle is named after `GameData.userName` ("MarsRacer" when unset), is created as quarry with no message, and spawns with the base's rotation (`Game.js` `Init()` and `iV`). `eSI` now runs only 0.5 s after a bot is axed, as in `Game.js`; the first-slice note above that the player "Defaulted to Quarry" at the start described the port, not the original.

Play-test (2026-11-18, Unity `6000.3.27f1` editor, macOS `27.2.1`, Play mode from `Init` into Fort Boxy, driven through the Unity MCP; no console errors or warnings throughout):

- Start: the vehicle is named "MarsRacer", is quarry with no "Defaulted" message, and spawns with the base's rotation. "Welcome to Fort Boxy!" is the first message; `lasersFatal` and `hideNames` are on, `zorbSpeed` is 0. `terrainMask` reads 2049 and `FallReset` is gone.
- Bots: with bot fire off and the player held still, the bot drove into the player and took the quarry ("MarsRacer's bot rammed the Quarry") with RORBs off. The bot spawned with the player's colours, the player's saved colour was unchanged, and the player (quarry) began fading to green as soon as the bot joined. Axing the bot gave "MarsRacer removed the last bot", then "MarsRacer Defaulted to Quarry" after the 0.5 s wait.
- Bounds: the player dropped to −305 m was put back on the spawn slab with "MarsRacer fell off the planet". Moved 2 m below the surface of a Fort Boxy box in one step, it was put back at the surface + 0.1 m (4.68 m against 4.59 m).
- Scripted tests: `Run Bot Test` passed (its axe check now waits 0.75 s for the original 0.5 s delay; the bot spawned 5.3 m from the player, rammed it after 2.5 s and then tagged it), `Run Lock-on Test` and `Run Snipe Trajectory Test` passed, and `Run Laser Test` scored as before (its snipe floor shot again went astray after the lock-on test had moved the buggy, as on 2026-10-23).

Not checked: ramming by the lock sphere alone (without body contact), and any of this by hand with the keyboard. Bot ramming is now common because bots spawn beside the player at the base and pursue the quarry; that matches the source, but has not been compared with the official build.

By hand and lock sphere (2026-11-18, same editor and macOS): with bot driving and firing off, both vehicles held still and placed so their bodies did not touch (no penetration; the player's body 1.78 m from the bot's centre, inside its 3 m `LaserLock` sphere), the bot took the quarry ("MarsRacer's bot rammed the Quarry"). So ramming by the lock sphere alone works. Driven with real keys: holding W on open floor south of the castle reached 57 MPH in 4 s with tyre tracks, holding A turned the buggy left (heading 183° to 120°), and Space opens and closes the wings. Holding the left mouse button fires at the 0.25 s auto rate in first person and in chase view while driving. In chase view, firing stops when the mouse is over the left quarter of the screen or the right 200 px (the menu column), as `VehicleMe.js` intends; an apparent "no fire in chase view" was this rule.

## Low audit rows (2026-11-19)

Unity `6000.3.27f1` editor, macOS `27.2.1`.

- RORB lifecycle (`Vehicle.js` `OnPrefsUpdated`, `lR`): `Settings.updatePrefs()` now ends with `updateObjects()`, which calls `OnPrefsUpdated` on every `VehicleRamoSphereModern`, as `Settings.js` sent it to every object; each vehicle also calls it from `Start`. The sphere is built a second later, keeping the rigidbody's inertia tensor and centre of mass; with RORBs off it shrinks and is destroyed 2 s later. A laser reset with "L Hits Rematerialize" shrinks the sphere, destroys it after 2 s and builds a new one; the `r` chat reset destroys it and `updatePrefs()` rebuilds it, and no longer zeroes the angular velocity (`Game.js` sets only the velocity). Switching RORBs off and on within 2 s ends with no sphere, as in the source.
- Shield flashes (`RamoSphere.js`): another ORB entering raises the shield's alpha to 10, and any laser hit on the vehicle to 5. `RocketModern` broadcasts `OnLaserHit` with a bool, and a method without the parameter did not receive it, so the sphere's handler takes the bool.
- `quarryDist` is written only by a quarry other than the player, and the HUD readout shows DST whenever the player is not quarry, both as in `Vehicle.js`.
- The laser aimer shows whenever weapons lock is on, also with lasers off or no firepower (`VehicleMe.js`). Jump waits out `Game`'s 0.1 s key guard.
- Settings: particles emit only from render level 3 (the aimers are switched back on, as `Vehicle.js` did); the frame rate is capped at 100 from preferences; `showHints`, `useMusic`, `useSfx`, `useHypersound`, `useMinimap` and `superCam` are loaded (music, sound effects and HyperSound are applied since the later 2026-11-19 audio port, see [ui.md](ui.md)); `resetTime` starts at 0 (no longer serialised, because the Game scene held the port's -10).
- `Game` sets `Application.runInBackground` every frame, as `Game.js` did.

Checked in Play mode from `Init` (`tools/compile-check.sh` passes; Bot, Lock-on, Laser and Snipe tests pass): with RORBs on, no sphere existed straight after the change and both vehicles had one at 9.49 later; a bot's shield went from alpha 0.28 to 5 on a laser hit; the `r` reset put the player at the base with no sphere and a new one was built; the aimer stayed on with lasers off. With the editor in the background Unity ran at 10 fps, and at that rate the laser test's two floor shots stopped at their start with no hit. With `Time.captureFramerate` 50 they hit and sparked as before, so this is the editor's throttling, not these changes.
