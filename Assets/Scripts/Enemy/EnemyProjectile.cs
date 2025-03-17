using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f; // How long the projectile lives before self-destructing
    [SerializeField] private int damage = 1; // Damage dealt to player
    [SerializeField] private bool rotateToDirection = true; // Whether to rotate the sprite to match direction
    [SerializeField] private GameObject hitEffect; // Optional hit effect prefab
    [SerializeField] private string[] collisionTags = { "Ground", "Obstacle", "Wall" }; // Add collision tags
    
    private bool hasHit = false;
    
    private void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
        
        // Debug logging to help troubleshoot
        Debug.Log($"Enemy projectile spawned: {gameObject.name}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        Debug.Log("Enemy projectile hit: " + other.gameObject.name + " with tag: " + other.tag);
        
        if (other.CompareTag("Player"))
        {
            hasHit = true;
            
            // Deal damage to player
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            // Spawn hit effect
            SpawnHitEffect();
            
            // Destroy the projectile
            Destroy(gameObject);
        }
        // Check if we hit a wall or ground by checking against multiple possible tags and layers
        else if (ShouldStickToSurface(other.tag) || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasHit = true;
            Debug.Log("Enemy projectile hit surface: " + other.gameObject.name);
            
            // Spawn hit effect
            SpawnHitEffect();
            
            // Stop the projectile
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            
            // Destroy after a delay
            Destroy(gameObject, 0.2f);
        }
    }
    
    private bool ShouldStickToSurface(string tag)
    {
        // Check if the tag is any of our collision tags
        foreach (string collisionTag in collisionTags)
        {
            if (tag == collisionTag)
                return true;
        }
        return false;
    }
    
    // Handle collision with non-trigger colliders as well
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        
        Debug.Log("Enemy projectile collided with: " + collision.gameObject.name);
        hasHit = true;
        
        // Spawn hit effect
        SpawnHitEffect();
        
        // Stop the projectile
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Destroy after a delay
        Destroy(gameObject, 0.2f);
    }
    
    private void SpawnHitEffect()
    {
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
    }
}