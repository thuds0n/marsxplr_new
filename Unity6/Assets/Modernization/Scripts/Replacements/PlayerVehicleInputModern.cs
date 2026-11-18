using UnityEngine;

// Local player input, ported from the input section of VehicleMe.js.
[RequireComponent(typeof(VehicleStateModern))]
public class PlayerVehicleInputModern : MonoBehaviour
{
    private VehicleStateModern _state;

    private void Awake()
    {
        _state = GetComponent<VehicleStateModern>();
    }

    private void Update()
    {
        bool chatting = Game.Messaging != null && Game.Messaging.chatting;
        // VehicleMe.js: Jump also waits out Game's short key guard.
        if (Input.GetButtonDown("Jump") && !chatting && (Game.Controller == null || Time.time > Game.Controller.kpTime))
        {
            if (Game.Controller != null) Game.Controller.kpTime = Time.time + Game.Controller.kpDur;
            // Buggy.OnSetSpecialInput: special input opens the wings and smooths the camera.
            _state.specialInput = !_state.specialInput;
            _state.camSmooth = _state.specialInput;
        }

        _state.brakes = Input.GetButton("Fire3");

        Vector4 input = new Vector4(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical"),
            Input.GetAxis("Throttle"),
            Input.GetAxis("Yaw"));
        if (_state.inputThrottle)
        {
            input.z = (input.z + 1f) * 0.5f;
        }
        else
        {
            if (input.x > -0.1f && input.x < 0.1f) input.x = input.w;
            if (input.y > -0.1f && input.y < 0.1f) input.y = input.z;
        }
        _state.input = input;
    }
}
