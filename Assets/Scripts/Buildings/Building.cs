using System.Collections.Generic;
using UnityEngine;

public enum BuildingState
{
    Construction,
    Complete,
    Destroyed
}

// Edificio de los Knights (castillo, casa, torre). Tiene tres estados con su sprite:
// en construcción -> completo -> destruido. Los pawns libres lo construyen y reparan.
// Con E el héroe puede: llamar a un constructor, reparar (madera) o reconstruir.
[RequireComponent(typeof(Health))]
public class Building : Interactable
{
    public static readonly List<Building> All = new List<Building>();

    public string displayName = "Edificio";
    [Tooltip("Segundos de trabajo de un pawn para terminarlo.")]
    public float buildTime = 12f;
    public bool startCompleted = true;
    [Tooltip("Cuánta población extra da (las casas).")]
    public int populationBonus;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite constructionSprite;
    public Sprite completeSprite;
    public Sprite destroyedSprite;

    [Header("Se activan solo cuando está completo (ej. Tower)")]
    public Behaviour[] enableWhenComplete;

    [Header("Reparación")]
    public Cost repairCost = new Cost(0, 10, 0);
    [Range(0f, 1f)] public float repairAmount = 0.3f;
    public Cost rebuildCost = new Cost(10, 20, 0);

    [Header("Efectos")]
    public GameObject fireEffectPrefab;
    public Vector2[] fireOffsets = { new Vector2(-0.4f, 0.6f), new Vector2(0.4f, 1f) };
    [Range(0f, 1f)] public float fireBelowHealth = 0.4f;
    public GameObject destroyEffectPrefab;

    public BuildingState State { get; private set; }
    public float Progress { get; private set; }
    public Health Health { get; private set; }
    public float Radius => Health != null ? Health.radius : 0.5f;

    // Necesita que un pawn libre venga a trabajar en él.
    public bool NeedsWork => State == BuildingState.Construction;

    readonly List<GameObject> fires = new List<GameObject>();

    void Awake()
    {
        Health = GetComponent<Health>();
        Health.team = Team.Knights;
        Health.isStructure = true;
        Health.destroyOnDeath = false;
        Health.OnDied += HandleDestroyed;
        Health.OnChanged += UpdateFires;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        All.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        All.Remove(this);
    }

    void Start()
    {
        if (startCompleted) Complete();
        else BeginConstruction();
    }

    public void BeginConstruction()
    {
        State = BuildingState.Construction;
        Progress = 0;
        Health.Revive(0.1f);
        SetSprite(constructionSprite);
        SetComponents(false);
        UpdateFires();
    }

    public void AddWork(float workSeconds)
    {
        if (State != BuildingState.Construction) return;
        Progress = Mathf.Clamp01(Progress + workSeconds / Mathf.Max(0.1f, buildTime));
        Health.SetFraction(Mathf.Max(Health.Normalized, 0.1f + 0.9f * Progress));
        if (Progress >= 1f) Complete();
    }

    void Complete()
    {
        State = BuildingState.Complete;
        Progress = 1;
        Health.Revive(1f);
        SetSprite(completeSprite);
        SetComponents(true);
        UpdateFires();
    }

    void HandleDestroyed()
    {
        State = BuildingState.Destroyed;
        SetSprite(destroyedSprite);
        SetComponents(false);
        if (destroyEffectPrefab != null)
            Instantiate(destroyEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        UpdateFires();
    }

    void SetSprite(Sprite s)
    {
        if (spriteRenderer != null && s != null) spriteRenderer.sprite = s;
    }

    void SetComponents(bool on)
    {
        if (enableWhenComplete == null) return;
        foreach (var c in enableWhenComplete) if (c != null) c.enabled = on;
    }

    // Enciende fuego cuando el edificio está muy dañado.
    void UpdateFires()
    {
        bool burning = State != BuildingState.Construction && !Health.IsDead && Health.Normalized <= fireBelowHealth;
        if (burning && fires.Count == 0 && fireEffectPrefab != null)
        {
            foreach (var off in fireOffsets)
                fires.Add(Instantiate(fireEffectPrefab, transform.position + (Vector3)off, Quaternion.identity, transform));
        }
        else if (!burning && fires.Count > 0)
        {
            foreach (var f in fires) if (f != null) Destroy(f);
            fires.Clear();
        }
    }

    public override string GetPrompt()
    {
        switch (State)
        {
            case BuildingState.Construction:
                int builders = PawnManager.Instance != null ? PawnManager.Instance.CountJob(PawnJob.Idle) : 0;
                return builders > 0
                    ? $"{displayName}: construyendo {Mathf.RoundToInt(Progress * 100)}%"
                    : $"[E] Llamar un pawn para construir {displayName}";
            case BuildingState.Complete:
                if (Health.IsFull) return null;
                return $"[E] Reparar {displayName} ({repairCost})";
            default:
                return $"[E] Reconstruir {displayName} ({rebuildCost})";
        }
    }

    public override void Interact(Player player)
    {
        var rm = ResourceManager.Instance;
        switch (State)
        {
            case BuildingState.Construction:
                if (PawnManager.Instance != null && PawnManager.Instance.CountJob(PawnJob.Idle) == 0)
                    PawnManager.Instance.RemoveFromJob(BusiestJob());
                break;
            case BuildingState.Complete:
                if (!Health.IsFull && rm != null && rm.Spend(repairCost))
                    Health.Heal(Health.maxHealth * repairAmount);
                break;
            case BuildingState.Destroyed:
                if (rm != null && rm.Spend(rebuildCost))
                    BeginConstruction();
                break;
        }
    }

    static PawnJob BusiestJob()
    {
        var pm = PawnManager.Instance;
        PawnJob best = PawnJob.Wood;
        int most = -1;
        foreach (var j in new[] { PawnJob.Wood, PawnJob.Gold, PawnJob.Meat })
        {
            int c = pm.CountJob(j);
            if (c > most) { most = c; best = j; }
        }
        return best;
    }

    public static Building FindNeedingWork(Vector2 from)
    {
        Building best = null;
        float bestDist = float.MaxValue;
        foreach (var b in All)
        {
            if (!b.NeedsWork) continue;
            float d = Vector2.Distance(from, b.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        return best;
    }
}
