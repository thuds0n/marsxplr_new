using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptScanner
{
    public static void ScanActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var results = new List<string>();

        foreach (var root in roots)
        {
            ScanGameObject(root, results);
        }

        if (results.Count == 0)
        {
            Debug.Log("[MissingScriptScanner] No missing scripts found in active scene: " + scene.path);
            return;
        }

        Debug.Log("[MissingScriptScanner] Found " + results.Count + " missing script component(s) in: " + scene.path);
        foreach (var line in results)
        {
            Debug.Log(line);
        }
    }

    private static void ScanGameObject(GameObject go, List<string> results)
    {
        var components = go.GetComponents<Component>();
        for (var i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                results.Add("[MissingScript] " + GetHierarchyPath(go) + " (component index " + i + ")");
            }
        }

        foreach (Transform child in go.transform)
        {
            ScanGameObject(child.gameObject, results);
        }
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var path = go.name;
        var current = go.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
