using UnityEngine;
using UnityEngine.Rendering.Universal;

// Hace que una luz 2D parpadee como una antorcha o una fogata.
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Range(0f, 1f)] public float amount = 0.25f;
    public float speed = 6f;

    Light2D light2D;
    float baseIntensity;
    float seed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        light2D.intensity = baseIntensity * (1f + n * amount);
    }
}
