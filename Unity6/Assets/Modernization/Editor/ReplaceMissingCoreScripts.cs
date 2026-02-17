using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReplaceMissingCoreScripts
{
    [MenuItem("Tools/MarsXPLR/Replace Missing Core Scripts (Game Scene)")]
    public static void ReplaceInGameScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.path.EndsWith("Assets/_Scenes/Game.unity"))
        {
            Debug.LogWarning("Open Assets/_Scenes/Game.unity first, then run this tool.");
            return;
        }

        ReplaceOn("StarField", typeof(LobbyStarfield));

        ReplaceOn("Game", typeof(World));
        ReplaceOn("Game", typeof(Settings));
        ReplaceOn("Game", typeof(Messaging));
        ReplaceOn("Game", typeof(Game));
        ReplaceOn("Game", typeof(SinglePlayerPlayableBootstrap));
        AssignBuggyPrefab();

        ReplaceOn("Game/Camera", typeof(CameraVehicle));
        ReplaceOn("Game/MiniMap", typeof(MiniMap));

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Debug.Log("[ReplaceMissingCoreScripts] Applied replacement components. Save scene to persist.");
    }

    private static void AssignBuggyPrefab()
    {
        var go = FindByPath("Game");
        if (go == null) return;

        var bootstrap = go.GetComponent<SinglePlayerPlayableBootstrap>();
        if (bootstrap == null) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Buggy/Buggy.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[ReplaceMissingCoreScripts] Could not load Buggy prefab at expected path.");
            return;
        }

        if (bootstrap.playerVehiclePrefab != prefab)
        {
            bootstrap.playerVehiclePrefab = prefab;
            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[ReplaceMissingCoreScripts] Assigned Buggy prefab to SinglePlayerPlayableBootstrap.");
        }

        // Prefer a generated clean visual prefab that strips legacy scripts/components.
        var modelFallback = BuildBuggyVisualPrefab.BuildOrUpdate();
        if (modelFallback == null)
        {
            modelFallback = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Buggy/Components/Buggy Wheels Simple.blend");
            if (modelFallback == null)
            {
                Debug.LogWarning("[ReplaceMissingCoreScripts] Could not load generated or legacy Buggy model fallback.");
            }
        }

        if (modelFallback != null && bootstrap.playerVehicleModelFallbackPrefab != modelFallback)
        {
            bootstrap.playerVehicleModelFallbackPrefab = modelFallback;
            bootstrap.tryLegacyBuggyPrefab = false;
            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[ReplaceMissingCoreScripts] Assigned Buggy model fallback prefab: " + AssetDatabase.GetAssetPath(modelFallback));
        }
    }

    private static void ReplaceOn(string path, System.Type componentType)
    {
        var go = FindByPath(path);
        if (go == null)
        {
            Debug.LogWarning("[ReplaceMissingCoreScripts] Not found: " + path);
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        if (go.GetComponent(componentType) == null)
        {
            go.AddComponent(componentType);
            Debug.Log("[ReplaceMissingCoreScripts] Added " + componentType.Name + " -> " + path);
        }
    }

    private static GameObject FindByPath(string path)
    {
        var parts = path.Split('/');
        var root = GameObject.Find(parts[0]);
        if (root == null) return null;
        var current = root.transform;

        for (int i = 1; i < parts.Length; i++)
        {
            var child = current.Find(parts[i]);
            if (child == null) return null;
            current = child;
        }

        return current.gameObject;
    }
}
