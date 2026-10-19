using UnityEngine;

// Buggy ground physics ported from Buggy.js (FixedUpdate and OnCollisionStay).
// Ported: smart suspension with gear retract, centre of gravity, drag and
// brakes, steering, the default ("Modified Yoggy") motor and wheel friction,
// springs and dampers, collision friction switching, self-righting, the wing
// state machine (Update and OnSetSpecialInput), and flight (the wingOpen
// branch: drag, airbrakes, flight steering, sideslip, lava thermals, stalling).
// Not yet ported: lava floating and diving, the optional "new physics" motor.
// The wing mesh and trails are drawn by BuggyWingsModern. Wheels, axles, and skidmarks are drawn by
// BuggyPresentationModern from the telemetry below.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleStateModern))]
public class BuggyControllerModern : MonoBehaviour
{
    // Buggy.prefab: wheelPos (0.7, 0, 0.8); Buggy.js: wheelRadius 0.3.
    public Vector3 wheelPos = new Vector3(0.7f, 0f, 0.8f);
    public float wheelRadius = 0.3f;
    // Vehicle.js default: everything except the water layer.
    public LayerMask terrainMask = ~(1 << 4);

    // Read-only telemetry for presentation (wheel graphics, skidmarks, HUD).
    public float suspensionRange { get; private set; }
    public float motorSpeed { get; private set; }
    public readonly float[] hitDistance = { -1f, -1f, -1f, -1f };
    public readonly Quaternion[] wheelLocalRotation = new Quaternion[4];
    // Per-wheel ground contact from the latest FixedUpdate; skidIntensity is
    // the tyre-track opacity Buggy.js passed to Skidmarks.AddSkidMark.
    public readonly Vector3[] contactPoint = new Vector3[4];
    public readonly Vector3[] contactNormal = new Vector3[4];
    public readonly float[] skidIntensity = new float[4];
    // Wing animation: 1 to 2 while opening, -1 to 0 while closing, 0 at rest.
    public float wingState { get; private set; }
    public bool wingOpen => _wingOpen;
    public float motorInputSmoothed { get; private set; }

    private const float MaxAcceleration = 60f;
    private const float MotorMass = 1f;
    private const float MotorDrag = 1f;

    private Rigidbody _rb;
    private VehicleStateModern _state;
    private PhysicsMaterial _bodyMaterial;
    private readonly Vector3[] _wheelPositn = new Vector3[4];
    private readonly float[] _hitCompress = new float[4];
    private readonly Vector3[] _hitVelocity = new Vector3[4];
    private readonly RaycastHit[] _hits = new RaycastHit[8];
    private float _motorTorque;
    private bool _isInverted;
    private bool _wingOpen;
    private bool _specialInput;
    private float _brakePower;

    // Buggy.js flight constant.
    private const float StallSpeed = 16f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _state = GetComponent<VehicleStateModern>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.mass = 30f;

        // Vehicle Body.physicmaterial, instanced because its combine mode changes at runtime.
        _bodyMaterial = new PhysicsMaterial("Vehicle Body")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0.5f,
        };
        foreach (Collider c in GetComponents<Collider>())
        {
            if (!c.isTrigger) c.sharedMaterial = _bodyMaterial;
        }

        for (int i = 0; i < 4; i++)
        {
            _wheelPositn[i] = new Vector3(wheelPos.x * (i % 2 == 1 ? 1 : -1), wheelPos.y, wheelPos.z * (i < 2 ? 1 : -1));
            wheelLocalRotation[i] = Quaternion.identity;
        }
    }

    private void Update()
    {
        // OnSetSpecialInput, run when the special input changes.
        if (_state.specialInput != _specialInput)
        {
            _specialInput = _state.specialInput;
            _state.camSmooth = _specialInput;
            wingState = _specialInput ? 1f : -1f;
        }

        // Wings stay open (and keep flying) until the closing animation finishes.
        if (wingState != 0f)
        {
            wingState += Time.deltaTime * 2f;
            if (wingState >= 1f)
            {
                _wingOpen = true;
                if (wingState > 2f) wingState = 0f;
            }
            else if (wingState > 0f)
            {
                _wingOpen = false;
                wingState = 0f;
            }
        }
    }

    private void FixedUpdate()
    {
        Settings settings = Game.Settings;
        if (settings == null) return;

        float dt = Time.fixedDeltaTime;
        Vector4 input = _state.input;
        bool brakes = _state.brakes;
        bool wheelsAreTouchingGround = false;

        // Smart suspension
        if (settings.buggySmartSuspension)
        {
            // Auto gear retract in flight when nothing is within 5 m below.
            if (_wingOpen && !NearestHit(transform.position, Vector3.down, 5f, terrainMask, QueryTriggerInteraction.Ignore, out _))
            {
                if (suspensionRange > 0.01f) suspensionRange -= dt * 0.5f;
                else suspensionRange = 0f;
            }
            else suspensionRange = Mathf.Lerp(suspensionRange,
                _wingOpen ? 0.5f : Mathf.Lerp(0.4f, 0.2f, Mathf.Min(1f, Mathf.Abs(motorSpeed) / settings.buggySpeed)),
                dt * 3f);
        }
        else
        {
            suspensionRange = 0.4f;
        }
        Vector3 centreOfMass = _rb.centerOfMass;
        centreOfMass.z = 0.2f;
        centreOfMass.y = settings.buggyCG * suspensionRange * 0.5f;
        _rb.centerOfMass = centreOfMass;
        _rb.mass = 30f;

        if (_rb.isKinematic)
        {
            for (int i = 0; i < 4; i++)
            {
                if (WheelRaycast(i, out RaycastHit kinematicHit))
                {
                    motorSpeed = _hitVelocity[i].z;
                    hitDistance[i] = kinematicHit.distance;
                }
                else hitDistance[i] = -1f;
            }
            return;
        }

        float speed = _rb.linearVelocity.magnitude;
        if (_wingOpen)
        {
            Fly(settings, input, brakes, dt);
        }
        // Drag and brakes
        else if (brakes && speed < 1.5f)
        {
            _rb.linearDamping = input.y != 0f ? 2f : 50f;
            _rb.angularDamping = 1f;
        }
        else if (brakes && speed < 10f)
        {
            _rb.linearDamping = input.y != 0f ? 2f : 10f;
        }
        else
        {
            _rb.angularDamping = 0.2f;
            _rb.linearDamping = 0.01f;
        }

        // Steering: front wheels 0 and 1 steer with input, rear wheels 2 and 3 counter-steer at low speed.
        float speedRatio = speed / settings.buggySpeed;
        float steer = Mathf.Lerp(40f, 30f, speedRatio) / 90f;
        wheelLocalRotation[0] = wheelLocalRotation[1] = Quaternion.LookRotation(
            new Vector3(input.x * steer, 0f, 1f - Mathf.Abs(input.x * steer)));
        steer = Mathf.Lerp(20f, 0f, speedRatio) / 90f;
        wheelLocalRotation[2] = wheelLocalRotation[3] = Quaternion.LookRotation(
            new Vector3(-input.x * steer, 0f, 1f - Mathf.Abs(input.x * steer)));

        // Motor
        _motorTorque = Mathf.Max(1f, Mathf.Lerp(settings.buggyPower * 5f, 0f, motorSpeed / (settings.buggySpeed * 10f)) * Mathf.Abs(_wingOpen ? 0f : input.y));
        float motorAccel = Mathf.Lerp(MaxAcceleration, 0f, motorSpeed / (settings.buggySpeed * 10f));
        motorSpeed += input.y * motorAccel / MotorMass * dt;
        motorSpeed += -motorSpeed * (brakes ? 50f : MotorDrag) / _motorTorque * dt;

        // Wheel / terrain collisions
        for (int i = 0; i < 4; i++)
        {
            if (WheelRaycast(i, out RaycastHit hit))
            {
                wheelsAreTouchingGround = true;
                Quaternion wheelRotation = transform.rotation * wheelLocalRotation[i];
                Quaternion toWheel = Quaternion.Inverse(wheelRotation);
                _hitCompress[i] = -(hit.distance / (suspensionRange + wheelRadius)) + 1f;
                _hitVelocity[i] = toWheel * _rb.GetPointVelocity(transform.TransformPoint(_wheelPositn[i]));
                if (hit.rigidbody != null)
                {
                    _rb.AddForceAtPosition((_hitVelocity[i] - toWheel * hit.rigidbody.GetPointVelocity(hit.point)) / 4f, hit.point, ForceMode.VelocityChange);
                }
                float friction = settings.buggyTr * 9f * Mathf.Lerp(0.5f, 1f, _hitCompress[i]) * Mathf.Max(1f, (20f - _hitVelocity[i].magnitude) / 4f);
                _rb.AddForceAtPosition(wheelRotation * Vector3.Min(new Vector3(
                    -_hitVelocity[i].x * friction,                     // Sideslip
                    0f,
                    -(_hitVelocity[i].z - motorSpeed) * friction       // Motor
                ), new Vector3(1000f, 1000f, 1000f)), hit.point);
                motorSpeed += ((_hitVelocity[i].z - motorSpeed) * friction * dt) / _motorTorque;
                hitDistance[i] = hit.distance;
                contactPoint[i] = hit.point;
                contactNormal[i] = hit.normal;
                skidIntensity[i] = Mathf.Abs(_hitVelocity[i].x) > Mathf.Abs(_hitVelocity[i].z) * 0.3f
                    ? Mathf.Abs(input.y) * 0.5f + 0.25f
                    : Mathf.Min(0.5f, friction * 0.05f);
            }
            else
            {
                hitDistance[i] = -1f;
            }
        }

        // Suspension
        for (int i = 0; i < 4; i++)
        {
            if (hitDistance[i] == -1f) continue;
            _rb.AddForceAtPosition(transform.up * (
                -_hitVelocity[i].y * settings.buggySh * (_wingOpen ? 3f : 1f)
                + _hitCompress[i] * (20f * _rb.mass) * (_wingOpen && i < 2 ? 8f : 1f)),
                transform.TransformPoint(_wheelPositn[i]));
        }

        // Collision friction: slide when driving, grip when crashing.
        bool driving = _wingOpen || wheelsAreTouchingGround
            || Physics.Raycast(transform.position, -transform.up, 3f, terrainMask, QueryTriggerInteraction.Ignore);
        _bodyMaterial.frictionCombine = driving ? PhysicsMaterialCombine.Minimum : PhysicsMaterialCombine.Maximum;

        _state.grounded = wheelsAreTouchingGround;
        _state.flightMode = _wingOpen;
        _state.SyncFromRigidbody(_rb);
    }

    private void Fly(Settings settings, Vector4 input, bool brakes, float dt)
    {
        motorInputSmoothed = Mathf.Lerp(input.y, motorInputSmoothed + (brakes ? -1f : 0f), 0.8f);
        Vector3 locVel = transform.InverseTransformDirection(_rb.linearVelocity);
        Vector3 euler = transform.eulerAngles;
        float roll = euler.z > 180f ? euler.z - 360f : euler.z;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;

        // Stalling below sqrt(16) = 4 m/s.
        if (locVel.sqrMagnitude <= StallSpeed)
        {
            _rb.angularDamping = 1f;
            _rb.linearDamping = _rb.linearVelocity.magnitude / settings.buggyFlightDrag * 9f;
            // Buggy.js wrote "input.y + 0.5 * 100": a constant nose-down pitch of 50.
            _rb.AddRelativeTorque(new Vector3(input.y + 0.5f * 100f, 0f, input.x * -30f));
            return;
        }

        _rb.linearDamping = _rb.linearVelocity.magnitude / settings.buggyFlightDrag * 0.3f;

        // Airbrakes
        if (brakes)
        {
            if (_brakePower < 1f) _brakePower += dt * 0.15f;
            float multiplier = -_brakePower * 2f;
            _rb.AddRelativeForce(locVel.x * multiplier * 5f, locVel.y * multiplier * 100f, locVel.z * 150f * multiplier);
            _rb.AddRelativeTorque(new Vector3((pitch + input.y * -100f) * -2f, input.x * 280f, roll * -1f));
        }
        // Standard flight: velocity follows the nose.
        else
        {
            _brakePower = 0f;
            float angDelta = Vector3.Angle(_rb.linearVelocity, transform.forward);
            if (angDelta > 10f && settings.buggyFlightSlip)
            {
                _rb.linearVelocity = transform.TransformDirection(locVel.x * 0.95f, locVel.y * 0.95f,
                    locVel.z + (Mathf.Abs(locVel.x) + Mathf.Abs(locVel.y)) * 0.1f * (angDelta / 360f));
            }
            else
            {
                float m = motorInputSmoothed;
                float thrust = settings.buggyFlightLooPower || (m < 0.999f && m > -0.999f) ? Mathf.Abs(m) / 10f : 0f;
                _rb.linearVelocity = transform.TransformDirection(0f, 0f, locVel.magnitude + dt * 50f * thrust);
            }
            _rb.AddRelativeTorque(new Vector3(motorInputSmoothed * 100f * settings.buggyFlightAgility, 0f, input.x * -100f * settings.buggyFlightAgility));
        }

        // Sideslip ("dihedral"); the original's pitch term was multiplied by zero.
        if (input.x == 0f && (euler.z < 90f || euler.z > 270f))
        {
            _rb.AddRelativeTorque(0f, roll * -0.6f, euler.z < 20f || euler.z > 340f ? roll * -0.5f : 0f);
        }
        else if (input.x == 0f)
        {
            _rb.AddRelativeTorque(0f, (euler.z - 180f) * 0.4f, 0f);
        }

        // Lava "thermals". Unity 3 raycasts hit triggers, so the water layer test does too.
        bool overLava = NearestHit(transform.position, Vector3.down, 10f, 1 << 4, QueryTriggerInteraction.Collide, out RaycastHit lava);
        if (transform.position.y < settings.lavaAlt + 10f || overLava)
        {
            _rb.AddForce(Vector3.up * (10f - (overLava ? lava.distance : 0f)) * 40f);
        }

        _rb.angularDamping = 5f;
    }

    // Self-righting
    private void OnCollisionStay(Collision collision)
    {
        if (_state.zorbBall) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (_isInverted && Vector3.Angle(transform.up, normal) < 50f) _isInverted = false;
            else if (!_isInverted && _rb.angularVelocity.sqrMagnitude < 5f && !_wingOpen && Vector3.Angle(transform.up, normal) > 120f) _isInverted = true;
            if (_isInverted) _rb.AddTorque(Vector3.Cross(transform.up, Vector3.up) * Vector3.Angle(transform.up, Vector3.up) * 3f);
        }
    }

    // The original ray started inside the body, which Unity 3 ignored; skip the
    // vehicle's own colliders explicitly. Triggers are ignored as a deliberate
    // change, so trigger volumes do not act as ground.
    private bool WheelRaycast(int i, out RaycastHit nearest)
    {
        return NearestHit(transform.TransformPoint(_wheelPositn[i]), -transform.up, suspensionRange + wheelRadius,
            terrainMask, QueryTriggerInteraction.Ignore, out nearest);
    }

    private bool NearestHit(Vector3 origin, Vector3 direction, float distance, int mask, QueryTriggerInteraction triggers, out RaycastHit nearest)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, _hits, distance, mask, triggers);
        nearest = default;
        float best = float.MaxValue;
        for (int h = 0; h < count; h++)
        {
            if (_hits[h].transform.IsChildOf(transform)) continue;
            if (_hits[h].distance < best)
            {
                best = _hits[h].distance;
                nearest = _hits[h];
            }
        }
        return best < float.MaxValue;
    }
}
