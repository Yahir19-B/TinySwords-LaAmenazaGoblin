using System.Collections.Generic;
using UnityEngine;

// Lugar donde los pawns entregan lo que recolectan (el castillo, y las casas si quieres).
public class DropOffPoint : MonoBehaviour
{
    public static readonly List<DropOffPoint> All = new List<DropOffPoint>();

    [Tooltip("Distancia desde el borde del edificio a la que el pawn puede entregar.")]
    public float reachDistance = 0.8f;

    Health health;

    void Awake() => health = GetComponent<Health>();

    // Distancia desde el centro a la que el pawn debe detenerse.
    public float StopDistance => reachDistance + (health != null ? health.radius : 0f);

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Usable
    {
        get
        {
            if (health != null && health.IsDead) return false;
            var b = GetComponent<Building>();
            return b == null || b.State == BuildingState.Complete;
        }
    }

    public static DropOffPoint FindClosest(Vector2 from)
    {
        DropOffPoint best = null;
        float bestDist = float.MaxValue;
        foreach (var p in All)
        {
            if (!p.Usable) continue;
            float d = Vector2.Distance(from, p.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }
}
