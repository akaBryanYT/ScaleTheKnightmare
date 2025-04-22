using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject player;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    
    private bool _isPaused = false;
    private MonoBehaviour[] playerComponents;
    private Animator[] animators;
    
    private static PauseManager instance;
    
    void Awake()
    {
        // Ensure we don't keep multiple PauseManagers between scenes
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        
        instance = this;
    }
    
    void Start()
    {
        
        pauseMenu.SetActive(false);
       
        
        // Make absolutely sure time scale is correct at scene start
        Time.timeScale = 1f;
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

       
        
        // Cache animators (for pausing animations)
        animators = FindObjectsOfType<Animator>();
    }
    
    void Update()
    {
        // Only check for escape key in gameplay scenes (not in main menu)
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isPaused = !_isPaused;
                
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
    }
    
    void PauseGame()
    {
        // Stop time
        Time.timeScale = 0;
        
        // Disable all player scripts
        if (player != null)
        {
            playerComponents = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in playerComponents)
            {
                if (component != null && component.enabled)
                {
                    component.enabled = false;
                }
            }
        }
        
        // Pause all animations
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.enabled)
            {
                animator.speed = 0;
            }
        }
        
        // Show pause menu
        pauseMenu.SetActive(true);
       
    }
    
    void ResumeGame()
    {
        // Resume time
        Time.timeScale = 1;
        
        // Re-enable all player scripts
        if (playerComponents != null)
        {
            foreach (MonoBehaviour component in playerComponents)
            {
                if (component != null) 
                {
                    component.enabled = true;
                }
            }
        }
        
        // Resume all animations
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.enabled)
            {
                animator.speed = 1;
            }
        }
        
        // Hide pause menu
        pauseMenu.SetActive(false);
      
    }
    
    public void MainMenu()
    {
        // Critical fix: Reset time scale
        Time.timeScale = 1;
        
        // Reset pause state
        _isPaused = false;
        
        // Destroy this manager to prevent problems when returning
        Destroy(this);
        
        // Load main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void ResumeButton()
    {
        _isPaused = false;
        ResumeGame();
    }

  
        
}