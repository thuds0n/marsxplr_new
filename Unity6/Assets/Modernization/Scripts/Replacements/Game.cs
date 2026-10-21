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
            GUI.Label(rect, tex);
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
