using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyStarfield : MonoBehaviour
{
    public MonoBehaviour Lobby;

    private ParticleSystem _ps;
    private Renderer _renderer;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _renderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        float guiHide = ReadLobbyGuiHide();

        if (sceneIndex == 1 && guiHide > 0f)
        {
            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                var col = _renderer.sharedMaterial.GetColor("_TintColor");
                col.a = 0.5f - guiHide / 2f;
                _renderer.sharedMaterial.SetColor("_TintColor", col);
            }
        }
        else if (sceneIndex > 1 && _ps != null)
        {
            var vel = _ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = -Mathf.Min(Time.timeSinceLevelLoad, 5f);

            if (Time.timeSinceLevelLoad > 7f)
            {
                var emission = _ps.emission;
                emission.enabled = false;
            }
        }
    }

    private float ReadLobbyGuiHide()
    {
        if (Lobby == null) return 0f;
        var type = Lobby.GetType();
        var field = type.GetField("GUIHide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null) return 0f;
        object value = field.GetValue(Lobby);
        if (value is float f) return f;
        return 0f;
    }
}
