using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HealthyRace.Editor.Build
{
    /// <summary>
    /// Automated build utility for creating standalone standalone executables.
    /// Supports batchmode continuous integration and release packaging.
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Healthy Race/Build/Windows 64-bit")]
        public static void BuildStandaloneWindows64()
        {
            var buildFolder = "Build/Windows";
            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            var buildPath = Path.Combine(buildFolder, "HealthyRace.exe");
            var scenes = new[]
            {
                "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity",
                "Assets/_Game/Scenes/VerticalSlice_Core.unity",
            };

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Starting Windows Standalone build to: {buildPath}");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            Debug.Log($"[BuildScript] Build completed with result: {summary.result}");
            Debug.Log(
                $"[BuildScript] Warnings: {summary.totalWarnings}, Errors: {summary.totalErrors}"
            );
            Debug.Log(
                $"[BuildScript] Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize} bytes"
            );

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
