using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<GameObject> enemyPrefabs = new List<GameObject>(); // List of possible enemy types
        public int enemyCount;
        public float spawnRate;
    }
    
    [Header("Wave Settings")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private Transform spawnPointsParent; // Parent object containing spawn points
    [SerializeField] private float timeBetweenWaves = 5f;
    
    [Header("Boss Settings")]
    [SerializeField] private int wavesPerBoss = 3;
    [SerializeField] private GameObject bossPrefab;
    
    [Header("Portal Settings")]
    [SerializeField] private GameObject nextLevelPortal; // Add reference to the portal
    
    private Wave currentWave;
    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private int enemiesAlive = 0;
    private float nextSpawnTime;
    private bool isSpawning = false;
    private List<Transform> spawnPoints = new List<Transform>();
    private int lastUsedSpawnPoint = -1; // Track last used spawn point for even distribution
    private bool isFirstWave = true;
    private bool bossSpawned = false;
    private bool levelCompleted = false;
    
    private void Awake()
    {
        // Get all child transforms from the spawn points parent
        if (spawnPointsParent != null)
        {
            for (int i = 0; i < spawnPointsParent.childCount; i++)
            {
                spawnPoints.Add(spawnPointsParent.GetChild(i));
            }
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found! Make sure spawnPointsParent has child transforms.");
        }
        
        // Ensure portal is initially inactive
        if (nextLevelPortal != null)
        {
            nextLevelPortal.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Start the first wave
        StartCoroutine(StartNextWave());
    }
    
    private void Update()
    {
        // Check if wave is complete
        if (isSpawning && enemiesRemaining <= 0 && enemiesAlive <= 0)
        {
            isSpawning = false;
            
            // After completing a set of waves, spawn a boss
            if ((currentWaveIndex) % wavesPerBoss == 0 && currentWaveIndex > 0 && !bossSpawned)
            {
                SpawnBoss();
                bossSpawned = true;
            }
            else if (!bossSpawned)
            {
                // Otherwise prepare for next wave
                StartCoroutine(StartNextWave());
            }
            else if (levelCompleted)
            {
                // Level is completed and we're just waiting for player to enter portal
                return;
            }
        }
        
        // Spawn enemies
        if (isSpawning && Time.time >= nextSpawnTime && enemiesRemaining > 0)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + 1f / currentWave.spawnRate;
        }
    }
    
    private IEnumerator StartNextWave()
    {
        Debug.Log("Waiting for next wave...");
        if(!isFirstWave)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
        }    
        else
        {
            isFirstWave = false;
            yield return new WaitForSeconds(5f);
        }
        
        if (currentWaveIndex < waves.Length)
        {
            currentWave = waves[currentWaveIndex];
            enemiesRemaining = currentWave.enemyCount;
            enemiesAlive = 0;
            
            Debug.Log("Wave " + (currentWaveIndex + 1) + ": " + currentWave.waveName);
            
            isSpawning = true;
            nextSpawnTime = Time.time;
            
            currentWaveIndex++;
        }
        else
        {
            // Enter endless mode or next level
            Debug.Log("All waves complete!");
            
            // If we've completed all waves and no boss is spawned/active,
            // activate the portal immediately
            if (!bossSpawned)
            {
                ActivateNextLevelPortal();
            }
        }
    }
    
    private void SpawnEnemy()
    {
        if (enemiesRemaining <= 0 || spawnPoints.Count == 0 || currentWave.enemyPrefabs.Count == 0) return;
        
        // Choose spawn point using even distribution
        Transform spawnPoint = GetNextSpawnPoint();
        
        // Choose random enemy type from available prefabs
        GameObject enemyPrefab = currentWave.enemyPrefabs[Random.Range(0, currentWave.enemyPrefabs.Count)];
        
        // Spawn enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Hook into enemy death event
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth)
        {
            enemyHealth.OnEnemyDeath += OnEnemyDeath;
            enemiesAlive++;
        }
        
        enemiesRemaining--;
    }
    
    private Transform GetNextSpawnPoint()
    {
        // Simple "round robin" distribution of spawn points
        lastUsedSpawnPoint = (lastUsedSpawnPoint + 1) % spawnPoints.Count;
        return spawnPoints[lastUsedSpawnPoint];
    }
    
    private void OnEnemyDeath()
    {
        enemiesAlive--;
        
        // Check if this was the last enemy
        if (enemiesAlive <= 0 && enemiesRemaining <= 0 && !isSpawning)
        {
            // If all waves are done and we're not spawning anymore
            if (currentWaveIndex >= waves.Length && !bossSpawned)
            {
                ActivateNextLevelPortal();
            }
        }
    }
    
    private void SpawnBoss()
    {
        Debug.Log("Spawning boss!");
        
        // Choose center spawn point or calculate center position
        Vector3 bossPosition = spawnPoints[0].position;
        
        // Spawn boss
        GameObject boss = Instantiate(bossPrefab, bossPosition, Quaternion.identity);
        
        // Hook into boss death event
        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth)
        {
            bossHealth.OnEnemyDeath += OnBossDeath;
            enemiesAlive++; // Count the boss as an enemy
        }
    }
    
    private void OnBossDeath()
    {
        Debug.Log("Boss defeated!");
        enemiesAlive--;
        
        // Important: Mark the level as completed
        levelCompleted = true;
        
        // Activate the portal to the next level
        ActivateNextLevelPortal();
    }
    
    private void ActivateNextLevelPortal()
    {
        // Activate the portal
        if (nextLevelPortal != null)
        {
            Debug.Log("Activating next level portal!");
            nextLevelPortal.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Next level portal is not assigned in LevelManager!");
            
            // Fallback - call the level complete event directly
            SceneLoader.OnLevelComplete();
        }
    }
}