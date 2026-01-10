using UnityEngine;
using System.Collections.Generic;

public class DrillRain : MonoBehaviour
{
    [Header("Drill Settings")]
    [Tooltip("The drill prefab to rain down")]
    public GameObject drillPrefab;

    [Tooltip("Scale of the drill models")]
    public Vector3 drillScale = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("Rain Area")]
    [Tooltip("Follow the main camera")]
    public bool followCamera = true;

    [Tooltip("Target camera to follow (leave empty to use Main Camera)")]
    public Camera targetCamera;

    [Tooltip("Offset from camera position")]
    public Vector3 cameraOffset = new Vector3(0, 0, 0);

    [Tooltip("Width of the rain area")]
    public float rainWidth = 20f;

    [Tooltip("Length of the rain area")]
    public float rainLength = 20f;

    [Tooltip("Height to spawn drills")]
    public float spawnHeight = 20f;

    [Header("Rain Intensity")]
    [Tooltip("How many drills spawn per second")]
    public float drillsPerSecond = 10f;

    [Tooltip("Random drills per second variation")]
    public float intensityVariation = 2f;

    [Header("Drill Physics")]
    [Tooltip("Falling speed")]
    public float fallSpeed = 5f;

    [Tooltip("Random speed variation")]
    public float speedVariation = 2f;

    [Tooltip("Add rotation while falling")]
    public bool rotateWhileFalling = true;

    [Tooltip("Rotation speed")]
    public Vector3 rotationSpeed = new Vector3(100, 200, 50);

    [Header("Drill Behavior")]
    [Tooltip("Drills bounce on impact")]
    public bool bounceOnImpact = false;

    [Tooltip("Bounce force")]
    public float bounceForce = 2f;

    [Tooltip("Lifetime before despawn (seconds)")]
    public float drillLifetime = 5f;

    [Tooltip("Destroy on ground impact")]
    public bool destroyOnImpact = false;

    [Header("Visual Effects")]
    [Tooltip("Trail renderer for falling drills")]
    public bool addTrails = false;

    [Tooltip("Trail color")]
    public Color trailColor = Color.white;

    [Tooltip("Trail width")]
    public float trailWidth = 0.1f;

    [Tooltip("Trail time")]
    public float trailTime = 0.5f;

    private float spawnTimer = 0f;
    private List<GameObject> activedrills = new List<GameObject>();
    private Camera activeCamera;

    void Start()
    {
        // Use assigned camera or fall back to main camera
        if (targetCamera != null)
        {
            activeCamera = targetCamera;
        }
        else
        {
            activeCamera = Camera.main;
        }

        if (activeCamera == null)
        {
            Debug.LogError("No camera found! Assign a Target Camera or tag your camera as MainCamera.");
        }
    }

    void Update()
    {
        // Follow camera if enabled
        if (followCamera && activeCamera != null)
        {
            Vector3 targetPos = activeCamera.transform.position + cameraOffset;
            targetPos.y = 0; // Keep Y at ground level
            transform.position = targetPos;
        }

        SpawnDrills();
        CleanupOldDrills();
    }

    void SpawnDrills()
    {
        if (drillPrefab == null) return;

        float currentIntensity = drillsPerSecond + Random.Range(-intensityVariation, intensityVariation);
        float spawnInterval = 1f / Mathf.Max(0.1f, currentIntensity);

        spawnTimer += Time.deltaTime;

        while (spawnTimer >= spawnInterval)
        {
            SpawnSingleDrill();
            spawnTimer -= spawnInterval;
        }
    }

    void SpawnSingleDrill()
    {
        // Random position within rain area
        Vector3 spawnPos = transform.position;
        spawnPos.x += Random.Range(-rainWidth / 2f, rainWidth / 2f);
        spawnPos.z += Random.Range(-rainLength / 2f, rainLength / 2f);
        spawnPos.y += spawnHeight;

        // Random rotation
        Quaternion spawnRot = Random.rotation;

        // Instantiate drill
        GameObject drill = Instantiate(drillPrefab, spawnPos, spawnRot);
        drill.transform.localScale = drillScale;

        // Add falling behavior
        DrillFallBehavior fallBehavior = drill.AddComponent<DrillFallBehavior>();
        fallBehavior.fallSpeed = fallSpeed + Random.Range(-speedVariation, speedVariation);
        fallBehavior.rotateWhileFalling = rotateWhileFalling;
        fallBehavior.rotationSpeed = rotationSpeed;
        fallBehavior.lifetime = drillLifetime;
        fallBehavior.bounceOnImpact = bounceOnImpact;
        fallBehavior.bounceForce = bounceForce;
        fallBehavior.destroyOnImpact = destroyOnImpact;

        // Add rigidbody if doesn't exist
        Rigidbody rb = drill.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = drill.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // We control falling
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Add collider if doesn't exist
        if (drill.GetComponent<Collider>() == null)
        {
            drill.AddComponent<BoxCollider>();
        }

        // Add trail
        if (addTrails)
        {
            TrailRenderer trail = drill.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailWidth;
            trail.endWidth = trailWidth * 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }

        activedrills.Add(drill);
    }

    void CleanupOldDrills()
    {
        activedrills.RemoveAll(d => d == null);
    }

    // Visualize rain area in editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);

        Vector3 center = transform.position;
        center.y += spawnHeight / 2f;

        Vector3 size = new Vector3(rainWidth, spawnHeight, rainLength);
        Gizmos.DrawWireCube(center, size);

        // Draw spawn plane
        Gizmos.color = Color.cyan;
        Vector3[] corners = new Vector3[4];
        corners[0] = transform.position + new Vector3(-rainWidth / 2, spawnHeight, -rainLength / 2);
        corners[1] = transform.position + new Vector3(rainWidth / 2, spawnHeight, -rainLength / 2);
        corners[2] = transform.position + new Vector3(rainWidth / 2, spawnHeight, rainLength / 2);
        corners[3] = transform.position + new Vector3(-rainWidth / 2, spawnHeight, rainLength / 2);

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }
}

// Behavior for individual falling drills
public class DrillFallBehavior : MonoBehaviour
{
    public float fallSpeed = 5f;
    public bool rotateWhileFalling = true;
    public Vector3 rotationSpeed = new Vector3(100, 200, 50);
    public float lifetime = 5f;
    public bool bounceOnImpact = false;
    public float bounceForce = 2f;
    public bool destroyOnImpact = false;

    private Rigidbody rb;
    private float aliveTime = 0f;
    private bool hasHitGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Fall down
        if (!hasHitGround)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }

        // Rotate while falling
        if (rotateWhileFalling)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        // Lifetime
        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitGround) return;

        hasHitGround = true;

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
        else if (bounceOnImpact && rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.up * bounceForce;
        }
        else if (rb != null)
        {
            rb.useGravity = true;
        }
    }
}