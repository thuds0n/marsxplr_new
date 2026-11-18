# Parity audit: `unity/` scripts against `Unity6/Assets`

Date: 2026-11-17. Branch `combat-parity-rorbs`, working tree including uncommitted changes. macOS 27.2.1.

Method: static reading only. Every script under `unity/Assets` was matched to its counterpart under `Unity6/Assets` and the two were compared line by line where a counterpart exists. Nothing was compiled, opened in Unity or played, so no finding here is confirmed at run time. Values held only in binary prefabs or scenes of `unity/` were not decoded; where a finding depends on one, it says so.

Follow-up (2026-11-17): summary items 1 to 6 and B1 were then fixed (V1, V2, V3, M1, M2, L1, L2, G1 to G4, G6, S1, C5, X1, X2, and the `terrainMask` decode: 2049, layers 0 and 11). The tables below describe the tree before those fixes. Details are in [combat.md](combat.md), [buggy.md](buggy.md) and [ui.md](ui.md). The fixes pass the editor-free compile check and were play-tested in the editor on 2026-11-18 (see [combat.md](combat.md)).

Second follow-up (2026-11-19, Unity `6000.3.27f1`, macOS `27.2.1`): C1, C2, C3, C4, C6, C8, S3, G7, G8 (crosshair while chatting only), G14, B4 and P12 were fixed or recorded; those rows are marked "Fixed 2026-11-19" below. Details are in [buggy.md](buggy.md#camera) and [ui.md](ui.md). The deliberate departures (B4, and the centred crosshair from G8) are now listed in `IMPROVEMENTS.md`. Everything else in the tables is still open, including the larger unported features: name tags (V4), the materialisation effect (V7, G5), RORB flashes (R1, R2), XORBs (M6, R3), music and hypersound (S2), lava floating and diving (B2), the "new physics" motor (B3), the other menu panels (S9, G10), roam camera (C7), worlds generated from `.utw` (W1) and the other vehicles.

## Path legend

Line references use the file name only. Names are unique within each side.

| Short form | Full path |
| --- | --- |
| `Vehicle.js`, `VehicleMe.js`, `VehicleLocal.js`, `VehicleBot.js`, `VehicleData.js`, `VehicleTrigger.js`, `RamoSphere.js`, `VehicleNet.js` | `unity/Assets/Martian Assets/Vehicles/Scripts/` |
| `Buggy.js`, `BuggyWheel.js` | `unity/Assets/Martian Assets/Vehicles/Buggy/Components/Scripts/` |
| `Skidmarks.js` | `unity/Assets/Martian Assets/Vehicles/Buggy/Components/Tracks/` |
| `Rocket.js` | `unity/Assets/Martian Assets/Vehicles/Lasers/Components/` |
| `Game.js`, `Settings.js`, `CameraVehicle.js`, `Messaging.js`, `MiniMap.js`, `GameData.js` | `unity/Assets/Martian Assets/Scripts/Game/` |
| `LobbyStarfield.js`, `Lobby.js`, `Init.js` | `unity/Assets/Martian Assets/Scripts/Lobby/` |
| `WhirldIn.js`, `World.js` | `unity/Assets/Whirld/Core/` |
| `WhirldLOD.js`, `WhirldObject.js` | `unity/Assets/Whirld/Tools/` |
| `BaseSimple.js`, `FloorController.js`, `JumpPoint.js`, `SeaData.js` | `unity/Assets/Whirld/ResourceSources/<name>/` |
| every `*.cs` named below, unless listed otherwise | `Unity6/Assets/Modernization/Scripts/Replacements/` |
| `SinglePlayerStartupBootstrap.cs`, `InitModern.cs`, `GameDataModern.cs` | `Unity6/Assets/Modernization/Scripts/` |
| `FortBoxy.utw` | `reference/whirlds/FortBoxy.utw` |

## Summary

The legacy `.js` files also exist under `Unity6/Assets` at their original paths (the Unity 5 port's copies). Unity 6 does not compile UnityScript, so they are inert; the live counterparts are the C# files under `Unity6/Assets/Modernization/Scripts`. All comparisons below are against those C# files.

The physics and combat ports (`BuggyControllerModern`, `RocketModern`, `VehicleBotModern`, `SkidmarksModern`, `MiniMap`, `BaseSimpleModern`) follow their sources closely. The differences that change what a player experiences are mostly in the code around them:

1. **Fort Boxy's own server settings are not applied.** `FortBoxy.utw` begins `[marsxplr:camo:1;lsrh:1;][message:Welcome to Fort Boxy!]`. The original applies them when the host loads the world (`Game.js:225-229`, `Game.js:1225`, `Game.js:1228`): "L Hits Rematerialize" and "Camouflage Badges" are on, and the welcome message is shown. The port reads the data (`WhirldObjectModern.cs:28-37`) but nothing uses it, so `lasersFatal` stays off (`Settings.cs:22`). On the reference world the original resets a lasered vehicle to the base; the port does not.
2. **Ramming by contact is missing.** In the original any vehicle whose body touches the quarry's rigidbody takes the quarry, with or without RORBs (`VehicleLocal.js:48-51`, `VehicleLocal.js:53-62`, `VehicleTrigger.js:7-10`, all into `Vehicle.js:258-263`). The port only rams through the RORB sphere (`VehicleRamoSphereModern.cs:105-116`), which is off by default.
3. **There is no message console on screen.** `Messaging.cs` has no `OnGUI`; messages go to a list and `Debug.Log` (`Messaging.cs:34-40`). The original draws the "Messaging Console" window and chat input (`Messaging.js:17-122`). The port instead binds the bare R and X keys to reset and XORB (`Messaging.cs:24-31`), which the original only does when typed into chat (`Messaging.js:66-77`).
4. **Bots are coloured differently.** Original bots copy the player's colours (`Vehicle.js:98-103`). In the port every vehicle, bots included, reads the saved colour and re-randomises it when the colour is not custom, saving the result over the player's preferences (`VehicleColourModern.cs:25-33`, `Settings.cs:63-73`, `Settings.cs:86-95`). Also, a new vehicle makes every vehicle recolour in the original (`Vehicle.js:117`), so the quarry turns green when the first bot joins; the port does not do that.
5. **Start of game.** The original names the player's vehicle after the user (`Game.js:206-209`) and starts the host as quarry silently (`Game.js:209`, `Game.js:1173`). The port names it "Player" (`SinglePlayerPlayableBootstrap.cs:66`) and announces "Player Defaulted to Quarry" (`GameModeSinglePlayerModern.cs:31-40`).
6. **Falling off and speed guards.** See `VehicleMe.js` and `VehicleLocal.js` below: different floor height, different reset, no message, no terrain-penetration guard, no 500 m/s cap.

`PLAN.md` has stale passages rather than overstated ones: its "Current status" section contradicts later sections and the code in five places, and Milestone 4 still lists ramming and fatal hits as open although both are in the tree. Details are in "PLAN.md claims" below.

## Script map

| `unity/` script | Counterpart in `Unity6/Assets` | State |
| --- | --- | --- |
| `Vehicle.js` | `VehicleStateModern`, `VehicleColourModern`, `VehicleMarkersModern`, `LaserLockModern`, `VehicleLaserResetModern`, `VehicleRamoSphereModern`, `GameModeSinglePlayerModern`, `Game.cs` (HUD readout) | Split; differences below |
| `VehicleMe.js` | `PlayerVehicleInputModern`, `CombatSystemModern`, `LegacyLaserSightController`, `FallReset`, `Settings.cs` (vehicle light) | Split; differences below |
| `VehicleLocal.js` | none | Not ported (terrain guard, contact ramming) |
| `VehicleBot.js` | `VehicleBotModern` | Faithful |
| `VehicleData.js` | values set in `SinglePlayerPlayableBootstrap`, `BuggyControllerModern` | Replaced by constants |
| `VehicleTrigger.js` | none | Not ported |
| `RamoSphere.js` | `VehicleRamoSphereModern` | Partly ported |
| `VehicleNet.js` | none | Networking, out of scope |
| `ColliderAtach.js` | none | Debug script |
| `Buggy.js` | `BuggyControllerModern`, `BuggyWingsModern`, `BuggyPresentationModern`, `BuggyColliderShape` | Faithful for the default motor; gaps below |
| `BuggyWheel.js` | none | Entirely commented out in the original |
| `Skidmarks.js` | `SkidmarksModern` | Faithful |
| `Rocket.js` | `RocketModern`, `PlasmaSparksModern` | Faithful for the local branch |
| `EntryPoint.js` | none | Not ported (materialisation effect) |
| `ThrustCone.js`, `Hovercraft.js`, `HoverThrust*.js`, `Jet.js`, `JetThruster.js`, `Tank.js`, `TankMe.js`, `TankTrack.js`, `TankTrackSimple.js` | none | Other vehicles, not started |
| `Game.js` | `Game.cs`, `SinglePlayerPlayableBootstrap`, `World.cs` (`FinishWorld`), `GameModeSinglePlayerModern` | Partly ported |
| `Settings.js` | `Settings.cs` | Partly ported |
| `CameraVehicle.js` | `CameraVehicle.cs` | Mostly faithful; differences below |
| `Messaging.js` | `Messaging.cs` | Storage only, no GUI |
| `MiniMap.js` | `MiniMap.cs` | Faithful |
| `GameData.js` | `GameDataModern.cs` | Faithful |
| `SoundEffect.js` | `PlasmaSparksModern` (lifetime only) | `useSfx` not ported |
| `PrefabHere.js`, `CustomCursor.js` | none | Not ported; cursor drawing is in `Game.cs` |
| `Init.js` | `InitModern`, `SinglePlayerStartupBootstrap` | Replaced by the offline flow |
| `Lobby.js`, `LobbyDecor.js` | none | Out of the active flow |
| `LobbyStarfield.js` | `LobbyStarfield.cs` | Ported; differences below |
| `WhirldIn.js` | `World.cs` (`TextureObject`, pyramid) | Loader replaced by a captured scene |
| `World.js` | `World.cs` statics | Faithful |
| `WhirldObject.js`, `WhirldData.js`, `WhirldLibrary.js` | `WhirldObjectModern`, `WhirldLibraryModern` | Faithful |
| `WhirldLOD.js` | `BaseMarkerModern` (quality-level branch only) | Distance branch not ported |
| `BaseSimple.js` | `BaseSimpleModern` | Faithful |
| `FloorController.js` | `FloorControllerModern` | Faithful |
| `JumpPoint.js`, `SeaData.js`, `TerrainController.js`, `WaterLightmapFog.js` | none | Not ported (not used by Fort Boxy) |
| `MeshSerializer.js`, `MonoBehaviourScript.js`, Whirld `Editor/*.js`, `RaiseHeightmap.js`, `UpdateTreeColors.js` | none | Editor and export tooling |
| Standard Assets `.js` (`MouseOrbit`, `SmoothFollow`, `SmoothLookAt`, `DragRigidbody`, `FPSWalker`, `TimedObjectDestructor`) | none | Unused samples |
| Image effects, `Water.cs`, `WaterSimple.cs`, `SeaShader.cs`, `LavaSimple.cs`, `CombineChildren.cs`, `MeshCombineUtility.cs`, `Triangulator.cs`, `MouseLook.cs`, `ActivateTrigger.cs`, `TerrainControllerData.cs`, the Unity Island effects | same path under `Unity6/Assets` | API updates only (see below) |
| `LightmapExport.cs`, `ObjExporter.cs` (Unity Island `Editor/`) | none | Absent from `Unity6/` |

## Differences by script

Severity: **High** changes the outcome of play on Fort Boxy; **Medium** is visible or changes a rule in some situations; **Low** is cosmetic, timing or only reachable through unported features.

### `Vehicle.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| V1 | High | `Vehicle.js:258-263` reached from `VehicleLocal.js:48-62` and `VehicleTrigger.js:7-10` | `VehicleRamoSphereModern.cs:105-126` | `OnRam` has no caller for body contact. Without RORBs the quarry cannot be rammed. |
| V2 | Medium | `Vehicle.js:98-103` | `VehicleColourModern.cs:25-33` | Bots take the player's colours in the original. The port randomises each bot when the colour is not custom, and writes that colour to the player's saved preferences (`Settings.cs:86-95`). |
| V3 | Medium | `Vehicle.js:117` | `SinglePlayerPlayableBootstrap.cs:96-103`; `GameModeSinglePlayerModern.cs:45-52` | Every vehicle recolours when a vehicle joins, so an existing quarry turns green as soon as there are two vehicles. The port recolours only when the quarry changes. |
| V4 | Medium | `Vehicle.js:181-206` | none | Name tags (name, vehicle short name, score) are not drawn. Acknowledged in `docs/combat.md`. |
| V5 | Low | `Vehicle.js:187` | `Game.cs:402-437` | HUD readout: the original separates the fields with five spaces in a 150 px button; the port uses two spaces in 190 px. The original shows "Static" below 0.05 m/s; the port below 0.05 MPH after its own rounding, so 0.05 to 0.22 m/s reads "Static" instead of "0 MPH" (`Game.cs:412`, `Game.cs:420`). |
| V6 | Low | `Vehicle.js:131-132` | none | Lava bubbles emitter is not ported. |
| V7 | Low | `Vehicle.js:274-288` | `VehicleLaserResetModern.cs:31-41` | The reset does not shrink, destroy and rebuild the RORB (`Vehicle.js:281-283`, `Vehicle.js:288`), and the two materialisation effects (`Vehicle.js:279-280`) are missing. The file's header comment (`VehicleLaserResetModern.cs:7-9`) still says the sphere "has not got yet" a port. |
| V8 | Low | `Vehicle.js:211-236` | `VehicleRamoSphereModern.cs:33-49` | The original waits 1 s before building the sphere and restores the rigidbody's inertia tensor and centre of mass afterwards; the port builds at once and does neither. If the setting is switched off and on again within 2 s, the port keeps the sphere that is already scheduled for destruction (`VehicleRamoSphereModern.cs:41`, `:47`) and ends with none. |
| V9 | Low | `Vehicle.js:143` | `GameModeSinglePlayerModern.cs:91-99` | `quarryDist` is only written by a non-player quarry in the original; the port recomputes it every frame. No visible effect, because the readout hides it while the player is quarry. |
| V10 | Low | `Vehicle.js:75-77` | `SinglePlayerPlayableBootstrap.cs:74-78` | Matches (player marker). Listed for completeness. |

### `VehicleMe.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| M1 | Medium | `VehicleMe.js:70-76` | `FallReset.cs:6`, `FallReset.cs:16-30` | The original resets below −300 m to the base position, keeps the direction of travel at 1 m/s, keeps the rotation and broadcasts "… fell off the planet". The port resets below −200 m to 4 m above the base, zeroes linear and angular velocity, resets the rotation and shows no message. Bots use the original rule (`VehicleBotModern.cs:152-159`). |
| M2 | Medium | `VehicleMe.js:77` | none | Speed above 500 m/s is halved each frame in the original. |
| M3 | Low | `VehicleMe.js:6-8` | `PlayerVehicleInputModern.cs:17-22` | The 0.1 s key guard (`kpTime`) is not applied to Jump. |
| M4 | Low | `VehicleMe.js:26-42` | `LegacyLaserSightController.cs:47-63` | The original shows an aimer whenever weapons lock is on. The port also requires lasers enabled and firepower above 0. |
| M5 | Low | `VehicleMe.js:94-98` | none | Wing trails are switched on only above render level 3 in the original; the port never gates them. |
| M6 | Low | `VehicleMe.js:80-87` | none | XORB driving forces. Acknowledged as not ported. |
| — | — | `VehicleMe.js:45-67` | `CombatSystemModern.cs:40-70` | Fire conditions, snipe rule, muzzle position and cooldowns match. |
| — | — | `VehicleMe.js:26-38` | `CombatSystemModern.cs:76-87` | Lock ray matches. |

### `VehicleLocal.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| L1 | High | `VehicleLocal.js:48-62` | none | Contact ramming (see V1). |
| L2 | Medium | `VehicleLocal.js:29-46` | none | Terrain-penetration guard: each physics step the original casts from the previous position to the current one against `terrainMask` and, on a hit less than √500 m away, puts the vehicle back on the surface. The port has no equivalent, so a fast buggy can pass through thin geometry that the original would stop. |
| L3 | Low | `VehicleLocal.js:54` | none | `OnCollisionStay` is rebroadcast to the vehicle model. The port's self-righting receives it directly (`BuggyControllerModern.cs:321`). |

### `VehicleBot.js`

Matches `VehicleBotModern.cs` branch for branch, including the `int` enemy timer (`VehicleBot.js:6`, `VehicleBotModern.cs:18`, `:39`) and firing regardless of `lasersAllowed` (`VehicleBot.js:124`, `VehicleBotModern.cs:144-149`). One difference: the fall message goes through `Game.Controller.msg` (`VehicleBotModern.cs:158`) rather than `Messaging.broadcast` (`VehicleBot.js:142`); the text is the same.

### `RamoSphere.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| R1 | Low | `RamoSphere.js:48-53` | none | Shield flash (alpha 10) when another ORB first enters. |
| R2 | Low | `RamoSphere.js:55-57` | none | Shield flash (alpha 5) on a laser hit. |
| R3 | Low | `RamoSphere.js:26-37` | none | `colorSet` and the collision flash belong to XORBs; acknowledged. |
| — | — | `RamoSphere.js:14-24`, `:39-46` | `VehicleRamoSphereModern.cs:54-60`, `:105-116` | Scroll, alpha easing and the stay trigger match. |

### `Buggy.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| B1 | Medium | `Vehicle.js:3` (`terrainMask : LayerMask = ~1<<4`) | `BuggyControllerModern.cs:19-20` | The port's comment reads the script default as "everything except the water layer". Unary `~` binds tighter than `<<`, so the script default is `(~1) << 4`: layers 5 to 31, excluding layers 0 to 4. The value actually used is the one serialised in the binary `Vehicle.prefab`, which was not decoded here. The port's `~(1 << 4)` is therefore an assumption, not the source default. It affects wheel rays, the gear-retract ray and the body-friction ray. |
| B2 | Medium | `Buggy.js:435-475` | none | Lava floating and diving. Listed as open in `PLAN.md`. |
| B3 | Low | `Buggy.js:181`, `:187`, `:310-353` | none | "New physics" motor. Listed as open in `PLAN.md`. `buggyNewPhysics` and `buggyAWD` do not exist in `Settings.cs`. |
| B4 | Low, recorded in `IMPROVEMENTS.md` 2026-11-19 | `Buggy.js:201`, `:400` | `BuggyControllerModern.cs:333-340` | Wheel rays ignore triggers in the port, described there as a deliberate change. In the original a trigger on a masked layer (another vehicle's lock or RORB sphere) counts as ground. Not recorded in `IMPROVEMENTS.md`. |
| B5 | Low | `Buggy.js:81-88` | `SinglePlayerPlayableBootstrap.cs:175`, `:344-355` | The original deactivates the wing trails on vehicles that are not the player's. The port clones them, enabled, on to bots as well. |
| B6 | Low | `Buggy.js:83` | `BuggyPresentationModern.cs:62-66` | The tyre-track object is put on layer 11 in the original; the port leaves it on the default layer. `LegacyLaserSightController.cs:117` notes that the minimap skips layer 11, so the tracks may show on the minimap. |
| B7 | Low | `Buggy.js:123-124`, `:519-525`; `WhirldLOD.js:16-20` | `BuggyPresentationModern.cs:41-47` | Distance level of detail (simple wheels, hidden axles, second wing mesh) is not ported; the detailed set is always shown. `World.lodDist` is set (`Settings.cs:160-163`) but nothing reads it. |
| — | — | `Buggy.js:178-208`, `:282-307`, `:392-433`, `:477-495` | `BuggyControllerModern.cs:120-246`, `:321-331` | Suspension, centre of gravity, kinematic branch, drag and brakes, steering, default motor, wheel friction, springs, collision friction and self-righting match, including the `int` truncation of `motorAccel` (`Buggy.js:53`, `BuggyControllerModern.cs:190`). |
| — | — | `Buggy.js:210-280` | `BuggyControllerModern.cs:248-318` | Flight matches, including the `input.y + 0.5 * 100` stall torque (`Buggy.js:278`, `BuggyControllerModern.cs:262`) and the constant 400 N thermal below `lavaAlt + 10` (`Buggy.js:265-266`, `BuggyControllerModern.cs:308-315`). |
| — | — | `Buggy.js:103-166`, `:501-513` | `BuggyControllerModern.cs:83-108`, `BuggyWingsModern.cs:42-90` | Wing state machine, unfold, flex and trail offsets match. |

### `Skidmarks.js`

`SkidmarksModern.cs` matches (`Skidmarks.js:53-93` against `SkidmarksModern.cs:46-77`; `Skidmarks.js:96-154` against `:84-138`; `Skidmarks.js:163-173` against `:145-154`). `markWidth` 0.225 is the script default on both sides; any prefab override was not decoded.

### `Rocket.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| K1 | Low | `Rocket.js:15-18` | `RocketModern.cs:33-37` | Inherited speed is fixed at zero. Correct offline, because only `VehicleNet.js` writes `Vehicle.velocity`. |
| K2 | Low | `Rocket.js:74-75` | `RocketModern.cs:85-89` | Hits are sorted nearest first; the original used engine order. Documented. |
| K3 | Low | `Rocket.js:32-43`, `:57`, `:82`, `:89` | none | Remote-vehicle prediction, lag extrapolation and the `lH` RPC are networking only. |
| — | — | `Rocket.js:22-53`, `:69-112`, `:114-128` | `RocketModern.cs:43-64`, `:74-118`, `:120-143` | Lock-on lead, gravity, ricochet, scoring calls, explosion force and sparks match. Scoring rules match (`Rocket.js:91-98`, `GameModeSinglePlayerModern.cs:68-88`). |

### `Game.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| G1 | High | `Game.js:225-229`; `FortBoxy.utw` header | `World.cs:55-92`; `WhirldObjectModern.cs:28-37` | World parameters `message` and `marsxplr` are not applied. For Fort Boxy that is `lasersFatal` on (`Game.js:1225`), `hideNames` on (`Game.js:1228`) and the welcome message. `docs/world-and-rendering.md` records the data as "not yet applied"; `PLAN.md` does not list it. |
| G2 | Medium | `Game.js:206-209` | `SinglePlayerPlayableBootstrap.cs:66`; `SinglePlayerStartupBootstrap.cs:74-84` | The vehicle is named "Player". `GameDataModern.userName` is set but never used, so every message reads "Player …" and bots are "Player's bot". The original uses the user's name in the bot's name (`Game.js:1009`). |
| G3 | Medium | `Game.js:209`, `:1173`; `Game.js:1049-1055` | `GameModeSinglePlayerModern.cs:24-40` | The host is created as quarry with no message, and `eSI` runs only after a bot is axed or a player leaves (`Game.js:694`, `:1033`). The port runs the check every frame and prints "Defaulted to Quarry" at spawn. |
| G4 | Medium | `Game.js:1165` | `SinglePlayerPlayableBootstrap.cs:108-111`, `:139` | The original spawns the local vehicle with the base's rotation. The port uses identity, and its comment says the original did. |
| G5 | Low | `Game.js:198`, `:1008`, `:1027`, `:1062-1064` | none | Materialisation effect at spawn, bot add and bot removal. Acknowledged. |
| G6 | Low | `Game.js:1022-1034` | `Game.cs:219-229` | The original waits 0.5 s before making someone quarry after a bot is axed; the port does it on the next frame. |
| G7 | Low, fixed 2026-11-19 | `Game.js:325-329`, `:532` | `Game.cs:314` | Frame rate shown is a 2 s average in the original and `1 / smoothDeltaTime` in the port. |
| G8 | Low, chatting gate fixed 2026-11-19; centring kept as a recorded departure | `Game.js:379-382` | `Game.cs:369-400` | Crosshair: the original draws it whenever the cursor is locked, at centre minus `cursorOffset`, choosing the texture by the player's vehicle type. The port hides it while chatting, centres it, and always reads `firepower[0]` (`Game.cs:373`). The centring is documented in `docs/combat.md`. |
| G9 | Low | `Game.js:265-273` | `Game.cs:162-180` | The delayed reset does not destroy and rebuild the RORB, and additionally zeroes angular velocity. |
| G10 | Low | `Game.js:395-430`, `:438-450`, `:454-480`, `:585-621`, `:288-307`, `:320-323` | `Game.cs:258-260` | Exit button, fullscreen notice, hint lines, Switch Vehicle, Settings Advisor, GUI fade and the `0` fullscreen key are not ported. The first six are acknowledged in `docs/ui.md`; the `0` key is not mentioned. |
| G11 | Low | `Game.js:162-164` | none | `CombineChildren` on worlds with more than 15 objects. |
| G12 | Low | `Game.js:190` | `World.cs:157-161` | The `nofloor` and `floor` world parameters are not checked before adding the floor. |
| G13 | Low | `Game.js:247` | none | `Application.runInBackground` is not set by script. |
| G14 | Low, fixed 2026-11-19 | none | `Game.cs:195`, `:231-247` | `RaycastIgnoringPlayer` and its buffer are left over from the removed hitscan laser; nothing calls them. |
| G15 | Low | none | `World.cs:174-177`, `:364-399` | `FallbackGround` is still created when nothing lies below the base. Non-original scaffolding, inactive on Fort Boxy. |
| — | — | `Game.js:484-563`, `:566-583` | `Game.cs:275-349`, `:353-367` | Side-panel layout and panel window match. |
| — | — | `Game.js:1005-1011`, `:1138-1148` | `Game.cs:201-216`; `SinglePlayerPlayableBootstrap.cs:110-112` | Bot announcement, 2 s delay and the clear-sphere spawn search match. |

### `Settings.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| S1 | Medium | `Settings.js:1596`, `:735` | `Settings.cs:26`, `:132-201`; `Messaging.cs:92-111` | With RORBs off the original forces `zorbSpeed` to 0, so XORBs are unavailable. The port leaves it at 7, so the X key reports "XORB Activated" and flips a flag (`zorbEnabled`) that nothing reads. |
| S2 | Medium | `Settings.js:1598-1607`; `CameraVehicle.js:37-39` | `Settings.cs:13-14` | Music selection and the hypersound pitch are not ported; the two fields are declared and unused. |
| S3 | Low, fixed 2026-11-19 | `Settings.js:794` | `Settings.cs:249-269` | The "L Hit ORBs" toggle is missing, so `lasersOptHit` (honoured at `RocketModern.cs:86`) cannot be switched on. |
| S4 | Low | `Settings.js:1832-1834` | none | Particle emitters are switched off below render level 3. |
| S5 | Low | `Settings.js:236` | none | The original caps the frame rate at 100 from preferences. |
| S6 | Low | `Settings.js:239-243` | `Settings.cs:121-130` | `showHints`, `useMusic`, `useSfx`, `useHypersound` and `useMinimap` are not loaded from preferences. |
| S7 | Low | `Settings.js:1626-1687` | none | Terrain detail settings by render level. No terrain on Fort Boxy. |
| S8 | Low | `Settings.js:76` | `Settings.cs:55` | `resetTime` starts at 0 in the original and −10 in the port; this only matters to the unported Switch Vehicle panel (`Game.js:456`). |
| S9 | Low | `Settings.js:257-675` | `Game.cs:361-363` | Game Settings, Player Settings and Active Players panels show "{Not ported yet}". Acknowledged. |
| — | — | `Settings.js:1551-1583`, `:1617-1624`, `:1689-1690`, `:1836-1839` | `Settings.cs:132-200` | Lock flag, LOD distance, contrast and SSAO, quality level, fixed step, far clip and fog, gravity, sea height and vehicle lights match. |
| — | — | `Settings.js:1701-1735` | `Settings.cs:63-95` | Colour randomising formula matches; who it is applied to does not (V2). |

### `CameraVehicle.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| C1 | Medium, fixed 2026-11-19 | `CameraVehicle.js:123-124` | `CameraVehicle.cs:105-106` | Mouse look is a quarter as sensitive while Snipe is held (0.5 against 2). The port always uses 2, so aiming while zoomed is four times faster. |
| C2 | Medium, fixed 2026-11-19 | `CameraVehicle.js:75-82` | `CameraVehicle.cs:229-244` | Keys 3 and 4 change only the chase distance. The port also forces chase mode. |
| C3 | Medium, fixed 2026-11-19 | none | `CameraVehicle.cs:270-275`, `:319-327` | The port shows a toast box and adds a message for every camera key. The original shows nothing. |
| C4 | Low, fixed 2026-11-19 | `CameraVehicle.js:62-65` | none | Escape leaves first person in the original. |
| C5 | Low | `CameraVehicle.js:66` | `CameraVehicle.cs:210-268` | Camera keys are ignored while chatting in the original; no check in the port (not reachable until chat exists). |
| C6 | Low, fixed 2026-11-19 | `CameraVehicle.js:112-118` | `CameraVehicle.cs:79-86` | The look angles reset only on Fire2 down or key 1. The port resets them every time first person becomes active, including on Snipe and after the entry effect. |
| C7 | Low | `CameraVehicle.js:229-239` | `CameraVehicle.cs:122-189` | Roam mode (4) is not ported; any mode other than 0, 2 and 3 is treated as chase. |
| C8 | Low, fixed 2026-11-19 | `CameraVehicle.js:24` | `CameraVehicle.cs:22-28` | The start orientation (x = 270.01°) is not set, so the entry effect starts from the scene's camera rotation. |
| — | — | `CameraVehicle.js:42-59`, `:100-106`, `:138-204`, `:216-226`, `:242-263` | `CameraVehicle.cs:40-55`, `:70-76`, `:122-189`, `:111-121`, `:191-205`, `:294-317` | Cursor lock, snipe zoom, arrow, entry effect, the three chase styles, soar, spectate and fog switching match. |

### `Messaging.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| X1 | High | `Messaging.js:17-122` | `Messaging.cs:34-40` | No console window, chat input, Tab handling or `chatting` state. Game messages are not visible to the player. |
| X2 | Medium | `Messaging.js:66-77` | `Messaging.cs:19-32` | Pressing R starts the 10 s reset and freezes the vehicle; pressing X toggles XORB. In the original both are chat commands. |
| X3 | Low | `Messaging.js:79` | `Messaging.cs:64-68` | Language filter not applied. |
| — | — | `Game.js:1346-1353` | `Messaging.cs:34-38` | The 50-entry cap matches. |

### `MiniMap.js`, `BaseSimple.js`, `FloorController.js`, `WhirldObject.js`, `GameData.js`

No behavioural differences found (`MiniMap.js:11-61` against `MiniMap.cs:20-77`; `BaseSimple.js:4-27` against `BaseSimpleModern.cs:12-54`; `FloorController.js:4-10` against `FloorControllerModern.cs:10-16`; `WhirldObject.js:18-26` against `WhirldObjectModern.cs:28-37`).

### `WhirldIn.js`, `WhirldLOD.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| W1 | Medium | `WhirldIn.js:35-203`, `:1027-1180` | `World.cs:24-92` | Worlds are not generated from `.utw` data; a captured scene is loaded. Only Fort Boxy is available. By design. |
| W2 | Low | `WhirldLOD.js:16-20` | `BaseMarkerModern.cs:45-53` | Only the quality-level branch is ported, for the base marker. |
| — | — | `WhirldIn.js:1181-1209` | `World.cs:262-295` | `TextureObject` matches. |

### `LobbyStarfield.js`

| # | Severity | Original | Port | Difference |
| --- | --- | --- | --- | --- |
| F1 | Low | `LobbyStarfield.js:22` | `LobbyStarfield.cs:38-42` | Emission also stops below the "Good" quality level in the original. |
| F2 | Low | `LobbyStarfield.js:15-17` | `LobbyStarfield.cs:25-30` | The port tints the shared material asset, not an instance. Only reached in the Lobby scene. |
| F3 | Low | `LobbyStarfield.js:11`, `:20` | `LobbyStarfield.cs:20-32` | Scene indices 1 and "above 1" meant Lobby and Game. The build list is now Init, InitGame, Game, Fort Boxy, so index 1 is InitGame. |

### C# scripts present on both sides

Compared with `diff` after normalising line endings. `VortexEffect`, `TwirlEffect`, `ImageEffectBase`, `GrayscaleEffect`, `SepiaToneEffect`, `EdgeDetectEffect`, `ColorCorrectionEffect`, `NoiseEffect`, `MotionBlur`, `ContrastStretchEffect`, `Triangulator` and `TerrainControllerData` are identical. `SSAOEffect`, `Water`, `WaterSimple`, `WaterWaveScroll`, `LavaSimple`, `MouseLook`, `ActivateTrigger` and both `CombineChildren` copies differ only by API updates (`camera` to `GetComponent<Camera>()`, `GL.SetRevertBackfacing` to `GL.invertCulling`, removed `isPowerOfTwo`). `BlurEffect`, `GlowEffect` and the two Unity Island effects load their shaders from `Resources` instead of compiling them from strings. `ImageEffects`, both `MeshCombineUtility` copies and `SeaShader` have larger edits that were not read line by line.

## `PLAN.md` claims the code or the rest of the plan does not support

Resolved 2026-11-19: `PLAN.md` was rewritten where these rows point (P1 to P5 in "Current status", P6 to P8 by the 2026-11-17 and 2026-11-19 status lines, P9 to P11 in Milestones 2 and 3). The line numbers below refer to the plan before that change.

| # | `PLAN.md` | Claim | What the tree shows |
| --- | --- | --- | --- |
| P1 | line 37 | "There is no verified arm64 player build yet." | Contradicted by line 91 (arm64 development player created and launched, ticked) and line 162 (Milestone 6 status). |
| P2 | line 37 | "The current working tree has not been compiled or played in Unity since its newest changes. Cached assemblies predate the latest scene-flow, validation, and vehicle-state work." | Contradicted by lines 83, 89-90 and by every dated editor verification in `docs/`. Stale. |
| P3 | line 30 | "`SimplePlayerController` provides temporary drive and flight behaviour" | `useModernBuggyController` defaults to true (`SinglePlayerPlayableBootstrap.cs:17`), so `BuggyControllerModern` drives; `SimplePlayerController` is only the fallback (`:157-162`). Line 99 says the same. |
| P4 | line 33 | "…bot name tags and impact visuals are not [ported]" | Impact visuals are ported: `PlasmaSparksModern.cs`, instantiated at `RocketModern.cs:136-142`. Line 148 says so too. |
| P5 | line 29 | "replacement C# components cover the active camera, HUD, messaging, minimap, world, settings" | Messaging has no on-screen component (X1). Settings covers one of four panels (S9). |
| P6 | line 148 | "Open: ramming, fatal hits, name tags, the materialisation effect." | Fatal hits are in `VehicleLaserResetModern.cs` and RORB ramming in `VehicleRamoSphereModern.cs` (commit `f696e52`); `docs/combat.md` records both. What remains open is contact ramming (V1), which the plan does not name. |
| P7 | line 168 | Next action: "…then ramming and fatal hits" | Same as P6. |
| P8 | line 148 | "with the placeholders removed" | The debug laser, target cylinder and impact stubs are gone. Leftovers remain: `Game.RaycastIgnoringPlayer` (G14), the R and X hotkeys (X2), the camera toast (C3), the "Single-player buggy spawned." message (`SinglePlayerPlayableBootstrap.cs:89`) and `FallReset` (M1). |
| P9 | line 116 | "Keep `specialInput`, `flightMode`, `camSmooth`, brakes, and throttle-channel selection as distinct state concepts." | `VehicleStateModern.SetFlightMode` sets all three together (`VehicleStateModern.cs:42-47`); it is called at spawn (`SinglePlayerPlayableBootstrap.cs:193`) and by `SimplePlayerController`. The modern controller keeps them separate. |
| P10 | lines 121-124 against 126 | Milestone 2 status "ported and verified on the probe pad" | All four acceptance boxes are unticked. One of the two is out of date. |
| P11 | line 136 | Milestone 3 lists only "laser aimer colour" as open | The world's own settings and welcome message (G1) are open and affect play on Fort Boxy; the fact is recorded only in `docs/world-and-rendering.md`. |
| P12 (recorded 2026-11-19) | line 50 | "Preserve original behaviour and presentation by default." | Deliberate departures that are not in `IMPROVEMENTS.md`: wheel rays ignoring triggers (B4), the camera toast (C3), keys 3 and 4 forcing chase mode (C2), and the R and X hotkeys (X2). |

Claims checked and supported: one scene-flow coordinator (line 66; `InitModern.cs:13` and `InitSceneLoaderModern.cs:17` both delegate to `SinglePlayerStartupBootstrap.cs:35-72`); Lobby out of Build Settings (line 68; `Unity6/ProjectSettings/EditorBuildSettings.asset`); flight state separate from `inputThrottle` (line 70; `VehicleStateModern.cs:9`, `:14`); body and beadlocks coloured from the vehicle state (line 105; `VehicleColourModern.cs:73-81`); recovery controller selectable (line 108); pyramids generated at ±0.5 (line 81; `World.cs:229-258`); lava floating, diving and the "new physics" motor open (line 99; `BuggyControllerModern.cs:9`); vehicle light off, then on for the player above render level 4 (line 136; `Settings.cs:175-187`).

## `docs/` statements the source does not support

`PLAN.md` defers detail to these files, so they are listed here.

| # | Document | Statement | Source |
| --- | --- | --- | --- |
| D1 | `docs/combat.md`, "RORBs and ramming" | "In the original, ramming exists only with RORBs" | `VehicleLocal.js:48-62` and `VehicleTrigger.js:7-10` call `OnRam` on body contact with no RORB check. |
| D2 | `docs/combat.md`, "Laser-hit rematerialise and quarry colour" | "As in the original, adding a bot does not recolour an existing quarry until the quarry next changes." | `Vehicle.js:117` recolours every vehicle when one starts. |
| D3 | `docs/combat.md`, "Bots" | A bot gets "the player's colours" | True in `Vehicle.js:101-102`; the port randomises unless the colour is custom (`VehicleColourModern.cs:28-33`). |
| D4 | `docs/combat.md`, "First slice" | "someone is always quarry, so the player alone 'Defaulted to Quarry'" | The host is created as quarry with no message (`Game.js:209`). |
| D5 | `docs/combat.md`, "Laser-hit rematerialise" | `lasersFatal` "off by default as in `Settings.js`" | True for the script default, but Fort Boxy switches it on (`FortBoxy.utw` header; `Game.js:229`, `:1225`). |
| D6 | `docs/ui.md`, "Left out" | "RORBs and XORBs" are left out of Server Settings | RORBs Enabled and its Size slider are present (`Settings.cs:225-241`). |
| D7 | `docs/combat.md`, 2026-11-02 session | Bots tagged the player repeatedly in the official build on Fort Boxy | With `lsrh:1` active the player would also have been returned to the base after each hit more than 10 m from it. The notes do not mention this, so either it was not observed or the setting did not apply; worth checking on the next session. |

## Not covered

- Values serialised in binary prefabs and scenes under `unity/` (`terrainMask`, `firepower`, `laserLock`, `markWidth`, rigidbody mass and drag from `VehicleData`, the `Vehicle Body` physic material).
- Shaders, materials, lighting and audio assets.
- Editor tooling under `Unity6/Assets/Modernization/Editor`.
- `SimplePlayerController`, which is declared scaffolding and has no original to compare against.
- The inert `.js` copies under `Unity6/Assets`, which differ from `unity/` by the Unity 5 port's API edits.
- `ImageEffects.cs`, `MeshCombineUtility.cs` and `SeaShader.cs` beyond a line count of their differences.
