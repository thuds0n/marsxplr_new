using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleStateModern))]
public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 22f;
    public float acceleration = 28f;
    public float turnSpeed = 8f;
    public float steerTorque = 26f;
    public float groundCheckDistance = 1.2f;
    public float flightAcceleration = 8f;
    public float flightLift = 6f;
    public float flightPitchTorque = 14f;
    public float flightRollTorque = 10f;
    public float flightYawTorque = 0f;
    public float sideSlipDamping = 8f;
    public float takeoffImpulse = 3.5f;
    public float groundMaxSpeed = 30f;
    public float flightStallSpeed = 16f;
    public bool flightMode;

    private Rigidbody _rb;
    private VehicleStateModern _state;
    private Vector2 _input;
    private float _nextFireAt;
    private float _nextNoLaserMessageAt;
    private float _nextWingScanAt;
    private readonly List<Renderer> _wingRenderers = new List<Renderer>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _state = GetComponent<VehicleStateModern>();
        _rb.mass = 2f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.drag = 0.5f;
        _rb.angularDrag = 0.05f;

        if (_state != null)
        {
            _state.camOffset = 2f;
            _state.rideTarget = transform;
            _state.SetFlightMode(flightMode);
            _state.SyncFromRigidbody(_rb);
        }
    }

    private void Update()
    {
        _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (_state != null)
        {
            _state.SetInput(_input.x, _input.y);
            _state.grounded = IsGrounded();
            _state.SyncFromRigidbody(_rb);
        }

        if (Input.GetButtonDown("Jump") && (Game.Messaging == null || !Game.Messaging.chatting))
        {
            ToggleFlightMode();
        }

        if (Input.GetButton("Fire1"))
        {
            TryFireLaser();
        }

        if (_wingRenderers.Count == 0 && Time.time >= _nextWingScanAt)
        {
            _nextWingScanAt = Time.time + 1f;
            CacheWingRenderers();
            ApplyWingVisuals();
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic)
        {
            return;
        }

        Vector2 input = _input;
        Vector3 desiredDir = new Vector3(input.x, 0f, input.y);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        if (flightMode)
        {
            // Original buggy keeps gravity; wing mode changes aero forces/drag instead.
            _rb.useGravity = true;
            float flightDragRef = Game.Settings != null ? Mathf.Max(50f, Game.Settings.buggyFlightDrag) : 300f;
            float agility = Game.Settings != null ? Mathf.Clamp(Game.Settings.buggyFlightAgility, 0.5f, 1.6f) : 1f;
            float speedMag = _rb.velocity.magnitude;
            _rb.drag = Mathf.Clamp(speedMag / flightDragRef * 0.3f, 0.04f, 0.55f);
            _rb.angularDrag = 1.8f;

            // Legacy-like wing mode: vertical axis is throttle/reverse and drives pitch.
            float throttle = input.y;
            _rb.AddForce(transform.forward * throttle * flightAcceleration * 0.7f, ForceMode.Acceleration);
            _rb.AddTorque(transform.right * (-throttle * flightPitchTorque * agility), ForceMode.Acceleration);
            _rb.AddTorque(transform.forward * (-input.x * flightRollTorque * agility), ForceMode.Acceleration);
            if (Mathf.Abs(flightYawTorque) > 0.001f)
            {
                _rb.AddTorque(Vector3.up * (input.x * flightYawTorque * agility), ForceMode.Acceleration);
            }

            // Speed-based aerodynamic lift with stall behavior; no constant climb force.
            float forwardSpeed = Vector3.Dot(_rb.velocity, transform.forward);
            float lift = Mathf.Clamp((forwardSpeed - flightStallSpeed) * 0.9f, 0f, flightLift * 2.4f);
            _rb.AddForce(transform.up * lift, ForceMode.Acceleration);
            if (forwardSpeed < flightStallSpeed)
            {
                float stallSink = (flightStallSpeed - forwardSpeed) * 0.45f;
                _rb.AddForce(Vector3.down * stallSink, ForceMode.Acceleration);
            }

            // Parasitic drag similar to legacy flight decel.
            _rb.AddForce(-_rb.velocity * (speedMag / flightDragRef) * 0.38f, ForceMode.Acceleration);

            // Ground-effect boost like original "wingOpen near surface" behavior.
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit groundHit, 2.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                float proximity = Mathf.Clamp01((2.2f - groundHit.distance) / 2.2f);
                _rb.AddForce(Vector3.up * (7f * proximity), ForceMode.Acceleration);
            }

            // Mild auto-stability without removing manual pitch/roll control.
            _rb.AddTorque(-transform.right * Vector3.Dot(transform.up, Vector3.forward) * 1.1f, ForceMode.Acceleration);
            _rb.AddTorque(-transform.forward * Vector3.Dot(transform.up, Vector3.right) * 0.8f, ForceMode.Acceleration);
        }
        else
        {
            _rb.useGravity = true;
            _rb.drag = 0.5f;
            _rb.angularDrag = 2.2f;

            float throttle = Mathf.Clamp(input.y, -1f, 1f);
            float steer = Mathf.Clamp(input.x, -1f, 1f);
            _rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);

            float speed = _rb.velocity.magnitude;
            if (Mathf.Abs(steer) > 0.01f && (speed > 0.25f || Mathf.Abs(throttle) > 0.01f))
            {
                // Keep buggy-style reverse behavior: reverse input drives backward, it doesn't force a turn-around.
                float directionSign = throttle < -0.05f ? -1f : 1f;
                float steerScale = Mathf.Clamp01(speed / 8f);
                _rb.AddTorque(Vector3.up * steer * steerTorque * directionSign * steerScale, ForceMode.Acceleration);
            }

            if (Mathf.Abs(throttle) < 0.01f)
            {
                Vector3 damped = new Vector3(_rb.velocity.x * 0.985f, _rb.velocity.y, _rb.velocity.z * 0.985f);
                _rb.velocity = damped;
            }

            // Reduce sideways drift while driving.
            Vector3 localVel = transform.InverseTransformDirection(_rb.velocity);
            localVel.x = Mathf.Lerp(localVel.x, 0f, Time.fixedDeltaTime * sideSlipDamping);
            _rb.velocity = transform.TransformDirection(localVel);

            Vector3 av = _rb.angularVelocity;
            av.y = Mathf.Clamp(av.y, -1.8f, 1.8f);
            _rb.angularVelocity = new Vector3(0f, av.y, 0f);

            // Keep vehicle upright in drive mode.
            Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);
            if (tiltAxis.sqrMagnitude > 0.0001f)
            {
                _rb.AddTorque(tiltAxis * 12f, ForceMode.Acceleration);
            }

            // Keep drive speed in original-ish buggy envelope.
            Vector3 flat = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            float maxSpd = Game.Settings != null ? Mathf.Max(8f, Game.Settings.buggySpeed) : groundMaxSpeed;
            if (flat.magnitude > maxSpd)
            {
                Vector3 capped = flat.normalized * maxSpd;
                _rb.velocity = new Vector3(capped.x, _rb.velocity.y, capped.z);
            }
        }

        if (_state != null)
        {
            _state.SetInput(input.x, input.y);
            _state.grounded = IsGrounded();
            _state.SyncFromRigidbody(_rb);
        }
    }

    private void TryFireLaser()
    {
        if (Game.Settings == null) return;
        if (!Game.Settings.lasersAllowed)
        {
            if (Game.Controller != null && Time.time >= _nextNoLaserMessageAt)
            {
                _nextNoLaserMessageAt = Time.time + 0.8f;
                Game.Controller.msg("Lasers disabled in settings.", 2);
            }
            return;
        }

        if (Time.time < _nextFireAt) return;
        _nextFireAt = Time.time + Mathf.Max(0.03f, Game.Settings.laserCooldown);

        float range = Mathf.Max(10f, Game.Settings.laserRange);
        float tunedSpeed = Mathf.Max(1f, Game.Settings.laserProjectileSpeed);
        float tunedGravity = Game.Settings.laserProjectileGravity;

        Camera cam = Camera.main;
        Ray ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, Mathf.Min(0.2f, range / tunedSpeed));
            if (Game.Controller != null)
            {
                Game.Controller.RegisterLaserHit(hit.collider, hit.point, hit.normal, ray.direction);
            }
        }
        else
        {
            // Projectile speed and gravity are read now so values can be tuned before a full projectile implementation.
            Vector3 end = ray.origin + ray.direction * range + Vector3.down * (0.5f * tunedGravity * 0.04f);
            Debug.DrawLine(ray.origin, end, Color.red, Mathf.Min(0.2f, range / tunedSpeed));
        }
    }

    private Vector3 ProjectToCameraPlane(Vector3 inputDir)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return transform.TransformDirection(inputDir);
        }

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 world = (right * inputDir.x + forward * inputDir.z);
        if (world.sqrMagnitude < 0.0001f)
        {
            return transform.forward;
        }

        return world.normalized;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
    }

    private void ToggleFlightMode()
    {
        flightMode = !flightMode;
        if (_state != null)
        {
            _state.SetFlightMode(flightMode);
        }

        if (_rb != null)
        {
            _rb.useGravity = true;
            _rb.constraints = flightMode ? RigidbodyConstraints.None : (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);
            if (!flightMode && !IsGrounded())
            {
                _rb.velocity = new Vector3(_rb.velocity.x, Mathf.Min(_rb.velocity.y, 0f), _rb.velocity.z);
            }

            if (!flightMode)
            {
                AlignToGroundOrUpright();
                _rb.angularVelocity = Vector3.zero;
            }
            else
            {
                // Carry momentum into takeoff so high-speed wing-open state can lift.
                float forwardSpeed = Vector3.Dot(_rb.velocity, transform.forward);
                if (forwardSpeed > flightStallSpeed * 0.6f)
                {
                    float boost = Mathf.Clamp(forwardSpeed * 0.035f, 0f, takeoffImpulse);
                    _rb.velocity += Vector3.up * boost;
                }
            }
        }

        CacheWingRenderers();
        ApplyWingVisuals();

        if (Game.Controller != null)
        {
            Game.Controller.msg("Wings " + (flightMode ? "Opened (Flight Mode)" : "Closed (Drive Mode)"), 2);
        }
    }

    private void AlignToGroundOrUpright()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        if (Physics.Raycast(transform.position + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 planeForward = Vector3.ProjectOnPlane(forward, hit.normal);
            if (planeForward.sqrMagnitude < 0.001f) planeForward = Vector3.ProjectOnPlane(Vector3.forward, hit.normal);
            transform.rotation = Quaternion.LookRotation(planeForward.normalized, hit.normal);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private void CacheWingRenderers()
    {
        _wingRenderers.Clear();
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            string n = renderers[i].name.ToLowerInvariant();
            if (n.Contains("wing"))
            {
                _wingRenderers.Add(renderers[i]);
            }
        }
    }

    private void ApplyWingVisuals()
    {
        for (int i = 0; i < _wingRenderers.Count; i++)
        {
            if (_wingRenderers[i] != null)
            {
                _wingRenderers[i].enabled = flightMode;
            }
        }
    }
}
