using UnityEngine;

// Progreso guardado entre partidas: niveles desbloqueados, oro y compras de la tienda.
public static class Progress
{
    public const int LevelCount = 10;

    const string UnlockedKey = "UnlockedLevel";
    const string GoldKey = "TotalGold";

    // Cuando se vuelve al menú desde un nivel, se abre directo la pantalla de niveles.
    public static bool OpenLevelSelect;

    public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, LevelCount);
    public static int TotalGold => PlayerPrefs.GetInt(GoldKey, 0);

    public static bool IsUnlocked(int level) => level <= Unlocked;
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt("Completed_" + level, 0) == 1;

    public static void CompleteLevel(int level, int goldEarned)
    {
        PlayerPrefs.SetInt("Completed_" + level, 1);
        if (level + 1 > Unlocked) PlayerPrefs.SetInt(UnlockedKey, Mathf.Min(level + 1, LevelCount));
        PlayerPrefs.SetInt(GoldKey, TotalGold + goldEarned);
        PlayerPrefs.Save();
    }

    public static bool SpendGold(int amount)
    {
        if (amount < 0 || TotalGold < amount) return false;
        PlayerPrefs.SetInt(GoldKey, TotalGold - amount);
        PlayerPrefs.Save();
        return true;
    }

    // Objetos de la tienda: nivel comprado (0 = no comprado).
    public static int GetItemLevel(string id) => PlayerPrefs.GetInt("Item_" + id, 0);

    public static void SetItemLevel(string id, int level)
    {
        PlayerPrefs.SetInt("Item_" + id, level);
        PlayerPrefs.Save();
    }

    public static string EquippedSkin
    {
        get => PlayerPrefs.GetString("Skin", "Blue");
        set
        {
            PlayerPrefs.SetString("Skin", value);
            PlayerPrefs.Save();
        }
    }

    public static void Reset()
    {
        foreach (var item in Shop.Items) PlayerPrefs.DeleteKey("Item_" + item.id);
        PlayerPrefs.DeleteKey("Skin");
        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.DeleteKey(GoldKey);
        for (int i = 1; i <= LevelCount; i++) PlayerPrefs.DeleteKey("Completed_" + i);
        PlayerPrefs.Save();
    }

    public static string SceneName(int level) => $"Nivel_{level:00}";
}
