using System.Collections.Generic;
using UnityEngine;

// Casa goblin: cuando el héroe se acerca, genera goblins cada cierto tiempo hasta que la destruyes.
// Para ganar el nivel hay que destruir todas las casas.
[RequireComponent(typeof(Health))]
public class GoblinHouse : MonoBehaviour
{
    public EnemyAI[] spawnPrefabs;
    public float spawnInterval = 9f;
    public int maxAlive = 2;
    [Tooltip("Solo genera goblins si el héroe está a esta distancia.")]
    public float activationRange = 9f;
    public Vector2 spawnOffset = new Vector2(0, -0.8f);

    [Header("Dificultad (la ajusta el generador de niveles)")]
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite destroyedSprite;
    public GameObject fireEffectPrefab;
    public GameObject destroyEffectPrefab;

    Health health;
    float timer;
    readonly List<Health> spawned = new List<Health>();

    void Awake()
    {
        health = GetComponent<Health>();
        health.team = Team.Goblins;
        health.isStructure = true;
        health.destroyOnDeath = false;
        health.OnDied += OnDestroyed;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        timer = spawnInterval * 0.5f;
    }

    void Update()
    {
        if (health.IsDead || spawnPrefabs == null || spawnPrefabs.Length == 0) return;
        var player = Player.Instance;
        if (player == null || player.IsDead) return;
        if (Vector2.Distance(player.transform.position, transform.position) > activationRange) return;

        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = spawnInterval;

        spawned.RemoveAll(h => h == null || h.IsDead);
        if (spawned.Count >= maxAlive) return;

        var prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
        if (prefab == null) return;
        var enemy = Instantiate(prefab, (Vector2)transform.position + spawnOffset, Quaternion.identity);
        var h = enemy.GetComponent<Health>();
        h.maxHealth *= healthMultiplier;
        h.Revive(1f);
        enemy.attackDamage *= damageMultiplier;
        enemy.aggroRange = Mathf.Max(enemy.aggroRange, activationRange);
        spawned.Add(h);
    }

    void OnDestroyed()
    {
        if (spriteRenderer != null && destroyedSprite != null) spriteRenderer.sprite = destroyedSprite;
        if (destroyEffectPrefab != null) Instantiate(destroyEffectPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);
        if (fireEffectPrefab != null) Instantiate(fireEffectPrefab, transform.position + new Vector3(0.2f, 0.9f), Quaternion.identity, transform);
    }
}
