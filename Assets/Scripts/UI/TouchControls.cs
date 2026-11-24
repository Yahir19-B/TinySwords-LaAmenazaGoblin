using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controles táctiles para celular y tableta: palanca a la izquierda y botones a la derecha.
// Se crean solos en cada nivel (no hay que modificar las escenas) y en computadora no aparecen.
public class TouchControls : MonoBehaviour
{
    // Para probarlos en el editor: ponlo en true antes de dar Play.
    public static bool ForceInEditor;

    public static bool Enabled => isTouchDevice || (Application.isEditor && ForceInEditor);

    // La página web manda "-tactil" en el iPad, que se presenta como computadora.
    static readonly bool isTouchDevice = Application.isMobilePlatform || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-tactil") >= 0;

    // Lo que leen el héroe y sus poderes.
    public static Vector2 Move { get; private set; }
    public static bool AttackHeld { get; private set; }
    static int dashFrame = -1, whirlwindFrame = -1, potionFrame = -1;
    public static bool DashDown => dashFrame == Time.frameCount;
    public static bool WhirlwindDown => whirlwindFrame == Time.frameCount;
    public static bool PotionDown => potionFrame == Time.frameCount;

    public enum Kind { Attack, Dash, Whirlwind, Potion }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Release();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (!Enabled) return;
        // Sin esto, cualquier toque (hasta el de la palanca) contaría como clic de ataque.
        Input.simulateMouseWithTouches = false;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void Release()
    {
        Move = Vector2.zero;
        AttackHeld = false;
        dashFrame = whirlwindFrame = potionFrame = -1;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Release();
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)) t.text = TouchText(t.text);
        var hud = LevelHUD.Instance;
        if (hud != null) Build(hud);
    }

    // La ventana "Cómo jugar" habla de teclas: en el celular se cambian por los botones.
    static string TouchText(string s)
    {
        switch (s)
        {
            case "W A S D  /  Flechas": return "Palanca izquierda";
            case "Espacio  /  J  /  Clic": return "Botón Atacar";
            case "Shift  /  K": return "Botón Esquivar";
            case "Esc": return "Botón de arriba";
            case "H": return "Pausa > Controles";
            case "Q  /  R": return "Botones de poderes";
            case "Torbellino  [Q]": return "Torbellino";
            case "Poción de vida  [R]": return "Poción de vida";
            case "H: controles   ·   Esc: pausa": return "";
            default: return s;
        }
    }

    static void Build(LevelHUD hud)
    {
        var root = new GameObject("ControlesTactiles", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(hud.transform, false);
        root.SetAsFirstSibling();   // detrás de las ventanas de pausa, victoria, etc.
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        FitSafeArea(root);
        root.gameObject.AddComponent<TouchControls>();

        // Zona de la palanca: toda la parte baja izquierda.
        var zone = NewImage("ZonaPalanca", root, new Color(0, 0, 0, 0));
        zone.rectTransform.anchorMin = Vector2.zero;
        zone.rectTransform.anchorMax = new Vector2(0.45f, 0.65f);
        zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
        zone.gameObject.AddComponent<TouchStick>().Setup();
    }

    // Deja libre la muesca y las esquinas redondeadas del teléfono.
    static void FitSafeArea(RectTransform rt)
    {
        var safe = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
    }

    // Lo llama PlayerLoadout: convierte el recuadro de un poder en botón táctil.
    public static void MakeButton(GameObject frame, Kind kind) => frame.AddComponent<TouchButton>().kind = kind;

    // Lugar de cada botón, medido desde la esquina inferior derecha.
    public static Vector2 Position(Kind kind)
    {
        switch (kind)
        {
            case Kind.Attack: return new Vector2(-50, 50);
            case Kind.Dash: return new Vector2(-300, 50);
            case Kind.Whirlwind: return new Vector2(-250, 230);
            default: return new Vector2(-50, 300);
        }
    }

    public static float Size(Kind kind) => kind == Kind.Attack ? 220 : 160;

    static Image NewImage(string name, Transform parent, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    void OnDisable() => Release();

    // ---------- Palanca ----------

    class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float Radius = 130f;
        static readonly Vector2 Rest = new Vector2(230, 220);
        RectTransform zone, ring, knob;
        int pointer = int.MinValue;

        public void Setup()
        {
            zone = (RectTransform)transform;
            var circle = Circle();
            ring = Piece("Aro", transform, circle, new Color(0.05f, 0.03f, 0.03f, 0.45f), Radius * 2);
            knob = Piece("Perilla", ring, circle, new Color(1f, 0.84f, 0.35f, 0.75f), Radius * 0.9f);
            ring.anchorMin = ring.anchorMax = Vector2.zero;
            ring.anchoredPosition = Rest;
        }

        static RectTransform Piece(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            var img = NewImage(name, parent, color);
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img.rectTransform;
        }

        // Círculo suave dibujado por código.
        static Sprite Circle()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f));
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(n / 2f - 1f - d)));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var p);
            return p - zone.rect.min;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            // La palanca aparece donde pones el dedo.
            ring.anchoredPosition = Local(e);
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            var d = Vector2.ClampMagnitude(Local(e) - ring.anchoredPosition, Radius);
            knob.anchoredPosition = d;
            Move = d.magnitude < Radius * 0.2f ? Vector2.zero : d / Radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointer) Stop();
        }

        void Stop()
        {
            pointer = int.MinValue;
            Move = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            ring.anchoredPosition = Rest;
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && knob != null) Stop();
        }
    }

    // ---------- Botones ----------

    class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Kind kind;

        public void OnPointerDown(PointerEventData e)
        {
            transform.localScale = Vector3.one * 0.92f;
            // Se avisa en el siguiente frame para que el héroe siempre alcance a leerlo.
            if (kind == Kind.Attack) AttackHeld = true;
            else if (kind == Kind.Dash) dashFrame = Time.frameCount + 1;
            else if (kind == Kind.Whirlwind) whirlwindFrame = Time.frameCount + 1;
            else potionFrame = Time.frameCount + 1;
        }

        public void OnPointerUp(PointerEventData e)
        {
            transform.localScale = Vector3.one;
            if (kind == Kind.Attack) AttackHeld = false;
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            transform.localScale = Vector3.one;
            if (kind == Kind.Attack) AttackHeld = false;
        }
    }
}
