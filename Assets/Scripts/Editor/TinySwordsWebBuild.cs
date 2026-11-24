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
        if (summary.result == BuildResult.Succeeded) FinishPage();
        Debug.Log($"Tiny Swords: resultado {summary.result}, tamaño {summary.totalSize / (1024 * 1024)} MB, errores {summary.totalErrors}");
        if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    // Deja la página lista: título, ícono y pantalla completa en iPhone/Android.
    static void FinishPage()
    {
        File.WriteAllText(Path.Combine(OutDir, ".nojekyll"), "");
        string icon = "Assets/Icono/completo.png";
        if (File.Exists(icon)) File.Copy(icon, Path.Combine(OutDir, "icono.png"), true);
        File.WriteAllText(Path.Combine(OutDir, "manifest.webmanifest"),
            "{\"name\":\"Tiny Swords: La Amenaza Goblin\",\"short_name\":\"Tiny Swords\",\"display\":\"fullscreen\",\"orientation\":\"landscape\"," +
            "\"background_color\":\"#1a0f14\",\"theme_color\":\"#1a0f14\",\"start_url\":\"./\",\"icons\":[{\"src\":\"icono.png\",\"sizes\":\"1024x1024\",\"type\":\"image/png\"}]}\n");

        string path = Path.Combine(OutDir, "index.html");
        string html = File.ReadAllText(path);
        html = System.Text.RegularExpressions.Regex.Replace(html, "<title>.*?</title>", "<title>Tiny Swords: La Amenaza Goblin</title>");
        html = System.Text.RegularExpressions.Regex.Replace(html, "(id=\"unity-build-title\">).*?<", "$1Tiny Swords: La Amenaza Goblin<");
        html = html.Replace("<html lang=\"en-us\">", "<html lang=\"es\">");
        html = html.Replace("</head>", Head + "  </head>");
        html = html.Replace("<body>", "<body>\n    <div id=\"girar\"><span>↻</span>Gira tu celular para jugar</div>");
        // iPad incluido: se presenta como Mac pero tiene pantalla táctil.
        html = html.Replace("if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
            "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) {\n" +
            "        document.documentElement.classList.add('tactil');\n" +
            "        config.arguments = ['-tactil'];\n" +
            "        config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 2);");
        html = html.Replace("initial-scale=1.0, user-scalable=no, shrink-to-fit=yes", "initial-scale=1.0, user-scalable=no");
        File.WriteAllText(path, html);
        Debug.Log("Tiny Swords: página lista, táctil " + html.Contains("config.arguments = ['-tactil']"));
    }

    const string Head = @"    <link rel=""apple-touch-icon"" href=""icono.png"">
    <link rel=""manifest"" href=""manifest.webmanifest"">
    <meta name=""apple-mobile-web-app-capable"" content=""yes"">
    <meta name=""mobile-web-app-capable"" content=""yes"">
    <meta name=""apple-mobile-web-app-title"" content=""Tiny Swords"">
    <meta name=""apple-mobile-web-app-status-bar-style"" content=""black"">
    <meta name=""theme-color"" content=""#1a0f14"">
    <style>
      #girar { display: none; }
      html.tactil, html.tactil body { margin: 0; height: 100%; overflow: hidden; background: #1a0f14; overscroll-behavior: none; touch-action: none; }
      html.tactil #unity-container { position: fixed; inset: 0; width: 100%; height: 100%; }
      html.tactil #unity-fullscreen-container, html.tactil #unity-canvas { width: 100% !important; height: 100% !important; }
      html.tactil #unity-footer { display: none; }
      html.tactil, html.tactil * { -webkit-user-select: none; user-select: none; -webkit-touch-callout: none; -webkit-tap-highlight-color: transparent; }
      @media (orientation: portrait) {
        html.tactil #girar { position: fixed; inset: 0; z-index: 10; display: flex; flex-direction: column; align-items: center; justify-content: center;
          background: #1a0f14; color: #ffd659; font: bold 24px sans-serif; text-align: center; padding: 20px; }
        html.tactil #girar span { font-size: 80px; }
      }
    </style>
";
}
