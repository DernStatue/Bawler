using UnityEngine;

public class MiningDrill : MonoBehaviour
{
    [Header("Mining Settings")]
    [Tooltip("How often to mine (seconds)")]
    public float miningInterval = 2f;

    [Tooltip("Where extracted resources spawn")]
    public Transform outputPoint;

    [Tooltip("How far to check for resource nodes")]
    public float detectionRange = 3f;

    [Tooltip("Layer of resource nodes")]
    public LayerMask resourceLayer;

    [Header("Visual Feedback")]
    public bool showMiningEffect = true;
    public Color miningColor = Color.yellow;
    public Color idleColor = Color.gray;
    public Color noResourceColor = Color.red;

    [Header("Animation")]
    public Transform drillHead;
    public float drillSpeed = 5f;
    public float drillDistance = 0.5f;

    private float miningTimer = 0f;
    private Material material;
    private Renderer drillRenderer;
    private ResourceNode currentNode;
    private Vector3 drillStartPos;
    private bool isDrilling = false;

    void Start()
    {
        drillRenderer = GetComponent<Renderer>();
        if (drillRenderer != null)
        {
            material = drillRenderer.material;
        }

        // Create output point if not assigned
        if (outputPoint == null)
        {
            GameObject output = new GameObject("OutputPoint");
            output.transform.parent = transform;
            output.transform.localPosition = new Vector3(0, 0.5f, 1f);
            outputPoint = output.transform;
        }

        // Store drill head start position
        if (drillHead != null)
        {
            drillStartPos = drillHead.localPosition;
        }

        // Random start time to avoid all drills syncing
        miningTimer = Random.Range(0f, miningInterval);

        // Find nearby resource node
        FindResourceNode();
    }

    void Update()
    {
        // Check for resource node if we don't have one
        if (currentNode == null)
        {
            FindResourceNode();
            UpdateVisuals(false);
            return;
        }

        // Mine from the node
        miningTimer += Time.deltaTime;

        // Animate drill
        AnimateDrill();

        if (miningTimer >= miningInterval)
        {
            Mine();
            miningTimer = 0f;
        }

        // Visual feedback
        UpdateVisuals(true);
    }

    void FindResourceNode()
    {
        // Look for resource nodes in range
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, resourceLayer);

        if (hits.Length > 0)
        {
            // Get the closest node
            float closestDist = Mathf.Infinity;
            Collider closestHit = null;

            foreach (Collider hit in hits)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestHit = hit;
                }
            }

            if (closestHit != null)
            {
                currentNode = closestHit.GetComponent<ResourceNode>();
                if (currentNode != null)
                {
                    Debug.Log($"Drill found resource node: {currentNode.resourceName}");
                }
            }
        }
        else
        {
            currentNode = null;
        }
    }

    void Mine()
    {
        if (currentNode == null || currentNode.IsEmpty())
        {
            Debug.Log("No resource node or node is empty");
            currentNode = null;
            FindResourceNode();
            return;
        }

        // Extract resource from node
        GameObject resource = currentNode.Extract();

        if (resource != null)
        {
            // Spawn at output point
            resource.transform.position = outputPoint.position;
            resource.transform.rotation = Quaternion.identity;

            // Give it physics
            Rigidbody rb = resource.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * 1f + Vector3.up * 0.5f;
            }

            Debug.Log($"Extracted: {resource.name}");
        }
    }

    void AnimateDrill()
    {
        if (drillHead == null) return;

        // Bob up and down
        float offset = Mathf.Sin(Time.time * drillSpeed) * drillDistance;
        drillHead.localPosition = drillStartPos + new Vector3(0, offset, 0);

        // Rotate
        drillHead.Rotate(Vector3.up, Time.deltaTime * drillSpeed * 100f);
    }

    void UpdateVisuals(bool hasResource)
    {
        if (!showMiningEffect || material == null) return;

        if (hasResource)
        {
            // Pulse between idle and mining color
            float progress = miningTimer / miningInterval;
            material.color = Color.Lerp(idleColor, miningColor, progress);
        }
        else
        {
            // Show no resource color
            material.color = noResourceColor;
        }
    }

    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    // Visualize detection range and output point
    void OnDrawGizmos()
    {
        // Detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Output point
        if (outputPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(outputPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, outputPoint.position);
        }

        // Line to current resource node
        if (currentNode != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentNode.transform.position);
        }
    }
}
