using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraControls
{
    private const float EdgeBand = 18f;
    private const float MinSpeed = .2f;
    private const float MaxSpeed = 3f;
    private const string PanSpeedKey = "AshenOath.WorldMap.PanSpeed";
    private const string ZoomSpeedKey = "AshenOath.WorldMap.ZoomSpeed";

    private readonly Camera camera;
    private float panSpeed;
    private float zoomSpeed;
    public bool SettingsOpen { get; set; }

    public CameraControls(Camera camera)
    {
        this.camera = camera;
        panSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(PanSpeedKey, 1f), MinSpeed, MaxSpeed);
        zoomSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(ZoomSpeedKey, 1f), MinSpeed, MaxSpeed);
    }

    public Vector2 EdgeMovement(Vector2 cursor, float frameTime)
    {
        if (!new Rect(0, 0, Screen.width, Screen.height).Contains(cursor)) return Vector2.zero;

        float edge = Mathf.Max(1f, EdgeBand * Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        var direction = new Vector2(
            cursor.x < edge ? -1 : cursor.x >= Screen.width - edge ? 1 : 0,
            cursor.y < edge ? -1 : cursor.y >= Screen.height - edge ? 1 : 0);
        return direction.normalized * (camera.orthographicSize * 1.6f * panSpeed * frameTime);
    }

    public Vector2 ZoomAtCursor(Vector2 cursor, float scroll, float minSize, float maxSize)
    {
        if (scroll == 0f || !camera.orthographic || !camera.pixelRect.Contains(cursor))
            return Vector2.zero;

        // Input System нормализует колесо. Деление на 120 нужно только для старого формата Windows.
        if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
            (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
            scroll /= 120f;

        var screenPoint = new Vector3(cursor.x, cursor.y, -camera.transform.position.z);
        Vector2 anchor = camera.ScreenToWorldPoint(screenPoint);
        camera.orthographicSize = Mathf.Clamp(
            camera.orthographicSize * Mathf.Pow(.84f, scroll * zoomSpeed), minSize, maxSize);
        return anchor - (Vector2)camera.ScreenToWorldPoint(screenPoint);
    }

    public bool IsOverSettings(Vector2 cursor, Rect panel)
    {
        return SettingsOpen && panel.Contains(new Vector2(
            cursor.x * 1280f / Screen.width, (Screen.height - cursor.y) * 720f / Screen.height));
    }

    public void DrawSettings(Rect panel, Color border, Color background, GUIStyle label)
    {
        Color previousColor = GUI.color;
        GUI.color = border;
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = background;
        GUI.DrawTexture(new Rect(panel.x + 1, panel.y + 1, panel.width - 2, panel.height - 2), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUILayout.BeginArea(new Rect(panel.x + 16, panel.y + 10, panel.width - 32, panel.height - 20));
        panSpeed = DrawSpeedSlider("Прокрутка у краёв", panSpeed, PanSpeedKey, label);
        GUILayout.Space(12);
        zoomSpeed = DrawSpeedSlider("Приближение и отдаление", zoomSpeed, ZoomSpeedKey, label);
        GUILayout.EndArea();
    }

    private static float DrawSpeedSlider(string caption, float value, string key, GUIStyle label)
    {
        GUILayout.Label($"{caption}   {value:0.0}×", label, GUILayout.Height(24));
        float adjusted = Mathf.Round(GUILayout.HorizontalSlider(value, MinSpeed, MaxSpeed) * 10f) / 10f;
        if (adjusted != value) PlayerPrefs.SetFloat(key, adjusted);
        return adjusted;
    }
}
