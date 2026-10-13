using UnityEngine;
using UnityEngine.SceneManagement;

public static class SinglePlayerStartupBootstrap
{
    private const string InitSceneName = "Init";
    private const string GameInitSceneName = "InitGame";
    private const string GameSceneName = "Game";
    private const string UserNamePrefKey = "userName";
    private const string DefaultUserName = "MarsRacer";
    private static int _requestedFromSceneHandle = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _requestedFromSceneHandle = -1;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ContinueSinglePlayerFlowAfterSceneLoad()
    {
        // AfterSceneLoad only fires for the first scene, so later hops
        // (InitGame -> Game) are driven by sceneLoaded.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ContinueFromCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ContinueFromCurrentScene();
    }

    public static bool ContinueFromCurrentScene()
    {
        if (ModernizationConfig.MultiplayerEnabled)
        {
            return false;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.handle == _requestedFromSceneHandle)
        {
            return false;
        }

        string nextSceneName;
        if (activeScene.name == InitSceneName)
        {
            InitializeSinglePlayerDefaults();
            nextSceneName = GameInitSceneName;
        }
        else if (activeScene.name == GameInitSceneName)
        {
            nextSceneName = GameSceneName;
        }
        else
        {
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError("[SinglePlayerStartupBootstrap] Required scene is not available in Build Settings: " + nextSceneName);
            return false;
        }

        _requestedFromSceneHandle = activeScene.handle;
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        return true;
    }

    private static void InitializeSinglePlayerDefaults()
    {
        if (string.IsNullOrWhiteSpace(GameDataModern.userName))
        {
            var savedName = PlayerPrefs.GetString(UserNamePrefKey, string.Empty);
            GameDataModern.userName = string.IsNullOrWhiteSpace(savedName) ? DefaultUserName : savedName;
        }

        GameDataModern.errorMessage = string.Empty;
        GameDataModern.networkMode = 0;
    }
}
