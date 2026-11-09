using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila el juego para navegador (WebGL) en la carpeta "docs" del proyecto, lista para GitHub Pages.
// Desde el menú: Tiny Swords → Compilar para navegador (WebGL).
// Desde la terminal: Unity -batchmode -quit -projectPath <proyecto> -executeMethod TinySwordsWebBuild.Build
public static class TinySwordsWebBuild
{
    const string OutDir = "docs";

    [MenuItem("Tiny Swords/Compilar para navegador (WebGL)", priority = 50)]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log("Tiny Swords: compilando WebGL con " + scenes.Length + " escenas");

        // GitHub Pages no envía los encabezados de compresión, así que el navegador descomprime por su cuenta.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;

        if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log($"Tiny Swords: resultado {summary.result}, tamaño {summary.totalSize / (1024 * 1024)} MB, errores {summary.totalErrors}");
        if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
