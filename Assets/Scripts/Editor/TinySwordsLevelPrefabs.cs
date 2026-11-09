using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Parte del paso 2: prefabs para los niveles (jefe, casas y torres goblin, ruinas, árboles, decoraciones)
// y luces para el ambiente oscuro (fuego, antorchas, héroe).
public static class TinySwordsLevelPrefabs
{
    public const string Folder = TinySwordsPrefabs.Folder + "Level/";
    const string Pack = TinySwordsSprites.Pack;
    const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    // Decoraciones que van "pegadas al suelo" (se dibujan debajo de todo).
    static readonly int[] FlatDeco = { 1, 2, 4, 5, 10, 11, 14, 15 };
    // Decoraciones con collider (no se pueden atravesar).
    static readonly int[] SolidDeco = { 6, 9, 16, 17, 18 };

    public static Material Unlit => AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);

    public static void Run()
    {
        TinySwordsSprites.EnsureFolder(Folder.TrimEnd('/'));
        var unlit = Unlit;
        string P = TinySwordsPrefabs.Folder;

        // ---- Luces y materiales en prefabs existentes ----
        Edit(P + "Effects/Fire.prefab", go => { SetUnlit(go, unlit); AddLight(go, new Color(1f, 0.55f, 0.2f), 3.5f, 1.3f, new Vector2(0, 0.4f), true); });
        Edit(P + "Effects/Explosion.prefab", go => { SetUnlit(go, unlit); AddLight(go, new Color(1f, 0.6f, 0.25f), 4.5f, 1.6f, Vector2.zero, true); });
        Edit(P + "Effects/Dynamite.prefab", go => SetUnlit(go, unlit));
        Edit(P + "Resources/Pickup_Gold.prefab", go => { SetUnlit(go, unlit); AddLight(go, new Color(1f, 0.85f, 0.3f), 1.2f, 0.7f, new Vector2(0, 0.3f), false); });
        Edit(P + "Resources/Pickup_Meat.prefab", go =>
        {
            SetUnlit(go, unlit);
            AddLight(go, new Color(1f, 0.5f, 0.5f), 1.2f, 0.6f, new Vector2(0, 0.3f), false);
            var p = go.GetComponent<ResourcePickup>();
            p.healAmount = 35;
            p.amount = 0;
        });
        Edit(P + "Units/Player.prefab", go =>
        {
            AddLight(go, new Color(1f, 0.93f, 0.8f), 5f, 0.75f, new Vector2(0, 0.5f), false);
            go.GetComponent<Health>().deathEffectPrefab = Load(P + "Effects/Dead.prefab");
            go.GetComponent<WorldHealthBar>().material = unlit;
            var loadout = go.GetComponent<PlayerLoadout>();
            if (loadout == null) loadout = go.AddComponent<PlayerLoadout>();
            loadout.blueSkin = TinySwordsSprites.LoadController("Warrior");
            loadout.redSkin = TinySwordsSprites.LoadController("Warrior_Red");
            loadout.purpleSkin = TinySwordsSprites.LoadController("Warrior_Purple");
            loadout.yellowSkin = TinySwordsSprites.LoadController("Warrior_Yellow");
            loadout.whirlwindEffect = Load(P + "Effects/Explosion.prefab");
            loadout.whirlwindIcon = TinySwordsPrefabs.FirstFrame("Explosion");
            loadout.potionIcon = AssetDatabase.LoadAssetAtPath<Sprite>(Pack + "Resources/Resources/M_Idle_(NoShadow).png");
            loadout.dashIcon = TinySwordsPrefabs.FirstFrame("Warrior");
            loadout.slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Pack + "UI/Buttons/Button_Blue_9Slides.png");
        });
        Edit(P + "Enemies/Goblin_Torch.prefab", go =>
        {
            AddLight(go, new Color(1f, 0.5f, 0.15f), 2.8f, 1f, new Vector2(0.4f, 0.7f), true);
            go.GetComponent<EnemyAI>().aggroRange = 5f;
            go.GetComponent<WorldHealthBar>().material = unlit;
            SetLoot(go, 5);
        });
        Edit(P + "Enemies/Goblin_TNT.prefab", go => { go.GetComponent<EnemyAI>().aggroRange = 6.5f; go.GetComponent<WorldHealthBar>().material = unlit; SetLoot(go, 8); });
        Edit(P + "Enemies/Goblin_Barrel.prefab", go => { go.GetComponent<EnemyAI>().aggroRange = 5f; go.GetComponent<WorldHealthBar>().material = unlit; SetLoot(go, 3); });

        var fire = Load(P + "Effects/Fire.prefab");
        var explosion = Load(P + "Effects/Explosion.prefab");
        var dead = Load(P + "Effects/Dead.prefab");
        var torch = AssetDatabase.LoadAssetAtPath<EnemyAI>(P + "Enemies/Goblin_Torch.prefab");
        var barrel = AssetDatabase.LoadAssetAtPath<EnemyAI>(P + "Enemies/Goblin_Barrel.prefab");
        var dynamite = AssetDatabase.LoadAssetAtPath<Projectile>(P + "Effects/Dynamite.prefab");

        var goldPickup = AssetDatabase.LoadAssetAtPath<ResourcePickup>(P + "Resources/Pickup_Gold.prefab");

        // ---- Flecha enemiga (no persigue: se puede esquivar) ----
        var arrowGo = PrefabUtility.LoadPrefabContents(P + "Effects/Arrow.prefab");
        var enemyArrow = arrowGo.GetComponent<Projectile>();
        enemyArrow.homing = false;
        enemyArrow.speed = 11f;
        foreach (var sr in arrowGo.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(1f, 0.6f, 0.9f);
        PrefabUtility.SaveAsPrefabAsset(arrowGo, Folder + "Arrow_Enemy.prefab");
        PrefabUtility.UnloadPrefabContents(arrowGo);
        var enemyArrowPrefab = AssetDatabase.LoadAssetAtPath<Projectile>(Folder + "Arrow_Enemy.prefab");

        // ---- Goblin Veloz: rápido, frágil y en zigzag ----
        var fast = NewEnemy("Goblin_Fast", "TorchYellow", 35, EnemyAttackType.Melee, dead, goldPickup, 5, unlit);
        fast.moveSpeed = 3.4f; fast.zigzag = true; fast.aggroRange = 6f; fast.attackDamage = 7f;
        fast.attackCooldown = 0.8f; fast.attackRange = 0.7f; fast.hitDelay = 0.2f; fast.attackDuration = 0.4f;
        Save(fast.gameObject, "Goblin_Fast");

        // ---- Bombardero: lanza varias dinamitas ----
        var bomber = NewEnemy("Goblin_Bomber", "TNTPurple", 55, EnemyAttackType.Ranged, dead, goldPickup, 9, unlit);
        bomber.moveSpeed = 1.6f; bomber.aggroRange = 7f; bomber.attackRange = 5f; bomber.attackDamage = 14f;
        bomber.attackCooldown = 3.2f; bomber.hitDelay = 0.35f; bomber.attackDuration = 0.6f;
        bomber.projectilePrefab = dynamite; bomber.projectilesPerAttack = 3; bomber.projectileSpread = 1.6f;
        Save(bomber.gameObject, "Goblin_Bomber");

        // ---- Arquero Maldito: dispara flechas y retrocede si te acercas ----
        var archerE = NewEnemy("Archer_Cursed", "ArcherEnemy", 40, EnemyAttackType.Ranged, dead, goldPickup, 7, unlit);
        archerE.moveSpeed = 2.1f; archerE.aggroRange = 8f; archerE.attackRange = 6f; archerE.retreatDistance = 3.5f;
        archerE.attackDamage = 12f; archerE.attackCooldown = 1.7f; archerE.hitDelay = 0.45f; archerE.attackDuration = 0.6f;
        archerE.projectilePrefab = enemyArrowPrefab; archerE.throwOffset = new Vector2(0.3f, 0.6f);
        Save(archerE.gameObject, "Archer_Cursed");

        // ---- Caballero Oscuro: élite con armadura y embestida ----
        var knight = NewEnemy("Dark_Knight", "Warrior_Purple", 170, EnemyAttackType.Melee, explosion, goldPickup, 18, unlit);
        knight.transform.localScale = Vector3.one * 1.3f;
        knight.GetComponent<Health>().armor = 0.35f;
        knight.GetComponent<Health>().radius = 0.45f;
        knight.moveSpeed = 1.7f; knight.aggroRange = 7f; knight.attackRange = 0.9f; knight.attackDamage = 20f;
        knight.attackCooldown = 1.6f; knight.hitDelay = 0.3f; knight.attackDuration = 0.5f; knight.canDash = true;
        AddLight(knight.gameObject, new Color(0.7f, 0.3f, 1f), 2.5f, 0.9f, new Vector2(0, 0.6f), true);
        Save(knight.gameObject, "Dark_Knight");

        var fastPrefab = AssetDatabase.LoadAssetAtPath<EnemyAI>(Folder + "Goblin_Fast.prefab");
        var knightPrefab = AssetDatabase.LoadAssetAtPath<EnemyAI>(Folder + "Dark_Knight.prefab");

        // ---- Barril explosivo (neutral: explota al golpearlo) ----
        var barrelProp = new GameObject("Explosive_Barrel");
        var bsr = barrelProp.AddComponent<SpriteRenderer>();
        bsr.sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + $"Factions/Goblins/Troops/Barrel/{TinySwordsSprites.GoblinColor}/Barrel_{TinySwordsSprites.GoblinColor}.png")
            .OfType<Sprite>().FirstOrDefault(s => s.name.EndsWith("_0"));
        bsr.spriteSortPoint = SpriteSortPoint.Pivot;
        var bph = barrelProp.AddComponent<Health>();
        bph.team = Team.Neutral;
        bph.maxHealth = 10;
        bph.radius = 0.35f;
        barrelProp.AddComponent<ExplosiveBarrel>().explosionPrefab = explosion;
        var bc = barrelProp.AddComponent<CircleCollider2D>();
        bc.radius = 0.28f;
        bc.offset = new Vector2(0, 0.15f);
        Save(barrelProp, "Explosive_Barrel");

        // ---- Trampa de fuego (se enciende y apaga) ----
        var trap = new GameObject("Fire_Trap");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(fire, trap.transform);
        visual.transform.localPosition = Vector3.zero;
        var hz = trap.AddComponent<HazardZone>();
        hz.radius = 0.7f;
        hz.damagePerTick = 10f;
        hz.cycleTime = 2.2f;
        hz.visual = visual;
        var ember = new GameObject("Brasas");
        ember.transform.SetParent(trap.transform, false);
        var esr = ember.AddComponent<SpriteRenderer>();
        esr.sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "Resources/Trees/Tree.png").OfType<Sprite>().FirstOrDefault(s => s.name == "Tree_8");
        esr.color = new Color(0.35f, 0.2f, 0.15f);
        esr.sortingOrder = -6;
        Save(trap, "Fire_Trap");

        // ---- Jefe goblin ----
        var boss = TinySwordsPrefabs.NewUnit("Goblin_Boss", "Boss", Team.Goblins, 2200, 0.9f);
        boss.transform.localScale = Vector3.one * 2.2f;
        boss.GetComponent<Rigidbody2D>().mass = 20f;
        Object.DestroyImmediate(boss.GetComponent<WorldHealthBar>()); // el jefe usa la barra grande del HUD
        var bh = boss.GetComponent<Health>();
        bh.deathEffectPrefab = explosion;
        var bossAI = boss.AddComponent<GoblinBoss>();
        bossAI.dynamitePrefab = dynamite;
        bossAI.minionPrefabs = new[] { torch, fastPrefab, barrel, knightPrefab };
        bossAI.minionsPerSummon = 3;
        bossAI.slamDamage = 30f;
        bossAI.slamEffectPrefab = explosion;
        bossAI.summonEffectPrefab = dead;
        AddLight(boss, new Color(1f, 0.2f, 0.15f), 3f, 0.9f, new Vector2(0, 0.5f), true);
        Save(boss, "Goblin_Boss");

        // ---- Casa goblin (genera goblins) ----
        var house = new GameObject("Goblin_House");
        var hsr = Sprite(house, Pack + "Factions/Goblins/Buildings/Wood_House/Goblin_House.png");
        var hh = house.AddComponent<Health>();
        hh.team = Team.Goblins;
        hh.maxHealth = 220;
        hh.radius = 0.8f;
        hh.isStructure = true;
        hh.destroyOnDeath = false;
        var gh = house.AddComponent<GoblinHouse>();
        gh.spawnPrefabs = new[] { torch };
        gh.spriteRenderer = hsr;
        gh.destroyedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Pack + "Factions/Goblins/Buildings/Wood_House/Goblin_House_Destroyed.png");
        gh.fireEffectPrefab = fire;
        gh.destroyEffectPrefab = explosion;
        Box(house, new Vector2(1.4f, 0.6f), new Vector2(0, 0.3f));
        var hb = house.AddComponent<WorldHealthBar>();
        hb.offset = new Vector2(0, 2.7f);
        hb.width = 1.4f;
        hb.material = unlit;
        AddLight(house, new Color(1f, 0.45f, 0.15f), 2.5f, 0.9f, new Vector2(0, 0.6f), true);
        Save(house, "Goblin_House");

        // ---- Torre goblin (decoración con antorcha) ----
        var tower = new GameObject("Goblin_Tower");
        var tsr = tower.AddComponent<SpriteRenderer>();
        tsr.spriteSortPoint = SpriteSortPoint.Pivot;
        tsr.sprite = TinySwordsPrefabs.FirstFrame("GoblinTower");
        tower.AddComponent<Animator>().runtimeAnimatorController = TinySwordsSprites.LoadController("GoblinTower");
        Box(tower, new Vector2(1.8f, 0.7f), new Vector2(0, 0.35f));
        AddLight(tower, new Color(1f, 0.5f, 0.15f), 3.5f, 1.1f, new Vector2(0, 2f), true);
        Save(tower, "Goblin_Tower");

        // ---- Ruinas de los Knights ----
        Ruin("Ruin_House", "Factions/Knights/Buildings/House/House_Destroyed.png", new Vector2(1.5f, 0.7f));
        Ruin("Ruin_Tower", "Factions/Knights/Buildings/Tower/Tower_Destroyed.png", new Vector2(1.3f, 0.7f));
        Ruin("Ruin_Castle", "Factions/Knights/Buildings/Castle/Castle_Destroyed.png", new Vector2(4.2f, 1.3f));
        Ruin("Ruin_GoblinTower", "Factions/Goblins/Buildings/Wood_Tower/Wood_Tower_Destroyed.png", new Vector2(1.8f, 0.7f));

        // ---- Árboles ----
        var tree = new GameObject("Tree_Deco");
        var trs = tree.AddComponent<SpriteRenderer>();
        trs.spriteSortPoint = SpriteSortPoint.Pivot;
        trs.sprite = TinySwordsPrefabs.FirstFrame("Tree");
        tree.AddComponent<Animator>().runtimeAnimatorController = TinySwordsSprites.LoadController("Tree");
        var tc = tree.AddComponent<CircleCollider2D>();
        tc.radius = 0.3f;
        tc.offset = new Vector2(0, 0.15f);
        Save(tree, "Tree_Deco");

        var stump = new GameObject("Stump");
        var ssr = stump.AddComponent<SpriteRenderer>();
        ssr.sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "Resources/Trees/Tree.png").OfType<Sprite>().FirstOrDefault(s => s.name == "Tree_8");
        ssr.sortingOrder = -5;
        Save(stump, "Stump");

        var skull = new GameObject("Skull");
        var sk = skull.AddComponent<SpriteRenderer>();
        sk.sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "Factions/Knights/Troops/Dead/Dead.png").OfType<Sprite>().FirstOrDefault(s => s.name == "Dead_6");
        sk.sortingOrder = -5;
        Save(skull, "Skull");

        // ---- Decoraciones Deco/01..18 ----
        for (int i = 1; i <= 18; i++)
        {
            var go = new GameObject($"Deco_{i:00}");
            var sr = Sprite(go, $"{Pack}Deco/{i:00}.png");
            if (FlatDeco.Contains(i)) sr.sortingOrder = -5;
            if (SolidDeco.Contains(i))
            {
                var c = go.AddComponent<CircleCollider2D>();
                c.radius = i == 9 ? 0.4f : 0.25f;
                c.offset = new Vector2(0, 0.1f);
            }
            Save(go, $"Deco_{i:00}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Tiny Swords: prefabs de niveles creados en " + Folder);
    }

    static EnemyAI NewEnemy(string name, string animId, float hp, EnemyAttackType type, GameObject deathFx, ResourcePickup loot, int lootAmount, Material unlit)
    {
        var go = TinySwordsPrefabs.NewUnit(name, animId, Team.Goblins, hp, 0.35f);
        var h = go.GetComponent<Health>();
        h.deathEffectPrefab = deathFx;
        h.lootPrefab = loot;
        h.lootAmount = lootAmount;
        h.lootChance = 0.85f;
        go.GetComponent<WorldHealthBar>().material = unlit;
        var ai = go.AddComponent<EnemyAI>();
        ai.attackType = type;
        return ai;
    }

    static void SetLoot(GameObject go, int amount)
    {
        var h = go.GetComponent<Health>();
        h.lootAmount = amount;
        h.lootChance = 0.85f;
    }

    static void Ruin(string name, string sprite, Vector2 collider)
    {
        var go = new GameObject(name);
        Sprite(go, Pack + sprite);
        Box(go, collider, new Vector2(0, collider.y / 2f));
        Save(go, name);
    }

    static SpriteRenderer Sprite(GameObject go, string path)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return sr;
    }

    static void Box(GameObject go, Vector2 size, Vector2 offset)
    {
        var b = go.AddComponent<BoxCollider2D>();
        b.size = size;
        b.offset = offset;
    }

    public static Light2D AddLight(GameObject go, Color color, float radius, float intensity, Vector2 offset, bool flicker)
    {
        var existing = go.transform.Find("Light");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var l = new GameObject("Light");
        l.transform.SetParent(go.transform, false);
        l.transform.localPosition = offset;
        var light = l.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.15f;
        light.falloffIntensity = 0.6f;
        light.targetSortingLayers = SortingLayer.layers.Select(sl => sl.id).ToArray();
        if (flicker) l.AddComponent<LightFlicker>();
        return light;
    }

    static void SetUnlit(GameObject go, Material unlit)
    {
        if (unlit == null) return;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = unlit;
    }

    static void Edit(string path, System.Action<GameObject> change)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning("Tiny Swords: falta " + path);
            return;
        }
        var root = PrefabUtility.LoadPrefabContents(path);
        change(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void Save(GameObject go, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go, Folder + name + ".prefab");
        Object.DestroyImmediate(go);
    }
}
