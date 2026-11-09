using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;
using UI = TinySwordsUI;

// Paso 3: genera las 10 escenas de nivel (Assets/Scenes/Niveles) y el menú principal.
// Cada nivel es más largo, más oscuro y con más goblins; el 10 es la guarida del jefe.
// Las escenas generadas se pueden editar a mano después (mover enemigos, decoraciones, etc.).
// Si vuelves a ejecutar este paso, las escenas se REEMPLAZAN.
[InitializeOnLoad]
public static class TinySwordsLevels
{
    const string LevelFolder = "Assets/Scenes/Niveles";
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    const string Palette = "Assets/images/Palette/";
    const string AnimTiles = "Assets/images/AnimateTiles/";
    const string TriggerFile = "TinySwordsBuild.txt";
    const string MenuTriggerFile = "TinySwordsMenu.txt";
    const string LogFile = "Logs/TinySwordsBuild.log";

    static string P => TinySwordsPrefabs.Folder;
    static string LP => TinySwordsLevelPrefabs.Folder;

    static readonly string[] Titles =
    {
        "El Bosque Susurrante", "Aldea en Ruinas", "Pantano de Huesos", "Campamento Goblin", "Valle de las Calaveras",
        "Bosque Maldito", "Tierra Quemada", "Fortaleza de Madera", "La Última Aldea", "Guarida del Jefe Goblin"
    };

    // Lo nuevo de cada nivel (se muestra al empezar).
    static readonly string[] Tips =
    {
        "Consejo: presiona Shift para esquivar",
        "Nuevo: Goblin Veloz. ¡Golpea los barriles para que exploten!",
        "Nuevo: Goblin TNT y trampas de fuego",
        "Nuevo: Arquero Maldito. Esquiva sus flechas",
        "Nuevo: Barril kamikaze. ¡Cuidado con las emboscadas!",
        "Nuevo: Bombardero y goblins élite dorados",
        "Nuevo: Caballero Oscuro. Tiene armadura y embiste",
        "Todos los enemigos juntos. Haz combos para ganar más oro",
        "La última aldea... resiste hasta el final",
        "El Gran Jefe Goblin te espera"
    };

    // ---------- Ejecución ----------

    static double nextCheck;

    static TinySwordsLevels()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(TriggerFile))
            {
                File.Delete(TriggerFile);
                RunAllSteps();
            }
            else if (File.Exists(MenuTriggerFile))
            {
                File.Delete(MenuTriggerFile);
                RunAllSteps(levels: false);
            }
        };
    }

    // Ejecuta los pasos 1, 2 y 3 seguidos, guardando un registro en Logs/TinySwordsBuild.log.
    [MenuItem("Tiny Swords/Hacer todo (pasos 1, 2 y 3)", priority = 0)]
    public static void RunAllSteps() => RunAllSteps(true);

    static void RunAllSteps(bool levels)
    {
        var log = new StringBuilder("Inicio " + DateTime.Now + "\n");
        void Capture(string msg, string stack, LogType type)
        {
            if (type != LogType.Log || msg.StartsWith("Tiny Swords")) log.AppendLine($"[{type}] {msg}" + (type == LogType.Exception ? "\n" + stack : ""));
        }
        Application.logMessageReceived += Capture;
        try
        {
            EditorSceneManager.SaveOpenScenes();
            TinySwordsSprites.Run();
            TinySwordsPrefabs.Run();
            if (levels) BuildAll();
            else
            {
                BuildMenu(Profile("Assets/Settings/AmbienteTetrico.asset", new Color(0.85f, 0.85f, 1f)));
                EditorSceneManager.OpenScene(MenuScene);
            }
            log.AppendLine("FIN");
        }
        catch (Exception e)
        {
            log.AppendLine("[Exception] " + e);
        }
        finally
        {
            Application.logMessageReceived -= Capture;
            File.WriteAllText(LogFile, log.ToString());
        }
    }

    [MenuItem("Tiny Swords/3. Crear niveles y menú", priority = 3)]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildAll();
    }

    static void BuildAll()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LP + "Goblin_Boss.prefab") == null)
        {
            Debug.LogError("Tiny Swords: faltan los prefabs. Ejecuta primero los pasos 1 y 2.");
            return;
        }
        TinySwordsSprites.EnsureFolder(LevelFolder);
        var normal = Profile("Assets/Settings/AmbienteTetrico.asset", new Color(0.85f, 0.85f, 1f));
        var boss = Profile("Assets/Settings/AmbienteJefe.asset", new Color(1f, 0.7f, 0.7f));

        for (int n = 1; n <= Progress.LevelCount; n++)
        {
            EditorUtility.DisplayProgressBar("Tiny Swords", "Nivel " + n, n / 11f);
            BuildLevel(n, n == Progress.LevelCount ? boss : normal);
        }
        EditorUtility.DisplayProgressBar("Tiny Swords", "Menú principal", 1f);
        BuildMenu(normal);
        EditorUtility.ClearProgressBar();

        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScene, true) };
        for (int n = 1; n <= Progress.LevelCount; n++)
            scenes.Add(new EditorBuildSettingsScene($"{LevelFolder}/{Progress.SceneName(n)}.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.OpenScene(MenuScene);
        Debug.Log("Tiny Swords: 10 niveles y menú principal creados.");
    }

    // ---------- Niveles ----------

    class Placer
    {
        public readonly Random rng;
        readonly List<(Vector2 p, float r)> used = new List<(Vector2, float)>();
        readonly float xMin, xMax, yMin, yMax;

        public Placer(int seed, float xMin, float xMax, float yMin, float yMax)
        {
            rng = new Random(seed);
            this.xMin = xMin; this.xMax = xMax; this.yMin = yMin; this.yMax = yMax;
        }

        public float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        public bool Chance(float p) => rng.NextDouble() < p;
        public T Pick<T>(IList<T> list) => list[rng.Next(list.Count)];

        public bool Inside(Vector2 p) => p.x > xMin && p.x < xMax && p.y > yMin && p.y < yMax;

        public bool Free(Vector2 p, float r) => used.All(u => Vector2.Distance(u.p, p) >= u.r + r);

        public void Reserve(Vector2 p, float r) => used.Add((p, r));

        // Busca un lugar libre cerca de "center" (o en todo el mapa si center es null).
        public Vector2? Find(float r, Vector2? center = null, float spread = 0, int tries = 40)
        {
            for (int i = 0; i < tries; i++)
            {
                Vector2 p = center.HasValue
                    ? center.Value + new Vector2(Range(-spread, spread), Range(-spread, spread))
                    : new Vector2(Range(xMin, xMax), Range(yMin, yMax));
                if (!Inside(p) || !Free(p, r)) continue;
                Reserve(p, r);
                return p;
            }
            return null;
        }
    }

    static void BuildLevel(int n, VolumeProfile profile)
    {
        bool bossLevel = n == Progress.LevelCount;
        float t = (n - 1) / 9f; // 0 en el nivel 1, 1 en el nivel 10

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int width = bossLevel ? 40 : 30 + 4 * n;
        int height = bossLevel ? 24 : 18;
        int yMin = -height / 2, yMax = yMin + height;

        // Mapa: agua, espuma y pasto; cada nivel más oscuro y apagado.
        Color groundTint = bossLevel ? new Color(0.78f, 0.62f, 0.6f) : Color.Lerp(Color.white, new Color(0.72f, 0.78f, 0.68f), t);
        Color waterTint = bossLevel ? new Color(0.62f, 0.32f, 0.38f) : Color.Lerp(Color.white, new Color(0.5f, 0.55f, 0.72f), t);
        PaintIsland(0, width, yMin, yMax, groundTint, waterTint, n);

        var placer = new Placer(n * 7919, 1f, width - 1f, yMin + 1f, yMax - 1.3f);
        var decoRoot = new GameObject("Decoracion").transform;
        var obstacleRoot = new GameObject("Obstaculos").transform;
        var enemyRoot = new GameObject("Enemigos").transform;
        var pickupRoot = new GameObject("Recolectables").transform;

        // Héroe al inicio (izquierda).
        Vector2 start = new Vector2(3f, 0f);
        placer.Reserve(start, 3f);
        var player = Spawn(P + "Units/Player.prefab", start, null);
        Spawn(LP + "Deco_17.prefab", start + new Vector2(1.8f, 1.6f), decoRoot); // cartel del camino

        // Dificultad.
        // Curva de dificultad: casi plana al inicio y cada vez más empinada.
        // Vida: x1.0 (nivel 1), x1.05 (2), x1.4 (5), x2.3 (9). Daño: x1.0 → x1.8.
        float curve = Mathf.Pow(t, 1.6f);
        float hpMult = 1f + 1.5f * curve;
        float dmgMult = 1f + 0.9f * curve;
        float speedMult = 1f + 0.12f * curve;
        float eliteChance = n >= 6 ? 0.06f + 0.05f * (n - 6) : 0f;

        // Árboles en los bordes de arriba y abajo.
        for (float x = 0.8f; x < width - 0.5f; x += 1.3f)
        {
            if (placer.Chance(0.75f)) Obstacle(placer, LP + "Tree_Deco.prefab", new Vector2(x + placer.Range(-0.3f, 0.3f), yMax - 1.1f), 0.1f, obstacleRoot, force: true);
            if (placer.Chance(0.55f)) Obstacle(placer, LP + "Tree_Deco.prefab", new Vector2(x + placer.Range(-0.3f, 0.3f), yMin + 0.7f), 0.1f, obstacleRoot, force: true);
        }

        // Grupos de árboles y rocas en medio del camino.
        int clusters = width / 9;
        for (int i = 0; i < clusters; i++)
        {
            var c = placer.Find(2f, new Vector2(8 + i * (width - 14f) / Mathf.Max(1, clusters), 0), 5f);
            if (!c.HasValue) continue;
            int k = placer.rng.Next(2, 5);
            for (int j = 0; j < k; j++)
            {
                string prefab = placer.Chance(0.6f) ? "Tree_Deco" : placer.Chance(0.5f) ? "Deco_06" : "Deco_09";
                Obstacle(placer, LP + prefab + ".prefab", c.Value + new Vector2(placer.Range(-1.3f, 1.3f), placer.Range(-1.3f, 1.3f)), 0.5f, obstacleRoot);
            }
        }

        // Ruinas de los Knights (algunas en llamas).
        int ruins = bossLevel ? 2 : 1 + n / 3;
        var fire = P + "Effects/Fire.prefab";
        for (int i = 0; i < ruins; i++)
        {
            var pos = placer.Find(1.8f, new Vector2(placer.Range(8, width - 8), placer.Range(yMin + 3, yMax - 3)), 2f);
            if (!pos.HasValue) continue;
            Spawn(LP + (placer.Chance(0.5f) ? "Ruin_House" : "Ruin_Tower") + ".prefab", pos.Value, obstacleRoot);
            if (placer.Chance(0.4f + 0.5f * t)) Spawn(fire, pos.Value + new Vector2(placer.Range(-0.3f, 0.3f), 1.2f), decoRoot);
        }

        // Enemigos.
        var torch = LoadEnemy("Goblin_Torch");
        var tnt = LoadEnemy("Goblin_TNT");
        var barrel = LoadEnemy("Goblin_Barrel");
        var fast = LoadLevelEnemy("Goblin_Fast");
        var bomber = LoadLevelEnemy("Goblin_Bomber");
        var archer = LoadLevelEnemy("Archer_Cursed");
        var knight = LoadLevelEnemy("Dark_Knight");
        var enemies = new List<EnemyAI>();
        void AddN(EnemyAI prefab, int count) { for (int i = 0; i < count; i++) enemies.Add(prefab); }
        // Cada nivel presenta un enemigo nuevo y suma más de los anteriores.
        AddN(torch, bossLevel ? 4 : 2 + n);
        AddN(fast, bossLevel ? 3 : n >= 2 ? n / 2 : 0);
        AddN(tnt, bossLevel ? 2 : n >= 3 ? (n - 1) / 2 : 0);
        AddN(archer, bossLevel ? 2 : n >= 4 ? (n - 2) / 2 : 0);
        AddN(barrel, bossLevel ? 2 : n >= 5 ? (n - 3) / 2 : 0);
        AddN(bomber, bossLevel ? 1 : n >= 6 ? (n - 4) / 2 : 0);
        AddN(knight, bossLevel ? 2 : n >= 7 ? n - 6 : 0);
        enemies = enemies.OrderBy(_ => placer.rng.Next()).ToList();

        float campStart = bossLevel ? width - 16 : width - 9;
        int groups = bossLevel ? 2 : 2 + n / 2;
        int e = 0;
        for (int g = 0; g < groups; g++)
        {
            float gx = Mathf.Lerp(10f, campStart - 2f, groups == 1 ? 0.5f : g / (float)(groups - 1));
            var center = new Vector2(gx, placer.Range(yMin + 3f, yMax - 4f));
            int size = Mathf.CeilToInt((enemies.Count - e) / (float)(groups - g));
            for (int i = 0; i < size && e < enemies.Count; i++, e++)
            {
                var pos = placer.Find(0.7f, center, 2.2f) ?? placer.Find(0.7f, center, 4f);
                if (pos.HasValue) PlaceEnemy(enemies[e], pos.Value, hpMult, dmgMult, speedMult, enemyRoot, placer.Chance(eliteChance));
            }
            // Barriles explosivos cerca de los grupos: el jugador puede usarlos contra los goblins.
            if (n >= 2 && placer.Chance(0.75f))
            {
                var bp = placer.Find(0.5f, center + new Vector2(-2.5f, 0), 1.5f);
                if (bp.HasValue) Spawn(LP + "Explosive_Barrel.prefab", bp.Value, obstacleRoot);
            }
            if (n >= 3 && placer.Chance(0.6f))
            {
                var fp = placer.Find(0.6f, center, 1.5f);
                if (fp.HasValue) Spawn(fire, fp.Value, decoRoot);
            }
            Spawn(LP + "Skull.prefab", center + new Vector2(placer.Range(-2, 2), placer.Range(-2, 2)), decoRoot);
        }

        // Campamento goblin al final: casas (generan goblins), torres y fogatas.
        int houses = bossLevel ? 2 : n >= 4 ? (n - 2) / 2 : 0;
        int towers = bossLevel ? 2 : n >= 2 ? 1 + n / 3 : 0;
        Spawn(LP + "Deco_16.prefab", new Vector2(campStart - 1.5f, placer.Range(-2, 2)), decoRoot);
        for (int i = 0; i < houses; i++)
        {
            float hy = Mathf.Lerp(yMin + 3.5f, yMax - 4.5f, houses == 1 ? 0.5f : i / (float)(houses - 1));
            var pos = placer.Find(1.6f, new Vector2(campStart + 3f, hy), 1.5f) ?? placer.Find(1.6f, new Vector2(campStart + 3f, 0), 5f);
            if (!pos.HasValue) continue;
            var h = Spawn(LP + "Goblin_House.prefab", pos.Value, enemyRoot).GetComponent<GoblinHouse>();
            var spawnList = new List<EnemyAI> { torch };
            if (n >= 4) spawnList.Add(fast);
            if (n >= 5) spawnList.Add(tnt);
            if (n >= 6) spawnList.Add(archer);
            if (n >= 7) spawnList.Add(barrel);
            h.spawnPrefabs = spawnList.ToArray();
            h.spawnInterval = Mathf.Max(5f, 13f - 0.8f * n);
            h.maxAlive = 2 + n / 4;
            h.healthMultiplier = hpMult;
            h.damageMultiplier = dmgMult;
            var hh = h.GetComponent<Health>();
            hh.maxHealth *= hpMult;
        }
        for (int i = 0; i < towers; i++)
        {
            var pos = placer.Find(1.7f, new Vector2(campStart + placer.Range(0, 5), placer.Range(yMin + 3, yMax - 4)), 3f);
            if (pos.HasValue) Spawn(LP + "Goblin_Tower.prefab", pos.Value, obstacleRoot);
        }
        for (int i = 0; i < 1 + n / 3; i++)
        {
            var pos = placer.Find(0.8f, new Vector2(campStart + 2, 0), 5f);
            if (pos.HasValue) Spawn(fire, pos.Value, decoRoot);
        }

        // Jefe.
        if (bossLevel)
        {
            var bossPos = new Vector2(width - 8f, 0);
            placer.Reserve(bossPos, 3f);
            Spawn(LP + "Goblin_Boss.prefab", bossPos, enemyRoot);
            Spawn(LP + "Ruin_Castle.prefab", new Vector2(width - 5f, yMax - 5.5f), obstacleRoot);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var fp = bossPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 5.5f;
                if (placer.Inside(fp)) Spawn(fire, fp, decoRoot);
            }
            for (int i = 0; i < 14; i++)
                Spawn(LP + (placer.Chance(0.5f) ? "Skull" : "Deco_14") + ".prefab", bossPos + new Vector2(placer.Range(-5, 5), placer.Range(-5, 5)), decoRoot);
        }

        // Trampas de fuego que se encienden y apagan en el camino.
        int traps = bossLevel ? 8 : n >= 3 ? 1 + n / 2 : 0;
        for (int i = 0; i < traps; i++)
        {
            Vector2 near = bossLevel
                ? new Vector2(width - 8f, 0) + new Vector2(Mathf.Cos(i * 0.785f), Mathf.Sin(i * 0.785f)) * 3.2f
                : new Vector2(Mathf.Lerp(9, width - 10, (i + 0.5f) / traps), placer.Range(-4, 4));
            var tp = placer.Find(0.8f, near, bossLevel ? 0.3f : 2f);
            if (tp.HasValue) Spawn(LP + "Fire_Trap.prefab", tp.Value, obstacleRoot);
        }

        // Emboscadas: goblins que aparecen alrededor del héroe al pasar por ahí.
        int ambushes = bossLevel ? 1 : n >= 5 ? (n >= 8 ? 2 : 1) : 0;
        for (int i = 0; i < ambushes; i++)
        {
            var ap = placer.Find(1f, new Vector2(Mathf.Lerp(width * 0.35f, width * 0.7f, ambushes == 1 ? 0.5f : i), 0), 3f);
            if (!ap.HasValue) continue;
            var go = new GameObject("Emboscada");
            go.transform.SetParent(enemyRoot);
            go.transform.position = ap.Value;
            var amb = go.AddComponent<AmbushZone>();
            var pool = new List<EnemyAI> { torch, fast };
            if (n >= 6) pool.Add(archer);
            if (n >= 8) pool.Add(knight);
            amb.enemies = Enumerable.Range(0, 2 + n / 3).Select(_ => placer.Pick(pool)).ToArray();
            amb.healthMultiplier = hpMult;
            amb.damageMultiplier = dmgMult;
            amb.spawnEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P + "Effects/Dead.prefab");
        }

        // Tesoros escondidos cerca de los bordes (premian explorar).
        int treasures = 1 + n / 4;
        for (int i = 0; i < treasures; i++)
        {
            bool top = placer.Chance(0.5f);
            var tp = placer.Find(0.6f, new Vector2(placer.Range(6, width - 4), top ? yMax - 2.3f : yMin + 1.8f), 1f);
            if (!tp.HasValue) continue;
            var gold = Spawn(P + "Resources/Pickup_Gold.prefab", tp.Value, pickupRoot).GetComponent<ResourcePickup>();
            gold.lifetime = 0;
            gold.amount = 10 + 3 * n;
            gold.transform.localScale = Vector3.one;
        }

        // Carne para curarse.
        int meat = 2 + n / 3 + (bossLevel ? 2 : 0);
        for (int i = 0; i < meat; i++)
        {
            var pos = placer.Find(0.6f, new Vector2(Mathf.Lerp(8, width - 6, (i + 0.5f) / meat), placer.Range(-3, 3)), 3f);
            if (!pos.HasValue) continue;
            var m = Spawn(P + "Resources/Pickup_Meat.prefab", pos.Value, pickupRoot).GetComponent<ResourcePickup>();
            m.lifetime = 0;
        }

        // Decoración esparcida: más tétrica en cada nivel.
        var normalDeco = new[] { "Deco_01", "Deco_02", "Deco_03", "Deco_04", "Deco_05", "Deco_07", "Deco_08", "Deco_10", "Deco_11" };
        var spookyDeco = new[] { "Deco_14", "Deco_15", "Skull", "Stump", "Deco_12", "Deco_13", "Deco_14", "Skull" };
        var rareDeco = new[] { "Deco_18", "Deco_16", "Deco_03", "Deco_13" };
        int decoCount = Mathf.RoundToInt(width * height * (0.05f + 0.007f * n));
        for (int i = 0; i < decoCount; i++)
        {
            double roll = placer.rng.NextDouble();
            string[] pool = roll < 0.06 + 0.08 * t ? rareDeco : roll < 0.3 + 0.45 * t ? spookyDeco : normalDeco;
            string name = placer.Pick(pool);
            bool solid = name == "Deco_18" || name == "Deco_16";
            var pos = placer.Find(solid ? 0.6f : 0.3f);
            if (pos.HasValue) Spawn(LP + name + ".prefab", pos.Value, decoRoot);
        }

        // Rocas decorativas en el agua.
        // (las pinta PaintIsland)

        // Iluminación y ambiente.
        Color lightColor = bossLevel ? new Color(0.75f, 0.3f, 0.35f) : Color.Lerp(new Color(0.62f, 0.66f, 0.85f), new Color(0.38f, 0.32f, 0.55f), t);
        float lightIntensity = bossLevel ? 0.32f : Mathf.Lerp(0.62f, 0.3f, t);
        Ambient(lightColor, lightIntensity, profile, 0.35f + 0.065f * n);

        // Cámara.
        var cam = NewCamera(new Color(0.03f, 0.03f, 0.06f), 6.5f);
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.boundsMin = new Vector2(-1.5f, yMin - 1.5f);
        follow.boundsMax = new Vector2(width + 1.5f, yMax + 1.5f);
        cam.transform.position = new Vector3(start.x + 6, start.y, -10);

        // Reglas y HUD.
        var managers = new GameObject("Managers");
        managers.AddComponent<GameManager>();
        var lm = managers.AddComponent<LevelManager>();
        lm.levelNumber = n;
        lm.levelTitle = Titles[n - 1];
        lm.objective = bossLevel ? "Derrota al Gran Jefe Goblin" : houses > 0 ? "Derrota a los goblins y destruye sus casas" : "Derrota a todos los goblins";
        lm.showControlsOnStart = n == 1;
        lm.completionBonus = bossLevel ? 200 : 20 + 10 * n;
        lm.tip = Tips[n - 1];
        BuildLevelHUD();
        UI.EnsureEventSystem();

        EditorSceneManager.SaveScene(scene, $"{LevelFolder}/{Progress.SceneName(n)}.unity");
    }

    static void PlaceEnemy(EnemyAI prefab, Vector2 pos, float hpMult, float dmgMult, float speedMult, Transform parent, bool elite = false)
    {
        if (prefab == null) return;
        var go = Spawn(AssetDatabase.GetAssetPath(prefab), pos, parent);
        var ai = go.GetComponent<EnemyAI>();
        var h = go.GetComponent<Health>();
        ai.attackDamage *= dmgMult;
        ai.moveSpeed *= speedMult;
        h.maxHealth *= hpMult;
        if (!elite) return;

        // Élite: más grande, dorado, brilla y suelta mucho oro.
        go.name += " (Élite)";
        go.transform.localScale *= 1.3f;
        h.maxHealth *= 2.2f;
        h.lootAmount *= 3;
        h.lootChance = 1f;
        ai.attackDamage *= 1.4f;
        go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.88f, 0.55f);
        TinySwordsLevelPrefabs.AddLight(go, new Color(1f, 0.8f, 0.3f), 2.2f, 1f, new Vector2(0, 0.5f), true);
    }

    static EnemyAI LoadEnemy(string name) => AssetDatabase.LoadAssetAtPath<EnemyAI>($"{P}Enemies/{name}.prefab");
    static EnemyAI LoadLevelEnemy(string name) => AssetDatabase.LoadAssetAtPath<EnemyAI>($"{LP}{name}.prefab");

    static void Obstacle(Placer placer, string prefab, Vector2 pos, float r, Transform parent, bool force = false)
    {
        if (!force && (!placer.Inside(pos) || !placer.Free(pos, r))) return;
        placer.Reserve(pos, r);
        Spawn(prefab, pos, parent);
    }

    static GameObject Spawn(string path, Vector2 pos, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Tiny Swords: falta " + path);
            return new GameObject("Falta_" + Path.GetFileNameWithoutExtension(path));
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        return go;
    }

    // ---------- Mapa ----------

    static void PaintIsland(int x0, int x1, int y0, int y1, Color groundTint, Color waterTint, int seed)
    {
        TileBase F(int i) => AssetDatabase.LoadAssetAtPath<TileBase>($"{Palette}Tilemap_Flat_{i}.asset");
        var tl = F(0); var top = F(1); var tr = F(2);
        var left = F(10); var center = F(11); var right = F(12);
        var bl = F(18); var bottom = F(19); var br = F(20);
        var waterTile = AssetDatabase.LoadAssetAtPath<TileBase>(AnimTiles + "AguaMovimiento.asset");
        var foam = AssetDatabase.LoadAssetAtPath<TileBase>(TinySwordsSprites.FoamTilePath);
        var rocks = new[] { "Rocas1", "Rocas2", "Rocas3" }.Select(r => AssetDatabase.LoadAssetAtPath<TileBase>(AnimTiles + r + ".asset")).Where(r => r != null).ToArray();

        var grid = new GameObject("Mapa", typeof(Grid));
        var water = NewTilemap(grid.transform, "Agua", -12, waterTint);
        var foamMap = NewTilemap(grid.transform, "Espuma", -11, waterTint);
        var ground = NewTilemap(grid.transform, "Suelo", -10, groundTint);

        const int margin = 12;
        for (int x = x0 - margin; x < x1 + margin; x++)
            for (int y = y0 - margin; y < y1 + margin; y++)
                water.SetTile(new Vector3Int(x, y, 0), waterTile);

        for (int x = x0; x < x1; x++)
        {
            for (int y = y0; y < y1; y++)
            {
                bool l = x == x0, r = x == x1 - 1, b = y == y0, t = y == y1 - 1;
                TileBase tile = l && t ? tl : r && t ? tr : l && b ? bl : r && b ? br : t ? top : b ? bottom : l ? left : r ? right : center;
                ground.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        // La espuma (192 px) va debajo de las celdas del borde del pasto y sobresale hacia el agua.
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                if (x == x0 || x == x1 - 1 || y == y0 || y == y1 - 1)
                    foamMap.SetTile(new Vector3Int(x, y, 0), foam);

        var rng = new Random(seed);
        for (int i = 0; i < (x1 - x0) / 5 && rocks.Length > 0; i++)
        {
            bool topSide = rng.Next(2) == 0;
            var cell = new Vector3Int(rng.Next(x0 - 3, x1 + 3), topSide ? y1 + rng.Next(2, 5) : y0 - rng.Next(3, 6), 0);
            foamMap.SetTile(cell, rocks[rng.Next(rocks.Length)]);
        }

        // Paredes invisibles alrededor del pasto.
        var walls = new GameObject("Bordes");
        float w = x1 - x0, h = y1 - y0, cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
        Wall(walls.transform, new Vector2(x0 - 0.25f, cy), new Vector2(0.5f, h + 2));
        Wall(walls.transform, new Vector2(x1 + 0.25f, cy), new Vector2(0.5f, h + 2));
        Wall(walls.transform, new Vector2(cx, y1 - 0.25f), new Vector2(w + 2, 0.5f));
        Wall(walls.transform, new Vector2(cx, y0 - 0.25f), new Vector2(w + 2, 0.5f));
    }

    static Tilemap NewTilemap(Transform grid, string name, int order, Color color)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid, false);
        var tm = go.GetComponent<Tilemap>();
        tm.color = color;
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return tm;
    }

    static void Wall(Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject("Pared");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    // ---------- Luz, postprocesado y cámara ----------

    static void Ambient(Color color, float intensity, VolumeProfile profile, float weight)
    {
        var lightGo = new GameObject("Luz Global");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = color;
        light.intensity = intensity;
        light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();

        var volGo = new GameObject("Ambiente (postproceso)");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        vol.weight = Mathf.Clamp01(weight);
    }

    static VolumeProfile Profile(string path, Color filter)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile != null) return profile;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.5f);
        vignette.smoothness.Override(0.55f);
        vignette.color.Override(new Color(0.05f, 0f, 0.02f));

        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-30f);
        color.contrast.Override(18f);
        color.colorFilter.Override(filter);

        var grain = profile.Add<FilmGrain>(true);
        grain.intensity.Override(0.3f);

        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    static Camera NewCamera(Color background, float size)
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        var cam = go.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        return cam;
    }

    static void AddCursor(GameObject go)
    {
        var c = go.AddComponent<GameCursor>();
        c.cursorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TinySwordsSprites.Pack + "UI/Pointers/01.png");
    }

    // ---------- HUD del nivel ----------

    static void BuildLevelHUD()
    {
        var canvas = UI.NewCanvas("HUD");
        var t = canvas.transform;
        var hud = canvas.gameObject.AddComponent<LevelHUD>();
        AddCursor(canvas.gameObject);

        // Héroe: retrato, vida y oro (arriba a la izquierda).
        var heroPanel = UI.Carved("Heroe", t, new Vector2(0, 1), new Vector2(24, -24), new Vector2(560, 170));
        var portraitFrame = UI.SquareButton("Retrato", heroPanel.transform, false, 30);
        portraitFrame.interactable = false;
        portraitFrame.transition = Selectable.Transition.None;
        UI.Place((RectTransform)portraitFrame.transform, new Vector2(0, 0.5f), new Vector2(22, 4), new Vector2(128, 128));
        var portrait = UI.Plain("Warrior", portraitFrame.transform, TinySwordsPrefabs.FirstFrame("Warrior"));
        UI.Stretch(portrait.rectTransform, -34);
        portrait.rectTransform.offsetMax = new Vector2(34, 10);
        hud.healthFill = UI.Bar("Vida", heroPanel.transform, new Vector2(0, 0.5f), new Vector2(160, 30), new Vector2(370, 60), new Color(0.85f, 0.22f, 0.2f));
        hud.healthText = UI.Text("VidaTexto", hud.healthFill.parent, "150/150", 26);
        UI.Stretch(hud.healthText.rectTransform);
        var goldIcon = UI.Plain("OroIcono", heroPanel.transform, AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + "Resources/Resources/G_Idle_(NoShadow).png"));
        UI.Place(goldIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(160, -38), new Vector2(64, 64));
        hud.goldText = UI.Text("Oro", heroPanel.transform, "0", 36, TextAlignmentOptions.Left, UI.Gold, true);
        UI.Place(hud.goldText.rectTransform, new Vector2(0, 0.5f), new Vector2(230, -38), new Vector2(200, 56));

        // Nivel y objetivo (arriba al centro).
        hud.levelText = UI.Ribbon("Nivel", t, "Nivel 1", "Red", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(820, 110), 40);
        var objPanel = UI.Sliced("Objetivo", t, UI.S("Banners/Carved_3Slides.png"), 64);
        UI.Place(objPanel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(460, 64));
        hud.objectiveText = UI.Text("Texto", objPanel.transform, "Goblins: 0", 28);
        UI.Stretch(hud.objectiveText.rectTransform, 8);

        // Pausa (arriba a la derecha).
        UI.Action(UI.IconButton("Pausa", t, UI.IconSettings, false, new Vector2(1, 1), new Vector2(-28, -28), 96), UIButtonAction.Action.Resume);
        var hint = UI.Text("Ayuda", t, "H: controles   ·   Esc: pausa", 24, TextAlignmentOptions.Left, new Color(1, 1, 1, 0.75f));
        UI.Place(hint.rectTransform, new Vector2(0, 0), new Vector2(28, 20), new Vector2(600, 40));

        // Mensajes grandes.
        hud.messageText = UI.Text("Mensaje", t, "", 58, TextAlignmentOptions.Center, UI.Gold, true);
        UI.Place(hud.messageText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1400, 90));

        // Barra del jefe (abajo al centro).
        var bossPanel = UI.Rect("Jefe", t);
        UI.Place(bossPanel, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1100, 170));
        hud.bossPanel = bossPanel.gameObject;
        hud.bossNameText = UI.Ribbon("Nombre", bossPanel, "Gran Jefe Goblin", "Red", new Vector2(0.5f, 1), Vector2.zero, new Vector2(640, 96), 36);
        hud.bossFill = UI.Bar("Vida", bossPanel, new Vector2(0.5f, 0), Vector2.zero, new Vector2(1100, 72), new Color(0.7f, 0.1f, 0.25f));

        // Paneles.
        hud.pausePanel = Window(t, "Pausa", "Pausa", "Blue", new Vector2(760, 700), out var pauseBox, out _);
        var pb = UI.ButtonColumn(pauseBox, 170, new Vector2(440, 96), 18, ("Continuar", false), ("Controles", false), ("Reintentar", false), ("Niveles", true));
        UI.Action(pb[0], UIButtonAction.Action.Resume);
        UI.Action(pb[1], UIButtonAction.Action.ShowControls);
        UI.Action(pb[2], UIButtonAction.Action.Restart);
        UI.Action(pb[3], UIButtonAction.Action.LevelSelect);

        hud.victoryPanel = Window(t, "Victoria", "¡Nivel completado!", "Yellow", new Vector2(900, 720), out var winBox, out hud.victoryTitle);
        hud.victoryText = UI.Text("Texto", winBox, "", 34, TextAlignmentOptions.Center, UI.Parchment);
        UI.Place(hud.victoryText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(700, 120));
        var wb = UI.ButtonColumn(winBox, 320, new Vector2(460, 96), 16, ("Siguiente nivel", false), ("Reintentar", false), ("Niveles", true));
        UI.Action(wb[0], UIButtonAction.Action.NextLevel);
        UI.Action(wb[1], UIButtonAction.Action.Restart);
        UI.Action(wb[2], UIButtonAction.Action.LevelSelect);
        hud.nextLevelButton = wb[0].gameObject;

        hud.defeatPanel = Window(t, "Derrota", "Has caído", "Red", new Vector2(820, 600), out var loseBox, out _);
        var lt = UI.Text("Texto", loseBox, "Los goblins te derrotaron...\n¿Lo intentas otra vez?", 34, TextAlignmentOptions.Center, UI.Parchment);
        UI.Place(lt.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(640, 120));
        var lb = UI.ButtonColumn(loseBox, 320, new Vector2(440, 96), 18, ("Reintentar", false), ("Niveles", true));
        UI.Action(lb[0], UIButtonAction.Action.Restart);
        UI.Action(lb[1], UIButtonAction.Action.LevelSelect);

        hud.controlsPanel = UI.ControlsPanel(t, b => UI.Action(b, UIButtonAction.Action.CloseControls));
    }

    // Ventana: fondo oscuro + pergamino + cinta con título.
    static GameObject Window(Transform parent, string name, string title, string ribbonColor, Vector2 size, out RectTransform box, out TMP_Text titleText)
    {
        var overlay = UI.Overlay(name, parent);
        box = UI.Scroll("Pergamino", overlay.transform, size).rectTransform;
        titleText = UI.Ribbon("Titulo", overlay.transform, title, ribbonColor, UI.Center, new Vector2(0, size.y / 2f - 20), new Vector2(Mathf.Min(size.x, 720), 120), 52);
        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    // ---------- Tienda ----------

    static GameObject BuildShop(Transform parent)
    {
        var goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + "Resources/Resources/G_Idle_(NoShadow).png");
        var overlay = UI.Overlay("Tienda", parent, 0.8f);
        var scroll = UI.Scroll("Pergamino", overlay.transform, new Vector2(1640, 900));
        scroll.rectTransform.anchoredPosition = new Vector2(0, -20);
        UI.Ribbon("Titulo", overlay.transform, "Tienda", "Yellow", UI.Center, new Vector2(0, 415), new Vector2(620, 120), 54);
        UI.Action(UI.IconButton("Cerrar", overlay.transform, UI.IconClose, true, UI.Center, new Vector2(760, 380), 96), UIButtonAction.Action.ClosePanel, overlay.gameObject);

        // Oro disponible.
        var goldBox = UI.Carved("Oro", overlay.transform, UI.Center, new Vector2(-640, 380), new Vector2(260, 96));
        var gi = UI.Plain("Icono", goldBox.transform, goldSprite);
        UI.Place(gi.rectTransform, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(66, 66));
        var gt = UI.Text("Cantidad", goldBox.transform, "0", 40, TextAlignmentOptions.Left, UI.Gold, true);
        UI.Place(gt.rectTransform, new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(150, 70));
        overlay.gameObject.AddComponent<ShopGoldLabel>().text = gt;

        for (int i = 0; i < Shop.Items.Length; i++)
        {
            var item = Shop.Items[i];
            float x = -520 + (i % 3) * 520;
            float y = 225 - (i / 3) * 250;
            var card = UI.Carved(item.id, scroll.transform, UI.Center, new Vector2(x, y), new Vector2(490, 230), 34);
            var c = card.gameObject.AddComponent<ShopItemCard>();
            c.itemId = item.id;

            var icon = UI.Plain("Icono", card.transform, ShopIcon(item));
            UI.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(14, 10), new Vector2(140, 140));

            var name = UI.Text("Nombre", card.transform, item.name, 30, TextAlignmentOptions.Left, Color.white, true);
            UI.Place(name.rectTransform, new Vector2(0, 1), new Vector2(165, -22), new Vector2(310, 44));
            var desc = UI.Text("Descripcion", card.transform, item.description, 22, TextAlignmentOptions.TopLeft, new Color(1f, 0.95f, 0.85f));
            UI.Place(desc.rectTransform, new Vector2(0, 1), new Vector2(165, -68), new Vector2(305, 64));
            c.levelText = UI.Text("Nivel", card.transform, "", 22, TextAlignmentOptions.Left, UI.Gold, true);
            UI.Place(c.levelText.rectTransform, new Vector2(0, 0), new Vector2(24, 20), new Vector2(150, 36));

            var buy = UI.Button("Comprar", card.transform, "0", item.type == ShopItemType.Skin, 30);
            UI.Place((RectTransform)buy.transform, new Vector2(1, 0), new Vector2(-22, 18), new Vector2(230, 76));
            c.buyButton = buy;
            c.buyText = buy.GetComponentInChildren<TMP_Text>();
            c.buyText.rectTransform.offsetMin = new Vector2(64, 12);
            var coin = UI.Plain("Moneda", buy.transform, goldSprite);
            UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(26, 5), new Vector2(52, 52));
            c.priceIcon = coin.gameObject;
        }

        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    static Sprite ShopIcon(ShopItem item)
    {
        if (item.type == ShopItemType.Skin)
            return TinySwordsPrefabs.FirstFrame(item.skinColor == "Blue" ? "Warrior" : "Warrior_" + item.skinColor);
        switch (item.id)
        {
            case Shop.Sword: return ClipFrame("Warrior", "Attack", 3);
            case Shop.Armor: return ClipFrame("Warrior", "AttackUp", 0);
            case Shop.Boots: return ClipFrame("Warrior", "Run", 2);
            case Shop.Whirlwind: return ClipFrame("Explosion", "Idle", 3);
            default: return AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + "Resources/Resources/M_Idle_(NoShadow).png");
        }
    }

    static Sprite ClipFrame(string id, string anim, int frame)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{TinySwordsSprites.AnimFolder}{id}/{id}_{anim}.anim");
        if (clip == null) return null;
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault();
        var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        return keys.Length > 0 ? keys[Mathf.Min(frame, keys.Length - 1)].value as Sprite : null;
    }

    // Regenera solo el menú principal (con la tienda), sin tocar los niveles.
    [MenuItem("Tiny Swords/Regenerar solo el menú principal", priority = 4)]
    public static void RebuildMenuOnly()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildMenu(Profile("Assets/Settings/AmbienteTetrico.asset", new Color(0.85f, 0.85f, 1f)));
        EditorSceneManager.OpenScene(MenuScene);
    }

    // ---------- Menú principal ----------

    static void BuildMenu(VolumeProfile profile)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Fondo: una pequeña escena tenebrosa con ruinas, fuego y goblins.
        PaintIsland(-15, 15, -9, 9, new Color(0.75f, 0.78f, 0.7f), new Color(0.55f, 0.55f, 0.72f), 99);
        var root = new GameObject("Diorama").transform;
        var placer = new Placer(99, -14, 14, -8, 7.5f);
        placer.Reserve(new Vector2(0, -1.5f), 4f); // espacio para los botones
        Spawn(LP + "Ruin_Castle.prefab", new Vector2(-8, 2), root);
        Spawn(LP + "Goblin_Tower.prefab", new Vector2(9, 3), root);
        Spawn(LP + "Goblin_House.prefab", new Vector2(11, -3), root);
        Spawn(LP + "Ruin_Tower.prefab", new Vector2(-11, -4), root);
        foreach (var p in new[] { new Vector2(-6.5f, 4.2f), new Vector2(-9.5f, 3.5f), new Vector2(7, -1), new Vector2(-10.5f, -2.5f), new Vector2(4, 5) })
            Spawn(P + "Effects/Fire.prefab", p, root);
        foreach (var p in new[] { new Vector2(6, -3.5f), new Vector2(7.5f, -5), new Vector2(9, 0.5f), new Vector2(5.5f, 1.5f) })
            Spawn(P + "Enemies/Goblin_Torch.prefab", p, root);
        Spawn(LP + "Goblin_Boss.prefab", new Vector2(11.5f, 0.8f), root);
        for (float x = -14.2f; x < 14.5f; x += 1.3f)
        {
            Spawn(LP + "Tree_Deco.prefab", new Vector2(x, 7.9f), root);
            if (placer.Chance(0.6f)) Spawn(LP + "Tree_Deco.prefab", new Vector2(x + 0.4f, -8.3f), root);
        }
        for (int i = 0; i < 40; i++)
        {
            var pos = placer.Find(0.3f);
            if (pos.HasValue) Spawn(LP + placer.Pick(new[] { "Deco_14", "Skull", "Deco_12", "Stump", "Deco_04", "Deco_07", "Deco_15" }) + ".prefab", pos.Value, root);
        }
        Ambient(new Color(0.5f, 0.46f, 0.7f), 0.55f, profile, 0.6f);
        NewCamera(new Color(0.03f, 0.03f, 0.06f), 7.5f);

        // Interfaz.
        var canvas = UI.NewCanvas("Menu");
        var t = canvas.transform;
        AddCursor(canvas.gameObject);
        var menu = canvas.gameObject.AddComponent<MainMenu>();

        UI.Ribbon("Titulo", t, "TINY SWORDS", "Red", new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1100, 180), 100);
        var sub = UI.Text("Subtitulo", t, "La Amenaza Goblin", 64, TextAlignmentOptions.Center, UI.Gold, true);
        UI.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(1200, 90));

        var goldPanel = UI.Carved("OroTotal", t, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(280, 100));
        var gi = UI.Plain("Icono", goldPanel.transform, AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + "Resources/Resources/G_Idle_(NoShadow).png"));
        UI.Place(gi.rectTransform, new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(70, 70));
        menu.goldText = UI.Text("Oro", goldPanel.transform, "0", 40, TextAlignmentOptions.Left, UI.Gold, true);
        UI.Place(menu.goldText.rectTransform, new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(160, 70));

        var main = UI.Carved("Principal", t, UI.Center, new Vector2(0, -170), new Vector2(560, 560), 50);
        menu.mainPanel = main.gameObject;
        var mb = UI.ButtonColumn(main.transform, 50, new Vector2(440, 100), 22, ("Jugar", false), ("Tienda", false), ("Controles", false), ("Salir", true));

        // Selector de niveles.
        var levels = UI.Overlay("Niveles", t, 0.75f);
        menu.levelSelectPanel = levels.gameObject;
        var scroll = UI.Scroll("Pergamino", levels.transform, new Vector2(1560, 860));
        UI.Ribbon("Titulo", levels.transform, "Elige un nivel", "Yellow", UI.Center, new Vector2(0, 400), new Vector2(760, 120), 52);
        for (int i = 0; i < Progress.LevelCount; i++)
        {
            int n = i + 1;
            bool isBoss = n == Progress.LevelCount;
            float x = -520 + (i % 5) * 260;
            float y = i < 5 ? 150 : -170;
            var b = UI.SquareButton("Nivel_" + n, scroll.transform, isBoss, 44);
            UI.Place((RectTransform)b.transform, UI.Center, new Vector2(x, y), new Vector2(180, 180));
            var lb = b.gameObject.AddComponent<LevelButton>();
            lb.level = n;
            lb.label = UI.Text("Numero", b.transform, n.ToString(), 84, TextAlignmentOptions.Center, Color.white, true);
            UI.Stretch(lb.label.rectTransform);
            lb.label.rectTransform.offsetMin = new Vector2(0, 16);
            var lockImg = UI.Plain("Candado", b.transform, UI.Icon(UI.IconLock));
            UI.Stretch(lockImg.rectTransform, 50);
            lb.lockIcon = lockImg.gameObject;
            var done = UI.Plain("Completado", b.transform, AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + "Resources/Resources/G_Idle_(NoShadow).png"));
            UI.Place(done.rectTransform, new Vector2(1, 1), new Vector2(18, 18), new Vector2(64, 64));
            lb.completedMark = done.gameObject;
            var title = UI.Text("Nombre", scroll.transform, isBoss ? "JEFE FINAL" : Titles[i], 24, TextAlignmentOptions.Center, isBoss ? UI.Blood : UI.Parchment);
            UI.Place(title.rectTransform, UI.Center, new Vector2(x, y - 125), new Vector2(250, 60));
        }
        UI.Action(UI.IconButton("Cerrar", levels.transform, UI.IconClose, true, UI.Center, new Vector2(700, 360), 96), UIButtonAction.Action.ClosePanel, levels.gameObject);
        var reset = UI.Button("Reiniciar", scroll.transform, "Borrar progreso", true, 24);
        UI.Place((RectTransform)reset.transform, UI.Center, new Vector2(-530, -350), new Vector2(300, 70));
        UI.Action(reset, UIButtonAction.Action.ResetProgress);
        levels.gameObject.SetActive(false);

        // Controles.
        Button closeControls = null;
        var controls = UI.ControlsPanel(t, b => closeControls = b);
        UI.Action(closeControls, UIButtonAction.Action.ClosePanel, controls);
        menu.controlsPanel = controls;

        // Tienda.
        var shop = BuildShop(t);
        menu.shopPanel = shop;

        UI.Action(mb[0], UIButtonAction.Action.OpenPanel, levels.gameObject);
        UI.Action(mb[1], UIButtonAction.Action.OpenPanel, shop);
        UI.Action(mb[2], UIButtonAction.Action.OpenPanel, controls);
        UI.Action(mb[3], UIButtonAction.Action.Quit);

        UI.EnsureEventSystem();
        EditorSceneManager.SaveScene(scene, MenuScene);
    }
}
