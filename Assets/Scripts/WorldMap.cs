using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public sealed class WorldMap : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/WorldMap.unity";
    private const float MapWidth = 64f;
    private const float MapHeight = 32f;
    private const float TravelSpeed = 6f;
    private const float MinutesPerSecond = 12f;
    private const float HeaderHeight = 72f;
    private const float FooterHeight = 44f;
    private const float MapPadding = 1f;
    private const float MinZoomSize = 4.5f;
    private const float StartingZoomSize = 10f;
    private static readonly Rect CameraPanel = new Rect(930, 83, 326, 135);

    private static Vector2 partyPosition;
    private static double minutes;
    private static bool returningFromBattle;

    private Camera mapCamera;
    private Transform mapContent;
    private float maxZoomSize;
    private CameraControls cameraControls;
    private SpriteRenderer party, waypoint;
    private readonly List<Sprite> generatedSprites = new List<Sprite>(3);
    private Vector2 destination;
    private int speed = 1;
    private int speedBeforePause = 1;
    private bool loadingBattle;
    private GUIStyle title, text, button;
    private static readonly Color Panel = new Color(.065f, .075f, .085f);
    private static readonly Color Gold = new Color(.9f, .77f, .49f);
    private static readonly string[] SpeedLabels = { "Пауза", "1×", "2×", "3×" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetJourney()
    {
        partyPosition = Vector2.zero;
        minutes = 8 * 60;
        returningFromBattle = false;
    }

    public static void ReturnFromBattle()
    {
        returningFromBattle = true;
        SceneManager.LoadSceneAsync(ScenePath);
    }

    private void Start()
    {
        mapCamera = GetComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(.035f, .045f, .04f);
        mapCamera.rect = new Rect(0, FooterHeight / 720f, 1, 1 - (HeaderHeight + FooterHeight) / 720f);
        UpdateCameraViewport();
        mapCamera.orthographicSize = StartingZoomSize;
        SetCameraCenter(partyPosition);
        cameraControls = new CameraControls(mapCamera);

        mapContent = new GameObject("Глобальная карта — объекты").transform;
        AddSprite("Равнина", CreateGround(), 0);
        party = AddSprite("Отряд игрока", CreateMarker(true), 2);
        waypoint = AddSprite("Точка назначения", CreateMarker(false), 1);
        destination = partyPosition;
        party.transform.position = partyPosition;
        waypoint.enabled = false;
        if (returningFromBattle) SetSpeed(0);
        returningFromBattle = false;
    }

    private void Update()
    {
        if (loadingBattle) return;
        UpdateCameraViewport();
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) SetSpeed(speed == 0 ? speedBeforePause : 0);
            if (keyboard.digit1Key.wasPressedThisFrame) SetSpeed(1);
            if (keyboard.digit2Key.wasPressedThisFrame) SetSpeed(2);
            if (keyboard.digit3Key.wasPressedThisFrame) SetSpeed(3);
        }

        float frameTime = Time.unscaledDeltaTime;
        var mouse = Mouse.current;
        if (mouse != null && Application.isFocused)
        {
            Vector2 cursor = mouse.position.ReadValue();
            if (!cameraControls.IsOverSettings(cursor, CameraPanel))
            {
                UpdateCamera(cursor, mouse.scroll.ReadValue().y, frameTime);
                if (mouse.rightButton.wasPressedThisFrame && mapCamera.pixelRect.Contains(cursor))
                {
                    Vector2 point = ScreenToMapPoint(cursor);
                    if (Mathf.Abs(point.x) <= MapWidth / 2 && Mathf.Abs(point.y) <= MapHeight / 2)
                        destination = point;
                }
            }
        }

        float simulationStep = frameTime * speed;
        minutes += simulationStep * MinutesPerSecond;
        partyPosition = Vector2.MoveTowards(partyPosition, destination, TravelSpeed * simulationStep);
        party.transform.position = partyPosition;
        waypoint.transform.position = destination;
        waypoint.enabled = partyPosition != destination;
    }

    private void UpdateCameraViewport()
    {
        float aspect = (float)Mathf.Max(1, mapCamera.pixelWidth) / Mathf.Max(1, mapCamera.pixelHeight);
        if (maxZoomSize > 0 && aspect == mapCamera.aspect) return;

        bool showingWholeMap = mapCamera.orthographicSize == maxZoomSize;
        mapCamera.aspect = aspect;
        maxZoomSize = Mathf.Max(MapHeight / 2 + MapPadding, (MapWidth / 2 + MapPadding) / aspect);
        mapCamera.orthographicSize = showingWholeMap ? maxZoomSize : Mathf.Min(mapCamera.orthographicSize, maxZoomSize);
        SetCameraCenter(mapCamera.transform.position);
    }

    private void UpdateCamera(Vector2 cursor, float scroll, float frameTime)
    {
        Vector2 center = mapCamera.transform.position;
        center += cameraControls.ZoomAtCursor(cursor, scroll, MinZoomSize, maxZoomSize);
        SetCameraCenter(center + cameraControls.EdgeMovement(cursor, frameTime));
    }

    private void SetCameraCenter(Vector2 center)
    {
        Transform cameraTransform = mapCamera.transform;
        float size = mapCamera.orthographicSize;
        float limitX = Mathf.Max(0f, MapWidth / 2 + MapPadding - size * mapCamera.aspect);
        float limitY = Mathf.Max(0f, MapHeight / 2 + MapPadding - size);
        cameraTransform.position = new Vector3(
            Mathf.Clamp(center.x, -limitX, limitX), Mathf.Clamp(center.y, -limitY, limitY), cameraTransform.position.z);
    }

    private Vector2 ScreenToMapPoint(Vector2 cursor)
    {
        return mapCamera.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, -mapCamera.transform.position.z));
    }

    private void SetSpeed(int value)
    {
        speed = value;
        if (speed > 0) speedBeforePause = speed;
    }

    private void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            title.normal.textColor = Gold;
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            text.normal.textColor = new Color(.86f, .87f, .81f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousBackground = GUI.backgroundColor;
        bool previousEnabled = GUI.enabled;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        Fill(new Rect(0, 0, 1280, HeaderHeight), Panel);
        Fill(new Rect(0, 720 - FooterHeight, 1280, FooterHeight), Panel);
        Fill(new Rect(0, HeaderHeight - 1, 1280, 1), Gold);
        Fill(new Rect(0, 720 - FooterHeight, 1280, 1), Gold);

        GUI.Label(new Rect(24, 8, 320, 34), "Глобальная карта", title);
        int wholeMinutes = (int)minutes;
        int day = wholeMinutes / 1440 + 1;
        GUI.Label(new Rect(25, 36, 310, 28),
            $"День {day}   {wholeMinutes / 60 % 24:00}:{wholeMinutes % 60:00}", text);

        GUI.enabled = !loadingBattle;
        for (int i = 0; i < SpeedLabels.Length; i++)
        {
            GUI.backgroundColor = i == speed ? Gold : previousBackground;
            if (GUI.Button(new Rect(438 + i * 102, 19, 96, 34), SpeedLabels[i], button))
                SetSpeed(i);
        }
        GUI.backgroundColor = cameraControls.SettingsOpen ? Gold : previousBackground;
        if (GUI.Button(new Rect(868, 19, 172, 34), "Камера", button))
            cameraControls.SettingsOpen = !cameraControls.SettingsOpen;
        GUI.backgroundColor = previousBackground;
        if (GUI.Button(new Rect(1056, 19, 200, 34), "Тестовый бой", button))
            StartBattle();
        GUI.Label(new Rect(24, 682, 1232, 31),
            "ПКМ — идти   •   Края экрана — камера   •   Колесо — масштаб   •   Пробел — пауза   •   1–3 — скорость", text);
        if (cameraControls.SettingsOpen) cameraControls.DrawSettings(CameraPanel, Gold, Panel, text);

        GUI.enabled = previousEnabled;
        GUI.matrix = previousMatrix;
    }

    private void StartBattle()
    {
        if (loadingBattle) return;
        loadingBattle = true;
        SceneManager.LoadSceneAsync(BattlePrototype.ScenePath);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private SpriteRenderer AddSprite(string label, Sprite sprite, int order)
    {
        generatedSprites.Add(sprite);
        var item = new GameObject(label);
        item.transform.SetParent(mapContent, false);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite CreateGround()
    {
        const int width = 512;
        const int height = 256;
        var pixels = new Color32[width * height];
        Color dark = new Color(.24f, .30f, .19f);
        Color light = new Color(.55f, .56f, .35f);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float broad = Mathf.PerlinNoise(2 + x * .014f, 2 + y * .014f);
            float fine = Mathf.PerlinNoise(x * .13f, y * .13f);
            pixels[y * width + x] = Color.Lerp(dark, light, broad * .8f + fine * .2f);
        }
        return CreateSprite(pixels, width, height, width / MapWidth);
    }

    private static Sprite CreateMarker(bool isParty)
    {
        const int size = 64;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var point = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f);
            float radius = point.magnitude;
            if (radius > 29f) continue;
            bool silhouette = (point - new Vector2(0, 8)).sqrMagnitude < 49f ||
                (point.y > -15f && point.y < 0 && Mathf.Abs(point.x) < 11f);
            if (radius > 25f || (isParty && silhouette))
                pixels[y * size + x] = Gold;
            else if (isParty)
                pixels[y * size + x] = Panel;
        }
        return CreateSprite(pixels, size, size, isParty ? 36f : 48f);
    }

    private static Sprite CreateSprite(Color32[] pixels, int width, int height, float pixelsPerUnit)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f),
            pixelsPerUnit, 0, SpriteMeshType.FullRect);
        texture.Apply(false, true);
        return sprite;
    }

    private void OnDestroy()
    {
        if (mapContent != null) Destroy(mapContent.gameObject);
        foreach (var sprite in generatedSprites)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
