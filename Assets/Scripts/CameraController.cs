using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public static readonly Rect SettingsPanel = new Rect(930, 398, 326, 135);
    public CameraControls Controls { get; private set; }

    private Vector2 lastMousePosition;
    private Camera targetCamera;
    private BattlePrototype battle;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        Controls = new CameraControls(targetCamera);
    }

    public void Initialize(BattlePrototype controller)
    {
        battle = controller;
    }

    public bool IsOverSettings(Vector2 cursor)
    {
        return Controls.IsOverSettings(cursor, SettingsPanel);
    }

    private void LateUpdate()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        Vector2 cursor = mouse == null ? lastMousePosition : mouse.position.ReadValue();
        Vector2 mouseDelta = cursor - lastMousePosition;
        lastMousePosition = cursor;

        if (!Application.isFocused || (battle != null && (battle.InventoryOpen || battle.Battle?.Finished == true)) ||
            (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)))
            return;

        if (keyboard != null)
        {
            var direction = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            transform.position += (Vector3)(direction.normalized * moveSpeed * Time.deltaTime);
        }

        if (mouse == null || IsOverSettings(cursor)) return;

        bool dragging = mouse.middleButton.isPressed;
        if (dragging && !mouse.middleButton.wasPressedThisFrame)
        {
            float worldPerPixel = targetCamera.orthographicSize * 2f / Mathf.Max(1, targetCamera.pixelHeight);
            transform.position -= (Vector3)(mouseDelta * worldPerPixel);
        }
        UpdateMouseCamera(cursor, mouse.scroll.ReadValue().y, dragging ? 0f : Time.unscaledDeltaTime);
    }

    private void UpdateMouseCamera(Vector2 cursor, float scroll, float frameTime)
    {
        Vector2 zoomOffset = Controls.ZoomAtCursor(cursor, scroll, 3f, 64f);
        transform.position += (Vector3)(zoomOffset + Controls.EdgeMovement(cursor, frameTime));
    }
}
