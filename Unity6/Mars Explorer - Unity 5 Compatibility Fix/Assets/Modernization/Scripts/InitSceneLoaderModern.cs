using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InitSceneLoaderModern : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Lobby";

    private AsyncOperation _loadOperation;
    private bool _requestedLoad;

    private void Update()
    {
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
