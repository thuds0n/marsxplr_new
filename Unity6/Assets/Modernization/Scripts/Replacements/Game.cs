using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Game : MonoBehaviour
{
    public static Game Controller;
    public static Settings Settings;
    public static Messaging Messaging;
    public static GameObject Player;
    public static GameObject QuarryVeh;

    public bool loadingWorld;
    public float worldLoadTime;
    public Texture2D cursor;
    public Texture2D cursorLook;
    public Vector2 cursorOffset = Vector2.zero; // kept for inspector compatibility; not used for centering.
    public GUIStyle hudTextStyle;
    // Game.js objectMarkerMe: the player's own minimap marker.
    public GameObject objectMarkerMe;
    // Game.js objectMarker and objectMarkerQuarry: other vehicles' minimap
    // markers, swapped while that vehicle is quarry.
    public GameObject objectMarker;
    public GameObject objectMarkerQuarry;
    // Game.js botsInGame.
    public int botsInGame;
    // Game.js Skin: the Mars GUI skin for the menus.
    public GUISkin Skin;

    // Game.js GUIPanel: one side panel of the menu at the top right.
    public class GUIPanel
    {
        public string name;
        public bool active = true;
        public bool open;
        public bool important;
        public int minHeight = 300;
        public int maxHeight;
        public float curHeight;
        public float desHeight;
        public Vector2 scrollPos;
        public float openTime;
    }

    // Game.js GUIPanels, with the values serialised in the original Game.unity.
    [System.NonSerialized]
    public GUIPanel[] GUIPanels =
    {
        new GUIPanel { name = "Server Settings", active = true, minHeight = 250 },
        new GUIPanel { name = "Game Settings", active = true, minHeight = 250 },
        new GUIPanel { name = "Player Settings", active = true, minHeight = 250 },
        new GUIPanel { name = "Active Players", active = true, minHeight = 250 },
        new GUIPanel { name = "Switch Vehicle", active = false, open = true, important = true, minHeight = 250, maxHeight = 350 },
        new GUIPanel { name = "Settings Advisor", active = false, open = true, important = true, minHeight = 155, maxHeight = 160 },
    };
    private float closePanel;
    public float quarryDist;
    // Game.js objectRocket and objectRocketSnipe, with the explosions their
    // Rocket.js components reference.
    public GameObject objectRocket;
    public GameObject objectRocketSnipe;
    public GameObject objectRocketExplosion;
    public GameObject objectRocketSnipeExplosion;

    private void Awake()
    {
#if UNITY_EDITOR
        EnsureEditorHudAssetBindings();
#endif
        Controller = this;
        Settings = GetComponent<Settings>();
        Messaging = GetComponent<Messaging>();
        loadingWorld = false;
        worldLoadTime = Time.time;
        if (GetComponent<GameModeSinglePlayerModern>() == null)
        {
            gameObject.AddComponent<GameModeSinglePlayerModern>();
        }
        if (Settings != null && Settings.fogColor == Color.clear)
        {
            Settings.fogColor = RenderSettings.fogColor;
        }
    }

#if UNITY_EDITOR
    private void EnsureEditorHudAssetBindings()
    {
        bool changed = false;
        if (cursor == null)
        {
            cursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Martian Assets/GUI/Cursors/Crosshairs.png");
            changed |= cursor != null;
        }

        if (objectMarkerMe == null)
        {
            objectMarkerMe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Marker Me.prefab");
            changed |= objectMarkerMe != null;
        }

        if (Skin == null)
        {
            Skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Martian Assets/GUI/Mars GUI.guiskin");
            changed |= Skin != null;
        }

        if (objectMarker == null || objectMarkerQuarry == null)
        {
            objectMarker = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Marker.prefab");
            objectMarkerQuarry = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Marker Quarry.prefab");
            changed = true;
        }

        const string lasers = "Assets/Martian Assets/Vehicles/Lasers/";
        if (objectRocket == null || objectRocketSnipe == null || objectRocketExplosion == null || objectRocketSnipeExplosion == null)
        {
            objectRocket = AssetDatabase.LoadAssetAtPath<GameObject>(lasers + "LaserAuto.prefab");
            objectRocketSnipe = AssetDatabase.LoadAssetAtPath<GameObject>(lasers + "LaserSnipe.prefab");
            objectRocketExplosion = AssetDatabase.LoadAssetAtPath<GameObject>(lasers + "Components/PlasmaSparks.prefab");
            objectRocketSnipeExplosion = AssetDatabase.LoadAssetAtPath<GameObject>(lasers + "Components/PlasmaSparksSnipe.prefab");
            changed = true;
        }

        if (cursorLook == null)
        {
            cursorLook = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Martian Assets/GUI/Cursors/CrosshairsLook.png");
            changed |= cursorLook != null;
        }

        if (hudTextStyle == null || string.IsNullOrEmpty(hudTextStyle.name))
        {
            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Martian Assets/GUI/Mars GUI.guiskin");
            if (skin != null)
            {
                var style = skin.FindStyle("hudText");
                if (style != null)
                {
                    hudTextStyle = style;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Update()
    {
        if (Player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) Player = tagged;
        }

        // Legacy reset lifecycle: /r style delayed reset back to base.
        if (Settings == null || Player == null) return;
        if (Settings.resetTime > 0f && (10f - (Time.time - Settings.resetTime)) < 1f)
        {
            var spawnPos = World.@base != null ? World.@base.position : Vector3.up * 6f;
            var spawnRot = World.@base != null ? World.@base.rotation : Quaternion.identity;

            Player.transform.position = spawnPos;
            Player.transform.rotation = spawnRot;

            var rb = Player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Settings.resetTime = -10f;
            Settings.updatePrefs();
        }
        else if (Settings.resetTime < -1f)
        {
            Settings.resetTime += Time.deltaTime;
        }
    }

    public void msg(string text, int origin)
    {
        if (Messaging != null)
        {
            Messaging.AddMessage(text, origin);
        }
    }

    private static readonly RaycastHit[] _laserHits = new RaycastHit[32];

    // Nearest hit along the ray that does not belong to the player's own vehicle.
    // Game.js addBot(): announce, wait 2 s, then spawn "<name>'s bot[ n]"
    // at the base like any vehicle (iV). The materialisation effect is not
    // ported yet.
    public void addBot()
    {
        if (Player == null || botsInGame >= 10) return;
        StartCoroutine(AddBotRoutine());
    }

    private System.Collections.IEnumerator AddBotRoutine()
    {
        botsInGame += 1;
        int n = botsInGame;
        msg(Player.name + " added " + (n == 1 ? "a bot" : "bot " + n), 2);
        string botName = Player.name + "'s bot" + (n == 1 ? "" : " " + n);
        yield return new WaitForSeconds(2f);
        var spawner = FindFirstObjectByType<SinglePlayerPlayableBootstrap>();
        if (spawner != null) spawner.SpawnBot(botName);
    }

    // Game.js axeBot(): remove the newest bot, then make sure someone is quarry.
    public void axeBot()
    {
        if (Player == null) return;
        GameObject bot = GameObject.Find(Player.name + "'s bot" + (botsInGame != 1 ? " " + botsInGame : ""));
        if (bot == null) return;
        msg(Player.name + " removed " + (botsInGame == 1 ? "the last bot" : "bot " + botsInGame), 2);
        botsInGame -= 1;
        Rigidbody body = bot.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        Destroy(bot);
    }

    public static bool RaycastIgnoringPlayer(Ray ray, float range, out RaycastHit hit)
    {
        int count = Physics.RaycastNonAlloc(ray, _laserHits, range, ~0, QueryTriggerInteraction.Ignore);
        Transform playerRoot = Player != null ? Player.transform : null;
        hit = default;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (playerRoot != null && _laserHits[i].transform.IsChildOf(playerRoot)) continue;
            if (_laserHits[i].distance < best)
            {
                best = _laserHits[i].distance;
                hit = _laserHits[i];
            }
        }
        return best < float.MaxValue;
    }

    private void OnGUI()
    {
        if (Player == null || Settings == null || Messaging == null) return;

        DrawCrosshair();
        DrawHudReadout();
        DrawMenu();
    }

    // Game.js OnGUI from "Basic Setup": the hide button and the side panels.
    // Not ported: the GUI fade (GUIAlpha), the Exit Game button, the HUD hint
    // lines, and switching the Switch Vehicle and Settings Advisor panels on.
    private void DrawMenu()
    {
        GUI.skin = Skin;
        GUI.depth = 1;
        if (loadingWorld) return;

        // Hide GUI button.
        if (GUI.Button(new Rect(Screen.width - 50, Screen.height - 50, 40, 40), !Settings.simplified ? ">>" : "<<"))
        {
            Settings.simplified = !Settings.simplified;
        }

        if (Player == null || Settings.simplified) return;

        // Build side panels.
        int liquidPanels = 0;
        int solidPanels = 0;
        int buttonHeight = 25;
        int panelSpacing = 5;
        float areaHeight = Screen.height - 60;
        float areaOffset = panelSpacing * 2;
        float areaOffsetDes = areaOffset;

        // Calculate side panel heights.
        for (int i = 0; i < GUIPanels.Length; i++)
        {
            if (!GUIPanels[i].active) continue;
            if (GUIPanels[i].open) liquidPanels++;
            else solidPanels++;
        }

        // Area to fill.
        for (int i = 0; i < GUIPanels.Length; i++)
        {
            GUIPanel panel = GUIPanels[i];
            if (!panel.active && panel.curHeight <= buttonHeight) continue;
            if (panel.open && panel.active)
            {
                panel.desHeight = (areaHeight - ((solidPanels + liquidPanels) * panelSpacing + solidPanels * buttonHeight)) / liquidPanels;
                if (panel.maxHeight > 0 && panel.desHeight > panel.maxHeight) panel.desHeight = panel.maxHeight;
                else if (panel.minHeight > 0 && panel.desHeight < panel.minHeight) panel.desHeight = panel.minHeight;
            }
            else
            {
                panel.desHeight = buttonHeight;
            }
            if (panel.curHeight > panel.desHeight - 1 && panel.curHeight < panel.desHeight + 1) panel.curHeight = panel.desHeight;
            else panel.curHeight = Mathf.Lerp(panel.curHeight, panel.desHeight, Time.deltaTime * 3);

            // We are a button.
            if (panel.curHeight < buttonHeight * 1.5f)
            {
                string txt = panel.name;
                if (i == 1) txt += " (" + (1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)).ToString("f0") + " FPS)";
                if (Event.current.type != EventType.Layout && GUI.Button(new Rect(Screen.width - 180, areaOffset, 170, panel.curHeight), txt))
                {
                    panel.open = !panel.open;
                    panel.openTime = Time.time;
                    GUI.FocusWindow(20 + i);
                }
            }
            // We are a panel.
            else
            {
                GUI.Window(20 + i, new Rect(Screen.width - 180, areaOffset, 170, panel.curHeight), DrawPanel, panel.name + ":", panel.important ? "boldWindow" : "window");
            }

            areaOffset += panel.curHeight + panelSpacing;
            areaOffsetDes += panel.desHeight + panelSpacing;
        }

        // Close the oldest panel if we are flowing out of the area.
        if (Event.current.type != EventType.Layout)
        {
            if (areaOffsetDes > areaHeight + panelSpacing * 4)
            {
                closePanel += Time.deltaTime;
                if (closePanel > 1.5f)
                {
                    int oldestI = -1;
                    for (int i = 0; i < GUIPanels.Length; i++)
                    {
                        if (GUIPanels[i].open && GUIPanels[i].openTime > 0 && GUIPanels[i].openTime < Time.time - 0.5f && (oldestI == -1 || GUIPanels[i].openTime < GUIPanels[oldestI].openTime)) oldestI = i;
                    }
                    if (oldestI != -1) GUIPanels[oldestI].open = false;
                }
            }
            else closePanel = 0;
        }
    }

    // Game.js GUIPanel(): close button, scroll view and the panel's contents.
    private void DrawPanel(int id)
    {
        int i = id - 20;
        GUIStyle closeButtonStyle = GUI.skin.GetStyle("close_button");
        if (GUI.Button(new Rect(closeButtonStyle.padding.left, closeButtonStyle.padding.top, closeButtonStyle.normal.background.width, closeButtonStyle.normal.background.height), "", "close_button")) GUIPanels[i].open = false;
        GUILayout.Space(5);
        GUIPanels[i].scrollPos = GUILayout.BeginScrollView(GUIPanels[i].scrollPos);

        if (i == 0) Settings.showDialogServer();
        // Temporary: the other panels are not ported yet.
        else GUILayout.Label("{Not ported yet}");

        GUILayout.Space(5);
        GUILayout.EndScrollView();
    }

    private void DrawCrosshair()
    {
        if (!(ShouldShowAim() && !Messaging.chatting)) return;

        bool canFire = Settings.lasersAllowed && Settings.firepower != null && Settings.firepower.Length > 0 && Settings.firepower[0] > 0;
        Texture2D tex = canFire ? cursor : cursorLook;

        if (tex != null)
        {
            float x = (Screen.width * 0.5f) - (tex.width * 0.5f);
            float y = (Screen.height * 0.5f) - (tex.height * 0.5f);
            Rect rect = new Rect(x, y, tex.width, tex.height);
            GUI.depth = -999;
            // DrawTexture, not Label: Unity 6's label pads 3 px top and bottom,
            // which shrank the 64 px crosshair to 58 px pinned to the top-left,
            // 3 px left of the screen centre the lasers fly to.
            GUI.DrawTexture(rect, tex);
            return;
        }

        // Fallback if cursor textures are not assigned yet.
        GUI.depth = -999;
        GUI.Label(new Rect((Screen.width * 0.5f) - 6f, (Screen.height * 0.5f) - 10f, 20f, 20f), canFire ? "+" : "o");
    }

    private bool ShouldShowAim()
    {
        if (Settings.camMode == 0) return true;
        if (Input.GetButton("Fire2")) return true;
        if (Input.GetButton("Snipe") && !Messaging.chatting) return true;
        return false;
    }

    private void DrawHudReadout()
    {
        VehicleStateModern state = Player.GetComponent<VehicleStateModern>();
        Rigidbody rb = Player.GetComponent<Rigidbody>();
        if (state == null && rb == null) return;

        int mph = 0;
        int alt = Mathf.RoundToInt(Player.transform.position.y);
        if (state != null)
        {
            mph = state.mph < 0.05f ? 0 : Mathf.RoundToInt(state.mph);
            alt = Mathf.RoundToInt(state.altitude);
        }
        else if (rb != null)
        {
            mph = rb.linearVelocity.magnitude < 0.05f ? 0 : Mathf.RoundToInt(rb.linearVelocity.magnitude * 2.23f);
        }

        string text = (mph == 0 ? "Static" : mph + " MPH") + "     " + alt + " ALT";

        // Vehicle.js: only a player who is not the quarry sees the distance to it.
        if (QuarryVeh != null && (state == null || state.isIt == 0))
        {
            text += "     " + Mathf.RoundToInt(quarryDist) + " DST";
        }

        Rect rect = new Rect((Screen.width * 0.5f) - 95f, Screen.height - 30f, 190f, 20f);
        if (hudTextStyle != null)
        {
            GUI.Button(rect, text, hudTextStyle);
        }
        else
        {
            GUI.Box(rect, text);
        }
    }
}
