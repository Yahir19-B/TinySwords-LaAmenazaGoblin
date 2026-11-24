using System.Collections;
using UnityEngine;

public enum EnemyAttackType
{
    Melee,      // Goblin con antorcha: golpea de cerca
    Ranged,     // Goblin TNT: lanza dinamita desde lejos
    Kamikaze    // Barril: corre hasta el objetivo y explota
}

// IA de los goblins: busca al Knight más cercano (héroe, pawns, edificios).
// Si no ve a nadie, marcha hacia el castillo.
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    public EnemyAttackType attackType = EnemyAttackType.Melee;
    public float moveSpeed = 2f;
    [Tooltip("Distancia a la que detecta unidades (héroe/pawns). Los edificios los ve desde cualquier lado.")]
    public float aggroRange = 4f;
    public float attackRange = 0.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    public float attackDuration = 0.5f;
    [Tooltip("Momento del golpe o lanzamiento dentro de la animación.")]
    public float hitDelay = 0.25f;
    public float structureDamageMultiplier = 1f;

    [Header("A distancia (TNT, arquero)")]
    public Projectile projectilePrefab;
    public Vector2 throwOffset = new Vector2(0, 0.5f);
    public int projectilesPerAttack = 1;
    public float projectileSpread = 1.2f;
    [Tooltip("Si el héroe se acerca más que esto, retrocede (0 = nunca).")]
    public float retreatDistance;

    [Header("Movimiento especial")]
    [Tooltip("Se mueve en zigzag (más difícil de golpear).")]
    public bool zigzag;
    public bool canDash;
    public float dashMinRange = 2.2f;
    public float dashMaxRange = 6f;
    public float dashSpeed = 10f;
    public float dashCooldown = 4f;
    public float dashDamageMultiplier = 1.5f;

    [Header("Kamikaze (Barril)")]
    public float explosionRadius = 1.5f;
    public float fuseTime = 0.6f;
    public GameObject explosionPrefab;

    public Health Health { get; private set; }

    Health target;
    float retargetTimer;
    float cooldown;
    bool busy;
    Rigidbody2D rb;
    UnitAnimator unitAnim;
    Vector2 desiredVelocity;
    float dashTimer = 2f;
    float zigzagSeed;
    bool shouted;

    void Awake()
    {
        Health = GetComponent<Health>();
        Health.team = Team.Goblins;
        rb = GetComponent<Rigidbody2D>();
        unitAnim = GetComponent<UnitAnimator>();
        Health.OnDied += () => { StopAllCoroutines(); enabled = false; };
        zigzagSeed = Random.value * 10f;
    }

    void Update()
    {
        desiredVelocity = Vector2.zero;
        cooldown -= Time.deltaTime;
        dashTimer -= Time.deltaTime;
        if (busy) return;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0 || target == null || target.IsDead)
        {
            retargetTimer = 0.5f;
            target = ChooseTarget();
            // Grito la primera vez que ve al héroe.
            if (!shouted && target != null && !target.isStructure && attackType != EnemyAttackType.Kamikaze)
            {
                shouted = true;
                AudioManager.Play("goblin_grito", transform.position, 0.6f);
            }
        }

        if (target == null)
        {
            unitAnim?.Play("Idle");
            return;
        }

        float dist = target.DistanceFrom(transform.position);
        unitAnim?.Face(target.transform.position.x - transform.position.x);

        Vector2 toTarget = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

        if (retreatDistance > 0 && !target.isStructure && dist < retreatDistance)
        {
            // Retrocede para mantener la distancia (arqueros).
            desiredVelocity = -toTarget * moveSpeed;
            if (rb == null) transform.position += (Vector3)(desiredVelocity * Time.deltaTime);
            unitAnim?.Play("Run");
            if (cooldown <= 0) StartCoroutine(Attack(target));
        }
        else if (canDash && dashTimer <= 0 && dist >= dashMinRange && dist <= dashMaxRange)
        {
            StartCoroutine(Dash(target));
        }
        else if (dist <= attackRange)
        {
            if (cooldown <= 0) StartCoroutine(Attack(target));
            else unitAnim?.Play("Idle");
        }
        else
        {
            Vector2 dir = toTarget;
            if (zigzag) dir = (dir + Vector2.Perpendicular(dir) * Mathf.Sin(Time.time * 6f + zigzagSeed) * 0.9f).normalized;
            desiredVelocity = dir * moveSpeed;
            if (rb == null) transform.position += (Vector3)(desiredVelocity * Time.deltaTime);
            unitAnim?.Play("Run");
        }
    }

    void FixedUpdate()
    {
        if (rb != null && !busy) rb.linearVelocity = desiredVelocity;
    }

    Health ChooseTarget()
    {
        // 1) Unidades cercanas (héroe y pawns).
        var unit = Health.FindClosest(transform.position, Team.Knights, aggroRange, h => !h.isStructure);
        if (unit != null) return unit;

        // 2) El edificio más cercano, o el castillo.
        var building = Health.FindClosest(transform.position, Team.Knights, float.MaxValue, h => h.isStructure);
        if (building != null) return building;

        return Castle.Instance != null && !Castle.Instance.Health.IsDead ? Castle.Instance.Health : null;
    }

    IEnumerator Attack(Health victim)
    {
        busy = true;
        cooldown = attackCooldown;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector2 toTarget = victim.transform.position - transform.position;
        unitAnim?.Play(AttackState(toTarget), true);

        if (attackType == EnemyAttackType.Kamikaze)
        {
            AudioManager.Play("mecha", transform.position);
            yield return new WaitForSeconds(fuseTime);
            Explode();
            yield break;
        }

        if (attackType == EnemyAttackType.Melee) AudioManager.Play(canDash ? "espada" : "antorcha", transform.position, 0.7f);
        yield return new WaitForSeconds(hitDelay);

        if (victim != null && !victim.IsDead)
        {
            if (attackType == EnemyAttackType.Ranged && projectilePrefab != null)
            {
                AudioManager.Play(projectilePrefab.arc ? "lanzar" : "flecha", transform.position);
                for (int i = 0; i < projectilesPerAttack; i++)
                {
                    Vector2 aim = victim.transform.position;
                    if (i > 0) aim += Random.insideUnitCircle * projectileSpread;
                    var p = Instantiate(projectilePrefab, (Vector2)transform.position + throwOffset, Quaternion.identity);
                    p.Launch(victim, aim, Team.Goblins, attackDamage);
                }
            }
            else if (victim.DistanceFrom(transform.position) <= attackRange + 0.3f)
            {
                victim.TakeDamage(victim.isStructure ? attackDamage * structureDamageMultiplier : attackDamage);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0, attackDuration - hitDelay));
        busy = false;
    }

    // Embestida: carga en línea recta hacia el objetivo y golpea al llegar (caballero oscuro).
    IEnumerator Dash(Health victim)
    {
        busy = true;
        dashTimer = dashCooldown;
        Vector2 dir = ((Vector2)victim.transform.position - (Vector2)transform.position).normalized;
        unitAnim?.Face(dir.x);
        unitAnim?.Play("Idle");
        yield return new WaitForSeconds(0.35f); // aviso antes de cargar
        unitAnim?.Play("Run");
        AudioManager.Play("embestida", transform.position);
        float t = 0;
        while (t < 0.45f && victim != null && !victim.IsDead)
        {
            t += Time.deltaTime;
            if (rb != null) rb.linearVelocity = dir * dashSpeed;
            else transform.position += (Vector3)(dir * dashSpeed * Time.deltaTime);
            if (victim.DistanceFrom(transform.position) <= attackRange) break;
            yield return null;
        }
        if (rb != null) rb.linearVelocity = Vector2.zero;
        unitAnim?.Play(AttackState(dir), true);
        yield return new WaitForSeconds(hitDelay);
        CombatUtils.DamageArea((Vector2)transform.position + dir * 0.6f, attackRange + 0.4f, Team.Goblins, attackDamage * dashDamageMultiplier, structureDamageMultiplier);
        yield return new WaitForSeconds(Mathf.Max(0, attackDuration - hitDelay));
        busy = false;
    }

    string AttackState(Vector2 toTarget)
    {
        if (unitAnim == null) return "Attack";
        if (Mathf.Abs(toTarget.y) > Mathf.Abs(toTarget.x))
        {
            string vertical = toTarget.y > 0 ? "AttackUp" : "AttackDown";
            if (unitAnim.HasState(vertical)) return vertical;
        }
        return "Attack";
    }

    void Explode()
    {
        AudioManager.Play("explosion", transform.position);
        CombatUtils.DamageArea(transform.position, explosionRadius, Team.Goblins, attackDamage, structureDamageMultiplier);
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        // Muere sin soltar botín para que el jugador no gane oro por dejarse explotar.
        Health.lootAmount = 0;
        Health.deathEffectPrefab = null;
        Health.TakeDamage(float.MaxValue);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
