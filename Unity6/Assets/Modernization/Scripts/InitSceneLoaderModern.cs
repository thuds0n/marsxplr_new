using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InitSceneLoaderModern : MonoBehaviour
{
    // Retained for multiplayer/reference scene compatibility. The supported
    // single-player flow is owned by SinglePlayerStartupBootstrap.
    [SerializeField] private string nextSceneName = "Lobby";

    private AsyncOperation _loadOperation;
    private bool _requestedLoad;

    private void Update()
    {
        if (!ModernizationConfig.MultiplayerEnabled)
        {
            SinglePlayerStartupBootstrap.ContinueFromCurrentScene();
            enabled = false;
            return;
        }

        if (!_requestedLoad)
        {
            _requestedLoad = true;
            _loadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            if (_loadOperation != null)
            {
                _loadOperation.allowSceneActivation = true;
            }
        }

        if (_loadOperation == null)
        {
            return;
        }
    }
}
