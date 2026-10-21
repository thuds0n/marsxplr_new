using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Play-mode check of the laser slice: fires an auto and a snipe shot at the
// floor, then tags a temporary target vehicle and checks the quarry score.
// Logs to Unity6/Logs/laser-test.txt.
public static class LaserTest
{
    private static readonly System.Collections.Generic.Stack<IEnumerator> _routine = new System.Collections.Generic.Stack<IEnumerator>();
    private static float _waitUntil;
    private static readonly StringBuilder Log = new StringBuilder();

    [MenuItem("Tools/MarsXPLR/Run Laser Test")]
    private static void Run()
    {
        if (!EditorApplication.isPlaying || Game.Player == null)
        {
            Debug.LogWarning("Laser test: enter Play mode and wait for the buggy to spawn.");
            return;
        }
        Log.Clear();
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
        string path = Path.Combine(Application.dataPath, "../Logs/laser-test.txt");
        File.WriteAllText(path, Log.ToString());
        Debug.Log("Laser test finished:\n" + Log);
    }

    private static void Line(string text) => Log.AppendLine($"[{Time.time:F2}] {text}");

    private static IEnumerator Routine()
    {
        GameObject player = Game.Player;
        var state = player.GetComponent<VehicleStateModern>();
        var combat = player.GetComponent<CombatSystemModern>();
        Line($"Unity {Application.unityVersion}, {SystemInfo.operatingSystem}");
        Line($"player isIt={state.isIt} score={state.score} QuarryVeh={(Game.QuarryVeh != null ? Game.QuarryVeh.name : "none")} combat={(combat != null)}");
        Line($"prefabs auto={Game.Controller.objectRocket} snipe={Game.Controller.objectRocketSnipe} sparks={Game.Controller.objectRocketExplosion}/{Game.Controller.objectRocketSnipeExplosion}");

        // Shots at the floor 20 m ahead.
        foreach (bool snipe in new[] { false, true })
        {
            Vector3 origin = player.transform.position + Vector3.up * 1.5f;
            Vector3 aim = player.transform.position + player.transform.forward * 20f;
            RocketModern r = combat.Fire(snipe, origin, Quaternion.LookRotation(aim - origin));
            yield return Track(r, snipe ? "snipe at floor" : "auto at floor");
        }

        // A temporary target vehicle 25 m ahead; the player is quarry, so a
        // tag should score (iS) and a second tag inside 3 s should not.
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "LaserTestTarget";
        target.transform.localScale = new Vector3(3f, 2f, 3f);
        target.transform.position = player.transform.position + player.transform.forward * 25f + Vector3.up * 1.5f;
        var body = target.AddComponent<Rigidbody>();
        body.isKinematic = true;
        var targetState = target.AddComponent<VehicleStateModern>();
        yield return 0.1f;

        for (int i = 0; i < 3; i++)
        {
            int before = state.score;
            Vector3 origin = player.transform.position + Vector3.up * 1.5f;
            RocketModern r = combat.Fire(false, origin, Quaternion.LookRotation(target.transform.position - origin));
            yield return Track(r, "auto at target");
            Line($"score {before} -> {state.score}, target lastTag={targetState.lastTag:F2}");
            yield return i == 0 ? 1f : 3.2f;
        }

        Object.Destroy(target);
        Line("done");
    }

    private static IEnumerator Track(RocketModern r, string label)
    {
        if (r == null) { Line(label + ": no rocket"); yield break; }
        Vector3 start = r.transform.position;
        int sparksBefore = CountSparks();
        float t0 = Time.time;
        Rigidbody rb = r.GetComponent<Rigidbody>();
        while (r != null && Time.time - t0 < 3f)
        {
            if (Time.time - t0 > 0.05f && rb != null && rb.linearVelocity == Vector3.zero) break;
            yield return 0f;
        }
        if (r == null) { Line(label + ": rocket destroyed without a hit"); yield break; }
        string what = Physics.Raycast(start, r.transform.position - start, out RaycastHit h, Vector3.Distance(start, r.transform.position) + 0.5f, -1, QueryTriggerInteraction.Collide)
            ? h.collider.name + " layer " + h.collider.gameObject.layer + (h.collider.isTrigger ? " trigger" : "") : "nothing";
        Line($"{label}: hit {what}, stopped at {r.transform.position} ({Vector3.Distance(start, r.transform.position):F1} m) after {Time.time - t0:F2} s, sparks {sparksBefore} -> {CountSparks()}");
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
