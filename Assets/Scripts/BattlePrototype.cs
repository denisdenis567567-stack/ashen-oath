using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using AshenOath;

// Управление сценой и ввод. Правила находятся в BattleModel.
public class BattlePrototype : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/TestBattle.unity";
    private bool leavingBattle;

    [InspectorName("Настройки боя")] public BattleSettings settings;
    public BattleModel Battle { get; private set; }
    public BattlePaths Reachable { get; private set; }
    public bool MovingMode { get; private set; } = true;
    public bool Busy { get; private set; }
    public Hex? Hovered { get; private set; }
    public Camera BattleCamera { get; private set; }
    public CameraController CameraInput { get; private set; }
    public CharacterInventory Character { get { return Battle.Player.Inventory; } }
    public bool InventoryOpen { get; private set; }
    private int inventoryClosedFrame = -1;
    private const string NameKey = "AshenOath.CharacterName";
    private BattleBoardView view;
    private bool ownsSettings;
    private int screenWidth, screenHeight;

    private void Start()
    {
        if (settings == null) settings = Resources.Load<BattleSettings>("BattleSettings");
        if (settings == null) { settings = ScriptableObject.CreateInstance<BattleSettings>(); ownsSettings = true; }
        BattleCamera = Camera.main;
        if (BattleCamera == null)
        { BattleCamera = new GameObject("Main Camera").AddComponent<Camera>(); BattleCamera.tag = "MainCamera"; }
        BattleCamera.orthographic = true;
        BattleCamera.clearFlags = CameraClearFlags.SolidColor;
        BattleCamera.backgroundColor = new Color(.045f, .055f, .065f);
        CameraInput = BattleCamera.GetComponent<CameraController>();
        if (CameraInput == null) CameraInput = BattleCamera.gameObject.AddComponent<CameraController>();
        CameraInput.Initialize(this);
        view = gameObject.AddComponent<BattleBoardView>();
        gameObject.AddComponent<BattleHud>().Initialize(this);
        gameObject.AddComponent<BattleInventoryWindow>().Initialize(this);
        Restart();
    }
    public void Restart()
    {
        SetInventoryOpen(false);
        StopAllCoroutines(); Busy = false; MovingMode = true; Hovered = null;
        var previous = Battle;
        Battle = new BattleModel(settings.Rules());
        for (int i = 0; i < Battle.Allies.Count; i++)
        {
            var inventory = Battle.Allies[i].Inventory;
            inventory.Rename(PlayerPrefs.GetString(i == 0 ? NameKey : NameKey + "." + i, Battle.Allies[i].RoleName));
            if (previous != null)
            {
                System.Array.Copy(previous.Allies[i].Inventory.Equipped, inventory.Equipped, inventory.Equipped.Length);
                System.Array.Copy(previous.Allies[i].Inventory.Bag, inventory.Bag, inventory.Bag.Length);
            }
        }
        view.Build(Battle); Refresh(); FocusPlayer();
    }
    private void Update()
    {
        if (Battle == null) return;
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        { screenWidth = Screen.width; screenHeight = Screen.height; BattleCamera.rect = new Rect(0, .25f, 1, .55f); }
        if (Battle.Finished) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        { SetInventoryOpen(!InventoryOpen); return; }
        if (InventoryOpen)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetInventoryOpen(false);
            return;
        }
        if (inventoryClosedFrame == Time.frameCount) return;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame) Restart();
            if (keyboard.fKey.wasPressedThisFrame) FocusPlayer();
            if (keyboard.gKey.wasPressedThisFrame) Overview();
            if (keyboard.spaceKey.wasPressedThisFrame) EndTurn();
            if (keyboard.digit1Key.wasPressedThisFrame) SelectMove();
            if (keyboard.digit2Key.wasPressedThisFrame) PlayerAttack();
        }
        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 screen = mouse.position.ReadValue(); Hex? hovered = null;
        if (BattleCamera.pixelRect.Contains(screen) && !CameraInput.IsOverSettings(screen))
        {
            Vector3 world = BattleCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            Hex cell = BattleBoardView.FromWorld(world);
            if (Battle.Inside(cell) && Battle.Tile(cell).Visible) hovered = cell;
        }
        if (Hovered != hovered) { Hovered = hovered; view.Refresh(Battle, Reachable, MovingMode && CanAct, Hovered, !MovingMode && CanAct); }
        if (!mouse.leftButton.wasPressedThisFrame || !hovered.HasValue || Busy) return;
        var clicked = Battle.UnitAt(hovered.Value);
        if (clicked != null && !clicked.IsEnemy) { SelectUnit(clicked); return; }
        if (!CanAct) return;
        if (!MovingMode)
        {
            if (clicked != null && clicked.IsEnemy) { Battle.Attack(Battle.Player, clicked); Refresh(); }
            else Battle.Message = "Выберите врага в пределах дальности атаки.";
            return;
        }
        Hex target = hovered.Value;
        if (clicked != null) { Battle.Message = "Нажмите «Атака», затем выберите врага."; return; }
        int cost;
        if (!Reachable.Cost.TryGetValue(target, out cost) || cost > Battle.Player.AP)
        { Battle.Message = "Клетка заблокирована или не хватает ОД."; return; }
        if (target != Battle.Player.Position) StartCoroutine(MovePlayer(target));
    }
    public void ContinueJourney()
    {
        if (Battle == null || !Battle.Finished || leavingBattle) return;
        leavingBattle = true;
        WorldMap.ReturnFromBattle();
    }

    public bool CanAct { get { return Battle != null && Battle.CanUse(Battle.Player) && !Busy && !InventoryOpen && inventoryClosedFrame != Time.frameCount; } }
    public bool RenameCharacter(string name)
    {
        if (!Character.Rename(name)) return false;
        int index = Battle.Allies.IndexOf(Battle.Player);
        PlayerPrefs.SetString(index == 0 ? NameKey : NameKey + "." + index, Character.Name); PlayerPrefs.Save(); return true;
    }
    public void SetInventoryOpen(bool open)
    {
        InventoryOpen = open;
        if (!open) inventoryClosedFrame = Time.frameCount;
        Hovered = null;
        if (Battle != null && view != null) Refresh();
    }
    public void SelectUnit(BattleUnit unit)
    {
        if (Busy || !Battle.SelectPlayer(unit)) return;
        Hovered = null; MovingMode = true;
        Battle.Message = unit.Inventory.Name + (unit.Alive ? unit.TurnEnded ? ": ход завершён." : ": выберите действие." : ": погиб.");
        Refresh();
    }
    public void SelectMove()
    { if (CanAct) { MovingMode = true; Battle.Message = "Выберите подсвеченную клетку. Цена пути показана при наведении."; Refresh(); } }
    public void PlayerAttack()
    { if (CanAct) { MovingMode = false; Battle.Message = "Выберите врага: дальность " + Battle.Player.MinRange + "–" + Battle.Player.MaxRange + " клеток, атака " + Battle.Rules.AttackCost + " ОД."; Refresh(); } }
    private IEnumerator MovePlayer(Hex target)
    {
        Busy = true; var path = Reachable.To(target); int before = Battle.Player.AP;
        foreach (Hex step in path)
        {
            while (InventoryOpen) yield return null;
            if (!Battle.MoveStep(Battle.Player, step)) break;
            Refresh(); yield return new WaitForSeconds(settings.actionDelay);
        }
        Busy = false; Battle.Message = "Движение: потрачено " + (before - Battle.Player.AP) + " ОД."; Refresh();
    }
    public void EndTurn()
    {
        if (!CanAct || !Battle.EndSelectedTurn()) return;
        Refresh();
        if (!Battle.PlayerTurn) StartCoroutine(EnemyTurn());
    }
    public void EndSquadTurn()
    { if (!Busy && !InventoryOpen && Battle.PlayerTurn && !Battle.Finished) { Battle.BeginEnemyTurn(); StartCoroutine(EnemyTurn()); } }
    private IEnumerator EnemyTurn()
    {
        Busy = true; Battle.BeginEnemyTurn(); Refresh();
        yield return new WaitForSeconds(settings.actionDelay);
        while (!Battle.Finished && Battle.ActiveEnemy != null)
        {
            var enemy = Battle.ActiveEnemy;
            while (!Battle.Finished && enemy.AP > 0)
            {
                while (InventoryOpen) yield return null;
                var target = Battle.FindTarget(enemy);
                if (target != null) { if (!Battle.Attack(enemy, target)) break; }
                else
                {
                    var path = Battle.EnemyPath(enemy);
                    if (path.Count == 0 || !Battle.MoveStep(enemy, path[0])) break;
                    Battle.Message = "Ходит " + enemy.Inventory.Name;
                }
                Refresh(); yield return new WaitForSeconds(settings.actionDelay);
            }
            Battle.EndEnemyUnitTurn(); Refresh();
        }
        while (InventoryOpen) yield return null;
        Battle.BeginPlayerTurn(); Busy = false; Refresh();
    }
    public void Refresh()
    {
        Reachable = Battle.Paths(Battle.Player, true);
        view.Refresh(Battle, Reachable, MovingMode && Battle.CanUse(Battle.Player) && !Busy && !InventoryOpen, Hovered, !MovingMode && Battle.CanUse(Battle.Player) && !Busy && !InventoryOpen);
    }
    public void FocusPlayer()
    {
        BattleCamera.rect = new Rect(0, .25f, 1, .55f);
        Vector3 p = BattleBoardView.World(Battle.Player.Position);
        BattleCamera.transform.SetPositionAndRotation(new Vector3(p.x + 2.5f, p.y, -10), Quaternion.identity);
        BattleCamera.orthographicSize = Mathf.Max(6.5f, 9f / BattleCamera.aspect);
    }
    public void Overview()
    {
        Vector3 p = BattleBoardView.World(new Hex(Battle.Rules.Width - 1, Battle.Rules.Height - 1)) * .5f;
        BattleCamera.transform.position = new Vector3(p.x, p.y, -10);
        BattleCamera.orthographicSize = Mathf.Max(Battle.Rules.Height * .78f, Battle.Rules.Width * .9f / BattleCamera.aspect);
    }
    private void OnDestroy() { if (ownsSettings && settings != null) Destroy(settings); }
}

