// Update your ItemPickup.cs script with this code
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private Item item;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision detected with: " + other.gameObject.name);
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected");
            
            // Apply the item to the player
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null && item != null)
            {
                playerStats.ApplyItem(item);
                Debug.Log("Item applied: " + item.name);
                
                // Destroy the item
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Failed to apply item. PlayerStats: " + (playerStats != null) + ", Item: " + (item != null));
            }
        }
    }
}