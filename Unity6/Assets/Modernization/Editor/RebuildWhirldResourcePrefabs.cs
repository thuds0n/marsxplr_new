using UnityEditor;
using UnityEngine;

// Replaces the missing UnityScript components on the Whirld Resources Base and
// Floor prefabs with their C# ports, using the values the original prefabs
// stored (read from the Unity 4.6 copies in unity/). Saving re-serialises both
// prefabs from their Unity 2.5 binary format; safe to run again.
public static class RebuildWhirldResourcePrefabs
{
    private const string BasePath = "Assets/Whirld/Resources/Base.prefab";
    private const string FloorPath = "Assets/Whirld/Resources/Floor.prefab";

    [MenuItem("Tools/MarsXPLR/Rebuild Base and Floor Prefabs")]
    public static void Rebuild()
    {
        RebuildBase();
        RebuildFloor();
        AssetDatabase.SaveAssets();
        Debug.Log("[RebuildWhirldResourcePrefabs] Rebuilt " + BasePath + " and " + FloorPath);
    }

    // Buggy.prefab and Vehicle.prefab are read only as sources of meshes,
    // materials, trails and the laser aimer's child objects; their UnityScript
    // components (Buggy.js, VehicleData.js, VehicleTrigger.js, WhirldLOD.js,
    // Vehicle.js) are ported elsewhere, and the originals in unity/ keep their
    // values. Removing the missing components stops the warnings logged when
    // the Game scene loads them.
    [MenuItem("Tools/MarsXPLR/Remove Missing Scripts From Buggy and Vehicle Prefabs")]
    public static void StripVehiclePrefabs()
    {
        foreach (string path in new[] { "Assets/Martian Assets/Vehicles/Buggy/Buggy.prefab", "Assets/Martian Assets/Vehicles/Prefabs/Vehicle.prefab" })
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int removed = RemoveMissing(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[RebuildWhirldResourcePrefabs] " + path + ": removed " + removed + " missing scripts");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void RebuildBase()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BasePath);
        try
        {
            int removed = RemoveMissing(root);
            // WhirldLOD.js: lodObjs [LOD1, LOD0], lodLevMax 3.
            if (root.GetComponent<BaseMarkerModern>() == null) root.AddComponent<BaseMarkerModern>().lodLevMax = 3;
            // BaseSimple.js on both cylinders, upMode false.
            foreach (string part in new[] { "LOD1/Outer", "LOD1/Inner" })
            {
                Transform t = root.transform.Find(part);
                if (t == null) { Debug.LogError("[RebuildWhirldResourcePrefabs] Base has no " + part); continue; }
                if (t.GetComponent<BaseSimpleModern>() == null) t.gameObject.AddComponent<BaseSimpleModern>().upMode = false;
            }
            PrefabUtility.SaveAsPrefabAsset(root, BasePath);
            Debug.Log("[RebuildWhirldResourcePrefabs] Base: removed " + removed + " missing scripts");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RebuildFloor()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FloorPath);
        try
        {
            int removed = RemoveMissing(root);
            // WhirldObject.js: one parameter, "Texture", with no texture set.
            var whirldObject = root.GetComponent<WhirldObjectModern>();
            if (whirldObject == null)
            {
                whirldObject = root.AddComponent<WhirldObjectModern>();
                whirldObject.data = new[] { new WhirldObjectModern.WhirldData { n = "Texture", v = "" } };
            }
            // FloorController.js: whirldObject on the root, floorObject the Plane child.
            var controller = root.GetComponent<FloorControllerModern>();
            if (controller == null) controller = root.AddComponent<FloorControllerModern>();
            controller.whirldObject = whirldObject;
            Transform plane = root.transform.Find("Plane");
            if (plane == null) Debug.LogError("[RebuildWhirldResourcePrefabs] Floor has no Plane");
            else controller.floorObject = plane.gameObject;
            PrefabUtility.SaveAsPrefabAsset(root, FloorPath);
            Debug.Log("[RebuildWhirldResourcePrefabs] Floor: removed " + removed + " missing scripts");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int RemoveMissing(GameObject root)
    {
        int removed = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        return removed;
    }
}
