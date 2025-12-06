using UnityEngine;
using System.Collections;

public class CamShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private AnimationCurve shakeFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool useZ = false; // Option to shake Z-axis
    [SerializeField] private float frequency = 25f; // How fast the shake oscillates

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Private variables
    private Vector3 originalPos;
    private Coroutine currentShake;
    private bool isShaking = false;

    // Noise-based shake variables
    private float noiseOffsetX;
    private float noiseOffsetY;
    private float noiseOffsetZ;

    private void Start()
    {
        originalPos = transform.localPosition;

        // Initialize noise offsets with random values
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);
    }

    // Original shake method (improved)
    public void TriggerShake(float duration, float magnitude)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        // Stop any existing shake
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        Vector3 startPos = transform.localPosition;
        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float falloffMultiplier = shakeFalloff.Evaluate(normalizedTime);
            float currentMagnitude = magnitude * falloffMultiplier;

            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;
            float z = useZ ? Random.Range(-1f, 1f) * currentMagnitude : 0f;

            transform.localPosition = originalPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Smoothly return to original position
        yield return StartCoroutine(SmoothReturn(0.1f));
        isShaking = false;
    }

    // Perlin noise-based shake (smoother, more natural)
    public void TriggerPerlinShake(float duration, float magnitude, float frequency = 25f)
    {
        StartCoroutine(PerlinShake(duration, magnitude, frequency));
    }

    public IEnumerator PerlinShake(float duration, float magnitude, float shakeFrequency = 25f)
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float falloffMultiplier = shakeFalloff.Evaluate(normalizedTime);
            float currentMagnitude = magnitude * falloffMultiplier;

            // Use Perlin noise for smoother shake
            float x = (Mathf.PerlinNoise(Time.time * shakeFrequency + noiseOffsetX, 0f) - 0.5f) * 2f * currentMagnitude;
            float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency + noiseOffsetY) - 0.5f) * 2f * currentMagnitude;
            float z = useZ ? (Mathf.PerlinNoise(Time.time * shakeFrequency + noiseOffsetZ, Time.time * shakeFrequency) - 0.5f) * 2f * currentMagnitude : 0f;

            transform.localPosition = originalPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(SmoothReturn(0.1f));
        isShaking = false;
    }

    // Directional shake (useful for explosions, impacts from specific directions)
    public void TriggerDirectionalShake(float duration, float magnitude, Vector3 direction)
    {
        StartCoroutine(DirectionalShake(duration, magnitude, direction.normalized));
    }

    public IEnumerator DirectionalShake(float duration, float magnitude, Vector3 direction)
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float falloffMultiplier = shakeFalloff.Evaluate(normalizedTime);
            float currentMagnitude = magnitude * falloffMultiplier;

            // Add some randomness to the direction
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                useZ ? Random.Range(-0.3f, 0.3f) : 0f
            );

            Vector3 shakeOffset = (direction + randomOffset).normalized * currentMagnitude * Random.Range(0.5f, 1f);
            transform.localPosition = originalPos + shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(SmoothReturn(0.1f));
        isShaking = false;
    }

    // Impulse shake - quick, sharp shake that decays quickly
    public void TriggerImpulseShake(float magnitude, float decay = 0.95f)
    {
        StartCoroutine(ImpulseShake(magnitude, decay));
    }

    public IEnumerator ImpulseShake(float magnitude, float decay = 0.95f)
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        isShaking = true;
        float currentMagnitude = magnitude;

        while (currentMagnitude > 0.01f)
        {
            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;
            float z = useZ ? Random.Range(-1f, 1f) * currentMagnitude : 0f;

            transform.localPosition = originalPos + new Vector3(x, y, z);

            currentMagnitude *= decay;
            yield return null;
        }

        yield return StartCoroutine(SmoothReturn(0.05f));
        isShaking = false;
    }

    // Smooth return to original position
    private IEnumerator SmoothReturn(float duration)
    {
        Vector3 currentPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(currentPos, originalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    // Stop any ongoing shake
    public void StopShake()
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
            currentShake = null;
        }
        StartCoroutine(SmoothReturn(0.1f));
        isShaking = false;
    }

    // Utility methods
    public bool IsShaking => isShaking;

    public void SetOriginalPosition(Vector3 newOriginalPos)
    {
        originalPos = newOriginalPos;
    }

    public void ResetToOriginalPosition()
    {
        if (!isShaking)
        {
            transform.localPosition = originalPos;
        }
    }

    // Preset shake effects
    [System.Serializable]
    public class ShakePreset
    {
        public string name;
        public float duration;
        public float magnitude;
        public ShakeType type;
        public Vector3 direction = Vector3.zero;
        public AnimationCurve customFalloff;
    }

    public enum ShakeType
    {
        Random,
        Perlin,
        Directional,
        Impulse
    }

    [Header("Presets")]
    public ShakePreset[] shakePresets;

    public void TriggerPreset(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= shakePresets.Length) return;

        ShakePreset preset = shakePresets[presetIndex];

        // Temporarily override falloff curve if preset has custom one
        AnimationCurve originalCurve = shakeFalloff;
        if (preset.customFalloff != null && preset.customFalloff.keys.Length > 0)
        {
            shakeFalloff = preset.customFalloff;
        }

        switch (preset.type)
        {
            case ShakeType.Random:
                TriggerShake(preset.duration, preset.magnitude);
                break;
            case ShakeType.Perlin:
                TriggerPerlinShake(preset.duration, preset.magnitude, frequency);
                break;
            case ShakeType.Directional:
                TriggerDirectionalShake(preset.duration, preset.magnitude, preset.direction);
                break;
            case ShakeType.Impulse:
                TriggerImpulseShake(preset.magnitude);
                break;
        }

        // Restore original curve
        shakeFalloff = originalCurve;
    }

    public void TriggerPreset(string presetName)
    {
        for (int i = 0; i < shakePresets.Length; i++)
        {
            if (shakePresets[i].name == presetName)
            {
                TriggerPreset(i);
                return;
            }
        }

        if (debugMode)
        {
            Debug.LogWarning($"Shake preset '{presetName}' not found!");
        }
    }

    // Debug methods
    private void Update()
    {
        if (debugMode && Input.GetKeyDown(KeyCode.T))
        {
            TriggerShake(0.5f, 0.3f);
        }
    }
}