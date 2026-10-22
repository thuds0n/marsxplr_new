using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Play-mode check of weapons lock (VehicleMe.js, Rocket.js auto-targeting):
// lifts the player 200 m clear of the world, puts a target vehicle on its nose
// line moving sideways, and checks the lock, the autofire, the snipe rule and
// that the lead-aimed bolt hits. Logs to Unity6/Logs/lock-on-test.txt.
public static class LockOnTest
{
    private static readonly Stack<IEnumerator> _routine = new Stack<IEnumerator>();
    private static float _waitUntil;
    private static readonly StringBuilder Log = new StringBuilder();
    private static int _failures;

    [MenuItem("Tools/MarsXPLR/Run Lock-on Test")]
    private static void Run()
    {
        if (!EditorApplication.isPlaying || Game.Player == null)
        {
            Debug.LogWarning("Lock-on test: enter Play mode and wait for the buggy to spawn.");
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
        string path = Path.Combine(Application.dataPath, "../Logs/lock-on-test.txt");
        File.WriteAllText(path, Log.ToString());
        Debug.Log("Lock-on test finished:\n" + Log);
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
        var state = player.GetComponent<VehicleStateModern>();
        var combat = player.GetComponent<CombatSystemModern>();
        var body = player.GetComponent<Rigidbody>();
        Settings s = Game.Settings;
        Line($"Unity {Application.unityVersion}, {SystemInfo.operatingSystem}; laserLocking {s.laserLocking}, laserLock {s.laserLock[0]}, firepower {s.firepower[0]}, player isIt {state.isIt}");

        // Hold the player level, 200 m up.
        bool wasKinematic = body.isKinematic;
        Vector3 home = body.position;
        body.isKinematic = true;
        player.transform.SetPositionAndRotation(home + Vector3.up * 200f, Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));
        Physics.SyncTransforms();
        yield return 0.2f;
        Check(combat.laserLock == null, "no lock with nothing ahead");

        // A target vehicle 150 m along the nose line, moving sideways at 10 m/s.
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "LockOnTestTarget";
        target.transform.localScale = new Vector3(3f, 2f, 3f);
        target.transform.position = player.transform.position + player.transform.forward * 150f;
        var tb = target.AddComponent<Rigidbody>();
        tb.useGravity = false;
        tb.linearDamping = 0f;
        var targetState = target.AddComponent<VehicleStateModern>();
        target.AddComponent<LaserLockModern>();
        tb.linearVelocity = player.transform.right * 10f;
        int scoreBefore = state.score;
        int sparksBefore = CountSparks();
        float t0 = Time.time;

        RocketModern bolt = null;
        while (Time.time - t0 < 1f && bolt == null)
        {
            foreach (RocketModern r in Object.FindObjectsByType<RocketModern>(FindObjectsSortMode.None))
                if (r.targetVehicle == targetState) bolt = r;
            yield return 0f;
        }
        Check(bolt != null, $"locked and fired on its own after {Time.time - t0:F2} s (lock {(combat.laserLock != null ? combat.laserLock.name : "none")})");
        if (bolt == null) { Cleanup(target, player, body, home, wasKinematic); yield break; }
        Check(bolt.speedX == 1, $"auto bolt (speedX {bolt.speedX}): firepower 2 snipes only past 500 m or above 22.4 m/s");

        yield return 0.12f;
        Vector3 aim = bolt.transform.forward;
        Vector3 toTarget = (target.transform.position - bolt.transform.position).normalized;
        float lead = Vector3.SignedAngle(toTarget, aim, Vector3.up);
        Check(lead > 1f, $"bolt leads the target by {lead:F2}° towards its motion");

        float t1 = Time.time;
        while (Time.time - t1 < 2f && state.score == scoreBefore) yield return 0f;
        Check(state.score == scoreBefore + 1, $"lead-aimed bolt hit the moving target: score {scoreBefore} -> {state.score}, sparks +{CountSparks() - sparksBefore}");

        // Above 22.4 m/s the lock-on shot becomes a snipe.
        tb.position = player.transform.position + player.transform.forward * 150f - player.transform.right * 3f;
        tb.linearVelocity = player.transform.right * 30f;
        bolt = null;
        float t2 = Time.time;
        while (Time.time - t2 < 1f && bolt == null)
        {
            foreach (RocketModern r in Object.FindObjectsByType<RocketModern>(FindObjectsSortMode.None))
                if (r.targetVehicle == targetState && r.speedX == 10) bolt = r;
            yield return 0f;
        }
        Check(bolt != null, "a fast target draws a lock-on snipe");

        Cleanup(target, player, body, home, wasKinematic);
        Line("done");
    }

    private static void Cleanup(GameObject target, GameObject player, Rigidbody body, Vector3 home, bool wasKinematic)
    {
        Object.Destroy(target);
        player.transform.position = home;
        body.isKinematic = wasKinematic;
    }

    private static int CountSparks()
    {
        int n = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.parent == null && t.name.StartsWith("PlasmaSparks")) n++;
        return n;
    }
}
