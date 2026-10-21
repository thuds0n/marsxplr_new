# Future Improvements

Behaviour that matches the original Mars Explorer but could be improved later. Each item is faithful today; changing it would be a deliberate departure from the original, so it waits until parity work is complete.

## Vehicles

- **Brakes hover the buggy in mid-air.** Holding the brake (Command on macOS; middle mouse button in the original) with the wings closed sets very high drag below 10 m/s (`Buggy.js`), which also stops the buggy falling, so a slow buggy hangs in the air. It also stops a slow buggy almost instantly on the ground. Confirmed in the original under Wine.
- **Reversing does not track straight.** The buggy drifts off line when reversing. Confirmed in the original under Wine.
- **The buggy creeps forwards with no input.** With no throttle the buggy drifts forwards at a few MPH on level ground, so at Fort Boxy it rolls off the spawn slab within about 30 s. Confirmed in the original under Wine, and reproduced by the port.
- **Headlights do not light the world.** `Vehicle.prefab` carries a `VehicleLight` spotlight (blue-white 0.75, 0.85, 1; intensity 1; range 100 m; 60° cone; 1 m up and 1 m back, tilted 15° down; lights layer 0 only), but `Settings.js` `updatePrefs()` disables every light with that name, so in the 2.22 source (`unity/` and `Unity5/`) it is on only for about the first second after spawning. The newer Mars Explorer 4.0.2 under Wine shows it on. The port leaves it out; turning it on would follow 4.0.2 rather than the 2.22 source.
- **Shadow softness and filtering.** The original (Unity 2.5) drew its soft directional shadows with a heavy screen-space blur, so shadow edges are broad and fuzzy in the Wine version. Unity 6 has no equivalent softness setting and draws sharper edges with different cascade splits and bias, even with the same quality settings (soft shadows, 4 cascades, 250 m at Beautiful and 400 m at Fantastic, strength 0.9). Approximating the original look (lower shadow resolution, tuned strength and bias, or a blurred screen-space shadow pass) would be tuning by eye against the Wine version rather than a port.
