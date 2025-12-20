using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    [Header("Building Prefabs")]
    public GameObject[] buildingPrefabs;

    [Header("Placement Settings")]
    public LayerMask groundLayer;
    public float gridSize = 1f;
    public bool snapToGrid = true;
    public float placementHeight = 0.5f;

    [Header("Preview Settings")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
    public Color validColor = new Color(0, 1, 0, 0.5f);
    public Color invalidColor = new Color(1, 0, 0, 0.5f);

    [Header("Rotation")]
    public float rotationStep = 90f;

    private GameObject previewObject;
    private int selectedBuildingIndex = 0;
    private float currentRotation = 0f;
    private bool canPlace = false;
    private Material previewMaterial;

    void Start()
    {
        // Create preview materials if not assigned
        if (validPlacementMaterial == null)
        {
            validPlacementMaterial = new Material(Shader.Find("Standard"));
            validPlacementMaterial.color = validColor;
        }

        if (invalidPlacementMaterial == null)
        {
            invalidPlacementMaterial = new Material(Shader.Find("Standard"));
            invalidPlacementMaterial.color = invalidColor;
        }

        if (buildingPrefabs.Length > 0)
        {
            SelectBuilding(0);
        }
    }

    void Update()
    {
        HandleBuildingSelection();
        HandleRotation();
        UpdatePreview();
        HandlePlacement();
    }

    void HandleBuildingSelection()
    {
        // Number keys 1-9 to select buildings
        for (int i = 0; i < buildingPrefabs.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectBuilding(i);
            }
        }

        // Mouse scroll to cycle through buildings
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SelectBuilding((selectedBuildingIndex + 1) % buildingPrefabs.Length);
        }
        else if (scroll < 0f)
        {
            SelectBuilding((selectedBuildingIndex - 1 + buildingPrefabs.Length) % buildingPrefabs.Length);
        }
    }

    void SelectBuilding(int index)
    {
        if (index < 0 || index >= buildingPrefabs.Length) return;

        selectedBuildingIndex = index;
        currentRotation = 0f;

        // Destroy old preview
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        // Create new preview
        if (buildingPrefabs[selectedBuildingIndex] != null)
        {
            previewObject = Instantiate(buildingPrefabs[selectedBuildingIndex]);

            // Disable scripts on preview
            MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = false;
            }

            // Disable colliders on preview
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Set up preview materials
            SetPreviewMaterials(previewObject);

            Debug.Log($"Selected: {buildingPrefabs[selectedBuildingIndex].name}");
        }
    }

    void SetPreviewMaterials(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = validPlacementMaterial;
            }
            rend.materials = mats;
        }
    }

    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation += rotationStep;
            if (currentRotation >= 360f)
                currentRotation -= 360f;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            currentRotation -= rotationStep;
            if (currentRotation < 0f)
                currentRotation += 360f;
        }
    }

    void UpdatePreview()
    {
        if (previewObject == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = placementHeight;

            // Snap to grid
            if (snapToGrid)
            {
                targetPosition.x = Mathf.Round(targetPosition.x / gridSize) * gridSize;
                targetPosition.z = Mathf.Round(targetPosition.z / gridSize) * gridSize;
            }

            previewObject.transform.position = targetPosition;
            previewObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            // Check if position is valid
            canPlace = CheckPlacementValid(targetPosition);

            // Update preview color
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            Material mat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
            foreach (Renderer rend in renderers)
            {
                Material[] mats = new Material[rend.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }
                rend.materials = mats;
            }

            previewObject.SetActive(true);
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    bool CheckPlacementValid(Vector3 position)
    {
        // Check if there's already a building here
        Collider[] colliders = Physics.OverlapSphere(position, gridSize * 0.4f);

        foreach (Collider col in colliders)
        {
            // Ignore the preview object and ground
            if (col.gameObject == previewObject || col.gameObject.layer == LayerMask.NameToLayer("Ground"))
                continue;

            // If we found another object, placement is invalid
            if (col.gameObject != previewObject.gameObject)
            {
                return false;
            }
        }

        return true;
    }

    void HandlePlacement()
    {
        if (Input.GetMouseButtonDown(0) && canPlace && previewObject.activeSelf)
        {
            PlaceBuilding();
        }

        // Cancel placement
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }
    }

    void PlaceBuilding()
    {
        if (previewObject == null) return;

        Vector3 position = previewObject.transform.position;
        Quaternion rotation = previewObject.transform.rotation;

        // Instantiate the actual building
        GameObject newBuilding = Instantiate(buildingPrefabs[selectedBuildingIndex], position, rotation);

        Debug.Log($"Placed: {newBuilding.name} at {position}");

        // Reset preview rotation
        currentRotation = 0f;
    }

    void OnGUI()
    {
        // Display controls
        GUI.Box(new Rect(10, 10, 300, 150), "Building Placer");

        if (buildingPrefabs.Length > 0)
        {
            GUI.Label(new Rect(20, 35, 280, 20), $"Selected: {buildingPrefabs[selectedBuildingIndex].name}");
            GUI.Label(new Rect(20, 55, 280, 20), $"Rotation: {currentRotation}°");
        }

        GUI.Label(new Rect(20, 80, 280, 20), "1-9: Select building");
        GUI.Label(new Rect(20, 95, 280, 20), "Mouse Wheel: Cycle buildings");
        GUI.Label(new Rect(20, 110, 280, 20), "R/T: Rotate building");
        GUI.Label(new Rect(20, 125, 280, 20), "Left Click: Place");
        GUI.Label(new Rect(20, 140, 280, 20), "ESC: Cancel");
    }

    void OnDestroy()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }
}