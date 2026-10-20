using UnityEngine;

public class LegacyLaserSightController : MonoBehaviour
{
    public GameObject laserAimer;
    public GameObject laserAimerLocked;
    public GameObject laserLockMarker;
    public int vehicleId = 0;
    public float camOffset = 2f;

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
        _fallbackAim = EnsureFallbackVisual(laserAimer, materials != null ? materials.aim : null, new Color(0.25f, 1f, 0.35f, 1f), LegacyAimer.Aim);
        _fallbackAimLocked = EnsureFallbackVisual(laserAimerLocked, materials != null ? materials.aimLocked : null, new Color(1f, 0.25f, 0.15f, 1f), LegacyAimer.Locked);
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

    // The LaserAim and LaserAimLocked legacy particle settings, read from the
    // binary Vehicle.prefab (Unity 4.6). The emitter releases particles at rest;
    // a 200 m/s^2 forward force accelerates them, the renderer stretches them by
    // speed, and the colour animation keeps them transparent for the first
    // quarter of their 3 s life, so the beam only shows well ahead of the buggy.
    private struct LegacyAimer
    {
        public float size, emission, sizeGrow, velocityScale;

        public static readonly LegacyAimer Aim = new LegacyAimer { size = 0.05f, emission = 5f, sizeGrow = 6f, velocityScale = 0.4f };
        public static readonly LegacyAimer Locked = new LegacyAimer { size = 0.1f, emission = 7f, sizeGrow = 4f, velocityScale = 0.5f };

        public const float Energy = 3f;
        public const float Force = 200f;
        public const float LengthScale = 1f;
        public const float MaxParticleSize = 0.1f;
    }

    private GameObject EnsureFallbackVisual(GameObject root, Material sourceMaterial, Color color, LegacyAimer legacy)
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
        // The prefab's LaserAim objects are on layer 11, which the minimap skips.
        fallback.layer = root.layer;
        fallback.transform.SetParent(root.transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;

        var ps = fallback.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = LegacyAimer.Energy;
        main.startSpeed = 0f;
        main.startSize = legacy.size;
        main.startColor = sourceMaterial != null ? Color.white : color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.CeilToInt(legacy.emission * LegacyAimer.Energy) + 2;

        var emission = ps.emission;
        emission.rateOverTime = legacy.emission;

        // The legacy emitter was a point with an empty ellipsoid.
        var shape = ps.shape;
        shape.enabled = false;

        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.Local;
        force.x = 0f;
        force.y = 0f;
        force.z = LegacyAimer.Force;

        // ParticleAnimator colour keys at 0, 25, 50, 75 and 100% of life, all white.
        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0f, 0.25f),
                new GradientAlphaKey(144f / 255f, 0.5f),
                new GradientAlphaKey(88f / 255f, 0.75f),
                new GradientAlphaKey(0f, 1f),
            });
        colour.color = gradient;

        // The Unity 2 player's ParticleAnimator multiplies each particle's size
        // by pow(1 + sizeGrow, deltaTime) every frame, so size grows as
        // (1 + sizeGrow)^t: 0.05 m becomes 0.05 * 7^3 = 17 m after 3 s.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        float growth = 1f + legacy.sizeGrow;
        float endScale = Mathf.Pow(growth, LegacyAimer.Energy);
        const int keyCount = 13;
        var keys = new Keyframe[keyCount];
        for (int i = 0; i < keyCount; i++)
        {
            float x = i / (keyCount - 1f);
            float value = Mathf.Pow(growth, LegacyAimer.Energy * (x - 1f));
            float slope = value * Mathf.Log(growth) * LegacyAimer.Energy;
            keys[i] = new Keyframe(x, value, slope, slope);
        }
        size.size = new ParticleSystem.MinMaxCurve(endScale, new AnimationCurve(keys));

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
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = LegacyAimer.LengthScale;
            renderer.velocityScale = legacy.velocityScale;
            renderer.cameraVelocityScale = 0f;
            renderer.maxParticleSize = LegacyAimer.MaxParticleSize;
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
