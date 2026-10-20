using UnityEngine;

// The base marker's level of detail and particle glow. Ports WhirldLOD.js as
// configured on Resources/Base.prefab (lodObjs LOD0 and LOD1, lodLevMax 3):
// at quality level 3 and above LOD0's glow shows, otherwise LOD1's
// BaseSimple cylinders. LOD0 held a legacy ellipsoid particle emitter, which
// Unity no longer supports; it is rebuilt here as a ParticleSystem from the
// original emitter, animator, and renderer values. The animator's per-frame
// step, read from the original Mac player, is applied to each particle in
// LateUpdate, since lifetimes vary and no built-in module matches it.
public class BaseMarkerModern : MonoBehaviour
{
    public int lodLevMax = 3;
    public int level { get; private set; }

    private GameObject[] _lodObjs;
    private int _lastLevel = -1;
    private ParticleSystem _glow;
    private ParticleSystem.Particle[] _particles;

    // ParticleAnimator values from Resources/Base.prefab.
    private const float SizeGrow = 1f;
    private const float Damping = 0.7f;
    private static readonly Vector3 RndForce = new Vector3(0f, 5f, 0f);
    // EllipsoidParticleEmitter rndVelocity.
    private static readonly Vector3 RndVelocity = new Vector3(0.3f, 1f, 0.3f);
    private const float StartSize = 1f;

    private void Start()
    {
        Transform lod0 = transform.Find("LOD0");
        Transform lod1 = transform.Find("LOD1");
        if (lod0 == null || lod1 == null) return;
        _lodObjs = new[] { lod0.gameObject, lod1.gameObject };

        Transform inner = lod1.Find("Inner");
        Material lightning = inner != null ? inner.GetComponent<Renderer>()?.sharedMaterial : null;
        _glow = lod0.GetComponent<ParticleSystem>();
        if (_glow == null) _glow = BuildGlow(lod0.gameObject, lightning);
        _particles = new ParticleSystem.Particle[_glow.main.maxParticles];

        InvokeRepeating(nameof(SetLOD), 0f, Random.Range(30, 60) / 10f);
    }

    private void SetLOD()
    {
        // Settings.js set QualitySettings.currentLevel to renderLevel - 1.
        int quality = Game.Settings != null ? Game.Settings.renderLevel - 1 : QualitySettings.GetQualityLevel();
        level = quality >= lodLevMax ? 0 : 1;
        if (level == _lastLevel) return;
        for (int i = 0; i < _lodObjs.Length; i++) _lodObjs[i].SetActive(i == level);
        _lastLevel = level;
    }

    // The Unity 2 player's ParticleAnimator update: each frame velocity *=
    // pow(damping, dt), velocity += force * dt, size *= pow(1 + sizeGrow, dt).
    // So size(t) = startSize * (1 + sizeGrow)^t and speed decays as damping^t.
    private void LateUpdate()
    {
        if (_glow == null || !_glow.gameObject.activeInHierarchy) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        int count = _glow.GetParticles(_particles);
        float damping = Mathf.Pow(Damping, dt);
        for (int i = 0; i < count; i++)
        {
            ref ParticleSystem.Particle p = ref _particles[i];
            float age = p.startLifetime - p.remainingLifetime;
            Vector3 v = p.velocity;
            // Particles are emitted at rest; give each its emitter rndVelocity once.
            if (v == Vector3.zero) v = RandomBox(RndVelocity);
            v = v * damping + RandomBox(RndForce) * dt;
            p.velocity = v;
            p.startSize = StartSize * Mathf.Pow(1f + SizeGrow, age);
        }
        _glow.SetParticles(_particles, count);
    }

    private static Vector3 RandomBox(Vector3 extent) =>
        new Vector3(Random.Range(-extent.x, extent.x), Random.Range(-extent.y, extent.y), Random.Range(-extent.z, extent.z));

    private static ParticleSystem BuildGlow(GameObject go, Material material)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // EllipsoidParticleEmitter: 20 per second for 5 to 7 s, size 1, world space.
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f);
        main.startSize = 1f;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // rndVelocity, rndForce, damping and sizeGrow: see LateUpdate.

        // colorAnimation: white, alpha 10, 180, 255, 180, 10.
        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(10f / 255f, 0f), new GradientAlphaKey(180f / 255f, 0.25f), new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(180f / 255f, 0.75f), new GradientAlphaKey(10f / 255f, 1f),
            });
        colour.color = gradient;

        // ParticleRenderer: Lightning material, at most 0.3 of the screen.
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.3f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // BaseSimpleModern tiles and scrolls the cylinder's copy of this
        // material; the particles need the whole glow texture.
        if (material != null)
        {
            var glow = new Material(material) { name = "Lightning (Glow)" };
            glow.mainTextureScale = Vector2.one;
            glow.mainTextureOffset = Vector2.zero;
            renderer.sharedMaterial = glow;
        }

        ps.Play();
        return ps;
    }
}
