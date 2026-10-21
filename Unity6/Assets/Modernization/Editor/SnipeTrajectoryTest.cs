using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Play-mode check that a snipe bolt follows Rocket.js: launch speed
// laserSpeed × 10, gravity laserGrav × speedX × dt × 20 off the vertical
// velocity each physics step, a 5 s lifetime, and every hit on one step's
// swept ray handled. Logs to Unity6/Logs/snipe-trajectory-test.txt.
public static class SnipeTrajectoryTest
{
    private const int SnipeSpeedX = 10;
    private static readonly Stack<IEnumerator> _routine = new Stack<IEnumerator>();
    private static float _waitUntil;
    private static readonly StringBuilder Log = new StringBuilder();
    private static int _failures;

    [MenuItem("Tools/MarsXPLR/Run Snipe Trajectory Test")]
    private static void Run()
    {
        if (!EditorApplication.isPlaying || Game.Player == null)
        {
            Debug.LogWarning("Snipe test: enter Play mode and wait for the buggy to spawn.");
            return;
        }
        Log.Clear();
        _failures = 0;
        _routine.Clear();
        _routine.Push(Routine());
        _waitUntil = 0f;
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
    }

    private static void Step()
    {
        if (!EditorApplication.isPlaying) { Finish(); return; }
        if (Time.time < _waitUntil) return;
        if (_routine.Count == 0) { Finish(); return; }
        IEnumerator top = _routine.Peek();
        if (!top.MoveNext()) { _routine.Pop(); return; }
        if (top.Current is IEnumerator nested) _routine.Push(nested);
        else if (top.Current is float wait) _waitUntil = Time.time + wait;
    }

    private static void Finish()
    {
        EditorApplication.update -= Step;
        Log.AppendLine(_failures == 0 ? "PASS" : $"FAIL ({_failures})");
        string path = Path.Combine(Application.dataPath, "../Logs/snipe-trajectory-test.txt");
        File.WriteAllText(path, Log.ToString());
        Debug.Log("Snipe trajectory test finished:\n" + Log);
    }

    private static void Line(string text) => Log.AppendLine($"[{Time.time:F2}] {text}");

    private static void Check(bool ok, string text)
    {
        if (!ok) _failures++;
        Line((ok ? "ok   " : "FAIL ") + text);
    }

    private static IEnumerator Routine()
    {
        GameObject player = Game.Player;
        var combat = player.GetComponent<CombatSystemModern>();
        Settings s = Game.Settings;
        float dt = Time.fixedDeltaTime;
        float speed = s.laserSpeed * SnipeSpeedX;
        Line($"Unity {Application.unityVersion}, {SystemInfo.operatingSystem}; fixedDeltaTime {dt}, laserSpeed {s.laserSpeed}, laserGrav {s.laserGrav}, laserRico {s.laserRico}");

        // 1. Level shot at two static boxes 298 m and 310 m away, 60 m up.
        Vector3 origin = player.transform.position + Vector3.up * 60f;
        Vector3 dir = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        GameObject near = Box("SnipeTestNear", origin + dir * 300f);
        GameObject far = Box("SnipeTestFar", origin + dir * 312f);
        yield return 0.1f;

        int sparksBefore = CountSparks();
        RocketModern r = combat.Fire(true, origin, Quaternion.LookRotation(dir));
        Rigidbody rb = r.GetComponent<Rigidbody>();
        Line($"bolt rigidbody useGravity={rb.useGravity} linearDamping={rb.linearDamping} kinematic={rb.isKinematic} interpolation={rb.interpolation} collider={(r.GetComponent<Collider>() != null)}");
        yield return 0f;
        Check(Mathf.Abs(rb.linearVelocity.magnitude - speed) < 0.01f, $"launch speed {rb.linearVelocity.magnitude:F2} m/s, source {speed} (no inherited vehicle speed)");

        float t0 = Time.time;
        while (r != null && rb.linearVelocity != Vector3.zero && Time.time - t0 < 2f) yield return 0f;
        float stepLength = speed * dt;
        int hitStep = Mathf.CeilToInt(298f / stepLength);
        bool bothOnOneStep = 310f <= hitStep * stepLength;
        float stopped = Vector3.Dot(rb.position - origin, dir);
        Check(Mathf.Abs(stopped - (bothOnOneStep ? 310f : 298f)) < 0.05f, $"stopped at {stopped:F2} m; swept step {stepLength:F1} m, first face on step {hitStep}");
        int sparks = CountSparks() - sparksBefore;
        Check(sparks == (bothOnOneStep ? 2 : 1), $"sparks {sparks}, expected {(bothOnOneStep ? 2 : 1)} (Rocket.js handles every hit on the step)");
        Object.Destroy(near);
        Object.Destroy(far);
        yield return 0.2f;

        // 2. 45° shot into open sky with laserGrav 1, sampled against the
        // per-step law, then the lifetime.
        float savedGrav = s.laserGrav;
        s.laserGrav = 1f;
        float c = s.laserGrav * SnipeSpeedX * dt * 20f;
        Quaternion up45 = Quaternion.LookRotation(dir) * Quaternion.Euler(-45f, 0f, 0f);
        r = combat.Fire(true, origin, up45);
        rb = r.GetComponent<Rigidbody>();
        float horiz = speed * Mathf.Cos(45f * Mathf.Deg2Rad);
        float vy0 = speed * Mathf.Sin(45f * Mathf.Deg2Rad);
        float spawn = Time.time;
        float maxError = 0f;
        int samples = 0;
        while (r != null && Time.time - spawn < 7f)
        {
            Vector3 d = rb.position - origin;
            float k = Vector3.Dot(d, dir) / (horiz * dt);
            int steps = Mathf.RoundToInt(k);
            if (steps > 0 && Mathf.Abs(k - steps) < 0.01f)
            {
                float expected = vy0 * dt * steps - c * dt * steps * (steps + 1) * 0.5f;
                maxError = Mathf.Max(maxError, Mathf.Abs(d.y - expected));
                samples++;
            }
            yield return 0f;
        }
        s.laserGrav = savedGrav;
        float life = Time.time - spawn;
        float sourceLife = Mathf.Lerp(13f, 5f, s.laserSpeed * SnipeSpeedX * 0.003f);
        Check(samples > 10 && maxError < 0.05f, $"{samples} samples on whole steps, worst height error {maxError:F3} m against y = v0·dt·k − {c:F2}·dt·k(k+1)/2");
        Check(r == null && Mathf.Abs(life - sourceLife) < 0.1f, $"destroyed after {life:F2} s, source {sourceLife} s");
        Line("done");
    }

    private static GameObject Box(string name, Vector3 centre)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = centre;
        go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Game.Player.transform.forward, Vector3.up));
        go.transform.localScale = new Vector3(4f, 4f, 4f);
        return go;
    }

    private static int CountSparks()
    {
        int n = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.parent == null && t.name.StartsWith("PlasmaSparks")) n++;
        }
        return n;
    }
}
