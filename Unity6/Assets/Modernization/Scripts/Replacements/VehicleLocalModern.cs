using UnityEngine;

// Port of VehicleLocal.js, which every locally owned vehicle (the player and
// bots) carried, and of the bounds checks at the end of VehicleMe.js Update(),
// which only the player's vehicle ran. Networking (position and input sync) is
// not ported.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleLocalModern : MonoBehaviour
{
    // Vehicle.js terrainMask as serialised in the original Vehicle.prefab
    // (m_Bits 2049): layers 0 (Default) and 11. The script default, ~1<<4, was
    // overridden there.
    public const int TerrainMask = (1 << 0) | (1 << 11);

    private VehicleStateModern _vehicle;
    private Rigidbody _body;
    private Vector3 _prevPos = Vector3.zero;

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
        _body = GetComponent<Rigidbody>();
    }

    // VehicleMe.js Update(), bounds checking: the player's vehicle only.
    // VehicleBotModern does its own, as VehicleBot.js did.
    private void Update()
    {
        if (_vehicle.isBot || Game.Player != gameObject) return;
        if (_body.position.y < -300f)
        {
            _body.linearVelocity = _body.linearVelocity.normalized;
            _body.isKinematic = true;
            transform.position = World.@base != null ? World.@base.position : Vector3.zero;
            _body.isKinematic = false;
            if (Game.Controller != null) Game.Controller.msg(name + " fell off the planet", 2);
        }
        if (_body.linearVelocity.magnitude > 500f) _body.linearVelocity = _body.linearVelocity * 0.5f;
    }

    // VehicleLocal.js FixedUpdate(), terrain penetration: sweep from the last
    // position to this one and, if something else was crossed, sit back on
    // its surface. A jump of more than sqrt(500) m is a teleport and is let
    // through. Unity 2.x rays also hit triggers.
    private void FixedUpdate()
    {
        bool found = false;
        RaycastHit hit = default;
        if (_prevPos != Vector3.zero)
        {
            Vector3 delta = transform.position - _prevPos;
            RaycastHit[] hits = Physics.RaycastAll(_prevPos, delta.normalized, delta.magnitude, TerrainMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.root != transform.root)
                {
                    hit = hits[i];
                    found = true;
                    break;
                }
            }
        }
        if (found && hit.point != Vector3.zero)
        {
            if ((_prevPos - transform.position).sqrMagnitude < 500f) transform.position = hit.point + hit.normal * 0.1f;
            else _prevPos = transform.position;
        }
        else _prevPos = transform.position;
    }

    // VehicleLocal.js OnTriggerStay(): any of this vehicle's colliders or
    // triggers (the LaserLock sphere included) overlapping another rigidbody.
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LaserLockModern.Layer) return;
        if (other.attachedRigidbody != null) OnRam(other.attachedRigidbody.gameObject);
    }

    // VehicleLocal.js OnCollisionStay(): body contact with another rigidbody.
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.transform.root == transform.root) return;
        Transform parent = collision.transform.parent;
        if (parent != null && parent.GetComponent<Rigidbody>() != null) OnRam(parent.gameObject);
        else if (collision.rigidbody != null) OnRam(collision.gameObject);
    }

    // Vehicle.js OnRam(): we touched the quarry, so we become quarry.
    public void OnRam(GameObject other)
    {
        var veh = other.GetComponent<VehicleStateModern>();
        // A vehicle's own colliders never trigger each other in Unity, so the
        // original needed no self check; kept as a guard.
        if (veh == null || veh == _vehicle || veh.isIt != 1 || !veh.isResponding ||
            Time.time - _vehicle.lastTag < 3f || Time.time - veh.lastTag < 3f) return;
        _vehicle.lastTag = Time.time;
        if (GameModeSinglePlayerModern.Instance != null) GameModeSinglePlayerModern.Instance.SetQuarry(_vehicle, 1);
    }
}
