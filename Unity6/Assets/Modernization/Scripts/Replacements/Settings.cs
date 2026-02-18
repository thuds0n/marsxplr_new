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
    public bool autoSelectQuarry = true;
    public int quarryScore;
    public float lavaAlt = -300f;
    public float lavaFog = 0.005f;
    public float worldFog = 0.0004f;
    public Color fogColor = Color.clear;
    public int renderLevel = 4;
    public float resetTime = -10f;

    public bool enteredfullscreen;

    public void updatePrefs()
    {
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
