using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SinglePlayerPlayableBootstrap : MonoBehaviour
{
    public bool enabledForSinglePlayer = true;
    public Vector3 spawnOffset = new Vector3(0f, 4f, 0f);
    public GameObject playerVehiclePrefab;
    public GameObject playerVehicleModelFallbackPrefab;
    public GameObject legacyVehicleRigPrefab;
    // Original buggy wheel, axle, and tyre-track assets for BuggyPresentationModern.
    public GameObject buggyWheelPrefab;
    public GameObject buggyAxelPrefab;
    public Material buggySkidmarkMaterial;
    public bool tryLegacyBuggyPrefab = false;
    // BuggyControllerModern with the original body collider; disable to fall back to SimplePlayerController.
    public bool useModernBuggyController = true;

    private float _nextAttemptTime;

    private void Awake()
    {
#if UNITY_EDITOR
        if (legacyVehicleRigPrefab == null)
        {
            legacyVehicleRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Prefabs/Vehicle.prefab");
            if (legacyVehicleRigPrefab != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
        if (buggyWheelPrefab == null || buggyAxelPrefab == null || buggySkidmarkMaterial == null)
        {
            if (buggyWheelPrefab == null) buggyWheelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Buggy/Buggy Wheel.prefab");
            if (buggyAxelPrefab == null) buggyAxelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Martian Assets/Vehicles/Buggy/Buggy Axel.prefab");
            if (buggySkidmarkMaterial == null) buggySkidmarkMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Martian Assets/Vehicles/Buggy/Components/Tracks/skidmark.mat");
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private void Update()
    {
        if (!enabledForSinglePlayer || ModernizationConfig.MultiplayerEnabled)
        {
            return;
        }

        if (World.loading || Time.time < _nextAttemptTime)
        {
            return;
        }

        _nextAttemptTime = Time.time + 0.5f;

        if (Game.Player != null)
        {
            return;
        }

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Vector3 spawn = Vector3.up * 8f;
        Quaternion spawnRot = Quaternion.identity;
        if (World.@base != null)
        {
            spawn = World.@base.position + spawnOffset;
            spawnRot = World.@base.rotation;
        }

        // Always create a clean root player object for control/physics.
        GameObject go;
        Renderer rootRenderer = null;
        if (useModernBuggyController)
        {
            go = new GameObject();
            var body = go.AddComponent<MeshCollider>();
            body.sharedMesh = BuggyColliderShape.GetMesh();
            body.convex = true;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            rootRenderer = go.GetComponent<Renderer>();
            if (rootRenderer != null)
            {
                rootRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                rootRenderer.sharedMaterial.color = new Color(0.8f, 0.25f, 0.2f, 1f);
            }
        }

        go.name = "Player";
        go.tag = "Player";
        go.transform.position = spawn;
        go.transform.rotation = spawnRot;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;

        if (useModernBuggyController)
        {
            go.AddComponent<BuggyControllerModern>();
            go.AddComponent<PlayerVehicleInputModern>();
            var presentation = go.AddComponent<BuggyPresentationModern>();
            presentation.wheelPrefab = buggyWheelPrefab;
            presentation.axelPrefab = buggyAxelPrefab;
            presentation.skidmarkMaterial = buggySkidmarkMaterial;
        }
        else
        {
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.05f;
            go.AddComponent<SimplePlayerController>();
        }
        go.AddComponent<PlayerLaserModern>();
        var vehicleState = go.GetComponent<VehicleStateModern>();
        if (go.GetComponent<FallReset>() == null)
        {
            go.AddComponent<FallReset>();
        }

        bool usingBuggyVisual = false;
        GameObject attachedVisual = null;

        // Try cleaner model-only fallback first.
        GameObject fallbackVisual = TrySpawnFallbackModel(go.transform, spawn, spawnRot);
        if (fallbackVisual != null && HasVisibleMesh(fallbackVisual))
        {
            usingBuggyVisual = true;
            attachedVisual = fallbackVisual;
        }
        else if (fallbackVisual != null)
        {
            Destroy(fallbackVisual);
        }

        // Legacy buggy prefab still contains missing UnityScript components.
        // Keep this opt-in so default play mode is warning-free.
        if (!usingBuggyVisual && tryLegacyBuggyPrefab && playerVehiclePrefab != null)
        {
            GameObject visual = Instantiate(playerVehiclePrefab, spawn, spawnRot);
            if (visual != null)
            {
                visual.name = "BuggyVisual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;

                SanitizeVisualInstance(visual);
                EnsureSpawnVisuals(visual);
                usingBuggyVisual = HasVisibleMesh(visual);

                if (!usingBuggyVisual)
                {
                    Destroy(visual);
                }
                else
                {
                    attachedVisual = visual;
                }
            }
        }

        if (!usingBuggyVisual)
        {
            EnsureSpawnVisuals(go);
        }
        else
        {
            // Keep physics capsule, but hide it when we have a proper vehicle visual.
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }
        }

        if (attachedVisual != null)
        {
            AlignVisualToPlayerCollider(go, attachedVisual);
        }

        AttachLegacyLaserSight(go);

        if (vehicleState != null)
        {
            vehicleState.vehicleId = 0;
            vehicleState.camOffset = 2f;
            vehicleState.rideTarget = go.transform;
            vehicleState.SetFlightMode(false);
            vehicleState.SyncFromRigidbody(rb);
        }

        Game.Player = go;
        if (Game.Settings != null)
        {
            Game.Settings.simplified = false;
            Game.Settings.updatePrefs();
        }

        if (Game.Controller != null)
        {
            Game.Controller.msg(usingBuggyVisual ? "Single-player buggy spawned." : "Single-player fallback pawn spawned.", 2);
        }
    }

    private void AttachLegacyLaserSight(GameObject playerRoot)
    {
        if (playerRoot == null || legacyVehicleRigPrefab == null) return;
        if (playerRoot.transform.Find("LaserAim") != null) return;

        GameObject temp = Instantiate(legacyVehicleRigPrefab);
        temp.name = "__LegacyVehicleRigTemp";
        temp.SetActive(false);

        GameObject aimer = CloneNamedChild(temp.transform, "LaserAim", playerRoot.transform);
        GameObject aimerLocked = CloneNamedChild(temp.transform, "LaserAimLocked", playerRoot.transform);
        GameObject lockMarker = CloneNamedChild(temp.transform, "LaserLock", playerRoot.transform);
        Destroy(temp);

        if (aimer == null)
        {
            aimer = CreateLaserAnchor(playerRoot.transform, "LaserAim", new Vector3(0f, 0.8f, 1.5f));
        }
        if (aimerLocked == null)
        {
            aimerLocked = CreateLaserAnchor(playerRoot.transform, "LaserAimLocked", new Vector3(0f, 0.8f, 1.5f));
        }
        if (lockMarker == null)
        {
            lockMarker = CreateLaserAnchor(playerRoot.transform, "LaserLock", new Vector3(0f, 0.8f, 6f));
        }

        if (aimer == null && aimerLocked == null && lockMarker == null) return;

        var controller = playerRoot.GetComponent<LegacyLaserSightController>();
        if (controller == null)
        {
            controller = playerRoot.AddComponent<LegacyLaserSightController>();
        }

        controller.laserAimer = aimer;
        controller.laserAimerLocked = aimerLocked;
        controller.laserLockMarker = lockMarker;
        controller.vehicleId = 0;
        controller.camOffset = 2f;
    }

    private GameObject CreateLaserAnchor(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private GameObject CloneNamedChild(Transform sourceRoot, string childName, Transform targetRoot)
    {
        Transform source = FindChildRecursive(sourceRoot, childName);
        if (source == null) return null;

        GameObject clone = Instantiate(source.gameObject, targetRoot);
        clone.name = childName;
        clone.transform.localPosition = source.localPosition;
        clone.transform.localRotation = source.localRotation;
        clone.transform.localScale = source.localScale;
        SanitizeSightObject(clone);
        return clone;
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindChildRecursive(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    private void SanitizeSightObject(GameObject go)
    {
        if (go == null) return;

        var rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Destroy(rigidbodies[i]);
        }

        var colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        var scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] is LegacyLaserSightController) continue;
            Destroy(scripts[i]);
        }
    }

    private GameObject TrySpawnFallbackModel(Transform parent, Vector3 spawn, Quaternion spawnRot)
    {
        if (playerVehicleModelFallbackPrefab == null) return null;

        GameObject visual = Instantiate(playerVehicleModelFallbackPrefab, spawn, spawnRot);
        if (visual == null) return null;

        visual.name = "BuggyVisualFallback";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        SanitizeVisualInstance(visual);
        PrepareVisualForAttachment(visual);
        EnsureSpawnVisuals(visual);
        return visual;
    }

    private void SanitizeVisualInstance(GameObject visual)
    {
        if (visual == null) return;

        // Keep this purely visual: no legacy physics/network scripts running.
        MonoBehaviour[] scripts = visual.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
            {
                scripts[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void PrepareVisualForAttachment(GameObject visual)
    {
        if (visual == null) return;
        SetLayerRecursively(visual.transform, 0); // Default layer

        // Normalize old asset scale so it reads as a vehicle-sized object in modern scene units.
        if (!useModernBuggyController)
        {
            float scaleFactor = EstimateScaleToTargetLength(visual, 5.2f);
            if (scaleFactor > 0.001f)
            {
                visual.transform.localScale *= scaleFactor;
            }
        }

        visual.transform.localPosition = Vector3.zero;
    }

    private void AlignVisualToPlayerCollider(GameObject playerRoot, GameObject visual)
    {
        if (playerRoot == null || visual == null) return;

        float lowestY = float.MaxValue;
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled) continue;
            var b = renderers[i].bounds;
            Vector3 localMin = playerRoot.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            if (localMin.y < lowestY) lowestY = localMin.y;
        }

        if (lowestY == float.MaxValue) return;

        if (useModernBuggyController && FitToOriginalBuggyBody(playerRoot, visual)) return;

        // Align to the player collider bottom so visual stays grounded across scale changes.
        float desiredLowest = -0.95f;
        var capsule = playerRoot.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Vector3 worldBottom = new Vector3(playerRoot.transform.position.x, capsule.bounds.min.y, playerRoot.transform.position.z);
            desiredLowest = playerRoot.transform.InverseTransformPoint(worldBottom).y + 0.04f;
        }

        float delta = Mathf.Clamp(desiredLowest - lowestY, -0.15f, 1.2f);
        visual.transform.localPosition += new Vector3(0f, delta, 0f);
    }

    // Scales and places the visual so its detailed body matches the original
    // Body mesh (Buggy.jas, scaled by 2): 1.88 long, bottom at -0.18, centred
    // at z -0.02. Wings stay hidden while closed and only the detailed LOD shows.
    private bool FitToOriginalBuggyBody(GameObject playerRoot, GameObject visual)
    {
        Transform lod0 = FindChildRecursive(visual.transform, "Lod0");
        Renderer body = lod0 != null ? lod0.GetComponent<Renderer>() : null;
        if (body == null) return false;

        foreach (string hidden in new[] { "Wings", "Wings_1", "Lod1" })
        {
            Transform t = FindChildRecursive(visual.transform, hidden);
            if (t != null) t.gameObject.SetActive(false);
        }

        Bounds local = BoundsInSpace(body, playerRoot.transform);
        if (local.size.z < 0.001f) return false;
        visual.transform.localScale *= 1.88f / local.size.z;

        local = BoundsInSpace(body, playerRoot.transform);
        visual.transform.localPosition += new Vector3(-local.center.x, -0.18f - local.min.y, -0.02f - local.center.z);
        return true;
    }

    private static Bounds BoundsInSpace(Renderer renderer, Transform space)
    {
        Bounds world = renderer.bounds;
        Vector3 min = world.min, max = world.max;
        var result = new Bounds(space.InverseTransformPoint(min), Vector3.zero);
        for (int i = 1; i < 8; i++)
        {
            result.Encapsulate(space.InverseTransformPoint(new Vector3(
                (i & 1) != 0 ? max.x : min.x,
                (i & 2) != 0 ? max.y : min.y,
                (i & 4) != 0 ? max.z : min.z)));
        }
        return result;
    }

    private void EnsureSpawnVisuals(GameObject go)
    {
        if (go == null) return;

        go.SetActive(true);
        SetActiveRecursively(go.transform);

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
        }

        bool anyEnabled = HasVisibleMesh(go);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled) continue;
            if (renderers[i] is MeshRenderer)
            {
                var mf = renderers[i].GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    anyEnabled = true;
                }
            }
            else if (renderers[i] is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                anyEnabled = true;
            }
        }

        if (renderers.Length == 0 || !anyEnabled)
        {
            BuildBuggyProxyVisual(go.transform);
        }
    }

    private bool HasVisibleMesh(GameObject go)
    {
        if (go == null) return false;
        var meshes = go.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++)
        {
            if (!meshes[i].enabled) continue;
            var mf = meshes[i].GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return true;
        }

        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i].enabled && skinned[i].sharedMesh != null) return true;
        }

        return false;
    }

    private float EstimateScaleToTargetLength(GameObject go, float targetLength)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 1f;

        bool haveBounds = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled) continue;
            if (!haveBounds)
            {
                b = renderers[i].bounds;
                haveBounds = true;
            }
            else
            {
                b.Encapsulate(renderers[i].bounds);
            }
        }

        if (!haveBounds) return 1f;
        float currentLength = Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
        float raw = targetLength / currentLength;
        return Mathf.Clamp(raw, 0.2f, 12f);
    }

    private void SetActiveRecursively(Transform root)
    {
        root.gameObject.SetActive(true);
        for (int i = 0; i < root.childCount; i++)
        {
            SetActiveRecursively(root.GetChild(i));
        }
    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private void BuildBuggyProxyVisual(Transform parent)
    {
        var buggy = new GameObject("BuggyProxy");
        buggy.transform.SetParent(parent, false);
        buggy.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        buggy.transform.localRotation = Quaternion.identity;

        var bodyMat = NewStandardMaterial(new Color(0.82f, 0.27f, 0.21f, 1f));
        var darkMat = NewStandardMaterial(new Color(0.1f, 0.11f, 0.13f, 1f));
        var glassMat = NewStandardMaterial(new Color(0.5f, 0.75f, 0.9f, 0.7f));
        var glowMat = NewStandardMaterial(new Color(1f, 0.95f, 0.65f, 1f));
        if (glowMat != null)
        {
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", new Color(0.7f, 0.65f, 0.25f, 1f));
        }

        CreatePart("Chassis", PrimitiveType.Cube, buggy.transform,
            new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0.5f, 3.2f), bodyMat);
        CreatePart("Nose", PrimitiveType.Cube, buggy.transform,
            new Vector3(0f, 0.05f, 1.85f), new Vector3(1.5f, 0.35f, 0.9f), bodyMat);
        CreatePart("Rear", PrimitiveType.Cube, buggy.transform,
            new Vector3(0f, 0f, -1.8f), new Vector3(1.5f, 0.35f, 0.8f), bodyMat);
        CreatePart("Cockpit", PrimitiveType.Cube, buggy.transform,
            new Vector3(0f, 0.42f, 0.25f), new Vector3(1.15f, 0.45f, 1.2f), glassMat);
        CreatePart("WingL", PrimitiveType.Cube, buggy.transform,
            new Vector3(-1.15f, 0f, 0.2f), new Vector3(0.22f, 0.08f, 2.2f), darkMat);
        CreatePart("WingR", PrimitiveType.Cube, buggy.transform,
            new Vector3(1.15f, 0f, 0.2f), new Vector3(0.22f, 0.08f, 2.2f), darkMat);
        CreatePart("HeadlightL", PrimitiveType.Sphere, buggy.transform,
            new Vector3(-0.43f, 0.06f, 2.22f), new Vector3(0.18f, 0.18f, 0.18f), glowMat);
        CreatePart("HeadlightR", PrimitiveType.Sphere, buggy.transform,
            new Vector3(0.43f, 0.06f, 2.22f), new Vector3(0.18f, 0.18f, 0.18f), glowMat);

        CreateWheel("WheelFL", buggy.transform, new Vector3(-1.05f, -0.35f, 1.15f), darkMat);
        CreateWheel("WheelFR", buggy.transform, new Vector3(1.05f, -0.35f, 1.15f), darkMat);
        CreateWheel("WheelRL", buggy.transform, new Vector3(-1.05f, -0.35f, -1.25f), darkMat);
        CreateWheel("WheelRR", buggy.transform, new Vector3(1.05f, -0.35f, -1.25f), darkMat);
    }

    private void CreateWheel(string name, Transform parent, Vector3 localPos, Material material)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.43f, 0.2f, 0.43f);

        var c = wheel.GetComponent<Collider>();
        if (c != null) Destroy(c);
        var r = wheel.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = material;
    }

    private void CreatePart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
    {
        var go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        var c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = material;
    }

    private Material NewStandardMaterial(Color color)
    {
        var shader = Shader.Find("Standard");
        if (shader == null) return null;
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}
