using UnityEngine;

// Para efectos de una sola vez (explosiones, fuego, calaveras): se destruyen solos.
public class AutoDestroy : MonoBehaviour
{
    public float lifetime = 1f;

    void Start() => Destroy(gameObject, lifetime);
}
