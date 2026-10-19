using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Play-mode probe that drives the buggy through a fixed input script and logs
// HUD-equivalent telemetry for comparison with the original game.
public static class BuggyHandlingProbe
{
    [MenuItem("Tools/MarsXPLR/Run Buggy Handling Probe")]
    private static void Run()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyHandlingProbeRunner>();
    }

    // Take-off, glide, airbrake, stall, and landing on the probe pad.
    [MenuItem("Tools/MarsXPLR/Run Buggy Flight Probe")]
    private static void RunFlight()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyHandlingProbeRunner>().flight = true;
    }

    // Drives from wherever the buggy is: 3 s of throttle, then 1.5 s of full
    // right lock while coasting, for comparing tyre tracks on real ground.
    [MenuItem("Tools/MarsXPLR/Run Buggy Track Test")]
    private static void RunTrackTest()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyTrackTestRunner>();
    }
}

public class BuggyTrackTestRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        var state = GetComponent<VehicleStateModern>();
        var input = GetComponent<PlayerVehicleInputModern>();
        if (input != null) input.enabled = false;
        // Open floor south of Fort Boxy's castle, facing away from it.
        var rb = GetComponent<Rigidbody>();
        if (Physics.Raycast(new Vector3(0f, 200f, -300f), Vector3.down, out RaycastHit ground, 400f))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = ground.point + Vector3.up * 0.6f;
            rb.rotation = Quaternion.Euler(0f, 180f, 0f);
            transform.SetPositionAndRotation(rb.position, rb.rotation);
        }
        for (float t = 0f; t < 1.5f; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
        foreach (var step in new[] { (new Vector4(0, 1, 0, 0), 3f), (new Vector4(1, 0, 0, 0), 1.5f), (Vector4.zero, 2f) })
        {
            for (float t = 0f; t < step.Item2; t += Time.fixedDeltaTime)
            {
                state.input = step.Item1;
                yield return new WaitForFixedUpdate();
            }
        }
        if (input != null) input.enabled = true;
        Destroy(this);
    }
}

public class BuggyHandlingProbeRunner : MonoBehaviour
{
    public bool flight;
    private BuggyControllerModern _buggy;
    private VehicleStateModern _state;
    private Rigidbody _rb;
    private readonly StringBuilder _log = new StringBuilder();
    private Vector3 _origin;
    private Quaternion _heading;
    private GameObject _pad;
    private GameObject _ledge;

    private IEnumerator Start()
    {
        _state = GetComponent<VehicleStateModern>();
        _rb = GetComponent<Rigidbody>();
        _buggy = GetComponent<BuggyControllerModern>();
        PlayerVehicleInputModern input = GetComponent<PlayerVehicleInputModern>();
        if (input != null) input.enabled = false;

        Line("fixedDeltaTime " + Time.fixedDeltaTime);
        Line("mass " + _rb.mass + " inertia " + _rb.inertiaTensor.ToString("F2") + " inertiaRot " + _rb.inertiaTensorRotation.eulerAngles.ToString("F0")
            + " centreOfMass " + _rb.centerOfMass.ToString("F2") + " maxAngularVelocity " + _rb.maxAngularVelocity);
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            Line("collider " + c.GetType().Name + " on '" + c.name + "' enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy
                + " trigger=" + c.isTrigger + " layer=" + c.gameObject.layer + " bounds=" + c.bounds.size.ToString("F2"));
        }
        // A flat pad above the world removes Fort Boxy's ramps and bumps from the
        // measurements; a ledge 18 m higher matches the original spawn-platform drop.
        _pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _pad.name = "HandlingProbePad";
        _pad.transform.position = new Vector3(0f, 1000f, 0f);
        _pad.transform.localScale = new Vector3(2000f, 1f, 2000f);
        _ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ledge.name = "HandlingProbeLedge";
        _ledge.transform.position = new Vector3(0f, 1018f, -480f);
        _ledge.transform.localScale = new Vector3(40f, 1f, 40f);
        _origin = new Vector3(0f, 1001f, -800f);
        _heading = Quaternion.identity;
        if (flight)
        {
            yield return Flight();
            Finish(input, "flight-probe.txt");
            yield break;
        }
        yield return Reset("coast");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        Vector3 start = transform.position;
        float t = 0f;
        while (_state.mph > 1f && t < 30f) { yield return Hold(Vector4.zero, false, 1f, "coast"); t += 1f; }
        Line("coast distance " + Flat(transform.position - start).ToString("F1") + " m over " + t + " s");

        yield return Reset("brakes (Fire3)");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(Vector4.zero, true, "brakes");

        yield return Reset("reverse throttle as brake, then reverse");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(new Vector4(0, -1, 0, 0), false, "reverse-brake");
        yield return Hold(new Vector4(0, -1, 0, 0), false, 4f, "reverse");

        yield return Reset("cornering: full right lock while coasting");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        float yaw0 = transform.eulerAngles.y;
        yield return Hold(new Vector4(1, 0, 0, 0), false, 2f, "steer");
        Line("heading change " + Mathf.DeltaAngle(yaw0, transform.eulerAngles.y).ToString("F0") + " deg");

        _origin = new Vector3(0f, 1019f, -495f);
        yield return Reset("drop: drive off an 18 m ledge at full throttle");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 6f, "drive/fall/land", 0.1f);
        yield return Hold(Vector4.zero, false, 2f, "settle", 0.25f);

        Finish(input, "handling-probe.txt");
    }

    private IEnumerator Flight()
    {
        yield return Reset("flight: take-off from the pad");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle", 0.5f);
        _state.specialInput = true;
        yield return Hold(new Vector4(0, 1, 0, 0), false, 0.75f, "wings opening", 0.25f);
        // A full pull-up for more than about half a second loops the buggy.
        yield return Hold(new Vector4(0, -1, 0, 0), false, 0.4f, "pull up", 0.1f);
        yield return Hold(new Vector4(0, 0.5f, 0, 0), false, 0.3f, "level off", 0.1f);
        BuggyWingsModern wings = GetComponent<BuggyWingsModern>();
        if (wings != null && wings.wings != null)
        {
            Renderer r = wings.wings.GetComponent<Renderer>();
            Line("wings active=" + wings.wings.gameObject.activeInHierarchy + " bounds=" + r.bounds.size.ToString("F2")
                + " shader=" + r.sharedMaterial.shader.name + " supported=" + r.sharedMaterial.shader.isSupported + " colour=" + r.sharedMaterial.color
                + " spec=" + r.sharedMaterial.GetColor("_SpecColor") + " cube=" + r.sharedMaterial.GetTexture("_Cube") + " layer=" + r.gameObject.layer
                + " trails=" + (wings.leftTrail != null ? wings.leftTrail.localPosition.ToString("F1") : "none") + " " + (wings.rightTrail != null ? wings.rightTrail.localPosition.ToString("F1") : "none"));
        }
        Line("== glide");
        yield return Hold(Vector4.zero, false, 2f, "glide", 0.25f);
        yield return Hold(new Vector4(1, 0, 0, 0), false, 0.5f, "roll right", 0.1f);
        yield return Hold(Vector4.zero, false, 1.5f, "level", 0.25f);
        Line("== airbrake");
        yield return Hold(Vector4.zero, true, 1.5f, "airbrake", 0.1f);
        Line("== stall: climb steeply until airspeed falls below 4 m/s");
        yield return Hold(new Vector4(0, -1, 0, 0), false, 0.35f, "stall", 0.1f);
        yield return Hold(Vector4.zero, false, 3f, "stall", 0.1f);
        Line("== descend and land with wings open");
        float t = 0f;
        while (!_state.grounded && t < 30f) { yield return Hold(Vector4.zero, false, 0.5f, "descend", 0.5f); t += 0.5f; }
        yield return Hold(Vector4.zero, false, 2f, "landed", 0.5f);
        _state.specialInput = false;
        Line("== wings closing");
        yield return Hold(Vector4.zero, false, 1f, "closing", 0.25f);
        yield return Hold(Vector4.zero, false, 2f, "drive mode", 0.5f);
    }

    private void Finish(PlayerVehicleInputModern input, string file)
    {
        Destroy(_pad);
        Destroy(_ledge);
        if (input != null) input.enabled = true;
        string path = Path.Combine(Application.dataPath, "../Logs/" + file);
        File.WriteAllText(path, _log.ToString());
        Debug.Log("[HandlingProbe] done -> " + path);
        Destroy(this);
    }

    private IEnumerator Reset(string name)
    {
        Line("== " + name);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _state.specialInput = false;
        _rb.position = _origin + Vector3.up * 0.5f;
        _rb.rotation = _heading;
        transform.SetPositionAndRotation(_rb.position, _rb.rotation);
        yield return Hold(Vector4.zero, false, 1.5f, null);
    }

    private IEnumerator StopTest(Vector4 input, bool brakes, string label)
    {
        Vector3 start = transform.position;
        float t = 0f;
        float v0 = _state.mph;
        while (_state.mph > 1f && t < 15f)
        {
            _state.input = input;
            _state.brakes = brakes;
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
        _state.brakes = false;
        Line(label + ": " + v0.ToString("F0") + " MPH to <1 MPH in " + t.ToString("F2") + " s over " + Flat(transform.position - start).ToString("F1") + " m");
    }

    private IEnumerator Hold(Vector4 input, bool brakes, float seconds, string label, float sample = 0.5f)
    {
        float t = 0f, next = sample;
        while (t < seconds)
        {
            _state.input = input;
            _state.brakes = brakes;
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            if (label != null && t >= next - 1e-4f)
            {
                next += sample;
                Vector3 e = transform.eulerAngles;
                Line(label + " t=" + t.ToString("F1") + " mph=" + _state.mph.ToString("F0") + " alt=" + _state.altitude.ToString("F1")
                    + " pitch=" + Mathf.DeltaAngle(0, e.x).ToString("F0") + " roll=" + Mathf.DeltaAngle(0, e.z).ToString("F0")
                    + " vy=" + _rb.linearVelocity.y.ToString("F1") + " yawRate=" + (_rb.angularVelocity.y * Mathf.Rad2Deg).ToString("F0")
                    + (flight ? " wing=" + _buggy.wingState.ToString("F2") + (_buggy.wingOpen ? " open" : " shut") + " grounded=" + _state.grounded
                        + " susp=" + _buggy.suspensionRange.ToString("F2") + " drag=" + _rb.linearDamping.ToString("F2") + " angDrag=" + _rb.angularDamping.ToString("F1") : ""));
            }
        }
    }

    private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    private void Line(string s) { _log.AppendLine(s); }
}
