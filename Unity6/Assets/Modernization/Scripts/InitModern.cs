using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InitModern : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "InitGame";
    [SerializeField] private string defaultUserName = "MarsRacer";

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(GameDataModern.userName))
        {
            var savedName = PlayerPrefs.GetString("userName", "");
            GameDataModern.userName = string.IsNullOrWhiteSpace(savedName) ? defaultUserName : savedName;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("InitModern requires a target scene name.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
