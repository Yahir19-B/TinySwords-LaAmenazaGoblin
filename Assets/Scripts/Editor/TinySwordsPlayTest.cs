using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prueba automática (solo para desarrollo): cuando aparece "TinySwordsPlayTest.txt" en la raíz del proyecto,
// abre una escena, entra en Play, la deja correr, guarda estado y errores en Logs/TinySwordsPlayTest.log,
// toma una captura (Logs/TinySwordsPlayTest.png) y sale de Play.
// Contenido del archivo: "segundos velocidad escena [bot|niveles]". Con "bot" el héroe pelea solo (e invencible);
// con "niveles" el menú abre la pantalla de niveles; con "tienda", la tienda; con "compras", el nivel se juega
// con todo comprado (y bot). Después usa "reset" para borrar ese progreso de prueba.
[InitializeOnLoad]
public static class TinySwordsPlayTest
{
    const string TriggerFile = "TinySwordsPlayTest.txt";
    const string LogFile = "Logs/TinySwordsPlayTest.log";
    const string ShotFile = "Logs/TinySwordsPlayTest.png";

    static float duration = 30f;
    static float speed = 1f;
    static string scene = "Assets/Scenes/Niveles/Nivel_01.unity";
    static bool bot;
    static bool openShop;

    static float startTime = -1f;
    static bool shotTaken;
    static bool controlsClosed;
    static float botAttack;
    static double nextCheck;

    static TinySwordsPlayTest()
    {
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(TriggerFile) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        var parts = File.ReadAllText(TriggerFile).Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        File.Delete(TriggerFile);
        if (parts.Length > 0 && (parts[0] == "reset" || parts[0] == "set"))
        {
            ResetProgress();
            // "set N oro": deja completados los niveles 1..N y el oro indicado.
            if (parts[0] == "set")
            {
                int completed = int.Parse(parts[1]);
                for (int i = 1; i <= completed; i++) Progress.CompleteLevel(i, 0);
                Progress.CompleteLevel(Mathf.Max(1, completed), 0);
                PlayerPrefs.SetInt("TotalGold", int.Parse(parts[2]));
                PlayerPrefs.Save();
            }
            File.WriteAllText(LogFile, $"Progreso borrado: desbloqueado hasta {Progress.Unlocked}, oro {Progress.TotalGold}\nFIN\n");
            return;
        }
        duration = parts.Length > 0 ? float.Parse(parts[0], CultureInfo.InvariantCulture) : 30f;
        speed = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 1f;
        scene = parts.Length > 2 ? parts[2] : scene;
        bot = parts.Length > 3 && parts[3] == "bot";
        Progress.OpenLevelSelect = parts.Length > 3 && parts[3] == "niveles";
        openShop = parts.Length > 3 && parts[3] == "tienda";
        if (parts.Length > 3 && parts[3] == "compras")
        {
            // Simula tener todo comprado con la skin roja.
            foreach (var item in Shop.Items) Progress.SetItemLevel(item.id, item.MaxLevel);
            Progress.EquippedSkin = "Red";
            bot = true;
        }

        File.WriteAllText(LogFile, $"Prueba {scene} ({duration}s a {speed}x{(bot ? ", bot" : "")})\n");
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(scene);

        startTime = -1f;
        shotTaken = false;
        controlsClosed = false;
        Application.logMessageReceived -= Capture;
        Application.logMessageReceived += Capture;
        EditorApplication.update -= Watch;
        EditorApplication.update += Watch;
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tiny Swords/Herramientas/Borrar progreso guardado")]
    static void ResetProgress() => Progress.Reset();

    static void Capture(string msg, string stack, LogType type)
    {
        if (type == LogType.Log && !msg.StartsWith("Tiny Swords")) return;
        File.AppendAllText(LogFile, $"[{type}] {msg}\n{(type != LogType.Warning ? stack : "")}\n");
    }

    static void Watch()
    {
        if (!EditorApplication.isPlaying) return;
        if (EditorApplication.isPaused)
        {
            File.AppendAllText(LogFile, "(el editor estaba en pausa; se quitó la pausa)\n");
            EditorApplication.isPaused = false;
        }
        if (startTime < 0) startTime = Time.realtimeSinceStartup;
        float t = Time.realtimeSinceStartup - startTime;

        var hud = LevelHUD.Instance;
        // En la prueba se cierran los controles del nivel 1 a los 2 segundos para que el juego avance.
        if (!controlsClosed && t > 2f && hud != null && hud.controlsPanel != null && hud.controlsPanel.activeSelf)
        {
            hud.ShowControls(false);
            controlsClosed = true;
        }
        bool controlsOpen = hud != null && hud.controlsPanel != null && hud.controlsPanel.activeSelf;
        bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
        if (playing && !controlsOpen) Time.timeScale = speed;
        if (bot) Bot();
        if (openShop && t > 1f)
        {
            var menu = Object.FindAnyObjectByType<MainMenu>();
            if (menu != null && menu.shopPanel != null) menu.shopPanel.SetActive(true);
            openShop = false;
        }

        if (!shotTaken && t > duration - 2f)
        {
            ScreenCapture.CaptureScreenshot(ShotFile);
            shotTaken = true;
        }
        if (t < duration) return;

        EditorApplication.update -= Watch;
        Report();
        Application.logMessageReceived -= Capture;
        EditorApplication.ExitPlaymode();
    }

    // Piloto automático: camina hacia el goblin o casa más cercana y ataca.
    static void Bot()
    {
        var p = Player.Instance;
        if (p == null || p.IsDead || Time.timeScale == 0) return;
        p.Health.Heal(1000);
        var target = Health.FindClosest(p.transform.position, Team.Goblins, 1000f);
        if (target == null) return;
        p.transform.position = Vector2.MoveTowards(p.transform.position, target.transform.position, 4f * Time.deltaTime);
        botAttack -= Time.deltaTime;
        if (botAttack <= 0 && target.DistanceFrom(p.transform.position) < 1.2f)
        {
            botAttack = 0.4f;
            CombatUtils.DamageArea(p.transform.position, 1.5f, Team.Knights, 40f);
        }
    }

    static void Report()
    {
        var sb = new System.Text.StringBuilder($"\n=== Estado tras {duration}s reales ({Time.time:0}s de juego) ===\n");
        var lm = LevelManager.Instance;
        if (lm != null) sb.AppendLine($"Nivel {lm.levelNumber} '{lm.levelTitle}': goblins {lm.GoblinsLeft}, casas {lm.HousesLeft}, oro {lm.GoldCollected}, terminado {lm.Finished}");
        if (lm != null && lm.Boss != null) sb.AppendLine($"Jefe: despierto {lm.Boss.IsAwake}, vida {lm.Boss.Health.Current:0}/{lm.Boss.Health.maxHealth}");
        var player = Player.Instance;
        if (player != null) sb.AppendLine($"Heroe daño {player.attackDamage}, velocidad {player.speed}, skin {player.GetComponent<Animator>().runtimeAnimatorController?.name}");
        if (player != null) sb.AppendLine($"Heroe vida {player.Health.Current}/{player.Health.maxHealth} en {player.transform.position}");
        sb.AppendLine($"Goblins vivos: {Health.All.Count(h => h.team == Team.Goblins && !h.IsDead && !h.isStructure)}");
        sb.AppendLine("Escena: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        sb.AppendLine("GameState: " + (GameManager.Instance != null ? GameManager.Instance.State.ToString() : "sin GameManager"));
        sb.AppendLine($"Progreso: desbloqueado hasta {Progress.Unlocked}");
        // Audio: qué sonó, qué música hay y si faltó algún archivo.
        sb.AppendLine("Música: " + (AudioManager.Instance != null ? AudioManager.Instance.CurrentMusic : "sin AudioManager"));
        sb.AppendLine("Sonidos: " + string.Join(", ", AudioManager.Played.OrderBy(p => p.Key).Select(p => p.Key + " x" + p.Value)));
        sb.AppendLine("Sonidos que faltan: " + (AudioManager.Missing.Count == 0 ? "ninguno" : string.Join(", ", AudioManager.Missing)));
        sb.AppendLine("FIN");
        File.AppendAllText(LogFile, sb.ToString());
    }
}
