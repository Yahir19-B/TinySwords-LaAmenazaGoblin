using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tarjeta de un objeto en la tienda: muestra nivel/estado y el botón para comprar o equipar.
public class ShopItemCard : MonoBehaviour
{
    public string itemId;
    public TMP_Text levelText;
    public Button buyButton;
    public TMP_Text buyText;
    public GameObject priceIcon;

    ShopItem item;

    void Awake()
    {
        item = Shop.Get(itemId);
        if (buyButton != null) buyButton.onClick.AddListener(OnClick);
    }

    void OnEnable() => Refresh();

    void OnClick()
    {
        if (item == null) return;
        if (item.type == ShopItemType.Skin && Shop.IsOwned(item)) Shop.Equip(item);
        else if (Shop.TryBuy(item)) AudioManager.Play("comprar");
        // Al comprar cambia el oro, así que se actualizan todas las tarjetas.
        foreach (var card in FindObjectsByType<ShopItemCard>()) card.Refresh();
    }

    public void Refresh()
    {
        if (item == null) item = Shop.Get(itemId);
        if (item == null) return;

        int level = Shop.Level(item.id);
        bool showPrice = false;
        string status, button;
        bool canClick;

        if (item.type == ShopItemType.Skin)
        {
            bool owned = Shop.IsOwned(item);
            bool equipped = Shop.IsEquipped(item);
            status = equipped ? "Equipado" : owned ? "Comprado" : "";
            button = equipped ? "Equipado" : owned ? "Equipar" : Shop.NextPrice(item).ToString();
            showPrice = !owned;
            canClick = !equipped && (owned || Progress.TotalGold >= Shop.NextPrice(item));
        }
        else if (Shop.IsMaxed(item))
        {
            status = item.MaxLevel > 1 ? $"Nivel {level}/{item.MaxLevel}" : "Comprado";
            button = "Máximo";
            canClick = false;
        }
        else
        {
            status = item.MaxLevel > 1 ? $"Nivel {level}/{item.MaxLevel}" : "";
            button = Shop.NextPrice(item).ToString();
            showPrice = true;
            canClick = Progress.TotalGold >= Shop.NextPrice(item);
        }

        if (levelText != null) levelText.text = status;
        if (buyText != null) buyText.text = button;
        if (priceIcon != null) priceIcon.SetActive(showPrice);
        if (buyButton != null) buyButton.interactable = canClick;
    }
}
