using System.Collections.Generic;
using UnityEngine;

// Emboscada: cuando el héroe entra en la zona, aparecen goblins a su alrededor.
// Si limpias el resto del nivel sin pasar por aquí, la emboscada salta igual al final.
public class AmbushZone : MonoBehaviour
{
    public static readonly List<AmbushZone> Pending = new List<AmbushZone>();

    public float triggerRadius = 3f;
    public EnemyAI[] enemies;
    public float spawnDistance = 4f;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public GameObject spawnEffectPrefab;

    bool triggered;

    void OnEnable() => Pending.Add(this);
    void OnDisable() => Pending.Remove(this);

    void Update()
    {
        var player = Player.Instance;
        if (triggered || player == null || player.IsDead) return;
        if (Vector2.Distance(player.transform.position, transform.position) <= triggerRadius) Trigger();
    }

    public void Trigger()
    {
        if (triggered) return;
        triggered = true;
        Pending.Remove(this);
        var player = Player.Instance;
        Vector2 center = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            float angle = (i / (float)enemies.Length) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
            if (spawnEffectPrefab != null) Instantiate(spawnEffectPrefab, pos, Quaternion.identity);
            var e = Instantiate(enemies[i], pos, Quaternion.identity);
            var h = e.GetComponent<Health>();
            h.maxHealth *= healthMultiplier;
            h.Revive(1f);
            e.attackDamage *= damageMultiplier;
            e.aggroRange = 20f;
        }
        CameraFollow.Shake(0.2f, 0.4f);
        LevelHUD.Instance?.ShowMessage("¡EMBOSCADA!", 2f);
        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
