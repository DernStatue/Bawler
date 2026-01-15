using UnityEngine;
using System.Collections.Generic;

public class ProceduralTownGenerator : MonoBehaviour
{
    [Header("Town Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridDepth = 10;
    [SerializeField] private float blockSize = 20f;
    [SerializeField] private float roadWidth = 5f;
    [SerializeField] private int seed = 12345;

    [Header("Building Density")]
    [SerializeField, Range(0f, 1f)] private float buildingDensity = 0.7f;
    [SerializeField, Range(0f, 0.5f)] private float parkDensity = 0.15f;

    [Header("Road Prefabs (Optional)")]
    [SerializeField] private GameObject[] roadStraightPrefabs;
    [SerializeField] private GameObject[] roadIntersectionPrefabs;

    [Header("Building Prefabs (Optional)")]
    [SerializeField] private GameObject[] housePrefabs;
    [SerializeField] private GameObject[] apartmentPrefabs;
    [SerializeField] private GameObject[] shopPrefabs;
    [SerializeField] private GameObject[] officePrefabs;
    [SerializeField] private GameObject[] parkPrefabs;

    [Header("Building Materials (If no prefabs)")]
    [SerializeField] private Material houseMaterial;
    [SerializeField] private Material apartmentMaterial;
    [SerializeField] private Material shopMaterial;
    [SerializeField] private Material officeMaterial;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Material parkMaterial;

    private System.Random rng;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        GenerateTown();
    }

    [ContextMenu("Generate Town")]
    public void GenerateTown()
    {
        ClearTown();
        rng = new System.Random(seed);

        GenerateRoads();
        GenerateBuildings();
    }

    [ContextMenu("Clear Town")]
    public void ClearTown()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
    }

    void GenerateRoads()
    {
        GameObject roadParent = new GameObject("Roads");
        roadParent.transform.parent = transform;
        spawnedObjects.Add(roadParent);

        float totalSize = blockSize + roadWidth;
        float townWidth = gridWidth * totalSize + roadWidth;
        float townDepth = gridDepth * totalSize + roadWidth;

        // Track intersections to avoid duplicates
        HashSet<Vector2Int> intersections = new HashSet<Vector2Int>();

        // Generate intersections first
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z <= gridDepth; z++)
            {
                Vector3 position = new Vector3(x * totalSize, 0, z * totalSize);
                GameObject intersection = CreateRoadIntersection(position);
                if (intersection != null)
                {
                    intersection.transform.parent = roadParent.transform;
                    intersection.name = $"Intersection_{x}_{z}";
                }
                intersections.Add(new Vector2Int(x, z));
            }
        }

        // Horizontal road segments (between intersections)
        for (int z = 0; z <= gridDepth; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector3 position = new Vector3(x * totalSize + totalSize / 2f, 0, z * totalSize);
                GameObject roadH = CreateRoadSegment(position, 0f); // 0 degrees = horizontal
                if (roadH != null)
                {
                    roadH.transform.parent = roadParent.transform;
                    roadH.name = $"Road_H_{x}_{z}";
                }
            }
        }

        // Vertical road segments (between intersections)
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 position = new Vector3(x * totalSize, 0, z * totalSize + totalSize / 2f);
                GameObject roadV = CreateRoadSegment(position, 90f); // 90 degrees = vertical
                if (roadV != null)
                {
                    roadV.transform.parent = roadParent.transform;
                    roadV.name = $"Road_V_{x}_{z}";
                }
            }
        }
    }

    GameObject CreateRoadIntersection(Vector3 position)
    {
        if (roadIntersectionPrefabs != null && roadIntersectionPrefabs.Length > 0)
        {
            GameObject prefab = roadIntersectionPrefabs[rng.Next(0, roadIntersectionPrefabs.Length)];
            if (prefab != null)
            {
                return Instantiate(prefab, position, Quaternion.identity);
            }
        }

        // Create procedural intersection
        GameObject intersection = GameObject.CreatePrimitive(PrimitiveType.Cube);
        intersection.transform.position = position;
        intersection.transform.localScale = new Vector3(roadWidth, 0.1f, roadWidth);

        if (roadMaterial != null)
        {
            intersection.GetComponent<Renderer>().material = roadMaterial;
        }
        else
        {
            intersection.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.25f);
        }

        return intersection;
    }

    GameObject CreateRoadSegment(Vector3 position, float rotation)
    {
        GameObject segment;

        if (roadStraightPrefabs != null && roadStraightPrefabs.Length > 0)
        {
            GameObject prefab = roadStraightPrefabs[rng.Next(0, roadStraightPrefabs.Length)];
            if (prefab != null)
            {
                segment = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0));
                return segment;
            }
        }

        // Create procedural road segment
        float totalSize = blockSize + roadWidth;
        float segmentLength = totalSize - roadWidth; // Length between intersections

        segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.transform.position = position;
        segment.transform.rotation = Quaternion.Euler(0, rotation, 0);
        segment.transform.localScale = new Vector3(roadWidth, 0.1f, segmentLength);

        if (roadMaterial != null)
        {
            segment.GetComponent<Renderer>().material = roadMaterial;
        }
        else
        {
            segment.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
        }

        return segment;
    }

    void GenerateBuildings()
    {
        GameObject buildingParent = new GameObject("Buildings");
        buildingParent.transform.parent = transform;
        spawnedObjects.Add(buildingParent);

        float totalSize = blockSize + roadWidth;
        Vector2 townCenter = new Vector2(gridWidth / 2f, gridDepth / 2f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                float rand = (float)rng.NextDouble();

                // Skip empty lots
                if (rand > buildingDensity + parkDensity) continue;

                Vector3 blockPosition = new Vector3(
                    x * totalSize + roadWidth + blockSize / 2f,
                    0,
                    z * totalSize + roadWidth + blockSize / 2f
                );

                // Calculate distance from center for building type selection
                float distFromCenter = Vector2.Distance(new Vector2(x, z), townCenter);

                GameObject building;

                if (rand > buildingDensity)
                {
                    // Create park
                    building = CreatePark(blockPosition);
                }
                else
                {
                    // Determine building type based on location
                    BuildingType type = DetermineBuildingType(distFromCenter);
                    building = CreateBuilding(blockPosition, type);
                }

                if (building != null)
                {
                    building.transform.parent = buildingParent.transform;
                    building.name = $"Block_{x}_{z}";
                }
            }
        }
    }

    BuildingType DetermineBuildingType(float distFromCenter)
    {
        float maxDist = Mathf.Sqrt(gridWidth * gridWidth + gridDepth * gridDepth) / 2f;
        float normalizedDist = distFromCenter / maxDist;

        if (normalizedDist < 0.3f)
        {
            return BuildingType.Office;
        }
        else if (normalizedDist < 0.6f)
        {
            return (float)rng.NextDouble() > 0.5f ? BuildingType.Shop : BuildingType.Apartment;
        }
        else
        {
            return BuildingType.House;
        }
    }

    GameObject CreateBuilding(Vector3 position, BuildingType type)
    {
        GameObject[] prefabs = GetPrefabsForType(type);

        if (prefabs != null && prefabs.Length > 0)
        {
            GameObject prefab = prefabs[rng.Next(0, prefabs.Length)];
            if (prefab != null)
            {
                return Instantiate(prefab, position, Quaternion.Euler(0, rng.Next(0, 4) * 90, 0));
            }
        }

        // Create procedural building if no prefab
        GameObject building = new GameObject($"Building_{type}");
        building.transform.position = position;

        float width = blockSize * Random.Range(0.4f, 0.7f);
        float depth = blockSize * Random.Range(0.4f, 0.7f);
        float height = GetHeightForType(type);

        // Main building body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.parent = building.transform;
        body.transform.localPosition = new Vector3(0, height / 2f, 0);
        body.transform.localScale = new Vector3(width, height, depth);
        body.name = "Body";

        Material mat = GetMaterialForType(type);
        if (mat != null)
        {
            body.GetComponent<Renderer>().material = mat;
        }
        else
        {
            body.GetComponent<Renderer>().material.color = GetColorForType(type);
        }

        // Add roof for houses and shops
        if (type == BuildingType.House || type == BuildingType.Shop)
        {
            CreateRoof(building, width, depth, height);
        }

        return building;
    }

    void CreateRoof(GameObject building, float width, float depth, float height)
    {
        GameObject roof = new GameObject("Roof");
        roof.transform.parent = building.transform;
        roof.transform.localPosition = new Vector3(0, height, 0);

        MeshFilter mf = roof.AddComponent<MeshFilter>();
        MeshRenderer mr = roof.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-width/2, 0, -depth/2),
            new Vector3(width/2, 0, -depth/2),
            new Vector3(width/2, 0, depth/2),
            new Vector3(-width/2, 0, depth/2),
            new Vector3(0, height * 0.3f, 0)
        };

        int[] triangles = new int[]
        {
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = new Color(0.4f, 0.25f, 0.15f);
    }

    GameObject CreatePark(Vector3 position)
    {
        if (parkPrefabs != null && parkPrefabs.Length > 0)
        {
            GameObject prefab = parkPrefabs[rng.Next(0, parkPrefabs.Length)];
            if (prefab != null)
            {
                return Instantiate(prefab, position, Quaternion.identity);
            }
        }

        GameObject park = GameObject.CreatePrimitive(PrimitiveType.Cube);
        park.transform.position = position;
        park.transform.localScale = new Vector3(blockSize * 0.8f, 0.1f, blockSize * 0.8f);
        park.name = "Park";

        if (parkMaterial != null)
        {
            park.GetComponent<Renderer>().material = parkMaterial;
        }
        else
        {
            park.GetComponent<Renderer>().material.color = new Color(0.2f, 0.5f, 0.1f);
        }

        // Add some trees
        for (int i = 0; i < 3; i++)
        {
            Vector3 treePos = position + new Vector3(
                Random.Range(-blockSize * 0.3f, blockSize * 0.3f),
                0,
                Random.Range(-blockSize * 0.3f, blockSize * 0.3f)
            );
            CreateTree(treePos, park.transform);
        }

        return park;
    }

    void CreateTree(Vector3 position, Transform parent)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.position = position;
        tree.transform.parent = parent;

        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.parent = tree.transform;
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
        trunk.GetComponent<Renderer>().material.color = new Color(0.4f, 0.25f, 0.1f);

        // Foliage
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.parent = tree.transform;
        foliage.transform.localPosition = new Vector3(0, 3.5f, 0);
        foliage.transform.localScale = new Vector3(2f, 2f, 2f);
        foliage.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 0.1f);
    }

    float GetHeightForType(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House:
                return Random.Range(3f, 5f);
            case BuildingType.Apartment:
                return Random.Range(8f, 15f);
            case BuildingType.Shop:
                return Random.Range(4f, 6f);
            case BuildingType.Office:
                return Random.Range(10f, 20f);
            default:
                return 5f;
        }
    }

    Color GetColorForType(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House:
                return new Color(0.7f, 0.5f, 0.3f);
            case BuildingType.Apartment:
                return new Color(0.6f, 0.6f, 0.5f);
            case BuildingType.Shop:
                return new Color(0.5f, 0.6f, 0.4f);
            case BuildingType.Office:
                return new Color(0.5f, 0.5f, 0.6f);
            default:
                return Color.gray;
        }
    }

    Material GetMaterialForType(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House:
                return houseMaterial;
            case BuildingType.Apartment:
                return apartmentMaterial;
            case BuildingType.Shop:
                return shopMaterial;
            case BuildingType.Office:
                return officeMaterial;
            default:
                return null;
        }
    }

    GameObject[] GetPrefabsForType(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House:
                return housePrefabs;
            case BuildingType.Apartment:
                return apartmentPrefabs;
            case BuildingType.Shop:
                return shopPrefabs;
            case BuildingType.Office:
                return officePrefabs;
            default:
                return null;
        }
    }

    enum BuildingType
    {
        House,
        Apartment,
        Shop,
        Office
    }
}