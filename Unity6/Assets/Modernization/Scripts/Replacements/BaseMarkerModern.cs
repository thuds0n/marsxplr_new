using UnityEngine;

// The base marker's level of detail and particle glow. Ports WhirldLOD.js as
// configured on Resources/Base.prefab (lodObjs LOD0 and LOD1, lodLevMax 3):
// at quality level 3 and above LOD0's glow shows, otherwise LOD1's
// BaseSimple cylinders. LOD0 held a legacy ellipsoid particle emitter, which
// Unity no longer supports; it is rebuilt here as a ParticleSystem from the
// original emitter, animator, and renderer values. Size growth, the random
// force, and the omitted damping are tuned against the Wine build.
public class BaseMarkerModern : MonoBehaviour
{
    public int lodLevMax = 3;
    public int level { get; private set; }

    private GameObject[] _lodObjs;
    private int _lastLevel = -1;

    private void Start()
    {
        Transform lod0 = transform.Find("LOD0");
        Transform lod1 = transform.Find("LOD1");
        if (lod0 == null || lod1 == null) return;
        _lodObjs = new[] { lod0.gameObject, lod1.gameObject };

        Transform inner = lod1.Find("Inner");
        Material lightning = inner != null ? inner.GetComponent<Renderer>()?.sharedMaterial : null;
        if (lod0.GetComponent<ParticleSystem>() == null) BuildGlow(lod0.gameObject, lightning);

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

    private static void BuildGlow(GameObject go, Material material)
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

        // rndVelocity (0.3, 1, 0.3).
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1f, 1f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        // ParticleAnimator rndForce (0, 5, 0), re-randomised each frame.
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.randomized = true;
        force.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        force.y = new ParticleSystem.MinMaxCurve(-5f, 5f);
        force.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // ParticleAnimator damping 0.7 is not reproduced: applied as drag it
        // held the particles in a small ball, whereas the original (compared
        // in Wine) drifts into a tall column several metres high.

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

        // sizeGrow 1: growth tuned against the Wine build, where blobs are a
        // few metres across. Linear 1 to 7 m was too thin; exponential over
        // 6 s (about 400 times) filled the screen.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        const float growth = 3f;
        var curve = new AnimationCurve();
        for (int i = 0; i <= 6; i++)
        {
            float t = i / 6f;
            curve.AddKey(t, Mathf.Exp(growth * (t - 1f)));
        }
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        size.size = new ParticleSystem.MinMaxCurve(Mathf.Exp(growth), curve);

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
    }
}
