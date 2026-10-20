# Measuring the original (Wine version)

The original used for side-by-side checks is Mars Explorer 4.0.2 under the `/Applications/Mars Explorer.app` Wine wrapper. It is newer than the 2.22 source in `unity/`, so values taken only from it are recorded as inferences (see Decisions in [PLAN.md](../PLAN.md)).

- Host a local Fort Boxy game. It runs at about 11–21 FPS.
- For look comparisons, lock Rendering Quality to Fantastic, the visibility cap to maximum, and the Agile chase camera at the maximum chase distance (20). Use the same chase distance in the port.
- Capture the window with `screencapture -l <window id>` on the `wine-preloader` window. Ordinary full-screen captures may leave it out.
- Measure mean brightness of floor-only patches on the open floor south of the castle, from far chase views. First-person and low camera angles are not comparable.
- HUD figures (speed, altitude) are read off screenshots, so timings are accurate to about ±0.5 s.
- Wine does not pass Cmd through as the Windows key, so `Fire3` brakes cannot be tested there.

In the port, `Tools -> MarsXPLR -> Run Buggy Track Test`, `Run Buggy Handling Probe`, `Run Buggy Flight Probe`, `Log Scene Look` and `Log Lighting Over Time` provide the matching runs. The editor only advances frames while it is in front.
