using TMPro;
using UnityEngine;

// Número de daño que sube y se desvanece (blanco = daño normal, amarillo = crítico, rojo = daño al héroe).
public class DamagePopup : MonoBehaviour
{
    TextMeshPro text;
    float life;
    Color color;
    Vector3 velocity;

    public static void Show(Vector3 position, float amount, bool critical, bool isPlayer)
    {
        string label = critical ? $"¡{Mathf.RoundToInt(amount)}!" : Mathf.RoundToInt(amount).ToString();
        Color c = isPlayer ? new Color(1f, 0.35f, 0.3f) : critical ? new Color(1f, 0.85f, 0.2f) : Color.white;
        ShowText(position, label, c, critical ? 7f : 5f);
    }

    public static void ShowText(Vector3 position, string label, Color color, float size = 5f)
    {
        var go = new GameObject("Popup");
        go.transform.position = position + (Vector3)(Random.insideUnitCircle * 0.2f);
        var popup = go.AddComponent<DamagePopup>();
        popup.text = go.AddComponent<TextMeshPro>();
        popup.text.text = label;
        popup.text.fontSize = size;
        popup.text.fontStyle = FontStyles.Bold;
        popup.text.alignment = TextAlignmentOptions.Center;
        popup.text.color = color;
        popup.text.outlineWidth = 0.25f;
        popup.text.outlineColor = new Color32(20, 10, 10, 255);
        popup.text.sortingOrder = 300;
        popup.text.rectTransform.sizeDelta = new Vector2(6, 2);
        popup.color = color;
        popup.life = 0.8f;
        popup.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 1.8f, 0);
    }

    void Update()
    {
        life -= Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity *= 0.92f;
        color.a = Mathf.Clamp01(life / 0.4f);
        text.color = color;
        if (life <= 0) Destroy(gameObject);
    }
}
