using UnityEngine;

// Port of VehicleBot.js: picks an enemy every 2 s, hides or pursues by quarry
// state (the original's always-on "Rabbit Hunt" mode), and fires a lock-on
// auto shot (fRl) at the enemy once a second while it has line of sight.
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleBotModern : MonoBehaviour
{
    private VehicleStateModern _vehicle;
    private Rigidbody _body;
    private CombatSystemModern _combat;
    private Transform _ridePos;

    private GameObject _enemy;
    private int _botMovement;
    private int _botEnemySelection;
    // VehicleBot.js declares enemyUpdateTime as an int, so it truncates.
    private int _enemyUpdateTime;
    private float _rocketFireTime;

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
        _body = GetComponent<Rigidbody>();
        _combat = GetComponent<CombatSystemModern>();
    }

    private void Update()
    {
        if (Game.Settings == null) return;
        if (_ridePos == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == "RideCam") { _ridePos = t; break; }
        }

        // Enemy updates.
        if (_enemyUpdateTime == 0 || Time.time - 2f > _enemyUpdateTime)
        {
            _enemyUpdateTime = (int)Time.time;
            if (_botEnemySelection == 1)
            {
                float distance = Mathf.Infinity;
                foreach (VehicleStateModern veh in FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None))
                {
                    GameObject go = veh.gameObject;
                    float curDistance = (go.transform.position - transform.position).sqrMagnitude;
                    if (curDistance < distance && go.name != name)
                    {
                        _enemy = go;
                        distance = curDistance;
                    }
                }
            }
            else if (_botEnemySelection == 2)
            {
                foreach (VehicleStateModern veh in FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None))
                {
                    if (veh.isIt == 1)
                    {
                        _enemy = veh.gameObject;
                        break;
                    }
                }
            }
        }

        // Rabbit Hunt mode (the original's Tag mode branch was never taken).
        if (_vehicle.isIt != 0)
        {
            _botMovement = 1;
            _botEnemySelection = 1;
        }
        else
        {
            _botMovement = 2;
            _botEnemySelection = 2;
        }

        Vector4 input = _vehicle.input;
        if (Game.Settings.botsCanDrive)
        {
            // Hiding movement.
            if (_botMovement == 1)
            {
                if (_vehicle.vehicleId == 2)
                {
                    input.x = Time.time % 2 == 0 ? 0f : Random.value * 2f - 1f;
                    input.y = 1f;
                }
                else
                {
                    input.x = Random.value * 2f - 1f;
                    input.y = 1f;
                }
            }
            // Pursuing movement.
            else if (_botMovement == 2 && _enemy != null)
            {
                float distance = Vector3.Distance(_enemy.transform.position, _body.position);
                float rotation = Quaternion.LookRotation(_enemy.transform.position - _body.position).eulerAngles.y - transform.localRotation.eulerAngles.y;
                if (rotation > 180f) rotation -= 360f;
                if (rotation < -180f) rotation += 360f;

                if (_vehicle.vehicleId == 0)
                {
                    rotation /= 20f;
                    if (distance < 15f) input.x = Mathf.Abs(rotation) < 0.1f ? 0f : Mathf.Clamp(rotation, -1f, 1f);
                    else input.x = Mathf.Abs(rotation) < 0.1f ? 0f : Mathf.Clamp(rotation, -0.6f, 0.6f);
                    input.y = 1f;
                    _vehicle.specialInput = false;
                }
                else if (_vehicle.vehicleId == 1)
                {
                    rotation /= 15f;
                    input.x = Mathf.Abs(rotation) < 0.1f ? 0f : Mathf.Clamp(rotation, -1f, 1f);
                    input.y = 1f;
                    _vehicle.specialInput = false;
                }
                else if (_vehicle.vehicleId == 2)
                {
                    rotation /= 80f;
                    input.x = Mathf.Abs(rotation) < 0.3f ? 0f : Mathf.Clamp(rotation, -1f, 1f);
                    input.y = Mathf.Abs(rotation) > 1f ? 0f : 1f;
                    _vehicle.specialInput = false;
                }
                else if (_vehicle.vehicleId == 3)
                {
                    rotation /= 10f;
                    input.x = Mathf.Abs(rotation) < 0.1f ? 0f : Mathf.Clamp(rotation, -1f, 1f);
                    input.y = 1f;
                    input.z = Physics.Raycast(transform.position, Vector3.down, 4f) ? 1f : 0.3f;
                    _vehicle.specialInput = true;
                }
            }
        }
        else
        {
            input.x = 0f;
            input.y = 0f;
        }
        _vehicle.input = input;

        // Fire! Layer 8 (Lightmapped) is the world geometry that blocks sight.
        if (_enemy != null && Game.Settings.botsCanFire && _ridePos != null && _combat != null &&
            Time.time - 1f > _rocketFireTime && !Physics.Linecast(transform.position, _enemy.transform.position, 1 << 8))
        {
            _rocketFireTime = Time.time;
            _combat.Fire(false, _ridePos.position + transform.up * -0.1f, Quaternion.identity, _enemy.GetComponent<VehicleStateModern>());
        }

        // Bounds checking.
        if (_body.position.y < -300f)
        {
            _body.linearVelocity = _body.linearVelocity.normalized;
            _body.isKinematic = true;
            transform.position = World.@base != null ? World.@base.position : Vector3.zero;
            _body.isKinematic = false;
            if (Game.Controller != null) Game.Controller.msg(name + " fell off the planet", 2);
        }
    }
}
