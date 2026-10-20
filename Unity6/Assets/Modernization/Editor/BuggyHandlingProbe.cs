using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Play-mode probe that drives the buggy through a fixed input script and logs
// HUD-equivalent telemetry for comparison with the original game.
public static class BuggyHandlingProbe
{
    [MenuItem("Tools/MarsXPLR/Run Buggy Handling Probe")]
    private static void Run()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyHandlingProbeRunner>();
    }

    // The same tests on Fort Boxy's open floor south of the castle, with the
    // jump taken by driving off the spawn slab from the base.
    [MenuItem("Tools/MarsXPLR/Run Buggy Handling Probe On Fort Boxy")]
    private static void RunFortBoxy()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyHandlingProbeRunner>().fortBoxy = true;
    }

    // Take-off, glide, airbrake, stall, and landing on the probe pad.
    [MenuItem("Tools/MarsXPLR/Run Buggy Flight Probe")]
    private static void RunFlight()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyHandlingProbeRunner>().flight = true;
    }

    // Drives from wherever the buggy is: 3 s of throttle, then 1.5 s of full
    // right lock while coasting, for comparing tyre tracks on real ground.
    [MenuItem("Tools/MarsXPLR/Run Buggy Track Test")]
    private static void RunTrackTest()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[HandlingProbe] Enter Play mode first."); return; }
        BuggyControllerModern buggy = Object.FindAnyObjectByType<BuggyControllerModern>();
        if (buggy == null) { Debug.LogWarning("[HandlingProbe] No BuggyControllerModern found."); return; }
        buggy.gameObject.AddComponent<BuggyTrackTestRunner>();
    }
}

public class BuggyTrackTestRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        var state = GetComponent<VehicleStateModern>();
        var input = GetComponent<PlayerVehicleInputModern>();
        if (input != null) input.enabled = false;
        // Open floor south of Fort Boxy's castle, facing away from it.
        var rb = GetComponent<Rigidbody>();
        if (Physics.Raycast(new Vector3(0f, 200f, -300f), Vector3.down, out RaycastHit ground, 400f))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = ground.point + Vector3.up * 0.6f;
            rb.rotation = Quaternion.Euler(0f, 180f, 0f);
            transform.SetPositionAndRotation(rb.position, rb.rotation);
        }
        for (float t = 0f; t < 1.5f; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
        foreach (var step in new[] { (new Vector4(0, 1, 0, 0), 3f), (new Vector4(1, 0, 0, 0), 1.5f), (Vector4.zero, 2f) })
        {
            for (float t = 0f; t < step.Item2; t += Time.fixedDeltaTime)
            {
                state.input = step.Item1;
                yield return new WaitForFixedUpdate();
            }
        }
        if (input != null) input.enabled = true;
        Destroy(this);
    }
}

public class BuggyHandlingProbeRunner : MonoBehaviour
{
    public bool flight;
    public bool fortBoxy;
    private BuggyControllerModern _buggy;
    private VehicleStateModern _state;
    private Rigidbody _rb;
    private readonly StringBuilder _log = new StringBuilder();
    private Vector3 _origin;
    private Quaternion _heading;
    private GameObject _pad;
    private GameObject _ledge;

    private IEnumerator Start()
    {
        _state = GetComponent<VehicleStateModern>();
        _rb = GetComponent<Rigidbody>();
        _buggy = GetComponent<BuggyControllerModern>();
        PlayerVehicleInputModern input = GetComponent<PlayerVehicleInputModern>();
        if (input != null) input.enabled = false;

        Line("fixedDeltaTime " + Time.fixedDeltaTime);
        Line("mass " + _rb.mass + " inertia " + _rb.inertiaTensor.ToString("F2") + " inertiaRot " + _rb.inertiaTensorRotation.eulerAngles.ToString("F0")
            + " centreOfMass " + _rb.centerOfMass.ToString("F2") + " maxAngularVelocity " + _rb.maxAngularVelocity);
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            Line("collider " + c.GetType().Name + " on '" + c.name + "' enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy
                + " trigger=" + c.isTrigger + " layer=" + c.gameObject.layer + " bounds=" + c.bounds.size.ToString("F2"));
        }
        // A flat pad above the world removes Fort Boxy's ramps and bumps from the
        // measurements; a ledge 18 m higher matches the original spawn-platform drop.
        if (fortBoxy)
        {
            yield return FortBoxy();
            Finish(input, "handling-probe-fortboxy.txt");
            yield break;
        }
        _pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _pad.name = "HandlingProbePad";
        _pad.transform.position = new Vector3(0f, 1000f, 0f);
        _pad.transform.localScale = new Vector3(2000f, 1f, 2000f);
        _ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ledge.name = "HandlingProbeLedge";
        _ledge.transform.position = new Vector3(0f, 1018f, -480f);
        _ledge.transform.localScale = new Vector3(40f, 1f, 40f);
        _origin = new Vector3(0f, 1001f, -800f);
        _heading = Quaternion.identity;
        if (flight)
        {
            yield return Flight();
            Finish(input, "flight-probe.txt");
            yield break;
        }
        yield return Reset("coast");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        Vector3 start = transform.position;
        float t = 0f;
        while (_state.mph > 1f && t < 30f) { yield return Hold(Vector4.zero, false, 1f, "coast"); t += 1f; }
        Line("coast distance " + Flat(transform.position - start).ToString("F1") + " m over " + t + " s");

        yield return Reset("brakes (Fire3)");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(Vector4.zero, true, "brakes");

        yield return Reset("reverse throttle as brake, then reverse");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(new Vector4(0, -1, 0, 0), false, "reverse-brake");
        yield return Hold(new Vector4(0, -1, 0, 0), false, 4f, "reverse");

        yield return Reset("cornering: full right lock while coasting");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        float yaw0 = transform.eulerAngles.y;
        yield return Hold(new Vector4(1, 0, 0, 0), false, 2f, "steer");
        Line("heading change " + Mathf.DeltaAngle(yaw0, transform.eulerAngles.y).ToString("F0") + " deg");

        _origin = new Vector3(0f, 1019f, -495f);
        yield return Reset("drop: drive off an 18 m ledge at full throttle");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 6f, "drive/fall/land", 0.1f);
        yield return Hold(Vector4.zero, false, 2f, "settle", 0.25f);

        Finish(input, "handling-probe.txt");
    }

    private IEnumerator FortBoxy()
    {
        // Open floor south of the castle, facing away from it.
        if (!Physics.Raycast(new Vector3(0f, 200f, -300f), Vector3.down, out RaycastHit ground, 400f)) { Line("no ground found"); yield break; }
        _origin = ground.point + Vector3.up * 0.1f;
        _heading = Quaternion.Euler(0f, 180f, 0f);
        Line("floor at " + ground.point.ToString("F1") + " on '" + ground.collider.name + "'");

        yield return Reset("long throttle: 20 s from rest");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 20f, "throttle", 1f);

        yield return Reset("coast");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        Vector3 start = transform.position;
        float t = 0f;
        while (_state.mph > 1f && t < 30f) { yield return Hold(Vector4.zero, false, 1f, "coast"); t += 1f; }
        Line("coast distance " + Flat(transform.position - start).ToString("F1") + " m over " + t + " s");

        yield return Reset("brakes (Fire3)");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(Vector4.zero, true, "brakes");

        yield return Reset("reverse throttle as brake, then reverse");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        yield return StopTest(new Vector4(0, -1, 0, 0), false, "reverse-brake");
        yield return Hold(new Vector4(0, -1, 0, 0), false, 4f, "reverse");

        yield return Reset("cornering: full right lock while coasting");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle");
        float yaw0 = transform.eulerAngles.y;
        yield return Hold(new Vector4(1, 0, 0, 0), false, 2f, "steer");
        Line("heading change " + Mathf.DeltaAngle(yaw0, transform.eulerAngles.y).ToString("F0") + " deg");

        // Spawn as Game.js does, then drive straight off the spawn slab.
        _origin = World.@base != null ? World.@base.position : Vector3.up * 17f;
        while (Physics.CheckSphere(_origin, 3f)) _origin += Vector3.up;
        _origin -= Vector3.up * 0.5f;
        _heading = Quaternion.identity;
        yield return Reset("jump: from the base, full throttle off the spawn slab");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 6f, "drive/fall/land", 0.1f);
        yield return Hold(Vector4.zero, false, 3f, "settle", 0.25f);
    }

    private IEnumerator Flight()
    {
        yield return Reset("flight: take-off from the pad");
        yield return Hold(new Vector4(0, 1, 0, 0), false, 4f, "throttle", 0.5f);
        _state.specialInput = true;
        yield return Hold(new Vector4(0, 1, 0, 0), false, 0.75f, "wings opening", 0.25f);
        // A full pull-up for more than about half a second loops the buggy.
        yield return Hold(new Vector4(0, -1, 0, 0), false, 0.4f, "pull up", 0.1f);
        yield return Hold(new Vector4(0, 0.5f, 0, 0), false, 0.3f, "level off", 0.1f);
        BuggyWingsModern wings = GetComponent<BuggyWingsModern>();
        if (wings != null && wings.wings != null)
        {
            Renderer r = wings.wings.GetComponent<Renderer>();
            Line("wings active=" + wings.wings.gameObject.activeInHierarchy + " bounds=" + r.bounds.size.ToString("F2")
                + " shader=" + r.sharedMaterial.shader.name + " supported=" + r.sharedMaterial.shader.isSupported + " colour=" + r.sharedMaterial.color
                + " spec=" + r.sharedMaterial.GetColor("_SpecColor") + " cube=" + r.sharedMaterial.GetTexture("_Cube") + " layer=" + r.gameObject.layer
                + " trails=" + (wings.leftTrail != null ? wings.leftTrail.localPosition.ToString("F1") : "none") + " " + (wings.rightTrail != null ? wings.rightTrail.localPosition.ToString("F1") : "none"));
        }
        Line("== glide");
        yield return Hold(Vector4.zero, false, 2f, "glide", 0.25f);
        yield return Hold(new Vector4(1, 0, 0, 0), false, 0.5f, "roll right", 0.1f);
        yield return Hold(Vector4.zero, false, 1.5f, "level", 0.25f);
        Line("== airbrake");
        yield return Hold(Vector4.zero, true, 1.5f, "airbrake", 0.1f);
        Line("== stall: climb steeply until airspeed falls below 4 m/s");
        yield return Hold(new Vector4(0, -1, 0, 0), false, 0.35f, "stall", 0.1f);
        yield return Hold(Vector4.zero, false, 3f, "stall", 0.1f);
        Line("== descend and land with wings open");
        float t = 0f;
        while (!_state.grounded && t < 30f) { yield return Hold(Vector4.zero, false, 0.5f, "descend", 0.5f); t += 0.5f; }
        yield return Hold(Vector4.zero, false, 2f, "landed", 0.5f);
        _state.specialInput = false;
        Line("== wings closing");
        yield return Hold(Vector4.zero, false, 1f, "closing", 0.25f);
        yield return Hold(Vector4.zero, false, 2f, "drive mode", 0.5f);
    }

    private void Finish(PlayerVehicleInputModern input, string file)
    {
        Destroy(_pad);
        Destroy(_ledge);
        if (input != null) input.enabled = true;
        string path = Path.Combine(Application.dataPath, "../Logs/" + file);
        File.WriteAllText(path, _log.ToString());
        Debug.Log("[HandlingProbe] done -> " + path);
        Destroy(this);
    }

    private IEnumerator Reset(string name)
    {
        Line("== " + name);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _state.specialInput = false;
        _rb.position = _origin + Vector3.up * 0.5f;
        _rb.rotation = _heading;
        transform.SetPositionAndRotation(_rb.position, _rb.rotation);
        yield return Hold(Vector4.zero, false, 1.5f, null);
    }

    private IEnumerator StopTest(Vector4 input, bool brakes, string label)
    {
        Vector3 start = transform.position;
        float t = 0f;
        float v0 = _state.mph;
        while (_state.mph > 1f && t < 15f)
        {
            _state.input = input;
            _state.brakes = brakes;
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
        _state.brakes = false;
        Line(label + ": " + v0.ToString("F0") + " MPH to <1 MPH in " + t.ToString("F2") + " s over " + Flat(transform.position - start).ToString("F1") + " m");
    }

    private IEnumerator Hold(Vector4 input, bool brakes, float seconds, string label, float sample = 0.5f)
    {
        float t = 0f, next = sample;
        while (t < seconds)
        {
            _state.input = input;
            _state.brakes = brakes;
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            if (label != null && t >= next - 1e-4f)
            {
                next += sample;
                Vector3 e = transform.eulerAngles;
                Line(label + " t=" + t.ToString("F1") + " mph=" + _state.mph.ToString("F0") + " alt=" + _state.altitude.ToString("F1")
                    + " pitch=" + Mathf.DeltaAngle(0, e.x).ToString("F0") + " roll=" + Mathf.DeltaAngle(0, e.z).ToString("F0")
                    + " vy=" + _rb.linearVelocity.y.ToString("F1") + " yawRate=" + (_rb.angularVelocity.y * Mathf.Rad2Deg).ToString("F0")
                    + (flight ? " wing=" + _buggy.wingState.ToString("F2") + (_buggy.wingOpen ? " open" : " shut") + " grounded=" + _state.grounded
                        + " susp=" + _buggy.suspensionRange.ToString("F2") + " drag=" + _rb.linearDamping.ToString("F2") + " angDrag=" + _rb.angularDamping.ToString("F1") : ""));
            }
        }
    }

    private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    private void Line(string s) { _log.AppendLine(s); }
}

// Logs what decides the world's look: skybox, camera clear mode, and the base marker's materials.
public static class SceneLookProbe
{
    [MenuItem("Tools/MarsXPLR/Log Scene Look")]
    private static void Log()
    {
        var sb = new StringBuilder();
        Material sky = RenderSettings.skybox;
        sb.AppendLine("skybox=" + (sky != null ? sky.name + " shader=" + sky.shader.name + " supported=" + sky.shader.isSupported : "none")
            + " fog=" + RenderSettings.fog + " fogColour=" + RenderSettings.fogColor + " ambient=" + RenderSettings.ambientLight);
        foreach (Camera c in Camera.allCameras)
        {
            var sbx = c.GetComponent<Skybox>();
            sb.AppendLine("camera " + c.name + " clear=" + c.clearFlags + " bg=" + c.backgroundColor + " far=" + c.farClipPlane
                + " skyboxComponent=" + (sbx != null && sbx.material != null ? sbx.material.name + "/" + sbx.material.shader.name : "none")
                + " pos=" + c.transform.position.ToString("F1") + " euler=" + c.transform.eulerAngles.ToString("F1"));
        }
        if (Game.Settings != null) sb.AppendLine("settings camMode=" + Game.Settings.camMode + " camChase=" + Game.Settings.camChase + " camDist=" + Game.Settings.camDist + " flightCam=" + Game.Settings.flightCam);
        if (Game.Player != null && Camera.main != null)
        {
            Transform p = Game.Player.transform;
            Vector3 local = p.InverseTransformPoint(Camera.main.transform.position);
            var rb = Game.Player.GetComponent<Rigidbody>();
            sb.AppendLine("camera offset in buggy space=" + local.ToString("F2") + " buggy euler=" + p.eulerAngles.ToString("F1")
                + " velocity=" + (rb != null ? rb.linearVelocity.ToString("F2") : "-"));
        }
        GameObject b = GameObject.Find("Base");
        if (b != null)
        {
            foreach (Renderer r in b.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("base " + r.name + " material=null"); continue; }
                    sb.AppendLine("base " + r.name + " active=" + r.gameObject.activeInHierarchy + " enabled=" + r.enabled + " " + r.GetType().Name
                        + " material=" + m.name + " shader=" + m.shader.name + " supported=" + m.shader.isSupported
                        + " tex=" + (m.HasProperty("_MainTex") ? m.mainTexture : null) + " colour=" + (m.HasProperty("_Color") ? m.color.ToString() : "-"));
                }
            }
            foreach (ParticleSystem ps in b.GetComponentsInChildren<ParticleSystem>(true)) sb.AppendLine("base particles " + ps.name + " playing=" + ps.isPlaying + " count=" + ps.particleCount + " at " + ps.transform.position.ToString("F1"));
            foreach (Light l in b.GetComponentsInChildren<Light>(true)) sb.AppendLine("base light " + l.name + " " + l.type + " colour=" + l.color + " range=" + l.range + " enabled=" + l.enabled);
            foreach (Component c in b.GetComponentsInChildren<Component>(true)) if (c == null) sb.AppendLine("base has a missing script");
        }
        var sight = Object.FindAnyObjectByType<LegacyLaserSightController>();
        if (sight != null)
        {
            Transform veh = sight.transform;
            sb.AppendLine("vehicle forward=" + veh.forward.ToString("F2") + " pos=" + veh.position.ToString("F1"));
            foreach (ParticleSystem ps in sight.GetComponentsInChildren<ParticleSystem>())
            {
                var parts = new ParticleSystem.Particle[ps.particleCount];
                int n = ps.GetParticles(parts);
                var line = new StringBuilder("beam " + ps.transform.parent.name + " parent localPos=" + ps.transform.parent.localPosition.ToString("F2")
                    + " localRot=" + ps.transform.parent.localEulerAngles.ToString("F1") + " n=" + n + " ahead(m):");
                for (int i = 0; i < n; i++)
                {
                    Vector3 world = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local ? ps.transform.TransformPoint(parts[i].position) : parts[i].position;
                    line.Append(" " + Vector3.Dot(world - veh.position, veh.forward).ToString("F0"));
                }
                sb.AppendLine(line.ToString());
            }
        }
        if (Camera.main != null) sb.AppendLine("screen brightness (0-255, mean RGB) " + MeasureBrightness(Camera.main));
        string path = Path.Combine(Application.dataPath, "../Logs/scene-look.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log("[SceneLook] -> " + path);
    }

    // Logs the buggy's Trail1/Trail2 settings as Unity 6 imported them, for
    // comparison with the original Buggy.prefab and WhiteDot.mat values.
    [MenuItem("Tools/MarsXPLR/Log Buggy Trails")]
    private static void LogTrails()
    {
        var sb = new StringBuilder();
        foreach (TrailRenderer t in Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine("trail " + t.transform.parent?.name + "/" + t.name + " active=" + t.gameObject.activeInHierarchy + " enabled=" + t.enabled + " emitting=" + t.emitting
                + " localPos=" + t.transform.localPosition.ToString("F2") + " layer=" + t.gameObject.layer);
            sb.AppendLine("  time=" + t.time + " minVertexDistance=" + t.minVertexDistance + " width=" + t.startWidth + "/" + t.endWidth + " widthMultiplier=" + t.widthMultiplier
                + " alignment=" + t.alignment + " textureMode=" + t.textureMode + " cornerVertices=" + t.numCornerVertices + " capVertices=" + t.numCapVertices
                + " shadows=" + t.shadowCastingMode + " positions=" + t.positionCount);
            var g = t.colorGradient;
            var colours = new StringBuilder("  gradient mode=" + g.mode + " colour:");
            foreach (GradientColorKey k in g.colorKeys) colours.Append(" " + k.time.ToString("F2") + "=" + k.color);
            colours.Append(" alpha:");
            foreach (GradientAlphaKey k in g.alphaKeys) colours.Append(" " + k.time.ToString("F2") + "=" + k.alpha.ToString("F2"));
            sb.AppendLine(colours.ToString());
            foreach (Material m in t.sharedMaterials)
            {
                if (m == null) { sb.AppendLine("  material=null"); continue; }
                sb.AppendLine("  material=" + m.name + " shader=" + m.shader.name + " supported=" + m.shader.isSupported + " queue=" + m.renderQueue
                    + " tex=" + (m.HasProperty("_MainTex") ? m.mainTexture : null) + " tint=" + (m.HasProperty("_TintColor") ? m.GetColor("_TintColor").ToString() : "-"));
            }
        }
        if (sb.Length == 0) sb.AppendLine("no TrailRenderer found");
        string path = Path.Combine(Application.dataPath, "../Logs/buggy-trails.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log("[SceneLook] -> " + path);
    }

    // Samples the lighting state and screen brightness every 0.5 s for 30 s,
    // to find what changes the picture's brightness during play.
    [MenuItem("Tools/MarsXPLR/Log Lighting Over Time")]
    private static void LogLightingOverTime()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SceneLook] Enter Play mode first."); return; }
        var sb = new StringBuilder();
        double start = EditorApplication.timeSinceStartup, next = start;
        string path = Path.Combine(Application.dataPath, "../Logs/lighting-over-time.txt");
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            double now = EditorApplication.timeSinceStartup;
            if (!Application.isPlaying || now - start > 30.0)
            {
                EditorApplication.update -= tick;
                File.WriteAllText(path, sb.ToString());
                Debug.Log("[SceneLook] -> " + path);
                return;
            }
            if (now < next) return;
            next += 0.5;
            Camera cam = Camera.main;
            var line = new StringBuilder();
            line.Append("t=" + (now - start).ToString("F1") + " fog=" + RenderSettings.fog + " fogCol=" + RenderSettings.fogColor + " dens=" + RenderSettings.fogDensity.ToString("F5")
                + " amb=" + RenderSettings.ambientLight + " mode=" + RenderSettings.ambientMode);
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                line.Append(" light[" + l.name + " " + l.type + " i=" + l.intensity.ToString("F2") + " on=" + (l.enabled && l.gameObject.activeInHierarchy) + "]");
            var sight = Object.FindAnyObjectByType<LegacyLaserSightController>();
            if (sight != null)
            {
                var rb = sight.GetComponent<Rigidbody>();
                var vs = sight.GetComponent<VehicleStateModern>();
                line.Append(" speed=" + (rb != null ? rb.linearVelocity.magnitude.ToString("F1") : "-"));
                foreach (ParticleSystem ps in sight.GetComponentsInChildren<ParticleSystem>(true))
                    line.Append(" ps[" + ps.transform.parent.name + "/" + ps.name + " active=" + ps.gameObject.activeInHierarchy + " playing=" + ps.isPlaying
                        + " emit=" + ps.emission.enabled + " n=" + ps.particleCount + "]");
            }
            if (cam != null)
            {
                line.Append(" cam=" + cam.transform.position.ToString("F1") + " clear=" + cam.clearFlags);
                foreach (Behaviour b in cam.GetComponents<Behaviour>()) if (b != null && !(b is Transform)) line.Append(" " + b.GetType().Name + "=" + b.enabled);
                line.Append(" " + MeasureBrightness(cam));
            }
            sb.AppendLine(line.ToString());
        };
        EditorApplication.update += tick;
        Debug.Log("[SceneLook] Sampling lighting for 30 s");
    }

    // Renders the camera off screen and reports the mean brightness of the bottom
    // third (usually floor) and the centre, for comparison with Wine screenshots.
    private static string MeasureBrightness(Camera cam)
    {
        const int w = 320, h = 200;
        var rt = RenderTexture.GetTemporary(w, h, 24);
        RenderTexture previousTarget = cam.targetTexture, previousActive = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        cam.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);
        Color[] px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        float Mean(int x0, int x1, int y0, int y1)
        {
            float sum = 0f; int n = 0;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) { Color c = px[y * w + x]; sum += (c.r + c.g + c.b) / 3f; n++; }
            return sum / n * 255f;
        }
        return "bottomThird=" + Mean(0, w, 0, h / 3).ToString("F0") + " centre=" + Mean(w / 3, 2 * w / 3, h / 3, 2 * h / 3).ToString("F0");
    }
}

// Sets the saved render level (1 Fastest to 6 Fantastic, as in Settings.js)
// and applies it to a running game.
public static class RenderLevelProbe
{
    [MenuItem("Tools/MarsXPLR/Render Level/Simple (3)")] private static void Simple() => Set(3);
    [MenuItem("Tools/MarsXPLR/Render Level/Good (4)")] private static void Good() => Set(4);
    [MenuItem("Tools/MarsXPLR/Render Level/Fantastic (6)")] private static void Fantastic() => Set(6);

    private static void Set(int level)
    {
        PlayerPrefs.SetInt("renderLevel", level);
        PlayerPrefs.Save();
        if (Game.Settings != null)
        {
            Game.Settings.renderLevel = level;
            Game.Settings.updatePrefs();
        }
        Debug.Log("[RenderLevel] " + level);
    }
}
