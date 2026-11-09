using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aplica al héroe lo comprado en la tienda: mejoras (daño, vida, velocidad), skin y poderes.
// Q: Torbellino   R: Poción de vida   (también muestra la recarga de esquivar)
[RequireComponent(typeof(Player))]
public class PlayerLoadout : MonoBehaviour
{
    [Header("Skins (Animator Controllers del guerrero)")]
    public RuntimeAnimatorController blueSkin;
    public RuntimeAnimatorController redSkin;
    public RuntimeAnimatorController purpleSkin;
    public RuntimeAnimatorController yellowSkin;

    [Header("Torbellino (Q)")]
    public KeyCode whirlwindKey = KeyCode.Q;
    public float whirlwindCooldown = 6f;
    public float whirlwindRadius = 2.2f;
    public float whirlwindDamageMultiplier = 2f;
    public GameObject whirlwindEffect;
    public Sprite whirlwindIcon;

    [Header("Poción (R)")]
    public KeyCode potionKey = KeyCode.R;
    public float potionCooldown = 25f;
    [Range(0f, 1f)] public float potionHeal = 0.5f;
    public Sprite potionIcon;

    [Header("Botones de poderes en pantalla")]
    public Sprite slotSprite;
    public Sprite dashIcon;

    Player player;
    UnitAnimator unitAnim;
    bool hasWhirlwind, hasPotion;
    float whirlwindTimer, potionTimer;
    Slot whirlwindSlot, potionSlot, dashSlot;

    class Slot
    {
        public Image icon;
        public TMP_Text cooldown;
    }

    void Start()
    {
        player = GetComponent<Player>();
        unitAnim = GetComponent<UnitAnimator>();

        int sword = Shop.Level(Shop.Sword);
        int armor = Shop.Level(Shop.Armor);
        int boots = Shop.Level(Shop.Boots);
        player.attackDamage *= 1f + 0.25f * sword;
        player.speed *= 1f + 0.12f * boots;
        player.Health.maxHealth += 40f * armor;
        player.Health.Revive(1f);

        var skin = SkinFor(Progress.EquippedSkin);
        var animator = GetComponent<Animator>();
        if (skin != null && animator != null) animator.runtimeAnimatorController = skin;

        StartCoroutine(UpdatePortrait());

        hasWhirlwind = Shop.Level(Shop.Whirlwind) > 0;
        hasPotion = Shop.Level(Shop.Potion) > 0;
        BuildSlots();
    }

    // El retrato del HUD toma el sprite del héroe (así muestra el color de la skin).
    System.Collections.IEnumerator UpdatePortrait()
    {
        yield return null; // espera un frame para que el Animator ponga el sprite de la skin
        var hud = LevelHUD.Instance;
        var portrait = hud != null ? hud.transform.Find("Heroe/Retrato/Warrior") : null;
        var sr = GetComponent<SpriteRenderer>();
        if (portrait != null && sr != null && portrait.TryGetComponent<Image>(out var img)) img.sprite = sr.sprite;
    }

    RuntimeAnimatorController SkinFor(string color)
    {
        switch (color)
        {
            case "Red": return redSkin;
            case "Purple": return purpleSkin;
            case "Yellow": return yellowSkin;
            default: return blueSkin;
        }
    }

    void Update()
    {
        whirlwindTimer -= Time.deltaTime;
        potionTimer -= Time.deltaTime;
        UpdateSlot(whirlwindSlot, whirlwindTimer);
        UpdateSlot(potionSlot, potionTimer);
        UpdateSlot(dashSlot, player.DashCooldownLeft);

        if (player.IsDead || Time.timeScale == 0f) return;
        if (hasWhirlwind && whirlwindTimer <= 0 && Input.GetKeyDown(whirlwindKey)) Whirlwind();
        if (hasPotion && potionTimer <= 0 && Input.GetKeyDown(potionKey)) DrinkPotion();
    }

    void Whirlwind()
    {
        whirlwindTimer = whirlwindCooldown;
        unitAnim?.Play("AttackDown", true);
        CombatUtils.DamageArea(transform.position, whirlwindRadius, Team.Knights, player.attackDamage * whirlwindDamageMultiplier, 0.5f);
        if (whirlwindEffect != null)
        {
            var fx = Instantiate(whirlwindEffect, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            fx.transform.localScale = Vector3.one * 1.3f;
        }
        CameraFollow.Shake(0.15f, 0.25f);
    }

    void DrinkPotion()
    {
        if (player.Health.IsFull) return;
        potionTimer = potionCooldown;
        player.Health.Heal(player.Health.maxHealth * potionHeal);
        LevelHUD.Instance?.ShowMessage("¡Poción! +vida", 1.5f);
    }

    // ---------- Botones de poderes (abajo a la izquierda del HUD) ----------

    void BuildSlots()
    {
        var hud = LevelHUD.Instance;
        if (hud == null) return;
        int index = 0;
        dashSlot = CreateSlot(hud.transform, dashIcon, "Shift", index++);
        if (hasWhirlwind) whirlwindSlot = CreateSlot(hud.transform, whirlwindIcon, whirlwindKey.ToString(), index++);
        if (hasPotion) potionSlot = CreateSlot(hud.transform, potionIcon, potionKey.ToString(), index);
    }

    Slot CreateSlot(Transform parent, Sprite icon, string key, int index)
    {
        var frame = new GameObject("Poder_" + key, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        frame.transform.SetParent(parent, false);
        frame.sprite = slotSprite;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 2.5f;
        var rt = frame.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(28 + index * 130, 70);
        rt.sizeDelta = new Vector2(115, 115);

        var slot = new Slot();
        slot.icon = new GameObject("Icono", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        slot.icon.transform.SetParent(frame.transform, false);
        slot.icon.sprite = icon;
        slot.icon.preserveAspect = true;
        Stretch(slot.icon.rectTransform, 16);

        var keyText = NewText(frame.transform, key, key.Length > 1 ? 20 : 26);
        var kr = keyText.rectTransform;
        kr.anchorMin = kr.anchorMax = kr.pivot = new Vector2(1, 1);
        kr.anchoredPosition = new Vector2(-8, -4);
        kr.sizeDelta = new Vector2(70, 34);
        keyText.alignment = TextAlignmentOptions.Right;

        slot.cooldown = NewText(frame.transform, "", 40);
        Stretch(slot.cooldown.rectTransform, 0);
        return slot;
    }

    static TMP_Text NewText(Transform parent, string text, float size)
    {
        var t = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    static void UpdateSlot(Slot slot, float timer)
    {
        if (slot == null) return;
        bool ready = timer <= 0;
        slot.icon.color = ready ? Color.white : new Color(0.35f, 0.35f, 0.35f);
        string text = ready ? "" : Mathf.CeilToInt(timer).ToString();
        if (slot.cooldown.text != text) slot.cooldown.text = text;
    }
}
