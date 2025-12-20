using UnityEngine;
using System.Collections.Generic;

public class ProcessingMachine : MonoBehaviour
{
    [System.Serializable]
    public class RecipeMapping
    {
        [Tooltip("Input item tag (e.g., 'Ore')")]
        public string inputTag;

        [Tooltip("What this input becomes")]
        public GameObject outputPrefab;

        [Tooltip("Processing time for this recipe (seconds)")]
        public float processingTime = 2f;
    }

    [Header("Input/Output Points")]
    [Tooltip("Where items enter the machine")]
    public Transform inputPoint;

    [Tooltip("Where items exit the machine")]
    public Transform outputPoint;

    [Header("Recipe System")]
    [Tooltip("Define what inputs become what outputs")]
    public RecipeMapping[] recipes;

    [Header("Visual Feedback")]
    public bool showProcessingEffect = true;
    public Color processingColor = Color.cyan;
    public Color idleColor = Color.gray;

    [Header("Item Storage")]
    [Tooltip("Maximum items the machine can hold")]
    public int maxCapacity = 3;

    [Header("Output Settings")]
    [Tooltip("Speed items are pushed out")]
    public float outputSpeed = 2f;

    private Queue<ProcessingItem> itemQueue = new Queue<ProcessingItem>();
    private bool isProcessing = false;
    private float processingTimer = 0f;
    private Material material;
    private Renderer machineRenderer;
    private RecipeMapping currentRecipe;

    private class ProcessingItem
    {
        public GameObject item;
        public RecipeMapping recipe;
    }

    void Start()
    {
        machineRenderer = GetComponent<Renderer>();
        if (machineRenderer != null)
        {
            material = machineRenderer.material;
            material.color = idleColor;
        }

        // Create input/output points if they don't exist
        if (inputPoint == null)
        {
            GameObject input = new GameObject("InputPoint");
            input.transform.parent = transform;
            input.transform.localPosition = new Vector3(0, 0, -1);
            inputPoint = input.transform;
        }

        if (outputPoint == null)
        {
            GameObject output = new GameObject("OutputPoint");
            output.transform.parent = transform;
            output.transform.localPosition = new Vector3(0, 0, 1);
            outputPoint = output.transform;
        }
    }

    void Update()
    {
        if (isProcessing)
        {
            ProcessItem();
        }
        else if (itemQueue.Count > 0)
        {
            StartProcessing();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name} with tag: {other.tag}");

        // Check if we're at capacity
        if (itemQueue.Count >= maxCapacity)
        {
            Debug.LogWarning($"Machine full! Capacity: {maxCapacity}");
            return;
        }

        // Find matching recipe for this item
        RecipeMapping matchedRecipe = FindRecipeForTag(other.tag);

        if (matchedRecipe == null)
        {
            Debug.LogWarning($"No recipe found for tag: {other.tag}");
            return;
        }

        // Check if item has rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"Item {other.name} has no Rigidbody!");
            return;
        }

        // Accept the item with its recipe
        AcceptItem(other.gameObject, matchedRecipe);
    }

    RecipeMapping FindRecipeForTag(string tag)
    {
        foreach (RecipeMapping recipe in recipes)
        {
            if (recipe.inputTag == tag)
            {
                return recipe;
            }
        }
        return null;
    }

    void AcceptItem(GameObject item, RecipeMapping recipe)
    {
        Debug.Log($"Machine accepted: {item.name} (Tag: {item.tag}) -> Will output: {recipe.outputPrefab.name}");

        // Create processing item with recipe
        ProcessingItem procItem = new ProcessingItem
        {
            item = item,
            recipe = recipe
        };

        // Add to queue
        itemQueue.Enqueue(procItem);

        // Move item to input point and disable physics
        item.transform.position = inputPoint.position;
        item.transform.rotation = Quaternion.identity;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Hide the item while processing
        item.SetActive(false);
    }

    void StartProcessing()
    {
        isProcessing = true;
        processingTimer = 0f;

        // Get the current item being processed
        ProcessingItem currentItem = itemQueue.Peek();
        currentRecipe = currentItem.recipe;

        if (showProcessingEffect && material != null)
        {
            material.color = processingColor;
        }

        Debug.Log($"Started processing... Output will be: {currentRecipe.outputPrefab.name}");
    }

    void ProcessItem()
    {
        processingTimer += Time.deltaTime;

        // Visual feedback - pulse effect
        if (showProcessingEffect && material != null)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            material.color = Color.Lerp(idleColor, processingColor, pulse);
        }

        // Check if processing is complete (use recipe's processing time)
        if (processingTimer >= currentRecipe.processingTime)
        {
            OutputItem();
            isProcessing = false;

            if (material != null)
            {
                material.color = idleColor;
            }
        }
    }

    void OutputItem()
    {
        if (itemQueue.Count == 0) return;

        ProcessingItem processedItem = itemQueue.Dequeue();

        // Create the output based on recipe
        GameObject outputItem = Instantiate(
            processedItem.recipe.outputPrefab,
            outputPoint.position,
            Quaternion.identity
        );

        // Destroy the input item
        Destroy(processedItem.item);

        // Re-enable physics on output
        Rigidbody rb = outputItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            // Push in the direction the machine is facing
            rb.linearVelocity = transform.forward * outputSpeed;
        }

        Debug.Log($"Output item: {outputItem.name}");
    }

    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    // Visualize input/output points in editor
    void OnDrawGizmos()
    {
        if (inputPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(inputPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, inputPoint.position);
        }

        if (outputPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(outputPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, outputPoint.position);
        }
    }
}