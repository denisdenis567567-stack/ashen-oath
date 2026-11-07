using System;
using AshenOath;

public static class BattleMapGenerator
{
    public static void Generate(BattleModel battle)
    {
        var rules = battle.Rules;
        var random = new Random(rules.Seed);
        for (int x = 0; x < rules.Width; x++) for (int y = 0; y < rules.Height; y++)
        {
            battle.Tiles[x, y] = new BattleTile();
            if (random.NextDouble() < rules.ObstacleDensity)
                battle.Tiles[x, y].Obstacle = random.Next(2) == 0 ? Obstacle.Rock : Obstacle.Tree;
        }
        // Связные участки местности вместо случайной россыпи отдельных клеток.
        int patches = Math.Max(6, rules.Width * rules.Height / 90);
        for (int i = 0; i < patches; i++)
        {
            Hex center = new Hex(random.Next(rules.Width), random.Next(rules.Height));
            int radius = random.Next(2, 5);
            for (int x = 0; x < rules.Width; x++) for (int y = 0; y < rules.Height; y++)
                if (Hex.Distance(center, new Hex(x, y)) <= radius)
                    battle.Tiles[x, y].Terrain = i % 2 == 0 ? Terrain.Hill : Terrain.Swamp;
        }
        // Гарантируем проходимое соединение стартов, не убирая разную цену земли.
        Hex current = battle.Player.Position;
        while (current != battle.Enemy.Position)
        {
            battle.Tile(current).Obstacle = Obstacle.None;
            foreach (Hex next in current.Neighbors())
                if (battle.Inside(next) && Hex.Distance(next, battle.Enemy.Position) < Hex.Distance(current, battle.Enemy.Position))
                { current = next; break; }
        }
        battle.Tile(current).Obstacle = Obstacle.None;
        battle.Tile(battle.Player.Position).Terrain = Terrain.Ground;
        battle.Tile(battle.Enemy.Position).Terrain = Terrain.Ground;
        // Наглядные примеры трёх поверхностей и двух препятствий рядом со стартом.
        Set(battle, 1, 1, Terrain.Hill, Obstacle.None);
        Set(battle, 2, 1, Terrain.Hill, Obstacle.None);
        Set(battle, 1, -1, Terrain.Swamp, Obstacle.None);
        Set(battle, 2, -1, Terrain.Swamp, Obstacle.None);
        Set(battle, -1, 1, Terrain.Ground, Obstacle.Tree);
        Set(battle, -1, -1, Terrain.Ground, Obstacle.Rock);
        // Соединяем старты всех бойцов с центральным проходом.
        foreach (var unit in battle.Units)
        {
            current = unit.Position;
            while (current != battle.Player.Position)
            {
                battle.Tile(current).Obstacle = Obstacle.None;
                foreach (Hex next in current.Neighbors())
                    if (battle.Inside(next) && Hex.Distance(next, battle.Player.Position) < Hex.Distance(current, battle.Player.Position))
                    { current = next; break; }
            }
            battle.Tile(unit.Position).Obstacle = Obstacle.None;
            battle.Tile(unit.Position).Terrain = Terrain.Ground;
        }
    }
    private static void Set(BattleModel battle, int dx, int dy, Terrain terrain, Obstacle obstacle)
    {
        Hex p = new Hex(battle.Player.Position.X + dx, battle.Player.Position.Y + dy);
        if (!battle.Inside(p)) return;
        battle.Tile(p).Terrain = terrain; battle.Tile(p).Obstacle = obstacle;
    }
}
