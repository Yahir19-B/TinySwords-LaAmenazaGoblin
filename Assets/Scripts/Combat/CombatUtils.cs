using System.Collections.Generic;
using UnityEngine;

public static class CombatUtils
{
    static readonly List<Health> hits = new List<Health>();

    public static Team Opposite(Team team) => team == Team.Knights ? Team.Goblins : Team.Knights;

    // Hace daño a los enemigos de "attackerTeam" (y a los objetos neutrales) dentro de un círculo.
    // Devuelve cuántos golpeó.
    public static int DamageArea(Vector2 center, float radius, Team attackerTeam, float damage, float structureMultiplier = 1f, bool critical = false)
    {
        Team target = Opposite(attackerTeam);
        hits.Clear();
        foreach (var h in Health.All)
        {
            // Una explosión neutral daña a todos (incluso a otros barriles: reacción en cadena).
            bool enemy = attackerTeam == Team.Neutral || h.team == target || h.team == Team.Neutral;
            if (!h.IsDead && enemy && h.DistanceFrom(center) <= radius)
                hits.Add(h);
        }
        // Se aplica después de recorrer la lista, porque TakeDamage puede destruir objetos y modificar Health.All.
        foreach (var h in hits)
            h.TakeDamage(h.isStructure ? damage * structureMultiplier : damage, critical);
        return hits.Count;
    }
}
