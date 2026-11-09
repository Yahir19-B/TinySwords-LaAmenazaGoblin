using UnityEngine;

// Entrena pawns en el castillo y reparte los trabajos (oro, madera, carne, libres).
// La población máxima sube al construir casas.
public class PawnManager : MonoBehaviour
{
    public static PawnManager Instance { get; private set; }

    public Pawn pawnPrefab;
    public Cost trainCost = new Cost(10, 0, 10);
    public float trainTime = 4f;
    public int basePopulation = 3;
    public int startingPawns = 2;
    public PawnJob[] startingJobs = { PawnJob.Wood, PawnJob.Gold };

    public int Queued { get; private set; }
    public float TrainProgress => Queued > 0 ? 1f - trainTimer / trainTime : 0f;

    float trainTimer;

    void Awake() => Instance = this;

    void Start()
    {
        for (int i = 0; i < startingPawns; i++)
        {
            var job = startingJobs != null && i < startingJobs.Length ? startingJobs[i] : PawnJob.Idle;
            SpawnPawn(job);
        }
    }

    public int Capacity
    {
        get
        {
            int cap = basePopulation;
            foreach (var b in Building.All)
                if (b.State == BuildingState.Complete) cap += b.populationBonus;
            return cap;
        }
    }

    public int Population => Pawn.All.Count + Queued;

    public bool CanTrain => Population < Capacity
        && ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(trainCost)
        && Castle.Instance != null && !Castle.Instance.Health.IsDead;

    public bool TryTrain()
    {
        if (!CanTrain) return false;
        ResourceManager.Instance.Spend(trainCost);
        if (Queued == 0) trainTimer = trainTime;
        Queued++;
        return true;
    }

    void Update()
    {
        if (Queued == 0) return;
        trainTimer -= Time.deltaTime;
        if (trainTimer <= 0)
        {
            Queued--;
            SpawnPawn(LeastStaffedResourceJob());
            trainTimer = trainTime;
        }
    }

    Pawn SpawnPawn(PawnJob job)
    {
        if (pawnPrefab == null) return null;
        Vector2 pos = Castle.Instance != null ? Castle.Instance.SpawnPosition : (Vector2)transform.position;
        pos += Random.insideUnitCircle * 0.5f;
        var pawn = Instantiate(pawnPrefab, pos, Quaternion.identity);
        pawn.job = job;
        return pawn;
    }

    public int CountJob(PawnJob job)
    {
        int n = 0;
        foreach (var p in Pawn.All) if (p.job == job) n++;
        return n;
    }

    // Pone un pawn más en ese trabajo: primero toma uno libre y, si no hay, al trabajo que tenga más gente.
    public bool AddToJob(PawnJob job)
    {
        Pawn candidate = FindWithJob(PawnJob.Idle);
        if (candidate == null)
        {
            PawnJob busiest = PawnJob.Idle;
            int most = 0;
            foreach (PawnJob j in System.Enum.GetValues(typeof(PawnJob)))
            {
                if (j == job || j == PawnJob.Idle) continue;
                int c = CountJob(j);
                if (c > most) { most = c; busiest = j; }
            }
            if (most > 0) candidate = FindWithJob(busiest);
        }
        if (candidate == null) return false;
        candidate.SetJob(job);
        return true;
    }

    // Quita un pawn de ese trabajo y lo deja libre (constructor).
    public bool RemoveFromJob(PawnJob job)
    {
        if (job == PawnJob.Idle) return false;
        var p = FindWithJob(job);
        if (p == null) return false;
        p.SetJob(PawnJob.Idle);
        return true;
    }

    Pawn FindWithJob(PawnJob job)
    {
        foreach (var p in Pawn.All)
            if (p.job == job) return p;
        return null;
    }

    PawnJob LeastStaffedResourceJob()
    {
        PawnJob best = PawnJob.Wood;
        int fewest = int.MaxValue;
        foreach (var j in new[] { PawnJob.Wood, PawnJob.Gold, PawnJob.Meat })
        {
            int c = CountJob(j);
            if (c < fewest) { fewest = c; best = j; }
        }
        return best;
    }
}
