using System.Collections.Generic;
using UnityEngine;

// Fuente de recursos que los pawns explotan: mina de oro, árbol u oveja.
public class ResourceNode : MonoBehaviour
{
    public static readonly List<ResourceNode> All = new List<ResourceNode>();

    public ResourceType type = ResourceType.Wood;
    public int amount = 60;
    public int amountPerTrip = 5;
    [Tooltip("Segundos que tarda un pawn en recolectar un viaje.")]
    public float gatherTime = 3f;
    [Tooltip("Distancia a la que el pawn se detiene para trabajar.")]
    public float workDistance = 0.8f;
    [Tooltip("El pawn se esconde dentro mientras trabaja (como en las minas de oro).")]
    public bool hideWorkerInside;

    [Header("Al agotarse")]
    [Tooltip("0 = no vuelve a crecer.")]
    public float regrowTime = 0f;
    public bool destroyWhenDepleted;

    [Header("Visual (opcional)")]
    public SpriteRenderer spriteRenderer;
    public Sprite normalSprite;
    public Sprite workingSprite;
    public Sprite depletedSprite;
    public UnitAnimator unitAnimator;   // árbol: "Idle" / "Chop" / "Stump"; oveja: "Idle"

    public bool Depleted => remaining <= 0;
    public int Remaining => remaining;

    int remaining;
    int workers;
    float regrowTimer;

    void Awake()
    {
        remaining = amount;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (unitAnimator == null) unitAnimator = GetComponent<UnitAnimator>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start() => RefreshVisual();

    void Update()
    {
        if (Depleted && regrowTime > 0)
        {
            regrowTimer -= Time.deltaTime;
            if (regrowTimer <= 0)
            {
                remaining = amount;
                RefreshVisual();
            }
        }
    }

    public void StartWorking()
    {
        workers++;
        RefreshVisual();
    }

    public void StopWorking()
    {
        workers = Mathf.Max(0, workers - 1);
        RefreshVisual();
    }

    // El pawn pide recursos; devuelve cuánto pudo sacar.
    public int Harvest(int requested)
    {
        int taken = Mathf.Min(requested, remaining);
        remaining -= taken;
        if (Depleted) OnDepleted();
        return taken;
    }

    void OnDepleted()
    {
        workers = 0;
        regrowTimer = regrowTime;
        if (destroyWhenDepleted)
        {
            Destroy(gameObject);
            return;
        }
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (unitAnimator != null)
            unitAnimator.Play(Depleted ? "Stump" : workers > 0 ? "Chop" : "Idle");

        if (spriteRenderer == null) return;
        if (Depleted && depletedSprite != null) spriteRenderer.sprite = depletedSprite;
        else if (!Depleted && workers > 0 && workingSprite != null) spriteRenderer.sprite = workingSprite;
        else if (!Depleted && normalSprite != null) spriteRenderer.sprite = normalSprite;
    }

    public static ResourceNode FindClosest(Vector2 from, ResourceType type)
    {
        ResourceNode best = null;
        float bestDist = float.MaxValue;
        foreach (var n in All)
        {
            if (n.Depleted || n.type != type) continue;
            float d = Vector2.Distance(from, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }
        return best;
    }
}
