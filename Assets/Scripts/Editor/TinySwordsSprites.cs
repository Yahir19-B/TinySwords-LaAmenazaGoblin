using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Paso 1: configura los .png de Tiny Swords (pixel art, 64 px por unidad), trocea los sprite sheets
// en frames y crea los Animation Clips y Animator Controllers en Assets/Animations/Generated.
// Se puede ejecutar varias veces: conserva los IDs de los sprites, así que no rompe animaciones existentes.
public static class TinySwordsSprites
{
    public const string Pack = "Assets/images/Tiny Swords (Update 010)/";
    public const string AnimFolder = "Assets/Animations/Generated/";
    public const int PixelsPerUnit = 64;

    public const string KnightColor = "Blue";
    public const string GoblinColor = "Red";

    static readonly Vector2 FeetPivot = new Vector2(0.5f, 0.35f);

    class Anim
    {
        public string name;
        public int row;
        public bool loop;
        public float fps;

        public Anim(string name, int row, bool loop = true, float fps = 10f)
        {
            this.name = name;
            this.row = row;
            this.loop = loop;
            this.fps = fps;
        }
    }

    class Sheet
    {
        public string id;          // nombre del controller
        public string path;
        public int cellW, cellH;
        public Vector2 pivot;
        public Anim[] anims;
    }

    static Sheet[] Sheets => new[]
    {
        new Sheet { id = "Warrior", path = $"Factions/Knights/Troops/Warrior/{KnightColor}/Warrior_{KnightColor}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 2, false, 12), new Anim("Attack2", 3, false, 12),
                            new Anim("AttackDown", 4, false, 12), new Anim("AttackDown2", 5, false, 12), new Anim("AttackUp", 6, false, 12), new Anim("AttackUp2", 7, false, 12) } },
        WarriorSheet("Red"), WarriorSheet("Purple"), WarriorSheet("Yellow"),
        new Sheet { id = "Pawn", path = $"Factions/Knights/Troops/Pawn/{KnightColor}/Pawn_{KnightColor}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Build", 2), new Anim("Chop", 3), new Anim("IdleCarry", 4), new Anim("RunCarry", 5) } },
        new Sheet { id = "Archer", path = $"Factions/Knights/Troops/Archer/{KnightColor}/Archer_{KnightColor}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Shoot", 4, false, 14) } },
        new Sheet { id = "Torch", path = $"Factions/Goblins/Troops/Torch/{GoblinColor}/Torch_{GoblinColor}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 2, false, 12), new Anim("AttackDown", 3, false, 12), new Anim("AttackUp", 4, false, 12) } },
        new Sheet { id = "TNT", path = $"Factions/Goblins/Troops/TNT/{GoblinColor}/TNT_{GoblinColor}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 2, false, 12) } },
        new Sheet { id = "TorchYellow", path = "Factions/Goblins/Troops/Torch/Yellow/Torch_Yellow.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1, true, 14), new Anim("Attack", 2, false, 14), new Anim("AttackDown", 3, false, 14), new Anim("AttackUp", 4, false, 14) } },
        new Sheet { id = "TNTPurple", path = "Factions/Goblins/Troops/TNT/Purple/TNT_Purple.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 2, false, 12) } },
        new Sheet { id = "ArcherEnemy", path = "Factions/Knights/Troops/Archer/Purple/Archer_Purlple.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 4, false, 14) } },
        new Sheet { id = "Barrel", path = $"Factions/Goblins/Troops/Barrel/{GoblinColor}/Barrel_{GoblinColor}.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Idle", 0), new Anim("Run", 4), new Anim("Attack", 5, true, 10) } },
        new Sheet { id = "Boss", path = "Factions/Goblins/Troops/Torch/Purple/Torch_Purple.png", cellW = 192, cellH = 192, pivot = FeetPivot,
            anims = new[] { new Anim("Idle", 0, true, 8), new Anim("Run", 1, true, 8), new Anim("Attack", 2, false, 10), new Anim("AttackDown", 3, false, 10), new Anim("AttackUp", 4, false, 10) } },
        new Sheet { id = "GoblinTower", path = $"Factions/Goblins/Buildings/Wood_Tower/Wood_Tower_{GoblinColor}.png", cellW = 256, cellH = 192, pivot = new Vector2(0.5f, 0.12f),
            anims = new[] { new Anim("Idle", 0, true, 6) } },
        new Sheet { id = "Foam", path = "Terrain/Water/Foam/Foam.png", cellW = 192, cellH = 192, pivot = new Vector2(0.5f, 0.5f),
            anims = new Anim[0] },
        new Sheet { id = "Dead", path = "Factions/Knights/Troops/Dead/Dead.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Dead", 0, false, 10) } },
        new Sheet { id = "Arrow", path = "Factions/Knights/Troops/Archer/Arrow/Arrow.png", cellW = 64, cellH = 64, pivot = new Vector2(0.5f, 0.5f),
            anims = new Anim[0] },
        new Sheet { id = "Dynamite", path = "Factions/Goblins/Troops/TNT/Dynamite/Dynamite.png", cellW = 64, cellH = 64, pivot = new Vector2(0.5f, 0.5f),
            anims = new[] { new Anim("Idle", 0, true, 14) } },
        new Sheet { id = "Explosion", path = "Effects/Explosion/Explosions.png", cellW = 192, cellH = 192, pivot = new Vector2(0.5f, 0.4f),
            anims = new[] { new Anim("Idle", 0, false, 14) } },
        new Sheet { id = "Fire", path = "Effects/Fire/Fire.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.2f),
            anims = new[] { new Anim("Idle", 0, true, 10) } },
        new Sheet { id = "Tree", path = "Resources/Trees/Tree.png", cellW = 192, cellH = 192, pivot = new Vector2(0.5f, 0.12f),
            anims = new[] { new Anim("Idle", 0, true, 6), new Anim("Chop", 1, true, 8), new Anim("Stump", 2) } },
        new Sheet { id = "Sheep", path = "Resources/Sheep/HappySheep_Idle.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Idle", 0, true, 8) } },
        new Sheet { id = "PickupGold", path = "Resources/Resources/G_Spawn.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Idle", 0, false, 12) } },
        new Sheet { id = "PickupWood", path = "Resources/Resources/W_Spawn.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Idle", 0, false, 12) } },
        new Sheet { id = "PickupMeat", path = "Resources/Resources/M_Spawn.png", cellW = 128, cellH = 128, pivot = new Vector2(0.5f, 0.3f),
            anims = new[] { new Anim("Idle", 0, false, 12) } },
    };

    // Otros colores del guerrero para las skins de la tienda (mismas animaciones que el azul).
    static Sheet WarriorSheet(string color) => new Sheet
    {
        id = "Warrior_" + color, path = $"Factions/Knights/Troops/Warrior/{color}/Warrior_{color}.png", cellW = 192, cellH = 192, pivot = FeetPivot,
        anims = new[] { new Anim("Idle", 0), new Anim("Run", 1), new Anim("Attack", 2, false, 12), new Anim("Attack2", 3, false, 12),
                        new Anim("AttackDown", 4, false, 12), new Anim("AttackDown2", 5, false, 12), new Anim("AttackUp", 6, false, 12), new Anim("AttackUp2", 7, false, 12) }
    };

    // Sprites sueltos (edificios, mina, iconos) con su pivote.
    static (string path, Vector2 pivot)[] Singles => new[]
    {
        ($"Factions/Knights/Buildings/Castle/Castle_{KnightColor}.png", new Vector2(0.5f, 0.12f)),
        ("Factions/Knights/Buildings/Castle/Castle_Construction.png", new Vector2(0.5f, 0.12f)),
        ("Factions/Knights/Buildings/Castle/Castle_Destroyed.png", new Vector2(0.5f, 0.12f)),
        ($"Factions/Knights/Buildings/House/House_{KnightColor}.png", new Vector2(0.5f, 0.1f)),
        ("Factions/Knights/Buildings/House/House_Construction.png", new Vector2(0.5f, 0.1f)),
        ("Factions/Knights/Buildings/House/House_Destroyed.png", new Vector2(0.5f, 0.1f)),
        ($"Factions/Knights/Buildings/Tower/Tower_{KnightColor}.png", new Vector2(0.5f, 0.08f)),
        ("Factions/Knights/Buildings/Tower/Tower_Construction.png", new Vector2(0.5f, 0.08f)),
        ("Factions/Knights/Buildings/Tower/Tower_Destroyed.png", new Vector2(0.5f, 0.08f)),
        ("Resources/Gold Mine/GoldMine_Active.png", new Vector2(0.5f, 0.2f)),
        ("Resources/Gold Mine/GoldMine_Inactive.png", new Vector2(0.5f, 0.2f)),
        ("Resources/Gold Mine/GoldMine_Destroyed.png", new Vector2(0.5f, 0.2f)),
        ("Factions/Goblins/Buildings/Wood_House/Goblin_House.png", new Vector2(0.5f, 0.1f)),
        ("Factions/Goblins/Buildings/Wood_House/Goblin_House_Destroyed.png", new Vector2(0.5f, 0.1f)),
        ("Factions/Goblins/Buildings/Wood_Tower/Wood_Tower_Destroyed.png", new Vector2(0.5f, 0.12f)),
        ("Resources/Resources/G_Idle_(NoShadow).png", new Vector2(0.5f, 0.5f)),
        ("Resources/Resources/W_Idle_(NoShadow).png", new Vector2(0.5f, 0.5f)),
        ("Resources/Resources/M_Idle_(NoShadow).png", new Vector2(0.5f, 0.5f)),
    };

    // Decoraciones Deco/01..18 con el pivote en la base.
    static IEnumerable<(string, Vector2)> DecoSingles()
    {
        for (int i = 1; i <= 18; i++)
        {
            Vector2 pivot = i == 18 ? new Vector2(0.5f, 0.3f) : i >= 16 ? new Vector2(0.5f, 0.1f) : new Vector2(0.5f, 0.25f);
            yield return ($"Deco/{i:00}.png", pivot);
        }
    }

    // Sprites de UI y su borde para estirarlos como 9 partes (izquierda, abajo, derecha, arriba).
    static IEnumerable<(string path, Vector4 border)> UISprites()
    {
        var nine = new Vector4(64, 64, 64, 64);
        var three = new Vector4(64, 0, 64, 0);
        foreach (var n in new[] { "Carved_9Slides", "Banner_Horizontal", "Banner_Vertical" }) yield return ($"UI/Banners/{n}.png", nine);
        yield return ("UI/Banners/Carved_3Slides.png", three);
        foreach (var c in new[] { "Blue", "Red", "Hover", "Disable" })
        {
            yield return ($"UI/Buttons/Button_{c}_9Slides.png", nine);
            yield return ($"UI/Buttons/Button_{c}_3Slides.png", three);
        }
        foreach (var c in new[] { "Blue", "Red" })
        {
            yield return ($"UI/Buttons/Button_{c}_9Slides_Pressed.png", nine);
            yield return ($"UI/Buttons/Button_{c}_3Slides_Pressed.png", three);
        }
        foreach (var c in new[] { "Blue", "Red", "Yellow" }) yield return ($"UI/Ribbons/Ribbon_{c}_3Slides.png", three);
        for (int i = 1; i <= 10; i++) yield return ($"UI/Icons/Regular_{i:00}.png", Vector4.zero);
    }

    static void ConfigureUISprite(string path, Vector4 border)
    {
        var importer = GetImporter(path);
        if (importer == null) return;
        ApplyPixelArtSettings(importer, SpriteImportMode.Single);
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    // El cursor necesita poder leerse desde código.
    static void ConfigureCursor(string path)
    {
        var importer = GetImporter(path);
        if (importer == null) return;
        ApplyPixelArtSettings(importer, SpriteImportMode.Single);
        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    [MenuItem("Tiny Swords/1. Configurar sprites y animaciones", priority = 1)]
    public static void Run()
    {
        EnsureFolder(AnimFolder.TrimEnd('/'));
        try
        {
            var sheets = Sheets;
            var singles = Singles.Concat(DecoSingles()).ToArray();
            foreach (var (path, border) in UISprites()) ConfigureUISprite(Pack + path, border);
            ConfigureCursor(Pack + "UI/Pointers/01.png");
            int total = sheets.Length + singles.Length;
            int i = 0;

            foreach (var (path, pivot) in singles)
            {
                EditorUtility.DisplayProgressBar("Tiny Swords", path, (float)i++ / total);
                ConfigureSingle(Pack + path, pivot);
            }

            foreach (var sheet in sheets)
            {
                EditorUtility.DisplayProgressBar("Tiny Swords", sheet.path, (float)i++ / total);
                var grid = SliceGrid(Pack + sheet.path, sheet.cellW, sheet.cellH, sheet.pivot);
                if (grid == null || sheet.anims.Length == 0) continue;
                BuildAnimations(sheet, grid);
            }

            BuildFoamTile();
            ConfigureSortingAxis();
            EnsureLayer("Units");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Tiny Swords: sprites y animaciones listos en " + AnimFolder);
    }

    // ---------- Importación ----------

    static TextureImporter GetImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) Debug.LogWarning("Tiny Swords: no se encontró " + path);
        return importer;
    }

    static bool ApplyPixelArtSettings(TextureImporter importer, SpriteImportMode mode)
    {
        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != mode
            || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        return changed;
    }

    static void ConfigureSingle(string path, Vector2 pivot)
    {
        var importer = GetImporter(path);
        if (importer == null) return;
        ApplyPixelArtSettings(importer, SpriteImportMode.Single);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // Trocea una imagen en celdas y devuelve los sprites por [fila][columna] (solo celdas con dibujo).
    static List<List<Sprite>> SliceGrid(string path, int cellW, int cellH, Vector2 pivot)
    {
        var importer = GetImporter(path);
        if (importer == null) return null;

        if (ApplyPixelArtSettings(importer, SpriteImportMode.Multiple))
            importer.SaveAndReimport();

        var pixels = LoadPixels(path, out int texW, out int texH);
        int cols = texW / cellW;
        int rows = texH / cellH;
        string baseName = Path.GetFileNameWithoutExtension(path);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var existingIds = new Dictionary<string, GUID>();
        foreach (var r in provider.GetSpriteRects())
            if (!existingIds.ContainsKey(r.name)) existingIds.Add(r.name, r.spriteID);

        var rects = new List<SpriteRect>();
        var names = new string[rows, cols];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Las filas de la imagen se cuentan desde arriba; en Unity la Y empieza abajo.
                var rect = new Rect(col * cellW, texH - (row + 1) * cellH, cellW, cellH);
                if (IsEmpty(pixels, texW, rect)) continue;

                string name = $"{baseName}_{row * cols + col}";
                names[row, col] = name;
                rects.Add(new SpriteRect
                {
                    name = name,
                    rect = rect,
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = existingIds.TryGetValue(name, out var id) ? id : GUID.Generate()
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        var grid = new List<List<Sprite>>();
        for (int row = 0; row < rows; row++)
        {
            var list = new List<Sprite>();
            for (int col = 0; col < cols; col++)
            {
                if (names[row, col] == null) break; // la animación termina en la primera celda vacía
                if (sprites.TryGetValue(names[row, col], out var s)) list.Add(s);
            }
            grid.Add(list);
        }
        return grid;
    }

    static Color32[] LoadPixels(string path, out int width, out int height)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        width = tex.width;
        height = tex.height;
        var pixels = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        return pixels;
    }

    static bool IsEmpty(Color32[] pixels, int texW, Rect rect)
    {
        for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
            for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                if (pixels[y * texW + x].a > 10) return false;
        return true;
    }

    // ---------- Animaciones ----------

    static void BuildAnimations(Sheet sheet, List<List<Sprite>> grid)
    {
        string folder = AnimFolder + sheet.id;
        EnsureFolder(folder);

        var states = new List<(string, AnimationClip)>();
        foreach (var anim in sheet.anims)
        {
            if (anim.row >= grid.Count || grid[anim.row].Count == 0)
            {
                Debug.LogWarning($"Tiny Swords: {sheet.id} no tiene frames en la fila {anim.row} ({anim.name})");
                continue;
            }
            var clip = SaveClip($"{folder}/{sheet.id}_{anim.name}.anim", grid[anim.row], anim.fps, anim.loop);
            states.Add((anim.name, clip));
        }
        SaveController($"{folder}/{sheet.id}.controller", states);
    }

    static AnimationClip SaveClip(string path, List<Sprite> frames, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps, name = Path.GetFileNameWithoutExtension(path) };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        // Se repite el último frame al final para que dure lo mismo que los demás.
        var keys = new ObjectReferenceKeyframe[frames.Count + 1];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / fps, value = frames[frames.Count - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void SaveController(string path, List<(string name, AnimationClip clip)> states)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
            ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);

        // Sin transiciones: los scripts cambian de estado con UnitAnimator.Play("Nombre").
        for (int i = 0; i < states.Count; i++)
        {
            var state = sm.AddState(states[i].name, new Vector3(300, 60 * i, 0));
            state.motion = states[i].clip;
            if (i == 0) sm.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
    }

    public const string FoamTilePath = AnimFolder + "FoamTile.asset";

    // Tile animado con la espuma blanca del borde de las islas.
    static void BuildFoamTile()
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(Pack + "Terrain/Water/Foam/Foam.png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        if (frames.Length == 0) return;
        var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.AnimatedTile>(FoamTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.AnimatedTile>();
            AssetDatabase.CreateAsset(tile, FoamTilePath);
        }
        tile.m_AnimatedSprites = frames;
        tile.m_MinSpeed = 8f;
        tile.m_MaxSpeed = 8f;
        EditorUtility.SetDirty(tile);
    }

    public static RuntimeAnimatorController LoadController(string id)
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimFolder}{id}/{id}.controller");
    }

    public static float ClipLength(string id, string anim)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimFolder}{id}/{id}_{anim}.anim");
        return clip != null ? clip.length : 1f;
    }

    // ---------- Proyecto ----------

    // Los objetos más abajo en pantalla se dibujan encima (profundidad por Y).
    static void ConfigureSortingAxis()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Renderer2DData"))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(asset);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode == null || axis == null) continue;
            mode.intValue = (int)TransparencySortMode.CustomAxis;
            axis.vector3Value = new Vector3(0, 1, 0);
            so.ApplyModifiedProperties();
        }
        UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);
    }

    public static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var prop = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogWarning("Tiny Swords: no hay espacio para la capa " + layerName);
        return -1;
    }

    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
