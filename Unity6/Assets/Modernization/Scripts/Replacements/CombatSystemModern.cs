using UnityEngine;

// Port of VehicleMe.js firing for the local player: auto (fR) and snipe (fS)
// shots, the firepower rules and cooldowns. Weapons-lock firing (fRl, fSl) is
// not ported yet; the aimer presentation stays in LegacyLaserSightController.
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

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
    }

    private void Update()
    {
        if (Game.Settings == null || Game.Controller == null) return;
        if (_ridePos == null) _ridePos = FindRidePos();

        Settings s = Game.Settings;
        int firepower = s.firepower != null && s.firepower.Length > _vehicle.vehicleId ? s.firepower[_vehicle.vehicleId] : 0;
        bool chatting = Game.Messaging != null && Game.Messaging.chatting;
        if (
            _ridePos != null &&
            s.lasersAllowed &&
            _rocketFireTime < Time.time &&
            firepower > 0 &&
            !chatting &&
            ((Input.GetButton("Fire1") && !Input.GetMouseButton(0)) ||
             (Input.GetButton("Fire1") && (Input.GetButton("Fire2") || Input.GetButton("Snipe") || s.camMode == 0)) ||
             (Input.GetButton("Fire1") && Input.mousePosition.x > Screen.width * 0.25f && Input.mousePosition.x < Screen.width - 200))
        )
        {
            Camera cam = Camera.main;
            Quaternion rang = (s.camMode == 0 || Input.GetButton("Fire2") || Input.GetButton("Snipe")) && cam != null
                ? cam.transform.rotation
                : _ridePos.rotation;
            bool snipe = (Input.GetButton("Snipe") && firepower > 1) || firepower > 2;
            Fire(snipe, _ridePos.position + transform.up * -0.1f, rang);
            _rocketFireTime = Time.time + (snipe ? (firepower > 2 ? 0.5f : 2f) : 0.25f);
        }
    }

    // Vehicle.js fR() and fS().
    public RocketModern Fire(bool snipe, Vector3 pos, Quaternion rot)
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
