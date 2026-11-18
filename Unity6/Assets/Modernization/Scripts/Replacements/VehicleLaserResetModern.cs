using System.Collections;
using UnityEngine;

// Port of Vehicle.js OnLaserHit and lR ("L Hits Rematerialize"): with the
// lasersFatal server setting on, a fatal laser hit more than 10 m from the base
// freezes the vehicle and, 2 s later, puts it back at the base. A second reset
// within 3 s is ignored. The RORB shrinks, is destroyed and is rebuilt, as
// lR() does. Not ported: the materialisation effect the original shows at
// both ends (Game.js mE, Entry Effect.prefab).
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleLaserResetModern : MonoBehaviour
{
    private Rigidbody _body;
    private float _lastReset = -10f;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    // RocketModern broadcasts this to the root of whatever it hits.
    private void OnLaserHit(bool isFatal)
    {
        if (!isFatal || Game.Settings == null || !Game.Settings.lasersFatal || World.@base == null) return;
        if (Vector3.Distance(transform.position, World.@base.position) <= 10f) return;
        if (_body != null) _body.isKinematic = true;
        StartCoroutine(Reset());
    }

    // Vehicle.js lR().
    private IEnumerator Reset()
    {
        if (Time.time - _lastReset < 3f || _body == null || World.@base == null) yield break;
        _lastReset = Time.time;
        _body.isKinematic = true;
        var orb = GetComponent<VehicleRamoSphereModern>();
        if (orb != null) orb.Shrink();
        yield return new WaitForSeconds(2f);
        if (orb != null) orb.DestroySphere();
        if (World.@base == null) yield break;
        transform.position = World.@base.position;
        _body.position = World.@base.position;
        _body.isKinematic = false;
        // Vehicle.js lR(): rebuild a new sphere.
        if (orb != null) orb.OnPrefsUpdated();
    }
}
