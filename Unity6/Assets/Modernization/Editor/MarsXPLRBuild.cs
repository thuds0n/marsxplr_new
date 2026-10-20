using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the native macOS Apple-silicon development player.
// Output: Unity6/Build/macOS-arm64/Mars Explorer.app (ignored by Git).
public static class MarsXPLRBuild
{
    private const string OutputPath = "Build/macOS-arm64/Mars Explorer.app";

    [MenuItem("Tools/MarsXPLR/Build macOS arm64 Development Player")]
    public static void BuildMacArm64DevelopmentMenu()
    {
        BuildMacArm64Development();
    }

    public static void BuildMacArm64DevelopmentBatchMode()
    {
        bool ok = false;
        try
        {
            ok = BuildMacArm64Development();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool BuildMacArm64Development()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        // Values are "x64", "ARM64" and "x64ARM64"; stored per user in Library/.
        EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", "ARM64");

        // The IMGUI HUD is laid out in pixels, as on Unity 2.5, which had no
        // Retina support; rendering at the backing scale would halve its size.
        if (PlayerSettings.macRetinaSupport)
        {
            PlayerSettings.macRetinaSupport = false;
            AssetDatabase.SaveAssets();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[MarsXPLRBuild] " + summary.result + ": " + summary.outputPath
            + " (" + summary.totalSize / (1024 * 1024) + " MB, " + summary.totalErrors + " errors, "
            + summary.totalWarnings + " warnings, " + summary.totalTime + ")");
        return summary.result == BuildResult.Succeeded;
    }
}
