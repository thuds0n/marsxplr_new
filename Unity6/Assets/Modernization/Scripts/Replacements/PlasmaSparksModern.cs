using UnityEngine;

// The PlasmaSparks and PlasmaSparksSnipe impact effects. Their legacy
// EllipsoidParticleEmitter, ParticleAnimator and ParticleRenderer were dropped
// on import, so the effect is rebuilt here as a ParticleSystem from the values
// read from the binary prefabs. Added by RocketModern to each sparks instance;
// the prefab keeps its own AudioSource for the hit sound.
public class PlasmaSparksModern : MonoBehaviour
{
    private struct Legacy
    {
        public int emission;
        public float energy, sizeGrow, maxParticleSize;
        public string material;
    }

    // Shared: size 0.4–0.7, rndVelocity (3, 0, 3), force (0, -1, 0), damping 1,
    // stretched particles with length scale 1.5, alpha keys 10, 180, 255, 180, 10.
    private static readonly Legacy Auto = new Legacy { emission = 4, energy = 0.8f, sizeGrow = 2f, maxParticleSize = 1f, material = "Plasma Green" };
    private static readonly Legacy Snipe = new Legacy { emission = 8, energy = 1.3f, sizeGrow = 4f, maxParticleSize = 2f, material = "Plasma Red" };

    public Material material;
    public bool snipe;

    private void Start()
    {
        Legacy legacy = snipe ? Snipe : Auto;

        var go = new GameObject("Sparks");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = legacy.energy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = legacy.emission;
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        // ParticleAnimator force: 1 m/s² down, in world space.
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.x = 0f;
        force.y = -1f;
        force.z = 0f;

        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(10f / 255f, 0f),
                new GradientAlphaKey(180f / 255f, 0.25f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(180f / 255f, 0.75f),
                new GradientAlphaKey(10f / 255f, 1f),
            });
        colour.color = gradient;

        // The legacy animator grows size as (1 + sizeGrow)^t.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        float growth = 1f + legacy.sizeGrow;
        const int keyCount = 9;
        var keys = new Keyframe[keyCount];
        for (int i = 0; i < keyCount; i++)
        {
            float x = i / (keyCount - 1f);
            float value = Mathf.Pow(growth, legacy.energy * x);
            float slope = value * Mathf.Log(growth) * legacy.energy;
            keys[i] = new Keyframe(x, value, slope, slope);
        }
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.5f;
        renderer.velocityScale = 0f;
        renderer.cameraVelocityScale = 0f;
        renderer.maxParticleSize = legacy.maxParticleSize;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // One-shot emitter: every particle at once, from a point, with a random
        // velocity inside the rndVelocity box.
        for (int i = 0; i < legacy.emission; i++)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = transform.position,
                velocity = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)),
                startSize = Random.Range(0.4f, 0.7f),
                startLifetime = legacy.energy,
                applyShapeToPosition = false,
            };
            ps.Emit(p, 1);
        }

        // ParticleAnimator autodestruct, kept until the hit sound has finished.
        float life = legacy.energy;
        var audio = GetComponent<AudioSource>();
        if (audio != null && audio.clip != null) life = Mathf.Max(life, audio.clip.length / Mathf.Max(0.01f, Mathf.Abs(audio.pitch)));
        Destroy(gameObject, life + 0.1f);
    }
}
