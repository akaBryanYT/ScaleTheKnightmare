using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
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
        isDead = true;
        
        // Disable player movement
        playerMovement.enabled = false;
        
        // Freeze physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Play death animation
        animator.SetTrigger("isDead");
        
        // Restart level after delay
        StartCoroutine(RestartAfterDelay());
    }
    
    private IEnumerator RestartAfterDelay()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(deathDelay);
        
        // Show simple game over text
        ShowGameOverText();
        
        // Wait for player to press any key to restart
        while (!Input.anyKeyDown)
        {
            yield return null;
        }
        
        RestartLevel();
    }
    
    private void ShowGameOverText()
    {
        // Create very simple UI
        GameObject gameOverObj = new GameObject("GameOverText");
        gameOverObj.transform.position = Camera.main.transform.position + new Vector3(0, 0, 1);
        
        TextMesh textMesh = gameOverObj.AddComponent<TextMesh>();
        textMesh.text = "GAME OVER\nPress any key to restart";
        textMesh.fontSize = 24;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.red;
    }
    
    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Optional: Method for manual restart with R key
    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }
}