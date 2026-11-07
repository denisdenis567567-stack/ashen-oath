using System;
using System.Collections.Generic;

public enum InventoryCategory { All, Weapons, Armor, Artifacts, Consumables }

public class CharacterInventory
{
    // Индексы первых двух оружий сохранены для существующих предметов прототипа.
    public static readonly string[] SlotNames = {
        "Голова", "Тело", "Оружие 1", "Оружие 2", "Артефакт 1", "Артефакт 2",
        "Перчатки", "Ноги", "Ботинки", "Питомец", "Расходник"
    };
    public string Name { get; private set; } = "Без имени";
    public int Level = 1;
    public int Experience, ExperienceToNextLevel;
    public string ClassName = "", Background = "";
    public readonly List<string> StatusEffects = new List<string>();
    public readonly InventoryItem[] Equipped = new InventoryItem[SlotNames.Length];
    public readonly InventoryItem[] Bag = new InventoryItem[60];
    public bool Rename(string value)
    {
        value = (value ?? "").Trim();
        if (value.Length == 0 || value.Length > 24) return false;
        foreach (char c in value) if (char.IsControl(c) || c == '<' || c == '>') return false;
        Name = value; return true;
    }
    public bool Equip(int index)
    {
        if (index < 0 || index >= Bag.Length || Bag[index] == null) return false;
        InventoryItem item = Bag[index];
        InventoryItem previous = Equipped[item.Slot];
        Equipped[item.Slot] = item; Bag[index] = previous; return true;
    }
    public bool Unequip(int slot)
    {
        if (slot < 0 || slot >= Equipped.Length || Equipped[slot] == null) return false;
        int free = Array.FindIndex(Bag, item => item == null);
        if (free < 0) return false;
        Bag[free] = Equipped[slot]; Equipped[slot] = null; return true;
    }
}
public class InventoryItem
{
    public readonly string Name, Description;
    public readonly int Slot;
    public readonly InventoryCategory Category;
    public InventoryItem(string name, int slot, string description)
    {
        if (slot < 0 || slot >= CharacterInventory.SlotNames.Length) throw new ArgumentOutOfRangeException(nameof(slot));
        Name = name; Slot = slot; Description = description;
        Category = slot == 2 || slot == 3 ? InventoryCategory.Weapons :
            slot == 4 || slot == 5 ? InventoryCategory.Artifacts :
            slot == 10 ? InventoryCategory.Consumables :
            slot == 9 ? InventoryCategory.All : InventoryCategory.Armor;
    }
}
