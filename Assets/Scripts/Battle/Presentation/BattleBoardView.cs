using System.Collections.Generic;
using UnityEngine;
using AshenOath;

// Графика отделена от логики боя; спрайты можно заменить независимо.
public class BattleBoardView : MonoBehaviour
{
    private Transform root;
    private SpriteRenderer[,] ground, details, highlights;
    private readonly Dictionary<BattleUnit, SpriteRenderer> units = new Dictionary<BattleUnit, SpriteRenderer>();
    private Sprite shield, archer;
    private readonly List<Sprite> sprites = new List<Sprite>();
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private Sprite hex, rock, tree, soldier, hill, water;
    public static Vector3 World(Hex p) { return new Vector3(1.7320508f * (p.X + .5f * (p.Y & 1)), 1.5f * p.Y, 0); }
    public static Hex FromWorld(Vector3 p)
    {
        int row = Mathf.RoundToInt(p.y / 1.5f); Hex best = new Hex(); float distance = float.MaxValue;
        for (int y = row - 1; y <= row + 1; y++)
        {
            int column = Mathf.RoundToInt(p.x / 1.7320508f - .5f * (y & 1));
            for (int x = column - 1; x <= column + 1; x++)
            {
                Hex candidate = new Hex(x, y); float d = (World(candidate) - p).sqrMagnitude;
                if (d < distance) { distance = d; best = candidate; }
            }
        }
        return best;
    }
    private Sprite Shape(string shape)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
        var colors = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float px = (x + .5f - 64) / 64, py = (y + .5f - 64) / 64;
            float ax = Mathf.Abs(px); bool inside = false;
            switch (shape)
            {
                case "hex": inside = ax < .866f && Mathf.Abs(py) < 1 - ax / 1.732f; break;
                case "rock": inside = ax < .7f + py * .12f && py > -.48f && py < .6f - ax * .55f; break;
                case "tree": inside = (ax < .1f && py > -.7f && py < .4f) || (py > -.3f && py < .87f && ax < (.9f - py) * .57f); break;
                case "soldier": inside = (px * px + (py - .35f) * (py - .35f) < .13f) || (py > -.55f && py < .12f && ax < .48f - py * .15f); break;
                case "shield": inside = (py > -.7f && py < .65f && ax < .62f && ax < .85f + py * .5f); break;
                case "archer": inside = (Mathf.Abs(px - (.45f - py * py * .65f)) < .09f && Mathf.Abs(py) < .8f) || (Mathf.Abs(px + .05f) < .035f && Mathf.Abs(py) < .8f) || (Mathf.Abs(py) < .04f && ax < .8f); break;
                case "hill": inside = py > -.28f && py < .48f - ax * 1.2f; break;
                case "water": inside = ax < .62f && (Mathf.Abs(py + .22f) < .045f || Mathf.Abs(py - .03f) < .045f || Mathf.Abs(py - .28f) < .045f); break;
            }
            float shade = .8f + .2f * (py + 1) / 2;
            colors[y * size + x] = inside ? new Color(shade, shade, shade, 1) : Color.clear;
        }
        texture.SetPixels(colors); texture.Apply(); textures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64);
        sprites.Add(sprite); return sprite;
    }
    private SpriteRenderer Make(string label, Sprite sprite, Vector3 p, float scale, int order)
    {
        var item = new GameObject(label); item.transform.SetParent(root, false);
        item.transform.localPosition = p; item.transform.localScale = Vector3.one * scale;
        var renderer = item.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order; return renderer;
    }
    public void Build(BattleModel battle)
    {
        if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
        if (hex == null) { hex = Shape("hex"); rock = Shape("rock"); tree = Shape("tree"); soldier = Shape("soldier"); hill = Shape("hill"); water = Shape("water"); }
        if (shield == null) { shield = Shape("shield"); archer = Shape("archer"); }
        units.Clear();
        root = new GameObject("Battle board — graphics").transform; root.SetParent(transform, false);
        int w = battle.Rules.Width, h = battle.Rules.Height;
        ground = new SpriteRenderer[w, h]; details = new SpriteRenderer[w, h]; highlights = new SpriteRenderer[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++)
        {
            Vector3 p = World(new Hex(x, y));
            ground[x, y] = Make("Hex " + x + "," + y, hex, p, .96f, 0);
            highlights[x, y] = Make("Movement overlay", hex, p, .87f, 1);
            details[x, y] = Make("Terrain / obstacle", hill, p, .7f, 2);
        }
        foreach (var unit in battle.Units) AddUnit(unit);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        testHighlight = Make("Test target", hex, Vector3.zero, .92f, 3);
        testHighlight.enabled = false;
#endif
    }
    private void AddUnit(BattleUnit unit)
    {
        var renderer = Make(unit.Inventory.Name, unit.Role == UnitRole.Shield ? shield : unit.Role == UnitRole.Archer ? archer : soldier, World(unit.Position), .8f, 4);
        units.Add(unit, renderer);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private SpriteRenderer testHighlight;
    public void AddTestUnit(BattleUnit unit) { AddUnit(unit); }
    public void RemoveTestUnit(BattleUnit unit)
    {
        if (!units.TryGetValue(unit, out var renderer)) return;
        units.Remove(unit);
        renderer.enabled = false;
        Destroy(renderer.gameObject);
    }
    public void ShowTestTarget(Hex? position, Color color)
    {
        if (testHighlight == null) return;
        testHighlight.enabled = position.HasValue;
        if (!position.HasValue) return;
        testHighlight.transform.localPosition = World(position.Value);
        testHighlight.color = color;
    }
#endif
    public void Refresh(BattleModel battle, BattlePaths paths, bool moveMode, Hex? hovered, bool attackMode = false)
    {
        var preview = new HashSet<Hex>();
        if (moveMode && hovered.HasValue && paths.Cost.ContainsKey(hovered.Value) && paths.Cost[hovered.Value] <= battle.Player.AP)
            preview.UnionWith(paths.To(hovered.Value));
        for (int x = 0; x < battle.Rules.Width; x++) for (int y = 0; y < battle.Rules.Height; y++)
        {
            Hex p = new Hex(x, y); var tile = battle.Tile(p);
            Color color = tile.Terrain == Terrain.Ground ? new Color(.36f, .32f, .25f) : tile.Terrain == Terrain.Hill ? new Color(.6f, .5f, .31f) : new Color(.19f, .36f, .33f);
            color *= 1 + ((x * 13 + y * 7) % 5 - 2) * .022f; color.a = 1;
            ground[x, y].color = tile.Visible ? color : tile.Explored ? Color.Lerp(new Color(.06f, .07f, .08f), color, .22f) : new Color(.025f, .03f, .04f);
            var detail = details[x, y]; detail.enabled = tile.Explored;
            if (tile.Obstacle == Obstacle.Tree) { detail.sprite = tree; detail.color = new Color(.16f, .25f, .17f); }
            else if (tile.Obstacle == Obstacle.Rock) { detail.sprite = rock; detail.color = new Color(.55f, .57f, .57f); }
            else if (tile.Terrain == Terrain.Hill) { detail.sprite = hill; detail.color = new Color(.77f, .67f, .43f); }
            else if (tile.Terrain == Terrain.Swamp) { detail.sprite = water; detail.color = new Color(.38f, .59f, .55f); }
            else detail.enabled = false;
            if (!tile.Visible) detail.color *= new Color(.25f, .25f, .25f, 1);
            int cost; bool reachable = moveMode && tile.Visible && paths.Cost.TryGetValue(p, out cost) && cost > 0 && cost <= battle.Player.AP;
            bool selected = p == battle.Player.Position;
            bool attackRange = attackMode && battle.CanUse(battle.Player) && tile.Visible &&
                Hex.Distance(battle.Player.Position, p) >= battle.Player.MinRange && Hex.Distance(battle.Player.Position, p) <= battle.Player.MaxRange;
            highlights[x, y].enabled = reachable || selected || attackRange;
            highlights[x, y].color = selected ? new Color(.95f, .73f, .29f, .75f) : attackRange ? new Color(.85f, .3f, .2f, .3f) : preview.Contains(p) ? new Color(.55f, .9f, .85f, .7f) : new Color(.36f, .62f, .53f, .28f);
        }
        foreach (var entry in units)
        {
            var unit = entry.Key; var renderer = entry.Value;
            renderer.transform.position = World(unit.Position);
            renderer.enabled = unit.Alive && (!unit.IsEnemy || battle.Tile(unit.Position).Visible);
            Color color = unit.IsEnemy ? new Color(.95f, .35f, .25f) : new Color(.4f, .83f, .95f);
            renderer.color = unit.TurnEnded ? Color.Lerp(color, Color.gray, .6f) : color;
        }
    }
    private void OnDestroy()
    { foreach (var sprite in sprites) if (sprite != null) Destroy(sprite); foreach (var texture in textures) if (texture != null) Destroy(texture); }
}
