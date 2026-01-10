using UnityEngine;

public class PerspectiveCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Camera movement speed")]
    public float moveSpeed = 10f;

    [Tooltip("Sprint speed multiplier (hold Shift)")]
    public float sprintMultiplier = 2f;

    [Tooltip("Use WASD keys for movement")]
    public bool useKeyboardMovement = true;

    [Tooltip("Use middle mouse drag for movement")]
    public bool useMiddleMouseDrag = true;

    [Tooltip("Use edge scrolling when mouse near screen edge")]
    public bool useEdgeScrolling = false;

    [Tooltip("Distance from edge to trigger scrolling (pixels)")]
    public float edgeScrollBorder = 50f;

    [Header("Mouse Look Settings")]
    [Tooltip("Enable right-click camera rotation")]
    public bool enableMouseLook = true;

    [Tooltip("Mouse sensitivity for camera rotation")]
    public float mouseSensitivity = 2f;

    [Tooltip("Limit vertical rotation")]
    public bool clampVerticalRotation = true;

    [Tooltip("Minimum pitch angle (looking down)")]
    public float minPitch = -80f;

    [Tooltip("Maximum pitch angle (looking up)")]
    public float maxPitch = 80f;

    [Header("Zoom Settings")]
    [Tooltip("Mouse scroll zoom speed")]
    public float zoomSpeed = 5f;

    [Tooltip("Minimum field of view (zoomed in)")]
    public float minFOV = 20f;

    [Tooltip("Maximum field of view (zoomed out)")]
    public float maxFOV = 90f;

    [Tooltip("Smooth zoom transition")]
    public bool smoothZoom = true;

    [Tooltip("Zoom smoothing speed")]
    public float zoomSmoothSpeed = 5f;

    [Header("Boundary Settings")]
    [Tooltip("Limit camera movement to boundaries")]
    public bool useBoundaries = true;

    [Tooltip("Minimum X position")]
    public float minX = -50f;

    [Tooltip("Maximum X position")]
    public float maxX = 50f;

    [Tooltip("Minimum Y position (height)")]
    public float minY = 1f;

    [Tooltip("Maximum Y position (height)")]
    public float maxY = 50f;

    [Tooltip("Minimum Z position")]
    public float minZ = -50f;

    [Tooltip("Maximum Z position")]
    public float maxZ = 50f;

    [Header("Height Control")]
    [Tooltip("Use Q/E for height control")]
    public bool enableHeightControl = true;

    [Tooltip("Height change speed")]
    public float heightSpeed = 5f;

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    private float targetFOV;
    private float currentPitch = 0f;
    private float currentYaw = 0f;
    private bool isRotating = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam.orthographic)
        {
            Debug.LogWarning("Camera is orthographic! Switching to perspective mode.");
            cam.orthographic = false;
        }

        targetFOV = cam.fieldOfView;

        // Initialize rotation from current camera rotation
        Vector3 currentRotation = transform.eulerAngles;
        currentYaw = currentRotation.y;
        currentPitch = currentRotation.x;

        // Fix pitch if it's wrapped around 360
        if (currentPitch > 180f)
            currentPitch -= 360f;

        // Lock and hide cursor when rotating (optional)
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleZoom();

        if (enableHeightControl)
        {
            HandleHeightControl();
        }
    }

    void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        float currentSpeed = moveSpeed;

        // Sprint modifier
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        // Keyboard movement (WASD)
        if (useKeyboardMovement)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Move relative to camera direction
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // Flatten direction (no up/down movement from looking)
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            movement += right * horizontal + forward * vertical;
        }

        // Edge scrolling
        if (useEdgeScrolling && !isRotating)
        {
            Vector3 mousePos = Input.mousePosition;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            if (mousePos.x < edgeScrollBorder)
                movement -= right;
            if (mousePos.x > Screen.width - edgeScrollBorder)
                movement += right;
            if (mousePos.y < edgeScrollBorder)
                movement -= forward;
            if (mousePos.y > Screen.height - edgeScrollBorder)
                movement += forward;
        }

        // Middle mouse drag
        if (useMiddleMouseDrag)
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
                isDragging = true;
            }

            if (Input.GetMouseButton(2) && isDragging)
            {
                Vector3 difference = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;

                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 dragMovement = -right * difference.x * 0.01f - forward * difference.y * 0.01f;
                transform.position += dragMovement * currentSpeed * 0.1f;
            }

            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
        }

        // Apply movement
        if (movement != Vector3.zero)
        {
            transform.position += movement.normalized * currentSpeed * Time.deltaTime;
        }

        // Apply boundaries
        if (useBoundaries)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            transform.position = pos;
        }
    }

    void HandleMouseLook()
    {
        if (!enableMouseLook) return;

        // Right mouse button to rotate camera
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
        }

        if (isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;
            currentPitch -= mouseY;

            // Clamp vertical rotation
            if (clampVerticalRotation)
            {
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }

            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            targetFOV -= scroll * zoomSpeed;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        // Apply zoom
        if (smoothZoom)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSmoothSpeed);
        }
        else
        {
            cam.fieldOfView = targetFOV;
        }
    }

    void HandleHeightControl()
    {
        float heightChange = 0f;

        if (Input.GetKey(KeyCode.Q))
            heightChange = -1f;
        if (Input.GetKey(KeyCode.E))
            heightChange = 1f;

        if (heightChange != 0f)
        {
            Vector3 pos = transform.position;
            pos.y += heightChange * heightSpeed * Time.deltaTime;

            if (useBoundaries)
            {
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }

            transform.position = pos;
        }
    }

    // Public methods for scripted camera control
    public void LookAt(Vector3 target)
    {
        transform.LookAt(target);

        // Update rotation values
        Vector3 rotation = transform.eulerAngles;
        currentYaw = rotation.y;
        currentPitch = rotation.x;
        if (currentPitch > 180f)
            currentPitch -= 360f;
    }

    public void MoveTo(Vector3 position, float duration = 1f)
    {
        StartCoroutine(MoveToCoroutine(position, duration));
    }

    public void SetFOV(float fov)
    {
        targetFOV = Mathf.Clamp(fov, minFOV, maxFOV);
    }

    private System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        transform.position = targetPos;
    }

    // Visualize boundaries in editor
    void OnDrawGizmosSelected()
    {
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;

            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

            Gizmos.DrawWireCube(center, size);
        }
    }
}