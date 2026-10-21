# Buggy ground model, presentation and camera

Detail for Milestone 1 in [PLAN.md](../PLAN.md). Summary: steps 1–8 of the ground model are ported into `BuggyControllerModern`, `BuggyPresentationModern` and `SkidmarksModern`, and match the Wine version within measurement precision on a flat pad. Tyre tracks now match the original's darkness. Open: the `Buggy Skidmarks` prefab's track width, lava floating and diving, and the optional "new physics" motor.

## Implementation order

Implementation order:

1. Define wheel anchor and wheel-contact data structures.
2. Port four raycast wheel contacts from the original buggy scripts.
3. Port suspension compression, spring force, and damping.
4. Port per-wheel longitudinal and lateral tyre forces.
5. Port motor torque, braking, steering, reversing, and speed limiting.
6. Port centre-of-gravity and self-righting behaviour.
7. Drive wheel and axle presentation from the authoritative contact state.
8. Switch the bootstrap between fallback and modern controllers without changing camera, HUD, or combat consumers.

## Status and recovered values

Status (2026-10-19): steps 1–8 are ported. Steps 1–6 and 8 live in `BuggyControllerModern`, from the default ("Modified Yoggy") branch of `Buggy.js`. Step 7 lives in `BuggyPresentationModern` and `SkidmarksModern`. Source values recovered from the original assets:

- `wheelPos` (0.7, 0, 0.8) and `axelPos` (0.375, −0.1, 0.75) from `Buggy.prefab`; wheel radius 0.3 and body mass 30 from `Buggy.js`.
- The convex body collider from the `Collider` mesh in `Buggy.jas` (lost in the FBX conversion), at the prefab's ×2 scale, in `BuggyColliderShape`.
- `Vehicle Body.physicmaterial`: zero friction, bounciness 0.5.
- `Settings.js` defaults for `buggyCG`, `buggyTr`, `buggySh`, and smart suspension, and its runtime fixed timestep of 0.02 s (0.025 s at low render levels). The project setting is 0.002 s, so the player previously ran physics at 500 Hz.

Supporting changes: `PlayerVehicleInputModern` ports the `VehicleMe.js` input mapping (including `Fire3` brakes and the `Throttle`/`Yaw` fallbacks); the placeholder laser moved to `PlayerLaserModern`; the body is the original `Buggy.prefab` detailed LOD (`Lod0`, `Cockpit`, `Headlights`) with its original materials, copied at the prefab's own transforms with the wings closed.

Deliberate deviations: wheel raycasts skip the vehicle's own colliders and ignore triggers.

Verified in the Unity `6000.3.26f1` editor on fallback ground: the buggy spawns on its wheels, accelerates to about 49 MPH, steers while coasting, and logs no errors. Not yet verified: braking, reversing, side slip, landing, uneven terrain, and handling feel against the original.

## Uneven ground

Uneven ground (2026-10-21, Unity `6000.3.26f1` editor, macOS `27.0.1`, Play mode entered directly from `Game.unity`): `Tools -> MarsXPLR -> Run Buggy Ramp Test` finds the nearest tilted box collider on the floor, which in Fort Boxy is the castle's `Castle/cube` base slope (8.1°), places the buggy 8 m below its lowest edge yawed 35° off the slope line, drives at half throttle for 2.5 s and coasts for 2.5 s. It logs each wheel's ray hit distance (0.7 m means full extension) and the body's roll and pitch every 0.1 s to `Unity6/Logs/ramp-test.txt`. No code change was needed: each wheel already casts its own ray and applies its own spring and friction force (`BuggyControllerModern`, ported from `Buggy.js`).

| Time | FL | FR | RL | RR | Roll | Pitch |
| --- | --- | --- | --- | --- | --- | --- |
| 2.3 s (flat) | 0.57 | 0.57 | 0.59 | 0.59 | 0.1° | 0.7° |
| 2.5 s (FL meets the slope) | 0.50 | 0.57 | 0.59 | 0.58 | −0.8° | 0.2° |
| 2.7 s (FR follows, RL unloads) | 0.47 | 0.53 | 0.60 | 0.57 | −2.8° | −1.8° |
| 3.5 s (all four on the slope) | 0.55 | 0.55 | 0.60 | 0.59 | −5.6° | −4.6° |

The front-left wheel compresses first while the other three hold, then the front-right, while the rear-left briefly extends; the body rolls and pitches with the slope, and the four distances stay distinct once it is on the slope. So the wheels respond independently. The buggy did not reach one of the castle's smaller ramps, and no matched Wine run was made: the Wine version shows no suspension telemetry, and the earlier Fort Boxy handling comparison already crossed its ramps and bumps without a visible difference. The editor stopped advancing Play mode while it was in the background; the run completed once Unity was frontmost.


Wheel presentation (2026-10-19): `BuggyPresentationModern` instantiates the original `Buggy Wheel` and `Buggy Axel` prefabs, with `axelPos` (0.375, −0.1, 0.75) from `Buggy.prefab`. It places each wheel at the suspension contact distance, applies the controller's steering rotation, spins the wheel graphic from `motorSpeed`, and points each axle at its wheel, as `Buggy.js` `Update()` did. Only the detailed wheel LOD is shown. `SkidmarksModern` ports `Skidmarks.js` and is fed per-wheel contact points and the original track intensity from the controller. The prefab and material references are serialised on `SinglePlayerPlayableBootstrap` in `Game.unity` so player builds include them.

Verified in the Unity `6000.3.26f1` editor on Fort Boxy: wheels and axles render at the original positions, the front wheels steer, and no errors are logged. The skidmark mesh fills with segments while driving, but the tracks were not confirmed visually. The original material draws them black at about 25% opacity, which barely shows on Fort Boxy's dark floor.

## Camera

Camera (2026-10-19): `CameraVehicle` again uses the original chase distance: the vehicle's `camOffset` (2, confirmed in the original `Buggy.prefab` `VehicleData`) plus the player's `camDist` setting. This replaces the 4.2 m scaffold tuned for the old oversized visual. The Agile and Smooth formulas, the 7 s world-entry glide, the RideCam ride position, and the ground lift (Smooth and Agile only) are restored from `CameraVehicle.js`. `World` restarts the entry glide when the world finishes loading.

Snipe zoom (2026-10-19): holding Shift (`Snipe`, not while chatting) drops the camera's field of view from 60 to 10 degrees and the scroll wheel adjusts it between 1.5 and 50, as in `CameraVehicle.js`; releasing restores 60. Verified in the Unity `6000.3.26f1` editor.

Known parity behaviour: the Agile camera's direction starts at the original (1, 1, 1), so it sits below a stationary buggy, under Fort Boxy's one-sided floor, until the buggy moves. Compare this start with the Wine version before deciding whether to change it. Judged comparable to the Wine version by eye on 2026-10-21; kept as is, with an instrumented comparison backlogged.

Verified: editor-free compile check, and an arm64 development build that launches into Fort Boxy (Unity `6000.3.26f1`, macOS `27.0.1`). Not yet verified: chase framing while driving, and a comparison with the Wine version. Keyboard input did not reach the player during the automated check.

## Body and colour

Body (2026-10-19): `SinglePlayerPlayableBootstrap` copies the meshes and materials of `Lod0`, `Cockpit`, and `Headlights` from the original `Buggy.prefab`. It does not instantiate the prefab, which still carries missing UnityScript components. This replaces the generated `BuggyVisual` prefab, its generator, and the bounds-based fitting. In the original, the body mesh is already at vehicle scale: 1.96 m long, bottom at −0.19, centred at z −0.02. Wings and trails stay out until the wing port. Verified in the Unity `6000.3.26f1` editor on Fort Boxy: the body sits on the wheels and axles at the original size, the cockpit and headlights render, and no new errors are logged. The body shows the material's default blue rather than the player colour, because `Vehicle.js` `setColor()` (player colour from `vehColR/G/B` preferences) is not ported yet. The Wine version drew the buggy magenta.

Vehicle colour (2026-10-19): `VehicleColourModern` ports the colouring from `Vehicle.js`, and `Settings` ports the colour preferences from `Settings.js`. The body material fades to `vehicleColor`, and the wheel beadlocks fade to `vehicleAccent`. Both colours are stored in `VehicleStateModern`. As in `Game.js`, each game starts with a random colour unless `vehColCustom` is set: red and blue up to 0.7, green limited relative to them, and the accent 0.25 darker. Verified in the Unity `6000.3.26f1` editor: two runs gave the buggy two different random colours. Rechecked (2026-10-21, Unity `6000.3.26f1`, macOS `27.0.1`, ramp test log): `VehicleStateModern` held `vehicleColor` (0.52, 0.25, 0.59) and `vehicleAccent` (0.27, 0.00, 0.34); 1.5 s after spawn the `Body` material was (0.47, 0.24, 0.55) and the four `Beadlock` materials (0.24, 0.00, 0.36), still fading towards them at alpha 0.5 as in `Vehicle.js`. Not ported: the green "it" colours, which belong to the quarry rules; wing tinting, which waits for the wing port; and the colour sliders in the settings window.

## Tyre tracks

Wheel spin and tyre tracks (2026-10-19): the wheel spin direction has been confirmed correct by eye. The tyre tracks match the original in code: `SkidmarksModern` is a line-for-line port of `Skidmarks.js`, and the per-wheel opacity follows `Buggy.js`. The material and texture are the original `skidmark.mat` and a byte-identical `skidmark.png`. The track width keeps the script default of 0.225 m, because the original `Buggy Skidmarks` prefab is binary and any value it overrides could not be read. On Fort Boxy's orange floor, the Wine version's tracks darken the floor by about 15% (screenshot brightness about 120 to 102). The formula predicts 10–20% opacity: 2 × 0.255 tint × 0.2–0.5 per-wheel opacity × about 0.46 texture alpha. With the floor brightness now matching (see Lighting in [world-and-rendering.md](world-and-rendering.md#lighting)), the port's tracks still darken the floor by under 3% behind a moving buggy, so they were fainter than the Wine version's (fixed below). `Tools -> MarsXPLR -> Run Buggy Track Test` drives the buggy on open floor south of the castle for this comparison.

Tyre-track opacity fixed (2026-10-20): the faint tracks came from soft particles, not the track code. Unity 6's legacy `Particles/Alpha Blended` shader fades by depth difference when `SOFTPARTICLES_ON` is set, which happens at the Beautiful and Fantastic quality levels (`softParticles: 1`). The track mesh sits 0.05 m above the floor, so with the default `_InvFade` of 1 the fade multiplied its opacity by about 0.05. Unity 2.6 had no soft particles. `skidmark.mat` now saves `_InvFade: 100`, so the fade is 1 for anything more than 1 cm above the floor; the shader's 0.01–3 slider limits only the inspector. Measured in the Unity `6000.3.26f1` editor (macOS `27.0.1`, Fantastic, Play mode from `Init`, `Run Buggy Track Test`, far Agile chase): floor 122, track centre 81 (5-pixel mean, darkest point per row), so about 33% darker; floor-only patches dip to about 90 from texture noise alone. The Wine version measured floor 120 and tracks 85–90 (about 27%). The texture is red with alpha and the tint is black, so tracks are drawn black; the formula 2 × 0.255 tint × per-wheel opacity × texture alpha (0.62 at the sampled row) agrees with these figures.

Open follow-ups: confirm the track width saved in the original `Buggy Skidmarks` prefab; lava floating and diving; the optional "new physics" motor.

## Centre trail

Centre trail (2026-10-20): the white streak behind the buggy is `Buggy.js`'s `leftTrail` and `rightTrail` (`Trail1` and `Trail2` under `Lod0`). While the wings are closed both sit at the body origin, so they draw on top of each other. They exist on the player's own buggy only and are always on in 2.22, because the `Settings.js` line that would switch them off is commented out. Original values, read from the binary `Buggy.prefab` and `WhiteDot.mat` through the Unity 4.6 type tree: 5 s time, 0.1 m start and end width, 0.3 m minimum vertex distance, white with alpha 0, 1, 1, 1, 0 at five even steps along the trail. The material is `WhiteDot` on built-in shader 200 (Particles/Additive), tint (0.5, 0.5, 0.5, 0.5). The archived 2.22 Mac build carries the same data, so it adds nothing here.

`Tools -> MarsXPLR -> Log Buggy Trails` writes the trails as Unity 6 imported them to `Unity6/Logs/buggy-trails.txt`. In the Unity `6000.3.26f1` editor on macOS `27.0.1` every value matches: 5 s, 0.1/0.1 m, 0.3 m, view alignment, stretch texture mode, a blend gradient with alpha 0, 1, 1, 1, 0 at 0, 0.25, 0.5, 0.75 and 1, and `WhiteDot` on `Legacy Shaders/Particles/Additive` with the same tint and texture.

Compared by eye with the Wine version (4.0.2, Fantastic) from the far Agile chase view on open floor: both show a thin white additive line from the centre of the buggy, between the tyre tracks, fading out behind. The Wine frame was taken almost stopped and turning, and the port's while driving straight at about 20 MPH, so length and width near the camera were not compared like for like.

Matched comparison (2026-10-20): a Wine screenshot driving straight at 17 MPH (Fantastic, far Agile chase) against the port at about 20 MPH from the same view. The trail's width relative to the tyre-track spacing is about the same in both. Brightness differs: measured as the blue channel's peak above the floor along the trail, the Wine trail is about 220–250 from a few metres behind the buggy all the way to the bottom of the view, close to clipping. The port's fades in behind the buggy (13, 32, 63 and 110 over roughly the first third) and then levels off at about 160–195. So the Wine trail is brighter overall, and much brighter just behind the buggy.

What was ruled out:

- Data: the Wine build (Mars Explorer 4.0.2, `Mars Explorer_Data`) and the archived Mac 2.22 build both run on Unity 2.5.0f5, and both carry the same `Trail1`/`Trail2` values, `WhiteDot` material and `WhiteDot` texture (16 × 16 RGB565, mipmapped, bilinear, repeat) as `unity/` and the port.
- Trail construction: the archived Mac player (i386 slice, stripped) was disassembled. Its TrailRenderer fields are time 0xf8, start width 0xe0, end width 0xe4, minimum vertex distance 0xfc, colours 0x100 (five RGBA32 values) and autodestruct 0x130 (serialisation function at 0x157990). The renderer (0x1586d0) hands a shared line builder (0x14dc30, also used by `LineRenderer`) the points, the colour array and the widths. The builder gives point *i* of *n* the parameter t = i / (n − 1). It looks up the colour by splitting t into four equal segments between the five keys, and writes UV (t, 0) and (t, 1). This is the same five-key gradient and the same full-length texture stretch that Unity 6 uses in `Stretch` mode, apart from Unity 2.5 spacing t by point index rather than by distance, which is equivalent while the buggy moves steadily.

Fog ruled out (2026-10-21, Unity `6000.3.26f1`, macOS `27.0.1`): Fort Boxy's fog is exponential squared with density 0.00142, and the far Agile chase camera sits 27 m from the buggy, so fog keeps 99.85% of the trail's colour at the buggy and 98% at 100 m. It cannot account for the difference.

Not yet explained: the brightness difference. Remaining candidates are the legacy additive shader (Unity 2.5 fixed function under Direct3D and DXVK against Unity 6's `Legacy Shaders/Particles/Additive` on Metal), which is not being pursued further, how the newest point at the buggy is placed in Unity 2.5's update (0x158a80), and camera distance (the port's buggy is about 20% smaller in frame).

## Handling comparison with the Wine version

Handling comparison with the Wine version (2026-10-19, macOS `27.0.1`). The original is Mars Explorer 4.0.2 under the `/Applications/Mars Explorer.app` Wine wrapper. It hosted a local Fort Boxy game and ran at 11–21 FPS. Its figures come from the HUD, read off screenshots, so their timing is only accurate to about ±0.5 s. It was driven on the open floor outside the castle. The port ran in the Unity `6000.3.26f1` editor with a 0.02 s fixed timestep. Synthetic key presses do not reach the editor's Game view, so the port was driven by `Tools -> MarsXPLR -> Run Buggy Handling Probe` (`Editor/BuggyHandlingProbe.cs`). The probe writes the same inputs into `VehicleStateModern`, runs on a flat pad it builds above the world (with an 18 m ledge for the drop), and logs to `Unity6/Logs/handling-probe.txt`.

| Test | Original (Wine) | Port (Unity 6, flat pad) |
| --- | --- | --- |
| Full throttle for 4 s from rest | 51 MPH | 58 MPH |
| Coast after release | 53 → 47 MPH over about 5 s (about 1.5 MPH/s) | 58 → 47 MPH over 5.5 s (about 2 MPH/s) |
| Reverse throttle as a brake | 32 → 1 MPH in about 2 s (about 16 MPH/s) | 58 → <1 MPH in 3.4 s (about 17 MPH/s) |
| Reverse from rest, 1 s holds | 4, 20, 32, 44 MPH, then crashed and flipped | 16 MPH at 1 s, 31 at 2 s, 43 at 3 s, then yaws into a spin |
| Full right lock for 2 s while coasting | From 46 MPH: about 90° turn, slides onto its outer wheels, ends at 14 MPH | From 57 MPH: 73° turn, ends at 22 MPH |
| `Fire3` brakes | Not measured: Wine does not pass Cmd through as the Windows key | 59 → <1 MPH in 1.5 s over 23.9 m |
| Drop of about 18 m at speed | Lands upright, no visible bounce, keeps driving | Pitches nose-down 42° in the fall, lands upright with one small bounce, keeps driving |

Within the precision of the Wine readings, the port matches the original on every test. The motor, friction, suspension, and drag code is a line-for-line port of the default "Modified Yoggy" path in `Buggy.js`. Both games use the same `Settings.js` defaults, and the controller needed no changes. The first comparison had reported large gaps (slow acceleration, steering that scrubbed speed, a 16 MPH reverse cap). Those were measurement errors. The port's runs had started wherever the buggy came to rest on Fort Boxy, so they crossed ramps and bumps. The Wine reverse readings came from a run against a pillar and from a single second of input.

Fort Boxy comparison (2026-10-21, macOS `27.0.1`). The port ran in the Unity `6000.3.26f1` editor with `Tools -> MarsXPLR -> Run Buggy Handling Probe On Fort Boxy`, which runs the probe's tests on the open floor south of the castle (floor at y = 2.4, heading south) and takes the jump by driving off the spawn slab from the base. It logs to `Unity6/Logs/handling-probe-fortboxy.txt`. The Wine version (4.0.2, about 34 FPS) was driven by hand on the same ground and recorded with `screencapture` every 0.55 s. Its speeds were read from the HUD by text recognition, so timings are accurate to about ±0.5 s. For about 40 s the Wine window stopped redrawing and the captures repeated one frame; those frames were dropped, which lost the Wine coast test.

| Test | Original (Wine, Fort Boxy) | Port (Unity 6, Fort Boxy) |
| --- | --- | --- |
| Full throttle from rest | 8 → 106 MPH in about 8.6 s; 4 → 70 MPH in about 4.9 s; 79 → 143 MPH in about 8.6 s (with a 4 s bump at about 60 MPH earlier in that run) | 58 MPH at 4 s, 70 at 5 s, 106 at about 8.5 s, 143 at about 13.5 s, 175 at 20 s; 81 → 146 MPH over 8 s |
| Coast after release | Lost (window froze) | 58 → 26 MPH over 30 s, 555 m (as on the flat pad) |
| Reverse throttle as a brake | 28 → 8 MPH in about 1.6 s (start of the stop not captured) | 58 → <1 MPH in 3.44 s over 49 m |
| Reverse from rest | 8, 16, 24 MPH at 0.5 s steps, about 54 MPH at 4 s, then hit something | 8, 16, 24 MPH at 0.5 s steps, 46 MPH at 3.5 s, then yaws into a spin |
| Full right lock while coasting | 70 → 55 → 43 → 35 MPH at 0.55 s steps, then about 32 MPH | 57 → 54 → 44 → 34 → 22 MPH at 0.5 s steps, 73° turn |
| Jump: full throttle off the spawn slab | 33 MPH at 2.2 s, gains only about 5 MPH per 0.5 s while in the air, lands upright and keeps driving, 75 MPH at 6.1 s, then hits a castle pillar (75 → 56 → 42) | 36 MPH leaving the slab at 2.4 s, airborne 1.2 s gaining about 4.5 MPH per 0.5 s, lands upright with one bounce, 68 MPH at 5.4 s, then hits the same pillar (68 → 41) |
| `Fire3` brakes | Not measured (Wine does not pass Cmd through) | 59 → <1 MPH in 1.54 s over 24 m |

On Fort Boxy the port matches the original within the precision of the Wine readings. Acceleration matches to about 1 s up to 106 MPH, and reverse matches exactly over its first 1.5 s. The Wine run took about 1–2 s longer than the port to reach the highest speeds, which is within what bumps and the hand-held run explain. Neither version has a top speed within 20 s. The jump matches closely: both slow their gain while airborne, land upright, keep driving and hit the same castle pillar at a similar time. The user drove the Wine tests and judged the handling to feel similar. Not compared: the coast (lost in the Wine capture; the 2026-10-19 flat comparison stands), the full stop under reverse throttle, and `Fire3` brakes in the original.

Spawn (2026-10-21, Unity `6000.3.26f1`, macOS `27.0.1`): the port spawned 4 m above `Base` with `Base`'s rotation. It now follows `Game.js`: at `World.base.position` with identity rotation, raised a metre at a time until a 3 m sphere is clear. The original `FortBoxy.utw` places `Base` at (0, 17, 0), between the `AirCastle` slabs with tops at y = 10.5 and y = 29–30, and neither `Base` nor `Floor` has a collider in 2.22 or 4.0.2. So the buggy drops about 6.5 m onto the lower slab. Verified in the editor: it lands at 11 ALT, then creeps forwards at 2–4 MPH with no input, rolls off the slab after about 30 s and stops on the floor at 3 ALT. The Wine version does the same: it lands at 11 ALT and drifts forwards, and its buggy creeps forwards with no input wherever it is. An earlier note that the original rests at 18 ALT was a misreading. The creep is recorded in `IMPROVEMENTS.md`.
