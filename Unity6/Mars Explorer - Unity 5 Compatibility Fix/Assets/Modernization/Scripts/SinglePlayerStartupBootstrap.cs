using UnityEngine;
using UnityEngine.SceneManagement;

public static class SinglePlayerStartupBootstrap
{
    private const string InitSceneName = "Init";
    private const string GameInitSceneName = "InitGame";
    private const string UserNamePrefKey = "userName";
    private const string DefaultUserName = "MarsRacer";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RedirectInitSceneToSinglePlayer()
    {
        if (ModernizationConfig.MultiplayerEnabled)
        {
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != InitSceneName)
        {
            return;
        }

        InitializeSinglePlayerDefaults();
        SceneManager.LoadScene(GameInitSceneName, LoadSceneMode.Single);
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
