using UnityEngine;

// Lugar marcado en el mapa donde el héroe puede ordenar construir un edificio con E.
public class BuildSpot : Interactable
{
    public Building buildingPrefab;
    public Cost cost = new Cost(10, 30, 0);
    [Tooltip("Se oculta este objeto (el cartel) cuando se construye encima.")]
    public GameObject marker;

    public override string GetPrompt()
    {
        if (buildingPrefab == null) return null;
        string name = buildingPrefab.displayName;
        bool afford = ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(cost);
        return afford
            ? $"[E] Construir {name} ({cost})"
            : $"{name}: necesitas {cost}";
    }

    public override void Interact(Player player)
    {
        if (buildingPrefab == null || ResourceManager.Instance == null) return;
        if (!ResourceManager.Instance.Spend(cost)) return;

        var b = Instantiate(buildingPrefab, transform.position, Quaternion.identity);
        b.startCompleted = false;
        gameObject.SetActive(false);
    }
}
