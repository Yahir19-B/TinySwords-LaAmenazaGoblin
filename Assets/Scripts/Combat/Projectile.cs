using UnityEngine;

// Flechas (torres/arqueros) y dinamita (goblin TNT).
// - Sin arco: vuela directo y persigue al objetivo.
// - Con arco: describe una parábola hasta el punto donde estaba el objetivo y explota en área.
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public bool arc;
    [Tooltip("Si es falso, vuela al punto donde estaba el objetivo y se puede esquivar.")]
    public bool homing = true;
    [Tooltip("Radio de impacto de los proyectiles que no persiguen.")]
    public float hitRadius = 0.5f;
    public float arcHeight = 1.5f;
    [Tooltip("0 = golpea solo al objetivo. Mayor que 0 = daño en área al impactar.")]
    public float areaRadius;
    public float structureDamageMultiplier = 1f;
    public bool rotateToDirection = true;
    public float spinSpeed;
    public GameObject impactEffectPrefab;
    public float maxLifetime = 5f;

    Team team;
    float damage;
    Health target;
    Vector2 start, end;
    float t, flightTime;
    Vector3 lastPos;

    public void Launch(Health target, Vector2 targetPoint, Team team, float damage)
    {
        this.target = target;
        this.team = team;
        this.damage = damage;
        start = transform.position;
        end = targetPoint;
        flightTime = Mathf.Max(0.15f, Vector2.Distance(start, end) / speed);
        lastPos = transform.position;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (arc)
        {
            t += Time.deltaTime / flightTime;
            Vector2 flat = Vector2.Lerp(start, end, t);
            float height = arcHeight * 4f * t * (1f - t);
            transform.position = flat + Vector2.up * height;
            if (t >= 1f) Impact();
        }
        else
        {
            if (homing && target != null && !target.IsDead) end = target.transform.position;
            transform.position = Vector2.MoveTowards(transform.position, end, speed * Time.deltaTime);
            if (Vector2.Distance(transform.position, end) < 0.1f) Impact();
        }

        Vector3 delta = transform.position - lastPos;
        if (spinSpeed != 0) transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
        else if (rotateToDirection && delta.sqrMagnitude > 0.00001f)
            transform.right = delta;
        lastPos = transform.position;
    }

    void Impact()
    {
        if (areaRadius > 0)
        {
            AudioManager.Play("explosion", transform.position, 0.8f);
            CombatUtils.DamageArea(transform.position, areaRadius, team, damage, structureDamageMultiplier);
        }
        else if (!homing)
            CombatUtils.DamageArea(transform.position, hitRadius, team, damage);
        else if (target != null && !target.IsDead)
            target.TakeDamage(damage);

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
