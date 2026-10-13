using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarsXPLRProjectTools
{
    private const string SupportedEditorVersion = "6000.3.26f1";
    private const string InitScenePath = "Assets/_Scenes/Init.unity";
    private const string LobbyScenePath = "Assets/_Scenes/Lobby.unity";
    private const string InitGameScenePath = "Assets/_Scenes/InitGame.unity";
    private const string GameScenePath = "Assets/_Scenes/Game.unity";

    [MenuItem("Tools/MarsXPLR/Prepare Game Scene Baseline")]
    public static void PrepareGameSceneBaseline()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != GameScenePath)
        {
            Debug.LogWarning("[MarsXPLRProjectTools] Open Assets/_Scenes/Game.unity first, then run Prepare Game Scene Baseline.");
            return;
        }

        var buggyVisual = BuildBuggyVisualPrefab.BuildOrUpdate();
        if (buggyVisual == null)
        {
            Debug.LogWarning("[MarsXPLRProjectTools] Buggy visual regeneration did not produce a prefab. Continuing with existing references.");
        }

        ReplaceMissingCoreScripts.ReplaceInGameScene();
        MissingScriptScanner.ScanActiveScene();
        ValidateProjectBaseline(logSuccess: false);

        Debug.Log("[MarsXPLRProjectTools] Game scene baseline prepared. Save the scene if Unity marks it dirty.");
    }

    [MenuItem("Tools/MarsXPLR/Validate Project Baseline")]
    public static void ValidateProjectBaselineMenu()
    {
        ValidateProjectBaseline(logSuccess: true);
    }

    public static void ValidateProjectBaselineBatchMode()
    {
        if (!Application.isBatchMode)
        {
            Debug.LogError("[MarsXPLRProjectTools] Batch validation must be run with Unity's -batchmode argument.");
            return;
        }

        bool ok = false;
        try
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            ok = ValidateProjectBaseline(logSuccess: true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        Debug.Log(ok
            ? "[MarsXPLRProjectTools] Batch baseline validation passed."
            : "[MarsXPLRProjectTools] Batch baseline validation failed.");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool ValidateProjectBaseline(bool logSuccess)
    {
        bool ok = true;

        ok &= ValidateEditorVersion();
        ok &= ValidateSceneAssets();
        ok &= ValidateBuildSettings();
        ok &= ValidateRequiredAssets();
        ok &= ValidateActiveGameSceneIfOpen();

        if (ok)
        {
            if (logSuccess)
            {
                Debug.Log("[MarsXPLRProjectTools] Project baseline looks healthy.");
            }
        }
        else
        {
            Debug.LogWarning("[MarsXPLRProjectTools] Project baseline validation found issues. Review the warnings above.");
        }

        return ok;
    }

    private static bool ValidateEditorVersion()
    {
        if (!string.Equals(Application.unityVersion, SupportedEditorVersion, StringComparison.Ordinal))
        {
            Debug.LogWarning("[MarsXPLRProjectTools] Supported editor is " + SupportedEditorVersion + " but current editor is " + Application.unityVersion + ".");
            return false;
        }

        return true;
    }

    private static bool ValidateSceneAssets()
    {
        string[] paths =
        {
            InitScenePath,
            LobbyScenePath,
            InitGameScenePath,
            GameScenePath
        };

        bool ok = true;
        for (int i = 0; i < paths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[i]) == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] Missing required scene asset: " + paths[i]);
                ok = false;
            }
        }

        return ok;
    }

    private static bool ValidateBuildSettings()
    {
        string[] required =
        {
            InitScenePath,
            InitGameScenePath,
            GameScenePath
        };

        var scenes = EditorBuildSettings.scenes;
        bool ok = true;
        string firstEnabledScene = null;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled && firstEnabledScene == null)
            {
                firstEnabledScene = scenes[i].path;
            }
        }

        if (!string.Equals(firstEnabledScene, InitScenePath, StringComparison.Ordinal))
        {
            Debug.LogWarning("[MarsXPLRProjectTools] The first enabled build scene must be " + InitScenePath + ".");
            ok = false;
        }

        for (int requiredIndex = 0; requiredIndex < required.Length; requiredIndex++)
        {
            bool foundEnabled = false;
            for (int sceneIndex = 0; sceneIndex < scenes.Length; sceneIndex++)
            {
                if (scenes[sceneIndex].enabled && string.Equals(scenes[sceneIndex].path, required[requiredIndex], StringComparison.Ordinal))
                {
                    foundEnabled = true;
                    break;
                }
            }

            if (!foundEnabled)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] Required single-player scene is missing or disabled in Build Settings: " + required[requiredIndex]);
                ok = false;
            }
        }

        return ok;
    }

    private static bool ValidateRequiredAssets()
    {
        bool ok = true;
        ok &= ValidateAsset("Assets/Modernization/Generated/BuggyVisual.prefab", "generated buggy visual prefab");
        ok &= ValidateAsset("Assets/Martian Assets/Vehicles/Buggy/Buggy.prefab", "legacy buggy prefab source");
        ok &= ValidateAsset("Assets/Martian Assets/Vehicles/Prefabs/Vehicle.prefab", "legacy vehicle rig prefab");
        ok &= ValidateAsset("Assets/Martian Assets/GUI/Cursors/Crosshairs.png", "crosshair texture");
        ok &= ValidateAsset("Assets/Martian Assets/GUI/Cursors/CrosshairsLook.png", "look crosshair texture");
        ok &= ValidateAsset("Assets/Martian Assets/GUI/Mars GUI.guiskin", "Mars GUI skin");
        return ok;
    }

    private static bool ValidateAsset(string path, string description)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
        {
            return true;
        }

        Debug.LogWarning("[MarsXPLRProjectTools] Missing " + description + ": " + path);
        return false;
    }

    private static bool ValidateActiveGameSceneIfOpen()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != GameScenePath)
        {
            Debug.Log("[MarsXPLRProjectTools] Active scene is not Game.unity. Open Assets/_Scenes/Game.unity to validate runtime component wiring.");
            return true;
        }

        bool ok = true;
        ok &= ValidateRequiredGameObject("StarField");
        ok &= ValidateRequiredGameObject("Game");
        ok &= ValidateRequiredGameObject("Game/Camera");
        ok &= ValidateRequiredGameObject("Game/MiniMap");

        var starField = FindByPath("StarField");
        ok &= ValidateComponent<LobbyStarfield>(starField, "StarField");

        var gameObject = FindByPath("Game");
        ok &= ValidateComponent<World>(gameObject, "Game");
        ok &= ValidateComponent<Settings>(gameObject, "Game");
        ok &= ValidateComponent<Messaging>(gameObject, "Game");
        ok &= ValidateComponent<Game>(gameObject, "Game");
        ok &= ValidateComponent<SinglePlayerPlayableBootstrap>(gameObject, "Game");

        var camera = FindByPath("Game/Camera");
        ok &= ValidateComponent<CameraVehicle>(camera, "Game/Camera");

        var miniMap = FindByPath("Game/MiniMap");
        ok &= ValidateComponent<MiniMap>(miniMap, "Game/MiniMap");

        var game = gameObject != null ? gameObject.GetComponent<Game>() : null;
        if (game != null)
        {
            if (game.cursor == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] Game.cursor is not assigned.");
                ok = false;
            }

            if (game.cursorLook == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] Game.cursorLook is not assigned.");
                ok = false;
            }

            if (game.hudTextStyle == null || string.IsNullOrEmpty(game.hudTextStyle.name))
            {
                Debug.LogWarning("[MarsXPLRProjectTools] Game.hudTextStyle is not assigned.");
                ok = false;
            }
        }

        var bootstrap = gameObject != null ? gameObject.GetComponent<SinglePlayerPlayableBootstrap>() : null;
        if (bootstrap != null)
        {
            if (bootstrap.playerVehiclePrefab == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] SinglePlayerPlayableBootstrap.playerVehiclePrefab is not assigned.");
                ok = false;
            }

            if (bootstrap.playerVehicleModelFallbackPrefab == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] SinglePlayerPlayableBootstrap.playerVehicleModelFallbackPrefab is not assigned.");
                ok = false;
            }

            if (bootstrap.legacyVehicleRigPrefab == null)
            {
                Debug.LogWarning("[MarsXPLRProjectTools] SinglePlayerPlayableBootstrap.legacyVehicleRigPrefab is not assigned.");
                ok = false;
            }
        }

        return ok;
    }

    private static bool ValidateRequiredGameObject(string path)
    {
        if (FindByPath(path) != null)
        {
            return true;
        }

        Debug.LogWarning("[MarsXPLRProjectTools] Missing required Game scene object: " + path);
        return false;
    }

    private static bool ValidateComponent<T>(GameObject go, string path) where T : Component
    {
        if (go != null && go.GetComponent<T>() != null)
        {
            return true;
        }

        Debug.LogWarning("[MarsXPLRProjectTools] Missing required component " + typeof(T).Name + " on " + path + ".");
        return false;
    }

    private static GameObject FindByPath(string path)
    {
        string[] parts = path.Split('/');
        var root = GameObject.Find(parts[0]);
        if (root == null)
        {
            return null;
        }

        Transform current = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            var child = current.Find(parts[i]);
            if (child == null)
            {
                return null;
            }

            current = child;
        }

        return current.gameObject;
    }
}
