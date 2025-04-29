using UnityEngine;
using TMPro;

public class StatChecker : MonoBehaviour
{
    public PlayerStats playerStats;  // Reference to PlayerStats script instead of PlayerMovement
    public TMP_Text statText;  // Reference to the TMP_Text component for displaying stats
    public GameObject statMenu;  // Reference to the GameObject that holds the stat display UI
    private bool isMenuActive = false;  // Track whether the menu is currently active
    
    void Start()
    {
        // Find PlayerStats if not assigned
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats component not found! Please assign it in the inspector.");
            }
        }
        
        statMenu.SetActive(false);
    }
    
    void Update()
    {
        // Check if the "I" key is pressed
        if (Input.GetKeyDown(KeyCode.I))
        {
            // Toggle the menu visibility (spawn or despawn)
            isMenuActive = !isMenuActive;
            statMenu.SetActive(isMenuActive);  // Show or hide the stat menu
            
            // Update immediately when opening
            if (isMenuActive)
            {
                UpdateStatDisplay();
            }
        }
        
        // If the menu is active, update the stat display
        if (isMenuActive)
        {
            UpdateStatDisplay();
        }
    }
    
    public void UpdateStatDisplay()
    {
        if (playerStats == null) return;
        
        // Create a formatted string with all the player stats
        string statsInfo = "<b>Player Stats</b>\n\n";
        
        // Movement Speed
        statsInfo += $"<color=#00FFFF>Movement Speed:</color> {playerStats.ActualMoveSpeed:F2}";
        
        // Show modifier if it's not 1x
        if (playerStats.moveSpeedModifier != 1f)
        {
            statsInfo += $" <color=#00FF00>(+{(playerStats.moveSpeedModifier - 1) * 100:F0}%)</color>";
        }
        statsInfo += "\n";
        
        // Attack Speed
        statsInfo += $"<color=#FFFF00>Attack Speed:</color> {playerStats.ActualAttackSpeed:F2}";
        
        // Show modifier if it's not 1x
        if (playerStats.attackSpeedModifier != 1f)
        {
            statsInfo += $" <color=#00FF00>(+{(playerStats.attackSpeedModifier - 1) * 100:F0}%)</color>";
        }
        statsInfo += "\n";
        
        // Attack Damage
        statsInfo += $"<color=#FF6A00>Attack Damage:</color> {playerStats.ActualAttackDamage:F1}";
        
        // Show modifier if it's not 1x
        if (playerStats.attackDamageModifier != 1f)
        {
            statsInfo += $" <color=#00FF00>(+{(playerStats.attackDamageModifier - 1) * 100:F0}%)</color>";
        }
        statsInfo += "\n";
        
        // Max Health
        statsInfo += $"<color=#FF0000>Max Health:</color> {playerStats.ActualMaxHealth:F0}";

        // Show modifier if it's not 0
        float extraHealth = (playerStats.maxHealthModifier - 1f);
        if (extraHealth != 0)
        {
            statsInfo += $" <color=#00FF00>(+{extraHealth:F0} HP)</color>";
        }
        
        // Update the TMP text
        statText.text = statsInfo;
    }
}