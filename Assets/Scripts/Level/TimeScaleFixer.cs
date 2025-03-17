using UnityEngine;
using UnityEngine.SceneManagement;

// This class ensures TimeScale is always reset when loading a new scene
public class TimeScaleFixer : MonoBehaviour
{
    void Awake()
    {
        // Reset TimeScale to ensure it's never stuck
        Time.timeScale = 1f;
        
        // Register for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Make this object persistent
        DontDestroyOnLoad(gameObject);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always reset time scale when a new scene loads
        Time.timeScale = 1f;
        Debug.Log($"Scene '{scene.name}' loaded with TimeScale reset to 1");
    }
    
    void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}