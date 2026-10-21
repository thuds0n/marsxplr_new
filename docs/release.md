# Release path

Detail for Milestone 6 in [PLAN.md](../PLAN.md). Summary: an arm64 Mono development player builds and launches into Fort Boxy. Open: controls and image effects in the player, a clean-clone build, the release scripting backend, and the universal player.

## Acceptance

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

## arm64 development player

Built with `Tools -> MarsXPLR -> Build macOS arm64 Development Player` (`MarsXPLRBuild.BuildMacArm64DevelopmentBatchMode` in batch mode) on 2026-10-15: Unity `6000.3.26f1`, macOS `27.0.1`, Mono scripting backend, Metal, development build, output `Unity6/Build/macOS-arm64/Mars Explorer.app` (not committed).

- `file` and `lipo` report a single `arm64` Mach-O executable, so it cannot run under Rosetta.
- A fresh launch reached `Game` through `Init -> InitGame -> Game`, spawned the buggy on fallback ground, and logged no exceptions.
- The build reports 9 errors, all missing legacy scripts on prefabs (`Buggy`, `Wings`, `Sea`, `JumpPoint`, `Floor`); the player logs the same missing components on `Init`, `Vehicle`, `Buggy`, `Wings`, `ORB`, and `Buggy Skidmarks`.
- The placeholder laser repeatedly reports hits on the player itself.
- `Lobby.unity` failed to parse during the build (line 171).
- Not yet verified: controls and image effects in the player, and a launch from a clean clone.

Rebuilt on 2026-10-20 with the same toolchain (Unity `6000.3.26f1`, macOS `27.0.1` build `26A434`) after the buggy body, `VehicleColourModern`, Whirld texture and lighting changes:

- `Lobby` was removed from Build Settings rather than repaired. Its re-serialised YAML nests a block-style colour inside a flow mapping (a stray `}` at line 171), so fixing it means hand-editing a serialised scene that Unity cannot then verify; nothing in `Init -> InitGame -> Game` loads it, and baseline validation only requires the scene asset to exist. The asset stays in the project as reference content. `LobbyStarfield` keys off build index 1, which is now `InitGame`, but only acts when given a `Lobby` component, so it stays inert.
- The build succeeded (164 MB) with no Lobby parse error. `file` and `lipo` again report a single `arm64` executable. The 8 build errors are Metal shader compile errors in `Hidden/SSAO`, `ForceField`, and `FX/Island Water Simple`/`Full`; missing legacy scripts are now reported as warnings (including `Fort Boxy`'s `Castle`, `AirCastle`, and `Base`).
- A fresh launch reached Fort Boxy through `Init -> InitGame -> Game` (`[World] Loaded world: Fort Boxy`, space skybox) and logged no exceptions; the only errors are the known missing-script messages.
- On screen: the original buggy body in a random player colour (purple) with wings, wheels and axles; the hex-textured castle; and the brighter orange floor. Pyramid meshes were not in view and remain unconfirmed in the player.

Build of 2026-10-21 (Unity `6000.3.26f1`, macOS `27.0.1`, Mono, arm64 only): succeeded (164 MB). The first build of the day reported the 8 known Metal shader errors; the later incremental builds reported none. A fresh launch reached Fort Boxy through `Init -> InitGame -> Game` with no exceptions, including the Fort Boxy spawn, trail and handling changes of 2026-10-20 and 2026-10-21.

HUD size on Retina displays: the IMGUI HUD is laid out in pixels. Unity 2.5 and the Wine version see a Retina display at its size in points (1512 × 982 on a 14-inch MacBook Pro), but the Unity 6 player picked the full pixel size (3024 × 1898), even with `macRetinaSupport` off, so the bottom readout and minimap drew at half size. The build script now turns `macRetinaSupport` off, and `Settings` halves the resolution at startup on macOS when the display is above 150 dpi and the game is running at more than three-quarters of the desktop's pixel width (Unity restores the last resolution on launch, so it is not halved twice). macOS gives the player no backing scale, so the 150 dpi threshold is a heuristic: a scaled 4K display below that density would keep its full pixel size. Measured in the player: the readout is 8 points tall against 9 in the Wine version, in a 949-point-tall window against 982, and the minimap takes the same share of the window. The 3D view now also renders at point resolution, as it did in the original.

Build of 2026-10-21, evening (Unity `6000.3.26f1`, macOS `27.0.1`, Mono, arm64): succeeded (164 MB; 4 errors, all the known island water and `FX/Water` Metal shader errors). It includes the floor, guidance arrow, SSAO, quality-level and shadow fixes of 2026-10-21. A fresh launch at Beautiful reached Fort Boxy through `Init -> InitGame -> Game` at 1512 × 949 with no exceptions and no missing scripts. Driven by hand under the spawn slab (reverse, then Command to hold the buggy with the `Fire3` brake): the slab's underside shows overhead and the floor under it is in shadow, with sunlit patches only further out, as in the editor after the shadow fix and in the Wine version.

Build of 2026-10-21, late (Unity `6000.3.26f1`, macOS `27.0.1` build `26A434`, Apple M4, Mono, arm64, from `master` at `8dc659c`): succeeded (164 MB; 4 errors, all the known island water and `FX/Water` Metal shader errors; 17 warnings). `file` reports a single `arm64` executable. It includes Milestone 1, the laser and quarry slice, the guidance arrow fix, and the floor and vehicle alpha fixes. A fresh launch at 1512 × 949 reached Fort Boxy through `Init -> InitGame -> Game`; the player log has no exceptions and no missing scripts.

- Driving (arrow keys, 43 MPH), steering, braking (Command) and the chase and first-person cameras (`1`, `2`) work.
- Flight: Space spreads the wings, and the buggy climbed to 30 ALT and glided back down.
- The log reports "Player Defaulted to Quarry", and the green guidance arrow shows at the bottom right.
- The floor draws in the saturated orange of the original, and the buggy body draws in its player colour with no transparency artefacts.
- Not confirmed: laser fire. With synthetic input (left Control, and the mouse in first person facing a wall) no bolt or sparks showed in screenshots, and the player logs nothing on firing, so this is a test limit or a player-only fault. Hits and scoring were not tested: offline there is no other vehicle to tag.

Laser fire in the player (2026-10-22, Unity `6000.3.26f1`, macOS `27.0.1`, arm64): the player fired nothing and played no laser sounds because `Game`'s `objectRocket`, `objectRocketSnipe`, `objectRocketExplosion` and `objectRocketSnipeExplosion` were never saved in `Game.unity`. Only an editor-only `AssetDatabase` fallback in `Game.Awake()` filled them, so the editor worked and the player had none, and the prefabs (which carry the fire and hit sounds) were left out of the build. `Prepare Game Scene Baseline` now assigns them, and `Game.unity` was saved with them. `CombatSystemModern` logs a warning once if they are missing. Rebuilt: succeeded (164 MB, the 4 known water shader errors); the four prefabs are now in the player's `sharedassets2.assets`, and a fresh launch reached Fort Boxy with no exceptions. Firing by hand in the player is still to be confirmed: synthetic input does not reach it.
