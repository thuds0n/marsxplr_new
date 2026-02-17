using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildBuggyVisualPrefab
{
    public const string OutputPrefabPath = "Assets/Modernization/Generated/BuggyVisual.prefab";

    [MenuItem("Tools/MarsXPLR/Build Buggy Visual Prefab")]
    public static void BuildMenu()
    {
        var prefab = BuildOrUpdate();
        if (prefab == null)
        {
            Debug.LogWarning("[BuildBuggyVisualPrefab] Failed to generate prefab.");
            return;
        }

        Debug.Log("[BuildBuggyVisualPrefab] Generated " + OutputPrefabPath);
    }

    public static GameObject BuildOrUpdate()
    {
        const string buggyModelPath = "Assets/Martian Assets/Vehicles/Buggy/Components/Buggy.jas";
        const string wheelModelPath = "Assets/Martian Assets/Vehicles/Buggy/Components/Buggy Wheel.jas";

        var bodyMeshes = LoadMeshes(buggyModelPath);
        var wheelMeshes = LoadMeshes(wheelModelPath);

        if (bodyMeshes.Count == 0 && wheelMeshes.Count == 0)
        {
            Debug.LogWarning("[BuildBuggyVisualPrefab] No mesh sub-assets found in .jas files. Trying prefab/model fallback sources.");
            return BuildFromPrefabSources();
        }

        var root = new GameObject("BuggyVisual");
        var mainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Main.mat");
        var cockpitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Cockpit.mat");
        var headlightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Headlights.mat");
        var forceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Forcefield.mat");

        foreach (var mesh in bodyMeshes)
        {
            string n = mesh.name.ToLowerInvariant();
            if (n.Contains("collider")) continue;

            var child = new GameObject(mesh.name);
            child.transform.SetParent(root.transform, false);
            var mf = child.AddComponent<MeshFilter>();
            var mr = child.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;

            if (n.Contains("cockpit")) mr.sharedMaterial = cockpitMat != null ? cockpitMat : mainMat;
            else if (n.Contains("head")) mr.sharedMaterial = headlightMat != null ? headlightMat : mainMat;
            else if (n.Contains("wing")) mr.sharedMaterial = forceMat != null ? forceMat : mainMat;
            else mr.sharedMaterial = mainMat;
        }

        if (wheelMeshes.Count > 0)
        {
            // Place simple four-wheel visuals from wheel mesh bounds.
            Mesh wheel = wheelMeshes[0];
            Vector3[] wheelPos =
            {
                new Vector3(-1.1f, -0.35f, 1.35f),
                new Vector3( 1.1f, -0.35f, 1.35f),
                new Vector3(-1.1f, -0.35f,-1.35f),
                new Vector3( 1.1f, -0.35f,-1.35f)
            };

            for (int i = 0; i < wheelPos.Length; i++)
            {
                var w = new GameObject("Wheel_" + i);
                w.transform.SetParent(root.transform, false);
                w.transform.localPosition = wheelPos[i];
                if (i % 2 == 1) w.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var mf = w.AddComponent<MeshFilter>();
                var mr = w.AddComponent<MeshRenderer>();
                mf.sharedMesh = wheel;
                mr.sharedMaterial = mainMat;
            }
        }

        // Center visual around origin for easier player attachment.
        RecenterChildren(root);

        EnsureOutputFolder();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);

        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return saved;
    }

    private static GameObject BuildFromPrefabSources()
    {
        string[] sourcePaths =
        {
            "Assets/Martian Assets/Vehicles/Buggy/Buggy.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy Wheel.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy Axel.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy Skidmarks.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy.prefab",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy Wheel.prefab",
            "Assets/Martian Assets/Vehicles/Buggy/Components/Converted/Buggy.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Components/Converted/Buggy.prefab",
            "Assets/Martian Assets/Vehicles/Buggy/Components/Converted/BuggyWheel.fbx",
            "Assets/Martian Assets/Vehicles/Buggy/Components/Converted/BuggyWheel.prefab",
            "Assets/Martian Assets/Vehicles/Buggy/Components/Buggy Wheels Simple.blend",
            "Assets/Martian Assets/Vehicles/Buggy/Buggy Skidmarks.prefab"
        };
        EnsureOutputFolder();

        for (int i = 0; i < sourcePaths.Length; i++)
        {
            string path = sourcePaths[i];
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null)
            {
                Debug.LogWarning("[BuildBuggyVisualPrefab] Source not found: " + path);
                continue;
            }

            var instance = Object.Instantiate(src);
            instance.name = "BuggyVisual";
            RemoveMissingScriptsRecursive(instance);
            SanitizeRuntimeComponents(instance);
            EnsureRenderersEnabled(instance);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyBuggyMaterials(instance);
            }
            RecenterChildren(instance);

            int meshCount = CountRenderableMeshes(instance);
            if (meshCount == 0)
            {
                Debug.LogWarning("[BuildBuggyVisualPrefab] Source has no valid mesh data after sanitize: " + path);
                Object.DestroyImmediate(instance);
                continue;
            }

            var saved = PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (saved != null)
            {
                Debug.Log("[BuildBuggyVisualPrefab] Built visual prefab from source: " + path);
                return saved;
            }
        }

        return null;
    }

    private static List<Mesh> LoadMeshes(string assetPath)
    {
        var result = new List<Mesh>();
        var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        Debug.Log("[BuildBuggyVisualPrefab] " + assetPath + " assets: " + all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is Mesh m)
            {
                result.Add(m);
            }
        }
        Debug.Log("[BuildBuggyVisualPrefab] " + assetPath + " meshes: " + result.Count);
        return result;
    }

    private static void RemoveMissingScriptsRecursive(GameObject root)
    {
        var allTransforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(allTransforms[i].gameObject);
        }
    }

    private static void SanitizeRuntimeComponents(GameObject root)
    {
        var scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
            {
                Object.DestroyImmediate(scripts[i], true);
            }
        }

        var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Object.DestroyImmediate(rigidbodies[i], true);
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i], true);
        }
    }

    private static void EnsureRenderersEnabled(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
        }
    }

    private static void ApplyBuggyMaterials(GameObject root)
    {
        var mainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Accent.mat");
        if (mainMat == null)
        {
            mainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Main.mat");
        }
        if (mainMat == null)
        {
            mainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Main.mat");
        }
        var cockpitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Cockpit.mat");
        var headlightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Headlights.mat");
        var forceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Materials/Vehicle Simple Forcefield.mat");

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            string n = r.gameObject.name.ToLowerInvariant();
            Material chosen = mainMat;

            if (n.Contains("cockpit") || n.Contains("glass"))
            {
                chosen = cockpitMat != null ? cockpitMat : mainMat;
            }
            else if (n.Contains("head") || n.Contains("light"))
            {
                chosen = headlightMat != null ? headlightMat : mainMat;
            }
            else if (n.Contains("wing") || n.Contains("force"))
            {
                chosen = forceMat != null ? forceMat : mainMat;
            }

            if (chosen != null)
            {
                int count = Mathf.Max(1, r.sharedMaterials != null ? r.sharedMaterials.Length : 1);
                var mats = new Material[count];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = chosen;
                }

                r.sharedMaterials = mats;
            }
        }
    }


    private static void EnsureOutputFolder()
    {
        const string modernizationDir = "Assets/Modernization";
        const string generatedDir = "Assets/Modernization/Generated";
        if (!AssetDatabase.IsValidFolder(modernizationDir))
        {
            AssetDatabase.CreateFolder("Assets", "Modernization");
        }

        if (!AssetDatabase.IsValidFolder(generatedDir))
        {
            AssetDatabase.CreateFolder(modernizationDir, "Generated");
        }
    }

    private static int CountRenderableMeshes(GameObject root)
    {
        int count = 0;

        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh != null) count++;
        }

        var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i].sharedMesh != null) count++;
        }

        return count;
    }

    private static void RecenterChildren(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        Vector3 center = b.center;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            child.position -= center;
        }

        // Keep model slightly above ground.
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            child.localPosition += new Vector3(0f, 0.8f, 0f);
        }
    }
}
