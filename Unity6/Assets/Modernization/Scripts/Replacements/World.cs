using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class World : MonoBehaviour
{
    public static int lodDist;
    public static Transform @base;
    public static Terrain[] terrains;
    public static Transform sea;
    public static Color seaFogColor;
    public static Color seaGlowColor;
    public static GameObject fallbackGround;
    // True while the world scene is streaming in; the player spawn waits for it.
    public static bool loading;

    // Fort Boxy is the first entry in the original Game scene's built-in world
    // list, which the game fell back to when the update server was unreachable.
    public string worldSceneName = "Fort Boxy";
    public bool autoCreateFallbackGround = true;
    public float fallbackGroundSize = 2000f;
    public float fallbackGroundThickness = 8f;

    private void Awake()
    {
        @base = null;
        sea = null;
        fallbackGround = null;
        loading = false;

        if (seaFogColor == default) seaFogColor = new Color(0.6f, 0.2f, 0.1f, 1f);
        if (seaGlowColor == default) seaGlowColor = new Color(1f, 0.4f, 0.1f, 1f);

        if (!string.IsNullOrEmpty(worldSceneName))
        {
            if (SceneManager.GetSceneByName(worldSceneName).isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(worldSceneName));
            }
            else if (Application.CanStreamedLevelBeLoaded(worldSceneName))
            {
                loading = true;
                StartCoroutine(LoadWorldScene());
                return;
            }
            else
            {
                Debug.LogError("[World] World scene is not available in Build Settings: " + worldSceneName);
            }
        }

        FinishWorld();
    }

    private IEnumerator LoadWorldScene()
    {
        yield return SceneManager.LoadSceneAsync(worldSceneName, LoadSceneMode.Additive);

        // The world's skybox, fog, and ambient light apply, as when Whirld loaded it.
        var worldScene = SceneManager.GetSceneByName(worldSceneName);
        if (worldScene.IsValid()) SceneManager.SetActiveScene(worldScene);

        if (worldScene.IsValid()) GiveRenderersOwnMaterials(worldScene);

        FinishWorld();
        if (Game.Settings != null) Game.Settings.fogColor = RenderSettings.fogColor;
        // Restart the camera's world entry effect from the moment the world is ready.
        if (Game.Controller != null) Game.Controller.worldLoadTime = Time.time;
        loading = false;
        Debug.Log("[World] Loaded world: " + worldSceneName);
    }

    // Mirrors the standard-component checks in Game.js Init().
    private void FinishWorld()
    {
        var baseGo = GameObject.Find("Base");
        if (baseGo != null) @base = baseGo.transform;
        else
        {
            var basePrefab = Resources.Load<GameObject>("Base");
            if (basePrefab != null) @base = Instantiate(basePrefab, Vector3.up * 5f, Quaternion.identity).transform;
        }

        // The base prefab's BaseSimple.js components are missing scripts; restore them.
        if (@base != null)
        {
            foreach (string part in new[] { "Outer", "Inner" })
            {
                Transform t = @base.Find(part);
                if (t == null) t = FindDescendant(@base, part);
                if (t != null && t.GetComponent<BaseSimpleModern>() == null) t.gameObject.AddComponent<BaseSimpleModern>();
            }
        }

        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);

        var seaGo = GameObject.Find("Sea");
        if (seaGo != null)
        {
            sea = seaGo.transform;
            if (Game.Settings != null) Game.Settings.lavaAlt = sea.position.y;
        }

        if (GameObject.Find("Floor") == null && terrains.Length == 0 && sea == null)
        {
            var floorPrefab = Resources.Load<GameObject>("Floor");
            if (floorPrefab != null) Instantiate(floorPrefab, Vector3.zero, Quaternion.identity).name = "Floor";
        }

        bool thereIsALight = false;
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.gameObject.name != "VehicleLight") thereIsALight = true;
        }
        if (!thereIsALight)
        {
            var lightPrefab = Resources.Load<GameObject>("Light");
            if (lightPrefab != null) Instantiate(lightPrefab);
        }

        if (autoCreateFallbackGround)
        {
            EnsureFallbackGround();
        }
    }

    // Compatibility workaround: in Unity 6 on Metal, legacy Whirld meshes that
    // share one material (Fort Boxy's AirCastle) draw with the wrong texture or
    // not at all, depending on draw order. A material per renderer renders them
    // correctly.
    private static void GiveRenderersOwnMaterials(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null) shared[i] = new Material(shared[i]);
                }
                r.sharedMaterials = shared;
            }
        }
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    private void EnsureFallbackGround()
    {
        if (terrains != null && terrains.Length > 0) return;
        if (fallbackGround != null) return;

        Vector3 probe = @base != null ? @base.position + Vector3.up * 50f : Vector3.up * 50f;
        if (Physics.Raycast(probe, Vector3.down, 300f, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        fallbackGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallbackGround.name = "FallbackGround";
        fallbackGround.transform.position = new Vector3(0f, -2f, 0f);
        if (@base != null)
        {
            fallbackGround.transform.position = new Vector3(@base.position.x, @base.position.y - 4f, @base.position.z);
        }

        fallbackGround.transform.localScale = new Vector3(fallbackGroundSize, fallbackGroundThickness, fallbackGroundSize);
        fallbackGround.layer = 8;

        var mr = fallbackGround.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            mat.mainTexture = CreateCheckerTexture();
            mat.mainTextureScale = new Vector2(180f, 180f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            mr.sharedMaterial = mat;
        }

        Debug.Log("[World] Created fallback ground for play-testing (no terrain detected).");
    }

    private static Texture2D CreateCheckerTexture()
    {
        const int size = 128;
        const int step = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "FallbackGroundChecker";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color a = new Color(0.53f, 0.28f, 0.18f, 1f);
        Color b = new Color(0.38f, 0.20f, 0.13f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool even = ((x / step) + (y / step)) % 2 == 0;
                tex.SetPixel(x, y, even ? a : b);
            }
        }

        tex.Apply(false, false);
        return tex;
    }
}
