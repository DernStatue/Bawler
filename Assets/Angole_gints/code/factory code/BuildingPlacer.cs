using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    public GameObject minerPrefab;
    public GameObject smelterPrefab;
    public GameObject forgePrefab;
    public GameObject factoryPrefab;
    public GameObject sellerPrefab;

    public int minerCost = 20;
    public int smelterCost = 30;
    public int forgeCost = 50;
    public int factoryCost = 80;
    public int sellerCost = 40;

    private GameObject selectedPrefab;
    private int selectedCost;

    public LayerMask groundLayer;

    void Update()
    {
        if (selectedPrefab != null && Input.GetMouseButtonDown(0))
        {
            PlaceBuilding();
        }
    }

    public void SelectMiner() { SelectBuilding(minerPrefab, minerCost); }
    public void SelectSmelter() { SelectBuilding(smelterPrefab, smelterCost); }
    public void SelectForge() { SelectBuilding(forgePrefab, forgeCost); }
    public void SelectFactory() { SelectBuilding(factoryPrefab, factoryCost); }
    public void SelectSeller() { SelectBuilding(sellerPrefab, sellerCost); }

    void SelectBuilding(GameObject prefab, int cost)
    {
        selectedPrefab = prefab;
        selectedCost = cost;
    }

    void PlaceBuilding()
    {
        if (!ResourceManager.Instance.HasResource("money", selectedCost))
        {
            Debug.Log("Not enough money!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 spawnPos = hit.point;
            spawnPos.y = 0.5f; // Adjust height as needed

            Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            ResourceManager.Instance.RemoveResource("money", selectedCost);
        }
    }
}