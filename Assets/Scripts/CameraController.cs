using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;

    private Vector3 lastMousePosition;
    private Camera targetCamera;
    private BattlePrototype battle;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (battle == null) battle = FindFirstObjectByType<BattlePrototype>();
        if (battle != null && (battle.InventoryOpen || (Keyboard.current != null &&
            (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))))
        {
            if (Mouse.current != null) lastMousePosition = Mouse.current.position.ReadValue();
            return;
        }
        Vector3 keyboard = new Vector3(
            (Keyboard.current != null && Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current != null && Keyboard.current.aKey.isPressed ? 1f : 0f),
            (Keyboard.current != null && Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current != null && Keyboard.current.sKey.isPressed ? 1f : 0f),
            0f);
        if (keyboard.sqrMagnitude > 0.001f)
        {
            transform.position += keyboard.normalized * moveSpeed * Time.deltaTime;
        }

        Camera camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera == null)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            Vector3 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePosition - lastMousePosition;
            float worldPerPixel = camera.orthographicSize * 2f / camera.pixelHeight;
            transform.position += new Vector3(-delta.x * worldPerPixel, -delta.y * worldPerPixel, 0f);
            lastMousePosition = currentMousePosition;
        }

        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
        if (Mathf.Abs(scroll) > 0.001f && camera.orthographic)
        {
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize - scroll * zoomSpeed, 3f, 64f);
        }
    }
}
