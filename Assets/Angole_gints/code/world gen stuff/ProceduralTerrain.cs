using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 256;
    public int height = 256;
    public float scale = 20f;
    public float heightMultiplier = 10f;

    [Header("Noise Settings")]
    public float noiseScale = 0.3f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;

    [Header("Water")]
    public GameObject waterPrefab;
    public float waterLevel = 3f;

    private GameObject waterInstance;
    private Mesh mesh;
    private Vector3[] vertices;

    void Start()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        // Check if this is a Unity Terrain component
        Terrain terrain = GetComponent<Terrain>();
        if (terrain != null)
        {
            GenerateUnityTerrain(terrain);
        }
        else
        {
            GenerateMeshTerrain();
        }

        // Add water if prefab is assigned
        if (waterPrefab != null)
        {
            SpawnWater();
        }
    }

    void GenerateUnityTerrain(Terrain terrain)
    {
        terrain.terrainData = GenerateTerrainData(terrain.terrainData);
    }

    TerrainData GenerateTerrainData(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightMultiplier, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    void GenerateMeshTerrain()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        CreateMeshVertices();
        CreateMeshTriangles();

        mesh.RecalculateNormals();

        // Add mesh collider if not present
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<MeshCollider>();
        }
        collider.sharedMesh = mesh;
    }

    void CreateMeshVertices()
    {
        vertices = new Vector3[(width + 1) * (height + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0, z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float y = CalculateHeight(x, z) * heightMultiplier;
                vertices[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2(x / (float)width, z / (float)height);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
    }

    void CreateMeshTriangles()
    {
        int[] triangles = new int[width * height * 6];

        for (int ti = 0, vi = 0, z = 0; z < height; z++, vi++)
        {
            for (int x = 0; x < width; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + width + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + width + 1;
                triangles[ti + 5] = vi + width + 2;
            }
        }

        mesh.triangles = triangles;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width + 1, height + 1];

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }

        return heights;
    }

    float CalculateHeight(int x, int y)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x / scale) * frequency * noiseScale + offset.x;
            float sampleY = (y / scale) * frequency * noiseScale + offset.y;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight;
    }

    void SpawnWater()
    {
        // Remove old water instance if exists
        if (waterInstance != null)
        {
            Destroy(waterInstance);
        }

        // Create water plane
        waterInstance = Instantiate(waterPrefab, transform.position + new Vector3(width / 2f, waterLevel, height / 2f), Quaternion.identity);
        waterInstance.transform.localScale = new Vector3(width / 10f, 1f, height / 10f);
        waterInstance.transform.parent = transform;
    }

    public Vector2 GetOffset()
    {
        return offset;
    }

    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
    }
}