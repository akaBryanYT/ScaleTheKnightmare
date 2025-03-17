using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f; // How long the projectile lives before self-destructing
    [SerializeField] private int damage = 1; // Damage dealt to player
    [SerializeField] private bool rotateToDirection = true; // Whether to rotate the sprite to match direction
    [SerializeField] private GameObject hitEffect; // Optional hit effect prefab
    
    private bool hasHit = false;
    
    private void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
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
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy the projectile
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            hasHit = true;
            
            // Spawn hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy the projectile
            Destroy(gameObject);
        }
    }
    
    // For non-trigger collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy the projectile
        Destroy(gameObject);
    }
}