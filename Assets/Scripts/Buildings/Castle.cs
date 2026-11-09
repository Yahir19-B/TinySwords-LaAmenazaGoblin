using UnityEngine;

// El castillo de los Knights. Si lo destruyen, se pierde la partida.
// Aquí aparecen los pawns nuevos y reaparece el héroe.
[RequireComponent(typeof(Building))]
public class Castle : MonoBehaviour
{
    public static Castle Instance { get; private set; }

    public Transform spawnPoint;

    public Health Health { get; private set; }
    public Vector2 SpawnPosition => spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position + Vector2.down * 1.5f;

    void Awake()
    {
        Instance = this;
        Health = GetComponent<Health>();
        Health.OnDied += () => GameManager.Instance?.Defeat();
    }
}
