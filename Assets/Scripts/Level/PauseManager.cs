using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    private bool _isPaused = false;
    public GameObject GameManager;
    public GameObject pauseMenu;
    public PlayerMovement playerMovement; //Reference to PlayerMovement Script


    void Start()
    {
        pauseMenu.SetActive(false);
    }
        

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isPaused = !_isPaused; // Toggle pause state
            if (_isPaused)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    void PauseGame()
    {
        Time.timeScale = 0; // Stop time
        playerMovement.enabled = false; //Stops Player Movement while pause menu is active
        pauseMenu.SetActive(true);
        // Add UI or other pause logic here (e.g., show pause menu)
    }

    void ResumeGame()
    {
        Time.timeScale = 1; // Resume time
        playerMovement.enabled = true; //Returns movement when player menu is active.
        pauseMenu.SetActive(false);
        // Add UI or other unpause logic here (e.g., hide pause menu)
    }

    public void MainMenu() //Main Menu button in pause menu will change scene to the main menu
    {
        SceneManager.LoadScene("Main Menu"); //Sends player back to the main menu
        Time.timeScale = 1; //Resumes time
        playerMovement.enabled = true;//Returns movement
    }
}