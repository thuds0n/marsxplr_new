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
        // LobbyStarfield.js tested level 1 (the Lobby) and levels above it (the
        // game). The build list is now Init, InitGame, Game and the world, so
        // the scenes are told apart by name.
        string scene = SceneManager.GetActiveScene().name;
        bool inLobby = scene == "Lobby";
        bool inGame = !inLobby && scene != "Init" && scene != "InitGame";
        float guiHide = ReadLobbyGuiHide();

        if (inLobby && guiHide > 0f)
        {
            // .material, as in the source: the tint goes on this starfield's
            // own copy, not the shared asset.
            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                var col = _renderer.material.GetColor("_TintColor");
                col.a = 0.5f - guiHide / 2f;
                _renderer.material.SetColor("_TintColor", col);
            }
        }
        else if (inGame && _ps != null)
        {
            var vel = _ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = -Mathf.Min(Time.timeSinceLevelLoad, 5f);

            // Emission also stops below the Good quality level (index 3).
            if (Time.timeSinceLevelLoad > 7f || QualitySettings.GetQualityLevel() < 3)
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
