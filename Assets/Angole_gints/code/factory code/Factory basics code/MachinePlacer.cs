using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ===== MACHINE PLACER SCRIPT =====
public class MachinePlacer : MonoBehaviour
{
    [System.Serializable]
    public class BuildingData
    {
        public string name;
        public GameObject prefab;
        public int cost;
        public Sprite icon;
        [TextArea] public string description;
    }

    [Header("Buildings")]
    public BuildingData[] buildings;

    [Header("Placement Settings")]
    public LayerMask groundLayer;
    public float gridSize = 1f;
    public bool snapToGrid = true;
    public float placementHeight = 0.5f;

    [Header("Preview Materials")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Rotation")]
    public float rotationStep = 90f;

    [Header("Resources")]
    public int playerMoney = 1000;

    private GameObject previewObject;
    private int selectedBuildingIndex = -1;
    private float currentRotation = 0f;
    private bool canPlace = false;
    private bool isPlacementMode = false;

    // Reference to UI
    private FactoryUI factoryUI;

    void Start()
    {
        factoryUI = FindObjectOfType<FactoryUI>();

        if (factoryUI != null)
        {
            factoryUI.InitializeUI(this);
        }

        // Create materials if not assigned
        if (validPlacementMaterial == null)
        {
            validPlacementMaterial = new Material(Shader.Find("Standard"));
            validPlacementMaterial.color = new Color(0, 1, 0, 0.5f);
        }

        if (invalidPlacementMaterial == null)
        {
            invalidPlacementMaterial = new Material(Shader.Find("Standard"));
            invalidPlacementMaterial.color = new Color(1, 0, 0, 0.5f);
        }
    }

    void Update()
    {
        if (isPlacementMode)
        {
            HandleRotation();
            UpdatePreview();
            HandlePlacement();

            // Cancel with ESC or right click
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    public void SelectBuilding(int index)
    {
        if (index < 0 || index >= buildings.Length) return;

        selectedBuildingIndex = index;
        currentRotation = 0f;
        isPlacementMode = true;

        // Destroy old preview
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        // Create new preview
        if (buildings[selectedBuildingIndex].prefab != null)
        {
            previewObject = Instantiate(buildings[selectedBuildingIndex].prefab);

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

            SetPreviewMaterials(previewObject, true);

            Debug.Log($"Selected: {buildings[selectedBuildingIndex].name}");
        }
    }

    void SetPreviewMaterials(GameObject obj, bool valid)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Material mat = valid ? validPlacementMaterial : invalidPlacementMaterial;

        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
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

            // Update preview material
            SetPreviewMaterials(previewObject, canPlace);

            previewObject.SetActive(true);
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    bool CheckPlacementValid(Vector3 position)
    {
        // Check if player has enough money
        if (playerMoney < buildings[selectedBuildingIndex].cost)
        {
            return false;
        }

        // Check if there's already a building here
        Collider[] colliders = Physics.OverlapSphere(position, gridSize * 0.4f);

        foreach (Collider col in colliders)
        {
            if (col.gameObject == previewObject)
                continue;

            if (col.gameObject.layer != LayerMask.NameToLayer("Ground"))
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
    }

    void PlaceBuilding()
    {
        if (previewObject == null) return;

        BuildingData building = buildings[selectedBuildingIndex];

        // Deduct cost
        playerMoney -= building.cost;

        Vector3 position = previewObject.transform.position;
        Quaternion rotation = previewObject.transform.rotation;

        // Instantiate the actual building
        GameObject newBuilding = Instantiate(building.prefab, position, rotation);

        Debug.Log($"Placed: {newBuilding.name} at {position}. Money: {playerMoney}");

        // Update UI
        if (factoryUI != null)
        {
            factoryUI.UpdateMoneyDisplay();
        }

        // Reset preview rotation
        currentRotation = 0f;
    }

    public void CancelPlacement()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        isPlacementMode = false;
        selectedBuildingIndex = -1;

        if (factoryUI != null)
        {
            factoryUI.DeselectAllButtons();
        }
    }

    void OnDestroy()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }
}