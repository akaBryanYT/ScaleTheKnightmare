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
        
        Debug.Log("Player died! Restarting level after delay...");
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
        
        // Restart level after delay
        StartCoroutine(RestartAfterDelay());
    }
    
    private IEnumerator RestartAfterDelay()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(deathDelay);
        
        Debug.Log("Restarting level now!");
        RestartLevel();
    }
    
    private void RestartLevel()
    {
        // Make sure the current scene is in build settings
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Attempting to reload scene: " + currentSceneName);
        
        // Reload the current scene
        SceneManager.LoadScene(currentSceneName);
    }
}