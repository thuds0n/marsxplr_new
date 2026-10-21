# Validation

How the project is checked, and the results of the Unity 6.3 import. Summary: the editor and editor-free compiles pass; batch baseline validation passes; `Init -> InitGame -> Game` reaches Fort Boxy. Open: interactive validation, direct `Game` entry, a clean-clone import, and the legacy image effects under Metal.

## Strategy

#### Static checks

- Every source asset has its matching `.meta` and vice versa.
- Required scenes and generated assets exist.
- Only one automatic offline scene-flow coordinator exists.
- Modern runtime code does not gain new legacy networking dependencies.
- Documentation and roadmap state match the working tree.

#### Unity Editor checks

- Clean import and compile with zero blocking errors.
- Game-scene preparation and baseline validation.
- Direct `Game` Play mode smoke test.
- `Init -> InitGame -> Game` smoke test.
- Missing-script and critical-reference checks.

#### Player checks

- arm64 development build completes and launches natively.
- Fresh launch reaches the playable single-player path without online services.
- Input, camera, HUD, buggy, representative terrain, and reset path work.
- No critical missing-script, missing-reference, shader, or asset-loading failures occur.

## Unity 6.3 import results

First import on 2026-10-13 with Unity `6000.3.26f1` on macOS `27.0.1` (`arm64`), batch mode:

- Script compilation succeeded with no errors; the API updater changed no scripts. Obsolete-API warnings remain (`Rigidbody.velocity`, `drag`, `angularDrag`, `FindObjectsOfType`, `SystemInfo.supportsImageEffects`, `GameObject.active`, and others).
- Batch `Validate Project Baseline` passed; `Game.unity` was not modified.
- The image-effect shaders under `Pro Standard Assets/Image Based/Resources/` imported without shader errors.
- Unity re-serialised 236 binary assets (materials, flares, the `Init`, `InitGame`, and `Lobby` scenes, some prefabs, and project settings) and generated per-scene `*Settings.lighting` files, `_TerrainAutoUpgrade/` terrain layers, and three `No Name` materials. These were committed as one dedicated upgrade change; `pre-unity6-upgrade` holds the previous state.

Known issues exposed by the import (pre-existing, not regressions):

- `Buggy`, `Jet`, `Hovercraft`, `Tank`, `LaserAuto`, `LaserSnipe`, and the Whirld `Sea`, `Base`, `JumpPoint`, and `Floor` prefabs still reference legacy UnityScript components, which Unity no longer compiles. Unity refuses to re-save prefabs with missing scripts, so these remain in their previous serialisation until modern components replace the missing ones.
- `Whirld/ResourceSources/Materials/Object.mat` uses `Legacy Shaders/Diffuse Fast`, which Unity 6 no longer supports.

## Editor-free compile status

`tools/compile-check.sh` against Unity `6000.3.26f1` passes for all predefined assemblies (2026-10-13), matching the editor compile.

The legacy image effects (`Pro Standard Assets/Image Based/` and the Unity Island copies) previously built materials from shader source strings, which modern Unity rejects. Those shaders now live, unchanged in substance, as fixed-function shader assets under `Pro Standard Assets/Image Based/Resources/`, and `ImageEffects.Blit` once again honours its blend mode instead of the earlier plain-copy stub. Still unverified in the editor:

- the fixed-function shaders render correctly under Metal;
- glow, colour correction, and motion blur match the original look in `Game`.

## Milestone 0 checks (2026-10-21)

Unity `6000.3.26f1`, macOS `27.0.1`, Apple silicon.

- `Prepare Game Scene Baseline`, run with `Game.unity` open: no missing scripts, and every replacement component was already in place. Saving produced no intended changes, only Unity 6 re-serialisation: newer `RenderSettings` and `LightmapSettings` versions, `Settings` gaining its newer serialised fields and dropping the removed `baseCamOffset`, and the guidance `Arrow` (an instance of `Martian Assets/GUI/Objects/Arrow.jas`) collapsed from an expanded Unity 2-era copy into a plain model reference with no overrides. That last change would drop the arrow mesh's stored rotation (90° about X), position (z −0.003) and material, so the save was reverted. Re-serialising `Game.unity` should be a dedicated change that keeps those values and checks the arrow on screen.
- Interactive `Validate Project Baseline`: passed ("Project baseline looks healthy").
- Direct Play mode entry from `Game.unity`: Fort Boxy loads, the buggy spawns, no exceptions or missing-script warnings.
- Clean clone: a fresh `git clone` of commit `6dcaaec`, opened with `-batchmode` and `MarsXPLRProjectTools.ValidateProjectBaselineBatchMode`, rebuilt the Library from nothing (1,010 assets imported), compiled with no errors, found no missing `.meta` files and passed the batch validation. Known import errors only: Blender and Maya are not installed (six `.blend`/`.mb` source models), and the unported legacy prefabs (jet, hovercraft, tank, `Sea`, `JumpPoint`, lasers) cannot be upgraded because of their UnityScript components. `MarsXPLRBuild.BuildMacArm64DevelopmentBatchMode` then built an arm64 development player from the clone (164 MB, the 6 known Metal shader errors in `Hidden/SSAO` and the island water shaders). A fresh launch went through `Init -> InitGame -> Game` to Fort Boxy at 1512 × 949 with no exceptions and no missing scripts.
