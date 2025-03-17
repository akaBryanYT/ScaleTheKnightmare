using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private float knockbackForce = 5f;
    
    private int currentHealth;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // Event that can be subscribed to when the enemy dies
    public event Action OnEnemyDeath;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Play hurt animation
        animator.SetBool("isHurt", true);
        Invoke("ResetHurtTrigger", 0.1f);
        
        // Flash the sprite
        StartCoroutine(FlashCoroutine());
        
        // Apply knockback
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 knockbackDirection = transform.position.x < player.transform.position.x ? 
                Vector2.left : Vector2.right;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private System.Collections.IEnumerator FlashCoroutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
    
    private void ResetHurtTrigger()
    {
        animator.SetBool("isHurt", false);
    }
    
    private void Die()
    {
        // Trigger death animation
        animator.SetBool("isDead", true);
        
        // Disable enemy components
        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        
        // Disable any enemy AI script
        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // Invoke the death event
        OnEnemyDeath?.Invoke();
        
        // Destroy after animation plays
        Destroy(gameObject, 1f);
    }
}