using UnityEngine;
using System.Collections.Generic;

public class ConveyorBeltScroll : MonoBehaviour
{
    [Header("Visual Settings")]
    public float scrollSpeed = 0.5f;
    public Vector2 scrollDirection = new Vector2(0, 1);

    [Header("Physics Settings")]
    public bool moveItems = true;
    public float itemSpeed = 2f;

    [Tooltip("Local direction the belt moves items")]
    public Vector3 itemMoveDirection = Vector3.forward;

    [Header("Options")]
    [Tooltip("Prevent items from rotating/rolling")]
    public bool freezeItemRotation = true;

    [Tooltip("Pull items toward the center of the belt")]
    public bool centerItems = true;

    [Tooltip("How strongly items are pulled to center (higher = faster)")]
    public float centeringStrength = 5f;

    private Material material;
    private Vector2 offset;
    private HashSet<Rigidbody> itemsOnBelt = new HashSet<Rigidbody>();

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;

        // Ensure belt has a kinematic rigidbody for collisions
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        // Animate texture
        offset += scrollDirection * scrollSpeed * Time.deltaTime;
        material.mainTextureOffset = offset;
    }

    void FixedUpdate()
    {
        if (moveItems)
        {
            MoveItemsOnBelt();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            itemsOnBelt.Add(rb);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            itemsOnBelt.Remove(rb);
        }
    }

    void MoveItemsOnBelt()
    {
        // Clean up any null references
        itemsOnBelt.RemoveWhere(rb => rb == null);

        // Get belt's center line
        Vector3 beltCenter = transform.position;

        foreach (Rigidbody rb in itemsOnBelt)
        {
            // Convert local direction to world space using belt's rotation
            Vector3 worldDirection = transform.TransformDirection(itemMoveDirection.normalized);

            // Create target velocity in world space
            Vector3 targetVelocity = worldDirection * itemSpeed * -1f;
            targetVelocity.y = rb.linearVelocity.y; // Preserve gravity

            // CENTER ITEMS TOWARD BELT MIDDLE
            if (centerItems)
            {
                // Get the perpendicular direction to belt movement (the "width" axis)
                Vector3 rightDirection = transform.right;

                // Project item position onto the belt's plane
                Vector3 itemPos = rb.position;
                Vector3 toBelt = beltCenter - itemPos;

                // Calculate offset from center (only perpendicular to movement)
                float offsetFromCenter = Vector3.Dot(toBelt, rightDirection);

                // Add centering force perpendicular to movement direction
                Vector3 centeringForce = rightDirection * offsetFromCenter * centeringStrength;
                targetVelocity += centeringForce;
            }

            // Apply velocity
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

            // Optional: Stop rotation
            if (freezeItemRotation)
            {
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    // Visual helper to see belt direction in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 worldDir = transform.TransformDirection(itemMoveDirection.normalized);
        Gizmos.DrawRay(transform.position, worldDir * 2f);
        Gizmos.DrawSphere(transform.position + worldDir * 2f, 0.2f);
    }
}