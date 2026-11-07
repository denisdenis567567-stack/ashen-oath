using UnityEngine;
using AshenOath;

// Экран 1280×720 масштабируется вместе с окном игры.
public class BattleHud : MonoBehaviour
{
    private BattlePrototype controller;
    private GUIStyle title, text, small, button, resultTitle, resultText;
    private Texture2D moveIcon, attackIcon;
    private readonly Color ink = new Color(.85f, .86f, .81f);
    private static readonly Color Panel = new Color(.065f, .075f, .085f);
    private static readonly Color Gold = new Color(.9f, .77f, .49f);
    public void Initialize(BattlePrototype value) { controller = value; }
    private Texture2D Icon(bool attack)
    {
        var texture = new Texture2D(48, 48);
        var pixels = new Color[48 * 48];
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
        {
            bool draw = attack ? (Mathf.Abs(x - y) < 3 && x > 13 && x < 42) || (Mathf.Abs(x + y - 30) < 3 && x > 5 && x < 24) || (x < 15 && y < 15 && Mathf.Abs(x - y) < 4)
                : (x > 8 && x < 35 && Mathf.Abs(y - 24) < 3) || (x > 23 && x < 39 && Mathf.Abs(Mathf.Abs(y - 24) - (x - 23)) < 3);
            pixels[y * 48 + x] = draw ? new Color(.89f, .78f, .48f) : Color.clear;
        }
        texture.SetPixels(pixels); texture.Apply(); return texture;
    }
    private void Styles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
        text = new GUIStyle(GUI.skin.label) { fontSize = 17 };
        small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        button = new GUIStyle(GUI.skin.button) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = text.normal.textColor = small.normal.textColor = ink;
        resultTitle = new GUIStyle(title) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
        resultText = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
        moveIcon = Icon(false); attackIcon = Icon(true);
    }
    private void Fill(Rect rect, Color color)
    { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
    private void Label(float x, float y, float w, string value, GUIStyle style)
    { GUI.Label(new Rect(x, y, w, 28), value, style); }
    private void Stats(float x, string name, BattleUnit unit, Color color, bool visible)
    {
        Label(x, 8, 350, name, text);
        Label(x, 34, 350, "ХП " + (visible ? unit.HP.ToString() : "?") + "/" + unit.MaxHP + "   Урон " + unit.Damage + "   ОД " + (visible ? unit.AP.ToString() : "?") + "/" + unit.MaxAP, small);
        Fill(new Rect(x, 63, 300, 4), new Color(.16f, .18f, .19f));
        if (visible) Fill(new Rect(x, 63, 300 * (float)unit.HP / unit.MaxHP, 4), color);
    }
    private void OnGUI()
    {
        if (controller == null || controller.Battle == null || controller.InventoryOpen) return;
        Styles(); var battle = controller.Battle;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        Fill(new Rect(0, 0, 1280, 144), Panel);
        Fill(new Rect(0, 540, 1280, 180), Panel);
        Fill(new Rect(0, 143, 1280, 1), new Color(.4f, .35f, .23f));
        Fill(new Rect(0, 540, 1280, 1), new Color(.4f, .35f, .23f));
        Label(20, 10, 460, "ПЕПЕЛЬНАЯ КЛЯТВА", title);
        Label(20, 40, 460, battle.Rules.Width + "×" + battle.Rules.Height + " гексов   /   Раунд " + battle.Round + "   /   " + (battle.Finished ? "Бой окончен" : battle.PlayerTurn ? "Ваш ход" : "Ход врага"), small);
        Stats(520, controller.Character.Name, battle.Player, new Color(.35f, .72f, .8f), true);
        Label(890, 8, 370, "ОЧЕРЕДЬ: отряд → враги", text);
        Label(890, 34, 370, battle.Enemies.Count > 2 ? "Врагов на поле: " + battle.Enemies.Count : "Мечник → лучник → новый раунд", small);
        if (battle.Finished && !controller.TestToolsActive)
        {
            DrawResult(battle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DrawTestMenu();
#endif
            GUI.matrix = oldMatrix;
            return;
        }
        DrawRoster(battle);
        Label(22, 553, 720, battle.Message, small);
        GUI.enabled = controller.CanAct;
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = controller.MovingMode ? new Color(.4f, .6f, .53f) : Color.white;
        if (GUI.Button(new Rect(20, 590, 230, 62), new GUIContent("    Перемещение [1]", moveIcon), button)) controller.SelectMove();
        GUI.backgroundColor = old;
        if (GUI.Button(new Rect(262, 590, 220, 62), new GUIContent("    Атака [2]\n    " + battle.Rules.AttackCost + " ОД", attackIcon), button)) controller.PlayerAttack();
        if (GUI.Button(new Rect(494, 590, 240, 62), "Завершить ход бойца\n[Пробел]", button)) controller.EndTurn();
        GUI.enabled = !controller.Busy && !controller.TestToolsActive && battle.PlayerTurn && !battle.Finished;
        if (GUI.Button(new Rect(970, 554, 290, 28), "Завершить ход всего отряда", small)) controller.EndSquadTurn();
        GUI.enabled = !controller.InventoryOpen;
        if (GUI.Button(new Rect(970, 590, 135, 29), "К бойцу [F]", small)) controller.FocusPlayer();
        if (GUI.Button(new Rect(1115, 590, 145, 29), "Вся карта [G]", small)) controller.Overview();
        if (GUI.Button(new Rect(970, 626, 135, 26), "Начать заново [R]", small)) controller.Restart();
        CameraControls cameraControls = controller.CameraInput.Controls;
        GUI.backgroundColor = cameraControls.SettingsOpen ? Gold : old;
        if (GUI.Button(new Rect(1115, 626, 145, 26), "Камера", small))
            cameraControls.SettingsOpen = !cameraControls.SettingsOpen;
        GUI.backgroundColor = old;
        GUI.enabled = !controller.TestToolsActive;
        if (GUI.Button(new Rect(750, 553, 210, 29), "Инвентарь [Tab]", small)) controller.SetInventoryOpen(true);
        GUI.enabled = true;
        GUI.Label(new Rect(750, 588, 210, 68), "Земля: " + battle.Rules.MoveCost + " ОД\nПодъём: +" + battle.Rules.ClimbExtra + " ОД\nБолото: ×" + battle.Rules.SwampMultiplier, small);
        string hover = "Наведите курсор на видимый гекс — появится описание и цена пути.";
        if (controller.Hovered.HasValue)
        {
            Hex p = controller.Hovered.Value; var tile = battle.Tile(p); int cost;
            hover = "[" + p + "] " + (tile.Terrain == Terrain.Ground ? "Земля" : tile.Terrain == Terrain.Hill ? "Возвышенность" : "Болото");
            if (tile.Obstacle != Obstacle.None) hover += tile.Obstacle == Obstacle.Tree ? " · Дерево: проход закрыт" : " · Большой камень: проход закрыт";
            else if (battle.UnitAt(p) != null) hover += " · " + battle.UnitAt(p).Inventory.Name;
            else if (controller.Reachable.Cost.TryGetValue(p, out cost)) hover += " · Путь: " + cost + " ОД";
        }
        Label(20, 663, 1240, hover, small);
        Label(20, 689, 1240, "Камера: края экрана / WASD / СКМ / колесо   •   Обзор: " + battle.Rules.Vision + " клеток   •   Атака выбранного: " + battle.Player.MinRange + "–" + battle.Player.MaxRange + " клеток   •   ЛКМ по союзнику — выбор", small);
        if (cameraControls.SettingsOpen) cameraControls.DrawSettings(CameraController.SettingsPanel, Gold, Panel, text);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DrawTestMenu();
#endif
        GUI.matrix = oldMatrix;
        GUI.enabled = true;
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DrawTestMenu()
    {
        if (GUI.Button(new Rect(338, 12, 170, 29), "Тестирование [F8]", small)) controller.ToggleTestMenu();
        if (!controller.TestToolsActive) return;

        if (controller.TestMenuOpen)
        {
            Fill(new Rect(20, 159, 392, 210), new Color(.43f, .36f, .23f));
            Fill(new Rect(21, 160, 390, 208), new Color(.075f, .087f, .096f));
            Label(36, 170, 360, "Тестирование боя", title);
            Label(36, 205, 360, "Добавить противника", small);
            TestChoice(36, 226, "Мечник", BattlePrototype.TestTool.Swordsman);
            TestChoice(212, 226, "Лучник", BattlePrototype.TestTool.Archer);
            Label(36, 262, 360, "Добавить объект", small);
            TestChoice(36, 283, "Дерево", BattlePrototype.TestTool.Tree);
            TestChoice(212, 283, "Камень", BattlePrototype.TestTool.Rock);
            if (GUI.Button(new Rect(36, 327, 344, 28), "Убрать объект", button))
                controller.SelectTestTool(BattlePrototype.TestTool.Remove);
            return;
        }

        string action;
        switch (controller.CurrentTestTool)
        {
            case BattlePrototype.TestTool.Swordsman: action = "Мечник"; break;
            case BattlePrototype.TestTool.Archer: action = "Лучник"; break;
            case BattlePrototype.TestTool.Tree: action = "Дерево"; break;
            case BattlePrototype.TestTool.Rock: action = "Камень"; break;
            default: action = "Удаление"; break;
        }
        Fill(new Rect(15, 550, 722, 32), new Color(.15f, .16f, .15f));
        Label(22, 553, 709, action + "  ·  " + controller.TestHint + "  ·  Esc — отмена", small);
    }
    private void TestChoice(int x, int y, string caption, BattlePrototype.TestTool tool)
    {
        if (GUI.Button(new Rect(x, y, 168, 29), caption, button)) controller.SelectTestTool(tool);
    }
#endif
    private void DrawResult(BattleModel battle)
    {
        Color accent = battle.AlliesAlive ? new Color(.93f, .77f, .42f) : new Color(.91f, .48f, .42f);
        Fill(new Rect(0, 0, 1280, 720), new Color(.025f, .03f, .035f, .65f));
        Fill(new Rect(400, 253, 480, 214), accent);
        Fill(new Rect(402, 255, 476, 210), Panel);
        resultTitle.normal.textColor = accent;
        GUI.Label(new Rect(420, 271, 440, 57), battle.AlliesAlive ? "Победа" : "Поражение", resultTitle);
        GUI.Label(new Rect(420, 332, 440, 30), battle.Message, resultText);
        if (GUI.Button(new Rect(500, 386, 280, 50), "Продолжить путешествие", button))
            controller.ContinueJourney();
    }

    private void DrawRoster(BattleModel battle)
    {
        int index = 0;
        foreach (var unit in battle.Units)
        {
            if (index >= 5) break;
            bool visible = !unit.IsEnemy || battle.Tile(unit.Position).Visible;
            string state = !visible ? "в тумане" : !unit.Alive ? "погиб" : unit.TurnEnded ? "ход завершён" : unit == battle.ActiveEnemy ? "ходит сейчас" : "готов";
            string caption = (unit == battle.Player ? "► " : "") + (unit.IsEnemy ? "ВРАГ · " : "") + unit.RoleName;
            caption += " · " + state + "\n" + (visible ? "ХП " + unit.HP + "/" + unit.MaxHP + "   ОД " + unit.AP + "/" + unit.MaxAP : "ХП ?   ОД ?");
            GUI.enabled = !unit.IsEnemy && !controller.Busy && !controller.TestToolsActive;
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = unit == battle.Player ? new Color(.65f, .55f, .25f) : Color.white;
            if (GUI.Button(new Rect(20 + index * 250, 78, 240, 55), caption, small)) controller.SelectUnit(unit);
            GUI.backgroundColor = old; index++;
        }
        GUI.enabled = true;
    }
    private void OnDestroy() { if (moveIcon != null) Destroy(moveIcon); if (attackIcon != null) Destroy(attackIcon); }
}
