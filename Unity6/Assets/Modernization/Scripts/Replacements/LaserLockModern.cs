using UnityEngine;

// Port of Vehicle.js laserLock: an invisible trigger sphere (Vehicle.prefab's
// LaserLock: radius 0.5, layer 14) that other vehicles' weapons-lock rays
// look for. OnPrefsUpdated scales it to (laserLock + camOffset × 0.1) × 10 and
// disables it when the vehicle type's laserLock setting is 0.
[RequireComponent(typeof(VehicleStateModern))]
public class LaserLockModern : MonoBehaviour
{
    public const int Layer = 14;

    private VehicleStateModern _vehicle;
    private GameObject _lock;

    private void Awake()
    {
        _vehicle = GetComponent<VehicleStateModern>();
        Transform existing = transform.Find("LaserLock");
        if (existing != null)
        {
            _lock = existing.gameObject;
        }
        else
        {
            _lock = new GameObject("LaserLock");
            _lock.transform.SetParent(transform, false);
            var sphere = _lock.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphere.isTrigger = true;
        }
        _lock.layer = Layer;
    }

    private void Update()
    {
        Settings s = Game.Settings;
        float setting = s != null && s.laserLock != null && s.laserLock.Length > _vehicle.vehicleId ? s.laserLock[_vehicle.vehicleId] : 0f;
        bool on = setting > 0f;
        if (_lock.activeSelf != on) _lock.SetActive(on);
        _lock.transform.localScale = on ? Vector3.one * ((setting + _vehicle.camOffset * 0.1f) * 10f) : Vector3.zero;
    }
}
