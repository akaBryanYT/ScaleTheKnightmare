using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    
    [Header("Stat Bonuses")]
    public float moveSpeedBonus;
    public float attackSpeedBonus;
    public float attackDamageBonus;
    public float maxHealthBonus;
}