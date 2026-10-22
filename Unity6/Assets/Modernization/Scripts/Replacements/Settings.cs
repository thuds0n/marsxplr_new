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
    // Settings.js laser defaults.
    public float laserSpeed = 180f;
    public float laserGrav;
    public float laserRico;
    public bool lasersFatal;
    public bool lasersOptHit;
    public float zorbSpeed = 7f;
    public bool zorbEnabled;
    public int[] firepower = { 2, 2, 3, 2 };
    // Settings.js bot defaults.
    public bool botsCanFire = true;
    public bool botsCanDrive = true;
    public float buggyPower = 1f;
    public float buggySpeed = 30f;
    public float buggyFlightDrag = 300f;
    public float buggyFlightAgility = 1f;
    // Settings.js "Stall Blending" and "Powered Loops", both off by default.
    public bool buggyFlightSlip;
    public bool buggyFlightLooPower;
    // Settings.js defaults.
    public bool buggySmartSuspension = true;
    public float buggyCG = -0.4f;
    public float buggyTr = 1f;
    public float buggySh = 70f;
    public float lavaAlt = -300f;
    public float lavaFog = 0.005f;
    public float worldFog = 0.0004f;
    // Settings.js: the player's visibility cap (1000 means no cap) and the
    // world's view distance, which together set the camera's far clip.
    public float renderViewCap = 1000f;
    public float worldViewDist = 5000f;
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

    // Settings.js Start: load saved preferences, then apply them.
    private void Start()
    {
        UsePointResolution();
        getPrefs();
        updatePrefs();
    }

    // The IMGUI HUD is laid out in pixels. Unity 2.5 (and the Wine version)
    // saw a Retina display at its size in points, but the Unity 6 player picks
    // the full pixel size, which draws the HUD at half its original size.
    // macOS reports no backing scale to the player, so displays above 150 dpi
    // are taken as Retina (2x) and the resolution halved to match.
    private static void UsePointResolution()
    {
        if (Application.platform != RuntimePlatform.OSXPlayer || Screen.dpi < 150f) return;
        // Already at point size (Unity restores the last resolution on launch).
        if (Screen.width <= Screen.currentResolution.width * 0.75f) return;
        int width = Screen.width / 2, height = Screen.height / 2;
        if (width < 640 || height < 480) return;
        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }

    // Settings.js getPrefs(), the rendering and camera preferences only.
    public void getPrefs()
    {
        renderLevel = PlayerPrefs.GetInt("renderLevel", 4);
        renderViewCap = PlayerPrefs.GetFloat("viewCap", 1000f);
        camMode = PlayerPrefs.GetInt("cam", 1);
        camChase = PlayerPrefs.GetInt("camChase", 1);
        camDist = PlayerPrefs.GetFloat("camDist", 0.01f);
        flightCam = PlayerPrefs.GetInt("flightCam", 0) == 1;
        gyroCam = PlayerPrefs.GetInt("gyroCam", 0) == 1;
    }

    public void updatePrefs()
    {
        // Settings.js: QualitySettings.currentLevel = renderLevel - 1, so render
        // levels 1-6 select Fastest to Fantastic, including their shadows (none
        // below Beautiful) and pixel light counts.
        QualitySettings.SetQualityLevel(Mathf.Clamp(renderLevel - 1, 0, QualitySettings.names.Length - 1), true);

        // Settings.js ran physics at 50 Hz, or 40 Hz at lower render levels.
        Time.fixedDeltaTime = renderLevel > 3 ? 0.02f : 0.025f;

        // Beyond 2000 m CameraVehicle clears to the skybox instead of the fog colour.
        if (Camera.main != null)
        {
            Camera.main.farClipPlane = renderViewCap == 1000f ? worldViewDist : Mathf.Min(renderViewCap, worldViewDist);
            worldFog = Mathf.Lerp(0.007f, 0.0003f, Camera.main.farClipPlane / 6000f);

            // Settings.js rendering effects: contrast stretch at Beautiful and
            // Fantastic, plus SSAO (downsampling 2) at Fantastic.
            var contrast = Camera.main.GetComponent<ContrastStretchEffect>();
            if (contrast != null) contrast.enabled = renderLevel > 4;
            var ssao = Camera.main.GetComponent<SSAOEffect>();
            if (ssao != null)
            {
                ssao.enabled = renderLevel > 5;
                if (ssao.enabled) ssao.m_Downsampling = 2;
            }
        }

        if (renderLevel > 4) World.lodDist = 1000;
        else if (renderLevel > 3) World.lodDist = 400;
        else if (renderLevel > 2) World.lodDist = 75;
        else World.lodDist = 0;

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
