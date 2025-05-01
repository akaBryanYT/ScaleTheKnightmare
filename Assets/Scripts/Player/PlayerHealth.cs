using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public HealthBar healthBar;
    
    [SerializeField] public int maxHealth = 3;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private float deathDelay = 2f;
    
    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;
    
    private Animator animator;
    private PlayerMovement playerMovement;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(int amount)
    {
        if (isInvincible || isDead) return;
        
        currentHealth -= amount;
        healthBar.SetHealth(currentHealth);
        Debug.Log("Player took damage! Health: " + currentHealth + "/" + maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Play hurt animation
            animator.SetTrigger("isHurt");
            StartCoroutine(InvincibilityFrames());
        }
    }
    
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        // Flash the sprite to show invincibility
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        
        while (elapsed < invincibilityDuration)
        {
            sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        sprite.enabled = true;
        isInvincible = false;
    }
    
    private void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        
        Debug.Log("Player died! Returning to main menu after delay...");
        isDead = true;
        
        // Disable player movement
        if (playerMovement != null)
            playerMovement.enabled = false;
        
        // Disable player combat
        var combat = GetComponent<PlayerCombat>();
        if (combat != null)
            combat.enabled = false;
        
        // Freeze physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Play death animation
        animator.SetTrigger("isDead");
        
        // Return to main menu after delay
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }
    
    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(deathDelay);
        
        Debug.Log("Returning to main menu now!");
        ReturnToMainMenu();
    }
    
    private void ReturnToMainMenu()
    {
        // Reset all progression data
        GameProgressionData.ResetProgression();
        
        // Reset player stat modifiers
        PlayerData.moveSpeedModifier = 1f;
        PlayerData.attackSpeedModifier = 1f;
        PlayerData.attackDamageModifier = 1f;
        PlayerData.maxHealthModifier = 1f;
        
        // Use SceneLoader if available
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
        }
        else
        {
            // Fallback direct method if SceneLoader is not available
            SceneManager.LoadScene("Main Menu");
        }
    }
}