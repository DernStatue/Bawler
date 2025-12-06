using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class AdvancedRoadSystem : MonoBehaviour
{
    [Header("Road Type")]
    public RoadType roadType = RoadType.Standard;

    [Header("Road Path")]
    public List<Vector3> controlPoints = new List<Vector3>();

    [Header("Road Properties")]
    public float roadWidth = 8f;
    public int lanes = 2;
    public float laneWidth = 3.5f;
    public int segmentsPerUnit = 2;
    public float uvScale = 1f;

    [Header("Highway Settings")]
    public bool isHighway = false;
    public float shoulderWidth = 2f;
    public bool hasMedian = false;
    public float medianWidth = 4f;

    [Header("Bridge Settings")]
    public bool isBridge = false;
    public float bridgeHeight = 5f;
    public float pillarSpacing = 10f;
    public float pillarWidth = 1.5f;
    public float pillarHeight = 10f;

    [Header("Intersection Settings")]
    public IntersectionType intersectionType = IntersectionType.None;
    public float intersectionRadius = 10f;
    public int intersectionRoads = 4; // For standard intersections

    [Header("Roundabout Settings")]
    public bool isRoundabout = false;
    public float roundaboutRadius = 15f;
    public float roundaboutRoadWidth = 8f;
    public int roundaboutSegments = 32;
    public int roundaboutExits = 4;

    [Header("Materials")]
    public Material roadMaterial;
    public Material bridgeMaterial;
    public Material pillarMaterial;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private List<GameObject> childObjects = new List<GameObject>();

    public enum RoadType
    {
        Standard,
        Highway,
        Bridge,
        Intersection,
        Roundabout
    }

    public enum IntersectionType
    {
        None,
        TJunction,
        CrossIntersection,
        YJunction,
        Custom
    }

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        if (controlPoints.Count == 0)
        {
            InitializeDefaultPoints();
        }

        GenerateRoad();
    }

    void InitializeDefaultPoints()
    {
        switch (roadType)
        {
            case RoadType.Standard:
                controlPoints.Add(new Vector3(0, 0, 0));
                controlPoints.Add(new Vector3(20, 0, 0));
                controlPoints.Add(new Vector3(40, 0, 10));
                break;

            case RoadType.Roundabout:
                isRoundabout = true;
                break;

            case RoadType.Intersection:
                intersectionType = IntersectionType.CrossIntersection;
                break;
        }
    }

    public void GenerateRoad()
    {
        ClearChildObjects();

        switch (roadType)
        {
            case RoadType.Standard:
            case RoadType.Highway:
                GenerateStandardRoad();
                break;

            case RoadType.Bridge:
                GenerateBridge();
                break;

            case RoadType.Intersection:
                GenerateIntersection();
                break;

            case RoadType.Roundabout:
                GenerateRoundabout();
                break;
        }
    }

    void GenerateStandardRoad()
    {
        if (controlPoints.Count < 2) return;

        Mesh roadMesh = new Mesh();
        roadMesh.name = "Road Mesh";

        List<Vector3> pathPoints = GenerateSmoothPath();
        GenerateRoadMesh(roadMesh, pathPoints, roadWidth);

        meshFilter.sharedMesh = roadMesh;
        meshCollider.sharedMesh = roadMesh;

        if (isHighway)
        {
            GenerateHighwayFeatures(pathPoints);
        }

        if (isBridge)
        {
            GenerateBridgePillars(pathPoints);
        }
    }

    void GenerateBridge()
    {
        isBridge = true;
        GenerateStandardRoad();

        // Add bridge railings
        List<Vector3> pathPoints = GenerateSmoothPath();
        GenerateBridgeRailings(pathPoints);
    }

    void GenerateIntersection()
    {
        Mesh intersectionMesh = new Mesh();

        switch (intersectionType)
        {
            case IntersectionType.TJunction:
                GenerateTJunction(intersectionMesh);
                break;

            case IntersectionType.CrossIntersection:
                GenerateCrossIntersection(intersectionMesh);
                break;

            case IntersectionType.YJunction:
                GenerateYJunction(intersectionMesh);
                break;
        }

        meshFilter.sharedMesh = intersectionMesh;
        meshCollider.sharedMesh = intersectionMesh;
    }

    void GenerateRoundabout()
    {
        Mesh roundaboutMesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Generate circular road
        float angleStep = 360f / roundaboutSegments;

        for (int i = 0; i <= roundaboutSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;

            // Inner circle
            Vector3 innerPoint = new Vector3(
                Mathf.Cos(angle) * (roundaboutRadius - roundaboutRoadWidth * 0.5f),
                0,
                Mathf.Sin(angle) * (roundaboutRadius - roundaboutRoadWidth * 0.5f)
            );

            // Outer circle
            Vector3 outerPoint = new Vector3(
                Mathf.Cos(angle) * (roundaboutRadius + roundaboutRoadWidth * 0.5f),
                0,
                Mathf.Sin(angle) * (roundaboutRadius + roundaboutRoadWidth * 0.5f)
            );

            vertices.Add(innerPoint);
            vertices.Add(outerPoint);

            float uvY = i / (float)roundaboutSegments;
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));

            if (i < roundaboutSegments)
            {
                int baseIndex = i * 2;

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }
        }

        roundaboutMesh.vertices = vertices.ToArray();
        roundaboutMesh.triangles = triangles.ToArray();
        roundaboutMesh.uv = uvs.ToArray();
        roundaboutMesh.RecalculateNormals();

        meshFilter.sharedMesh = roundaboutMesh;
        meshCollider.sharedMesh = roundaboutMesh;

        // Generate exit roads
        GenerateRoundaboutExits();

        // Center island
        GenerateCenterIsland();
    }

    void GenerateCrossIntersection(Mesh mesh)
    {
        float halfWidth = roadWidth * 0.5f;
        float size = intersectionRadius;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Create square intersection
        vertices.Add(new Vector3(-size, 0, -size));
        vertices.Add(new Vector3(size, 0, -size));
        vertices.Add(new Vector3(size, 0, size));
        vertices.Add(new Vector3(-size, 0, size));

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    void GenerateTJunction(Mesh mesh)
    {
        float halfWidth = roadWidth * 0.5f;
        float size = intersectionRadius;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // T-shaped intersection
        vertices.Add(new Vector3(-size, 0, -halfWidth));
        vertices.Add(new Vector3(size, 0, -halfWidth));
        vertices.Add(new Vector3(size, 0, halfWidth));
        vertices.Add(new Vector3(halfWidth, 0, halfWidth));
        vertices.Add(new Vector3(halfWidth, 0, size));
        vertices.Add(new Vector3(-halfWidth, 0, size));
        vertices.Add(new Vector3(-halfWidth, 0, halfWidth));
        vertices.Add(new Vector3(-size, 0, halfWidth));

        for (int i = 0; i < 8; i++)
        {
            uvs.Add(new Vector2(i / 8f, 0));
        }

        // Triangulate the T-shape
        int[] tris = { 0, 1, 2, 0, 2, 7, 7, 2, 6, 2, 3, 6, 6, 3, 5, 3, 4, 5 };
        triangles.AddRange(tris);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    void GenerateYJunction(Mesh mesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfWidth = roadWidth * 0.5f;
        int segments = 16;

        // Create Y-junction using three roads meeting at 120 degrees
        for (int road = 0; road < 3; road++)
        {
            float angle = road * 120f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 center = direction * intersectionRadius * t;

                vertices.Add(center - perpendicular * halfWidth);
                vertices.Add(center + perpendicular * halfWidth);

                uvs.Add(new Vector2(0, t));
                uvs.Add(new Vector2(1, t));
            }
        }

        // Simple triangulation
        for (int road = 0; road < 3; road++)
        {
            int baseIdx = road * (segments + 1) * 2;
            for (int i = 0; i < segments; i++)
            {
                int idx = baseIdx + i * 2;

                triangles.Add(idx);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);

                triangles.Add(idx + 1);
                triangles.Add(idx + 3);
                triangles.Add(idx + 2);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    void GenerateBridgePillars(List<Vector3> pathPoints)
    {
        if (pathPoints.Count < 2) return;

        float distance = 0f;
        float nextPillarDistance = pillarSpacing;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);

            while (distance + segmentLength >= nextPillarDistance)
            {
                float t = (nextPillarDistance - distance) / segmentLength;
                Vector3 pillarPos = Vector3.Lerp(pathPoints[i - 1], pathPoints[i], t);

                CreatePillar(pillarPos);

                nextPillarDistance += pillarSpacing;
            }

            distance += segmentLength;
        }
    }

    void CreatePillar(Vector3 position)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.transform.parent = transform;
        pillar.transform.position = position - new Vector3(0, pillarHeight * 0.5f, 0);
        pillar.transform.localScale = new Vector3(pillarWidth, pillarHeight, pillarWidth);

        if (pillarMaterial != null)
        {
            pillar.GetComponent<MeshRenderer>().material = pillarMaterial;
        }

        childObjects.Add(pillar);
    }

    void GenerateBridgeRailings(List<Vector3> pathPoints)
    {
        // Create railings on both sides
        for (int side = 0; side < 2; side++)
        {
            float offset = (side == 0 ? -1 : 1) * roadWidth * 0.5f;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Vector3 p1 = pathPoints[i];
                Vector3 p2 = pathPoints[i + 1];
                Vector3 forward = (p2 - p1).normalized;
                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

                Vector3 railingPos = p1 + right * offset;
                Vector3 railingEnd = p2 + right * offset;

                GameObject railing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railing.transform.parent = transform;
                railing.transform.position = (railingPos + railingEnd) * 0.5f + Vector3.up * 1f;
                railing.transform.rotation = Quaternion.LookRotation(forward);
                railing.transform.localScale = new Vector3(0.2f, 1f, Vector3.Distance(railingPos, railingEnd));

                childObjects.Add(railing);
            }
        }
    }

    void GenerateHighwayFeatures(List<Vector3> pathPoints)
    {
        if (hasMedian)
        {
            // Create median barrier
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Vector3 p1 = pathPoints[i];
                Vector3 p2 = pathPoints[i + 1];

                GameObject median = GameObject.CreatePrimitive(PrimitiveType.Cube);
                median.transform.parent = transform;
                median.transform.position = (p1 + p2) * 0.5f + Vector3.up * 0.5f;
                median.transform.rotation = Quaternion.LookRotation(p2 - p1);
                median.transform.localScale = new Vector3(0.5f, 1f, Vector3.Distance(p1, p2));

                childObjects.Add(median);
            }
        }
    }

    void GenerateRoundaboutExits()
    {
        float angleStep = 360f / roundaboutExits;

        for (int i = 0; i < roundaboutExits; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 startPos = direction * (roundaboutRadius + roundaboutRoadWidth * 0.5f);
            Vector3 endPos = startPos + direction * 15f;

            // Create exit road mesh directly
            GameObject exitRoad = new GameObject("Exit Road " + i);
            exitRoad.transform.parent = transform;
            exitRoad.transform.position = transform.position;

            MeshFilter mf = exitRoad.AddComponent<MeshFilter>();
            MeshRenderer mr = exitRoad.AddComponent<MeshRenderer>();
            MeshCollider mc = exitRoad.AddComponent<MeshCollider>();

            if (roadMaterial != null)
            {
                mr.material = roadMaterial;
            }

            Mesh exitMesh = new Mesh();
            List<Vector3> exitPath = new List<Vector3> { startPos, endPos };
            GenerateRoadMesh(exitMesh, exitPath, roadWidth);

            mf.sharedMesh = exitMesh;
            mc.sharedMesh = exitMesh;

            childObjects.Add(exitRoad);
        }
    }

    void GenerateCenterIsland()
    {
        GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        island.transform.parent = transform;
        island.transform.position = transform.position + Vector3.up * 0.05f;
        island.transform.localScale = new Vector3(
            (roundaboutRadius - roundaboutRoadWidth * 0.5f) * 2f,
            0.1f,
            (roundaboutRadius - roundaboutRoadWidth * 0.5f) * 2f
        );

        childObjects.Add(island);
    }

    List<Vector3> GenerateSmoothPath()
    {
        List<Vector3> smoothPath = new List<Vector3>();

        float totalLength = 0f;
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(controlPoints[i], controlPoints[i + 1]);
        }

        int totalSegments = Mathf.Max(2, (int)(totalLength * segmentsPerUnit));

        for (int i = 0; i <= totalSegments; i++)
        {
            float t = i / (float)totalSegments;
            Vector3 point = GetCatmullRomPoint(t);
            smoothPath.Add(point);
        }

        return smoothPath;
    }

    Vector3 GetCatmullRomPoint(float t)
    {
        int numPoints = controlPoints.Count;
        float scaledT = t * (numPoints - 1);
        int p1Index = Mathf.FloorToInt(scaledT);

        if (p1Index >= numPoints - 1)
        {
            return controlPoints[numPoints - 1];
        }

        float localT = scaledT - p1Index;

        Vector3 p0 = controlPoints[Mathf.Max(0, p1Index - 1)];
        Vector3 p1 = controlPoints[p1Index];
        Vector3 p2 = controlPoints[Mathf.Min(numPoints - 1, p1Index + 1)];
        Vector3 p3 = controlPoints[Mathf.Min(numPoints - 1, p1Index + 2)];

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void GenerateRoadMesh(Mesh mesh, List<Vector3> pathPoints, float width)
    {
        int pointCount = pathPoints.Count;
        Vector3[] vertices = new Vector3[pointCount * 2];
        Vector2[] uvs = new Vector2[pointCount * 2];
        int[] triangles = new int[(pointCount - 1) * 6];

        float distanceAlongPath = 0f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 point = pathPoints[i];
            Vector3 forward;

            if (i < pointCount - 1)
            {
                forward = (pathPoints[i + 1] - point).normalized;
            }
            else
            {
                forward = (point - pathPoints[i - 1]).normalized;
            }

            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

            vertices[i * 2] = point - right * width * 0.5f;
            vertices[i * 2 + 1] = point + right * width * 0.5f;

            uvs[i * 2] = new Vector2(0, distanceAlongPath * uvScale);
            uvs[i * 2 + 1] = new Vector2(1, distanceAlongPath * uvScale);

            if (i > 0)
            {
                distanceAlongPath += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
            }
        }

        int triIndex = 0;
        for (int i = 0; i < pointCount - 1; i++)
        {
            int vertIndex = i * 2;

            triangles[triIndex++] = vertIndex;
            triangles[triIndex++] = vertIndex + 1;
            triangles[triIndex++] = vertIndex + 2;

            triangles[triIndex++] = vertIndex + 1;
            triangles[triIndex++] = vertIndex + 3;
            triangles[triIndex++] = vertIndex + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void ClearChildObjects()
    {
        foreach (GameObject obj in childObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        childObjects.Clear();
    }

    void OnDrawGizmos()
    {
        if (controlPoints == null || controlPoints.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(controlPoints[i]);
            Gizmos.DrawSphere(worldPos, 0.5f);

            if (i < controlPoints.Count - 1)
            {
                Vector3 nextWorldPos = transform.TransformPoint(controlPoints[i + 1]);
                Gizmos.DrawLine(worldPos, nextWorldPos);
            }
        }
    }
}