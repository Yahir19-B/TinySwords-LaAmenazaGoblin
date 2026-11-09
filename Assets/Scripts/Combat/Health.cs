using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Vida de cualquier cosa que pueda recibir daño: héroe, goblins, casas y barriles.
public class Health : MonoBehaviour
{
    // Registro global de todo lo que tiene vida, para buscar objetivos sin usar física.
    public static readonly List<Health> All = new List<Health>();

    // Se avisa cada vez que algo muere (lo usa el sistema de combos).
    public static event Action<Health> OnAnyDied;

    public Team team = Team.Knights;
    public float maxHealth = 100;
    [Tooltip("Tamaño aproximado del objeto. Los edificios grandes necesitan un radio mayor para que se les pueda pegar desde el borde.")]
    public float radius = 0.4f;
    public bool isStructure;
    [Tooltip("Fracción del daño que se ignora (0.4 = recibe 40% menos daño).")]
    [Range(0f, 0.9f)] public float armor;
    [Tooltip("Muestra números de daño flotantes.")]
    public bool showDamageNumbers = true;

    [Header("Al morir")]
    public bool destroyOnDeath = true;
    public float destroyDelay = 0f;
    public GameObject deathEffectPrefab;
    public ResourcePickup lootPrefab;
    public int lootAmount;
    [Range(0f, 1f)] public float lootChance = 1f;

    [Header("Sonidos (opcional)")]
    public AudioClip hitSound;
    public AudioClip deathSound;

    public float Current { get; private set; }
    public bool IsDead { get; private set; }
    public float Normalized => maxHealth > 0 ? Current / maxHealth : 0;
    public bool IsFull => Current >= maxHealth;
    // Mientras sea mayor que 0 no recibe daño (esquivar).
    public float InvulnerableTime { get; set; }

    public event Action<float> OnDamaged;
    public event Action OnDied;
    public event Action OnChanged;

    readonly List<SpriteRenderer> flashRenderers = new List<SpriteRenderer>();
    readonly List<Color> originalColors = new List<Color>();
    Coroutine flashRoutine;

    void Awake()
    {
        Current = maxHealth;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (InvulnerableTime > 0) InvulnerableTime -= Time.deltaTime;
    }

    public void TakeDamage(float amount) => TakeDamage(amount, false);

    public void TakeDamage(float amount, bool critical)
    {
        if (IsDead || amount <= 0 || InvulnerableTime > 0) return;

        amount *= 1f - armor;
        Current = Mathf.Max(0, Current - amount);
        OnDamaged?.Invoke(amount);
        OnChanged?.Invoke();
        PlaySound(hitSound);

        if (showDamageNumbers && amount < 9999)
            DamagePopup.Show(transform.position + Vector3.up * (1f + radius), amount, critical, team == Team.Knights && !isStructure);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        if (Current <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        OnChanged?.Invoke();
    }

    // Revive con una fracción de la vida (edificios reconstruidos, héroe que reaparece).
    public void Revive(float fraction = 1f)
    {
        IsDead = false;
        Current = Mathf.Clamp01(fraction) * maxHealth;
        OnChanged?.Invoke();
    }

    public void SetFraction(float fraction)
    {
        Current = Mathf.Clamp01(fraction) * maxHealth;
        OnChanged?.Invoke();
    }

    void Die()
    {
        IsDead = true;
        PlaySound(deathSound);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (lootPrefab != null && lootAmount > 0 && UnityEngine.Random.value <= lootChance)
        {
            var loot = Instantiate(lootPrefab, transform.position, Quaternion.identity);
            loot.amount = lootAmount;
        }

        OnDied?.Invoke();
        OnAnyDied?.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }

    // Parpadeo rojo al recibir daño. Guarda el color original de cada sprite para restaurarlo
    // y no toca las barras de vida (antes las dejaba en blanco).
    IEnumerator Flash()
    {
        if (flashRenderers.Count == 0)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            {
                if (r.transform.parent != null && r.transform.parent.name == "HealthBar") continue;
                flashRenderers.Add(r);
                originalColors.Add(r.color);
            }
        }
        for (int i = 0; i < flashRenderers.Count; i++)
            if (flashRenderers[i] != null) flashRenderers[i].color = originalColors[i] * new Color(1f, 0.45f, 0.45f);
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < flashRenderers.Count; i++)
            if (flashRenderers[i] != null) flashRenderers[i].color = originalColors[i];
        flashRoutine = null;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // Distancia desde un punto hasta el "borde" de este objeto.
    public float DistanceFrom(Vector2 point)
    {
        return Mathf.Max(0, Vector2.Distance(point, transform.position) - radius);
    }

    // Busca el objetivo vivo más cercano de un bando dentro de un rango.
    public static Health FindClosest(Vector2 from, Team team, float maxDistance, Predicate<Health> filter = null)
    {
        Health best = null;
        float bestDist = maxDistance;
        foreach (var h in All)
        {
            if (h.IsDead || h.team != team) continue;
            if (filter != null && !filter(h)) continue;
            float d = h.DistanceFrom(from);
            if (d <= bestDist)
            {
                bestDist = d;
                best = h;
            }
        }
        return best;
    }
}
