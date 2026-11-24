using System.Collections;
using UnityEngine;

// Jefe goblin del nivel 10. Duerme hasta que el héroe se acerca y luego:
// - golpea el suelo cuando estás cerca (daño en área),
// - lanza ráfagas de dinamita cuando estás lejos,
// - invoca goblins cuando tiene menos de 3/4 de vida,
// - se enfurece (más rápido) cuando le queda poca vida.
[RequireComponent(typeof(Health))]
public class GoblinBoss : MonoBehaviour
{
    public string bossName = "Gran Jefe Goblin";
    public float moveSpeed = 1.6f;
    public float wakeRange = 8f;

    [Header("Golpe al suelo")]
    public float slamRange = 1.6f;
    public float slamRadius = 2f;
    public float slamDamage = 25f;
    public float slamCooldown = 2.2f;
    public float slamHitDelay = 0.4f;
    public float slamDuration = 0.8f;
    public GameObject slamEffectPrefab;

    [Header("Dinamita")]
    public Projectile dynamitePrefab;
    public int dynamitePerVolley = 3;
    public float throwDamage = 15f;
    public float throwCooldown = 5f;

    [Header("Invocación")]
    public EnemyAI[] minionPrefabs;
    public int minionsPerSummon = 2;
    public float summonCooldown = 14f;
    public GameObject summonEffectPrefab;

    [Header("Furia")]
    [Range(0f, 1f)] public float enrageBelow = 0.35f;
    public float enrageMultiplier = 1.5f;

    public Health Health { get; private set; }
    bool awake;
    public bool IsAwake
    {
        get => awake;
        private set
        {
            if (value && !awake) AudioManager.Play("jefe_rugido");
            awake = value;
        }
    }
    public bool Enraged => Health.Normalized <= enrageBelow;

    UnitAnimator unitAnim;
    Rigidbody2D rb;
    Vector2 desiredVelocity;
    bool busy;
    bool furySounded;
    float slamTimer, throwTimer = 3f, summonTimer = 6f;

    void Awake()
    {
        Health = GetComponent<Health>();
        Health.team = Team.Goblins;
        unitAnim = GetComponent<UnitAnimator>();
        rb = GetComponent<Rigidbody2D>();
        Health.OnDied += () => { StopAllCoroutines(); enabled = false; AudioManager.Play("jefe_muere"); CameraFollow.Shake(0.4f, 0.8f); };
        Health.OnDamaged += _ => IsAwake = true;
    }

    void Start()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.Boss = this;
    }

    void Update()
    {
        desiredVelocity = Vector2.zero;
        var player = Player.Instance;
        if (player == null || player.IsDead)
        {
            if (!busy) unitAnim?.Play("Idle");
            return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;
        float dist = toPlayer.magnitude;
        if (!IsAwake)
        {
            unitAnim?.Play("Idle");
            if (dist <= wakeRange) IsAwake = true;
            return;
        }

        if (Enraged && !furySounded)
        {
            furySounded = true;
            AudioManager.Play("jefe_furia");
        }
        float rate = Enraged ? enrageMultiplier : 1f;
        slamTimer -= Time.deltaTime * rate;
        throwTimer -= Time.deltaTime * rate;
        summonTimer -= Time.deltaTime * rate;
        if (busy) return;

        unitAnim?.Face(toPlayer.x);

        if (summonTimer <= 0 && Health.Normalized < 0.75f && minionPrefabs != null && minionPrefabs.Length > 0)
            StartCoroutine(Summon());
        else if (dist <= slamRange && slamTimer <= 0)
            StartCoroutine(Slam(toPlayer));
        else if (dist > 3f && throwTimer <= 0 && dynamitePrefab != null)
            StartCoroutine(ThrowVolley(player));
        else if (dist > slamRange * 0.8f)
        {
            desiredVelocity = toPlayer.normalized * moveSpeed * rate;
            if (rb == null) transform.position += (Vector3)(desiredVelocity * Time.deltaTime);
            unitAnim?.Play("Run");
        }
        else unitAnim?.Play("Idle");
    }

    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = busy ? Vector2.zero : desiredVelocity;
    }

    IEnumerator Slam(Vector2 toPlayer)
    {
        busy = true;
        slamTimer = slamCooldown;
        string state = Mathf.Abs(toPlayer.y) > Mathf.Abs(toPlayer.x) ? (toPlayer.y > 0 ? "AttackUp" : "AttackDown") : "Attack";
        unitAnim?.Play(state, true);
        yield return new WaitForSeconds(slamHitDelay);

        Vector2 point = (Vector2)transform.position + toPlayer.normalized * 0.8f;
        AudioManager.Play("jefe_golpe", point);
        CombatUtils.DamageArea(point, slamRadius, Team.Goblins, slamDamage);
        if (slamEffectPrefab != null) Instantiate(slamEffectPrefab, point, Quaternion.identity);
        CameraFollow.Shake(0.25f, 0.3f);

        yield return new WaitForSeconds(Mathf.Max(0, slamDuration - slamHitDelay));
        busy = false;
    }

    IEnumerator ThrowVolley(Player player)
    {
        busy = true;
        throwTimer = throwCooldown;
        int count = Enraged ? dynamitePerVolley + 2 : dynamitePerVolley;
        for (int i = 0; i < count; i++)
        {
            if (player == null || player.IsDead) break;
            unitAnim?.Play("Attack", true);
            Vector2 target = (Vector2)player.transform.position + Random.insideUnitCircle * 1.2f;
            var p = Instantiate(dynamitePrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            AudioManager.Play("lanzar", transform.position);
            p.Launch(null, target, Team.Goblins, throwDamage);
            yield return new WaitForSeconds(0.35f);
        }
        busy = false;
    }

    IEnumerator Summon()
    {
        busy = true;
        summonTimer = summonCooldown;
        AudioManager.Play("jefe_invoca");
        unitAnim?.Play("Idle");
        CameraFollow.Shake(0.15f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 2f;
            if (summonEffectPrefab != null) Instantiate(summonEffectPrefab, pos, Quaternion.identity);
            var prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            if (prefab != null)
            {
                var m = Instantiate(prefab, pos, Quaternion.identity);
                m.aggroRange = 20f;
            }
        }
        yield return new WaitForSeconds(0.4f);
        busy = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
