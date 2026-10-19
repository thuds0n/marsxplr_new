using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReplaceMissingCoreScripts
{
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
        AssignGameHudAssets();

        ReplaceOn("Game/Camera", typeof(CameraVehicle));
        ReplaceOn("Game/MiniMap", typeof(MiniMap));

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Debug.Log("[ReplaceMissingCoreScripts] Applied replacement components. Save scene to persist.");
    }

    private static void AssignGameHudAssets()
    {
        var go = FindByPath("Game");
        if (go == null) return;

        var game = go.GetComponent<Game>();
        if (game == null) return;

        bool changed = false;

        var crosshairs = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Martian Assets/GUI/Cursors/Crosshairs.png");
        var crosshairsLook = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Martian Assets/GUI/Cursors/CrosshairsLook.png");
        if (crosshairs != null && game.cursor != crosshairs)
        {
            game.cursor = crosshairs;
            changed = true;
        }
        if (crosshairsLook != null && game.cursorLook != crosshairsLook)
        {
            game.cursorLook = crosshairsLook;
            changed = true;
        }

        var guiSkin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Martian Assets/GUI/Mars GUI.guiskin");
        if (guiSkin != null)
        {
            var hud = guiSkin.FindStyle("hudText");
            if (hud != null && game.hudTextStyle != hud)
            {
                game.hudTextStyle = hud;
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(game);
            Debug.Log("[ReplaceMissingCoreScripts] Assigned HUD cursor/style assets to Game.");
        }
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

        var legacyRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Vehicle.prefab");
        if (legacyRig != null && bootstrap.legacyVehicleRigPrefab != legacyRig)
        {
            bootstrap.legacyVehicleRigPrefab = legacyRig;
            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[ReplaceMissingCoreScripts] Assigned legacy vehicle rig prefab for laser sight objects.");
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
