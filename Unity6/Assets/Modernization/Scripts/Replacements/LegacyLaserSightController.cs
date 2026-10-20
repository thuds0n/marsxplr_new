using UnityEngine;

public class LegacyLaserSightController : MonoBehaviour
{
    public GameObject laserAimer;
    public GameObject laserAimerLocked;
    public GameObject laserLockMarker;
    public int vehicleId = 0;
    public float camOffset = 2f;
    public float fallbackBeamLength = 16f;
    public float fallbackBeamWidth = 0.035f;

    private GameObject _fallbackAim;
    private GameObject _fallbackAimLocked;
    private GameObject _fallbackLockMarker;

    private void Awake()
    {
        if (laserAimer == null)
        {
            var t = transform.Find("LaserAim");
            if (t != null) laserAimer = t.gameObject;
        }
        if (laserAimerLocked == null)
        {
            var t = transform.Find("LaserAimLocked");
            if (t != null) laserAimerLocked = t.gameObject;
        }
        if (laserLockMarker == null)
        {
            var t = transform.Find("LaserLock");
            if (t != null) laserLockMarker = t.gameObject;
        }

        var materials = Resources.Load<LaserSightMaterials>("LaserSightMaterials");
        _fallbackAim = EnsureFallbackVisual(laserAimer, materials != null ? materials.aim : null, new Color(0.25f, 1f, 0.35f, 0.9f), fallbackBeamLength, fallbackBeamWidth);
        _fallbackAimLocked = EnsureFallbackVisual(laserAimerLocked, materials != null ? materials.aimLocked : null, new Color(1f, 0.25f, 0.15f, 0.85f), fallbackBeamLength * 1.1f, fallbackBeamWidth * 1.1f);
        _fallbackLockMarker = EnsureFallbackMarker(laserLockMarker);
    }

    private void Update()
    {
        var state = GetComponent<VehicleStateModern>();
        if (state != null)
        {
            vehicleId = state.vehicleId;
            camOffset = state.camOffset;
        }

        if (Game.Settings == null)
        {
            SetActive(laserAimer, false);
            SetActive(laserAimerLocked, false);
            SetActive(laserLockMarker, false);
            return;
        }

        bool hasFirepower = Game.Settings.firepower != null && Game.Settings.firepower.Length > vehicleId && Game.Settings.firepower[vehicleId] > 0;
        bool lasersEnabled = Game.Settings.lasersAllowed && hasFirepower;
        bool lockingEnabled = lasersEnabled && Game.Settings.laserLocking;

        float lockScale = 0f;
        if (Game.Settings.laserLock != null && Game.Settings.laserLock.Length > vehicleId)
        {
            lockScale = Mathf.Max(0f, Game.Settings.laserLock[vehicleId]);
        }

        bool locked = lockingEnabled && HasQuarryLock(lockScale);
        bool markerEnabled = locked;
        SetActive(laserLockMarker, markerEnabled);
        if (markerEnabled && laserLockMarker != null)
        {
            float size = 0.45f;
            laserLockMarker.transform.localScale = Vector3.one * size;
        }
        if (_fallbackLockMarker != null)
        {
            _fallbackLockMarker.SetActive(markerEnabled);
        }

        if (!lockingEnabled)
        {
            SetActive(laserAimer, false);
            SetActive(laserAimerLocked, false);
            return;
        }

        SetActive(laserAimer, !locked);
        SetActive(laserAimerLocked, locked);
    }

    private bool HasQuarryLock(float lockScale)
    {
        if (Game.QuarryVeh == null) return false;

        float rayStart = ((lockScale + camOffset * 0.1f) * 15f);
        Vector3 origin = transform.position + transform.forward * rayStart;
        if (Game.RaycastIgnoringPlayer(new Ray(origin, transform.forward), Mathf.Infinity, out RaycastHit hit))
        {
            Transform t = hit.transform;
            Transform q = Game.QuarryVeh.transform;
            return t == q || t.IsChildOf(q) || q.IsChildOf(t);
        }

        return false;
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active)
        {
            go.SetActive(active);
        }

        ParticleSystem[] ps = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            var em = ps[i].emission;
            em.enabled = active;
            if (active && !ps[i].isPlaying) ps[i].Play(true);
            if (!active && ps[i].isPlaying) ps[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private GameObject EnsureFallbackVisual(GameObject root, Material sourceMaterial, Color color, float beamLength, float beamWidth)
    {
        if (root == null) return null;
        Transform existing = root.transform.Find("FallbackBeam");
        if (existing != null)
        {
            if (existing.GetComponent<ParticleSystem>() != null)
            {
                return existing.gameObject;
            }
            Destroy(existing.gameObject);
        }
        if (HasAnyVisibleComponent(root)) return null;

        var fallback = new GameObject("FallbackBeam");
        fallback.name = "FallbackBeam";
        fallback.transform.SetParent(root.transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;

        var ps = fallback.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = beamLength / 22f;
        main.startSpeed = 22f;
        main.startSize = beamWidth;
        main.startColor = sourceMaterial != null ? Color.white : color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 64;

        var emission = ps.emission;
        emission.rateOverTime = 85f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 0.01f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = 0f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.35f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.Ribbon;
        trails.ratio = 1f;
        trails.lifetime = 0.06f;
        trails.dieWithParticles = true;
        trails.sizeAffectsWidth = true;

        var renderer = fallback.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Material mat = sourceMaterial;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
                mat.color = color;
            }
            renderer.sharedMaterial = mat;
            renderer.trailMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Local;
        }

        ps.Play(true);

        return fallback;
    }

    private GameObject EnsureFallbackMarker(GameObject root)
    {
        if (root == null) return null;
        Transform existing = root.transform.Find("FallbackLockMarker");
        if (existing != null) return existing.gameObject;
        if (HasAnyVisibleComponent(root)) return null;

        var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.name = "FallbackLockMarker";
        fallback.transform.SetParent(root.transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one * 0.25f;

        var col = fallback.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = fallback.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.25f, 1f, 0.3f, 0.9f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.18f, 0.7f, 0.22f, 1f));
            }
            r.sharedMaterial = mat;
        }

        fallback.SetActive(false);
        return fallback;
    }

    private bool HasAnyVisibleComponent(GameObject go)
    {
        if (go == null) return false;
        if (go.GetComponentInChildren<Renderer>(true) != null) return true;
        if (go.GetComponentInChildren<ParticleSystem>(true) != null) return true;
        if (go.GetComponentInChildren<TrailRenderer>(true) != null) return true;
        return false;
    }
}
