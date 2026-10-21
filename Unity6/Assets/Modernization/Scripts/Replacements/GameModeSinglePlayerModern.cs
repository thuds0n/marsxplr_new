using UnityEngine;

// Local quarry rules from Game.js and Vehicle.js: someone is always the
// quarry (eSI), quarry changes hands (sQ), and laser tags change the score
// (iS, dS). Ramming (OnRam) and fatal laser hits (lR) are not ported yet.
public class GameModeSinglePlayerModern : MonoBehaviour
{
    public static GameModeSinglePlayerModern Instance;

    // chatOrigins.Server in the original Messaging.
    private const int ServerOrigin = 2;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        EnsureSomeoneIsQuarry();
        UpdateQuarryDistance();
    }

    // Game.js eSI(): if nobody is quarry, the player defaults to it.
    private void EnsureSomeoneIsQuarry()
    {
        if (Game.Player == null) return;
        foreach (VehicleStateModern veh in FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None))
        {
            if (veh.isIt == 1) return;
        }
        var player = Game.Player.GetComponent<VehicleStateModern>();
        if (player != null) SetQuarry(player, 3);
    }

    // Vehicle.js sQ().
    public void SetQuarry(VehicleStateModern veh, int mode)
    {
        foreach (VehicleStateModern other in FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None))
        {
            other.isIt = 0;
        }
        veh.isIt = 1;
        Game.QuarryVeh = veh.gameObject;
        if (mode == 0) return;
        if (mode == 1) Game.Controller.msg(veh.name + " rammed the Quarry", ServerOrigin);
        else if (mode == 2) Game.Controller.msg(veh.name + " is now the Quarry", ServerOrigin);
        else if (mode == 3) Game.Controller.msg(veh.name + " Defaulted to Quarry", ServerOrigin);
        veh.lastTag = Time.time;
    }

    // Rocket.js: the firer's instance scores a laser that reaches another vehicle.
    public void OnVehicleLasered(VehicleStateModern launch, VehicleStateModern veh)
    {
        if (!veh.isResponding) return;
        if (launch.isIt == 1 && (Time.time - veh.lastTag) > 3f)
        {
            // We are quarry, and made a tag (iS).
            launch.score += 1;
            Game.Controller.msg(launch.name + " Got  " + veh.name, ServerOrigin);
            veh.lastTag = Time.time;
        }
        else if (veh.isIt == 1 && (Time.time - launch.lastTag) > 3f && (Time.time - veh.lastTag) > 3f)
        {
            // They were quarry, and we tagged them.
            SetQuarry(launch, 2);
        }
        else if (veh.isIt == 0 && launch.isIt == 0)
        {
            // They weren't quarry, and we weren't supposed to shoot them (dS).
            launch.score -= 1;
        }
    }

    // Vehicle.js: quarryDist is the distance from the quarry to the player.
    private void UpdateQuarryDistance()
    {
        if (Game.Player == null || Game.QuarryVeh == null)
        {
            Game.Controller.quarryDist = 0f;
            return;
        }
        Game.Controller.quarryDist = Vector3.Distance(Game.QuarryVeh.transform.position, Game.Player.transform.position);
    }
}
