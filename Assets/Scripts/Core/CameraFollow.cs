using UnityEngine;

// Cámara que sigue al héroe con suavidad, sin salirse del mapa, y que puede temblar (golpes del jefe).
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public Vector2 offset = new Vector2(0, 0.5f);
    [Tooltip("Límites del mapa (esquina inferior izquierda y superior derecha).")]
    public Vector2 boundsMin = new Vector2(-50, -50);
    public Vector2 boundsMax = new Vector2(50, 50);

    static float shakeAmount, shakeTime;

    Camera cam;
    Vector3 velocity;

    void Awake() => cam = GetComponent<Camera>();

    void Start()
    {
        if (target == null && Player.Instance != null) target = Player.Instance.transform;
        if (target != null) transform.position = Clamp((Vector2)target.position + offset);
    }

    public static void Shake(float amount, float duration)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
        shakeTime = Mathf.Max(shakeTime, duration);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = Clamp((Vector2)target.position + offset);
        Vector3 pos = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        if (shakeTime > 0)
        {
            shakeTime -= Time.unscaledDeltaTime;
            pos += (Vector3)(Random.insideUnitCircle * shakeAmount);
            if (shakeTime <= 0) shakeAmount = 0;
        }
        transform.position = pos;
    }

    Vector3 Clamp(Vector2 p)
    {
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        float x = boundsMax.x - boundsMin.x > 2 * w ? Mathf.Clamp(p.x, boundsMin.x + w, boundsMax.x - w) : (boundsMin.x + boundsMax.x) / 2f;
        float y = boundsMax.y - boundsMin.y > 2 * h ? Mathf.Clamp(p.y, boundsMin.y + h, boundsMax.y - h) : (boundsMin.y + boundsMax.y) / 2f;
        return new Vector3(x, y, -10f);
    }
}
