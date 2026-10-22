using UnityEngine;

// Port of VehicleMe.js weapons lock and firing for the local player: auto (fR)
// and snipe (fS) shots, lock-on shots (fRl, fSl), the firepower rules and
// cooldowns. The aimer presentation stays in LegacyLaserSightController.
[RequireComponent(typeof(VehicleStateModern))]
public class CombatSystemModern : MonoBehaviour
{
    // Rocket.js values serialised in LaserAuto.prefab and LaserSnipe.prefab
    // (read from the binary prefabs): speedX 1 and 10; mask Default and
    // LaserHit; maskOpt adds LaserOpt.
    private const int AutoSpeedX = 1;
    private const int SnipeSpeedX = 10;
    private const int RocketMask = (1 << 0) | (1 << 12);
    private const int RocketMaskOpt = RocketMask | (1 << 13);

    private VehicleStateModern _vehicle;
    private Transform _ridePos;
    private float _rocketFireTime;
    private int _shotCount;
    private bool _warnedMissingPrefab;

    // VehicleMe.js laserLock: the vehicle the lock ray found, else null.
    public VehicleStateModern laserLock { get; private set; }

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
    }

    private void Update()
    {
        // VehicleMe.js runs only on the local player's vehicle.
        if (Game.Settings == null || Game.Controller == null || Game.Player != gameObject) return;
        if (_ridePos == null) _ridePos = FindRidePos();

        Settings s = Game.Settings;
        UpdateWeaponsLock(s);
        int firepower = s.firepower != null && s.firepower.Length > _vehicle.vehicleId ? s.firepower[_vehicle.vehicleId] : 0;
        if (
            _ridePos != null &&
            s.lasersAllowed &&
            _rocketFireTime < Time.time &&
            firepower > 0 &&
            (laserLock != null ||
             (Input.GetButton("Fire1") && !Input.GetMouseButton(0)) ||
             (Input.GetButton("Fire1") && (Input.GetButton("Fire2") || Input.GetButton("Snipe") || s.camMode == 0)) ||
             (Input.GetButton("Fire1") && Input.mousePosition.x > Screen.width * 0.25f && Input.mousePosition.x < Screen.width - 200))
        )
        {
            bool snipe;
            Vector3 pos = _ridePos.position + transform.up * -0.1f;
            if (laserLock != null)
            {
                Rigidbody lockBody = laserLock.GetComponent<Rigidbody>();
                Vector3 lockVelocity = lockBody != null ? lockBody.linearVelocity : Vector3.zero;
                snipe = firepower > 2 || (firepower > 1 && (lockVelocity.sqrMagnitude > 500f || Vector3.Distance(transform.position, laserLock.transform.position) > 500f));
                Fire(snipe, pos, Quaternion.identity, laserLock);
            }
            else
            {
                Camera cam = Camera.main;
                Quaternion rang = (s.camMode == 0 || Input.GetButton("Fire2") || Input.GetButton("Snipe")) && cam != null
                    ? cam.transform.rotation
                    : _ridePos.rotation;
                snipe = (Input.GetButton("Snipe") && firepower > 1) || firepower > 2;
                Fire(snipe, pos, rang);
            }
            _rocketFireTime = Time.time + (snipe ? (firepower > 2 ? 0.5f : 2f) : 0.25f);
        }
    }

    // VehicleMe.js weapons locking: a ray along the vehicle's nose (not the
    // camera), starting (laserLock + camOffset × 0.1) × 15 m ahead, against
    // the LaserLock spheres on layer 14; it locks when either side is quarry.
    private void UpdateWeaponsLock(Settings s)
    {
        laserLock = null;
        if (!s.laserLocking) return;
        float setting = s.laserLock != null && s.laserLock.Length > _vehicle.vehicleId ? s.laserLock[_vehicle.vehicleId] : 0f;
        Vector3 origin = transform.position + transform.forward * ((setting + _vehicle.camOffset * 0.1f) * 15f);
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, Mathf.Infinity, 1 << LaserLockModern.Layer, QueryTriggerInteraction.Collide))
        {
            var veh = hit.transform.GetComponentInParent<VehicleStateModern>();
            if (veh != null && (_vehicle.isIt != 0 || veh.isIt != 0)) laserLock = veh;
        }
    }

    // Vehicle.js fR(), fS(), and with a target fRl() and fSl().
    public RocketModern Fire(bool snipe, Vector3 pos, Quaternion rot, VehicleStateModern target = null)
    {
        GameObject prefab = snipe ? Game.Controller.objectRocketSnipe : Game.Controller.objectRocket;
        if (prefab == null)
        {
            if (!_warnedMissingPrefab)
            {
                Debug.LogWarning("[CombatSystemModern] Game has no laser prefab assigned; run Prepare Game Scene Baseline and save Game.unity.");
                _warnedMissingPrefab = true;
            }
            return null;
        }

        GameObject go = Instantiate(prefab, pos, rot);
        RocketModern r = go.AddComponent<RocketModern>();
        r.launchVehicle = _vehicle;
        r.targetVehicle = target;
        r.laserID = Time.time.ToString("F3") + "-" + (_shotCount++);
        r.speedX = snipe ? SnipeSpeedX : AutoSpeedX;
        r.mask = RocketMask;
        r.maskOpt = RocketMaskOpt;
        GameObject sparks = snipe ? Game.Controller.objectRocketSnipeExplosion : Game.Controller.objectRocketExplosion;
        r.explosion = sparks != null ? sparks.transform : null;
        var shot = go.GetComponent<AudioSource>();
        if (shot != null) PlasmaSparksModern.Make3D(shot);
        // The bolt's trail uses the same plasma material as its sparks.
        var trail = go.GetComponent<TrailRenderer>();
        r.sparksMaterial = trail != null ? trail.sharedMaterial : null;
        return r;
    }

    // The buggy model's RideCam marks the original ridePos.
    private Transform FindRidePos()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "RideCam") return t;
        }
        return null;
    }
}
