using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila el APK de Android en Builds/Android/TinySwords.apk (necesita el módulo Android de Unity Hub).
// Desde el menú: Tiny Swords → Compilar para Android (APK).
// Desde la terminal: Unity -batchmode -quit -projectPath <proyecto> -buildTarget Android -executeMethod TinySwordsAndroidBuild.Build
public static class TinySwordsAndroidBuild
{
    const string OutFile = "Builds/Android/TinySwords.apk";

    [MenuItem("Tiny Swords/Compilar para Android (APK)", priority = 51)]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Tiny Swords: falta el módulo Android. Instálalo en Unity Hub → Installs → Add modules → Android Build Support.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Si Unity no trae su propio SDK, se usa el de Android Studio.
        string engine = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
        string studioSdk = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library/Android/sdk");
        if (!Directory.Exists(Path.Combine(engine, "SDK/platforms")) && Directory.Exists(studioSdk))
        {
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetString("AndroidSdkRoot", studioSdk);
        }

        // El nombre del producto se cambia solo mientras compila: en computadora el progreso
        // guardado depende de que siga siendo "RIPG2D".
        string oldName = PlayerSettings.productName;
        bool ok = false;
        try
        {
            PlayerSettings.productName = "Tiny Swords";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yahir19b.tinyswords");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.useCustomKeystore = false;   // firma de prueba: instala bien, pero no sirve para Play Store
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutFile,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.CompressWithLz4HC
            });
            ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"Tiny Swords: Android {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, errores {report.summary.totalErrors}");
        }
        finally
        {
            PlayerSettings.productName = oldName;
            AssetDatabase.SaveAssets();
        }
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
