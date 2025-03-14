using UnityEngine;

public class DamageTile : MonoBehaviour
{
    public int damageAmount = 10; // Amount of damage the tile inflicts
    public float damageInterval = 1.0f; // Time in seconds between damage ticks

    private bool isPlayerOnTile = false;
    private float damageTimer = 2.0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // Ensure the Player object has the "Player" tag
        {
            isPlayerOnTile = true;
            damageTimer = 0; // Reset the timer
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerOnTile = false;
        }
    }

    private void Update()
    {
        if (isPlayerOnTile)
        {
            damageTimer += Time.deltaTime;
            Debug.Log($"Damage Timer: {damageTimer}");

            if (damageTimer >= damageInterval)
            {
                // Access the player's health script to apply damage
                Debug.Log("Damage");
                PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount);
                }

                damageTimer = 0; // Reset the timer
            }
        }
    }
}
