using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MiniMap : MonoBehaviour
{
    public float camHeight = 650f;

    private bool _revertFogState;
    private int _terrainLod;
    private float _terrainTreeDistance;
    private float _terrainDetailDistance;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Game.Settings == null || Game.Controller == null || !Game.Settings.minimapAllowed || !Game.Settings.useMinimap || Game.Controller.loadingWorld || Game.Player == null)
        {
            _cam.enabled = false;
            return;
        }

        _cam.enabled = true;
        var main = Camera.main;
        if (main == null) return;

        transform.position = main.transform.position;
        var p = transform.position;
        p.y = Game.Player.transform.position.y + camHeight;
        transform.position = p;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(90f, Game.Player.transform.eulerAngles.y, 0f), Time.deltaTime);
    }

    private void OnPreRender()
    {
        _revertFogState = RenderSettings.fog;
        RenderSettings.fog = false;

        if (World.terrains == null || World.terrains.Length == 0) return;

        _terrainLod = World.terrains[0].heightmapMaximumLOD;
        _terrainTreeDistance = World.terrains[0].treeDistance;
        _terrainDetailDistance = World.terrains[0].detailObjectDistance;

        foreach (var trn in World.terrains)
        {
            if (Game.Settings != null && Game.Settings.renderLevel > 4) trn.heightmapMaximumLOD = 3;
            else if (Game.Settings != null && Game.Settings.renderLevel > 3) trn.heightmapMaximumLOD = 4;
            else trn.heightmapMaximumLOD = 5;
            trn.treeDistance = 0;
            trn.detailObjectDistance = 0;
        }
    }

    private void OnPostRender()
    {
        RenderSettings.fog = _revertFogState;
        if (World.terrains == null) return;

        foreach (var trn in World.terrains)
        {
            trn.treeDistance = _terrainTreeDistance;
            trn.detailObjectDistance = _terrainDetailDistance;
            trn.heightmapMaximumLOD = _terrainLod;
        }
    }

    private void OnGUI()
    {
        if (Game.Settings == null || !Game.Settings.minimapAllowed || !Game.Settings.useMinimap || Game.Player == null) return;
        camHeight = GUI.HorizontalSlider(new Rect(Screen.width * 0.01f + 25f, Screen.height - 20f - Screen.height * 0.001f, Screen.width * 0.25f - 50f, 20f), camHeight, 200f, 1300f);
    }
}
