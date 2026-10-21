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
        RestoreGuidanceArrow();

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

        var markerMe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Marker Me.prefab");
        if (markerMe != null && game.objectMarkerMe != markerMe)
        {
            game.objectMarkerMe = markerMe;
            changed = true;
        }

        // The laser bolts and their sparks must be saved in the scene: the
        // player has no AssetDatabase to fill them in at runtime.
        const string lasers = "Assets/Martian Assets/Vehicles/Lasers/";
        changed |= AssignIfDifferent(ref game.objectRocket, lasers + "LaserAuto.prefab");
        changed |= AssignIfDifferent(ref game.objectRocketSnipe, lasers + "LaserSnipe.prefab");
        changed |= AssignIfDifferent(ref game.objectRocketExplosion, lasers + "Components/PlasmaSparks.prefab");
        changed |= AssignIfDifferent(ref game.objectRocketSnipeExplosion, lasers + "Components/PlasmaSparksSnipe.prefab");

        if (changed)
        {
            EditorUtility.SetDirty(game);
            Debug.Log("[ReplaceMissingCoreScripts] Assigned HUD and laser assets to Game.");
        }
    }

    private static bool AssignIfDifferent(ref GameObject field, string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null || field == asset) return false;
        field = asset;
        return true;
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

    // The green guidance arrow (Game/Camera/Arrow) holds an instance of the
    // Arrow.jas model. The Unity 2 scene stored the model's child expanded, with
    // the GUIArrow material, a 90 degree turn about X and z -0.003; Unity 6 loads
    // the instance from the model's defaults instead (Default-Material, no turn),
    // so those values are reapplied as prefab-instance overrides. Also links the
    // arrow to CameraVehicle, whose serialised reference was never set.
    private static void RestoreGuidanceArrow()
    {
        var cameraGo = FindByPath("Game/Camera");
        Transform outer = cameraGo != null ? cameraGo.transform.Find("Arrow") : null;
        Transform inner = outer != null ? outer.Find("Arrow") : null;
        if (inner == null)
        {
            Debug.LogWarning("[ReplaceMissingCoreScripts] Game/Camera/Arrow/Arrow not found.");
            return;
        }

        var cameraVehicle = cameraGo.GetComponent<CameraVehicle>();
        if (cameraVehicle != null && cameraVehicle.arrow != outer)
        {
            cameraVehicle.arrow = outer;
            EditorUtility.SetDirty(cameraVehicle);
        }

        inner.localRotation = new Quaternion(0.7071068f, 0f, 0f, 0.7071068f);
        inner.localPosition = new Vector3(0f, 0f, -0.003f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(inner);

        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/GUI/Objects/Materials/GUIArrow.mat");
        var renderer = inner.GetComponent<Renderer>();
        if (renderer != null && material != null && renderer.sharedMaterial != material)
        {
            renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        Debug.Log("[ReplaceMissingCoreScripts] Restored the guidance arrow's material and orientation.");
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
