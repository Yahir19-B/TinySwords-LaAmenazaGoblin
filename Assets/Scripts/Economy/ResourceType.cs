using System;

public enum ResourceType
{
    Gold,
    Wood,
    Meat
}

// Precio de algo (edificio, pawn, reparación).
[Serializable]
public struct Cost
{
    public int gold;
    public int wood;
    public int meat;

    public Cost(int gold, int wood, int meat)
    {
        this.gold = gold;
        this.wood = wood;
        this.meat = meat;
    }

    public bool IsFree => gold <= 0 && wood <= 0 && meat <= 0;

    public override string ToString()
    {
        if (IsFree) return "gratis";
        var parts = new System.Collections.Generic.List<string>();
        if (gold > 0) parts.Add(gold + " oro");
        if (wood > 0) parts.Add(wood + " madera");
        if (meat > 0) parts.Add(meat + " carne");
        return string.Join(", ", parts);
    }
}
