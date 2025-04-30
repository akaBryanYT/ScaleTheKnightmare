using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    [Header("Level Transition")]
    [SerializeField] private float restDuration = 30f;
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private bool loadRandomLevelNext = true;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI progressionLevelText;

    // References for buttons
    private Button playButton;
    private Button exitButton;

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

    private void OnEnable()
    {
        // Subscribe to scene load event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Dynamically reconnect buttons in the new scene
        FindAndSetupButtons();

        // Update UI to show current progression level
        UpdateProgressionUI();

        // Load player stats
        LoadPlayerStats();
    }

    private void FindAndSetupButtons()
    {
        // Find buttons in the current scene
        playButton = GameObject.Find("Play Button")?.GetComponent<Button>();
        exitButton = GameObject.Find("Exit")?.GetComponent<Button>();

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(playButtonClicked);
        }
        else
        {
            Debug.LogWarning("PlayButton not found in the scene.");
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
        else
        {
            Debug.LogWarning("ExitButton not found in the scene.");
        }
    }

    private void UpdateProgressionUI()
    {
        // Find progression level text if it exists in the scene
        if (progressionLevelText == null)
        {
            progressionLevelText = GameObject.Find("ProgressionLevel")?.GetComponent<TextMeshProUGUI>();
        }

        // Update UI with current progression level if available
        if (progressionLevelText != null)
        {
            progressionLevelText.text = $"Current Run: Level {GameProgressionData.progressionLevel}";
            
            // Color code based on difficulty
            if (GameProgressionData.progressionLevel >= 5)
            {
                progressionLevelText.color = new Color(1f, 0.5f, 0f); // Orange for hard
            }
            else if (GameProgressionData.progressionLevel >= 2)
            {
                progressionLevelText.color = new Color(1f, 1f, 0f); // Yellow for medium
            }
            else
            {
                progressionLevelText.color = new Color(0f, 1f, 0f); // Green for easy
            }
        }
    }

    public void playButtonClicked()
    {
        // Reset progression when starting a new game from main menu
        if (SceneManager.GetActiveScene().name == "Main Menu")
        {
            GameProgressionData.ResetProgression();
        }
        
        // Save player stats before loading new level
        SavePlayerStats();

        // Reset time scale
        Time.timeScale = 1f;

        Debug.Log("Play button pressed, loading game level...");

        int numlevel = rngLevel();
        string level = $"Level{numlevel}";

        // Load the level
        SceneManager.LoadScene(level);
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

    public void LoadNextLevel()
    {
        // Increase progression level for next level
        GameProgressionData.IncreaseProgression();
        
        // Update UI to show new progression level
        UpdateProgressionUI();
        
        if (loadRandomLevelNext)
        {
            // Load a random level
            int numlevel = rngLevel();
            string level = $"Level{numlevel}";
            
            // Load the level
            SceneManager.LoadScene(level);
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

    private void SavePlayerStats()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.SaveStatsToPlayerData();
                Debug.Log("Saved modifiers only.");
            }
        }
    }

    private void LoadPlayerStats()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.LoadStatsFromPlayerData();
                Debug.Log("Loaded modifiers into player.");
            }
        }
    }
}