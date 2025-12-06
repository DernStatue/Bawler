using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
    [Header("References")]
    public Transform viewer;
    public GameObject terrainPrefab;

    [Header("Settings")]
    public int viewDistance = 2;
    public float terrainSize = 100f;

    [Header("UI (Optional)")]
    public Text coordinateText;

    private Dictionary<Vector2, GameObject> terrainChunks = new Dictionary<Vector2, GameObject>();
    private Vector2 viewerPosition;
    private Vector2 oldViewerPosition;
    private Vector2 currentChunkCoord;

    void Start()
    {
        if (viewer == null)
        {
            viewer = Camera.main.transform;
        }

        oldViewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // Calculate and update current chunk coordinates
        currentChunkCoord = new Vector2(
            Mathf.RoundToInt(viewerPosition.x / terrainSize),
            Mathf.RoundToInt(viewerPosition.y / terrainSize)
        );

        // Update UI if text component is assigned
        if (coordinateText != null)
        {
            coordinateText.text = $"Chunk: ({currentChunkCoord.x}, {currentChunkCoord.y})\nPosition: ({viewer.position.x:F1}, {viewer.position.z:F1})";
        }

        // Only update when viewer has moved at least one chunk
        if ((oldViewerPosition - viewerPosition).sqrMagnitude > terrainSize * terrainSize / 4f)
        {
            oldViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        // Calculate current chunk coordinates
        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / terrainSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / terrainSize);

        // Track which chunks should exist
        HashSet<Vector2> chunksToKeep = new HashSet<Vector2>();

        // Create/update chunks in view distance
        for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
        {
            for (int yOffset = -viewDistance; yOffset <= viewDistance; yOffset++)
            {
                Vector2 chunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);
                chunksToKeep.Add(chunkCoord);

                if (!terrainChunks.ContainsKey(chunkCoord))
                {
                    CreateTerrainChunk(chunkCoord);
                }
            }
        }

        // Remove chunks that are too far away
        List<Vector2> chunksToRemove = new List<Vector2>();
        foreach (var chunk in terrainChunks)
        {
            if (!chunksToKeep.Contains(chunk.Key))
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            Destroy(terrainChunks[chunkCoord]);
            terrainChunks.Remove(chunkCoord);
        }
    }

    void CreateTerrainChunk(Vector2 coord)
    {
        // Calculate world position
        Vector3 position = new Vector3(coord.x * terrainSize, 0, coord.y * terrainSize);

        // Instantiate terrain chunk
        GameObject chunk = Instantiate(terrainPrefab, position, Quaternion.identity, transform);
        chunk.name = $"Terrain_{coord.x}_{coord.y}";

        // Set up procedural terrain with unique offset
        ProceduralTerrain procTerrain = chunk.GetComponent<ProceduralTerrain>();
        if (procTerrain != null)
        {
            // Use chunk coordinates to create seamless noise offset
            procTerrain.SetOffset(new Vector2(coord.x * procTerrain.noiseScale * 100f, coord.y * procTerrain.noiseScale * 100f));
            procTerrain.GenerateTerrain();
        }

        terrainChunks.Add(coord, chunk);
    }

    // Public method to get current chunk coordinates (useful for other scripts)
    public Vector2 GetCurrentChunkCoord()
    {
        return currentChunkCoord;
    }

    // Public method to get total loaded chunks count
    public int GetLoadedChunksCount()
    {
        return terrainChunks.Count;
    }
}