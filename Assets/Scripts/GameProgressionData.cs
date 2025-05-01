using UnityEngine;

public static class GameProgressionData
{
    // Current level of progression (increases as player clears levels)
    public static int progressionLevel = 0;
    
    // Scaling factors
    public static float enemyHealthMultiplier = 1f;
    public static float enemyDamageMultiplier = 1f;
    
    // Increase rate per level
    private static float healthScalingRate = 0.45f; // 45% increase per level
    private static float damageScalingRate = 0.55f; // 55% increase per level
    
    // Call this when advancing to a new level
    public static void IncreaseProgression()
    {
        progressionLevel++;
        UpdateScalingFactors();
        Debug.Log($"Game Progression: Level {progressionLevel}, Health Mult: {enemyHealthMultiplier}, Damage Mult: {enemyDamageMultiplier}");
    }
    
    // Call this when returning to main menu or starting a new game
    public static void ResetProgression()
    {
        progressionLevel = 0;
        enemyHealthMultiplier = 1f;
        enemyDamageMultiplier = 1f;
        Debug.Log("Game Progression Reset");
    }
    
    // Update the multipliers based on current progression level
    private static void UpdateScalingFactors()
    {
        enemyHealthMultiplier = 1f + (progressionLevel * healthScalingRate);
        enemyDamageMultiplier = 1f + (progressionLevel * damageScalingRate);
    }
}