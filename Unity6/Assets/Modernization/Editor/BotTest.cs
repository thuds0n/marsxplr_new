using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Bot controls and a Play-mode check of VehicleBot.js. The original's Add Bot
// and Axe Bot buttons are in the Settings window, which is not ported yet, so
// the menu stands in for them. The test adds a bot, checks that it chases and
// fires at the quarry (the player), that its tags move the quarry, and that
// axing it leaves the player as quarry. Logs to Unity6/Logs/bot-test.txt.
public static class BotTest
{
    private static readonly Stack<IEnumerator> _routine = new Stack<IEnumerator>();
    private static float _waitUntil;
    private static readonly StringBuilder Log = new StringBuilder();
    private static int _failures;

    [MenuItem("Tools/MarsXPLR/Add Bot")]
    private static void AddBot()
    {
        if (EditorApplication.isPlaying && Game.Controller != null) Game.Controller.addBot();
    }

    [MenuItem("Tools/MarsXPLR/Axe Bot")]
    private static void AxeBot()
    {
        if (EditorApplication.isPlaying && Game.Controller != null) Game.Controller.axeBot();
    }

    [MenuItem("Tools/MarsXPLR/Run Bot Test")]
    private static void Run()
    {
        if (!EditorApplication.isPlaying || Game.Player == null)
        {
            Debug.LogWarning("Bot test: enter Play mode and wait for the buggy to spawn.");
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
        string path = Path.Combine(Application.dataPath, "../Logs/bot-test.txt");
        File.WriteAllText(path, Log.ToString());
        Debug.Log("Bot test finished:\n" + Log);
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
        var playerState = player.GetComponent<VehicleStateModern>();
        Line($"Unity {Application.unityVersion}, {SystemInfo.operatingSystem}; botsCanDrive {Game.Settings.botsCanDrive}, botsCanFire {Game.Settings.botsCanFire}, player isIt {playerState.isIt}");

        int before = Game.Controller.botsInGame;
        Game.Controller.addBot();
        string botName = player.name + "'s bot" + (before == 0 ? "" : " " + (before + 1));
        yield return 1.5f;
        Check(GameObject.Find(botName) == null, "no bot before the 2 s delay");
        yield return 1f;
        GameObject bot = GameObject.Find(botName);
        Check(bot != null, $"bot \"{botName}\" spawned after 2 s");
        if (bot == null) yield break;
        var botState = bot.GetComponent<VehicleStateModern>();
        Check(botState.isBot && bot.GetComponent<VehicleBotModern>() != null && bot.GetComponent<PlayerVehicleInputModern>() == null && bot.transform.Find("LaserAim") == null,
            "bot is driven by VehicleBotModern, not the keyboard, and has no aimer");
        Check(bot.transform.Find("LaserLock") != null, "bot carries a LaserLock sphere");

        // The player is quarry, so the bot pursues and fires lock-on shots.
        float startDistance = Vector3.Distance(bot.transform.position, player.transform.position);
        int botShots = 0;
        var seen = new HashSet<RocketModern>();
        float t0 = Time.time;
        float closest = startDistance;
        while (Time.time - t0 < 20f && playerState.isIt == 1)
        {
            foreach (RocketModern r in Object.FindObjectsByType<RocketModern>(FindObjectsSortMode.None))
                if (r.launchVehicle == botState && r.targetVehicle == playerState && seen.Add(r)) botShots++;
            closest = Mathf.Min(closest, Vector3.Distance(bot.transform.position, player.transform.position));
            yield return 0f;
        }
        Line($"bot input {botState.input}, distance {startDistance:F1} m -> closest {closest:F1} m, shots {botShots}");
        Check(botShots > 0, $"bot fired {botShots} lock-on shots at the quarry");
        Check(botState.isIt == 1 && playerState.isIt == 0, $"bot tagged the quarry and became quarry after {Time.time - t0:F1} s");

        // Now the bot hides: random steering at full throttle.
        yield return 1f;
        Check(botState.input.y == 1f, $"quarry bot drives on (input {botState.input})");

        Game.Controller.axeBot();
        yield return 0.5f;
        Check(GameObject.Find(botName) == null, "axed bot is removed");
        Check(playerState.isIt == 1, "player is quarry again after the bot is axed");
        Line("done");
    }
}
