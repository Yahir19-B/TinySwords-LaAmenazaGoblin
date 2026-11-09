public enum ShopItemType
{
    Upgrade,    // mejora con varios niveles
    Power,      // poder con tecla propia
    Skin        // color del guerrero
}

public class ShopItem
{
    public readonly string id;
    public readonly string name;
    public readonly string description;
    public readonly ShopItemType type;
    public readonly int[] prices;       // precio de cada nivel
    public readonly string skinColor;   // solo skins: Blue, Red, Purple, Yellow

    public ShopItem(string id, string name, string description, ShopItemType type, int[] prices, string skinColor = null)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.type = type;
        this.prices = prices;
        this.skinColor = skinColor;
    }

    public int MaxLevel => prices.Length;
}

// Catálogo de la tienda. Todo se compra con el oro que se recoge en los niveles.
public static class Shop
{
    public const string Sword = "espada";
    public const string Armor = "armadura";
    public const string Boots = "botas";
    public const string Whirlwind = "torbellino";
    public const string Potion = "pocion";

    public static readonly ShopItem[] Items =
    {
        new ShopItem(Sword, "Espada afilada", "+25% de daño por nivel", ShopItemType.Upgrade, new[] { 60, 130, 240 }),
        new ShopItem(Armor, "Armadura", "+40 de vida máxima por nivel", ShopItemType.Upgrade, new[] { 50, 110, 200 }),
        new ShopItem(Boots, "Botas veloces", "+12% de velocidad por nivel", ShopItemType.Upgrade, new[] { 40, 100 }),
        new ShopItem(Whirlwind, "Torbellino  [Q]", "Giras y dañas a todos los goblins cerca", ShopItemType.Power, new[] { 150 }),
        new ShopItem(Potion, "Poción de vida  [R]", "Recupera la mitad de tu vida", ShopItemType.Power, new[] { 100 }),
        new ShopItem("skin_blue", "Guerrero Azul", "El clásico caballero del reino", ShopItemType.Skin, new[] { 0 }, "Blue"),
        new ShopItem("skin_red", "Guerrero Rojo", "Armadura teñida de sangre goblin", ShopItemType.Skin, new[] { 80 }, "Red"),
        new ShopItem("skin_purple", "Guerrero Morado", "El color de la noche maldita", ShopItemType.Skin, new[] { 120 }, "Purple"),
        new ShopItem("skin_yellow", "Guerrero Dorado", "Para héroes de leyenda", ShopItemType.Skin, new[] { 180 }, "Yellow"),
    };

    public static ShopItem Get(string id)
    {
        foreach (var item in Items) if (item.id == id) return item;
        return null;
    }

    public static int Level(string id) => Progress.GetItemLevel(id);

    public static bool IsOwned(ShopItem item) => item.skinColor == "Blue" || Level(item.id) > 0;
    public static bool IsMaxed(ShopItem item) => Level(item.id) >= item.MaxLevel;
    public static int NextPrice(ShopItem item) => IsMaxed(item) ? 0 : item.prices[Level(item.id)];
    public static bool IsEquipped(ShopItem item) => item.type == ShopItemType.Skin && Progress.EquippedSkin == item.skinColor;

    // Compra el siguiente nivel (o la skin). Las skins se equipan al comprarlas.
    public static bool TryBuy(ShopItem item)
    {
        if (IsMaxed(item) || !Progress.SpendGold(NextPrice(item))) return false;
        Progress.SetItemLevel(item.id, Level(item.id) + 1);
        if (item.type == ShopItemType.Skin) Progress.EquippedSkin = item.skinColor;
        return true;
    }

    public static void Equip(ShopItem item)
    {
        if (item.type == ShopItemType.Skin && IsOwned(item)) Progress.EquippedSkin = item.skinColor;
    }
}
