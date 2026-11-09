using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ayudantes para construir la interfaz con los sprites de Tiny Swords (UI/Banners, Buttons, Ribbons, Icons).
public static class TinySwordsUI
{
    const string UI = TinySwordsSprites.Pack + "UI/";
    const string Fonts = "Assets/TextMesh Pro/Resources/Fonts & Materials/";

    public static readonly Color Parchment = new Color(0.25f, 0.16f, 0.1f);   // texto oscuro sobre pergamino
    public static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
    public static readonly Color Blood = new Color(0.85f, 0.2f, 0.15f);

    public static Sprite S(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(UI + relative);
    public static Sprite Icon(int n) => S($"Icons/Regular_{n:00}.png");
    public const int IconClose = 1, IconSettings = 2, IconLock = 10;

    // ---------- Básicos ----------

    public static Canvas NewCanvas(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    // Ancla y pivote en el mismo punto: "pos" es el desplazamiento desde ese punto.
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float padding = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
        return rt;
    }

    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    // Imagen estirada en 9 partes. "border" es el tamaño en pantalla de las esquinas.
    public static Image Sliced(string name, Transform parent, Sprite sprite, float border = 60f)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        // Con 64 px por unidad, un borde de 64 px mide 100 unidades de UI; el multiplicador lo ajusta.
        img.pixelsPerUnitMultiplier = 100f / border;
        return img;
    }

    public static Image Plain(string name, Transform parent, Sprite sprite, Color? color = null)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = sprite;
        img.preserveAspect = sprite != null;
        img.color = color ?? Color.white;
        return img;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string content, float size,
        TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null, bool outline = false)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color ?? Color.white;
        t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Fonts + (outline ? "LiberationSans SDF - Outline.mat" : "LiberationSans SDF - Drop Shadow.mat"));
        if (mat != null && color != Parchment) t.fontSharedMaterial = mat;
        return t;
    }

    // ---------- Piezas de Tiny Swords ----------

    // Botón largo (Button_*_3Slides) con cambio de sprite al pasar el mouse, presionar o deshabilitar.
    public static Button Button(string name, Transform parent, string label, bool red = false, float fontSize = 34)
    {
        string c = red ? "Red" : "Blue";
        var img = Sliced(name, parent, S($"Buttons/Button_{c}_3Slides.png"), 60);
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState
        {
            highlightedSprite = S("Buttons/Button_Hover_3Slides.png"),
            selectedSprite = S($"Buttons/Button_{c}_3Slides.png"),
            pressedSprite = S($"Buttons/Button_{c}_3Slides_Pressed.png"),
            disabledSprite = S("Buttons/Button_Disable_3Slides.png")
        };
        var t = Text("Texto", img.transform, label, fontSize, TextAlignmentOptions.Center, Color.white, true);
        Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(20, 12);   // el sprite tiene sombra abajo
        t.rectTransform.offsetMax = new Vector2(-20, -4);
        return b;
    }

    // Botón cuadrado (Button_*_9Slides), por ejemplo para los niveles o cerrar.
    public static Button SquareButton(string name, Transform parent, bool red = false, float border = 40)
    {
        string c = red ? "Red" : "Blue";
        var img = Sliced(name, parent, S($"Buttons/Button_{c}_9Slides.png"), border);
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState
        {
            highlightedSprite = S("Buttons/Button_Hover_9Slides.png"),
            selectedSprite = S($"Buttons/Button_{c}_9Slides.png"),
            pressedSprite = S($"Buttons/Button_{c}_9Slides_Pressed.png"),
            disabledSprite = S("Buttons/Button_Disable_9Slides.png")
        };
        return b;
    }

    // Cinta de título (Ribbon_*_3Slides). color: "Red", "Blue" o "Yellow".
    public static TextMeshProUGUI Ribbon(string name, Transform parent, string text, string color, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize = 44)
    {
        var img = Sliced(name, parent, S($"Ribbons/Ribbon_{color}_3Slides.png"), size.y);
        Place(img.rectTransform, anchor, pos, size);
        var t = Text("Texto", img.transform, text, fontSize, TextAlignmentOptions.Center, Color.white, true);
        Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(size.y * 0.9f, size.y * 0.18f);
        t.rectTransform.offsetMax = new Vector2(-size.y * 0.9f, -size.y * 0.05f);
        t.enableAutoSizing = true;
        t.fontSizeMin = 18;
        t.fontSizeMax = fontSize;
        return t;
    }

    // Panel de madera tallada (Carved_9Slides).
    public static Image Carved(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float border = 36)
    {
        var img = Sliced(name, parent, S("Banners/Carved_9Slides.png"), border);
        Place(img.rectTransform, anchor, pos, size);
        return img;
    }

    // Pergamino grande (Banner_Horizontal) para ventanas.
    public static Image Scroll(string name, Transform parent, Vector2 size)
    {
        var img = Sliced(name, parent, S("Banners/Banner_Horizontal.png"), 90);
        Place(img.rectTransform, Center, Vector2.zero, size);
        return img;
    }

    // Fondo oscuro que cubre la pantalla detrás de una ventana.
    public static Image Overlay(string name, Transform parent, float alpha = 0.7f)
    {
        var img = Plain(name, parent, null, new Color(0.03f, 0.01f, 0.02f, alpha));
        Stretch(img.rectTransform);
        return img;
    }

    // Barra con marco de madera. Devuelve el relleno (se llena moviendo anchorMax.x).
    public static RectTransform Bar(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
    {
        var frame = Sliced(name, parent, S("Banners/Carved_3Slides.png"), size.y);
        Place(frame.rectTransform, anchor, pos, size);
        var back = Plain("Fondo", frame.transform, null, new Color(0.1f, 0.05f, 0.05f, 1f));
        Stretch(back.rectTransform);
        back.rectTransform.offsetMin = new Vector2(size.y * 0.35f, size.y * 0.28f);
        back.rectTransform.offsetMax = new Vector2(-size.y * 0.35f, -size.y * 0.28f);
        var fill = Plain("Relleno", back.transform, null, fillColor);
        Stretch(fill.rectTransform);
        return fill.rectTransform;
    }

    public static Button IconButton(string name, Transform parent, int icon, bool red, Vector2 anchor, Vector2 pos, float size = 80)
    {
        var b = SquareButton(name, parent, red, size * 0.3f);
        Place((RectTransform)b.transform, anchor, pos, new Vector2(size, size));
        var i = Plain("Icono", b.transform, Icon(icon));
        Stretch(i.rectTransform, size * 0.22f);
        i.rectTransform.offsetMin += new Vector2(0, size * 0.06f);
        return b;
    }

    public static UIButtonAction Action(Button b, UIButtonAction.Action action, GameObject target = null, int level = 1)
    {
        var a = b.gameObject.AddComponent<UIButtonAction>();
        a.action = action;
        a.target = target;
        a.level = level;
        return a;
    }

    // Columna de botones centrada dentro de un contenedor, empezando en "top".
    public static Button[] ButtonColumn(Transform parent, float top, Vector2 size, float gap, params (string label, bool red)[] buttons)
    {
        var result = new Button[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            result[i] = Button(buttons[i].label, parent, buttons[i].label, buttons[i].red);
            Place((RectTransform)result[i].transform, new Vector2(0.5f, 1), new Vector2(0, -top - i * (size.y + gap)), size);
        }
        return result;
    }

    // ---------- Panel de controles (tutorial) ----------

    // closeButtonAction: en el menú oculta el panel; en un nivel reanuda el juego.
    public static GameObject ControlsPanel(Transform parent, System.Action<Button> wireClose)
    {
        var overlay = Overlay("Controles", parent, 0.75f);
        var scroll = Scroll("Pergamino", overlay.transform, new Vector2(1180, 860));
        Ribbon("Titulo", overlay.transform, "Cómo jugar", "Blue", Center, new Vector2(0, 390), new Vector2(620, 110), 50);

        var rows = new (string keys, string action)[]
        {
            ("W A S D  /  Flechas", "Moverse"),
            ("Espacio  /  J  /  Clic", "Atacar con la espada"),
            ("Shift  /  K", "Esquivar (no recibes daño)"),
            ("Esc", "Pausar el juego"),
            ("H", "Ver estos controles"),
            ("Q  /  R", "Poderes (se compran en la Tienda)"),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float y = 272 - i * 68;
            var key = Sliced("Tecla", scroll.transform, S("Buttons/Button_Blue_9Slides.png"), 26);
            Place(key.rectTransform, Center, new Vector2(-230, y), new Vector2(430, 60));
            var kt = Text("Texto", key.transform, rows[i].keys, 30, TextAlignmentOptions.Center, Color.white, true);
            Stretch(kt.rectTransform);
            kt.rectTransform.offsetMin = new Vector2(10, 10);
            var at = Text("Accion", scroll.transform, rows[i].action, 34, TextAlignmentOptions.Left, Parchment);
            Place(at.rectTransform, Center, new Vector2(230, y), new Vector2(460, 66));
        }

        var tips = new (string sprite, string text)[]
        {
            ("Resources/Resources/M_Idle_(NoShadow).png", "La carne te cura"),
            ("Resources/Resources/G_Idle_(NoShadow).png", "Los goblins sueltan oro"),
        };
        for (int i = 0; i < tips.Length; i++)
        {
            float x = -250 + i * 500;
            var ic = Plain("Icono", scroll.transform, AssetDatabase.LoadAssetAtPath<Sprite>(TinySwordsSprites.Pack + tips[i].sprite));
            Place(ic.rectTransform, Center, new Vector2(x - 150, -150), new Vector2(70, 70));
            var tt = Text("Tip", scroll.transform, tips[i].text, 28, TextAlignmentOptions.Left, Parchment);
            Place(tt.rectTransform, Center, new Vector2(x + 60, -150), new Vector2(330, 70));
        }

        var goal = Text("Objetivo", scroll.transform,
            "Derrota a todos los goblins y destruye sus casas para ganar.\nEn el nivel 10 te espera el Gran Jefe Goblin.", 30, TextAlignmentOptions.Center, Parchment);
        Place(goal.rectTransform, Center, new Vector2(0, -245), new Vector2(980, 100));

        var ok = Button("Entendido", overlay.transform, "¡A pelear!");
        Place((RectTransform)ok.transform, Center, new Vector2(0, -385), new Vector2(420, 100));
        wireClose(ok);
        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }
}
