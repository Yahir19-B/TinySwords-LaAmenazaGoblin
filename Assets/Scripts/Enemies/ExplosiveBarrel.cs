using System.Collections;
using UnityEngine;

// Barril de pólvora: al golpearlo explota y daña a TODOS alrededor (goblins, héroe y otros barriles).
// Úsalo a tu favor atrayendo goblins cerca.
[RequireComponent(typeof(Health))]
public class ExplosiveBarrel : MonoBehaviour
{
    public float radius = 2.2f;
    public float damage = 70f;
    public float fuse = 0.25f;
    public GameObject explosionPrefab;

    void Awake()
    {
        var h = GetComponent<Health>();
        h.team = Team.Neutral;
        h.destroyOnDeath = false;
        h.showDamageNumbers = false;
        h.OnDied += () => StartCoroutine(Explode());
    }

    IEnumerator Explode()
    {
        yield return new WaitForSeconds(fuse);
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
        CameraFollow.Shake(0.2f, 0.3f);
        CombatUtils.DamageArea(transform.position, radius, Team.Neutral, damage, 1f);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
