using System.Collections;
using UnityEngine;

// Héroe controlado por el jugador (Warrior azul).
// WASD / flechas: moverse   Espacio / J / clic izquierdo: atacar   Shift / K: esquivar
// E: interactuar (modo castillo)
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Movimiento")]
    public float speed = 5;
    [Range(0f, 1f)] public float moveSpeedWhileAttacking = 0.2f;

    [Header("Ataque")]
    public float attackDamage = 25;
    public float attackReach = 0.9f;
    public float attackRadius = 0.9f;
    public float attackDuration = 0.5f;
    [Tooltip("Momento del golpe dentro de la animación (en segundos).")]
    public float hitDelay = 0.2f;
    public float structureDamageMultiplier = 0.5f;
    public AudioClip swingSound;
    [Range(0f, 1f)] public float critChance = 0.15f;
    public float critMultiplier = 2f;

    [Header("Esquivar")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 1f;
    public float dashInvulnerability = 0.3f;

    [Header("Reaparición")]
    public float respawnTime = 5f;

    public Health Health { get; private set; }
    public Interactable CurrentInteractable { get; private set; }
    public bool IsDead => Health != null && Health.IsDead;
    public float RespawnCountdown { get; private set; }
    public float DashCooldownLeft => Mathf.Max(0, dashTimer);

    Rigidbody2D rg2d;
    UnitAnimator unitAnim;
    Collider2D col;
    Vector2 movementInput;
    Vector2 facing = Vector2.right;
    float attackTimer;
    float dashTimer;
    float dashTime;
    Vector2 dashDir;
    bool comboFlip;

    void Awake()
    {
        Instance = this;
        rg2d = GetComponent<Rigidbody2D>();
        unitAnim = GetComponent<UnitAnimator>();
        if (unitAnim == null) unitAnim = gameObject.AddComponent<UnitAnimator>();
        col = GetComponent<Collider2D>();
        Health = GetComponent<Health>();
        if (Health == null)
        {
            Health = gameObject.AddComponent<Health>();
            Health.maxHealth = 150;
            Health.Revive(1f);
        }
        Health.team = Team.Knights;
        Health.destroyOnDeath = false;
        Health.OnDied += HandleDeath;
    }

    void Update()
    {
        if (IsDead || Time.timeScale == 0f)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        if (TouchControls.Enabled && TouchControls.Move != Vector2.zero) movementInput = TouchControls.Move;
        movementInput = movementInput.normalized;

        if (movementInput != Vector2.zero)
        {
            facing = movementInput;
            unitAnim.Face(movementInput.x);
        }

        attackTimer -= Time.deltaTime;
        dashTimer -= Time.deltaTime;
        dashTime -= Time.deltaTime;
        bool attacking = attackTimer > 0;

        if (dashTimer <= 0 && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K) || TouchControls.DashDown))
        {
            // Esquivar: impulso rápido hacia donde te mueves, sin recibir daño por un instante.
            dashDir = movementInput != Vector2.zero ? movementInput : facing;
            dashTime = dashDuration;
            dashTimer = dashCooldown;
            Health.InvulnerableTime = dashInvulnerability;
            unitAnim.Play("Run", true);
            AudioManager.Play("esquivar");
        }

        if (!attacking && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) || TouchControls.AttackHeld))
            StartCoroutine(Attack());
        else if (!attacking)
            unitAnim.Play(movementInput != Vector2.zero ? "Run" : "Idle");

        CurrentInteractable = Interactable.FindClosest(transform.position);
        if (CurrentInteractable != null && Input.GetKeyDown(KeyCode.E))
            CurrentInteractable.Interact(this);
    }

    void FixedUpdate()
    {
        if (dashTime > 0 && !IsDead)
        {
            rg2d.linearVelocity = dashDir * dashSpeed;
            return;
        }
        float mult = attackTimer > 0 ? moveSpeedWhileAttacking : 1f;
        rg2d.linearVelocity = movementInput * speed * mult;
    }

    IEnumerator Attack()
    {
        attackTimer = attackDuration;

        // Dirección del golpe: arriba, abajo o de lado según hacia dónde mira el héroe.
        string suffix = comboFlip && unitAnim.HasState("Attack2") ? "2" : "";
        comboFlip = !comboFlip;
        Vector2 dir;
        string state;
        if (Mathf.Abs(facing.y) > Mathf.Abs(facing.x))
        {
            dir = facing.y > 0 ? Vector2.up : Vector2.down;
            state = (facing.y > 0 ? "AttackUp" : "AttackDown") + suffix;
        }
        else
        {
            dir = facing.x >= 0 ? Vector2.right : Vector2.left;
            state = "Attack" + suffix;
        }
        unitAnim.Play(state, true);
        if (swingSound != null) AudioSource.PlayClipAtPoint(swingSound, transform.position);
        else AudioManager.Play("espada", 0.7f);

        yield return new WaitForSeconds(hitDelay);
        if (IsDead) yield break;

        Vector2 hitPoint = (Vector2)transform.position + dir * attackReach;
        bool crit = Random.value < critChance;
        int hits = CombatUtils.DamageArea(hitPoint, attackRadius, Team.Knights, crit ? attackDamage * critMultiplier : attackDamage, structureDamageMultiplier, crit);
        if (hits > 0) AudioManager.Play(crit ? "critico" : "impacto");
        if (crit && hits > 0) CameraFollow.Shake(0.08f, 0.12f);
    }

    void HandleDeath()
    {
        StopAllCoroutines();
        attackTimer = 0;
        rg2d.linearVelocity = Vector2.zero;
        // Con castillo (modo defensa) reaparece; en los niveles, morir es perder.
        if (Castle.Instance != null) StartCoroutine(Respawn());
        else StartCoroutine(DieAndLose());
    }

    IEnumerator DieAndLose()
    {
        unitAnim.SetVisible(false);
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(1.5f);
        GameManager.Instance?.Defeat();
    }

    IEnumerator Respawn()
    {
        unitAnim.SetVisible(false);
        if (col != null) col.enabled = false;

        RespawnCountdown = respawnTime;
        while (RespawnCountdown > 0)
        {
            RespawnCountdown -= Time.deltaTime;
            yield return null;
        }

        if (Castle.Instance != null)
            transform.position = Castle.Instance.SpawnPosition;

        Health.Revive(1f);
        unitAnim.SetVisible(true);
        if (col != null) col.enabled = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + facing * attackReach, attackRadius);
    }
}
