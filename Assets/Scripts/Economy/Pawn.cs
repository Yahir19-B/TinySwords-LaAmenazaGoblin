using System.Collections.Generic;
using UnityEngine;

// Trabajo asignado a un pawn. "Idle" = libre: construye y repara edificios si hay obras pendientes.
public enum PawnJob
{
    Idle,
    Gold,
    Wood,
    Meat
}

// Trabajador de los Knights: va al recurso, lo recolecta, lo lleva al castillo y repite.
// Si está libre, ayuda a construir.
public class Pawn : MonoBehaviour
{
    public static readonly List<Pawn> All = new List<Pawn>();

    public PawnJob job = PawnJob.Idle;
    public float speed = 2.5f;
    [Tooltip("Segundos de obra que aporta por segundo.")]
    public float buildRate = 1f;
    public float buildDistance = 1.2f;

    [Header("Icono del recurso que carga (opcional)")]
    public SpriteRenderer carryIcon;
    public Sprite goldIcon;
    public Sprite woodIcon;
    public Sprite meatIcon;

    enum State { Idle, ToNode, Gathering, ToDropOff, ToBuild, Building }

    State state;
    ResourceNode node;
    Building buildTarget;
    int carrying;
    ResourceType carryType;
    float timer;
    Vector2 idleSpot;

    UnitAnimator unitAnim;
    Rigidbody2D rb;
    Vector2 desiredVelocity;
    bool movedThisFrame;

    public string StatusText
    {
        get
        {
            switch (state)
            {
                case State.Gathering: return "recolectando";
                case State.ToDropOff: return "entregando";
                case State.Building: return "construyendo";
                case State.ToBuild: return "yendo a construir";
                case State.ToNode: return "yendo al recurso";
                default: return "libre";
            }
        }
    }

    void Awake()
    {
        unitAnim = GetComponent<UnitAnimator>();
        rb = GetComponent<Rigidbody2D>();
        idleSpot = transform.position;
    }

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        All.Remove(this);
        ReleaseNode();
    }

    public void SetJob(PawnJob newJob)
    {
        if (newJob == job) return;
        job = newJob;
        ReleaseNode();
        buildTarget = null;
        state = State.Idle;
        unitAnim?.SetVisible(true);
    }

    void Update()
    {
        desiredVelocity = Vector2.zero;
        movedThisFrame = false;

        switch (state)
        {
            case State.Idle: Think(); break;
            case State.ToNode: UpdateToNode(); break;
            case State.Gathering: UpdateGathering(); break;
            case State.ToDropOff: UpdateToDropOff(); break;
            case State.ToBuild: UpdateToBuild(); break;
            case State.Building: UpdateBuilding(); break;
        }

        UpdateAnimation();
        UpdateCarryIcon();
    }

    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = desiredVelocity;
    }

    void Think()
    {
        if (carrying > 0)
        {
            state = State.ToDropOff;
            return;
        }

        if (job == PawnJob.Idle)
        {
            buildTarget = Building.FindNeedingWork(transform.position);
            if (buildTarget != null)
            {
                state = State.ToBuild;
                return;
            }
            // Nada que hacer: se queda cerca de donde estaba.
            MoveTo(idleSpot, 0.3f);
            return;
        }

        node = ResourceNode.FindClosest(transform.position, JobToResource(job));
        if (node != null) state = State.ToNode;
    }

    void UpdateToNode()
    {
        if (node == null || node.Depleted)
        {
            node = null;
            state = State.Idle;
            return;
        }
        if (MoveTo(node.transform.position, node.workDistance))
        {
            state = State.Gathering;
            timer = node.gatherTime;
            node.StartWorking();
            if (node.hideWorkerInside) unitAnim?.SetVisible(false);
        }
    }

    void UpdateGathering()
    {
        if (node == null || node.Depleted)
        {
            ReleaseNode();
            state = State.Idle;
            return;
        }
        timer -= Time.deltaTime;
        if (timer > 0) return;

        carryType = node.type;
        carrying = node.Harvest(node.amountPerTrip);
        ReleaseNode();
        state = carrying > 0 ? State.ToDropOff : State.Idle;
    }

    void UpdateToDropOff()
    {
        var drop = DropOffPoint.FindClosest(transform.position);
        if (drop == null) return; // sin castillo no hay dónde entregar

        if (MoveTo(drop.transform.position, drop.StopDistance))
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.Add(carryType, carrying);
            carrying = 0;
            state = State.Idle;
        }
    }

    void UpdateToBuild()
    {
        if (job != PawnJob.Idle || buildTarget == null || !buildTarget.NeedsWork)
        {
            buildTarget = null;
            state = State.Idle;
            return;
        }
        if (MoveTo(buildTarget.transform.position, buildDistance + buildTarget.Radius))
            state = State.Building;
    }

    void UpdateBuilding()
    {
        if (job != PawnJob.Idle || buildTarget == null || !buildTarget.NeedsWork)
        {
            buildTarget = null;
            state = State.Idle;
            return;
        }
        unitAnim?.Face(buildTarget.transform.position.x - transform.position.x);
        buildTarget.AddWork(buildRate * Time.deltaTime);
    }

    // Camina hacia un punto. Devuelve true cuando ya llegó.
    bool MoveTo(Vector2 target, float stopDistance)
    {
        Vector2 pos = transform.position;
        Vector2 delta = target - pos;
        if (delta.magnitude <= stopDistance) return true;

        Vector2 vel = delta.normalized * speed;
        movedThisFrame = true;
        if (rb != null) desiredVelocity = vel;
        else transform.position = pos + vel * Time.deltaTime;
        unitAnim?.Face(vel.x);
        return false;
    }

    void ReleaseNode()
    {
        if (state == State.Gathering && node != null) node.StopWorking();
        if (state == State.Gathering) unitAnim?.SetVisible(true);
        node = null;
    }

    void UpdateAnimation()
    {
        if (unitAnim == null) return;
        bool moving = movedThisFrame;

        if (state == State.Building) unitAnim.Play("Build");
        else if (state == State.Gathering) unitAnim.Play(node != null && node.type == ResourceType.Gold ? "Build" : "Chop");
        else if (carrying > 0) unitAnim.Play(moving ? "RunCarry" : "IdleCarry");
        else unitAnim.Play(moving ? "Run" : "Idle");
    }

    void UpdateCarryIcon()
    {
        if (carryIcon == null) return;
        carryIcon.enabled = carrying > 0;
        if (carrying > 0)
            carryIcon.sprite = carryType == ResourceType.Gold ? goldIcon : carryType == ResourceType.Wood ? woodIcon : meatIcon;
    }

    public static ResourceType JobToResource(PawnJob job)
    {
        switch (job)
        {
            case PawnJob.Gold: return ResourceType.Gold;
            case PawnJob.Meat: return ResourceType.Meat;
            default: return ResourceType.Wood;
        }
    }
}
