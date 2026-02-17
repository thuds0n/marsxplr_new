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
    public float zorbSpeed = 7f;
    public bool zorbEnabled;
    public int[] firepower = { 2, 2, 3, 2 };
    public float lavaAlt = -300f;
    public float lavaFog = 0.005f;
    public float worldFog = 0.0004f;
    public Color fogColor = Color.clear;
    public int renderLevel = 4;
    public float resetTime = -10f;

    public bool enteredfullscreen;

    public void updatePrefs()
    {
        // Compatibility stub; tuned values can still be edited in inspector.
    }
}
