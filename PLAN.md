# Mars Explorer Modernisation Plan

## Objective

Deliver a recognisably original Mars Explorer single-player experience as a native macOS application, targeting Apple silicon (`arm64`) first and a universal binary only after the arm64 path is stable.

`Unity6/` is the canonical runtime project. Its baseline editor is Unity 6.3 LTS (`6000.3.26f1`), supported until December 2027. `unity/` is retained as original behavioural and asset reference material; the ignored local `Unity5/` project is an asset-conversion bridge only.

## Current status

The repository contains a promising but incomplete modernisation foundation:

- replacement C# components cover the active camera, HUD, messaging, minimap, world, settings, and game scaffolding;
- `SimplePlayerController` provides temporary drive and flight behaviour;
- `VehicleStateModern` provides a shared vehicle-state seam;
- the runtime bootstrap spawns the buggy with the original body, wheel, and axle models;
- quarry, laser, and score behaviour are placeholders rather than parity ports;
- editor tooling can rebuild and inspect the `Game` scene baseline;
- the intended offline flow is now `Init -> InitGame -> Game`, with `Lobby` retained outside the active vertical slice.

The current working tree has not been compiled or played in Unity since its newest changes. Cached assemblies predate the latest scene-flow, validation, and vehicle-state work. There is no verified arm64 player build yet.

## Decisions

- Order of authority for parity:
  1. The original code and assets decide behaviour and values. `unity/` is Aubrey Falconer's open-source release (2.22, upgraded by him to Unity 4.6.1f1 with no script changes). The ignored local `Unity5/` is the "Mars Explorer - Unity 5 Compatibility Fix" folder: a Unity 5.0.0f4 port by Ethan (BluetoothBoy), merged by the author as pull request #1 on 2015-03-29. It holds the same logic with Unity 5 API updates only, so either can be read. If they ever disagree on logic, `unity/` wins.
  2. How the engine the game shipped on (Unity 4 and earlier) rendered that content decides the look. Neither `Unity5/` nor a modern editor shows the original look, for example the doubled legacy lighting.
  3. The Wine build of Mars Explorer 4.0.2, which is newer than the 2.22 source, is evidence for checks and for data found nowhere else (downloaded worlds, values in binary prefabs). Values taken only from it are recorded as inferences.

- Keep `Unity6/` as the only active gameplay implementation.
- Target Unity 6.3 LTS directly. The earlier plan to capture a Unity `2022.3.62f3` proof build first was dropped (2026-10-13): the project had never been verified in 2022.3, so that build would have protected no known-good state. The tag `pre-unity6-upgrade` is the pre-upgrade reference instead.
- Remain on the Built-in Render Pipeline, IMGUI, and the legacy Input Manager for parity; any move to URP or the Input System is a separate later decision.
- Preserve original behaviour and presentation by default.
- Complete one single-player buggy vertical slice before restoring other vehicles or networking.
- Keep networking, master-server services, authentication, adverts, and retired web services out of scope.
- Treat `VehicleStateModern` as the shared state contract.
- Treat `SimplePlayerController` as removable scaffolding, not the final buggy controller.
- Keep generated assets deterministic and committed when the runtime depends on them.
- Require matching source assets and `.meta` files in a fresh checkout.
- Do not mass-reserialise legacy binary assets until a known-good editor baseline has been captured.

## Milestone 0: Reproducible baseline

### Implemented in the current working tree

- [x] Define public project status and contributor workflow in `README.md`.
- [x] Remove personal absolute paths from the public documentation.
- [x] Make PNG source assets visible despite the user's global Git ignore.
- [x] Consolidate offline automatic routing under one scene-flow coordinator.
- [x] Define `Init -> InitGame -> Game` as the active offline path.
- [x] Allow `Lobby` to remain in Build Settings without making it part of the active path.
- [x] Add a Unity batchmode baseline-validation entry point.
- [x] Separate buggy flight state from the legacy `inputThrottle` state flag.
- [x] Stage every PNG source asset alongside its existing `.meta` (static check: no asset without a `.meta`; the only `.meta` files without tracked sources are empty folders).
- [x] Stop tracking IDE-generated solution and project files in `Unity6/`.
- [x] Anchor root-only ignore rules so `Unity6/Assets/Whirld/Docs/` is no longer ignored on case-insensitive file systems.

### Required before the baseline is locked

- [x] Add `tools/compile-check.sh`, an editor-free compile check against Unity 6 reference assemblies.
- [x] Fix the editor-free compile errors (see Editor-free compile status).
- [x] Open the project in Unity `6000.3.26f1`, let the API updater run, and complete a clean script compile.
- [x] Review scene, prefab, and project-setting re-serialisation against `pre-unity6-upgrade` before committing it (binary assets reviewed by file list only).
- [x] Remove the `com.unity.multiplayer.center` package Unity 6 added during import.
- [ ] Run `Prepare Game Scene Baseline` with `Game.unity` open and review all changes before saving.
- [x] Run the batch `Validate Project Baseline` check.
- [ ] Run the interactive `Validate Project Baseline` check.
- [ ] Verify direct Play mode entry from `Game.unity`.
- [x] Verify the complete `Init -> InitGame -> Game` flow (Unity `6000.3.26f1` editor, 2026-10-13: reaches `Game`, spawns the buggy on fallback ground, no exceptions; required driving later scene hops from `SceneManager.sceneLoaded`).
- [x] Create and launch an arm64 development player on Apple silicon (see arm64 development player).
- [ ] Repeat the import and smoke test from a clean checkout or clean clone.
- [x] Record the Unity version, macOS version, scripting backend, architecture, and known warnings.

## Milestone 1: Original buggy ground model

Create `BuggyControllerModern` without folding new physics into `SimplePlayerController`.

Implementation order:

1. Define wheel anchor and wheel-contact data structures.
2. Port four raycast wheel contacts from the original buggy scripts.
3. Port suspension compression, spring force, and damping.
4. Port per-wheel longitudinal and lateral tyre forces.
5. Port motor torque, braking, steering, reversing, and speed limiting.
6. Port centre-of-gravity and self-righting behaviour.
7. Drive wheel and axle presentation from the authoritative contact state.
8. Switch the bootstrap between fallback and modern controllers without changing camera, HUD, or combat consumers.

Status (2026-10-19): steps 1–8 are ported. Steps 1–6 and 8 live in `BuggyControllerModern`, from the default ("Modified Yoggy") branch of `Buggy.js`. Step 7 lives in `BuggyPresentationModern` and `SkidmarksModern`. Source values recovered from the original assets:

- `wheelPos` (0.7, 0, 0.8) and `axelPos` (0.375, −0.1, 0.75) from `Buggy.prefab`; wheel radius 0.3 and body mass 30 from `Buggy.js`.
- The convex body collider from the `Collider` mesh in `Buggy.jas` (lost in the FBX conversion), at the prefab's ×2 scale, in `BuggyColliderShape`.
- `Vehicle Body.physicmaterial`: zero friction, bounciness 0.5.
- `Settings.js` defaults for `buggyCG`, `buggyTr`, `buggySh`, and smart suspension, and its runtime fixed timestep of 0.02 s (0.025 s at low render levels). The project setting is 0.002 s, so the player previously ran physics at 500 Hz.

Supporting changes: `PlayerVehicleInputModern` ports the `VehicleMe.js` input mapping (including `Fire3` brakes and the `Throttle`/`Yaw` fallbacks); the placeholder laser moved to `PlayerLaserModern`; the body is the original `Buggy.prefab` detailed LOD (`Lod0`, `Cockpit`, `Headlights`) with its original materials, copied at the prefab's own transforms with the wings closed.

Deliberate deviations: wheel raycasts skip the vehicle's own colliders and ignore triggers.

Verified in the Unity `6000.3.26f1` editor on fallback ground: the buggy spawns on its wheels, accelerates to about 49 MPH, steers while coasting, and logs no errors. Not yet verified: braking, reversing, side slip, landing, uneven terrain, and handling feel against the original.

Wheel presentation (2026-10-19): `BuggyPresentationModern` instantiates the original `Buggy Wheel` and `Buggy Axel` prefabs, with `axelPos` (0.375, −0.1, 0.75) from `Buggy.prefab`. It places each wheel at the suspension contact distance, applies the controller's steering rotation, spins the wheel graphic from `motorSpeed`, and points each axle at its wheel, as `Buggy.js` `Update()` did. Only the detailed wheel LOD is shown. `SkidmarksModern` ports `Skidmarks.js` and is fed per-wheel contact points and the original track intensity from the controller. The prefab and material references are serialised on `SinglePlayerPlayableBootstrap` in `Game.unity` so player builds include them.

Verified in the Unity `6000.3.26f1` editor on Fort Boxy: wheels and axles render at the original positions, the front wheels steer, and no errors are logged. The skidmark mesh fills with segments while driving, but the tracks were not confirmed visually. The original material draws them black at about 25% opacity, which barely shows on Fort Boxy's dark floor.

Camera (2026-10-19): `CameraVehicle` again uses the original chase distance: the vehicle's `camOffset` (2, confirmed in the original `Buggy.prefab` `VehicleData`) plus the player's `camDist` setting. This replaces the 4.2 m scaffold tuned for the old oversized visual. The Agile and Smooth formulas, the 7 s world-entry glide, the RideCam ride position, and the ground lift (Smooth and Agile only) are restored from `CameraVehicle.js`. `World` restarts the entry glide when the world finishes loading.

Known parity behaviour: the Agile camera's direction starts at the original (1, 1, 1), so it sits below a stationary buggy, under Fort Boxy's one-sided floor, until the buggy moves. Compare this start with the Wine version before deciding whether to change it.

Verified: editor-free compile check, and an arm64 development build that launches into Fort Boxy (Unity `6000.3.26f1`, macOS `27.0.1`). Not yet verified: chase framing while driving, and a comparison with the Wine version. Keyboard input did not reach the player during the automated check.

Body (2026-10-19): `SinglePlayerPlayableBootstrap` copies the meshes and materials of `Lod0`, `Cockpit`, and `Headlights` from the original `Buggy.prefab`. It does not instantiate the prefab, which still carries missing UnityScript components. This replaces the generated `BuggyVisual` prefab, its generator, and the bounds-based fitting. In the original, the body mesh is already at vehicle scale: 1.96 m long, bottom at −0.19, centred at z −0.02. Wings and trails stay out until the wing port. Verified in the Unity `6000.3.26f1` editor on Fort Boxy: the body sits on the wheels and axles at the original size, the cockpit and headlights render, and no new errors are logged. The body shows the material's default blue rather than the player colour, because `Vehicle.js` `setColor()` (player colour from `vehColR/G/B` preferences) is not ported yet. The Wine version drew the buggy magenta.

Vehicle colour (2026-10-19): `VehicleColourModern` ports the colouring from `Vehicle.js`, and `Settings` ports the colour preferences from `Settings.js`. The body material fades to `vehicleColor`, and the wheel beadlocks fade to `vehicleAccent`. Both colours are stored in `VehicleStateModern`. As in `Game.js`, each game starts with a random colour unless `vehColCustom` is set: red and blue up to 0.7, green limited relative to them, and the accent 0.25 darker. Verified in the Unity `6000.3.26f1` editor: two runs gave the buggy two different random colours. Not ported: the green "it" colours, which belong to the quarry rules; wing tinting, which waits for the wing port; and the colour sliders in the settings window.

Wheel spin and tyre tracks (2026-10-19): the wheel spin direction has been confirmed correct by eye. The tyre tracks match the original in code: `SkidmarksModern` is a line-for-line port of `Skidmarks.js`, and the per-wheel opacity follows `Buggy.js`. The material and texture are the original `skidmark.mat` and a byte-identical `skidmark.png`. The track width keeps the script default of 0.225 m, because the original `Buggy Skidmarks` prefab is binary and any value it overrides could not be read. On Fort Boxy's orange floor, the Wine version's tracks darken the floor by about 15% (screenshot brightness about 120 to 102). The formula predicts 10–20% opacity: 2 × 0.255 tint × 0.2–0.5 per-wheel opacity × about 0.46 texture alpha. With the floor brightness now matching (see Lighting under Milestone 3), the port's tracks still darken the floor by under 3% behind a moving buggy, so they are fainter than the Wine version's. Not yet determined: whether the 4.0.2 build ships different track values than the source in this repository, or whether Unity 6's legacy `Particles/Alpha Blended` shader treats the track opacity differently from Unity 2.6. `Tools -> MarsXPLR -> Run Buggy Track Test` drives the buggy on open floor south of the castle for this comparison.

Open follow-ups: find why the port's tyre tracks are fainter than the Wine version's (measure one segment of known opacity in both), and confirm the track width saved in the original `Buggy Skidmarks` prefab; lava floating and diving; the optional "new physics" motor.

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

Open (world data, not handling): with no input, the port's buggy does not stay at spawn. It spawns 4 m above `Base` (y = 17), falls about 10 m onto the castle's 30 × 30 slab at y = 10.5, rolls off it, and comes to rest on the floor at y = 2.4. The original rests at 18 ALT and stays there. The original spawns at `World.base.position` with identity rotation, raised until a 3 m sphere is clear (`Game.js`). The repository's Fort Boxy scene has nothing solid between y = 10.5 and the slab at y = 29–30. Mars Explorer 4.0.2 loads Fort Boxy from a downloaded `.utw` world file, which may place `Base` or the `AirCastle` slabs differently. No cached copy was found, so this is unconfirmed.

Acceptance criteria:

- [x] The buggy rests on its wheel contacts rather than a capsule approximation.
- [ ] All four wheels respond independently to uneven ground.
- [x] Acceleration, reversing, steering, braking, side slip, and landing are controllable (flat-pad probe compared with the Wine version, 2026-10-19).
- [x] Camera and HUD telemetry continue to consume `VehicleStateModern`.
- [x] `SimplePlayerController` can still be selected temporarily for recovery (`useModernBuggyController` on `SinglePlayerPlayableBootstrap`).
- [ ] Behaviour is tested on a small representative Mars terrain fixture, not only `FallbackGround` (Fort Boxy now loads; handling not yet assessed on it).

## Milestone 2: Wing and flight parity

- Port the original special-input and wing deployment state machine.
- Port wing mesh/flap presentation without mutating shared source meshes at runtime.
- Port lift, drag, stall, airbrakes, ground effect, take-off, and landing behaviour.
- Keep `specialInput`, `flightMode`, `camSmooth`, brakes, and throttle-channel selection as distinct state concepts.
- Validate drive-to-flight and flight-to-drive transitions against recorded parity scenarios.

Acceptance criteria:

- [ ] Wing opening and closing have deterministic state and presentation.
- [ ] Take-off requires plausible speed and surface interaction.
- [ ] Stall, roll, pitch, drag, airbrakes, and landing remain controllable.
- [ ] Returning to drive mode does not reset orientation or momentum incorrectly.

Status (2026-10-19): ported from `Buggy.js` into `BuggyControllerModern` (physics) and the new `BuggyWingsModern` (presentation).

- Wing state machine: `Update` and `OnSetSpecialInput`, driven by changes to `VehicleStateModern.specialInput` (toggled on Jump by `PlayerVehicleInputModern`). Wings open over half a second and keep flying until the closing animation finishes, as in the original. `wingFlaps` was only ever reset in the original, so it is not ported.
- Flight (`wingOpen` branch): speed-dependent drag, airbrakes, velocity-follows-nose flight with pitch and roll torque, sideslip ("dihedral") torque, lava thermals, and stalling below 4 m/s. The smart-suspension gear retract in flight and the stiffer wing-open springs are ported. The `Settings.js` "Stall Blending" and "Powered Loops" options are added to `Settings` (both off by default, as in the original).
- Presentation: `SinglePlayerPlayableBootstrap` copies `Buggy.prefab` `Lod0/Wings` (mesh and material, without instantiating the prefab) and clones the `Trail1` and `Trail2` trail objects. `BuggyWingsModern` animates a per-vehicle copy of the wing mesh (unfold, then flex with roll and smoothed pitch input) and moves the trails to the wingtips. `VehicleColourModern` tints the wings with the accent colour. Trail1 is assumed to be the left trail; this is unconfirmed.
- Shader fix: the wings were invisible because `GlassGlowing.shader` still declared its programs with Unity 2 comment directives (`// vertex vert`), so modern Unity compiled no vertex or fragment program. The shader now uses `#pragma vertex`/`#pragma fragment`, `appdata_base` (the wing mesh has no tangents), and `SV_Target`, and drops the fixed-function `SetTexture` line. Verified visible in the editor.
- Probe: `Tools -> MarsXPLR -> Run Buggy Flight Probe` drives a take-off, glide, roll, airbrake, stall, landing, and wing close on the probe pad and writes `Unity6/Logs/flight-probe.txt`.

Verified in the Unity `6000.3.26f1` editor on macOS `27.0.1` (probe pad, full flow from `Init`, no errors logged): the wings open (state 1 to 2) and close (-1 to 0) and draw at a 9 m span as translucent glass in the accent colour; the buggy takes off at about 65 MPH with a short pull-up, glides with the gear retracting, rolls, lands at about 45 MPH, and returns to drive mode level and without losing momentum. A long full pull-up loops the buggy, stalls it at the top (angular drag 1, nose-down pitch), and it recovers in a dive. The airbrake and stall steps in the scripted run mostly ran after touchdown, so airborne airbrake behaviour is unverified.

Not yet verified or ported: comparison with the Wine build, flight on Fort Boxy terrain, the wing-flex animation viewed close up, LOD 1 wings, lava floating and diving, and player-driven flight with real keyboard input.

## Milestone 3: Representative world vertical slice

- Restore one bounded original environment suitable for vehicle and camera evaluation.
- Replace fallback ground when representative world geometry is available.
- Make world and base discovery deterministic for full-flow and direct-scene entry.
- Audit missing scripts and incompatible shaders within the selected environment only.
- Validate suspension feel, scale, speed perception, altitude, minimap, arrow, and camera obstruction behaviour.

Status (2026-10-18): `Game` now loads Fort Boxy (`Whirlds/FortBoxy/Fort Boxy.unity`) additively in place of `FallbackGround`. Fort Boxy is the first entry in the original `Game` scene's built-in world list, which the game used when the update server was unreachable, so it is the world an offline original build opens on. `World` loads the scene, makes it the active scene so its skybox, fog, and ambient light apply, then repeats the standard-component checks from `Game.js` `Init()`: find `Base` (or instantiate `Resources/Base`), collect terrains and `Sea`, and instantiate `Resources/Floor` and `Resources/Light` when the world has none. The player spawn waits for the load. `FallbackGround` remains only as a last resort when no ground is found under `Base`.

Verified in the Unity `6000.3.26f1` editor on macOS `27.0.1`: both `Init -> InitGame -> Game` and direct `Game` entry log `[World] Loaded world: Fort Boxy`, spawn the buggy on the castle beside `Base`, create no fallback ground, and log no errors. The chase camera starts at y = 10000 and descends to the buggy; it only does so while the editor is advancing frames (an unfocused editor or locked screen leaves the Game view showing flat fog colour).

Base marker (2026-10-19): `BaseSimpleModern` ports `BaseSimple.js`, whose missing script left the base marker's `Outer` and `Inner` cylinders at their smallest size. They now grow with camera distance (0.5 to 10 times), spin, and scroll their texture. `World` attaches it when the world loads. Verified in the editor: the marker is visible from the far camera.

Parity fixes from a Wine comparison (2026-10-19):

- Sky: `Settings` now sets the camera's far clip as `Settings.js` did (`worldViewDist` 5000, capped by the saved `viewCap`) and derives `worldFog` from it, so `CameraVehicle` clears to the skybox beyond 2000 m. `World` keeps the `Game` scene's skybox if the world scene brings none. Verified in the editor: the `Space` skybox shows and the far clip is 5000.
- Chase camera: `Settings` now loads the saved camera preferences on start as `Settings.js` `getPrefs()` did (`cam`, `camChase` default 1, `camDist`, `flightCam`, `gyroCam`, `renderLevel`, `viewCap`). The `Game` scene had serialised `camChase` 0 (Smooth), whose world-space `Vector3.one` offset left the camera diagonally behind; the original default is 1 (Agile), directly behind along the velocity. Verified in the editor: the camera sits level and directly behind while driving straight.
- Base marker: `BaseMarkerModern` ports `WhirldLOD.js` as configured on `Resources/Base.prefab` (`LOD0`/`LOD1`, `lodLevMax` 3), so at the default render level the `LOD1` cylinders hide and `LOD0` shows. `LOD0`'s legacy particle emitter, which Unity no longer supports, is rebuilt as a `ParticleSystem` from the original values (read from the Unity 4 prefab's type tree): 20 per second, 5 to 7 s life, `Lightning` additive material (its own copy with untiled texture, since `BaseSimpleModern` tiles the cylinder's copy), alpha fading in and out, vertical random force, at most 0.3 of the screen. The legacy `sizeGrow` and `damping` semantics are not reproduced exactly: the damping is omitted and size growth (about 20 times over a particle's life) is tuned by side-by-side comparison with the Wine build. Verified in the editor: the cylinders hide and the glow's size and column shape were judged close to Wine at near and far range. Not yet compared: brightness of the white core close up.

Rendering workaround (2026-10-19): in Unity `6000.3.26f1` on Metal, Fort Boxy's AirCastle renderers that share the `Object` material drew with the wrong texture, or not at all, depending on camera distance and draw order. The spawn slab looked like the floor and the hanging pyramids vanished. A material copy per renderer draws them correctly; a single shared fresh copy does not. Ruled out: shader (`Diffuse Fast` and `Diffuse` behave the same), lightmaps, occlusion data, dynamic batching (already off), mesh bounds and mipmaps. The likely cause is the legacy meshes' differing vertex layouts across draws that share a material, but this is unconfirmed. `World` gives every world renderer its own material copy after loading. Verified in the editor at near and far camera distances.

World textures (2026-10-19): in Fort Boxy as captured in this repository, 880 `Castle` and `CenterRamp` cubes used Unity's white default material, and 48 `Castle` pyramids had no mesh, so they were neither drawn nor solid. The original builds these primitives at load time in `WhirldIn.js`, using the Whirld `Object` hex material, and its `TextureObject()` gives them world-space UVs (one texture repeat per metre). `World` now restores both. Primitives named `cube`, `pyramid`, `cone`, or `mesh` that are still on a default material get the `Object` material from `Resources/Cube` and a port of `TextureObject()`. Pyramids without a mesh get a generated one, with a matching mesh collider. The source `Pyramid.blend` imports only when Blender is installed, and its converted FBX is empty. The generated mesh has the `.blend` geometry at half size: base corners at ±0.5 and the apex at (0, 0.5, 0). At that size the pyramids sit flush on the surrounding geometry: tower bases rest on the ground, and the spire bases meet the roof tops at y = 85. Verified in the Unity `6000.3.26f1` editor: the castle shows the same hex pattern everywhere, as in the Wine version, and the pyramids are drawn.

Lighting (2026-10-19): Fort Boxy rendered at about half the Wine version's brightness (orange floor about 62–67 against 110–120 in screenshots). The built-in legacy shaders of Unity 4 and earlier (the original shipped on them; `unity/` is on 4.6.1f1) finished with `texture * lighting DOUBLE`, so lights and ambient light counted twice. Unity 5 dropped the doubling. After the world loads, `World` now doubles every light's intensity and the ambient colour once. Fort Boxy's only light is the `Resources/Light` directional light (intensity 1). Ambient is flat 0.2 and the colour space is Gamma, as in the original. The 0.2 ambient comes from the scene in this repository. Downloaded worlds set their own ambient light (`rndAmbientLight` in `.utw` files), and the author's example exports use about 0.5–0.66. Fort Boxy's real value is unknown, because its world file is not in the repository history or the Wine install. Verified in the Unity `6000.3.26f1` editor: the floor now measures about 116–131, matching the Wine version.

Open: Fort Boxy's original world parameters (fog, floor texture, `ccc`) came from the downloaded `.utw` file and are not restored.; world objects stay on layer 0 rather than the terrain layer 8 the camera and minimap test against; the `Base`, `Floor`, and Whirld library objects still carry missing legacy scripts; not yet checked in an arm64 player build.

## Milestone 4: Gameplay loop parity

Replace scaffolding with dedicated systems:

- `CombatSystemModern` for fire modes, cooldowns, projectiles, lock/snipe behaviour, hits, and feedback;
- `GameModeSinglePlayerModern` for quarry selection, score flow, player lifecycle, and local game rules;
- original-style HUD, message feed, crosshair variants, telemetry, camera feedback, minimap, and guidance.

Do not make placeholder cylinders, debug-line lasers, or impact stubs part of the permanent architecture.

## Milestone 5: Rendering and asset compatibility

- Establish one approved source for each converted model and texture.
- Preserve original colours and materials with supported shaders.
- Classify legacy effects as gameplay-critical, cosmetic parity, or permanent replacement.
- Remove runtime dependencies on unsupported legacy scripts and editor-only repair paths.
- Adopt Force Text serialisation only as a dedicated change after a verified baseline, with scene and prefab diffs reviewed separately.

## Milestone 6: Release path

The Unity 6.3 LTS baseline is accepted only when it provides:

- clean script compilation;
- deterministic scene flow;
- vehicle and world smoke tests;
- generated asset integrity;
- arm64 player launch;
- rendering and input behaviour within documented tolerances.

After the arm64 path is stable:

- decide the release scripting backend (IL2CPP was the earlier intent; development builds use Mono);
- produce a universal macOS player;
- inspect the application with `file`/`lipo` to confirm both architectures;
- test Apple-silicon launch without Rosetta;
- test Intel launch where suitable hardware or CI is available;
- document signing, notarisation, packaging, and minimum supported macOS.

## Validation strategy

### Static checks

- Every source asset has its matching `.meta` and vice versa.
- Required scenes and generated assets exist.
- Only one automatic offline scene-flow coordinator exists.
- Modern runtime code does not gain new legacy networking dependencies.
- Documentation and roadmap state match the working tree.

### Unity Editor checks

- Clean import and compile with zero blocking errors.
- Game-scene preparation and baseline validation.
- Direct `Game` Play mode smoke test.
- `Init -> InitGame -> Game` smoke test.
- Missing-script and critical-reference checks.

### Player checks

- arm64 development build completes and launches natively.
- Fresh launch reaches the playable single-player path without online services.
- Input, camera, HUD, buggy, representative terrain, and reset path work.
- No critical missing-script, missing-reference, shader, or asset-loading failures occur.

## Immediate next actions

1. Verify direct `Game` Play mode entry and buggy controls in the editor.
2. Check the image effects render correctly under Metal.
3. Save and review only intentional scene changes.
4. Lock that evidence-backed baseline in version control.
5. Resolve the Fort Boxy spawn height (the buggy rolls off a lower slab at spawn).

## Unity 6.3 import results

First import on 2026-10-13 with Unity `6000.3.26f1` on macOS `27.0.1` (`arm64`), batch mode:

- Script compilation succeeded with no errors; the API updater changed no scripts. Obsolete-API warnings remain (`Rigidbody.velocity`, `drag`, `angularDrag`, `FindObjectsOfType`, `SystemInfo.supportsImageEffects`, `GameObject.active`, and others).
- Batch `Validate Project Baseline` passed; `Game.unity` was not modified.
- The image-effect shaders under `Pro Standard Assets/Image Based/Resources/` imported without shader errors.
- Unity re-serialised 236 binary assets (materials, flares, the `Init`, `InitGame`, and `Lobby` scenes, some prefabs, and project settings) and generated per-scene `*Settings.lighting` files, `_TerrainAutoUpgrade/` terrain layers, and three `No Name` materials. These were committed as one dedicated upgrade change; `pre-unity6-upgrade` holds the previous state.

Known issues exposed by the import (pre-existing, not regressions):

- `Buggy`, `Jet`, `Hovercraft`, `Tank`, `LaserAuto`, `LaserSnipe`, and the Whirld `Sea`, `Base`, `JumpPoint`, and `Floor` prefabs still reference legacy UnityScript components, which Unity no longer compiles. Unity refuses to re-save prefabs with missing scripts, so these remain in their previous serialisation until modern components replace the missing ones.
- `Whirld/ResourceSources/Materials/Object.mat` uses `Legacy Shaders/Diffuse Fast`, which Unity 6 no longer supports.

### arm64 development player

Built with `Tools -> MarsXPLR -> Build macOS arm64 Development Player` (`MarsXPLRBuild.BuildMacArm64DevelopmentBatchMode` in batch mode) on 2026-10-15: Unity `6000.3.26f1`, macOS `27.0.1`, Mono scripting backend, Metal, development build, output `Unity6/Build/macOS-arm64/Mars Explorer.app` (not committed).

- `file` and `lipo` report a single `arm64` Mach-O executable, so it cannot run under Rosetta.
- A fresh launch reached `Game` through `Init -> InitGame -> Game`, spawned the buggy on fallback ground, and logged no exceptions.
- The build reports 9 errors, all missing legacy scripts on prefabs (`Buggy`, `Wings`, `Sea`, `JumpPoint`, `Floor`); the player logs the same missing components on `Init`, `Vehicle`, `Buggy`, `Wings`, `ORB`, and `Buggy Skidmarks`.
- `Lobby.unity` fails to parse during the build (line 171). `Lobby` is outside the single-player flow but still in Build Settings.
- The placeholder laser repeatedly reports hits on the player itself.
- Not yet verified: controls and image effects in the player, and a launch from a clean clone.

### HUD font

The Ethnocentric fonts were empty in every copy of the project because their data lived in classic Mac resource forks. The TrueType data was recovered from the AppleDouble sidecars in `unity/` and the fonts switched from "ASCII upper case" to Dynamic, which Unity 6's IMGUI text renderer requires. All Ethnocentric lowercase glyphs match its capitals, so presentation is unchanged. Verified in the Unity `6000.3.26f1` editor on 2026-10-15: no font warnings and HUD text renders.

## Editor-free compile status

`tools/compile-check.sh` against Unity `6000.3.26f1` passes for all predefined assemblies (2026-10-13), matching the editor compile.

The legacy image effects (`Pro Standard Assets/Image Based/` and the Unity Island copies) previously built materials from shader source strings, which modern Unity rejects. Those shaders now live, unchanged in substance, as fixed-function shader assets under `Pro Standard Assets/Image Based/Resources/`, and `ImageEffects.Blit` once again honours its blend mode instead of the earlier plain-copy stub. Still unverified in the editor:

- the fixed-function shaders render correctly under Metal;
- glow, colour correction, and motion blur match the original look in `Game`.

## Definition of done for the single-player milestone

A fresh checkout can be opened with the documented editor, validated without manual archaeology, built as a native macOS arm64 application, and launched into a recognisably original-feeling buggy experience with deterministic scene flow, representative terrain, original-style vehicle behaviour, camera and HUD, and a functional local quarry/combat loop.
