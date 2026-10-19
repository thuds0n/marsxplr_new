using UnityEngine;

public class Settings : MonoBehaviour
{
    public bool simplified = true;
    public bool minimapAllowed = true;
    public bool useMinimap = true;
    public int camMode = 1;
    public int camChase = 1;
    public float camDist = 0.01f;
    public bool flightCam = false;
    public bool gyroCam;
    public int useHypersound;
    public AudioSource gameMusic;
    public bool lasersAllowed = true;
    public bool laserLocking = true;
    public float[] laserLock = { 0.4f, 0.4f, 0.4f, 0.4f };
    public float laserCooldown = 0.16f;
    public float laserRange = 1200f;
    public float laserImpactForce = 18f;
    public float laserProjectileSpeed = 240f; // Placeholder for future projectile-based path.
    public float laserProjectileGravity = 0f; // Placeholder for future projectile-based path.
    public float zorbSpeed = 7f;
    public bool zorbEnabled;
    public int[] firepower = { 2, 2, 3, 2 };
    public float buggyPower = 1f;
    public float buggySpeed = 30f;
    public float buggyFlightDrag = 300f;
    public float buggyFlightAgility = 1f;
    // Settings.js defaults.
    public bool buggySmartSuspension = true;
    public float buggyCG = -0.4f;
    public float buggyTr = 1f;
    public float buggySh = 70f;
    public bool autoSelectQuarry = true;
    public int quarryScore;
    public float lavaAlt = -300f;
    public float lavaFog = 0.005f;
    public float worldFog = 0.0004f;
    public Color fogColor = Color.clear;
    public int renderLevel = 4;
    public float resetTime = -10f;

    public bool enteredfullscreen;
    // Settings.js: true once the player picks a colour; otherwise each game
    // starts with a random one (Game.js).
    public bool colorCustom;

    // Settings.js ramdomizeVehicleColor().
    public void randomizeVehicleColor(VehicleStateModern vehicle)
    {
        Color c = vehicle.vehicleColor;
        c.r = Random.value * 0.7f;
        c.b = Random.value * 0.7f;
        c.g = Random.value * Mathf.Lerp(0.1f, 0.8f, (c.r + c.b) / 2f) * 0.7f;
        vehicle.vehicleColor = c;
        colorCustom = false;
        updateVehicleAccent(vehicle);
        saveVehicleColor(vehicle);
    }

    public void updateVehicleAccent(VehicleStateModern vehicle)
    {
        const float offset = -0.25f;
        Color c = vehicle.vehicleColor;
        Color a = vehicle.vehicleAccent;
        a.r = Mathf.Max(0f, c.r + offset);
        a.g = Mathf.Max(0f, c.g + offset);
        a.b = Mathf.Max(0f, c.b + offset);
        vehicle.vehicleAccent = a;
    }

    public void saveVehicleColor(VehicleStateModern vehicle)
    {
        PlayerPrefs.SetInt("vehColCustom", colorCustom ? 1 : 0);
        PlayerPrefs.SetFloat("vehColR", vehicle.vehicleColor.r);
        PlayerPrefs.SetFloat("vehColG", vehicle.vehicleColor.g);
        PlayerPrefs.SetFloat("vehColB", vehicle.vehicleColor.b);
        PlayerPrefs.SetFloat("vehColAccR", vehicle.vehicleAccent.r);
        PlayerPrefs.SetFloat("vehColAccG", vehicle.vehicleAccent.g);
        PlayerPrefs.SetFloat("vehColAccB", vehicle.vehicleAccent.b);
    }

    public void updatePrefs()
    {
        // Settings.js ran physics at 50 Hz, or 40 Hz at lower render levels.
        Time.fixedDeltaTime = renderLevel > 3 ? 0.02f : 0.025f;

        laserLocking = false;
        if (laserLock != null)
        {
            for (int i = 0; i < laserLock.Length; i++)
            {
                if (laserLock[i] > 0f)
                {
                    laserLocking = true;
                    break;
                }
            }
        }
    }
}
