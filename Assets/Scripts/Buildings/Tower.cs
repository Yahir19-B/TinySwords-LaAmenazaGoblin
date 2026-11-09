using UnityEngine;

// Torre de defensa: dispara flechas al goblin más cercano. Solo funciona cuando el edificio está completo
// (el componente Building la activa y desactiva).
public class Tower : MonoBehaviour
{
    public float range = 5f;
    public float damage = 12f;
    public float fireRate = 1f;
    public Projectile arrowPrefab;
    public Transform firePoint;
    [Tooltip("Arquero decorativo arriba de la torre (opcional).")]
    public UnitAnimator archer;

    float cooldown;

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0) return;

        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        var target = Health.FindClosest(origin, Team.Goblins, range);
        if (target == null)
        {
            archer?.Play("Idle");
            return;
        }

        cooldown = 1f / Mathf.Max(0.05f, fireRate);
        if (archer != null)
        {
            archer.Face(target.transform.position.x - origin.x);
            archer.Play("Shoot", true);
        }
        if (arrowPrefab != null)
        {
            var arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);
            arrow.Launch(target, target.transform.position, Team.Knights, damage);
        }
        else
        {
            target.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
