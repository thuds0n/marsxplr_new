using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Dumps what Unity imports from model files, so a converted model can be
// compared with the import it replaces: the hierarchy (names, local
// transforms, mesh and material slots) and every mesh's vertices, normals,
// UV sets and triangles, as JSON. Used by tools/jas2fbx/verify.py.
//
// Batch mode:
//   -executeMethod ModelImportProbe.DumpBatchMode
//   -probeModels "Assets/a.jas;Assets/b.fbx" -probeOut /path/out.json
public static class ModelImportProbe
{
    public static void DumpBatchMode()
    {
        string models = Arg("-probeModels");
        string output = Arg("-probeOut");
        if (string.IsNullOrEmpty(models) || string.IsNullOrEmpty(output))
        {
            Debug.LogError("[ModelImportProbe] Pass -probeModels \"path;path\" and -probeOut file.");
            EditorApplication.Exit(1);
            return;
        }

        var json = new StringBuilder("{\n");
        string[] paths = models.Split(';').Where(p => p.Length > 0).ToArray();
        for (int i = 0; i < paths.Length; i++)
        {
            if (Arg("-probeReimport") == "1")
                AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            json.Append("  ").Append(Str(paths[i])).Append(": ");
            DumpModel(paths[i], json);
            json.Append(i + 1 < paths.Length ? ",\n" : "\n");
        }
        json.Append("}\n");
        File.WriteAllText(output, json.ToString());
        Debug.Log("[ModelImportProbe] Wrote " + output);
        EditorApplication.Exit(0);
    }

    public static void DumpModel(string path, StringBuilder json)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            json.Append("null");
            return;
        }

        json.Append("{\"nodes\": [");
        bool first = true;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            json.Append(first ? "\n" : ",\n");
            first = false;
            var filter = t.GetComponent<MeshFilter>();
            var renderer = t.GetComponent<Renderer>();
            json.Append("    {\"path\": ").Append(Str(PathOf(t, root.transform)))
                .Append(", \"pos\": ").Append(Vec(t.localPosition))
                .Append(", \"rot\": ").Append(Vec(t.localRotation.eulerAngles))
                .Append(", \"scale\": ").Append(Vec(t.localScale))
                .Append(", \"mesh\": ").Append(filter != null && filter.sharedMesh != null ? Str(filter.sharedMesh.name) : "null")
                .Append(", \"materials\": ").Append(renderer != null ? renderer.sharedMaterials.Length : 0)
                .Append(", \"components\": [")
                .Append(string.Join(", ", t.GetComponents<Component>().Select(c => Str(c.GetType().Name))))
                .Append("]}");
        }
        json.Append("\n  ], \"meshes\": [");

        first = true;
        foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().OrderBy(m => m.name))
        {
            json.Append(first ? "\n" : ",\n");
            first = false;
            json.Append("    {\"name\": ").Append(Str(mesh.name))
                .Append(", \"localId\": ").Append(LocalId(mesh))
                .Append(", \"bounds\": [").Append(Vec(mesh.bounds.min)).Append(", ").Append(Vec(mesh.bounds.max)).Append("]")
                .Append(", \"vertices\": ").Append(Vecs(mesh.vertices))
                .Append(", \"normals\": ").Append(Vecs(mesh.normals))
                .Append(", \"uv\": ").Append(Vecs2(mesh.uv))
                .Append(", \"uv2\": ").Append(Vecs2(mesh.uv2))
                .Append(", \"submeshes\": [")
                .Append(string.Join(", ", Enumerable.Range(0, mesh.subMeshCount)
                    .Select(s => "[" + string.Join(",", mesh.GetTriangles(s)) + "]")))
                .Append("]}");
        }
        json.Append("\n  ]}");
    }

    private static long LocalId(UnityEngine.Object asset)
    {
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long id) ? id : 0;
    }

    private static string PathOf(Transform t, Transform root)
    {
        return t == root ? "" : (t.parent == root ? t.name : PathOf(t.parent, root) + "/" + t.name);
    }

    private static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    // Unity imports a .jas by having Cheetah 3D write Temp/ExportedCheetahFBXFile.fbx
    // and importing that. With -captureCheetahFbx dir and -probeReimport 1, each
    // .jas's intermediate FBX is copied to dir/<name>.fbx once Unity has read it
    // (before that, the file still holds the previous model).
    private class CheetahFbxCapture : AssetPostprocessor
    {
        private void OnPostprocessModel(GameObject model)
        {
            string dir = Arg("-captureCheetahFbx");
            if (string.IsNullOrEmpty(dir) || !assetPath.EndsWith(".jas", StringComparison.OrdinalIgnoreCase))
                return;
            const string exported = "Temp/ExportedCheetahFBXFile.fbx";
            if (!File.Exists(exported) || File.GetLastWriteTimeUtc(exported) < DateTime.UtcNow.AddMinutes(-1))
            {
                Debug.LogError("[ModelImportProbe] No fresh Cheetah FBX for " + assetPath);
                return;
            }
            Directory.CreateDirectory(dir);
            File.Copy(exported, Path.Combine(dir, Path.GetFileNameWithoutExtension(assetPath) + ".fbx"), true);
        }
    }

    private static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
    private static string Vec(Vector3 v) => "[" + F(v.x) + "," + F(v.y) + "," + F(v.z) + "]";
    private static string Vecs(Vector3[] a) => "[" + string.Join(",", a.Select(Vec)) + "]";
    private static string Vecs2(Vector2[] a) => "[" + string.Join(",", a.Select(v => "[" + F(v.x) + "," + F(v.y) + "]")) + "]";
    private static string Str(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
