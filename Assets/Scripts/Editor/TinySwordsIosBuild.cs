using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Genera el proyecto de Xcode para iPhone en Builds/iOS (necesita el módulo iOS de Unity y una Mac con Xcode).
// Luego se abre Builds/iOS/Unity-iPhone.xcodeproj, se elige el equipo de firma y se instala en el iPhone conectado.
// Desde la terminal: Unity -batchmode -quit -projectPath <proyecto> -buildTarget iOS -executeMethod TinySwordsIosBuild.Build
public static class TinySwordsIosBuild
{
    const string OutDir = "Builds/iOS";
    const string Version = "1.0.1";

    [MenuItem("Tiny Swords/Generar proyecto de iPhone (Xcode)", priority = 52)]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("Tiny Swords: falta el módulo iOS. Instálalo en Unity Hub → Installs → Add modules → iOS Build Support.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Igual que en Android: "RIPG2D" se conserva en computadora por el progreso guardado.
        string oldName = PlayerSettings.productName;
        bool ok = false;
        try
        {
            PlayerSettings.productName = "Tiny Swords";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.yahir19b.tinyswords");
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.hideHomeButton = false;
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;   // evita salir del juego sin querer
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icono/completo.png");
            if (icon != null)
                foreach (var kind in new[] { IconKind.Application, IconKind.Store })
                {
                    int n = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, kind).Length;
                    PlayerSettings.SetIcons(NamedBuildTarget.iOS, Enumerable.Repeat(icon, n).ToArray(), kind);
                }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            });
            ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"Tiny Swords: iOS {report.summary.result}, errores {report.summary.totalErrors}");
        }
        finally
        {
            PlayerSettings.productName = oldName;
            AssetDatabase.SaveAssets();
        }
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
