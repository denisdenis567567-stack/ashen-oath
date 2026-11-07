#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using AshenOath;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class BattlePrototype
{
    public enum TestTool { None, Swordsman, Archer, Tree, Rock, Remove }

    private readonly HashSet<BattleUnit> removableTestEnemies = new HashSet<BattleUnit>();
    private readonly HashSet<Hex> removableTestObstacles = new HashSet<Hex>();
    public bool TestMenuOpen { get; private set; }
    public TestTool CurrentTestTool { get; private set; }
    public bool TestToolsActive => TestMenuOpen || CurrentTestTool != TestTool.None;
    public string TestHint { get; private set; }

    private void ResetTestTools()
    {
        removableTestEnemies.Clear();
        removableTestObstacles.Clear();
        foreach (var cell in settings.testRemovableCells ?? System.Array.Empty<Vector2Int>())
        {
            var hex = new Hex(cell.x, cell.y);
            if (!Battle.Inside(hex)) continue;
            var unit = Battle.UnitAt(hex);
            if (unit != null && unit.IsEnemy) removableTestEnemies.Add(unit);
            if (Battle.Tile(hex).Obstacle != Obstacle.None) removableTestObstacles.Add(hex);
        }
        CloseTestTools();
    }

    public void ToggleTestMenu()
    {
        if (InventoryOpen) return;
        bool open = !TestMenuOpen;
        CloseTestTools();
        TestMenuOpen = open;
        Refresh();
    }

    public void SelectTestTool(TestTool tool)
    {
        if (!TestMenuOpen) return;
        TestMenuOpen = false;
        CurrentTestTool = tool;
        TestHint = null;
    }

    private void CloseTestTools()
    {
        TestMenuOpen = false;
        CurrentTestTool = TestTool.None;
        TestHint = null;
        Hovered = null;
        view.ShowTestTarget(null, Color.clear);
    }

    private bool HandleTestInput(Keyboard keyboard, Mouse mouse)
    {
        if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
        { ToggleTestMenu(); return true; }
        if (!TestToolsActive) return false;

        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) { CloseTestTools(); Refresh(); return true; }
            if (keyboard.rKey.wasPressedThisFrame) { Restart(); return true; }
            if (keyboard.fKey.wasPressedThisFrame) FocusPlayer();
            if (keyboard.gKey.wasPressedThisFrame) Overview();
        }
        if (TestMenuOpen) return true;

        bool removing = CurrentTestTool == TestTool.Remove;
        Hovered = mouse == null ? null : VisibleHexAt(mouse.position.ReadValue());
        var unit = Hovered.HasValue ? Battle.UnitAt(Hovered.Value) : null;
        bool valid = !Busy && Battle.PlayerTurn && Hovered.HasValue &&
            (removing
                ? (unit != null ? removableTestEnemies.Contains(unit) : removableTestObstacles.Contains(Hovered.Value))
                : Battle.CanPlaceTestItem(Hovered.Value));
        view.ShowTestTarget(valid ? Hovered : null, removing ? new Color(1f, .3f, .22f, .78f) : new Color(.34f, .92f, .65f, .7f));
        TestHint = Busy || !Battle.PlayerTurn ? "Дождитесь своего хода."
            : removing ? (valid ? "ЛКМ — убрать подсвеченную цель." : "Наведите курсор на добавленный или разрешённый объект.")
            : (valid ? "ЛКМ — поставить на подсвеченный гекс." : "Выберите свободный видимый гекс.");

        if (!valid || !mouse.leftButton.wasPressedThisFrame) return true;

        Hex position = Hovered.Value;
        if (removing)
        {
            if (unit != null && Battle.RemoveTestEnemy(unit))
            {
                removableTestEnemies.Remove(unit);
                view.RemoveTestUnit(unit);
            }
            else if (unit == null && Battle.SetTestObstacle(position, Obstacle.None))
                removableTestObstacles.Remove(position);
        }
        else if (CurrentTestTool == TestTool.Tree || CurrentTestTool == TestTool.Rock)
        {
            if (Battle.SetTestObstacle(position, CurrentTestTool == TestTool.Tree ? Obstacle.Tree : Obstacle.Rock))
                removableTestObstacles.Add(position);
        }
        else
        {
            var enemy = Battle.AddTestEnemy(position, CurrentTestTool == TestTool.Archer ? UnitRole.Archer : UnitRole.Swordsman);
            if (enemy != null)
            {
                removableTestEnemies.Add(enemy);
                view.AddTestUnit(enemy);
            }
        }
        view.ShowTestTarget(null, Color.clear);
        Refresh();
        return true;
    }
}
#endif
