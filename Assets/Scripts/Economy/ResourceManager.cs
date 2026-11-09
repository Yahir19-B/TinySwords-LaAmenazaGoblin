using System;
using UnityEngine;

// Guarda cuánto oro, madera y carne tiene el jugador.
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public int startingGold = 50;
    public int startingWood = 50;
    public int startingMeat = 20;

    public int Gold { get; private set; }
    public int Wood { get; private set; }
    public int Meat { get; private set; }

    public event Action OnChanged;

    void Awake()
    {
        Instance = this;
        Gold = startingGold;
        Wood = startingWood;
        Meat = startingMeat;
    }

    public int Get(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return Gold;
            case ResourceType.Wood: return Wood;
            default: return Meat;
        }
    }

    public void Add(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: Gold += amount; break;
            case ResourceType.Wood: Wood += amount; break;
            case ResourceType.Meat: Meat += amount; break;
        }
        OnChanged?.Invoke();
    }

    public bool CanAfford(Cost cost)
    {
        return Gold >= cost.gold && Wood >= cost.wood && Meat >= cost.meat;
    }

    public bool Spend(Cost cost)
    {
        if (!CanAfford(cost)) return false;
        Gold -= cost.gold;
        Wood -= cost.wood;
        Meat -= cost.meat;
        OnChanged?.Invoke();
        return true;
    }
}
