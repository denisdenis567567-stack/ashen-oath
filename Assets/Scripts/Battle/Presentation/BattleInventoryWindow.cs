using System.Collections.Generic;
using UnityEngine;

// Компоновка по макету: обоз слева, персонаж справа, ресурсы отдельной полосой.
public class BattleInventoryWindow : MonoBehaviour
{
    private BattlePrototype controller;
    private GUIStyle heading, section, label, muted, slot, field, action;
    private bool wasOpen, editing, skills;
    private CharacterInventory shownInventory;
    private string draft = "", error = "";
    private int selected = -1;
    private bool fromBag;
    private Vector2 bagScroll, filterScroll, statusScroll;
    private InventoryCategory filter = InventoryCategory.All;
    private readonly Dictionary<InventoryCategory, Texture2D> icons = new Dictionary<InventoryCategory, Texture2D>();
    // Добавление категории не требует изменения геометрии панели.
    private static readonly InventoryCategory[] Filters = { InventoryCategory.All, InventoryCategory.Weapons, InventoryCategory.Armor, InventoryCategory.Artifacts, InventoryCategory.Consumables };
    private static readonly string[] FilterNames = { "Все", "Оружие", "Броня", "Артефакты", "Расходники" };
    private readonly Color panel = new Color(.10f, .12f, .13f), tile = new Color(.16f, .19f, .20f),
        line = new Color(.29f, .32f, .32f), gold = new Color(.79f, .68f, .44f);
    public void Initialize(BattlePrototype value) { controller = value; }
    private void Styles()
    {
        if (heading != null) return;
        heading = new GUIStyle(GUI.skin.label) { fontSize = 23, richText = false };
        section = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        label = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = false };
        muted = new GUIStyle(label) { fontSize = 13 };
        slot = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, richText = false, alignment = TextAnchor.MiddleCenter };
        action = new GUIStyle(slot) { fontSize = 14 };
        field = new GUIStyle(GUI.skin.textField) { fontSize = 17, richText = false };
        heading.normal.textColor = label.normal.textColor = new Color(.92f, .91f, .86f);
        muted.normal.textColor = new Color(.70f, .74f, .74f);
        section.normal.textColor = gold;
        foreach (var category in Filters) icons[category] = MakeIcon(category);
    }
    private Texture2D MakeIcon(InventoryCategory category)
    {
        var texture = new Texture2D(24, 24) { filterMode = FilterMode.Point };
        var pixels = new Color[576];
        for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
        {
            bool draw;
            switch (category)
            {
                case InventoryCategory.Weapons: draw = (Mathf.Abs(x-y)<2 && x>5 && x<21) || (Mathf.Abs(x+y-13)<2 && x<12 && x>2) || (x<6 && y<6); break;
                case InventoryCategory.Armor: draw = y>3 && y<21 && x>3 && x<21 && (y>15 || Mathf.Abs(x-12)<y/2+2) && (x<6 || x>18 || y<6 || y>18); break;
                case InventoryCategory.Artifacts: draw = Mathf.Abs(Mathf.Abs(x-12)+Mathf.Abs(y-12)-9)<2; break;
                case InventoryCategory.Consumables: draw = (y>15 && y<22 && x>9 && x<15) || (y>3 && y<16 && x>5 && x<19 && (x<8 || x>16 || y<6 || y>13)); break;
                default: draw = x>3 && x<21 && y>3 && y<21 && (x<10 || x>13) && (y<10 || y>13); break;
            }
            pixels[y*24+x] = draw ? gold : Color.clear;
        }
        texture.SetPixels(pixels); texture.Apply(); return texture;
    }
    private void Fill(float x, float y, float w, float h, Color color)
    { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(new Rect(x,y,w,h), Texture2D.whiteTexture); GUI.color = old; }
    private void Text(float x, float y, float w, string value, GUIStyle style, float h = 26)
    { GUI.Label(new Rect(x,y,w,h), value, style); }
    private void SaveName()
    {
        if (controller.RenameCharacter(draft)) { editing = false; error = ""; GUI.FocusControl(null); }
        else error = "Имя: от 1 до 24 символов.";
    }
    private void OnGUI()
    {
        if (controller == null || controller.Battle == null) return;
        if (!controller.InventoryOpen) { wasOpen = false; return; }
        if (!wasOpen || shownInventory != controller.Character)
        { wasOpen = true; shownInventory = controller.Character; editing = skills = false; error = ""; selected = -1; bagScroll = statusScroll = Vector2.zero; }
        Styles();
        Matrix4x4 oldMatrix = GUI.matrix; int oldDepth = GUI.depth; bool oldEnabled = GUI.enabled;
        GUI.depth = -100; GUI.enabled = true;
        float scale = Mathf.Max(.01f, Mathf.Min(Screen.width/1280f, Screen.height/720f));
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0), Quaternion.identity, new Vector3(scale,scale,1));
        Fill(-Screen.width/scale,-Screen.height/scale,Screen.width*3/scale,Screen.height*3/scale,new Color(0,0,0,.8f));
        Fill(38,24,1204,672,line); Fill(39,25,1202,670,panel);
        Text(60,36,240,"ИНВЕНТАРЬ",heading,35);
        for (int i=0;i<controller.Battle.Allies.Count;i++)
        {
            var ally = controller.Battle.Allies[i]; GUI.enabled = !controller.Busy;
            if (GUI.Button(new Rect(310+i*195,38,185,30),(ally==controller.Battle.Player?"• ":"")+ally.Inventory.Name,action)) controller.SelectUnit(ally);
        }
        GUI.enabled = true;
        if (GUI.Button(new Rect(1090,38,130,30),"Закрыть · Esc",action)) controller.SetInventoryOpen(false);
        Fill(60,85,1160,1,line); Fill(710,104,1,551,line); Fill(1134,104,1,551,line);
        DrawFilters(); DrawBag(); DrawCharacterHeader();
        if (skills) DrawSkills(); else { DrawEquipment(); DrawStatuses(); DrawStats(); }
        DrawResources();
        Text(60,664,1150,error.Length>0?error:"Tab / Esc — закрыть     •     Выберите предмет: описание и действия появятся под сеткой",muted);
        if (editing && Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Return) { SaveName(); Event.current.Use(); }
        GUI.matrix=oldMatrix; GUI.depth=oldDepth; GUI.enabled=oldEnabled;
    }
    private void DrawFilters()
    {
        filterScroll=GUI.BeginScrollView(new Rect(60,104,630,60),filterScroll,new Rect(0,0,Mathf.Max(610,Filters.Length*122),40));
        for (int i=0;i<Filters.Length;i++)
        {
            Color old=GUI.backgroundColor; if(filter==Filters[i]) GUI.backgroundColor=gold;
            if(GUI.Button(new Rect(i*122,0,116,39),new GUIContent(FilterNames[i],icons[Filters[i]]),action))
            {filter=Filters[i];bagScroll=Vector2.zero;selected=-1;error="";}
            GUI.backgroundColor=old;
        }
        GUI.EndScrollView();
    }
    private void DrawBag()
    {
        var inventory=controller.Character; var indices=new List<int>(); int occupied=0;
        for(int i=0;i<inventory.Bag.Length;i++)
        {
            var item=inventory.Bag[i]; if(item!=null) occupied++;
            if(filter==InventoryCategory.All || (item!=null && item.Category==filter)) indices.Add(i);
        }
        float height=Mathf.Max(380,Mathf.Ceil(indices.Count/8f)*76);
        bagScroll=GUI.BeginScrollView(new Rect(60,174,630,382),bagScroll,new Rect(0,0,608,height));
        for(int n=0;n<indices.Count;n++)
        {
            int i=indices[n];var item=inventory.Bag[i]; Color old=GUI.backgroundColor;
            if(fromBag && selected==i) GUI.backgroundColor=gold;
            if(GUI.Button(new Rect(n%8*76,n/8*76,69,69),new GUIContent(item==null?"":item.Name,item==null?"Пустая ячейка":item.Name),slot))
            {selected=i;fromBag=true;error="";}
            GUI.backgroundColor=old;
        }
        if(indices.Count==0) Text(12,12,570,"В этой категории пока нет предметов",muted);
        GUI.EndScrollView();
        Text(60,561,630,"ОБОЗ   "+occupied+" / "+inventory.Bag.Length+" ячеек",muted);
        Fill(60,590,630,1,line);
        InventoryItem chosen=selected<0?null:(fromBag?inventory.Bag[selected]:inventory.Equipped[selected]);
        Text(60,599,470,chosen==null?(selected<0?"Выберите предмет":"Пустая ячейка"):chosen.Name,label);
        Text(60,625,470,chosen==null?"":chosen.Description,muted,32);
        if(chosen!=null && GUI.Button(new Rect(548,602,142,38),fromBag?"Надеть":"Снять",action))
        {
            bool moved=fromBag?inventory.Equip(selected):inventory.Unequip(selected);
            if(!moved) error="В инвентаре нет свободной ячейки.";
            else selected=-1;
        }
    }
    private void DrawCharacterHeader()
    {
        var inventory=controller.Character;
        if(editing)
        {
            draft=GUI.TextField(new Rect(730,103,247,30),draft,24,field);
            if(GUI.Button(new Rect(982,103,62,30),"OK",action))SaveName();
            if(GUI.Button(new Rect(1048,103,68,30),"Отмена",slot))editing=false;
        }
        else
        {
            if(GUI.Button(new Rect(730,103,247,30),new GUIContent(inventory.Name,"Изменить имя"),action)) {editing=true;draft=inventory.Name;}
            if(GUI.Button(new Rect(985,103,131,30),new GUIContent(skills?"← Назад":"✦ Навыки",skills?"К снаряжению":"Открыть навыки"),action)) skills=!skills;
        }
        Text(730,138,386,"Уровень "+inventory.Level+(inventory.ExperienceToNextLevel>0?"     "+inventory.Experience+" / "+inventory.ExperienceToNextLevel+" опыта":""),muted);
        Fill(730,164,386,5,line);
        if(inventory.ExperienceToNextLevel>0) Fill(730,164,386*Mathf.Clamp01((float)inventory.Experience/inventory.ExperienceToNextLevel),5,gold);
    }
    private void DrawEquipment()
    {
        Text(730,180,386,"СНАРЯЖЕНИЕ",section);
        // 3 колонки: голова в центре; руки вокруг тела; артефакты, ноги, питомец и расходник ниже.
        int[] order={2,0,3,6,1,10,4,7,5,9,8};
        for(int n=0;n<order.Length;n++)
        {
            int i=order[n]; var item=controller.Character.Equipped[i]; Color old=GUI.backgroundColor;
            if(!fromBag && selected==i)GUI.backgroundColor=gold;
            if(GUI.Button(new Rect(730+n%3*130,208+n/3*49,126,45),CharacterInventory.SlotNames[i]+"\n"+(item==null?"—":item.Name),slot))
            {selected=i;fromBag=false;error="";}
            GUI.backgroundColor=old;
        }
    }
    private void DrawStatuses()
    {
        Fill(730,416,386,1,line); Text(730,423,386,"СТАТУСЫ",section);
        var inventory=controller.Character;
        Text(730,449,386,"Класс: "+inventory.ClassName+"     Предыстория: "+inventory.Background,muted,36);
        statusScroll=GUI.BeginScrollView(new Rect(730,484,386,35),statusScroll,new Rect(0,0,365,Mathf.Max(30,inventory.StatusEffects.Count*26)));
        for(int i=0;i<inventory.StatusEffects.Count;i++)Text(0,i*26,360,inventory.StatusEffects[i],muted);
        GUI.EndScrollView();
    }
    private void DrawStats()
    {
        Fill(730,527,386,1,line); Text(730,535,386,"ХАРАКТЕРИСТИКИ",section);
        string[] names={"Атака","Броня","Здоровье","Шанс попадания","Мораль","Прочее"};
        for(int i=0;i<names.Length;i++)
        {float x=730+i%2*200,y=566+i/2*29;Text(x,y,182,names[i],muted);Fill(x,y+24,182,1,line);}
    }
    private void DrawSkills()
    {
        Text(730,187,386,"НАВЫКИ ПЕРСОНАЖА",section);
        // Дерево навыков пока не задано. Возврат не меняет предметы или фильтр.
        Fill(730,222,386,423,tile);
    }
    private void DrawResources()
    {
        Text(1148,105,77,"РЕСУРСЫ",section);
        string[] names={"Золото","Еда","Прочее"};
        for(int i=0;i<names.Length;i++)
        {Text(1148,160+i*115,77,names[i],muted);Fill(1148,190+i*115,77,56,tile);}
    }
    private void OnDestroy() {foreach(var icon in icons.Values) if(icon!=null) Destroy(icon);}
}
