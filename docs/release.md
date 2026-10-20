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
