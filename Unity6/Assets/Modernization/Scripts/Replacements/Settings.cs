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
    // Settings.js getPrefs(): loaded from the preferences. Hints are only
    // shown as the Game Settings toggle (the Settings Advisor panel is not
    // ported).
    public bool showHints = true;
    public int useMusic = 1;
    public bool useSfx = true;
    public bool superCam = true;
    // Settings.js gameMusic and musicTracks, as in the 2.22 build's level2:
    // the Camera's disabled, looping AudioSource (volume 0.5) and Classic,
    // Ambient and Carefree.
    public AudioSource gameMusic;
    public AudioClip[] musicTracks;
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
    // Settings.js resetTime starts at 0. Not serialised: the Game scene still
    // holds the port's earlier -10.
    [System.NonSerialized] public float resetTime;

    public bool enteredfullscreen;
    // Settings.js: true once the player picks a colour; otherwise each game
    // starts with a random one (Game.js).
    public bool colorCustom;
    // Settings.js colorUpdateTime: Game saves the colour 3 s after the last
    // slider change.
    [System.NonSerialized] public float colorUpdateTime;

    // Settings.js updateVehicleColor().
    public void updateVehicleColor(VehicleStateModern vehicle)
    {
        colorUpdateTime = Time.time + 3f;
        colorCustom = true;
        var colour = vehicle.GetComponent<VehicleColourModern>();
        if (colour != null) colour.setColor();
    }

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
        // Settings.js: the frame rate is capped at 100 unless the player set
        // another limit.
        Application.targetFrameRate = (int)PlayerPrefs.GetFloat("targetFrameRate", 100f);
        showHints = PlayerPrefs.GetInt("showHints", 1) != 0;
        useMusic = PlayerPrefs.GetInt("useMusic", 1);
        useSfx = PlayerPrefs.GetInt("useSfx", 1) != 0;
        useHypersound = PlayerPrefs.GetInt("useHypersound", 0);
        useMinimap = PlayerPrefs.GetInt("useMinimap", 1) != 0;
        superCam = PlayerPrefs.GetInt("superCam", 1) != 0;
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

        // Settings.js: music off, or the chosen track at normal pitch.
        if (gameMusic != null)
        {
            if (useMusic == 0)
            {
                gameMusic.enabled = false;
                if (gameMusic.isPlaying) gameMusic.Stop();
            }
            else if (musicTracks != null && useMusic <= musicTracks.Length)
            {
                gameMusic.enabled = true;
                gameMusic.pitch = 1f;
                // A new track is stopped into explicitly so that it always
                // starts from the beginning, as the original's Play() did.
                AudioClip track = musicTracks[useMusic - 1];
                if (gameMusic.clip != track)
                {
                    gameMusic.Stop();
                    gameMusic.clip = track;
                }
                if (!gameMusic.isPlaying) gameMusic.Play();
            }
        }

        Physics.gravity = new Vector3(0f, worldGrav, 0f);
        if (World.sea != null)
        {
            Vector3 seaPos = World.sea.position;
            seaPos.y = lavaAlt;
            World.sea.position = seaPos;
        }

        updateObjects();

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

    // Settings.js updateObjects(): particles emit only from render level 3,
    // every vehicle's spot light (Vehicle.prefab's VehicleLight) is switched
    // off, then every object hears OnPrefsUpdated.
    public void updateObjects()
    {
        foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            var emission = ps.emission;
            emission.enabled = renderLevel >= 3;
        }

        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.name != "VehicleLight") continue;
            light.enabled = false;
        }

        // Vehicle.js OnPrefsUpdated(): the laser aimers keep emitting.
        foreach (var sight in FindObjectsByType<LegacyLaserSightController>(FindObjectsSortMode.None))
        {
            foreach (GameObject aimer in new[] { sight.laserAimer, sight.laserAimerLocked })
            {
                if (aimer == null) continue;
                foreach (var ps in aimer.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var emission = ps.emission;
                    emission.enabled = true;
                }
            }
        }

        foreach (var orb in FindObjectsByType<VehicleRamoSphereModern>(FindObjectsSortMode.None)) orb.OnPrefsUpdated();

        // VehicleMe.js OnPrefsUpdated(): at Fantastic the player's own vehicle
        // light comes back on.
        if (renderLevel > 4 && Game.Player != null)
        {
            var playerLight = Game.Player.GetComponentInChildren<Light>();
            if (playerLight != null) playerLight.enabled = true;
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
            if (ramoSpheres != 0f && GUILayout.Toggle(lasersOptHit, "L Hit ORBs") != lasersOptHit)
            {
                lasersOptHit = !lasersOptHit;
                updateServerPrefs();
            }
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

    // SoundEffect.js Awake(): with sound effects off, a laser's or its sparks'
    // AudioSource is removed as it spawns (stopped first, as Destroy waits
    // until the end of the frame).
    public static void SoundEffect(GameObject go)
    {
        if (Game.Settings == null || Game.Settings.useSfx) return;
        var audio = go.GetComponent<AudioSource>();
        if (audio == null) return;
        audio.Stop();
        Destroy(audio);
    }

    // Settings.js showDialogGame().
    public void showDialogGame()
    {
        GUILayout.Label("Resolution:");
        // Game.cs handles the 0 key the same way: fullscreen at the current
        // resolution rather than Settings.js toggleFullscreen()'s largest one.
        if (GUILayout.Button((!Screen.fullScreen ? "Enter" : "Exit") + " Fullscreen (0)")) Screen.fullScreen = !Screen.fullScreen;
        if (Screen.fullScreen || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            // Settings.js steps to the next smaller or larger of the display's
            // resolutions by width (Screen.resolutions is smallest first). The
            // Unity 6 macOS player already lists them at point size.
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions.Length > 0)
            {
                GUILayout.BeginHorizontal();
                if ((resolutions[0].width < Screen.width || resolutions[0].height < Screen.height) && GUILayout.Button("<<", GUILayout.Width(28)))
                {
                    for (int i = resolutions.Length - 1; i >= 0; i--)
                    {
                        if (resolutions[i].width >= Screen.width) continue;
                        Screen.SetResolution(resolutions[i].width, resolutions[i].height, Screen.fullScreen);
                        break;
                    }
                }
                GUILayout.Label(Screen.width + "X" + Screen.height);
                Resolution largest = resolutions[resolutions.Length - 1];
                if ((largest.width > Screen.width || largest.height > Screen.height) && GUILayout.Button(">>", GUILayout.Width(28)))
                {
                    foreach (Resolution res in resolutions)
                    {
                        if (res.width <= Screen.width) continue;
                        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
                        break;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.Space(20);
        GUILayout.Label("Rendering Quality:");

        GUILayout.BeginHorizontal();
        if (renderLevel == 1) GUILayout.Label("Fastest");
        else if (renderLevel == 2) GUILayout.Label("Fast");
        else if (renderLevel == 3) GUILayout.Label("Simple");
        else if (renderLevel == 4) GUILayout.Label("Good");
        else if (renderLevel == 5) GUILayout.Label("Beautiful");
        else if (renderLevel == 6) GUILayout.Label("Fantastic");
        GUILayout.Label("(" + Game.Controller.fps.ToString("f0") + " FPS)");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (renderLevel > 1)
        {
            if (GUILayout.Button("<<"))
            {
                renderLevel--;
                PlayerPrefs.SetInt("renderLevel", renderLevel);
                updatePrefs();
            }
        }
        if (renderLevel > 1 && renderLevel < 6) GUILayout.Space(5);
        if (renderLevel < 6)
        {
            if (GUILayout.Button(">>"))
            {
                renderLevel++;
                PlayerPrefs.SetInt("renderLevel", renderLevel);
                updatePrefs();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Visibility Cap:   (" + (renderViewCap == 1000f ? "MAX" : Mathf.Floor(renderViewCap).ToString()) + ")");
        float cg = GUILayout.HorizontalSlider(renderViewCap, 200f, 1000f);
        if (renderViewCap != cg)
        {
            renderViewCap = cg;
            PlayerPrefs.SetFloat("viewCap", cg);
            updatePrefs();
        }

        GUILayout.Space(5);
        GUILayout.Label("FPS Cap:   (" + (Application.targetFrameRate == -1 ? "MAX" : Application.targetFrameRate.ToString()) + ")");
        // Settings.js: -1 (no cap) shows as the slider's 100 end, and 100 or 0
        // goes back to -1. The preference keeps the slider's unrounded value.
        if (Application.targetFrameRate == -1) Application.targetFrameRate = 100;
        cg = GUILayout.HorizontalSlider(Application.targetFrameRate, 10f, 100f);
        if (Application.targetFrameRate != cg)
        {
            Application.targetFrameRate = (int)cg;
            PlayerPrefs.SetFloat("targetFrameRate", cg);
        }
        if (Application.targetFrameRate == 0 || Application.targetFrameRate == 100) Application.targetFrameRate = -1;

        GUILayout.FlexibleSpace();
        GUILayout.Space(20);
        GUILayout.Label("Interface:");

        if (minimapAllowed && GUILayout.Toggle(useMinimap, "Enable Minimap") != useMinimap)
        {
            useMinimap = !useMinimap;
            PlayerPrefs.SetInt("useMinimap", useMinimap ? 1 : 0);
            updatePrefs();
        }

        // Saved and loaded; the Settings Advisor panel it drives is not ported.
        if (GUILayout.Toggle(showHints, "Enable Settings Advisor") != showHints)
        {
            showHints = !showHints;
            PlayerPrefs.SetInt("showHints", showHints ? 1 : 0);
            updatePrefs();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Space(20);
        GUILayout.Label("Audio:");

        // Settings.js: applies to sounds that start after the change.
        if (GUILayout.Toggle(useSfx, "Sound Effects Enabled") != useSfx)
        {
            useSfx = !useSfx;
            PlayerPrefs.SetInt("useSfx", useSfx ? 1 : 0);
        }

        MusicToggle(0, "No Music");
        MusicToggle(1, "Classic");
        MusicToggle(2, "Ambient");
        MusicToggle(3, "Carefree");

        if (GUILayout.Toggle(useHypersound == 1, "HyperSound") != (useHypersound == 1))
        {
            useHypersound = useHypersound == 1 ? 0 : 1;
            PlayerPrefs.SetInt("useHypersound", useHypersound);
            updatePrefs();
        }
    }

    // Settings.js showDialogPlayer(). Left out: the XORB button (XORBs are
    // not ported; /x answers "XORBs Unavailable").
    public void showDialogPlayer()
    {
        VehicleStateModern veh = Game.Player != null ? Game.Player.GetComponent<VehicleStateModern>() : null;
        if (veh == null) return;

        GUILayout.Space(20);

        if (resetTime > 0f) GUILayout.Label("Reset In " + (10f - (Time.time - resetTime)));
        else if (resetTime > -1f && GUILayout.Button("Reset My Position (/r)"))
        {
            // Same as the /r command (Messaging.SubmitInput); Game.Update
            // finishes the reset.
            resetTime = Time.time;
            var rb = Game.Player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Game.Controller.msg(Game.Player.name + " Resetting in 10 seconds...", Messaging.Server);
        }

        GUILayout.Space(20);
        GUILayout.Label("Camera Mode:");

        CamModeToggle(0, "Ride (1, alt)");
        CamModeToggle(1, "Chase (2)");
        CamModeToggle(2, "Soar(5)");
        CamModeToggle(3, "Spectate(6)");
        CamModeToggle(4, "Roam");

        if (GUILayout.Toggle(gyroCam, "GyRide Enabled (7)") != gyroCam)
        {
            gyroCam = !gyroCam;
            PlayerPrefs.SetInt("gyroCam", gyroCam ? 1 : 0);
        }

        if (GUILayout.Toggle(flightCam, "HyperCam Enabled") != flightCam)
        {
            flightCam = !flightCam;
            PlayerPrefs.SetInt("flightCam", flightCam ? 1 : 0);
        }

        if (camMode == 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("(Press (2) or (esc) keys to unlock your cursor)");
            GUILayout.Space(10);
        }
        else if (camMode == 1)
        {
            GUILayout.Space(5);
            GUILayout.Label("Chase Strategy:");
            CamChaseToggle(0, "Smooth");
            CamChaseToggle(1, "Agile");
            CamChaseToggle(2, "Arcade");
            GUILayout.Space(5);
            GUILayout.Label("Chase Distance: (3-4)");
            float dist = GUILayout.HorizontalSlider(camDist, 0f, 20f);
            if (camDist != dist)
            {
                camDist = dist;
                PlayerPrefs.SetFloat("camDist", dist);
            }
        }
        else if (camMode == 3 || camMode == 4)
        {
            GUILayout.Space(10);
            GUILayout.Label("(Move camera with UIOJKL keys)");
            GUILayout.Space(10);
        }

        GUILayout.FlexibleSpace();
        GUILayout.FlexibleSpace();
        GUILayout.Space(20);
        GUILayout.Label("Vehicle Color:");
        if (veh.isIt != 0 && FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None).Length > 1) GUILayout.Label("(You are quarry, and therefore green)");

        if (GUILayout.Button(colorCustom ? "Random Coloring" : "Randomize Coloring"))
        {
            randomizeVehicleColor(veh);
            var colour = veh.GetComponent<VehicleColourModern>();
            if (colour != null) colour.setColor();
        }

        // Settings.js: each body slider also resets the accent from the body.
        // Green is capped by the red and blue, read after the red row.
        Color c = veh.vehicleColor;
        if (ColourSlider(ref c.r, c.r, 1f, "Red")) BodyColourChanged(veh, c);
        c = veh.vehicleColor;
        float greenMax = Mathf.Lerp(0.1f, 0.8f, (c.r + c.b) / 2f);
        if (ColourSlider(ref c.g, Mathf.Min(greenMax, c.g), greenMax, "Green")) BodyColourChanged(veh, c);
        c = veh.vehicleColor;
        if (ColourSlider(ref c.b, c.b, 1f, "Blue")) BodyColourChanged(veh, c);

        GUILayout.Space(5);
        GUILayout.Label("Accent Color:");

        Color a = veh.vehicleAccent;
        bool accentChanged = ColourSlider(ref a.r, a.r, 1f, "Red");
        accentChanged |= ColourSlider(ref a.g, a.g, 1f, "Green");
        accentChanged |= ColourSlider(ref a.b, a.b, 1f, "Blue");
        if (accentChanged)
        {
            veh.vehicleAccent = a;
            updateVehicleColor(veh);
        }

        GUILayout.FlexibleSpace();
    }

    // Settings.js showDialogPlayers(), the active players list. Left out: the
    // joining players, Evict and Ban buttons and banned list (online only),
    // and each player's network text box (ping, jitter, IP address and view
    // ID), which has nothing to show offline. The original panel shows no
    // scores.
    public void showDialogPlayers()
    {
        GUILayout.FlexibleSpace();

        var vehicles = FindObjectsByType<VehicleStateModern>(FindObjectsSortMode.None);
        // Game.Players is a Hashtable, so the original order is arbitrary; the
        // port lists by name.
        System.Array.Sort(vehicles, (x, y) => string.CompareOrdinal(x.name, y.name));
        foreach (var veh in vehicles)
        {
            GUILayout.Space(10);
            GUILayout.Label(veh.name + (veh.gameObject == Game.Player ? " (Me)" : ""));
            GUILayout.FlexibleSpace();
        }
    }

    private void MusicToggle(int music, string label)
    {
        if (GUILayout.Toggle(useMusic == music, label) != (useMusic == music))
        {
            useMusic = music;
            PlayerPrefs.SetInt("useMusic", music);
            updatePrefs();
        }
    }

    private void BodyColourChanged(VehicleStateModern veh, Color c)
    {
        veh.vehicleColor = c;
        updateVehicleAccent(veh);
        updateVehicleColor(veh);
    }

    private void CamModeToggle(int mode, string label)
    {
        if (GUILayout.Toggle(camMode == mode, label) != (camMode == mode))
        {
            camMode = mode;
            PlayerPrefs.SetInt("cam", mode);
        }
    }

    private void CamChaseToggle(int chase, string label)
    {
        if (GUILayout.Toggle(camChase == chase, label) != (camChase == chase))
        {
            camChase = chase;
            PlayerPrefs.SetInt("camChase", chase);
        }
    }

    // One Settings.js colour row: the slider starts at shown and returns true
    // when its value differs from value, which it then overwrites.
    private static bool ColourSlider(ref float value, float shown, float max, string label)
    {
        GUILayout.BeginHorizontal();
        float cg = GUILayout.HorizontalSlider(shown, 0f, max);
        bool changed = value != cg;
        if (changed) value = cg;
        GUILayout.Label(label, GUILayout.Width(65));
        GUILayout.EndHorizontal();
        return changed;
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
