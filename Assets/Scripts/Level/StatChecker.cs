using UnityEngine;
using TMPro;

public class StatChecker : MonoBehaviour
{
    public PlayerMovement playerMovement;  // Reference to PlayerMovement script
    public TMP_Text statText;  // Reference to the TMP_Text component for displaying stats
    public GameObject statMenu;  // Reference to the GameObject that holds the stat display UI

    private bool isMenuActive = false;  // Track whether the menu is currently active

    void Start()
    {
        statMenu.SetActive(false);
    }

    void Update()
    {
        // Check if the "I" key is pressed
        if (Input.GetKeyDown(KeyCode.I))
        {
            // Toggle the menu visibility (spawn or despawn)
            isMenuActive = !isMenuActive;
            statMenu.SetActive(isMenuActive);  // Show or hide the stat menu
        }

        // If the menu is active, update the stat display
        if (isMenuActive)
        {
            statDisplay();
        }
    }

    public void statDisplay()
    {
        // Example: Assuming you want to display the player's speed
        float playerSpeed = playerMovement.speed;  
        statText.text = playerSpeed.ToString("F2");  // Format speed to 2 decimal places
    }
}
