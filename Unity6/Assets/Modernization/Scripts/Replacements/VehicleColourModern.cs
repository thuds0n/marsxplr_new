using System.Collections.Generic;
using UnityEngine;

// Player vehicle colouring, ported from Vehicle.js (Start and the updateColor
// section of Update) and the colour start-up in Game.js. The body material
// (materialMain) fades to vehicleColor and the accent materials (the buggy's
// wheel beadlocks and wings) to vehicleAccent. The green "it" colours
// are not ported with the rest of the quarry rules.
[RequireComponent(typeof(VehicleStateModern))]
public class VehicleColourModern : MonoBehaviour
{
    private VehicleStateModern _state;
    private readonly List<Material> _materialMain = new List<Material>();
    private readonly List<Material> _materialAccent = new List<Material>();
    private bool _updateColor;

    private void Start()
    {
        _state = GetComponent<VehicleStateModern>();
        _state.vehicleColor = new Color(PlayerPrefs.GetFloat("vehColR"), PlayerPrefs.GetFloat("vehColG"), PlayerPrefs.GetFloat("vehColB"));
        _state.vehicleAccent = new Color(PlayerPrefs.GetFloat("vehColAccR"), PlayerPrefs.GetFloat("vehColAccG"), PlayerPrefs.GetFloat("vehColAccB"));

        Settings settings = Game.Settings;
        if (settings != null)
        {
            settings.colorCustom = PlayerPrefs.GetInt("vehColCustom") == 1;
            if (!settings.colorCustom) settings.randomizeVehicleColor(_state);
        }
        setColor();
    }

    public void setColor()
    {
        _updateColor = true;
    }

    private void Update()
    {
        if (!_updateColor) return;
        // Wheels are added by BuggyPresentationModern, so gather on first use.
        if (_materialMain.Count == 0) CollectMaterials();
        _updateColor = false;
        float t = Time.deltaTime * 2f;

        if (_materialMain.Count > 0)
        {
            Color target = _state.vehicleColor;
            Color c = Color.Lerp(_materialMain[0].color, target, t);
            c.a = 0.5f;
            _materialMain[0].color = c;
            _materialMain[0].SetColor("_SpecColor", _materialAccent.Count > 0 ? _materialAccent[0].color : _state.vehicleAccent);
            if (!Near(c, target)) _updateColor = true;
            for (int i = 1; i < _materialMain.Count; i++) _materialMain[i].color = c;
        }
        if (_materialAccent.Count > 0)
        {
            Color target = _state.vehicleAccent;
            Color c = Color.Lerp(_materialAccent[0].color, target, t);
            c.a = 0.5f;
            _materialAccent[0].color = c;
            if (_materialMain.Count > 0) _materialAccent[0].SetColor("_SpecColor", _materialMain[0].color);
            if (!Near(c, target)) _updateColor = true;
            for (int i = 1; i < _materialAccent.Count; i++) _materialAccent[i].color = c;
        }
    }

    private void CollectMaterials()
    {
        foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>(true))
        {
            // Renderer.material gives each vehicle its own instance, as Vehicle.js did.
            if (r.name == "Body") _materialMain.Add(r.material);
            else if (r.name == "Beadlock" || r.name == "Wings") _materialAccent.Add(r.material);
        }
    }

    private static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= 0.05f && Mathf.Abs(a.g - b.g) <= 0.05f && Mathf.Abs(a.b - b.b) <= 0.05f;
    }
}
