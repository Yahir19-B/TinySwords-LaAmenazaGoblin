using System.Linq;
using UnityEditor;
using UnityEngine;

// Paso 2: crea los prefabs del juego en Assets/Prefabs con todos sus componentes configurados.
// Después puedes ajustar vida, daño, velocidad, etc. directamente en cada prefab.
public static class TinySwordsPrefabs
{
    public const string Folder = "Assets/Prefabs/";
    const string Pack = TinySwordsSprites.Pack;
    static string K => TinySwordsSprites.KnightColor;

    [MenuItem("Tiny Swords/2. Crear prefabs", priority = 2)]
    public static void Run()
    {
        if (TinySwordsSprites.LoadController("Warrior") == null)
        {
            EditorUtility.DisplayDialog("Tiny Swords", "Primero ejecuta \"Tiny Swords > 1. Configurar sprites y animaciones\".", "OK");
            return;
        }

        foreach (var sub in new[] { "Effects", "Units", "Enemies", "Buildings", "Resources" })
            TinySwordsSprites.EnsureFolder(Folder + sub);

        // Efectos y proyectiles primero, porque otros prefabs los usan.
        var explosion = Effect("Explosion", "Explosion", TinySwordsSprites.ClipLength("Explosion", "Idle"), 20);
        var fire = Effect("Fire", "Fire", 0, 5);
        var dead = Effect("Dead", "Dead", TinySwordsSprites.ClipLength("Dead", "Dead") + 0.5f, -1);

        var arrow = ProjectilePrefab("Arrow", SpriteFrom("Factions/Knights/Troops/Archer/Arrow/Arrow.png", "Arrow_0"), null);
        arrow.speed = 12f;
        Save(arrow.gameObject, "Effects/Arrow");

        var dynamite = ProjectilePrefab("Dynamite", SpriteFrom("Factions/Goblins/Troops/TNT/Dynamite/Dynamite.png", "Dynamite_0"),
            TinySwordsSprites.LoadController("Dynamite"));
        dynamite.speed = 6f;
        dynamite.arc = true;
        dynamite.arcHeight = 1.8f;
        dynamite.areaRadius = 1.1f;
        dynamite.structureDamageMultiplier = 1.5f;
        dynamite.rotateToDirection = false;
        dynamite.spinSpeed = 540f;
        dynamite.impactEffectPrefab = explosion;
        var dynamitePrefab = Save(dynamite.gameObject, "Effects/Dynamite").GetComponent<Projectile>();
        var arrowPrefab = AssetDatabase.LoadAssetAtPath<Projectile>(Folder + "Effects/Arrow.prefab");

        var goldPickup = Pickup("Gold", ResourceType.Gold);
        var woodPickup = Pickup("Wood", ResourceType.Wood);
        var meatPickup = Pickup("Meat", ResourceType.Meat);

        // ---- Knights ----
        var player = Unit("Player", "Warrior", Team.Knights, 150, 0.35f);
        player.AddComponent<Player>();
        player.GetComponent<Health>().destroyOnDeath = false;
        Save(player, "Units/Player");

        var pawnGo = Unit("Pawn", "Pawn", Team.Knights, 40, 0.3f);
        pawnGo.GetComponent<Health>().deathEffectPrefab = dead;
        var pawn = pawnGo.AddComponent<Pawn>();
        var icon = new GameObject("CarryIcon").AddComponent<SpriteRenderer>();
        icon.transform.SetParent(pawnGo.transform, false);
        icon.transform.localPosition = new Vector3(0, 0.95f, 0);
        icon.transform.localScale = Vector3.one * 0.6f;
        icon.sortingOrder = 1;
        icon.enabled = false;
        pawn.carryIcon = icon;
        pawn.goldIcon = SingleSprite("Resources/Resources/G_Idle_(NoShadow).png");
        pawn.woodIcon = SingleSprite("Resources/Resources/W_Idle_(NoShadow).png");
        pawn.meatIcon = SingleSprite("Resources/Resources/M_Idle_(NoShadow).png");
        Save(pawnGo, "Units/Pawn");

        // ---- Goblins ----
        var torch = Enemy("Goblin_Torch", "Torch", 50, EnemyAttackType.Melee, dead, goldPickup, 3);
        torch.moveSpeed = 2f;
        torch.attackRange = 0.7f;
        torch.attackDamage = 8f;
        torch.attackCooldown = 1.1f;
        torch.attackDuration = TinySwordsSprites.ClipLength("Torch", "Attack");
        torch.hitDelay = 0.3f;
        Save(torch.gameObject, "Enemies/Goblin_Torch");

        var tnt = Enemy("Goblin_TNT", "TNT", 35, EnemyAttackType.Ranged, dead, goldPickup, 5);
        tnt.moveSpeed = 1.8f;
        tnt.aggroRange = 6f;
        tnt.attackRange = 4.5f;
        tnt.attackDamage = 15f;
        tnt.attackCooldown = 2.5f;
        tnt.attackDuration = TinySwordsSprites.ClipLength("TNT", "Attack");
        tnt.hitDelay = 0.35f;
        tnt.structureDamageMultiplier = 1.5f;
        tnt.projectilePrefab = dynamitePrefab;
        Save(tnt.gameObject, "Enemies/Goblin_TNT");

        var barrel = Enemy("Goblin_Barrel", "Barrel", 30, EnemyAttackType.Kamikaze, dead, goldPickup, 2);
        barrel.moveSpeed = 3f;
        barrel.aggroRange = 5f;
        barrel.attackRange = 0.6f;
        barrel.attackDamage = 40f;
        barrel.structureDamageMultiplier = 2f;
        barrel.explosionRadius = 1.5f;
        barrel.fuseTime = 0.6f;
        barrel.explosionPrefab = explosion;
        barrel.GetComponent<WorldHealthBar>().offset = new Vector2(0, 0.7f);
        Save(barrel.gameObject, "Enemies/Goblin_Barrel");

        // ---- Edificios ----
        var castle = BuildingPrefab("Castle", "Castillo", "Factions/Knights/Buildings/Castle/Castle", 1000, 1.7f,
            new Vector2(4.2f, 1.3f), new Vector2(0, 0.45f), fire, explosion);
        castle.GetComponent<Building>().fireOffsets = new[] { new Vector2(-1.2f, 1.6f), new Vector2(0.3f, 2.4f), new Vector2(1.3f, 1.4f) };
        castle.GetComponent<Building>().rebuildCost = new Cost(0, 0, 0);
        castle.GetComponent<WorldHealthBar>().offset = new Vector2(0, 3.4f);
        castle.GetComponent<WorldHealthBar>().width = 2.5f;
        castle.GetComponent<WorldHealthBar>().height = 0.15f;
        var castleComp = castle.AddComponent<Castle>();
        var spawn = new GameObject("SpawnPoint").transform;
        spawn.SetParent(castle.transform, false);
        spawn.localPosition = new Vector3(0, -1f, 0);
        castleComp.spawnPoint = spawn;
        castle.AddComponent<DropOffPoint>();
        Save(castle, "Buildings/Castle");

        var house = BuildingPrefab("House", "Casa", "Factions/Knights/Buildings/House/House", 250, 0.9f,
            new Vector2(1.5f, 0.7f), new Vector2(0, 0.3f), fire, explosion);
        var houseB = house.GetComponent<Building>();
        houseB.populationBonus = 3;
        houseB.buildTime = 10f;
        houseB.fireOffsets = new[] { new Vector2(0, 1.4f) };
        house.GetComponent<WorldHealthBar>().offset = new Vector2(0, 2.6f);
        var housePrefab = Save(house, "Buildings/House").GetComponent<Building>();

        var tower = BuildingPrefab("Tower", "Torre", "Factions/Knights/Buildings/Tower/Tower", 300, 0.8f,
            new Vector2(1.3f, 0.7f), new Vector2(0, 0.3f), fire, explosion);
        var towerB = tower.GetComponent<Building>();
        towerB.buildTime = 14f;
        towerB.fireOffsets = new[] { new Vector2(0, 1.8f) };
        tower.GetComponent<WorldHealthBar>().offset = new Vector2(0, 3.6f);
        var towerComp = tower.AddComponent<Tower>();
        towerComp.arrowPrefab = arrowPrefab;
        var archer = new GameObject("Archer");
        archer.transform.SetParent(tower.transform, false);
        archer.transform.localPosition = new Vector3(0, 2.1f, 0);
        var archerSr = archer.AddComponent<SpriteRenderer>();
        archerSr.sprite = FirstSprite("Archer");
        archerSr.sortingOrder = 1;
        archer.AddComponent<Animator>().runtimeAnimatorController = TinySwordsSprites.LoadController("Archer");
        towerComp.archer = archer.AddComponent<UnitAnimator>();
        towerComp.firePoint = archer.transform;
        towerB.enableWhenComplete = new Behaviour[] { towerComp };
        var towerPrefab = Save(tower, "Buildings/Tower").GetComponent<Building>();

        BuildSpotPrefab("BuildSpot_House", housePrefab, new Cost(0, 40, 0), "Factions/Knights/Buildings/House/House_Construction.png");
        BuildSpotPrefab("BuildSpot_Tower", towerPrefab, new Cost(20, 50, 0), "Factions/Knights/Buildings/Tower/Tower_Construction.png");

        // ---- Recursos ----
        var mine = new GameObject("GoldMine");
        var mineSr = mine.AddComponent<SpriteRenderer>();
        mineSr.spriteSortPoint = SpriteSortPoint.Pivot;
        var mineNode = mine.AddComponent<ResourceNode>();
        mineNode.type = ResourceType.Gold;
        mineNode.amount = 500;
        mineNode.amountPerTrip = 5;
        mineNode.gatherTime = 3f;
        mineNode.workDistance = 1.6f;
        mineNode.hideWorkerInside = true;
        mineNode.spriteRenderer = mineSr;
        mineNode.normalSprite = SingleSprite("Resources/Gold Mine/GoldMine_Inactive.png");
        mineNode.workingSprite = SingleSprite("Resources/Gold Mine/GoldMine_Active.png");
        mineNode.depletedSprite = SingleSprite("Resources/Gold Mine/GoldMine_Destroyed.png");
        mineSr.sprite = mineNode.normalSprite;
        var mineCol = mine.AddComponent<BoxCollider2D>();
        mineCol.size = new Vector2(2.4f, 0.8f);
        mineCol.offset = new Vector2(0, 0.4f);
        Save(mine, "Resources/GoldMine");

        var tree = AnimatedObject("Tree", "Tree");
        var treeNode = tree.AddComponent<ResourceNode>();
        treeNode.type = ResourceType.Wood;
        treeNode.amount = 40;
        treeNode.amountPerTrip = 5;
        treeNode.gatherTime = 2.5f;
        treeNode.workDistance = 0.7f;
        treeNode.regrowTime = 60f;
        tree.AddComponent<UnitAnimator>();
        var treeCol = tree.AddComponent<CircleCollider2D>();
        treeCol.radius = 0.25f;
        treeCol.offset = new Vector2(0, 0.15f);
        Save(tree, "Resources/Tree");

        var sheep = AnimatedObject("Sheep", "Sheep");
        var sheepNode = sheep.AddComponent<ResourceNode>();
        sheepNode.type = ResourceType.Meat;
        sheepNode.amount = 20;
        sheepNode.amountPerTrip = 5;
        sheepNode.gatherTime = 2f;
        sheepNode.workDistance = 0.6f;
        sheepNode.destroyWhenDepleted = true;
        sheep.AddComponent<UnitAnimator>();
        Save(sheep, "Resources/Sheep");

        AssetDatabase.SaveAssets();
        TinySwordsLevelPrefabs.Run();
        Debug.Log("Tiny Swords: prefabs creados en " + Folder);
    }

    // ---------- Constructores ----------

    public static GameObject NewUnit(string name, string animId, Team team, float hp, float radius) => Unit(name, animId, team, hp, radius);

    public static Sprite FirstFrame(string animId) => FirstSprite(animId);

    static GameObject Unit(string name, string animId, Team team, float hp, float radius)
    {
        var go = new GameObject(name);
        int layer = LayerMask.NameToLayer("Units");
        if (layer >= 0) go.layer = layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite(animId);
        sr.spriteSortPoint = SpriteSortPoint.Pivot;

        go.AddComponent<Animator>().runtimeAnimatorController = TinySwordsSprites.LoadController(animId);
        var ua = go.AddComponent<UnitAnimator>();
        ua.animator = go.GetComponent<Animator>();
        ua.spriteRenderer = sr;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.offset = new Vector2(0, 0.1f);

        var h = go.AddComponent<Health>();
        h.team = team;
        h.maxHealth = hp;
        h.radius = radius;

        go.AddComponent<WorldHealthBar>().offset = new Vector2(0, 1.1f);
        return go;
    }

    static EnemyAI Enemy(string name, string animId, float hp, EnemyAttackType type, GameObject deathFx, ResourcePickup loot, int lootAmount)
    {
        var go = Unit(name, animId, Team.Goblins, hp, 0.35f);
        var h = go.GetComponent<Health>();
        h.deathEffectPrefab = deathFx;
        h.lootPrefab = loot;
        h.lootAmount = lootAmount;
        h.lootChance = 0.7f;
        var ai = go.AddComponent<EnemyAI>();
        ai.attackType = type;
        return ai;
    }

    static GameObject BuildingPrefab(string name, string displayName, string spriteBase, float hp, float radius,
        Vector2 colliderSize, Vector2 colliderOffset, GameObject fire, GameObject explosion)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.spriteSortPoint = SpriteSortPoint.Pivot;

        var h = go.AddComponent<Health>();
        h.team = Team.Knights;
        h.maxHealth = hp;
        h.radius = radius;
        h.isStructure = true;
        h.destroyOnDeath = false;

        var b = go.AddComponent<Building>();
        b.displayName = displayName;
        b.spriteRenderer = sr;
        b.completeSprite = SingleSprite($"{spriteBase}_{K}.png");
        b.constructionSprite = SingleSprite($"{spriteBase}_Construction.png");
        b.destroyedSprite = SingleSprite($"{spriteBase}_Destroyed.png");
        b.fireEffectPrefab = fire;
        b.destroyEffectPrefab = explosion;
        b.interactRange = radius + 1.2f;
        sr.sprite = b.completeSprite;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.offset = colliderOffset;

        go.AddComponent<WorldHealthBar>();
        return go;
    }

    static void BuildSpotPrefab(string name, Building building, Cost cost, string ghostSprite)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SingleSprite(ghostSprite);
        sr.color = new Color(1, 1, 1, 0.45f);
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        var spot = go.AddComponent<BuildSpot>();
        spot.buildingPrefab = building;
        spot.cost = cost;
        spot.interactRange = 1.8f;
        Save(go, "Buildings/" + name);
    }

    static GameObject Effect(string name, string animId, float lifetime, int sortingOrder)
    {
        var go = AnimatedObject(name, animId);
        go.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        if (lifetime > 0) go.AddComponent<AutoDestroy>().lifetime = lifetime;
        return Save(go, "Effects/" + name);
    }

    static Projectile ProjectilePrefab(string name, Sprite sprite, RuntimeAnimatorController controller)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        if (controller != null) go.AddComponent<Animator>().runtimeAnimatorController = controller;
        return go.AddComponent<Projectile>();
    }

    static ResourcePickup Pickup(string suffix, ResourceType type)
    {
        var go = AnimatedObject("Pickup_" + suffix, "Pickup" + suffix);
        go.transform.localScale = Vector3.one * 0.7f;
        var p = go.AddComponent<ResourcePickup>();
        p.type = type;
        return Save(go, "Resources/Pickup_" + suffix).GetComponent<ResourcePickup>();
    }

    static GameObject AnimatedObject(string name, string animId)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite(animId);
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        go.AddComponent<Animator>().runtimeAnimatorController = TinySwordsSprites.LoadController(animId);
        return go;
    }

    // ---------- Utilidades ----------

    static GameObject Save(GameObject go, string relativePath)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Folder + relativePath + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Sprite SingleSprite(string packPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Pack + packPath);
    }

    static Sprite SpriteFrom(string packPath, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Pack + packPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
    }

    // Primer frame de la animación por defecto de un controller generado (para que se vea en el editor).
    static Sprite FirstSprite(string animId)
    {
        var controller = TinySwordsSprites.LoadController(animId);
        if (controller == null || controller.animationClips.Length == 0) return null;
        var clip = controller.animationClips[0];
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys.Length > 0) return keys[0].value as Sprite;
        }
        return null;
    }
}
