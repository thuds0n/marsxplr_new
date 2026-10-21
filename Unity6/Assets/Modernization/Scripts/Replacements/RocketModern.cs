using System.Collections;
using UnityEngine;

// Port of Rocket.js for single player: a laser bolt that sweeps a ray along
// its path each physics step. Added at runtime to the original LaserAuto and
// LaserSnipe prefabs, whose serialised Rocket.js values are copied in by
// CombatSystemModern. Network lag compensation and lock-on lead targeting are
// not ported yet.
public class RocketModern : MonoBehaviour
{
    public Transform explosion;
    public VehicleStateModern launchVehicle;
    public int speedX = 1;
    public string laserID;
    public LayerMask mask = -1;
    public LayerMask maskOpt = -1;

    private Rigidbody _body;
    private Vector3 _strtPos;
    private int _stage = -1;
    private float _velocity;
    private readonly RaycastHit[] _hits = new RaycastHit[16];

    private IEnumerator Start()
    {
        name = "lsr#" + laserID;
        _body = GetComponent<Rigidbody>();
        Rigidbody launchBody = launchVehicle != null ? launchVehicle.GetComponent<Rigidbody>() : null;
        Vector3 launchVelocity = launchBody != null ? launchBody.linearVelocity : Vector3.zero;
        Transform launch = launchVehicle != null ? launchVehicle.transform : transform;
        _velocity = Game.Settings.laserSpeed * speedX + Mathf.Max(0f,
            launch.InverseTransformDirection(launchVelocity).z *
            Vector3.Dot(transform.forward, launch.forward));

        _body.linearVelocity = transform.TransformDirection(0f, 0f, _velocity);
        _strtPos = _body.position;
        _stage = 0;

        yield return new WaitForSeconds(Mathf.Lerp(13f, 5f, Game.Settings.laserSpeed * speedX * 0.003f));
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (_stage != 0 || launchVehicle == null || Game.Settings == null) return;
        if (Game.Settings.laserGrav != 0f)
        {
            Vector3 v = _body.linearVelocity;
            v.y -= Game.Settings.laserGrav * speedX * Time.deltaTime * 20f;
            _body.linearVelocity = v;
        }

        Vector3 vector = _body.position - _strtPos;
        int count = Physics.RaycastNonAlloc(_strtPos, vector, _hits, vector.magnitude,
            Game.Settings.lasersOptHit ? maskOpt : mask, QueryTriggerInteraction.Collide);
        // Rocket.js walks RaycastAll's hits unsorted; take them nearest first.
        System.Array.Sort(_hits, 0, count, HitDistanceComparer.Instance);
        for (int i = 0; i < count && _stage == 0; i++)
        {
            RaycastHit collision = _hits[i];

            // Launch vehicle is immune to hits.
            if (collision.collider.transform.root.gameObject == launchVehicle.gameObject) continue;

            if (collision.rigidbody != null && GameModeSinglePlayerModern.Instance != null)
            {
                var veh = collision.transform.root.GetComponent<VehicleStateModern>();
                if (veh != null) GameModeSinglePlayerModern.Instance.OnVehicleLasered(launchVehicle, veh);
            }

            if (collision.rigidbody != null || Game.Settings.laserRico == 0f)
            {
                LaserHit(collision.transform.root.gameObject, collision.point, collision.normal);
            }
            else
            {
                _body.position = collision.point;
                _body.linearVelocity = Game.Settings.laserRico * Vector3.Lerp(
                    Vector3.Scale(_body.linearVelocity, collision.normal),
                    Vector3.Reflect(_body.linearVelocity, collision.normal),
                    Game.Settings.laserRico);
            }
        }

        _strtPos = _body.position;
    }

    public void LaserHit(GameObject go, Vector3 pos, Vector3 norm)
    {
        _stage = 1;

        _body.position = pos;
        transform.position = pos;
        _body.linearVelocity = Vector3.zero;
        go.BroadcastMessage("OnLaserHit", true, SendMessageOptions.DontRequireReceiver);
        Collider[] colliders = Physics.OverlapSphere(pos, 10f);
        foreach (Collider hit in colliders)
        {
            if (hit.attachedRigidbody != null)
            {
                hit.attachedRigidbody.AddExplosionForce(350 + speedX * 300, pos, 1f, 2f);
            }
        }
        if (explosion != null) Instantiate(explosion, pos, Quaternion.FromToRotation(Vector3.up, norm));
    }

    private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }
}
