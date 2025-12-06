using UnityEngine;

public enum MachineType
{
    Miner,
    Smelter,
    Forge,
    Factory,
    Seller
}

public class Machine : MonoBehaviour
{
    public MachineType machineType;
    public float productionRate = 1f;
    private float productionTimer = 0f;

    public string inputResource;
    public string outputResource;
    public int inputAmount = 1;
    public int outputAmount = 1;

    void Start()
    {
        ConfigureMachine();
    }

    void ConfigureMachine()
    {
        switch (machineType)
        {
            case MachineType.Miner:
                outputResource = "ore";
                break;
            case MachineType.Smelter:
                inputResource = "ore";
                outputResource = "iron";
                break;
            case MachineType.Forge:
                inputResource = "iron";
                outputResource = "steel";
                break;
            case MachineType.Factory:
                inputResource = "steel";
                outputResource = "gear";
                break;
            case MachineType.Seller:
                inputResource = "gear";
                outputResource = "money";
                outputAmount = 10;
                break;
        }
    }

    void Update()
    {
        if (!GameManager.Instance.isRunning) return;

        productionTimer += Time.deltaTime;

        if (productionTimer >= productionRate)
        {
            Produce();
            productionTimer = 0f;
        }
    }

    void Produce()
    {
        // Check if we need input resource
        if (!string.IsNullOrEmpty(inputResource))
        {
            if (!ResourceManager.Instance.HasResource(inputResource, inputAmount))
                return;

            ResourceManager.Instance.RemoveResource(inputResource, inputAmount);
        }

        // Add output resource
        if (!string.IsNullOrEmpty(outputResource))
        {
            ResourceManager.Instance.AddResource(outputResource, outputAmount);
        }
    }
}