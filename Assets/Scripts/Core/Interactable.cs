using System.Collections.Generic;
using UnityEngine;

// Base para todo lo que el héroe puede usar con la tecla E:
// lugares para construir, edificios que reparar, etc.
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    public float interactRange = 1.5f;

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    // Texto que se muestra en pantalla. Devuelve null si ahora mismo no se puede usar.
    public abstract string GetPrompt();

    public abstract void Interact(Player player);

    public static Interactable FindClosest(Vector2 from)
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        foreach (var i in All)
        {
            float d = Vector2.Distance(from, i.transform.position);
            if (d <= i.interactRange && d < bestDist && i.GetPrompt() != null)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }
}
