using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    [Header("Level Transition")]
    [SerializeField] private float restDuration = 30f;
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private bool loadRandomLevelNext = true;
    
    // Static instance that persists between scenes
    public static SceneLoader Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern to ensure only one SceneLoader exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneLoader created as singleton");
        }
        else if (Instance != this)
        {
            Debug.Log("Destroying duplicate SceneLoader");
            Destroy(gameObject);
            return;
        }
        
        // Reset TimeScale in case it got stuck
        Time.timeScale = 1f;
    }
    
    public int rngLevel() 
    {
        System.Random rnd = new System.Random();
        int level = rnd.Next(1, 4); // Assuming you have levels 1-3
        
        // Avoid loading the current level again
        int currentLevelNumber = GetCurrentLevelNumber();
        if (level == currentLevelNumber)
            level = (level % 3) + 1; // Cycle to next level
            
        return level;
    }
    
    private int GetCurrentLevelNumber()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName.StartsWith("Level"))
        {
            string levelNumberStr = currentSceneName.Substring(5);
            if (int.TryParse(levelNumberStr, out int levelNumber))
                return levelNumber;
        }
        return 0; // Not in a level scene
    }
    
    public void ExitGame()
    {
        // Reset time scale before quitting
        Time.timeScale = 1f;
        
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public void playButton()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        Debug.Log("Play button pressed, loading game level...");
        
        int numlevel = rngLevel(); 
        string level = $"Level{numlevel}";
        
        // Load the level
        SceneManager.LoadScene(level);
    }
	
    public void StartLevelCompleteSequence()
    {
        StartCoroutine(LevelCompleteRoutine());
    }
    
    private IEnumerator LevelCompleteRoutine()
    {
        // Show level complete UI
        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(true);
        
        // Wait for rest period with countdown
        float timeRemaining = restDuration;
        
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            
            // Update countdown text if available
            if (countdownText != null)
            {
                countdownText.text = $"Next Level: {Mathf.CeilToInt(timeRemaining)}";
            }
            
            yield return null;
        }
        
        // Load next level
        LoadNextLevel();
    }
    
    private void LoadNextLevel()
    {
        if (loadRandomLevelNext)
        {
            // Load a random level
            playButton();
        }
        else
        {
            // Load the next sequential level
            int currentLevel = GetCurrentLevelNumber();
            int nextLevel = currentLevel + 1;
            
            // Check if next level exists, otherwise go back to level 1
            if (Application.CanStreamedLevelBeLoaded($"Level{nextLevel}"))
            {
                SceneManager.LoadScene($"Level{nextLevel}");
            }
            else
            {
                SceneManager.LoadScene("Level1");
            }
        }
    }
	
	public static void OnLevelComplete()
    {
        if (Instance != null)
        {
            Instance.StartLevelCompleteSequence();
        }
    }
}