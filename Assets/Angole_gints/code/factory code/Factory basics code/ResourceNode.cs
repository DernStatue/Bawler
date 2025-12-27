using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Resource Settings")]
    [Tooltip("Name of this resource")]
    public string resourceName = "Ore";

    [Tooltip("Prefab to spawn when extracted")]
    public GameObject resourcePrefab;

    [Tooltip("Total amount of resources in this node")]
    public int totalResources = 100;

    [Tooltip("Infinite resources (never runs out)")]
    public bool infiniteResources = false;

    [Header("Visual Feedback")]
    public bool showDepletionEffect = true;

    private int remainingResources;
    private Material material;
    private Color originalColor;

    void Start()
    {
        remainingResources = totalResources;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            material = rend.material;
            originalColor = material.color;
        }

        // Make sure it's on the correct layer
        if (gameObject.layer == 0)
        {
            Debug.LogWarning($"Resource node '{resourceName}' is not on a layer! Drills might not detect it.");
        }
    }

    public GameObject Extract()
    {
        if (IsEmpty() && !infiniteResources)
        {
            return null;
        }

        if (!infiniteResources)
        {
            remainingResources--;
        }

        // Update visual
        UpdateVisual();

        // Check if depleted
        if (IsEmpty() && !infiniteResources)
        {
            Debug.Log($"Resource node '{resourceName}' depleted!");
            // Optionally destroy or disable the node
            // Destroy(gameObject, 1f);
        }

        // Create and return resource
        if (resourcePrefab != null)
        {
            GameObject resource = Instantiate(resourcePrefab);
            return resource;
        }

        return null;
    }

    public bool IsEmpty()
    {
        return !infiniteResources && remainingResources <= 0;
    }

    public float GetDepletionPercent()
    {
        if (infiniteResources) return 0f;
        return 1f - ((float)remainingResources / totalResources);
    }

    void UpdateVisual()
    {
        if (!showDepletionEffect || material == null) return;

        float depletion = GetDepletionPercent();

        // Fade to gray as resources deplete
        Color depletedColor = Color.gray;
        material.color = Color.Lerp(originalColor, depletedColor, depletion);

        // Optional: Shrink the node
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, depletion);
    }

    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    void OnDrawGizmos()
    {
        // Show resource info
        Gizmos.color = IsEmpty() ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
