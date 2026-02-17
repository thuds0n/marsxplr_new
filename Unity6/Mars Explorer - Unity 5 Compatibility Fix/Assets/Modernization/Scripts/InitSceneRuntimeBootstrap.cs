using UnityEngine;
using UnityEngine.SceneManagement;

public static class InitSceneRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == "InitGame" || scene.name == "Init")
        {
            SceneLoaderModern.LoadNextFromCurrent();
        }
    }
}
