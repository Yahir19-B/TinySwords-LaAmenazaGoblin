using UnityEngine;

// Zona que hace daño a quien la pisa (trampas de fuego). Afecta al héroe y a los goblins.
// Puede encenderse y apagarse por turnos para que el jugador elija cuándo pasar.
public class HazardZone : MonoBehaviour
{
    public float radius = 0.7f;
    public float damagePerTick = 8f;
    public float tickInterval = 0.5f;
    [Tooltip("0 = siempre encendida. Si no, segundos encendida / apagada.")]
    public float cycleTime;
    public GameObject visual;

    float tickTimer;
    float cycleTimer;
    bool active = true;

    void Start() => cycleTimer = cycleTime * Random.value;

    void Update()
    {
        if (cycleTime > 0)
        {
            cycleTimer -= Time.deltaTime;
            if (cycleTimer <= 0)
            {
                active = !active;
                cycleTimer = cycleTime;
                if (visual != null) visual.SetActive(active);
                if (active) AudioManager.Play("fuego", transform.position, 0.5f);
            }
        }
        if (!active) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0) return;
        tickTimer = tickInterval;
        CombatUtils.DamageArea(transform.position, radius, Team.Neutral, damagePerTick);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
