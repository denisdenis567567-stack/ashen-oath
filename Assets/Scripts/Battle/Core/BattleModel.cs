using System;
using System.Collections.Generic;
using AshenOath;

// Чистые правила боя: не зависят от камеры, интерфейса и Unity.
public class BattleRules
{
    public int Width = 32, Height = 32, Seed = 42;
    public float ObstacleDensity = .12f;
    public int PlayerHP = 30, EnemyHP = 15, PlayerDamage = 10, EnemyDamage = 10;
    public int PlayerAP = 8, EnemyAP = 8, Vision = 5;
    public int ShieldHP = 60, ShieldDamage = 10, ShieldAP = 8;
    public int ArcherHP = 15, ArcherDamage = 10, ArcherAP = 8;
    public int ArcherMinRange = 2, ArcherMaxRange = 4;
    public int MoveCost = 1, ClimbExtra = 1, SwampMultiplier = 2, AttackCost = 4;
}

public enum Terrain { Ground, Hill, Swamp }
public enum Obstacle { None, Rock, Tree }
public class BattleTile
{
    public Terrain Terrain;
    public Obstacle Obstacle;
    public bool Visible, Explored;
    public int Elevation { get { return Terrain == Terrain.Hill ? 1 : 0; } }
}
public enum UnitRole { Swordsman, Shield, Archer }
public class BattleUnit
{
    public Hex Position;
    public UnitRole Role;
    public bool IsEnemy, TurnEnded;
    public int MinRange = 1, MaxRange = 1;
    public readonly CharacterInventory Inventory = new CharacterInventory();
    public bool Alive { get { return HP > 0; } }
    public string RoleName { get { return Role == UnitRole.Shield ? "Щитовик" : Role == UnitRole.Archer ? "Лучник" : "Мечник"; } }
    public int HP, AP, MaxHP, MaxAP, Damage;
    public BattleUnit(Hex p, int hp, int ap, int damage)
    { Position = p; HP = MaxHP = hp; AP = MaxAP = ap; Damage = damage; }
}
public class BattlePaths
{
    public readonly Dictionary<Hex, int> Cost = new Dictionary<Hex, int>();
    public readonly Dictionary<Hex, Hex> Previous = new Dictionary<Hex, Hex>();
    public List<Hex> To(Hex destination)
    {
        var path = new List<Hex>();
        if (!Cost.ContainsKey(destination)) return path;
        while (Previous.ContainsKey(destination)) { path.Add(destination); destination = Previous[destination]; }
        path.Reverse(); return path;
    }
}

public class BattleModel
{
    public readonly BattleRules Rules;
    public readonly BattleTile[,] Tiles;
    public readonly List<BattleUnit> Allies = new List<BattleUnit>();
    public readonly List<BattleUnit> Enemies = new List<BattleUnit>();
    public BattleUnit Player { get; private set; }
    public BattleUnit Enemy { get { return Enemies[0]; } }
    public BattleUnit ActiveEnemy { get; private set; }
    public IEnumerable<BattleUnit> Units { get { foreach (var u in Allies) yield return u; foreach (var u in Enemies) yield return u; } }
    public bool AlliesAlive { get { return Allies.Exists(u => u.Alive); } }
    public bool EnemiesAlive { get { return Enemies.Exists(u => u.Alive); } }
    public bool PlayerTurn = true;
    public int Round = 1;
    public bool Finished { get { return !AlliesAlive || !EnemiesAlive; } }
    public string Message = "Нажмите «Перемещение», затем выберите подсвеченный гекс.";

    public BattleModel(BattleRules rules)
    {
        Rules = rules;
        Tiles = new BattleTile[rules.Width, rules.Height];
        int x = rules.Width / 2 - 4, y = rules.Height / 2;
        Allies.Add(CreateUnit(new Hex(x, y), UnitRole.Swordsman, false, rules.PlayerHP, rules.PlayerAP, rules.PlayerDamage));
        Allies.Add(CreateUnit(new Hex(x, y + 1), UnitRole.Shield, false, rules.ShieldHP, rules.ShieldAP, rules.ShieldDamage));
        Allies.Add(CreateUnit(new Hex(x - 1, y), UnitRole.Archer, false, rules.ArcherHP, rules.ArcherAP, rules.ArcherDamage));
        Enemies.Add(CreateUnit(new Hex(x + 8, y), UnitRole.Swordsman, true, rules.EnemyHP, rules.EnemyAP, rules.EnemyDamage));
        Enemies.Add(CreateUnit(new Hex(x + 9, y + 1), UnitRole.Archer, true, rules.ArcherHP, rules.ArcherAP, rules.ArcherDamage));
        Player = Allies[0];
        BattleMapGenerator.Generate(this);
        UpdateVision();
    }
    private BattleUnit CreateUnit(Hex p, UnitRole role, bool enemy, int hp, int ap, int damage)
    {
        var u = new BattleUnit(p, hp, ap, damage) { Role = role, IsEnemy = enemy };
        if (role == UnitRole.Archer) { u.MinRange = Rules.ArcherMinRange; u.MaxRange = Rules.ArcherMaxRange; }
        u.Inventory.Rename((enemy ? "Враг: " : "") + u.RoleName);
        return u;
    }
    public BattleUnit UnitAt(Hex p)
    { foreach (var u in Units) if (u.Alive && u.Position == p) return u; return null; }
    public bool SelectPlayer(BattleUnit unit)
    { if (!Allies.Contains(unit)) return false; Player = unit; return true; }
    public bool CanUse(BattleUnit u)
    {
        return !Finished && u != null && u.Alive && !u.TurnEnded &&
            (PlayerTurn ? Allies.Contains(u) && u == Player : Enemies.Contains(u) && u == ActiveEnemy);
    }
    public bool EndSelectedTurn()
    {
        if (!PlayerTurn || Finished || !Player.Alive || Player.TurnEnded) return false;
        Player.TurnEnded = true;
        var next = Allies.Find(u => u.Alive && !u.TurnEnded);
        if (next != null) { Player = next; Message = "Ходит " + next.Inventory.Name + ". Можно выбрать другого бойца."; }
        else BeginEnemyTurn();
        return true;
    }
    public bool Inside(Hex p) { return p.X >= 0 && p.Y >= 0 && p.X < Rules.Width && p.Y < Rules.Height; }
    public BattleTile Tile(Hex p) { return Tiles[p.X, p.Y]; }
    public int StepCost(Hex from, Hex to)
    {
        int cost = Rules.MoveCost * (Tile(to).Terrain == Terrain.Swamp ? Rules.SwampMultiplier : 1);
        if (Tile(to).Elevation > Tile(from).Elevation) cost += Rules.ClimbExtra;
        return cost;
    }
    // Дейкстра: находит самый дешёвый путь с учётом болота и подъёмов.
    public BattlePaths Paths(BattleUnit unit, bool visibleOnly)
    {
        var paths = new BattlePaths();
        var open = new List<Hex> { unit.Position };
        var closed = new HashSet<Hex>();
        paths.Cost[unit.Position] = 0;
        while (open.Count > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++) if (paths.Cost[open[i]] < paths.Cost[open[best]]) best = i;
            Hex current = open[best]; open.RemoveAt(best);
            if (!closed.Add(current)) continue;
            foreach (Hex next in current.Neighbors())
            {
                if (!Inside(next) || (UnitAt(next) != null && next != unit.Position) || Tile(next).Obstacle != Obstacle.None || closed.Contains(next)) continue;
                if (visibleOnly && !Tile(next).Visible) continue;
                int cost = paths.Cost[current] + StepCost(current, next);
                if (paths.Cost.ContainsKey(next) && paths.Cost[next] <= cost) continue;
                paths.Cost[next] = cost; paths.Previous[next] = current; open.Add(next);
            }
        }
        return paths;
    }
    public void UpdateVision()
    {
        for (int x = 0; x < Rules.Width; x++) for (int y = 0; y < Rules.Height; y++)
        {
            var tile = Tiles[x, y];
            tile.Visible = Allies.Exists(u => u.Alive && Hex.Distance(u.Position, new Hex(x, y)) <= Rules.Vision);
            tile.Explored |= tile.Visible;
        }
    }
    public bool MoveStep(BattleUnit unit, Hex to)
    {
        if (!CanUse(unit) || !Inside(to) || Hex.Distance(unit.Position, to) != 1) return false;
        if (Tile(to).Obstacle != Obstacle.None || UnitAt(to) != null) return false;
        if (!unit.IsEnemy && !Tile(to).Visible) return false;
        int cost = StepCost(unit.Position, to);
        if (unit.AP < cost) return false;
        unit.AP -= cost; unit.Position = to; UpdateVision(); return true;
    }
    public bool InAttackRange(BattleUnit attacker, Hex from, BattleUnit target)
    {
        int distance = Hex.Distance(from, target.Position);
        return target.Alive && attacker.IsEnemy != target.IsEnemy && distance >= attacker.MinRange && distance <= attacker.MaxRange;
    }
    public BattleUnit FindTarget(BattleUnit attacker)
    {
        BattleUnit best = null; int distance = int.MaxValue;
        foreach (var target in attacker.IsEnemy ? Allies : Enemies)
        {
            int d = Hex.Distance(attacker.Position, target.Position);
            if (InAttackRange(attacker, attacker.Position, target) && (attacker.IsEnemy || Tile(target.Position).Visible) && d < distance)
            { best = target; distance = d; }
        }
        return best;
    }
    public bool Attack(BattleUnit attacker) { return Attack(attacker, FindTarget(attacker)); }
    public bool Attack(BattleUnit attacker, BattleUnit target)
    {
        if (!CanUse(attacker)) return false;
        if (target == null || !(attacker.IsEnemy ? Allies : Enemies).Contains(target) ||
            !InAttackRange(attacker, attacker.Position, target) || (!attacker.IsEnemy && !Tile(target.Position).Visible))
        { Message = "Нет цели: дальность " + attacker.MinRange + "–" + attacker.MaxRange + " клеток."; return false; }
        if (attacker.AP < Rules.AttackCost) { Message = "Для атаки нужно " + Rules.AttackCost + " ОД."; return false; }
        attacker.AP -= Rules.AttackCost; target.HP = Math.Max(0, target.HP - attacker.Damage);
        Message = attacker.Inventory.Name + " → " + target.Inventory.Name + ": " + attacker.Damage + " урона.";
        if (!target.Alive) { target.TurnEnded = true; Message += " Боец погиб."; }
        if (Finished) Message = AlliesAlive ? "Победа! Все враги повержены." : "Поражение. Отряд погиб.";
        UpdateVision(); return true;
    }
    // ИИ ищет дешёвую позицию для своего оружия; лучник вблизи сначала отходит.
    public List<Hex> EnemyPath() { return EnemyPath(Enemy); }
    public List<Hex> EnemyPath(BattleUnit unit)
    {
        var paths = Paths(unit, false);
        Hex best = unit.Position; int cost = int.MaxValue;
        foreach (var entry in paths.Cost)
        {
            if (entry.Value >= cost) continue;
            foreach (var target in Allies)
                if (InAttackRange(unit, entry.Key, target)) { best = entry.Key; cost = entry.Value; break; }
        }
        return paths.To(best);
    }
    public void BeginEnemyTurn()
    {
        if (Finished || !PlayerTurn) return;
        PlayerTurn = false;
        foreach (var u in Allies) u.TurnEnded = true;
        foreach (var u in Enemies) { u.AP = u.Alive ? u.MaxAP : 0; u.TurnEnded = !u.Alive; }
        ActiveEnemy = Enemies.Find(u => u.Alive);
        Message = "Ход противников…";
    }
    public void EndEnemyUnitTurn()
    {
        if (PlayerTurn || ActiveEnemy == null) return;
        ActiveEnemy.TurnEnded = true;
        ActiveEnemy = Enemies.Find(u => u.Alive && !u.TurnEnded);
    }
    public void BeginPlayerTurn()
    {
        if (Finished || PlayerTurn || Enemies.Exists(u => u.Alive && !u.TurnEnded)) return;
        PlayerTurn = true; ActiveEnemy = null; Round++;
        foreach (var u in Allies) { u.AP = u.Alive ? u.MaxAP : 0; u.TurnEnded = !u.Alive; }
        Player = Allies.Find(u => u.Alive);
        Message = "Ваш ход. Выберите бойца: порядок свободный.";
    }
}
