using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralRoad : MonoBehaviour
{
    [Header("Road Path")]
    [SerializeField] private List<Vector3> controlPoints = new List<Vector3>();

    [Header("Road Properties")]
    [SerializeField] private float roadWidth = 4f;
    [SerializeField] private int segmentsPerUnit = 2;
    [SerializeField] private float uvScale = 1f;

    [Header("Spline Settings")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float tension = 0.5f;

    [Header("Collision")]
    [SerializeField] private bool generateCollider = true;
    [SerializeField] private bool convexCollider = false;
    [SerializeField] private PhysicsMaterial physicMaterial;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh roadMesh;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Ensure MeshRenderer exists and is configured
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Create a default material if none exists
        if (meshRenderer.sharedMaterial == null)
        {
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial.color = Color.gray;
            Debug.Log("Created default material. Assign a road texture material for better results.");
        }

        // Initialize with some default points if empty
        if (controlPoints.Count == 0)
        {
            controlPoints.Add(new Vector3(0, 0, 0));
            controlPoints.Add(new Vector3(10, 0, 0));
            controlPoints.Add(new Vector3(20, 0, 5));
            controlPoints.Add(new Vector3(30, 0, 5));
        }

        GenerateRoad();
    }

    public void GenerateRoad()
    {
        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("Need at least 2 control points to generate a road");
            return;
        }

        // Ensure we have a mesh filter
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        roadMesh = new Mesh();
        roadMesh.name = "Procedural Road";

        List<Vector3> pathPoints = useSmoothing ?
            GenerateSmoothPath() : new List<Vector3>(controlPoints);

        GenerateRoadMesh(pathPoints);

        // Assign the mesh to the filter
        meshFilter.sharedMesh = roadMesh;
    }

    private List<Vector3> GenerateSmoothPath()
    {
        List<Vector3> smoothPath = new List<Vector3>();

        // Calculate total path length
        float totalLength = 0f;
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(controlPoints[i], controlPoints[i + 1]);
        }

        int totalSegments = Mathf.Max(2, (int)(totalLength * segmentsPerUnit));

        // Generate smooth spline using Catmull-Rom
        for (int i = 0; i <= totalSegments; i++)
        {
            float t = i / (float)totalSegments;
            Vector3 point = GetCatmullRomPoint(t);
            smoothPath.Add(point);
        }

        return smoothPath;
    }

    private Vector3 GetCatmullRomPoint(float t)
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

        return CatmullRom(p0, p1, p2, p3, localT, tension);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha)
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

    private void GenerateRoadMesh(List<Vector3> pathPoints)
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

            // Calculate forward direction
            if (i < pointCount - 1)
            {
                forward = (pathPoints[i + 1] - point).normalized;
            }
            else
            {
                forward = (point - pathPoints[i - 1]).normalized;
            }

            // Calculate right vector (perpendicular to forward)
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

            // Create left and right vertices
            vertices[i * 2] = point - right * roadWidth * 0.5f;
            vertices[i * 2 + 1] = point + right * roadWidth * 0.5f;

            // UV mapping
            uvs[i * 2] = new Vector2(0, distanceAlongPath * uvScale);
            uvs[i * 2 + 1] = new Vector2(1, distanceAlongPath * uvScale);

            if (i > 0)
            {
                distanceAlongPath += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
            }
        }

        // Generate triangles (winding order matters for visibility!)
        int triIndex = 0;
        for (int i = 0; i < pointCount - 1; i++)
        {
            int vertIndex = i * 2;

            // First triangle (counter-clockwise winding)
            triangles[triIndex++] = vertIndex;
            triangles[triIndex++] = vertIndex + 1;
            triangles[triIndex++] = vertIndex + 2;

            // Second triangle (counter-clockwise winding)
            triangles[triIndex++] = vertIndex + 1;
            triangles[triIndex++] = vertIndex + 3;
            triangles[triIndex++] = vertIndex + 2;
        }

        roadMesh.vertices = vertices;
        roadMesh.triangles = triangles;
        roadMesh.uv = uvs;
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();

        // Update collider if enabled
        if (generateCollider && meshCollider != null)
        {
            meshCollider.sharedMesh = null; // Clear first to avoid issues
            meshCollider.sharedMesh = roadMesh;
            meshCollider.convex = convexCollider;

            if (physicMaterial != null)
            {
                meshCollider.sharedMaterial = physicMaterial;
            }
        }
    }

    // Public methods to modify the road at runtime
    public void AddControlPoint(Vector3 point)
    {
        controlPoints.Add(point);
        GenerateRoad();
    }

    public void SetControlPoint(int index, Vector3 position)
    {
        if (index >= 0 && index < controlPoints.Count)
        {
            controlPoints[index] = position;
            GenerateRoad();
        }
    }

    public void RemoveControlPoint(int index)
    {
        if (index >= 0 && index < controlPoints.Count && controlPoints.Count > 2)
        {
            controlPoints.RemoveAt(index);
            GenerateRoad();
        }
    }

    // Visualize control points in editor
    void OnDrawGizmos()
    {
        if (controlPoints == null || controlPoints.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(controlPoints[i]);
            Gizmos.DrawSphere(worldPos, 0.3f);

            if (i < controlPoints.Count - 1)
            {
                Vector3 nextWorldPos = transform.TransformPoint(controlPoints[i + 1]);
                Gizmos.DrawLine(worldPos, nextWorldPos);
            }
        }
    }
}