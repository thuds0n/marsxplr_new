using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Records what happens while you play against bots by hand, for the questions
// that are hard to judge by eye: where each bot shot ends (on the player, or on
// what else), and when the player's aimer swaps to the locked aimer and how
// many of its particles are alive. It also logs the player's own lock-on shots
// (lead at launch, hit or miss, launch-to-impact time) for comparison with the
// original's homing. Start, play, then stop; logs to
// Unity6/Logs/bot-accuracy.txt.
public static class BotAccuracyLog
{
    private class Shot
    {
        public RocketModern rocket;
        public string bot;
        public bool snipe;
        public float fired;
        public float closest = float.MaxValue;
        public Vector3 last;
        public float launched = -1f;
        public float lead = -1f;
    }

    private static readonly StringBuilder Log = new StringBuilder();
    private static readonly List<Shot> _shots = new List<Shot>();
    private static readonly HashSet<int> _seen = new HashSet<int>();
    private static readonly List<Shot> _ended = new List<Shot>();
    private static int _fired, _onPlayer, _onFloor, _elsewhere, _noSparks, _tags;
    private static bool _wasLocked;
    private static float _lockedSince, _lastTag;
    // Player lock-on shots: count, hits on the target, and the sums behind the
    // mean lead (bolt direction against the line to the target at launch) and
    // the mean time from launch to impact for hits.
    private static int _playerShots, _playerHits;
    private static float _leadSum, _flightSum;
    private static int _leadCount;

    [MenuItem("Tools/MarsXPLR/Start Bot Accuracy Log")]
    private static void Start()
    {
        if (!EditorApplication.isPlaying || Game.Player == null)
        {
            Debug.LogWarning("Bot accuracy log: enter Play mode and wait for the buggy to spawn.");
            return;
        }
        Log.Clear();
        _shots.Clear();
        _seen.Clear();
        _ended.Clear();
        _fired = _onPlayer = _onFloor = _elsewhere = _noSparks = _tags = 0;
        _playerShots = _playerHits = _leadCount = 0;
        _leadSum = _flightSum = 0f;
        _wasLocked = false;
        _lastTag = Game.Player.GetComponent<VehicleStateModern>().lastTag;
        Line($"Unity {Application.unityVersion}, {SystemInfo.operatingSystem}");
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
        Debug.Log("Bot accuracy log started.");
    }

    [MenuItem("Tools/MarsXPLR/Stop Bot Accuracy Log")]
    private static void Stop()
    {
        EditorApplication.update -= Step;
        Line($"player lock-on shots {_playerShots}: hits {_playerHits}" +
            (_leadCount > 0 ? $", mean lead {_leadSum / _leadCount:F2} deg" : "") +
            (_playerHits > 0 ? $", mean launch-to-impact {_flightSum / _playerHits:F2} s" : ""));
        Line($"bot shots {_fired}: on the player {_onPlayer}, on the floor {_onFloor}, elsewhere {_elsewhere}, ended with no sparks {_noSparks}; player tagged {_tags} times");
        string path = Path.Combine(Application.dataPath, "../Logs/bot-accuracy.txt");
        File.WriteAllText(path, Log.ToString());
        Debug.Log("Bot accuracy log:\n" + Log);
    }

    private static void Line(string text) => Log.AppendLine($"[{Time.time:F2}] {text}");

    private static void Step()
    {
        if (!EditorApplication.isPlaying) { Stop(); return; }
        GameObject player = Game.Player;
        if (player == null) return;
        var state = player.GetComponent<VehicleStateModern>();
        Collider[] playerColliders = Solid(player);

        if (state.lastTag != _lastTag)
        {
            _lastTag = state.lastTag;
            _tags++;
            Line($"player tagged (score {state.score}, isIt {state.isIt})");
        }

        LogAimer(player);

        foreach (RocketModern r in Object.FindObjectsByType<RocketModern>(FindObjectsSortMode.None))
        {
            if (r.launchVehicle == null || !_seen.Add(r.GetInstanceID())) continue;
            bool bot = r.launchVehicle.isBot;
            if (!bot && r.targetVehicle == null) continue;
            _shots.Add(new Shot { rocket = r, bot = bot ? r.launchVehicle.name : null, snipe = r.speedX > 1, fired = Time.time, last = r.transform.position });
            if (bot) _fired++; else _playerShots++;
        }

        _ended.Clear();
        foreach (Shot s in _shots)
        {
            if (s.rocket == null) { _ended.Add(s); continue; }
            s.last = s.rocket.transform.position;
            if (s.bot == null) { TrackPlayerShot(s); continue; }
            s.closest = Mathf.Min(s.closest, Distance(playerColliders, s.last));
        }
        if (_ended.Count == 0) return;

        // RocketModern spawns the sparks at the hit point in the frame it ends.
        var sparks = Object.FindObjectsByType<PlasmaSparksModern>(FindObjectsSortMode.None)
            .Where(p => !_seen.Contains(p.GetInstanceID())).ToList();
        foreach (Shot s in _ended)
        {
            _shots.Remove(s);
            if (s.bot == null) { EndPlayerShot(s, sparks); continue; }
            PlasmaSparksModern spark = sparks.OrderBy(p => (p.transform.position - s.last).sqrMagnitude).FirstOrDefault();
            string kind = s.snipe ? "snipe" : "auto";
            if (spark == null || (spark.transform.position - s.last).sqrMagnitude > 400f)
            {
                _noSparks++;
                Line($"{s.bot} {kind}: ended after {Time.time - s.fired:F2} s with no sparks, closest {s.closest:F1} m");
                continue;
            }
            sparks.Remove(spark);
            _seen.Add(spark.GetInstanceID());
            Vector3 at = spark.transform.position;
            float toPlayer = Distance(playerColliders, at);
            string what = What(at, player);
            if (toPlayer < 0.5f) _onPlayer++;
            else if (what.Contains("Floor") || what.Contains("Plane")) _onFloor++;
            else _elsewhere++;
            Vector3 flat = at - player.transform.position;
            flat.y = 0f;
            Line($"{s.bot} {kind}: hit {what} {toPlayer:F1} m from the player ({flat.magnitude:F1} m across, {at.y - player.transform.position.y:F1} m up), closest pass {s.closest:F1} m, flight {Time.time - s.fired:F2} s");
        }
    }

    // RocketModern holds a lock-on bolt at the muzzle for 5 physics steps, then
    // gives it its velocity: that moment is the launch, and the angle between
    // the velocity and the line to the target is the lead.
    private static void TrackPlayerShot(Shot s)
    {
        VehicleStateModern target = s.rocket.targetVehicle;
        if (target == null) return;
        s.closest = Mathf.Min(s.closest, Distance(Solid(target.gameObject), s.last));
        if (s.launched >= 0f) return;
        Vector3 v = s.rocket.GetComponent<Rigidbody>().linearVelocity;
        if (v.sqrMagnitude < 1f) return;
        s.launched = Time.time;
        s.lead = Vector3.Angle(v, target.transform.position - s.last);
        _leadSum += s.lead;
        _leadCount++;
        Line($"player lock-on {(s.snipe ? "snipe" : "auto")} at {target.name}: launched {s.launched - s.fired:F2} s after firing, {Vector3.Distance(s.last, target.transform.position):F1} m away, target {target.GetComponent<Rigidbody>().linearVelocity.magnitude:F1} m/s, lead {s.lead:F2} deg");
    }

    private static void EndPlayerShot(Shot s, List<PlasmaSparksModern> sparks)
    {
        PlasmaSparksModern spark = sparks.OrderBy(p => (p.transform.position - s.last).sqrMagnitude).FirstOrDefault();
        bool hit = s.closest < 0.5f;
        if (spark != null) { sparks.Remove(spark); _seen.Add(spark.GetInstanceID()); }
        if (hit && s.launched >= 0f) { _playerHits++; _flightSum += Time.time - s.launched; }
        float flight = s.launched >= 0f ? Time.time - s.launched : 0f;
        Line($"player lock-on {(s.snipe ? "snipe" : "auto")}: {(hit ? "hit" : "missed")}, closest {s.closest:F1} m, launch to end {flight:F2} s");
    }

    private static Collider[] Solid(GameObject go) =>
        go.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();

    private static void LogAimer(GameObject player)
    {
        var combat = player.GetComponent<CombatSystemModern>();
        var sight = player.GetComponentInChildren<LegacyLaserSightController>(true);
        bool locked = combat != null && combat.laserLock != null;
        if (locked == _wasLocked) return;
        _wasLocked = locked;
        if (locked)
        {
            _lockedSince = Time.time;
            Line($"lock on {combat.laserLock.name}: locked aimer active={Active(sight != null ? sight.laserAimerLocked : null)} particles={Particles(sight != null ? sight.laserAimerLocked : null)}, normal aimer active={Active(sight != null ? sight.laserAimer : null)}");
        }
        else
        {
            Line($"lock released after {Time.time - _lockedSince:F2} s: normal aimer active={Active(sight != null ? sight.laserAimer : null)}");
        }
    }

    private static string Active(GameObject go) => go == null ? "missing" : go.activeInHierarchy.ToString();

    private static int Particles(GameObject go) =>
        go == null ? 0 : go.GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.particleCount);

    private static float Distance(Collider[] colliders, Vector3 point)
    {
        float best = float.MaxValue;
        foreach (Collider c in colliders) best = Mathf.Min(best, (c.ClosestPoint(point) - point).magnitude);
        return best;
    }

    private static string What(Vector3 point, GameObject player)
    {
        Collider best = null;
        float bestDistance = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(point, 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            float d = (c.ClosestPoint(point) - point).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = c; }
        }
        if (best == null) return "nothing within 1 m";
        return best.transform.root == player.transform ? "the player" : best.transform.root.name + "/" + best.name;
    }
}
