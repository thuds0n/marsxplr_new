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
    public float quarryDist;

    private float _nextQuarryUpdateAt;
    private float _nextNonQuarryHitMsgAt;
    private Material _impactMaterial;

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
        if (Settings != null)
        {
            Settings.quarryScore = 0;
            if (Settings.fogColor == Color.clear)
            {
                Settings.fogColor = RenderSettings.fogColor;
            }
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
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Settings.resetTime = -10f;
            Settings.updatePrefs();
        }
        else if (Settings.resetTime < -1f)
        {
            Settings.resetTime += Time.deltaTime;
        }

        UpdateQuarrySelection();
        UpdateQuarryDistance();
    }

    public void msg(string text, int origin)
    {
        if (Messaging != null)
        {
            Messaging.AddMessage(text, origin);
        }
    }

    private void UpdateQuarrySelection()
    {
        if (Settings == null || !Settings.autoSelectQuarry) return;
        if (Time.time < _nextQuarryUpdateAt) return;
        _nextQuarryUpdateAt = Time.time + 1f;

        QuarryTarget[] candidates = FindObjectsOfType<QuarryTarget>(true);
        if (candidates == null || candidates.Length == 0)
        {
            EnsureFallbackQuarryTarget();
            candidates = FindObjectsOfType<QuarryTarget>(true);
        }

        if (candidates == null || candidates.Length == 0) return;

        QuarryTarget best = candidates[0];
        if (Player != null)
        {
            float bestSqr = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] == null) continue;
                float sqr = (candidates[i].transform.position - Player.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidates[i];
                }
            }
        }

        if (best != null && QuarryVeh != best.gameObject)
        {
            QuarryVeh = best.gameObject;
            msg("Quarry target: " + QuarryVeh.name, 2);
        }
    }

    private void UpdateQuarryDistance()
    {
        if (Player == null || QuarryVeh == null)
        {
            quarryDist = 0f;
            return;
        }

        quarryDist = Vector3.Distance(Player.transform.position, QuarryVeh.transform.position);
    }

    private void EnsureFallbackQuarryTarget()
    {
        GameObject existing = GameObject.Find("QuarryTargetPlaceholder");
        if (existing != null)
        {
            if (existing.GetComponent<QuarryTarget>() == null)
            {
                existing.AddComponent<QuarryTarget>();
            }
            return;
        }

        Vector3 center = World.@base != null ? World.@base.position : Vector3.zero;
        Vector3 pos = center + new Vector3(70f, 8f, 70f);
        if (Physics.Raycast(pos + Vector3.up * 250f, Vector3.down, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point + Vector3.up * 6f;
        }

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        target.name = "QuarryTargetPlaceholder";
        target.transform.position = pos;
        target.transform.localScale = new Vector3(3f, 2.5f, 3f);

        Rigidbody rb = target.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (target.GetComponent<QuarryTarget>() == null)
        {
            target.AddComponent<QuarryTarget>();
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.95f, 0.78f, 0.22f, 1f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.35f, 0.2f, 0.02f, 1f));
            }
            renderer.sharedMaterial = mat;
        }
    }

    public void RegisterLaserHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, Vector3 shotDir)
    {
        if (hitCollider == null) return;

        SpawnImpactStub(hitPoint, hitNormal);

        Rigidbody hitBody = hitCollider.attachedRigidbody;
        if (hitBody != null && Player != null && hitBody.gameObject != Player)
        {
            hitBody.AddForceAtPosition(shotDir * (Settings != null ? Settings.laserImpactForce : 18f), hitPoint, ForceMode.Impulse);
        }

        bool quarryHit = IsQuarryHit(hitCollider.gameObject);
        if (quarryHit)
        {
            if (Settings != null)
            {
                QuarryTarget qt = hitCollider.GetComponentInParent<QuarryTarget>();
                Settings.quarryScore += qt != null ? Mathf.Max(1, qt.scoreValue) : 1;
                msg("Quarry hit! Score: " + Settings.quarryScore, 2);
            }
            else
            {
                msg("Quarry hit!", 2);
            }
            return;
        }

        if (Time.time >= _nextNonQuarryHitMsgAt)
        {
            _nextNonQuarryHitMsgAt = Time.time + 0.25f;
            msg("Hit: " + hitCollider.gameObject.name, 0);
        }
    }

    private bool IsQuarryHit(GameObject hitObject)
    {
        if (QuarryVeh == null || hitObject == null) return false;
        Transform hit = hitObject.transform;
        Transform quarry = QuarryVeh.transform;
        return hit == quarry || hit.IsChildOf(quarry) || quarry.IsChildOf(hit);
    }

    private void SpawnImpactStub(Vector3 point, Vector3 normal)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "LaserImpactStub";
        fx.transform.position = point + normal * 0.05f;
        fx.transform.localScale = Vector3.one * 0.35f;
        Destroy(fx.GetComponent<Collider>());

        Renderer renderer = fx.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetImpactMaterial();
        }

        Destroy(fx, 0.2f);
    }

    private Material GetImpactMaterial()
    {
        if (_impactMaterial != null) return _impactMaterial;

        _impactMaterial = new Material(Shader.Find("Standard"));
        _impactMaterial.color = new Color(1f, 0.4f, 0.1f, 1f);
        if (_impactMaterial.HasProperty("_EmissionColor"))
        {
            _impactMaterial.EnableKeyword("_EMISSION");
            _impactMaterial.SetColor("_EmissionColor", new Color(0.9f, 0.25f, 0.05f, 1f));
        }
        return _impactMaterial;
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
            mph = rb.velocity.magnitude < 0.05f ? 0 : Mathf.RoundToInt(rb.velocity.magnitude * 2.23f);
        }

        string text = (mph == 0 ? "Static" : mph + " MPH") + "     " + alt + " ALT";

        if (QuarryVeh != null)
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
