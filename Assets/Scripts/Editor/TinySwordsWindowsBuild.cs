using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila el ejecutable para Windows de 64 bits en Builds/Windows/TinySwords (necesita Windows Build Support (Mono)).
// Desde la terminal: Unity -batchmode -quit -projectPath <proyecto> -buildTarget Win64 -executeMethod TinySwordsWindowsBuild.Build
public static class TinySwordsWindowsBuild
{
    const string OutDir = "Builds/Windows/TinySwords";

    [MenuItem("Tiny Swords/Compilar para Windows (.exe)", priority = 53)]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            Debug.LogError("Tiny Swords: falta el módulo Windows Build Support (Mono) de Unity Hub.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Igual que en Android: "RIPG2D" se conserva en la Mac por el progreso guardado.
        string oldName = PlayerSettings.productName;
        bool ok = false;
        try
        {
            PlayerSettings.productName = "Tiny Swords";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icono/icono.png");
            if (icon != null)
            {
                int n = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application).Length;
                PlayerSettings.SetIcons(NamedBuildTarget.Standalone, Enumerable.Repeat(icon, n).ToArray(), IconKind.Application);
            }

            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            Directory.CreateDirectory(OutDir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutDir + "/TinySwords.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.CompressWithLz4HC
            });
            ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"Tiny Swords: Windows {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, errores {report.summary.totalErrors}");
        }
        finally
        {
            PlayerSettings.productName = oldName;
            AssetDatabase.SaveAssets();
        }
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
