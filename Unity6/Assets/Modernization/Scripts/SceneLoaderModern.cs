using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoaderModern
{
    public static void LoadByBuildIndex(int buildIndex)
    {
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    public static void LoadByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoaderModern.LoadByName called with an empty scene name.");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public static void LoadNextFromCurrent()
    {
        var active = SceneManager.GetActiveScene();
        var nextBuildIndex = active.buildIndex + 1;

        if (nextBuildIndex < 0 || nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("SceneLoaderModern.LoadNextFromCurrent could not find next scene in Build Settings.");
            return;
        }

        LoadByBuildIndex(nextBuildIndex);
    }
}
