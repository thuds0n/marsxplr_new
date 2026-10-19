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
}

public class BuggyHandlingProbeRunner : MonoBehaviour
{
    private VehicleStateModern _state;
    private Rigidbody _rb;
    private readonly StringBuilder _log = new StringBuilder();
    private Vector3 _origin;
    private Quaternion _heading;

    private IEnumerator Start()
    {
        _state = GetComponent<VehicleStateModern>();
        _rb = GetComponent<Rigidbody>();
        PlayerVehicleInputModern input = GetComponent<PlayerVehicleInputModern>();
        if (input != null) input.enabled = false;

        Line("fixedDeltaTime " + Time.fixedDeltaTime);
        yield return Hold(Vector4.zero, false, 2f, null);
        _origin = transform.position;
        Vector3 away = _origin;
        GameObject baseObject = GameObject.Find("Base");
        if (baseObject != null) away -= baseObject.transform.position;
        away.y = 0f;
        _heading = Quaternion.LookRotation(away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward);

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

        yield return Reset("jump: released 18 m up at 20 m/s");
        transform.position += Vector3.up * 18f;
        _rb.linearVelocity = transform.forward * 20f;
        yield return Hold(Vector4.zero, false, 5f, "fall/land", 0.1f);

        if (input != null) input.enabled = true;
        string path = Path.Combine(Application.dataPath, "../Logs/handling-probe.txt");
        File.WriteAllText(path, _log.ToString());
        Debug.Log("[HandlingProbe] done -> " + path);
        Destroy(this);
    }

    private IEnumerator Reset(string name)
    {
        Line("== " + name);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
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
                    + " vy=" + _rb.linearVelocity.y.ToString("F1"));
            }
        }
    }

    private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    private void Line(string s) { _log.AppendLine(s); }
}
