using UnityEngine;

// Barra de vida pequeña encima de unidades y edificios. Se crea sola con dos rectángulos de color
// y solo se muestra cuando el objeto está dañado.
[RequireComponent(typeof(Health))]
public class WorldHealthBar : MonoBehaviour
{
    public Vector2 offset = new Vector2(0, 0.9f);
    public float width = 0.8f;
    public float height = 0.1f;
    public bool hideWhenFull = true;
    public Color backColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    public Color knightsColor = new Color(0.35f, 0.85f, 0.35f);
    public Color goblinsColor = new Color(0.9f, 0.3f, 0.25f);
    public int sortingOrder = 100;
    [Tooltip("Material sin iluminación para que la barra se vea aunque el nivel esté oscuro.")]
    public Material material;

    static Sprite pixel;

    Health health;
    Transform root, fill;

    void Awake()
    {
        health = GetComponent<Health>();
        if (pixel == null)
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0, 0.5f), 4);

        root = new GameObject("HealthBar").transform;
        root.SetParent(transform, false);
        root.localPosition = offset;

        CreateRect("Back", backColor, sortingOrder, root);
        fill = CreateRect("Fill", health.team == Team.Knights ? knightsColor : goblinsColor, sortingOrder + 1, root);

        health.OnChanged += Refresh;
        root.gameObject.SetActive(false);
    }

    // En Start la vida ya está inicializada (en Awake podía valer 0 y la barra se veía vacía).
    void Start() => Refresh();

    Transform CreateRect(string name, Color color, int order, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(-width / 2f, 0, 0);
        go.transform.localScale = new Vector3(width, height, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.color = color;
        sr.sortingOrder = order;
        if (material != null) sr.sharedMaterial = material;
        return go.transform;
    }

    void Refresh()
    {
        if (root == null) return;
        bool show = !health.IsDead && !(hideWhenFull && health.IsFull);
        root.gameObject.SetActive(show);
        fill.localScale = new Vector3(width * health.Normalized, height, 1);
    }
}
