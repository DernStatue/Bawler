using UnityEngine;

public class Spin : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed on each axis (degrees per second)")]
    public Vector3 rotationSpeed = new Vector3(0, 100, 0);

    [Header("Rotation Mode")]
    public RotationMode mode = RotationMode.Constant;

    [Header("Space")]
    [Tooltip("Rotate in world space or local space")]
    public bool useLocalSpace = true;

    [Header("Pulse Mode Settings")]
    public float pulseSpeed = 2f;
    public float minSpeedMultiplier = 0.5f;
    public float maxSpeedMultiplier = 1.5f;

    [Header("Wave Mode Settings")]
    public float waveSpeed = 1f;
    public float waveAmplitude = 50f;

    [Header("Options")]
    [Tooltip("Randomize rotation direction on start")]
    public bool randomizeDirection = false;

    [Tooltip("Smoothly accelerate to target speed")]
    public bool smoothAcceleration = false;

    [Tooltip("Time to reach full speed (if smooth acceleration)")]
    public float accelerationTime = 1f;

    private Vector3 currentSpeed;
    private Vector3 baseRotationSpeed;
    private float accelerationProgress = 0f;

    public enum RotationMode
    {
        Constant,       // Normal constant rotation
        Pulse,          // Speed up and slow down
        Wave,           // Sine wave rotation
        RandomWobble    // Random wobble effect
    }

    void Start()
    {
        baseRotationSpeed = rotationSpeed;

        // Randomize direction if enabled
        if (randomizeDirection)
        {
            rotationSpeed.x *= Random.Range(-1f, 1f) > 0 ? 1 : -1;
            rotationSpeed.y *= Random.Range(-1f, 1f) > 0 ? 1 : -1;
            rotationSpeed.z *= Random.Range(-1f, 1f) > 0 ? 1 : -1;
        }

        // Start at zero if using smooth acceleration
        if (smoothAcceleration)
        {
            currentSpeed = Vector3.zero;
        }
        else
        {
            currentSpeed = rotationSpeed;
        }
    }

    void Update()
    {
        // Handle smooth acceleration
        if (smoothAcceleration && accelerationProgress < 1f)
        {
            accelerationProgress += Time.deltaTime / accelerationTime;
            currentSpeed = Vector3.Lerp(Vector3.zero, rotationSpeed, accelerationProgress);
        }
        else if (!smoothAcceleration)
        {
            currentSpeed = rotationSpeed;
        }

        // Calculate rotation based on mode
        Vector3 rotation = Vector3.zero;

        switch (mode)
        {
            case RotationMode.Constant:
                rotation = currentSpeed * Time.deltaTime;
                break;

            case RotationMode.Pulse:
                float pulseMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier,
                    (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                rotation = currentSpeed * pulseMultiplier * Time.deltaTime;
                break;

            case RotationMode.Wave:
                float wave = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
                rotation = new Vector3(
                    currentSpeed.x + wave,
                    currentSpeed.y + wave,
                    currentSpeed.z + wave
                ) * Time.deltaTime;
                break;

            case RotationMode.RandomWobble:
                Vector3 wobble = new Vector3(
                    Random.Range(-waveAmplitude, waveAmplitude),
                    Random.Range(-waveAmplitude, waveAmplitude),
                    Random.Range(-waveAmplitude, waveAmplitude)
                );
                rotation = (currentSpeed + wobble * Time.deltaTime) * Time.deltaTime;
                break;
        }

        // Apply rotation
        if (useLocalSpace)
        {
            transform.Rotate(rotation, Space.Self);
        }
        else
        {
            transform.Rotate(rotation, Space.World);
        }
    }

    // Public methods to control rotation from other scripts
    public void SetRotationSpeed(Vector3 newSpeed)
    {
        rotationSpeed = newSpeed;
        currentSpeed = newSpeed;
    }

    public void MultiplySpeed(float multiplier)
    {
        rotationSpeed *= multiplier;
    }

    public void StopRotation()
    {
        rotationSpeed = Vector3.zero;
        currentSpeed = Vector3.zero;
    }

    public void ResetRotation()
    {
        rotationSpeed = baseRotationSpeed;
        currentSpeed = baseRotationSpeed;
    }
}