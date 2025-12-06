using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class WaterWaves : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 1f;
    public float waveHeight = 0.5f;
    public float waveFrequency = 0.5f;

    [Header("Perlin Noise")]
    public float noiseScale = 0.3f;
    public float noiseStrength = 0.2f;
    public float noiseSpeed = 0.5f;

    [Header("Mesh Settings")]
    public int gridSize = 20;

    [Header("Object Interaction")]
    public float interactionRadius = 2f;
    public float staticDampingSpeed = 2f;
    public float movementThreshold = 0.1f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private float[] dampingFactors;
    private MeshCollider meshCollider;

    private Dictionary<Collider, ObjectInteractionData> trackedObjects = new Dictionary<Collider, ObjectInteractionData>();

    private class ObjectInteractionData
    {
        public Vector3 lastPosition;
        public Vector3 currentPosition;
        public float velocity;
        public float dampingAmount;

        public ObjectInteractionData(Vector3 pos)
        {
            lastPosition = pos;
            currentPosition = pos;
            velocity = 0f;
            dampingAmount = 0f;
        }
    }

    void Start()
    {
        GenerateWaterMesh();
        meshCollider = GetComponent<MeshCollider>();
    }

    void GenerateWaterMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Create vertices
        originalVertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        Vector2[] uvs = new Vector2[originalVertices.Length];
        dampingFactors = new float[originalVertices.Length];

        for (int i = 0, y = 0; y <= gridSize; y++)
        {
            for (int x = 0; x <= gridSize; x++, i++)
            {
                float xPos = (x / (float)gridSize - 0.5f) * 10f;
                float zPos = (y / (float)gridSize - 0.5f) * 10f;
                originalVertices[i] = new Vector3(xPos, 0, zPos);
                uvs[i] = new Vector2(x / (float)gridSize, y / (float)gridSize);
                dampingFactors[i] = 1f;
            }
        }

        // Create triangles
        int[] triangles = new int[gridSize * gridSize * 6];
        for (int ti = 0, vi = 0, y = 0; y < gridSize; y++, vi++)
        {
            for (int x = 0; x < gridSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + gridSize + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + gridSize + 1;
                triangles[ti + 5] = vi + gridSize + 2;
            }
        }

        mesh.vertices = originalVertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        displacedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);
    }

    void Update()
    {
        UpdateTrackedObjects();
        UpdateDampingFactors();
        AnimateWaves();

        // Update mesh collider for interaction detection
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }
    }

    void UpdateTrackedObjects()
    {
        List<Collider> toRemove = new List<Collider>();

        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            ObjectInteractionData data = kvp.Value;
            data.lastPosition = data.currentPosition;
            data.currentPosition = kvp.Key.transform.position;

            // Calculate velocity
            data.velocity = (data.currentPosition - data.lastPosition).magnitude / Time.deltaTime;

            // Update damping based on movement
            if (data.velocity < movementThreshold)
            {
                // Object is static, increase damping
                data.dampingAmount = Mathf.Min(data.dampingAmount + staticDampingSpeed * Time.deltaTime, 1f);
            }
            else
            {
                // Object is moving, reduce damping
                data.dampingAmount = Mathf.Max(data.dampingAmount - staticDampingSpeed * 2f * Time.deltaTime, 0f);
            }
        }

        foreach (var col in toRemove)
        {
            trackedObjects.Remove(col);
        }
    }

    void UpdateDampingFactors()
    {
        // Reset damping factors
        for (int i = 0; i < dampingFactors.Length; i++)
        {
            dampingFactors[i] = Mathf.Min(dampingFactors[i] + Time.deltaTime * 0.5f, 1f);
        }

        // Apply damping from tracked objects
        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key == null) continue;

            Vector3 objPos = kvp.Value.currentPosition;
            float dampAmount = kvp.Value.dampingAmount;

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
                float distance = Vector3.Distance(new Vector3(worldPos.x, objPos.y, worldPos.z), objPos);

                if (distance < interactionRadius)
                {
                    float influence = 1f - (distance / interactionRadius);
                    float targetDamping = 1f - (influence * dampAmount);
                    dampingFactors[i] = Mathf.Min(dampingFactors[i], targetDamping);
                }
            }
        }
    }

    void AnimateWaves()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            Vector3 worldPos = transform.TransformPoint(vertex);

            // Sine/Cosine waves
            float wave1 = Mathf.Sin(Time.time * waveSpeed + vertex.x * waveFrequency + vertex.z * waveFrequency) * waveHeight;
            float wave2 = Mathf.Cos(Time.time * waveSpeed * 0.7f + vertex.x * waveFrequency * 1.3f) * waveHeight * 0.5f;

            // Perlin noise for organic movement
            float perlinX = worldPos.x * noiseScale + Time.time * noiseSpeed;
            float perlinZ = worldPos.z * noiseScale + Time.time * noiseSpeed * 0.8f;
            float perlinNoise = (Mathf.PerlinNoise(perlinX, perlinZ) - 0.5f) * 2f * noiseStrength;

            // Combine all wave effects and apply damping
            float totalWave = (wave1 + wave2 + perlinNoise) * dampingFactors[i];

            vertex.y = totalWave;
            displacedVertices[i] = vertex;
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!trackedObjects.ContainsKey(other) && other.gameObject != gameObject)
        {
            trackedObjects.Add(other, new ObjectInteractionData(other.transform.position));
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!trackedObjects.ContainsKey(other) && other.gameObject != gameObject)
        {
            trackedObjects.Add(other, new ObjectInteractionData(other.transform.position));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (trackedObjects.ContainsKey(other))
        {
            trackedObjects.Remove(other);
        }
    }
}