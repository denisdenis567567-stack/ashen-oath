using UnityEngine;

// Все изменяемые числа находятся здесь. Настраивайте asset через Inspector.
[CreateAssetMenu(fileName = "BattleSettings", menuName = "Пепельная клятва/Настройки боя")]
public class BattleSettings : ScriptableObject
{
    [Header("Карта")]
    [InspectorName("Ширина, гексов"), Range(12, 64)] public int width = 32;
    [InspectorName("Высота, гексов"), Range(12, 64)] public int height = 32;
    [InspectorName("Зерно генерации")] public int seed = 42;
    [InspectorName("Доля препятствий"), Range(0, .35f)] public float obstacleDensity = .12f;
    [Header("Мечник игрока")]
    [InspectorName("Здоровье"), Min(1)] public int playerHP = 30;
    [InspectorName("Урон"), Min(1)] public int playerDamage = 10;
    [InspectorName("ОД за ход"), Min(1)] public int playerAP = 8;
    [Header("Щитовик игрока")]
    [InspectorName("Здоровье"), Min(1)] public int shieldHP = 60;
    [InspectorName("Урон"), Min(1)] public int shieldDamage = 10;
    [InspectorName("ОД за ход"), Min(1)] public int shieldAP = 8;
    [Header("Лучники обеих сторон")]
    [InspectorName("Здоровье"), Min(1)] public int archerHP = 15;
    [InspectorName("Урон"), Min(1)] public int archerDamage = 10;
    [InspectorName("ОД за ход"), Min(1)] public int archerAP = 8;
    [InspectorName("Минимальная дальность"), Min(1)] public int archerMinRange = 2;
    [InspectorName("Максимальная дальность"), Min(1)] public int archerMaxRange = 4;
    [Header("Мечник врага")]
    [InspectorName("Здоровье"), Min(1)] public int enemyHP = 15;
    [InspectorName("Урон"), Min(1)] public int enemyDamage = 10;
    [InspectorName("ОД за ход"), Min(1)] public int enemyAP = 8;
    [Header("Правила")]
    [InspectorName("Радиус видимости"), Range(1, 20)] public int vision = 5;
    [InspectorName("ОД за шаг"), Min(1)] public int moveCost = 1;
    [InspectorName("Доплата за подъём"), Min(0)] public int climbExtra = 1;
    [InspectorName("Множитель болота"), Min(1)] public int swampMultiplier = 2;
    [InspectorName("ОД за атаку"), Min(1)] public int attackCost = 4;
    [Header("Отображение")]
    [InspectorName("Пауза между действиями, сек"), Range(.05f, 1)] public float actionDelay = .18f;

    public BattleRules Rules()
    {
        return new BattleRules {
            Width = Mathf.Clamp(width, 12, 64), Height = Mathf.Clamp(height, 12, 64), Seed = seed,
            ObstacleDensity = Mathf.Clamp(obstacleDensity, 0, .35f),
            PlayerHP = Mathf.Max(1, playerHP), EnemyHP = Mathf.Max(1, enemyHP),
            PlayerDamage = Mathf.Max(1, playerDamage), EnemyDamage = Mathf.Max(1, enemyDamage),
            PlayerAP = Mathf.Max(1, playerAP), EnemyAP = Mathf.Max(1, enemyAP),
            ShieldHP = Mathf.Max(1, shieldHP), ShieldDamage = Mathf.Max(1, shieldDamage), ShieldAP = Mathf.Max(1, shieldAP),
            ArcherHP = Mathf.Max(1, archerHP), ArcherDamage = Mathf.Max(1, archerDamage), ArcherAP = Mathf.Max(1, archerAP),
            ArcherMinRange = Mathf.Max(1, archerMinRange), ArcherMaxRange = Mathf.Max(Mathf.Max(1, archerMinRange), archerMaxRange),
            Vision = Mathf.Max(1, vision), MoveCost = Mathf.Max(1, moveCost),
            ClimbExtra = Mathf.Max(0, climbExtra), SwampMultiplier = Mathf.Max(1, swampMultiplier),
            AttackCost = Mathf.Max(1, attackCost)
        };
    }
}
