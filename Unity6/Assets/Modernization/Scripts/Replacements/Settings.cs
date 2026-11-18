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
    // Settings.js ramoSpheres: RORB size, 0 when off.
    public float ramoSpheres;
    public float zorbSpeed = 7f;
    // Settings.js hideNames ("Camouflage Badges"). Name tags are not ported
    // yet, so nothing reads it.
    public bool hideNames;
    // Settings.js serverWelcome.
    public string serverWelcome = "";
    public int[] firepower = { 2, 2, 3, 2 };
    // Settings.js world default.
    public float worldGrav = -9.81f;
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

        // Settings.js: XORBs are unavailable without RORBs.
        if (ramoSpheres == 0f) zorbSpeed = 0f;

        Physics.gravity = new Vector3(0f, worldGrav, 0f);
        if (World.sea != null)
        {
            Vector3 seaPos = World.sea.position;
            seaPos.y = lavaAlt;
            World.sea.position = seaPos;
        }

        // Settings.js switched off every vehicle's spot light (Vehicle.prefab's
        // VehicleLight) whenever the preferences were applied.
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.name != "VehicleLight") continue;
            light.enabled = false;
        }

        // VehicleMe.js OnPrefsUpdated(): at Fantastic the player's own vehicle
        // light comes back on.
        if (renderLevel > 4 && Game.Player != null)
        {
            var playerLight = Game.Player.GetComponentInChildren<Light>();
            if (playerLight != null) playerLight.enabled = true;
        }

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

    // Game.js sSS(): apply a packed server-settings string such as a world's
    // "marsxplr" parameter ("camo:1;lsrh:1;"). Only the settings the port has
    // are read; the rest (other vehicles, networking) are skipped.
    public void applyServerString(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        foreach (string pref in str.Split(';'))
        {
            string[] val = pref.Split(':');
            if (val.Length < 2) continue;
            string v = val[1];
            float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f);
            switch (val[0])
            {
                case "lasr": lasersAllowed = v == "1"; break;
                case "lsrh": lasersFatal = v == "1"; break;
                case "lsro": lasersOptHit = v == "1"; break;
                case "mmap": minimapAllowed = v == "1"; break;
                case "camo": hideNames = v == "1"; break;
                case "rorb": ramoSpheres = f; break;
                case "xspd": zorbSpeed = f; break;
                case "grav": worldGrav = f * -1f; break;
                case "wvis": worldViewDist = f; break;
                case "lfog": lavaFog = f; break;
                case "lalt": lavaAlt = f; break;
                case "lspd": laserSpeed = f; break;
                case "lgvt": laserGrav = Mathf.Clamp(f, 0f, 1f); break;
                case "lrco": laserRico = Mathf.Clamp(f, 0f, 1f); break;
                case "botfire": botsCanFire = v == "1"; break;
                case "botdrive": botsCanDrive = v == "1"; break;
                case "bugflsl": buggyFlightSlip = v == "1"; break;
                case "bugflpw": buggyFlightLooPower = v == "1"; break;
                case "bugspn": buggySmartSuspension = v == "1"; break;
                case "bugfldr": buggyFlightDrag = Mathf.Clamp(f, 1f, 1000f); break;
                case "bugflag": buggyFlightAgility = Mathf.Clamp(f, 0.5f, 1.5f); break;
                case "bugcg": buggyCG = Mathf.Clamp(f, -1f, 0f); break;
                case "bugpow": buggyPower = Mathf.Clamp(f, 0.1f, 3f); break;
                case "bugspd": buggySpeed = Mathf.Clamp(f, 1f, 1000f); break;
                case "bugtr": buggyTr = Mathf.Clamp(f, 0.1f, 3f); break;
                case "bugsh": buggySh = Mathf.Clamp(f, 0f, 140f); break;
                case "bugfp": if (firepower != null && firepower.Length > 0) firepower[0] = Mathf.Clamp((int)f, 0, 3); break;
                case "bugll": if (laserLock != null && laserLock.Length > 0) laserLock[0] = Mathf.Clamp(f, 0f, 1f); break;
            }
        }
        updatePrefs();
    }

    // Settings.js updateServerPrefs(): offline there is no server to update,
    // so only the local settings are applied.
    public void updateServerPrefs()
    {
        updatePrefs();
    }

    // Settings.js showDialogServer(), the host branch, limited to what the
    // single-player port honours. Left out: the online settings (Default All,
    // server name, listing, password, welcome message, networking, settings
    // I/O and nTime), Camouflage Badges (name tags are not ported), XORBs, and the hovercraft, tank and jet sections.
    public void showDialogServer()
    {
        GUILayout.Space(20);
        GUILayout.Label("Game Features:");

        if (GUILayout.Toggle(minimapAllowed, "Minimap enabled") != minimapAllowed)
        {
            minimapAllowed = !minimapAllowed;
            updateServerPrefs();
        }

        if (GUILayout.Toggle(ramoSpheres != 0f, "RORBs Enabled") != (ramoSpheres != 0f))
        {
            ramoSpheres = ramoSpheres != 0f ? 0f : 0.5f;
            if (ramoSpheres != 0f) zorbSpeed = 7f;
            updateServerPrefs();
        }
        if (ramoSpheres != 0f)
        {
            GUILayout.BeginHorizontal();
            float size = GUILayout.HorizontalSlider(ramoSpheres, 0.001f, 1f);
            if (ramoSpheres != size)
            {
                ramoSpheres = size;
                updateServerPrefs();
            }
            GUILayout.Label("Size", GUILayout.Width(65));
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Toggle(lasersAllowed, "Lasers enabled") != lasersAllowed)
        {
            lasersAllowed = !lasersAllowed;
            updateServerPrefs();
        }

        if (lasersAllowed)
        {
            if (GUILayout.Toggle(lasersFatal, "L Hits Rematerialize") != lasersFatal)
            {
                lasersFatal = !lasersFatal;
                updateServerPrefs();
            }
            // laserSpeed is an int in Settings.js; the slider value truncates.
            GUILayout.BeginHorizontal();
            int lsr = (int)GUILayout.HorizontalSlider(laserSpeed, 20f, 340f);
            if (laserSpeed != lsr)
            {
                laserSpeed = lsr;
                updateServerPrefs();
            }
            GUILayout.Label("Lsr Spd", GUILayout.Width(65));
            GUILayout.EndHorizontal();
            Slider(ref laserGrav, 0f, 1f, "Lsr Gvt");
            Slider(ref laserRico, 0f, 1f, "Lsr Rco");
            GUILayout.Space(10);
        }

        Slider(ref worldGrav, -0.81f, -18.81f, "Gravity");
        Slider(ref worldViewDist, 300f, 9700f, "Visibility");

        if (World.sea != null)
        {
            Slider(ref lavaFog, 0.015f, -0.01f, "Sea Vis");
            Slider(ref lavaAlt, -100f, 100f, "Sea Alt");
        }

        GUILayout.Space(20);
        GUILayout.Label("Bots:");

        if (GUILayout.Toggle(botsCanFire, "Can Fire") != botsCanFire)
        {
            botsCanFire = !botsCanFire;
            updateServerPrefs();
        }

        if (GUILayout.Toggle(botsCanDrive, "Can Drive") != botsCanDrive)
        {
            botsCanDrive = !botsCanDrive;
            updateServerPrefs();
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (Game.Controller.botsInGame < 10)
        {
            if (GUILayout.Button("Add Bot")) Game.Controller.addBot();
        }
        if (Game.Controller.botsInGame > 0)
        {
            if (Game.Controller.botsInGame != 10) GUILayout.Space(5);
            if (GUILayout.Button("Axe Bot")) Game.Controller.axeBot();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.Label("Buggy:");

        if (GUILayout.Toggle(buggyFlightSlip, "Stall Blending") != buggyFlightSlip)
        {
            buggyFlightSlip = !buggyFlightSlip;
            updateServerPrefs();
        }
        if (GUILayout.Toggle(buggyFlightLooPower, "Powered Loops") != buggyFlightLooPower)
        {
            buggyFlightLooPower = !buggyFlightLooPower;
            updateServerPrefs();
        }
        if (GUILayout.Toggle(buggySmartSuspension, "Smart Suspension") != buggySmartSuspension)
        {
            buggySmartSuspension = !buggySmartSuspension;
            updateServerPrefs();
        }
        Slider(ref buggyFlightDrag, 50f, 550f, "Fl Speed");
        Slider(ref buggyFlightAgility, 0.5f, 1.5f, "Fl Agility");
        Slider(ref buggyCG, -0.1f, -0.7f, "Stability");
        Slider(ref buggyPower, 0.5f, 1.5f, "Power");
        Slider(ref buggySpeed, 5f, 55f, "Speed");
        Slider(ref buggyTr, 0.1f, 1.9f, "Traction");
        Slider(ref buggySh, 20f, 120f, "Shocks");

        if (lasersAllowed)
        {
            // firepower is an int[] in Settings.js; the slider value truncates.
            GUILayout.BeginHorizontal();
            int fp = (int)GUILayout.HorizontalSlider(firepower[0], 0f, 3f);
            if (firepower[0] != fp)
            {
                firepower[0] = fp;
                updateServerPrefs();
            }
            GUILayout.Label("Firepower", GUILayout.Width(65));
            GUILayout.EndHorizontal();

            Slider(ref laserLock[0], 0f, 1f, "Lsr Lck");
        }
    }

    // One Settings.js slider row: a horizontal slider and a 65 px label.
    private void Slider(ref float value, float left, float right, string label)
    {
        GUILayout.BeginHorizontal();
        float cg = GUILayout.HorizontalSlider(value, left, right);
        if (value != cg)
        {
            value = cg;
            updateServerPrefs();
        }
        GUILayout.Label(label, GUILayout.Width(65));
        GUILayout.EndHorizontal();
    }
}
