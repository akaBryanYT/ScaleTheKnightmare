using UnityEngine;

public class NextLevelDoor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject interactionPrompt; // Optional: UI prompt for interaction

    private bool isPlayerInRange = false;

    private void Start()
    {
        // Hide the interaction prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

  
    private void OnTriggerEnter2D(Collider2D other)
    {

        // Check if the player enters the door's trigger zone
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Player Collision Detected" + other.name);

            SceneLoader.Instance.LoadNextLevel();

            // Show interaction prompt
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }

    //private void OnTriggerExit2D(Collider2D other)
    //{
    //    // Check if the player leaves the door's trigger zone
    //    if (other.CompareTag("Player"))
    //    {
    //        isPlayerInRange = false;


    //        // Hide interaction prompt
    //        if (interactionPrompt != null)
    //        {
    //            interactionPrompt.SetActive(false);
    //        }
    //    }
    //}

}
