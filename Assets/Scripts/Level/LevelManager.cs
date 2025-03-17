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
    
    private Wave currentWave;
    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private int enemiesAlive = 0;
    private float nextSpawnTime;
    private bool isSpawning = false;
    private List<Transform> spawnPoints = new List<Transform>();
    private int lastUsedSpawnPoint = -1; // Track last used spawn point for even distribution
	private bool isFirstWave = true;
    
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
            if ((currentWaveIndex) % wavesPerBoss == 0 && currentWaveIndex > 0)
            {
                SpawnBoss();
            }
            else
            {
                // Otherwise prepare for next wave
                StartCoroutine(StartNextWave());
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
        }
    }
    
    private void OnBossDeath()
    {
        Debug.Log("Boss defeated!");
        StartCoroutine(StartNextWave());
    }
}