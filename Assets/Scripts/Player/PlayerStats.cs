using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 8f;
    public float attackSpeed = 1f;
    public float attackDamage = 1f;
    public float maxHealth = 3f;
    
    [Header("Modifiers")]
    public float moveSpeedModifier = 1f;
    public float attackSpeedModifier = 1f;
    public float attackDamageModifier = 1f;
    public float maxHealthModifier = 1f;

    [Header("Toggles")]
    private bool isSpeedBoosted = false;
    private float normalMoveSpeedModifier = 1f;
    private float potionSpeed = 1f;
    
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    
    // Property to get actual stats
    public float ActualMoveSpeed => moveSpeed * potionSpeed;
    public float ActualAttackSpeed => attackSpeed * attackSpeedModifier;
    public float ActualAttackDamage => attackDamage + attackDamageModifier;
    public float ActualMaxHealth => maxHealth + maxHealthModifier;
    
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
    }
    
    // Method to apply item effects
    public void ApplyItem(Item item)
    {
        moveSpeedModifier += item.moveSpeedBonus;
        attackSpeedModifier += item.attackSpeedBonus;
        attackDamageModifier += item.attackDamageBonus;
        maxHealthModifier += item.maxHealthBonus;
        
        potionSpeed = isSpeedBoosted ? Mathf.Min(moveSpeedModifier, 2f) : normalMoveSpeedModifier;

        // Update player components
        UpdateComponents();
        
        Debug.Log("Applied item: " + item.itemName);
    }

    public void ToggleSpeedBoost()
    {
        isSpeedBoosted = !isSpeedBoosted;

        if (isSpeedBoosted)
        {
            // Clamp potion boost to max 2.0x
            potionSpeed = Mathf.Min(moveSpeedModifier, 2f);
        }
        else
        {
            potionSpeed = normalMoveSpeedModifier; // Typically 1x
        }

        UpdateComponents();
    }

    public void SaveStatsToPlayerData()
    {
        PlayerData.moveSpeedModifier = moveSpeedModifier;
        PlayerData.attackSpeedModifier = attackSpeedModifier;
        PlayerData.attackDamageModifier = attackDamageModifier;
        PlayerData.maxHealthModifier = maxHealthModifier;
    }

    public void LoadStatsFromPlayerData()
    {
        moveSpeedModifier = PlayerData.moveSpeedModifier;
        attackSpeedModifier = PlayerData.attackSpeedModifier;
        attackDamageModifier = PlayerData.attackDamageModifier;
        maxHealthModifier = PlayerData.maxHealthModifier;

        UpdateComponents(); // Re-apply updated values to movement/combat/health
    }

    private void UpdateComponents()
    {
        // Update movement speed
        if (playerMovement)
        {
            playerMovement.speed = ActualMoveSpeed;
        }
        
        // Update combat
        if (playerCombat)
        {
            // We would need to add these properties to PlayerCombat
            // playerCombat.attackRate = ActualAttackSpeed;
            // playerCombat.attackDamage = Mathf.RoundToInt(ActualAttackDamage);
        }
        
        // Update health
        if (playerHealth)
        {
            // We would need to add these properties to PlayerHealth
            // playerHealth.maxHealth = Mathf.RoundToInt(ActualMaxHealth);
        }
        
        Debug.Log("Stats updated - Move Speed: " + ActualMoveSpeed + 
                  ", Attack Speed: " + ActualAttackSpeed + 
                  ", Attack Damage: " + ActualAttackDamage + 
                  ", Max Health: " + ActualMaxHealth);
    }
}