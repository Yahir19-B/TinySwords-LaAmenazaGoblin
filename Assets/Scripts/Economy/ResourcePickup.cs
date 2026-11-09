using UnityEngine;

// Bolsa de oro, tronco o carne tirada en el suelo (la sueltan los goblins al morir).
// Cuando el héroe se acerca, vuela hacia él y se suma a los recursos.
public class ResourcePickup : MonoBehaviour
{
    public ResourceType type = ResourceType.Gold;
    public int amount = 5;
    public float magnetRadius = 1.5f;
    public float flySpeed = 8f;
    [Tooltip("0 = nunca desaparece (los que se colocan en el mapa).")]
    public float lifetime = 30f;
    [Tooltip("Vida que recupera el héroe al recogerlo (la carne cura).")]
    public float healAmount;
    public AudioClip pickupSound;

    bool flying;

    void Start()
    {
        if (lifetime > 0) Destroy(gameObject, lifetime);
    }

    void Update()
    {
        var player = Player.Instance;
        if (player == null || player.IsDead) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (!flying && dist <= magnetRadius) flying = true;
        if (!flying) return;

        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, flySpeed * Time.deltaTime);
        if (dist < 0.2f)
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.Add(type, amount);
            if (type == ResourceType.Gold && LevelManager.Instance != null) LevelManager.Instance.AddGold(amount);
            if (healAmount > 0) player.Health.Heal(healAmount);
            if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            Destroy(gameObject);
        }
    }
}
