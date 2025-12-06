using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // WASD Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        // Mouse scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }
        else
        {
            transform.position += transform.forward * scroll * zoomSpeed;
        }
    }
}
