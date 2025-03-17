using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private string[] collisionTags = { "Ground", "Obstacle", "Wall" }; // Add more collision tags
    private bool hasHit = false;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Prevent multiple hits
        
        Debug.Log("Arrow hit: " + other.gameObject.name + " with tag: " + other.tag);
        
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            hasHit = true;
            
            // Deal damage to the enemy
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Debug.Log("Damaging enemy with arrow for " + damage + " damage");
                enemyHealth.TakeDamage(damage);
            }
            
            // Destroy the arrow
            Destroy(gameObject);
        }
        // Check if we hit a wall or ground by checking against multiple possible tags
        else if (ShouldStickToSurface(other.tag) || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasHit = true;
            Debug.Log("Arrow hit surface: " + other.gameObject.name);
            
            // Stick to the wall
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            
            // Destroy after a delay
            Destroy(gameObject, 1f);
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
        
        Debug.Log("Arrow collided with: " + collision.gameObject.name);
        hasHit = true;
        
        // Stop the arrow
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
		GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        
        // Destroy after a delay
        Destroy(gameObject, 1f);
    }
}