# Mars Explorer Modernisation Plan

## Objective

Deliver a recognisably original Mars Explorer single-player experience as a native macOS application, targeting Apple silicon (`arm64`) first and a universal binary only after the arm64 path is stable.

`Unity6/` is the canonical runtime project. Its baseline editor is Unity 6.3 LTS (`6000.3.26f1`), supported until December 2027. `unity/` is retained as original behavioural and asset reference material; the ignored local `Unity5/` project is an asset-conversion bridge only.

## Current status

The repository contains a promising but incomplete modernisation foundation:

- replacement C# components cover the active camera, HUD, messaging, minimap, world, settings, and game scaffolding;
- `SimplePlayerController` provides temporary drive and flight behaviour;
- `VehicleStateModern` provides a shared vehicle-state seam;
- a generated buggy visual and runtime bootstrap provide a playable placeholder vehicle path;
- quarry, laser, and score behaviour are placeholders rather than parity ports;
- editor tooling can rebuild and inspect the `Game` scene baseline;
- the intended offline flow is now `Init -> InitGame -> Game`, with `Lobby` retained outside the active vertical slice.

The current working tree has not been compiled or played in Unity since its newest changes. Cached assemblies predate the latest scene-flow, validation, and vehicle-state work. There is no verified arm64 player build yet.

## Decisions

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

Acceptance criteria:

- [ ] The buggy rests on its wheel contacts rather than a capsule approximation.
- [ ] All four wheels respond independently to uneven ground.
- [ ] Acceleration, reversing, steering, braking, side slip, and landing are controllable.
- [ ] Camera and HUD telemetry continue to consume `VehicleStateModern`.
- [ ] `SimplePlayerController` can still be selected temporarily for recovery.
- [ ] Behaviour is tested on a small representative Mars terrain fixture, not only `FallbackGround`.

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

## Milestone 3: Representative world vertical slice

- Restore one bounded original environment suitable for vehicle and camera evaluation.
- Replace fallback ground when representative world geometry is available.
- Make world and base discovery deterministic for full-flow and direct-scene entry.
- Audit missing scripts and incompatible shaders within the selected environment only.
- Validate suspension feel, scale, speed perception, altitude, minimap, arrow, and camera obstruction behaviour.

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
5. Begin `BuggyControllerModern` with wheel contacts and suspension.

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
