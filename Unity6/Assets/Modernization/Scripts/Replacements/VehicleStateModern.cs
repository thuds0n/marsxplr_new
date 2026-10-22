using UnityEngine;

[DisallowMultipleComponent]
public class VehicleStateModern : MonoBehaviour
{
    public int vehicleId;
    public float camOffset = 2f;
    public bool camSmooth;
    public bool inputThrottle;
    public Vector4 input;
    public bool specialInput;
    public bool brakes;
    public bool zorbBall;
    public bool flightMode;
    public bool grounded;
    public Vector3 velocity;
    public float forwardSpeed;
    public float mph;
    public float altitude;
    public Transform rideTarget;
    // Vehicle.js vehicleColor and vehicleAccent, applied by VehicleColourModern.
    public Color vehicleColor;
    public Color vehicleAccent;
    // Vehicle.js quarry state: isIt is 1 for the quarry; lastTag guards the
    // three seconds after a tag; isResponding is set for local vehicles.
    public int isIt;
    // Vehicle.js isBot.
    public bool isBot;
    public float lastTag;
    public int score;
    public bool isResponding = true;

    public void SetInput(float horizontal, float vertical, float auxiliary = 0f, float brakeAxis = 0f)
    {
        input = new Vector4(
            Mathf.Clamp(horizontal, -1f, 1f),
            Mathf.Clamp(vertical, -1f, 1f),
            Mathf.Clamp(auxiliary, -1f, 1f),
            Mathf.Clamp(brakeAxis, -1f, 1f));
    }

    public void SetFlightMode(bool enabled)
    {
        flightMode = enabled;
        specialInput = enabled;
        camSmooth = enabled;
    }

    public void SetInputThrottle(bool enabled)
    {
        inputThrottle = enabled;
    }

    public void SyncFromRigidbody(Rigidbody body)
    {
        if (body == null)
        {
            velocity = Vector3.zero;
            forwardSpeed = 0f;
            mph = 0f;
            altitude = transform.position.y;
            return;
        }

        velocity = body.linearVelocity;
        forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
        mph = body.linearVelocity.magnitude * 2.23f;
        altitude = transform.position.y;
    }
}
