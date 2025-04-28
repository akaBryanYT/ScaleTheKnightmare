using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyIndicatorManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject arrowIndicatorPrefab;
    [SerializeField] private float edgeOffset = 20f; // Distance from screen edge
    [SerializeField] private bool showIndicatorForVisibleEnemies = false;
    [SerializeField] private string enemyTag = "Enemy";
    
    [Header("Optional")]
    [SerializeField] private Transform player; // Optional, if you want to calculate distance from player
    [SerializeField] private Canvas targetCanvas; // The UI canvas to place indicators on
    
    private Camera mainCamera;
    private Dictionary<Transform, GameObject> enemyIndicators = new Dictionary<Transform, GameObject>();
    private List<Transform> enemiesInScene = new List<Transform>();
    private Vector3[] screenCorners = new Vector3[4]; // Used for screen bounds calculation
    
    private void Start()
    {
        // Get the main camera reference
        mainCamera = Camera.main;
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("No Canvas found in scene! Enemy indicators won't work.");
                this.enabled = false;
                return;
            }
        }
        
        // Initial enemy scan
        FindAllEnemies();
    }
    
    private void Update()
    {
        // Periodically look for new enemies
        if (Time.frameCount % 30 == 0) // Every 30 frames, adjust as needed
        {
            FindAllEnemies();
        }
        
        // Update all indicators
        UpdateIndicators();
    }
    
    private void FindAllEnemies()
    {
        // Find all enemies in the scene and add them to our list
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag(enemyTag);
        
        foreach (GameObject enemyObj in enemyObjects)
        {
            if (!enemiesInScene.Contains(enemyObj.transform))
            {
                enemiesInScene.Add(enemyObj.transform);
            }
        }
        
        // Remove destroyed enemies from our lists
        for (int i = enemiesInScene.Count - 1; i >= 0; i--)
        {
            if (enemiesInScene[i] == null)
            {
                enemiesInScene.RemoveAt(i);
            }
        }
    }
    
    private void UpdateIndicators()
    {
        // Check each enemy if it needs an indicator
        foreach (Transform enemy in enemiesInScene)
        {
            // Skip if enemy was destroyed
            if (enemy == null)
                continue;
            
            // Check if enemy is visible on screen
            bool isVisible = IsVisibleOnScreen(enemy);
            
            // Handle indicator creation/visibility
            if (!isVisible || showIndicatorForVisibleEnemies)
            {
                // If we don't have an indicator for this enemy yet, create one
                if (!enemyIndicators.TryGetValue(enemy, out GameObject indicator))
                {
                    indicator = Instantiate(arrowIndicatorPrefab, targetCanvas.transform);
                    enemyIndicators.Add(enemy, indicator);
                }
                
                // Update indicator position and rotation to point at the enemy
                UpdateIndicatorTransform(enemy, indicator);
                
                // Show indicator for off-screen enemies only
                indicator.SetActive(!isVisible);
            }
            else if (enemyIndicators.TryGetValue(enemy, out GameObject indicator))
            {
                // Hide the indicator if enemy is on screen
                indicator.SetActive(false);
            }
        }
        
        // Clean up indicators for destroyed enemies
        CleanupIndicators();
    }
    
    private void UpdateIndicatorTransform(Transform enemy, GameObject indicator)
    {
        // Convert enemy position to viewport position (0-1 range)
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(enemy.position);
        
        // Calculate center of screen
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        
        // Direction from screen center to enemy in viewport space
        Vector2 directionFromCenterToEnemy = new Vector2(viewportPosition.x - screenCenter.x, viewportPosition.y - screenCenter.y);
        
        // Normalized direction
        directionFromCenterToEnemy.Normalize();
        
        // Calculate the screen bounds (considering UI scale)
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        float canvasScale = targetCanvas.scaleFactor;
        screenSize /= canvasScale;
        
        // Calculate the position at the edge of the screen
        float angle = Mathf.Atan2(directionFromCenterToEnemy.y, directionFromCenterToEnemy.x);
        
        // Calculate scaled screen dimensions, considering safe area
        float screenWidth = screenSize.x - edgeOffset * 2;
        float screenHeight = screenSize.y - edgeOffset * 2;
        
        // Find the position at screen edge
        Vector2 edgePosition;
        
        // Determine which edge to place the indicator based on the angle
        if (Mathf.Abs(Mathf.Cos(angle)) > Mathf.Abs(Mathf.Sin(angle)))
        {
            // Indicator is on the left or right edge
            float xPos = Mathf.Sign(Mathf.Cos(angle)) * screenWidth * 0.5f;
            float yPos = Mathf.Tan(angle) * xPos;
            yPos = Mathf.Clamp(yPos, -screenHeight * 0.5f, screenHeight * 0.5f);
            edgePosition = new Vector2(xPos, yPos);
        }
        else
        {
            // Indicator is on the top or bottom edge
            float yPos = Mathf.Sign(Mathf.Sin(angle)) * screenHeight * 0.5f;
            float xPos = yPos / Mathf.Tan(angle);
            xPos = Mathf.Clamp(xPos, -screenWidth * 0.5f, screenWidth * 0.5f);
            edgePosition = new Vector2(xPos, yPos);
        }
        
        // Apply edge offset
        edgePosition += new Vector2(screenSize.x * 0.5f, screenSize.y * 0.5f);
        
        // Position the indicator
        indicator.transform.position = edgePosition;
        
        // Rotate the indicator to point at the enemy
        float rotationAngle = Mathf.Atan2(directionFromCenterToEnemy.y, directionFromCenterToEnemy.x) * Mathf.Rad2Deg;
        indicator.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
    }
    
    private bool IsVisibleOnScreen(Transform target)
    {
        if (target == null || mainCamera == null)
            return false;
            
        // Convert world position to viewport position
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(target.position);
        
        // Check if the position is within the viewport bounds and in front of the camera
        return (viewportPosition.x > 0 && viewportPosition.x < 1 && 
                viewportPosition.y > 0 && viewportPosition.y < 1 && 
                viewportPosition.z > 0);
    }
    
    private void CleanupIndicators()
    {
        // Create a temporary list of enemies to remove
        List<Transform> enemiesToRemove = new List<Transform>();
        
        // Find enemies that no longer exist
        foreach (KeyValuePair<Transform, GameObject> pair in enemyIndicators)
        {
            if (pair.Key == null)
            {
                enemiesToRemove.Add(pair.Key);
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }
        }
        
        // Remove them from the dictionary
        foreach (Transform enemy in enemiesToRemove)
        {
            enemyIndicators.Remove(enemy);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up all indicators when the manager is destroyed
        foreach (GameObject indicator in enemyIndicators.Values)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
    }
}