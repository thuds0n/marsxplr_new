# Mars Explorer Modernisation Plan

## Objective

Deliver a recognisably original Mars Explorer single-player experience as a native macOS application, targeting Apple silicon (`arm64`) first and a universal binary only after the arm64 path is stable.

`Unity6/` is the canonical runtime project. Its baseline editor is Unity 6.3 LTS (`6000.3.27f1`), supported until December 2027. `unity/` is retained as original behavioural and asset reference material; the ignored local `Unity5/` project is an asset-conversion bridge only.

## Documentation

Detail lives in `docs/`, one file per area. This file keeps status, decisions and next work only.

- [docs/buggy.md](docs/buggy.md): buggy ground model, wheels, tyre tracks, camera, body and colour, handling comparison.
- [docs/flight.md](docs/flight.md): wings and flight.
- [docs/world-and-rendering.md](docs/world-and-rendering.md): Fort Boxy, lighting, fog, materials, shaders, HUD font.
- [docs/validation.md](docs/validation.md): validation strategy, Unity 6.3 import results, editor-free compile status.
- [docs/release.md](docs/release.md): release path and arm64 player builds.
- [docs/combat.md](docs/combat.md): lasers, quarry, score and bots.
- [docs/ui.md](docs/ui.md): the in-game side-panel menu and Server Settings.
- [docs/wine-reference.md](docs/wine-reference.md): how to run and measure the original for comparisons.
- `IMPROVEMENTS.md`: faithful-but-improvable behaviour deferred from parity work, and the deliberate departures already made.

Record new findings in the matching `docs/` file, dated and with exact Unity and macOS versions, and update the status lines here.

## Current status

The single-player buggy slice runs from `Init` in the Unity `6000.3.27f1` editor and in an arm64 development player:

- the offline flow is `Init -> InitGame -> Game`, verified in the editor from `Init`; `Lobby` stays outside the active slice;
- `BuggyControllerModern` drives the buggy (`useModernBuggyController` is on by default); `SimplePlayerController` remains only as a selectable fallback;
- `VehicleStateModern` is the shared vehicle-state seam;
- the buggy uses the original body, wheel, axle and wing models, and wings and flight are ported (Milestone 2);
- replacement C# components cover the camera, HUD, minimap, Fort Boxy's world loading and settings, and the Messaging Console with chat; of the menu panels only Server Settings is ported;
- laser fire, `Rocket.js` projectiles and impact sparks, hits, quarry scoring, weapons lock, bots, fatal hits and ramming (RORB sphere and body contact) are ported; bot name tags, the materialisation effect and XORBs are not;
- editor tooling rebuilds and inspects the `Game` scene baseline and runs scripted Play-mode tests under `Tools -> MarsXPLR`.

The last arm64 development player was built and launched on 2026-11-19 from `combat-parity-rorbs` (Milestone 6, [docs/release.md](docs/release.md)); it includes the combat, messaging and parity-audit work.

## Decisions

- Order of authority for parity:
  1. The original code and assets decide behaviour and values. `unity/` is Aubrey Falconer's open-source release (2.22, upgraded by him to Unity 4.6.1f1 with no script changes). The ignored local `Unity5/` is the "Mars Explorer - Unity 5 Compatibility Fix" folder: a Unity 5.0.0f4 port by Ethan (BluetoothBoy), merged by the author as pull request #1 on 2015-03-29. It holds the same logic with Unity 5 API updates only, so either can be read. If they ever disagree on logic, `unity/` wins. Assets are different: every asset in `Unity6/` was converted through the Unity 5 editor, which reinterprets lighting, ambient, shaders and physics on import, so treat converted values in those areas as Unity 5's reading until checked against `unity/` (audit in [docs/world-and-rendering.md](docs/world-and-rendering.md)).
  2. How the engine the game shipped on (Unity 4 and earlier) rendered that content decides the look. Neither `Unity5/` nor a modern editor shows the original look, for example the doubled legacy lighting.
  3. The official Mars Explorer 2.22 Windows build under Wine (`~/Applications/Sikarugir/Mars Explorer 2.22.app`, recovered 2026-11-02; same version as `unity/`) is evidence for checks and for data found nowhere else (downloaded worlds, values in binary prefabs). Values taken only from it are recorded as inferences. The earlier community "4.0.2" Wine build has the same engine and content but recompiled scripts with a changed laser lead; laser and bot-accuracy figures from it must be re-measured (see [docs/wine-reference.md](docs/wine-reference.md)).

- Record faithful-but-improvable behaviour in `IMPROVEMENTS.md` rather than changing it during parity work.
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
- [x] Keep `Lobby` in the project without making it part of the active path. It was removed from Build Settings on 2026-10-20 because its scene YAML does not parse (see [docs/release.md](docs/release.md#arm64-development-player)).
- [x] Add a Unity batchmode baseline-validation entry point.
- [x] Separate buggy flight state from the legacy `inputThrottle` state flag.
- [x] Stage every PNG source asset alongside its existing `.meta` (static check: no asset without a `.meta`; the only `.meta` files without tracked sources are empty folders).
- [x] Stop tracking IDE-generated solution and project files in `Unity6/`.
- [x] Anchor root-only ignore rules so `Unity6/Assets/Whirld/Docs/` is no longer ignored on case-insensitive file systems.

### Required before the baseline is locked

- [x] Add `tools/compile-check.sh`, an editor-free compile check against Unity 6 reference assemblies.
- [x] Replace the 12 Cheetah 3D (`.jas`) models in `Unity6/` with Cheetah's own FBX export, so imports no longer need Cheetah 3D; Unity's import is unchanged and the models render intact (`tools/jas2fbx/`, [docs/world-and-rendering.md](docs/world-and-rendering.md)).
- [x] Replace the five Blender (`.blend`) models with FBX from Unity's own Blender exporter; Unity's import is unchanged and the tank, tank tracks, simple buggy wheel and `Pyramid.prefab` resolve and render (`tools/blend2fbx/`, [docs/world-and-rendering.md](docs/world-and-rendering.md)).
- [ ] When Unity Island is brought in: repoint `IslandsWhirld.unity`'s fence from the Maya file `Whirlds/Unity Island/Bridges/trailFence.mb` (imports only with Maya installed) to the author's own 2006 FBX export beside it, `trailFenceFBX.fbx` (same `fencePost` and `fenceRope` meshes, currently unused). Mesh IDs differ, so check the swap on screen, then remove the `.mb` from `Unity6/`.
- Decision (2026-11-01): FortBoxy `Castle.prefab`'s 12 pyramids keep their dangling mesh reference, inherited from the original project. `World` generates them at runtime at the shipped ±0.5 size, as `WhirldIn.js` did; repointing them at `Pyramid.fbx` would only tidy the editor view and needs a reviewed rewrite of a binary prefab.
- [x] Fix the editor-free compile errors (see [docs/validation.md](docs/validation.md#editor-free-compile-status)).
- [x] Open the project in Unity `6000.3.26f1`, let the API updater run, and complete a clean script compile.
- [x] Review scene, prefab, and project-setting re-serialisation against `pre-unity6-upgrade` before committing it (binary assets reviewed by file list only).
- [x] Remove the `com.unity.multiplayer.center` package Unity 6 added during import.
- [x] Run `Prepare Game Scene Baseline` with `Game.unity` open and review all changes before saving (2026-10-21: no intended changes; the save only re-serialised the scene and was reverted, see [docs/validation.md](docs/validation.md#milestone-0-checks-2026-10-21)).
- [x] Run the batch `Validate Project Baseline` check.
- [x] Run the interactive `Validate Project Baseline` check.
- [x] Verify direct Play mode entry from `Game.unity`.
- [x] Verify the complete `Init -> InitGame -> Game` flow (Unity `6000.3.26f1` editor, 2026-10-13: reaches `Game`, spawns the buggy on fallback ground, no exceptions; required driving later scene hops from `SceneManager.sceneLoaded`).
- [x] Create and launch an arm64 development player on Apple silicon (see [docs/release.md](docs/release.md#arm64-development-player)).
- [x] Repeat the import and smoke test from a clean checkout or clean clone.
- [x] Record the Unity version, macOS version, scripting backend, architecture, and known warnings.

## Milestone 1: Original buggy ground model

Create `BuggyControllerModern` without folding new physics into `SimplePlayerController`.

Status: complete (2026-10-21). Steps 1–8 are ported and match the Wine version on a flat pad and on Fort Boxy; the wheels respond independently on uneven ground and the body uses the player colour. Open: the `Buggy Skidmarks` track width, lava floating and diving, the optional "new physics" motor. Detail: [docs/buggy.md](docs/buggy.md).

Acceptance criteria:

- [x] The buggy rests on its wheel contacts rather than a capsule approximation.
- [x] All four wheels respond independently to uneven ground (Fort Boxy ramp test, 2026-10-21; see [docs/buggy.md](docs/buggy.md#uneven-ground)).
- [x] The body and wheel beadlocks use the player colour from `VehicleStateModern` (`Vehicle.js` `setColor()`, rechecked 2026-10-21).
- [x] Acceleration, reversing, steering, braking, side slip, and landing are controllable (flat-pad probe compared with the Wine version, 2026-10-19).
- [x] Camera and HUD telemetry continue to consume `VehicleStateModern`.
- [x] `SimplePlayerController` can still be selected temporarily for recovery (`useModernBuggyController` on `SinglePlayerPlayableBootstrap`).
- [x] Behaviour is tested on a small representative Mars terrain fixture, not only `FallbackGround` (Fort Boxy, compared with the Wine version on 2026-10-21; see [docs/buggy.md](docs/buggy.md)).

## Milestone 2: Wing and flight parity

- Port the original special-input and wing deployment state machine.
- Port wing mesh/flap presentation without mutating shared source meshes at runtime.
- Port lift, drag, stall, airbrakes, ground effect, take-off, and landing behaviour.
- Keep `specialInput`, `flightMode`, `camSmooth`, brakes, and throttle-channel selection as distinct state concepts. `BuggyControllerModern` keeps them separate; `VehicleStateModern.SetFlightMode` still sets the first three together and is used only at spawn and by the `SimplePlayerController` fallback.
- Validate drive-to-flight and flight-to-drive transitions against recorded parity scenarios.

Acceptance criteria:

- [x] Wing opening and closing have deterministic state and presentation.
- [x] Take-off requires plausible speed and surface interaction.
- [ ] Stall, roll, pitch, drag, airbrakes, and landing remain controllable (all but airborne airbrakes shown by the flight probe).
- [x] Returning to drive mode does not reset orientation or momentum incorrectly.

Status: ported and verified on the probe pad by `Run Buggy Flight Probe`. Judged comparable to the Wine version by eye on 2026-10-21; an instrumented comparison (take-off, stall, landing) is backlogged. Detail: [docs/flight.md](docs/flight.md).

## Milestone 3: Representative world vertical slice

- Restore one bounded original environment suitable for vehicle and camera evaluation.
- Replace fallback ground when representative world geometry is available.
- Make world and base discovery deterministic for full-flow and direct-scene entry.
- Audit missing scripts and incompatible shaders within the selected environment only.
- Validate suspension feel, scale, speed perception, altitude, minimap, arrow, and camera obstruction behaviour.

Status: Fort Boxy loads offline, its sky, fog, lighting, base marker and textures match the Wine version, and its own settings and welcome message are applied (2026-11-17). Open: laser aimer colour. Spawn light and horizon traced from source on 2026-10-23: light matches (engine differences only), `VehicleLight` now disabled as in `updatePrefs()`, horizon settings match; the dark band below the horizon at Fantastic was point-sampled SSAO depth and now reads interpolated decoded depth for the pixel and its samples; confirmed by hand in the editor; the faint speckle at steep upward chase angles also shows in Wine (arm64 player not yet checked). Detail: [docs/world-and-rendering.md](docs/world-and-rendering.md).

## Milestone 4: Gameplay loop parity

Replace scaffolding with dedicated systems:

- `CombatSystemModern` for fire modes, cooldowns, projectiles, lock/snipe behaviour, hits, and feedback;
- `GameModeSinglePlayerModern` for quarry selection, score flow, player lifecycle, and local game rules;
- original-style HUD, message feed, crosshair variants, telemetry, camera feedback, minimap, and guidance.

Do not make placeholder cylinders, debug-line lasers, or impact stubs part of the permanent architecture.

Status: in progress. First slice (2026-10-21): auto and snipe fire, `Rocket.js` projectiles, hits, the player defaulting to quarry and tag scoring, with the placeholders removed (the last leftovers went on 2026-11-19). Verified by a scripted Play-mode test and by firing by hand from `Init` (the buggy now has the original `RideCam`, and the impact sparks are rebuilt as a `ParticleSystem`). Not yet compared with the Wine version; sound not confirmed by ear. Weapons lock, lock-on shots and bots ported and tested 2026-10-23. Fatal hits and ramming (RORBs 2026-11-02; body contact, the world's own settings, the Messaging Console and the original start-of-game rules 2026-11-17, play-tested in the editor and by hand 2026-11-18) are ported. Open: name tags, the materialisation effect, XORBs. Menu: Server Settings ported 2026-10-23; Game, Player, Active Players, Switch Vehicle and Settings Advisor panels still to port. Detail: [docs/combat.md](docs/combat.md).

## Milestone 5: Rendering and asset compatibility

- Establish one approved source for each converted model and texture.
- Preserve original colours and materials with supported shaders.
- Classify legacy effects as gameplay-critical, cosmetic parity, or permanent replacement.
- Remove runtime dependencies on unsupported legacy scripts and editor-only repair paths.
- Adopt Force Text serialisation only as a dedicated change after a verified baseline, with scene and prefab diffs reviewed separately.

Shader and material findings so far are in [docs/world-and-rendering.md](docs/world-and-rendering.md).

## Milestone 6: Release path

Status: an arm64 Mono development player of `combat-parity-rorbs` (2026-11-19, Unity `6000.3.27f1`, macOS `27.2.1`) builds with only the 4 known water shader errors and launches into Fort Boxy with no exceptions or missing scripts. Chat, the HUD at Retina size, driving, wings, firing by hand, a bot from Server Settings, the crosshair and the `0` fullscreen key work in the player. Open: a clean-clone build, the release scripting backend and the universal player. Acceptance criteria, later steps and build records: [docs/release.md](docs/release.md). Validation strategy and import results: [docs/validation.md](docs/validation.md).

## Immediate next actions

The evidence-backed baseline is tagged `unity6-baseline-2026-10-21` (Milestone 0 complete; buggy ground handling, Fort Boxy, HUD, image effects and shadows checked against the Wine version).

1. The 2026-11-17 parity fixes ([docs/parity-audit.md](docs/parity-audit.md)) passed the editor play-test, the scripted tests and a hand test on 2026-11-18 (chat Tab and focus fixed for Unity 6). The 2026-11-19 follow-up fixed the camera keys and snipe look, the "L Hit ORBs" toggle, the frame-rate count and leftovers (C1-C4, C6, C8, S3, G7, G8, G14), passing the scripted tests in the editor; the snipe look speed is not yet tried with a real mouse. A third pass the same day fixed most Low rows (RORB lifecycle and flashes, HUD readout, settings loading, tracks, trails, roam camera, language filter); the Low rows still open are listed in the audit. Next, continue Milestone 4: the remaining audit rows, comparing the sparks and sounds with the Wine version, and the remaining menu panels.
2. Fort Boxy rendering (2026-10-21): the shadow strip east of the spawn slab no longer reproduces and the `Arrow.jas` mesh matches the original cone. The arrow now points at the base as the original did. The floor writes its original alpha again, so the contrast stretch saturates it as the original code did (see [docs/world-and-rendering.md](docs/world-and-rendering.md)).

## Definition of done for the single-player milestone

A fresh checkout can be opened with the documented editor, validated without manual archaeology, built as a native macOS arm64 application, and launched into a recognisably original-feeling buggy experience with deterministic scene flow, representative terrain, original-style vehicle behaviour, camera and HUD, and a functional local quarry/combat loop.
