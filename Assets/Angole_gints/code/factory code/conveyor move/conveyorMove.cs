using UnityEngine;

public class ConveyorBeltScroll : MonoBehaviour
{
    [Header("Visual Settings")]
    public float scrollSpeed = 0.5f;
    public Vector2 scrollDirection = new Vector2(0, 1);

    [Header("Physics Settings")]
    public bool moveItems = true;
    public float itemSpeed = 2f;
    public Vector3 itemMoveDirection = Vector3.forward;

    private Material material;
    private Vector2 offset;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;
    }

    void Update()
    {
        // Animate texture
        offset += scrollDirection * scrollSpeed * Time.deltaTime;
        material.mainTextureOffset = offset;
    }

    void FixedUpdate()
    {
        // Move items if enabled
        if (moveItems)
        {
            MoveItemsOnBelt();
        }
    }

    void MoveItemsOnBelt()
    {
        // Get the belt's collider
        Collider beltCollider = GetComponent<Collider>();
        if (beltCollider == null) return;

        // Find all objects touching the belt
        Collider[] objectsOnBelt = Physics.OverlapBox(
            beltCollider.bounds.center,
            beltCollider.bounds.extents,
            transform.rotation
        );

        foreach (Collider col in objectsOnBelt)
        {
            // Skip the belt itself
            if (col.gameObject == gameObject) continue;

            // Only move objects with rigidbodies
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                // Calculate movement direction in world space
                Vector3 moveDirection = transform.TransformDirection(itemMoveDirection.normalized);

                // Create target velocity
                Vector3 targetVelocity = moveDirection * itemSpeed;
                targetVelocity.y = rb.linearVelocity.y; // Preserve gravity

                // Smoothly move items to belt speed
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
            }
        }
    }

    void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}