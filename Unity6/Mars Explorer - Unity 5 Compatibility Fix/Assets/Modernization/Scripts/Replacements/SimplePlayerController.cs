using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 22f;
    public float acceleration = 18f;
    public float turnSpeed = 8f;
    public float jumpImpulse = 7f;
    public float groundCheckDistance = 1.2f;

    private Rigidbody _rb;
    private bool _jumpQueued;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass = 2f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpQueued = true;
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic)
        {
            _jumpQueued = false;
            return;
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 desiredDir = new Vector3(input.x, 0f, input.y);

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            desiredDir = desiredDir.normalized;
            Vector3 worldDir = ProjectToCameraPlane(desiredDir);
            Vector3 targetVelocity = worldDir * moveSpeed * Mathf.Clamp01(input.magnitude);
            Vector3 currentHorizontal = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            Vector3 delta = targetVelocity - currentHorizontal;
            _rb.AddForce(delta * acceleration, ForceMode.Acceleration);

            Quaternion look = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.fixedDeltaTime * turnSpeed);
        }
        else
        {
            Vector3 damped = new Vector3(_rb.velocity.x * 0.9f, _rb.velocity.y, _rb.velocity.z * 0.9f);
            _rb.velocity = damped;
        }

        if (_jumpQueued && IsGrounded())
        {
            _rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }

        _jumpQueued = false;
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
}
