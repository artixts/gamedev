using UnityEngine;

public class MouseCameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerBody;

    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 200f;
    [SerializeField] private float minimumVerticalAngle = -75f;
    [SerializeField] private float maximumVerticalAngle = 75f;

    private float verticalRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }

        verticalRotation = transform.localEulerAngles.x;

        if (verticalRotation > 180f)
            verticalRotation -= 360f;
    }

    private void Update()
    {
        // Press Escape to release the mouse.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click the Game view to lock the mouse again.
        if (Input.GetMouseButtonDown(0) &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Horizontal mouse movement rotates the player.
        if (playerBody != null)
        {
            playerBody.Rotate(
                Vector3.up * mouseX,
                Space.World
            );
        }

        // Vertical mouse movement rotates the camera only.
        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minimumVerticalAngle,
            maximumVerticalAngle
        );

        transform.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
