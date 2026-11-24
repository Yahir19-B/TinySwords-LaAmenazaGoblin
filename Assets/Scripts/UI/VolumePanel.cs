using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controles de volumen (música y efectos) en el menú principal y en la ventana de pausa.
// Se arma por código con los sprites que ya usa la escena, así no hay que modificar los niveles.
public class VolumePanel : MonoBehaviour
{
    Slider musicSlider, effectsSlider;
    TMP_Text hint;

    public static void AddToScene(MainMenu menu, LevelHUD hud)
    {
        if (menu != null)
        {
            var gold = menu.transform.Find("OroTotal");
            var box = Build(menu.transform, gold, Vector2.zero, new Vector2(30, 30), new Vector2(430, 190), false);
            // Justo después del oro para quedar detrás de las ventanas (niveles, tienda...).
            if (gold != null) box.SetSiblingIndex(gold.GetSiblingIndex() + 1);
        }
        if (hud != null && hud.pausePanel != null)
            Build(hud.pausePanel.transform, hud.transform.Find("Heroe"), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(760, 140), true);
    }

    static RectTransform Build(Transform parent, Transform spriteSource, Vector2 anchor, Vector2 pos, Vector2 size, bool horizontal)
    {
        var frame = NewImage("Volumen", parent, Color.white);
        var source = spriteSource != null ? spriteSource.GetComponent<Image>() : null;
        if (source != null)
        {
            frame.sprite = source.sprite;
            frame.type = source.type;
            frame.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }
        else frame.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);
        var rt = frame.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var panel = frame.gameObject.AddComponent<VolumePanel>();
        if (horizontal)
        {
            panel.musicSlider = NewRow(rt, "Música", new Vector2(-180, 16), AudioManager.MusicVolume);
            panel.effectsSlider = NewRow(rt, "Efectos", new Vector2(180, 16), AudioManager.EffectsVolume);
        }
        else
        {
            panel.musicSlider = NewRow(rt, "Música", new Vector2(0, 45), AudioManager.MusicVolume);
            panel.effectsSlider = NewRow(rt, "Efectos", new Vector2(0, -5), AudioManager.EffectsVolume);
        }
        panel.hint = NewText(rt, "", 22, new Vector2(0, -size.y / 2 + 38), new Vector2(size.x - 40, 30), TextAlignmentOptions.Center);

        panel.musicSlider.onValueChanged.AddListener(v => AudioManager.MusicVolume = v);
        panel.effectsSlider.onValueChanged.AddListener(v => { AudioManager.EffectsVolume = v; AudioManager.Play("moneda", 0.7f); });
        return rt;
    }

    void Update()
    {
        string text = AudioManager.Muted ? "SILENCIADO (M para oír)" : "M: silenciar";
        if (hint.text != text) hint.text = text;
    }

    void OnEnable()
    {
        if (musicSlider == null) return;
        musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
        effectsSlider.SetValueWithoutNotify(AudioManager.EffectsVolume);
    }

    static Slider NewRow(Transform parent, string label, Vector2 pos, float value)
    {
        NewText(parent, label, 28, pos + new Vector2(-100, 0), new Vector2(130, 40), TextAlignmentOptions.Left);

        var back = NewImage("Barra " + label, parent, new Color(0.1f, 0.05f, 0.05f, 1f));
        Center(back.rectTransform, pos + new Vector2(70, 0), new Vector2(190, 18));

        var fill = NewImage("Relleno", back.transform, new Color(1f, 0.84f, 0.35f));
        var fr = fill.rectTransform;
        fr.offsetMin = new Vector2(3, 3);
        fr.offsetMax = new Vector2(-3, -3);

        var handle = NewImage("Perilla", back.transform, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(16, 20);

        var slider = back.gameObject.AddComponent<Slider>();
        slider.fillRect = fr;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    static TMP_Text NewText(Transform parent, string text, float size, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        var t = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = align;
        t.raycastTarget = false;
        Center(t.rectTransform, pos, box);
        return t;
    }

    static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
