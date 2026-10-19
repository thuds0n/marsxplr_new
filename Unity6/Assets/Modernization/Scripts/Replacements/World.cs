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
        DoubleLegacyLighting();
    }

    private IEnumerator LoadWorldScene()
    {
        yield return SceneManager.LoadSceneAsync(worldSceneName, LoadSceneMode.Additive);

        // The world's skybox, fog, and ambient light apply, as when Whirld loaded it.
        var worldScene = SceneManager.GetSceneByName(worldSceneName);
        if (worldScene.IsValid()) SceneManager.SetActiveScene(worldScene);

        if (worldScene.IsValid())
        {
            RestoreWhirldPrimitiveTextures(worldScene);
            GiveRenderersOwnMaterials(worldScene);
        }

        FinishWorld();
        DoubleLegacyLighting();
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

    // Unity 2.x's built-in legacy shaders finished with "texture * lighting
    // DOUBLE", so lights and ambient light counted twice; Unity 5 dropped the
    // doubling. Doubling the world's light intensities and ambient colour once
    // restores the original brightness (Unity's own upgrade advice for legacy
    // content).
    private static void DoubleLegacyLighting()
    {
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            light.intensity *= 2f;
        }
        Color ambient = RenderSettings.ambientLight;
        RenderSettings.ambientLight = new Color(ambient.r * 2f, ambient.g * 2f, ambient.b * 2f, ambient.a);
    }

    // WhirldIn.js built world primitives named cube, pyramid, cone, or mesh from
    // Resources prefabs (the Whirld "Object" material) and gave them world-space
    // UVs in TextureObject(). Worlds captured as scenes, such as Fort Boxy's
    // Castle, lost both and render with Unity's default white material.
    private static void RestoreWhirldPrimitiveTextures(Scene scene)
    {
        GameObject cube = Resources.Load<GameObject>("Cube");
        Renderer source = cube != null ? cube.GetComponentInChildren<Renderer>(true) : null;
        Material objectMaterial = source != null ? source.sharedMaterial : null;
        if (objectMaterial == null) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = r.gameObject.name;
                if (n != "cube" && n != "pyramid" && n != "cone" && n != "mesh") continue;
                Material m = r.sharedMaterial;
                if (m != null && !m.name.StartsWith("Default-")) continue;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null) continue;
                // Fort Boxy's captured pyramids also lost their mesh, so they
                // were neither drawn nor solid.
                if (mf.sharedMesh == null && n == "pyramid")
                {
                    mf.sharedMesh = PyramidMesh();
                    MeshCollider mc = r.GetComponent<MeshCollider>();
                    if (mc != null && mc.sharedMesh == null)
                    {
                        mc.convex = false;
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                if (mf.sharedMesh == null) continue;
                r.sharedMaterial = objectMaterial;
                if (mf.sharedMesh.isReadable) ApplyWorldSpaceUVs(mf);
            }
        }
    }

    private static Mesh _pyramidMesh;

    // The Whirld pyramid from Pyramid.blend, which imports only when Blender is
    // installed (and its converted FBX is empty): a square base at y = -0.5 with
    // corners at x, z = ±0.5 and the apex at (0, 0.5, 0), flat shaded. The
    // .blend is twice this size; the halved size matches how Fort Boxy's
    // pyramids sit on the surrounding geometry.
    private static Mesh PyramidMesh()
    {
        if (_pyramidMesh != null) return _pyramidMesh;
        Vector3 apex = new Vector3(0f, 0.5f, 0f);
        Vector3[] c =
        {
            new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
        };
        var vertices = new Vector3[16];
        var triangles = new int[18];
        // Base, facing down.
        vertices[0] = c[0]; vertices[1] = c[1]; vertices[2] = c[2]; vertices[3] = c[3];
        triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
        triangles[3] = 0; triangles[4] = 2; triangles[5] = 3;
        // Sides, wound clockwise when seen from outside.
        for (int i = 0; i < 4; i++)
        {
            int v = 4 + i * 3;
            vertices[v] = c[(i + 1) % 4];
            vertices[v + 1] = c[i];
            vertices[v + 2] = apex;
            int t = 6 + i * 3;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
        }
        _pyramidMesh = new Mesh { name = "Pyramid", vertices = vertices, triangles = triangles };
        _pyramidMesh.RecalculateNormals();
        _pyramidMesh.RecalculateBounds();
        return _pyramidMesh;
    }

    // Port of WhirldIn.js TextureObject(): planar world-space UVs per triangle,
    // chosen by the face normal, so the texture repeats once per metre.
    private static void ApplyWorldSpaceUVs(MeshFilter mf)
    {
        Mesh mesh = mf.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] tris = mesh.triangles;
        var uvs = new Vector2[vertices.Length];
        Transform t = mf.transform;
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = t.TransformPoint(vertices[tris[i]]);
            Vector3 b = t.TransformPoint(vertices[tris[i + 1]]);
            Vector3 c = t.TransformPoint(vertices[tris[i + 2]]);
            Vector3 n = Vector3.Cross(a - c, b - c).normalized;
            if (Mathf.Abs(Vector3.Dot(Vector3.up, n)) >= 0.5f)
            {
                uvs[tris[i]] = new Vector2(a.x, a.z);
                uvs[tris[i + 1]] = new Vector2(b.x, b.z);
                uvs[tris[i + 2]] = new Vector2(c.x, c.z);
            }
            else if (Mathf.Abs(Vector3.Dot(Vector3.right, n)) >= 0.5f)
            {
                uvs[tris[i]] = new Vector2(a.y, a.z);
                uvs[tris[i + 1]] = new Vector2(b.y, b.z);
                uvs[tris[i + 2]] = new Vector2(c.y, c.z);
            }
            else
            {
                uvs[tris[i]] = new Vector2(a.y, a.x);
                uvs[tris[i + 1]] = new Vector2(b.y, b.x);
                uvs[tris[i + 2]] = new Vector2(c.y, c.x);
            }
        }
        mesh.uv = uvs;
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
