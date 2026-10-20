# Wing and flight

Detail for Milestone 2 in [PLAN.md](../PLAN.md). Summary: the wing state machine, flight physics and wing presentation are ported and work on the probe pad. Not yet compared with the Wine version or flown on Fort Boxy.

Status (2026-10-19): ported from `Buggy.js` into `BuggyControllerModern` (physics) and the new `BuggyWingsModern` (presentation).

- Wing state machine: `Update` and `OnSetSpecialInput`, driven by changes to `VehicleStateModern.specialInput` (toggled on Jump by `PlayerVehicleInputModern`). Wings open over half a second and keep flying until the closing animation finishes, as in the original. `wingFlaps` was only ever reset in the original, so it is not ported.
- Flight (`wingOpen` branch): speed-dependent drag, airbrakes, velocity-follows-nose flight with pitch and roll torque, sideslip ("dihedral") torque, lava thermals, and stalling below 4 m/s. The smart-suspension gear retract in flight and the stiffer wing-open springs are ported. The `Settings.js` "Stall Blending" and "Powered Loops" options are added to `Settings` (both off by default, as in the original).
- Presentation: `SinglePlayerPlayableBootstrap` copies `Buggy.prefab` `Lod0/Wings` (mesh and material, without instantiating the prefab) and clones the `Trail1` and `Trail2` trail objects. `BuggyWingsModern` animates a per-vehicle copy of the wing mesh (unfold, then flex with roll and smoothed pitch input) and moves the trails to the wingtips. `VehicleColourModern` tints the wings with the accent colour. Trail1 is assumed to be the left trail; this is unconfirmed.
- Shader fix: the wings were invisible because `GlassGlowing.shader` still declared its programs with Unity 2 comment directives (`// vertex vert`), so modern Unity compiled no vertex or fragment program. The shader now uses `#pragma vertex`/`#pragma fragment`, `appdata_base` (the wing mesh has no tangents), and `SV_Target`, and drops the fixed-function `SetTexture` line. Verified visible in the editor.
- Probe: `Tools -> MarsXPLR -> Run Buggy Flight Probe` drives a take-off, glide, roll, airbrake, stall, landing, and wing close on the probe pad and writes `Unity6/Logs/flight-probe.txt`.

Verified in the Unity `6000.3.26f1` editor on macOS `27.0.1` (probe pad, full flow from `Init`, no errors logged): the wings open (state 1 to 2) and close (-1 to 0) and draw at a 9 m span as translucent glass in the accent colour; the buggy takes off at about 65 MPH with a short pull-up, glides with the gear retracting, rolls, lands at about 45 MPH, and returns to drive mode level and without losing momentum. A long full pull-up loops the buggy, stalls it at the top (angular drag 1, nose-down pitch), and it recovers in a dive. The airbrake and stall steps in the scripted run mostly ran after touchdown, so airborne airbrake behaviour is unverified.

Not yet verified or ported: comparison with the Wine build, flight on Fort Boxy terrain, the wing-flex animation viewed close up, LOD 1 wings, lava floating and diving, and player-driven flight with real keyboard input.

Compared by eye with the Wine version (2026-10-21): flight looks and feels comparable. An instrumented comparison of take-off speed, stall and landing is backlogged, because screenshot-based measurement is costly for the detail it adds.
