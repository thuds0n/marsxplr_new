# Other Mars Explorer community projects

Notes from a review on 2026-10-30 to 2026-10-31 (Unity `6000.3.27f1`, macOS `27.2.0`), updated 2026-11-19. This project will not adopt their approach, but their work is useful as a second reference.

## The Wine reference was a community build

Until 2026-11-02 every Wine comparison used a copy of Mars Explorer "4.0.2" from VIA256's community releases, not an official download. The reference is now the official Mars Explorer 2.22 Windows build under Wine (`~/Applications/Sikarugir/Mars Explorer 2.22.app`), the same version as `unity/`; see [docs/wine-reference.md](docs/wine-reference.md).

The community copy's player and content files are byte-identical to the official build's; only the script assemblies, `UnityEngine.dll` and `UnityDomainLoad.exe` differ. A decompiler comparison (Unity 2.6.1f3 player, macOS `27.2`, details in [docs/wine-reference.md](docs/wine-reference.md)) showed that its scripts are the official 2.22 scripts, decompiled and recompiled as C#. Most changes are online-only: a new update URL on gitea, an invented version number `4.0.2`, archive.org links, the account check and profanity filter removed, and a JavaScript pop-up helper for links. The vehicle, camera and quality code is unchanged. Three changes touch single player:

- the laser's target-prediction sampling is 10 times shorter than in `Rocket.js`;
- a new QuarryCam setting, off by default;
- a rewritten gzip step for worlds, which is broken for `.gz` meshes and compressed terrain.

Worlds work in the community build because its update file lists live world URLs, not because of any loader fix. Laser measurements against moving targets taken in the community build must be re-measured in the official build; its other handling and visual findings stand.

## The projects

- [GandalfPotter/Mars-Explorer-Guide](https://github.com/GandalfPotter/Mars-Explorer-Guide): a README-only player guide to playing Mars Explorer after it was discontinued. It points to the two VIA256 projects below:
  - "original game with fixes" (the decomp): described as the stable build with working multiplayer and worlds, Windows and Wine only;
  - "remastered" (the Unity port): work in progress, with more native platforms.
  
  Support is through the Terran Labs Discord, `#mars_xplr`.
- [VIA256/marsxplr-decomp](https://gitea.moe/VIA256/marsxplr-decomp) and its [releases](https://gitea.moe/via256/marsxplr-decomp/releases): the "original game with fixes", the official 2.22 scripts decompiled and recompiled as C# with online fixes, on the original Unity 2.6.1f3 player and content. The repository itself refuses automated fetches, so it was reviewed through the build it ships (see above). Not a parity reference: its laser target prediction differs from the original.
- [VIA256/unity3d-marsxplr](https://github.com/VIA256/unity3d-marsxplr) and its [releases](https://github.com/via256/unity3d-marsxplr/releases): a fork of the original source, upgraded in place to Unity `6000.6.7f1`. MIT licence, as in the original. The newest pre-release, `4.1.1-rc+20261025`, adds native Apple silicon and WebGL builds. Multiplayer is temporarily disabled because the old networking was removed, and the author plans to replace it.
- The Unity 5 compatibility fix: a Unity 5.0.0f4 port of the original project by Ethan (BluetoothBoy), merged into the original repository ([terran-labs/unity3d-marsxplr](https://github.com/terran-labs/unity3d-marsxplr), now archived) as pull request #1 on 2015-03-29. Its changes are Unity 5 API updates only, with the same game logic. This project keeps a local, ignored copy as `Unity5/`, used for asset extraction and as a secondary reference; where it disagrees with `unity/`, `unity/` wins.
- [via256.github.io/play](https://via256.github.io/play/): the WebGL build of that fork, playable in a browser.

## How the Unity fork was made

- About 160 commits, February to October 2026.
- Upgraded one Unity version at a time: 3.5, 4.7, 5.x, 2017, 2018, 2019, 2020, 2022, 2023, 6000.0, then 6000.6. Unity 6.6 is not an LTS release.
- Every UnityScript file rewritten as C#, line by line, keeping the original structure (about 13.7k lines). This covers the buggy, tank, jet, hovercraft, bots, `Game` and the Whirld loader.
- Other changes:
  - legacy particles converted to `ParticleSystem` on most prefabs;
  - `.jas` meshes replaced with FBX;
  - TerrainLayers;
  - `UnityWebRequest`;
  - text serialisation.
- Build profiles for macOS (universal), Windows, Linux and WebGL.
- No plan, verification notes or parity measurements are published. The late upgrade commits ("fix prefabs", "try to fix settings") suggest the Unity 6 upgrade was rough.

An in-place upgrade means opening the same project in each newer editor in turn and fixing whatever breaks, so the original structure, scenes and prefabs carry forward. This project instead rebuilds the runtime in `Unity6/` and uses the original only as a reference.

Faithfulness of their C#: their `Buggy.cs` was checked line by line against `Buggy.js` and matches, including the original's quirks (the empty raycast hit in the thermals, the integer `motorAccel`, the wing-vertex rotation bug). The only change found was `activeInHierarchy` in place of `active`, which makes no practical difference. The other vehicle scripts have not been checked line by line.

Comparison: the fork covers more of the game and more platforms today. This project uses an LTS editor, a modern architecture and verifies parity against the original. Their approach would mean leaving 6.3 LTS and reverting to the original 2015 architecture, so it is not adopted.

## Useful to this project

1. **A second translation of the original scripts.** Their `Buggy.cs`, `Vehicle.cs`, `Tank.cs`, `Jet.cs`, `Hovercraft.cs` and `VehicleBot.cs` give a separate translation of the UnityScript to check against when porting. A line-by-line check of their `Buggy.cs` found it faithful. It also covers lava floating and diving and the "new physics" motor, which are not yet ported here.
2. **Particle conversions** for Jet, Hovercraft, EntryPoint, JumpPoint, PlasmaSparks, Fire and the starfield, plus their legacy-particle updater editor script.
3. **Whirld loader fixes**: the `WhirldIn` OBJ parser typo, the sea mesh and scale, JumpPoint timing, `.utw` comments, gzip for online assets. Check whether `WhirldLibraryModern` has the same bugs.
4. **Build profiles** for WebGL and the universal macOS player, for later.

## Parity findings from the comparison

Checked against the shipped 4.0.2 player data (read with UnityPy) and `unity/`:

- **Buggy physics.** Comparing both ports with `Buggy.js` found two differences in `BuggyControllerModern`, now fixed (see [docs/flight.md](docs/flight.md)):
  - lava thermals below `lavaAlt + 10` give a constant 400 N, because the original read an empty raycast hit there;
  - `motorAccel` is truncated to an integer, as in the original.
- **AirCastle spikes.** The original `Resources/Pyramid` is ±0.5. The fork's `Pyramid.fbx` (and `Cone.fbx`) is ±1, so in the web build the four hanging spikes are 30 m tall and pass through the spawn slab. This project's size is correct (see [docs/world-and-rendering.md](docs/world-and-rendering.md)).
- **Blue spawn column.** The column is `Base.prefab` `LOD0`'s legacy particle emitter. The fork dropped it, so the web build has no column. This project rebuilt it from the original values.
- **Buggy body and wheels.** The fork's vehicle materials reference the `Space` reflection cubemap by its old GUID, but their reimported cubemap has a new GUID. So the web build reflects a flat default instead of the starfield: brighter, even rims and a uniform, matte-looking body. This project's materials and cubemap are byte-identical to the shipped game's (see [docs/buggy.md](docs/buggy.md)). It looks shinier than the original by eye, which is not yet explained; the next check is a close-up beside the Wine version.
- **Contrast swings** in the web build are very likely the contrast-stretch problem this project fixed on 2026-10-20. In the original, the effect runs only above Good quality, and it reads the alpha the original shaders wrote. Unity 5 and later write alpha 1, so the effect measures the wrong values. This project restores that alpha with `keepalpha` (commit `8dc659c`). The fork does neither.
- **Spawn pavilion pillars.** `FortBoxy.utw` builds the AirCastle as a pavilion. It has a 30 × 30 m floor slab with its top at 30 m, eight L-shaped corner pillars from 30 m to 45 m, a roof slab with pyramids, and four spikes hanging below. This project's scene has all of these pieces, and they load in Play mode. A few pillars are 8 m long rather than 10 and sit 0.5 m from the `.utw` values, because the scene was captured from an older layout.
- The shipped player includes `Assembly - UnityScript.dll`. Decompiling it could confirm compiled behaviour, such as the `motorAccel` truncation.
