using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public List<GameObject> items = new List<GameObject>(); // List of possible items
    [SerializeField] private int baseMaxHealth = 2; // Renamed to baseMaxHealth
    [SerializeField] private float knockbackForce = 5f;
    
    private int currentHealth;
    private int scaledMaxHealth; // New variable for scaled health
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // Event that can be subscribed to when the enemy dies
    public event Action OnEnemyDeath;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Start()
    {
        // Calculate scaled max health
        scaledMaxHealth = Mathf.RoundToInt(baseMaxHealth * GameProgressionData.enemyHealthMultiplier);
        currentHealth = scaledMaxHealth;
        
        // Log the scaling for debugging
        if (GameProgressionData.progressionLevel > 0)
        {
            Debug.Log($"Enemy scaled: Base HP {baseMaxHealth} → Current HP {scaledMaxHealth}");
        }
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
        
        // Spawn item at enemy's location
        if(UnityEngine.Random.Range(0, 100) < 50){
            if (items.Count > 0)
            {
                // Choose a random item from the list
                GameObject itemToSpawn = items[UnityEngine.Random.Range(0, items.Count)];

                // Instantiate the item at the enemy's position (transform.position)
                GameObject item = Instantiate(itemToSpawn, transform.position, Quaternion.identity); // Quaternion.identity for default rotation

                // Check if item has Rigidbody2D component
                Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();

                if (itemRb != null)
                {
                    // Apply a random force to the item to make it fly
                    // Random direction for flight (horizontal and vertical)
                    Vector2 randomDirection = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1f, 2f)); // Horizontal & upward
                    float randomForce = UnityEngine.Random.Range(5f, 10f); // Random force magnitude

                    // Apply the force to the Rigidbody2D
                    itemRb.AddForce(randomDirection.normalized * randomForce, ForceMode2D.Impulse);
                }
                else
                {
                    Debug.LogWarning("Item does not have Rigidbody2D component!");
                }
            }
        }
        
        // Destroy after animation plays
        Destroy(gameObject, 1f);
    }
}