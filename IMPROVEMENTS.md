# Future Improvements

Behaviour that matches the original Mars Explorer but could be improved later. Each item is faithful today; changing it would be a deliberate departure from the original, so it waits until parity work is complete. The departures already made are listed at the end.

## Vehicles

- **Brakes hover the buggy in mid-air.** Holding the brake (Command on macOS; middle mouse button in the original) with the wings closed sets very high drag below 10 m/s (`Buggy.js`), which also stops the buggy falling, so a slow buggy hangs in the air. It also stops a slow buggy almost instantly on the ground. Confirmed in the original under Wine.
- **Reversing does not track straight.** The buggy drifts off line when reversing. Confirmed in the original under Wine.
- **The buggy creeps forwards with no input.** With no throttle the buggy drifts forwards at a few MPH on level ground, so at Fort Boxy it rolls off the spawn slab within about 30 s. Confirmed in the original under Wine, and reproduced by the port.
- **A resting game controller can drive the buggy.** The joystick `Throttle` axis has a dead zone of 0.0001 (original `InputManager.asset`), and `VehicleMe.js` uses it as throttle whenever `Vertical` is under 0.1, so a connected pad's resting drift adds throttle: a DualSense left untouched gave 0.06, on top of the creep above, which happens with zero input. A dead zone near the other axes' 0.19–0.2 would stop that.
- **Shadow softness and filtering.** The original (Unity 2.5) drew its soft directional shadows with a heavy screen-space blur, so shadow edges are broad and fuzzy in the Wine version. Unity 6 has no equivalent softness setting and draws sharper edges with different cascade splits and bias, even with the same quality settings (soft shadows, 4 cascades, 250 m at Beautiful and 400 m at Fantastic, strength 0.9). Approximating the original look (lower shadow resolution, tuned strength and bias, or a blurred screen-space shadow pass) would be tuning by eye against the Wine version rather than a port.

## HUD

- **The guidance arrow's direction is hard to read.** The green arrow is a plain cone seen nearly end-on in the bottom-right corner, so pointing directly ahead and directly behind look alike. It points at the base while the player is the quarry or there is none, and otherwise at the quarry (`CameraVehicle.js`). A clearer shape, shading that separates tip from base, or a small ring showing the camera's heading would make the direction obvious.

## Deliberate departures

Places where the port knowingly differs from the original, kept because the original behaviour depended on the old engine or was a defect.

- **Buggy wheel rays ignore triggers.** In `Buggy.js` the wheel, gear-retract and body-friction rays hit trigger colliders, so another vehicle's lock or RORB sphere on a masked layer counted as ground. `BuggyControllerModern` casts them with triggers ignored, so a buggy does not ride on those spheres.
- **The crosshair is centred.** `Game.js` drew the crosshair at the screen centre minus `cursorOffset` (16, 16) with the Unity 2.x label style. Unity 6's label padding shrank and shifted the texture, so the port draws it exactly at the centre, where the lasers fly.
- **Fullscreen keeps the point-size resolution.** `Settings.js` `toggleFullscreen()` switched to the display's largest resolution. The port's `0` key and the Game Settings fullscreen button only toggle fullscreen, keeping the halved resolution `Settings` picks on Retina displays so the IMGUI HUD stays at its original size.
- **World meshes are not combined.** `Game.js` added `CombineChildren` to worlds with more than 15 objects. The port gives each world renderer its own material when the world loads, so combining would merge almost nothing; it was only a draw-call optimisation and is left out.

