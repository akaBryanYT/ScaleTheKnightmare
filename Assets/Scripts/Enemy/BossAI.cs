using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float phaseSpeed = 4f;
    [SerializeField] private float maxFollowDistance = 15f;
    [SerializeField] private float jumpCooldown = 1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float platformDetectionDistance = 3f;
    
    [Header("Advanced Movement")]
    [SerializeField] private float ledgeDetectionDistance = 1.5f;
    [SerializeField] private float obstacleAvoidanceDistance = 1f;
    [SerializeField] private float unstuckCheckInterval = 2f;
    [SerializeField] private float minDistanceToMove = 0.1f;
    [SerializeField] private float platformJumpThreshold = 2.5f;
    [SerializeField] private float highLedgeJumpForceMultiplier = 1.5f;
    [SerializeField] private float teleportRangeFar = 15f; // Added: Distance considered "far"
    [SerializeField] private float teleportRangeClose = 5f; // Added: Distance for close teleport
    
    [Header("Attack Properties")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float generalAttackCooldown = 1.5f;
    [SerializeField] private bool forceAttackVariety = true; // Added: Force attack variety
    
    [Header("Light Attack")]
    [SerializeField] private Transform lightAttackPoint;
    [SerializeField] private float lightAttackRange = 0.5f;
    [SerializeField] private int lightAttackDamage = 1;
    [SerializeField] private float lightAttackDistance = 2f;
    [SerializeField] private float lightAttackCooldown = 1f;
    
    [Header("Heavy Attack")]
    [SerializeField] private Transform heavyAttackPoint;
    [SerializeField] private float heavyAttackRange = 0.7f;
    [SerializeField] private int heavyAttackDamage = 3;
    [SerializeField] private float heavyAttackDistance = 2.5f;
    [SerializeField] private float heavyAttackCooldown = 3f;
    
    [Header("Area Attack")]
    [SerializeField] private Transform areaAttackPoint;
    [SerializeField] private GameObject areaAttackIndicator;
    [SerializeField] private float areaAttackRadius = 4f;
    [SerializeField] private int areaAttackDamage = 2;
    [SerializeField] private float areaAttackCooldown = 5f;
    
    [Header("Projectile Attack")]
    [SerializeField] private bool hasProjectileAttack = false;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float minProjectileRange = 5f;
    [SerializeField] private float maxProjectileRange = 12f;
    [SerializeField] private float projectileCooldown = 2f;
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float projectileSpreadAngle = 15f;
    
    // State tracking
    private enum BossState { Idle, Chasing, Jumping, Phasing, Attacking, PlatformNavigating, Unstucking, Teleporting }
    private BossState currentState = BossState.Idle;
    private BossState previousState = BossState.Idle; // Added: Track previous state
    
    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private BoxCollider2D physicsCollider;
    
    // Attack cooldown timers
    private float nextLightAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;
    private float nextAreaAttackTime = 0f;
    private float nextProjectileTime = 0f;
    private float nextJumpTime = 0f;
    private float nextGeneralAttackTime = 0f;
    private float nextTeleportTime = 0f; // Added: Teleport cooldown
    
    // Attack variety tracking
    private int lastAttackType = -1; // Added: Track last attack type used
    private int consecutiveSameAttacks = 0; // Added: Count of same attack used consecutively
    
    // Platform phasing
    private PlatformEffector2D currentPlatform;
    private bool isPhasing = false;
    private bool isFacingRight = true;
    
    // Scaling values
    private int scaledLightAttackDamage;
    private int scaledHeavyAttackDamage;
    private int scaledAreaAttackDamage;
    private float bossHealthMultiplier = 1f;
    
    // Unstuck detection
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private float unstuckCheckTimer = 0f;
    private bool isCheckingIfStuck = false;
    private bool potentiallyStuck = false;
    private int unstuckAttempts = 0; // Added: Count unstuck attempts
    
    // Platforming
    private bool targetPlatformFound = false;
    private Vector2 targetPlatformPosition;
    private List<Vector2> recentPositions = new List<Vector2>();
    private int positionHistoryLength = 10;
    private float positionRecordInterval = 0.2f;
    private float lastPositionRecordTime = 0f;
    
    // Improved pathfinding
    private List<Transform> platformsInScene = new List<Transform>();
    private List<Transform> visitedPlatforms = new List<Transform>(); // Added: Track visited platforms
    
    // Animation state tracking
    private bool isAnimationTransitioning = false; // Added: Track animation transitions
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        physicsCollider = GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Create attack points if not assigned
        if (lightAttackPoint == null)
        {
            lightAttackPoint = new GameObject("BossLightAttackPoint").transform;
            lightAttackPoint.SetParent(transform);
            lightAttackPoint.localPosition = new Vector3(1f, 0f, 0f);
        }
        
        if (heavyAttackPoint == null)
        {
            heavyAttackPoint = new GameObject("BossHeavyAttackPoint").transform;
            heavyAttackPoint.SetParent(transform);
            heavyAttackPoint.localPosition = new Vector3(1.2f, 0f, 0f);
        }
        
        if (areaAttackPoint == null)
        {
            areaAttackPoint = new GameObject("BossAreaAttackPoint").transform;
            areaAttackPoint.SetParent(transform);
            areaAttackPoint.localPosition = Vector3.zero;
        }
        
        // Create projectile spawn point if needed
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = new GameObject("BossProjectilePoint").transform;
            projectileSpawnPoint.SetParent(transform);
            projectileSpawnPoint.localPosition = new Vector3(1f, 0.5f, 0f);
        }
        
        // Initialize position for unstuck detection
        lastPosition = transform.position;
        
        // Find all platforms in the scene for pathfinding
        FindAllPlatforms();
    }
    
    private void Start()
    {
        // Apply damage scaling for boss
        // Bosses get a slightly higher multiplier than regular enemies
        bossHealthMultiplier = GameProgressionData.enemyHealthMultiplier * 1.2f;
        
        // Scale the attack damages
        scaledLightAttackDamage = Mathf.RoundToInt(lightAttackDamage * GameProgressionData.enemyDamageMultiplier);
        scaledHeavyAttackDamage = Mathf.RoundToInt(heavyAttackDamage * GameProgressionData.enemyDamageMultiplier);
        scaledAreaAttackDamage = Mathf.RoundToInt(areaAttackDamage * GameProgressionData.enemyDamageMultiplier);
        
        // Debug log the scaling
        if (GameProgressionData.progressionLevel > 0)
        {
            Debug.Log($"Boss attacks scaled - Light: {lightAttackDamage} → {scaledLightAttackDamage}, " +
                     $"Heavy: {heavyAttackDamage} → {scaledHeavyAttackDamage}, " +
                     $"Area: {areaAttackDamage} → {scaledAreaAttackDamage}");
        }
        
        // Start unstuck detection coroutine
        StartCoroutine(UnstuckCoroutine());
    }
    
    private void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }
        
        // Skip other logic during teleporting
        if (currentState == BossState.Teleporting)
            return;
            
        // Record position history for movement analysis
        RecordPositionHistory();
        
        // Calculate distances
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = player.position.y - transform.position.y;
        float directDistance = Vector2.Distance(transform.position, player.position);
        
        // Face the player (only when not navigating platforms)
        if (currentState != BossState.PlatformNavigating && 
            currentState != BossState.Attacking && 
            currentState != BossState.Teleporting)
        {
            FacePlayer();
        }
        
        // If we're unstucking, don't do anything else
        if (currentState == BossState.Unstucking)
        {
            return;
        }
        
        // NEW: Add vertical distance component to determine if teleportation is needed
        float verticalDistanceAbs = Mathf.Abs(verticalDistance);
        bool isPlayerTooFar = directDistance > maxFollowDistance || verticalDistanceAbs > 5f;
        
        // NEW: Smart teleportation when player is too far away or on a different floor
        if (isPlayerTooFar && Time.time > nextTeleportTime)
        {
            // Try to teleport close to player
            TeleportSmartly(verticalDistance, horizontalDistance);
            return;
        }
        
        // First, check if we need to navigate complex platforms
        if (NeedToPlatform(verticalDistance, horizontalDistance))
        {
            NavigatePlatforms();
            return;
        }
        
        // Check if we're about to walk off a ledge or hit an obstacle
        if (currentState == BossState.Chasing && ShouldAvoidObstacle())
        {
            HandleObstacle();
            return;
        }
        
        // Is player above us and we need to jump?
        if (verticalDistance > 1.5f && IsGrounded() && Time.time > nextJumpTime && !ShouldAvoidObstacle())
        {
            Jump(verticalDistance);
            return;
        }
        
        // Should we phase through a platform to reach the player?
        if (verticalDistance < -1.5f && !isPhasing && IsOnPlatform())
        {
            StartCoroutine(PhaseDownThroughPlatform());
            return;
        }
        
        // NEW: Improved attack selection with variety
        if (Time.time > nextGeneralAttackTime)
        {
            bool attackPerformed = AttemptAttackWithVariety(horizontalDistance, directDistance);
            
            if (attackPerformed)
            {
                nextGeneralAttackTime = Time.time + generalAttackCooldown;
                return;
            }
        }
        
        // If we haven't attacked or jumped, chase the player
        ChasePlayer();
    }
    
    // NEW: Method to force attack variety
    private bool AttemptAttackWithVariety(float horizontalDistance, float directDistance)
    {
        // Create an array of available attacks
        List<int> availableAttacks = new List<int>();
        
        // Check each attack's availability and range requirements
        // Light attack (type 0)
        if (Time.time > nextLightAttackTime && horizontalDistance <= lightAttackDistance)
        {
            availableAttacks.Add(0);
        }
        
        // Heavy attack (type 1)
        if (Time.time > nextHeavyAttackTime && horizontalDistance <= heavyAttackDistance)
        {
            availableAttacks.Add(1);
        }
        
        // Area attack (type 2)
        if (Time.time > nextAreaAttackTime && directDistance <= areaAttackRadius)
        {
            availableAttacks.Add(2);
        }
        
        // Projectile attack (type 3)
        if (hasProjectileAttack && Time.time > nextProjectileTime && 
            directDistance >= minProjectileRange && directDistance <= maxProjectileRange)
        {
            availableAttacks.Add(3);
        }
        
        // If no attacks available, return false
        if (availableAttacks.Count == 0)
            return false;
            
        // If we have multiple attacks available and want variety, avoid using the same attack repeatedly
        if (availableAttacks.Count > 1 && forceAttackVariety && consecutiveSameAttacks >= 2)
        {
            // Remove last attack type from options to force variety
            availableAttacks.Remove(lastAttackType);
        }
        
        // Choose a random attack from available options
        int attackIndex = Random.Range(0, availableAttacks.Count);
        int attackType = availableAttacks[attackIndex];
        
        // Track attack variety
        if (attackType == lastAttackType)
        {
            consecutiveSameAttacks++;
        }
        else
        {
            consecutiveSameAttacks = 0;
            lastAttackType = attackType;
        }
        
        // Perform the selected attack
        switch (attackType)
        {
            case 0:
                PerformLightAttack();
                break;
            case 1:
                PerformHeavyAttack();
                break;
            case 2:
                PerformAreaAttack();
                break;
            case 3:
                PerformProjectileAttack();
                break;
        }
        
        return true;
    }
    
    #region Advanced Movement
    
    private void RecordPositionHistory()
    {
        // Record our position at intervals to detect movement patterns
        if (Time.time - lastPositionRecordTime >= positionRecordInterval)
        {
            lastPositionRecordTime = Time.time;
            
            // Add current position
            recentPositions.Add(transform.position);
            
            // Keep the list at the desired length
            if (recentPositions.Count > positionHistoryLength)
            {
                recentPositions.RemoveAt(0);
            }
        }
    }
    
    private void FindAllPlatforms()
    {
        // Find all potential platforms in the scene
        Collider2D[] allPlatforms = Physics2D.OverlapAreaAll(
            new Vector2(-1000, -1000),
            new Vector2(1000, 1000),
            groundLayer
        );
        
        foreach (Collider2D platform in allPlatforms)
        {
            platformsInScene.Add(platform.transform);
        }
        
        Debug.Log($"Found {platformsInScene.Count} platforms for boss navigation");
    }
    
    // NEW: Improved teleportation that's smarter about where to go
    private void TeleportSmartly(float verticalDistance, float horizontalDistance)
    {
        SetState(BossState.Teleporting);
        
        // Play teleport effect/animation
        animator.SetTrigger("teleport");
        
        Vector3 targetPosition;
        bool teleportedToFloor = false;
        
        // First check: Is player on a different floor?
        if (Mathf.Abs(verticalDistance) > 4f)
        {
            // Try to find a platform near the player's height
            Transform closestPlatform = FindPlatformNearPlayerHeight();
            
            if (closestPlatform != null)
            {
                // Teleport to platform near player's height
                targetPosition = closestPlatform.position;
                targetPosition.y += 1f; // Appear above the platform
                teleportedToFloor = true;
                
                Debug.Log("Boss teleporting to platform at player's height level");
            }
            else
            {
                // If no suitable platform, teleport with offset from player
                targetPosition = player.position;
                targetPosition.x += (Random.value > 0.5f ? 3f : -3f); // Random side offset
                targetPosition.y += 1f; // Appear slightly above the player's level
                
                Debug.Log("Boss teleporting near player (no suitable platform found)");
            }
        }
        else
        {
            // On the same floor but far horizontally
            targetPosition = player.position;
            
            // Maintain same height but teleport closer horizontally
            targetPosition.y = transform.position.y;
            targetPosition.x = player.position.x + (Random.value > 0.5f ? 3f : -3f); // Random side offset
            
            Debug.Log("Boss teleporting closer to player horizontally");
        }
        
        // Check if the target position is valid (not inside terrain)
        Collider2D overlap = Physics2D.OverlapCircle(targetPosition, 1f, groundLayer);
        if (overlap != null)
        {
            // Adjust position upward if we're inside terrain
            targetPosition.y += 2f;
            Debug.Log("Adjusted teleport position upward to avoid terrain overlap");
        }
        
        // Teleport with a slight delay
        StartCoroutine(DelayedTeleport(targetPosition));
        
        // Set cooldown - shorter for floor-to-floor teleports to be more aggressive
        nextTeleportTime = Time.time + (teleportedToFloor ? 3f : 5f);
    }
    
    private IEnumerator DelayedTeleport(Vector3 targetPosition)
    {
        // Teleport effect start - could fade out sprite
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color originalColor = sprite.color;
            sprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // Perform teleport
        transform.position = targetPosition;
        
        // Face the player after teleport
        FacePlayer();
        
        // Reset unstuck detection after teleport
        lastPosition = targetPosition;
        potentiallyStuck = false;
        
        // Teleport effect end - fade back in
        if (sprite != null)
        {
            Color originalColor = sprite.color;
            sprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        }
        
        // Return to normal state
        SetState(BossState.Idle);
    }
    
    // NEW: Find a platform at similar height to the player
    private Transform FindPlatformNearPlayerHeight()
    {
        Transform bestPlatform = null;
        float smallestHeightDifference = float.MaxValue;
        
        foreach (Transform platform in platformsInScene)
        {
            // Skip very distant platforms
            if (Vector2.Distance(platform.position, player.position) > maxFollowDistance * 1.5f)
                continue;
                
            // Calculate height difference
            float heightDifference = Mathf.Abs(platform.position.y - player.position.y);
            
            // Check horizontal distance to avoid platforms directly above/below the player
            float horizontalDistance = Mathf.Abs(platform.position.x - player.position.x);
            
            // Prefer platforms that are at similar height but not too close horizontally
            if (heightDifference < 3f && horizontalDistance > 2f && heightDifference < smallestHeightDifference)
            {
                smallestHeightDifference = heightDifference;
                bestPlatform = platform;
            }
        }
        
        return bestPlatform;
    }
    
    private bool NeedToPlatform(float verticalDistance, float horizontalDistance)
    {
        // Do we need to handle complex platforming?
        
        // If the player is significantly above us
        if (verticalDistance > platformJumpThreshold)
        {
            // Check if we need to navigate to a different platform
            RaycastHit2D directPath = Physics2D.Raycast(
                transform.position,
                player.position - transform.position,
                Vector2.Distance(transform.position, player.position),
                groundLayer
            );
            
            // If there's an obstacle in our direct path
            if (directPath.collider != null)
            {
                // Try to find a path to the player
                Transform nearestPlatform = FindNearestPlatformToPlayer();
                if (nearestPlatform != null)
                {
                    targetPlatformPosition = nearestPlatform.position;
                    targetPlatformFound = true;
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private Transform FindNearestPlatformToPlayer()
    {
        // Find the best platform to use to reach the player
        Transform bestPlatform = null;
        float bestScore = float.MaxValue;
        
        foreach (Transform platform in platformsInScene)
        {
            // Skip if this is the platform we're already on
            if (IsStandingOn(platform))
                continue;
            
            // Calculate scores based on proximity to boss and player
            float distanceToBoss = Vector2.Distance(transform.position, platform.position);
            float distanceToPlayer = Vector2.Distance(platform.position, player.position);
            
            // Skip platforms that are too high to reach with a single jump
            if (platform.position.y - transform.position.y > jumpForce * 0.25f)
                continue;
            
            // Give a slight penalty to recently visited platforms
            float visitedPenalty = 0f;
            if (visitedPlatforms.Contains(platform))
            {
                visitedPenalty = 10f;
            }
            
            // Weight: closer to boss is better, and closer to player is better
            float score = distanceToBoss * 0.4f + distanceToPlayer * 0.6f + visitedPenalty;
            
            // Prefer platforms that bring us closer to the player vertically
            if (transform.position.y < player.position.y && platform.position.y > transform.position.y)
                score *= 0.7f; // Bonus for platforms that help us go up
            
            if (score < bestScore)
            {
                bestScore = score;
                bestPlatform = platform;
            }
        }
        
        // Add the chosen platform to our visited list
        if (bestPlatform != null && !visitedPlatforms.Contains(bestPlatform))
        {
            visitedPlatforms.Add(bestPlatform);
            
            // Limit the size of visited platforms list
            if (visitedPlatforms.Count > 5)
            {
                visitedPlatforms.RemoveAt(0);
            }
        }
        
        return bestPlatform;
    }
    
    private bool IsStandingOn(Transform platform)
    {
        // Check if we're standing on this platform
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.down * 0.1f,
            Vector2.down,
            0.5f,
            groundLayer
        );
        
        return hit.collider != null && hit.collider.transform == platform;
    }
    
    private void NavigatePlatforms()
    {
        if (!targetPlatformFound)
            return;
        
        // Set state
        SetState(BossState.PlatformNavigating);
        
        // Calculate direction to target platform
        float horizontalDirection = targetPlatformPosition.x - transform.position.x;
        
        // Face the direction we need to go
        bool shouldFaceRight = horizontalDirection > 0;
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            FlipSprite();
        }
        
        // Move toward the platform
        rb.linearVelocity = new Vector2(Mathf.Sign(horizontalDirection) * moveSpeed, rb.linearVelocity.y);
        
        // Check horizontal distance to platform
        float horizontalDist = Mathf.Abs(horizontalDirection);
        
        // If we're close enough horizontally and need to jump
        if (horizontalDist < 2f && targetPlatformPosition.y > transform.position.y)
        {
            if (IsGrounded() && Time.time > nextJumpTime)
            {
                // Calculate jump force based on height difference
                float heightDifference = targetPlatformPosition.y - transform.position.y;
                float adjustedJumpForce = jumpForce;
                
                // If it's a high ledge, jump harder
                if (heightDifference > 3f)
                {
                    adjustedJumpForce *= highLedgeJumpForceMultiplier;
                }
                
                // Jump to the platform
                rb.AddForce(new Vector2(0, adjustedJumpForce), ForceMode2D.Impulse);
                nextJumpTime = Time.time + jumpCooldown;
                
                // Play jump animation
                animator.SetTrigger("jump");
            }
        }
        
        // If we're close to the target platform, resume normal behavior
        if (Vector2.Distance(transform.position, targetPlatformPosition) < 1f || IsOnTargetPlatform())
        {
            targetPlatformFound = false;
            SetState(BossState.Idle);
        }
    }
    
    private bool IsOnTargetPlatform()
    {
        // Check if we're on or very close to the target platform
        if (!IsGrounded())
            return false;
            
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.down * 0.1f,
            Vector2.down,
            0.5f,
            groundLayer
        );
        
        if (hit.collider != null)
        {
            Vector2 hitPoint = hit.point;
            return Vector2.Distance(hitPoint, targetPlatformPosition) < 2f;
        }
        
        return false;
    }
    
    private bool ShouldAvoidObstacle()
    {
        // Detect if there's a ledge ahead or an obstacle in the way
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + new Vector2(direction.x * physicsCollider.bounds.extents.x, -physicsCollider.bounds.extents.y);
        
        // Check for a ledge
        RaycastHit2D groundAhead = Physics2D.Raycast(
            origin,
            Vector2.down,
            ledgeDetectionDistance,
            groundLayer
        );
        
        // Check for obstacles
        RaycastHit2D obstacleAhead = Physics2D.Raycast(
            transform.position,
            direction,
            obstacleAvoidanceDistance,
            groundLayer
        );
        
        // Return true if there's a ledge ahead (no ground) or an obstacle
        return (groundAhead.collider == null && IsGrounded()) || obstacleAhead.collider != null;
    }
    
    private void HandleObstacle()
    {
        // Decide whether to jump or turn around
        bool canJumpOverObstacle = false;
        
        // Check if we can jump over the obstacle
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D highObstacleCheck = Physics2D.Raycast(
            transform.position + Vector3.up * 1.5f,
            direction,
            obstacleAvoidanceDistance * 2f,
            groundLayer
        );
        
        // Check for a platform on the other side of the obstacle
        RaycastHit2D platformBeyondObstacle = Physics2D.Raycast(
            transform.position + (Vector3)(direction * obstacleAvoidanceDistance * 2f) + Vector3.up * 0.5f,
            Vector2.down,
            2f,
            groundLayer
        );
        
        // If there's nothing blocking us higher up and there's ground beyond
        canJumpOverObstacle = highObstacleCheck.collider == null && platformBeyondObstacle.collider != null;
        
        if (canJumpOverObstacle && IsGrounded() && Time.time > nextJumpTime)
        {
            // Jump over the obstacle
            Jump(2f);
            
            // Add horizontal velocity to clear the obstacle
            rb.AddForce(direction * moveSpeed * 10f, ForceMode2D.Impulse);
        }
        else
        {
            // If we can't jump over it, try going around
            isFacingRight = !isFacingRight;
            FlipSprite();
            targetPlatformFound = false;
            
            // Find a new path
            Transform alternativePlatform = FindAlternativePath();
            if (alternativePlatform != null)
            {
                targetPlatformPosition = alternativePlatform.position;
                targetPlatformFound = true;
                SetState(BossState.PlatformNavigating);
            }
        }
    }
    
    private Transform FindAlternativePath()
    {
        // Find a platform we can go to that might lead to the player
        Transform bestPlatform = null;
        float bestScore = float.MaxValue;
        
        foreach (Transform platform in platformsInScene)
        {
            // Skip the current platform
            if (IsStandingOn(platform))
                continue;
            
            // Calculate scores
            float distanceToBoss = Vector2.Distance(transform.position, platform.position);
            float distanceToPlayer = Vector2.Distance(platform.position, player.position);
            
            // Skip platforms that are too far away
            if (distanceToBoss > maxFollowDistance * 0.5f)
                continue;
            
            // Skip platforms that are too high
            if (platform.position.y - transform.position.y > jumpForce * 0.2f)
                continue;
            
            // Weight: closer to boss is better, and closer to player is better
            float score = distanceToBoss * 0.3f + distanceToPlayer * 0.7f;
            
            // Prefer platforms in a different direction from the obstacle
            Vector2 platformDirection = platform.position - transform.position;
            if (isFacingRight && platformDirection.x < 0 || !isFacingRight && platformDirection.x > 0)
            {
                score *= 0.7f; // Bonus for platforms in the other direction
            }
            
            if (score < bestScore)
            {
                bestScore = score;
                bestPlatform = platform;
            }
        }
        
        return bestPlatform;
    }
    
    private IEnumerator UnstuckCoroutine()
    {
        while (true)
        {
            // Wait for the check interval
            yield return new WaitForSeconds(unstuckCheckInterval);
            
            // Skip if we're attacking, jumping or teleporting
            if (currentState == BossState.Attacking || 
                currentState == BossState.Jumping || 
                currentState == BossState.Teleporting)
            {
                continue;
            }
            
            // Check if we've moved significantly
            float distanceMoved = Vector2.Distance(lastPosition, transform.position);
            
            if (distanceMoved < minDistanceToMove)
            {
                if (!potentiallyStuck)
                {
                    // First time detecting potential stuck state
                    potentiallyStuck = true;
                    stuckTimer = Time.time;
                }
                else if (Time.time - stuckTimer > 2f) // Reduced from 3s to 2s for faster response
                {
                    // We've been potentially stuck for 2 seconds
                    // Try to unstuck
                    AttemptToUnstuck();
                    potentiallyStuck = false;
                }
            }
            else
            {
                // We've moved enough, reset the stuck state
                potentiallyStuck = false;
            }
            
            // Update the last position
            lastPosition = transform.position;
        }
    }
    
    private void AttemptToUnstuck()
    {
        Debug.Log("Boss attempting to unstuck!");
        SetState(BossState.Unstucking);
        
        unstuckAttempts++;
        
        // Try different unstucking methods based on the situation
        
        // 1. Check if we're stuck against a wall
        RaycastHit2D wallRight = Physics2D.Raycast(
            transform.position,
            Vector2.right,
            0.7f,
            groundLayer
        );
        
        RaycastHit2D wallLeft = Physics2D.Raycast(
            transform.position,
            Vector2.left,
            0.7f,
            groundLayer
        );
        
        if (wallRight.collider != null || wallLeft.collider != null)
        {
            // We might be against a wall, try to jump
            if (IsGrounded())
            {
                Jump(2f);
                
                // Push away from the wall
                Vector2 pushDirection = wallRight.collider != null ? Vector2.left : Vector2.right;
                rb.AddForce(pushDirection * 10f, ForceMode2D.Impulse);
                
                // Change facing direction
                if (pushDirection == Vector2.right && !isFacingRight || pushDirection == Vector2.left && isFacingRight)
                {
                    isFacingRight = !isFacingRight;
                    FlipSprite();
                }
            }
        }
        // 2. We might be stuck on a platform edge
        else if (IsGrounded())
        {
            // Try a bigger jump
            rb.AddForce(Vector2.up * jumpForce * 1.5f, ForceMode2D.Impulse);
            
            // Random horizontal force
            float randomDirection = Random.Range(-1f, 1f);
            rb.AddForce(Vector2.right * randomDirection * 8f, ForceMode2D.Impulse);
            
            // Update facing
            if (randomDirection > 0 && !isFacingRight || randomDirection < 0 && isFacingRight)
            {
                isFacingRight = !isFacingRight;
                FlipSprite();
            }
            
            // Play jump animation
            animator.SetTrigger("jump");
        }
        // 3. We might be stuck in the air
        else
        {
            // Apply downward force to get back to ground
            rb.AddForce(Vector2.down * 15f, ForceMode2D.Impulse);
            
            // Small random horizontal movement
            float randomDirection = Random.Range(-1f, 1f);
            rb.AddForce(Vector2.right * randomDirection * 5f, ForceMode2D.Impulse);
        }
        
        // If we've been stuck multiple times, teleport after a short delay
        if (unstuckAttempts >= 2)
        {
            StartCoroutine(TeleportIfStillStuck());
            unstuckAttempts = 0;
        }
        
        // Return to normal state after a delay
        StartCoroutine(ReturnToNormalState());
    }
    
    private IEnumerator ReturnToNormalState()
    {
        yield return new WaitForSeconds(1f);
        SetState(BossState.Idle);
    }
    
    private IEnumerator TeleportIfStillStuck()
    {
        // Give the previous unstuck attempt some time to work
        yield return new WaitForSeconds(1.5f); // Reduced from 2s to 1.5s
        
        // Check if we're still potentially stuck
        if (currentState == BossState.Unstucking)
        {
            Debug.Log("Boss teleporting to escape stuck state");
            
            // Try teleporting closer to the player's position
            TeleportSmartly(player.position.y - transform.position.y, 
                           Mathf.Abs(player.position.x - transform.position.x));
            
            // Reset our stuck detection
            potentiallyStuck = false;
            unstuckAttempts = 0;
        }
    }
    
    #endregion
    
    private void ChasePlayer()
    {
        if (currentState == BossState.Attacking || isPhasing) return;
        
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        
        SetState(BossState.Chasing);
        
        // Update animation
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
    }
    
    private void Jump(float heightNeeded)
    {
        if (!IsGrounded()) return;
        
        SetState(BossState.Jumping);
        
        // Adjust jump force based on required height
        float adjustedJumpForce = jumpForce;
        if (heightNeeded > 2.5f)
        {
            adjustedJumpForce *= highLedgeJumpForceMultiplier;
        }
        
        rb.AddForce(new Vector2(0, adjustedJumpForce), ForceMode2D.Impulse);
        nextJumpTime = Time.time + jumpCooldown;
        
        // Play jump animation
        animator.SetTrigger("jump");
    }
    
    private IEnumerator PhaseDownThroughPlatform()
    {
        // Find platform below us
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
        PlatformEffector2D platform = hit.collider?.GetComponent<PlatformEffector2D>();
        
        if (platform != null)
        {
            isPhasing = true;
            SetState(BossState.Phasing);
            
            // Disable collisions with platform
            Physics2D.IgnoreCollision(physicsCollider, hit.collider, true);
            
            // Apply downward force
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -phaseSpeed);
            
            // Play phasing animation
            animator.SetTrigger("phase");
            
            // Wait until we've cleared the platform
            yield return new WaitForSeconds(0.5f);
            
            // Re-enable collisions
            Physics2D.IgnoreCollision(physicsCollider, hit.collider, false);
            isPhasing = false;
            
            // Return to idle
            SetState(BossState.Idle);
        }
    }
    
    private void FacePlayer()
    {
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            FlipSprite();
        }
    }
    
    private void FlipSprite()
    {
        // Flip the sprite
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    
    #region Attack Methods
    
    private void PerformLightAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face the player before attacking
        FacePlayer();
        
        // Play animation
        animator.ResetTrigger("lightAttack"); // NEW: Reset before setting
        animator.SetTrigger("lightAttack");
        
        // Set cooldown
        nextLightAttackTime = Time.time + lightAttackCooldown;
        
        // Ensure attack ends - failsafe
        StartCoroutine(EndAttackAfterDelay(0.6f)); // NEW: Failsafe timer
    }
    
    private void PerformHeavyAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face the player before attacking
        FacePlayer();
        
        // Play animation
        animator.ResetTrigger("heavyAttack"); // NEW: Reset before setting
        animator.SetTrigger("heavyAttack");
        
        // Set cooldown
        nextHeavyAttackTime = Time.time + heavyAttackCooldown;
        
        // Ensure attack ends - failsafe
        StartCoroutine(EndAttackAfterDelay(0.8f)); // NEW: Failsafe timer
    }
    
    private void PerformAreaAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
        animator.ResetTrigger("areaAttack"); // NEW: Reset before setting
        animator.SetTrigger("areaAttack");
        
        // Set cooldown
        nextAreaAttackTime = Time.time + areaAttackCooldown;
        
        // Show area attack indicator if available
        if (areaAttackIndicator != null)
        {
            GameObject indicator = Instantiate(areaAttackIndicator, areaAttackPoint.position, Quaternion.identity);
            indicator.transform.localScale = new Vector3(areaAttackRadius * 2, areaAttackRadius * 2, 1);
            Destroy(indicator, 0.5f);
        }
        
        // Ensure attack ends - failsafe
        StartCoroutine(EndAttackAfterDelay(1.0f)); // NEW: Failsafe timer
    }
    
    private void PerformProjectileAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face the player before attacking
        FacePlayer();
        
        // Play animation
        animator.ResetTrigger("projectileAttack"); // NEW: Reset before setting
        animator.SetTrigger("projectileAttack");
        
        // Set cooldown
        nextProjectileTime = Time.time + projectileCooldown;
        
        // Ensure attack ends - failsafe
        StartCoroutine(EndAttackAfterDelay(0.7f)); // NEW: Failsafe timer
    }
    
    // NEW: Failsafe to ensure attack state ends
    private IEnumerator EndAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // If we're still in attacking state, force end it
        if (currentState == BossState.Attacking)
        {
            EndAttackState();
        }
    }
    
    private void DealLightAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(lightAttackPoint.position, lightAttackRange, playerLayer);
        
        if (hitPlayer != null)
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(scaledLightAttackDamage);
            }
        }
    }
    
    private void DealHeavyAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(heavyAttackPoint.position, heavyAttackRange, playerLayer);
        
        if (hitPlayer != null)
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(scaledHeavyAttackDamage);
                
                // Add knockback to player
                Rigidbody2D playerRb = hitPlayer.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (hitPlayer.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDir * 10f, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    private void DealAreaAttackDamage()
    {
        // Find all players in radius
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(areaAttackPoint.position, areaAttackRadius, playerLayer);
        
        foreach (Collider2D player in hitPlayers)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(scaledAreaAttackDamage);
                
                // Add knockback to player (away from boss)
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDir * 8f, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    private void FireProjectiles()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;
        
        // Calculate base direction to player
        Vector2 directionToPlayer = (player.position - projectileSpawnPoint.position).normalized;
        
        // Fire multiple projectiles with spread
        for (int i = 0; i < projectileCount; i++)
        {
            // Calculate spread angle
            float angle = 0;
            if (projectileCount > 1)
            {
                angle = -projectileSpreadAngle + (2 * projectileSpreadAngle * i / (projectileCount - 1));
            }
            
            // Rotate direction by spread angle
            Vector2 spreadDirection = RotateVector(directionToPlayer, angle);
            
            // Create projectile
            GameObject projectile = Instantiate(
                projectilePrefab, 
                projectileSpawnPoint.position, 
                Quaternion.identity
            );
            
            // Set projectile velocity
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = spreadDirection * projectileSpeed;
                
                // Set projectile rotation to match direction
                float projectileAngle = Mathf.Atan2(spreadDirection.y, spreadDirection.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.AngleAxis(projectileAngle, Vector3.forward);
            }
            
            // Apply damage scaling to projectile if it has an EnemyProjectile component
            EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
            if (enemyProjectile != null)
            {
                enemyProjectile.ApplyDamageScaling(GameProgressionData.enemyDamageMultiplier);
            }
        }
    }
    
    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    private void EndAttackState()
    {
        SetState(BossState.Idle);
    }
    
    #endregion
    
    private bool IsGrounded()
    {
        // Simple ground check using raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + new Vector3(0, -0.5f, 0), 
            Vector2.down, 
            0.5f, 
            groundLayer
        );
        return hit.collider != null;
    }
    
    private bool IsOnPlatform()
    {
        // Check if we're on a platform with a platform effector
        if (!IsGrounded()) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + new Vector3(0, -0.5f, 0),
            Vector2.down,
            0.5f,
            groundLayer
        );
        
        return hit.collider != null && hit.collider.GetComponent<PlatformEffector2D>() != null;
    }
    
    private void SetState(BossState newState)
    {
        // Skip if we're already in this state
        if (currentState == newState) return;
        
        // Save previous state
        previousState = currentState;
        currentState = newState;
        
        // Update animator parameters
        animator.SetBool("isAttacking", newState == BossState.Attacking);
        animator.SetBool("isJumping", newState == BossState.Jumping);
        animator.SetBool("isPhasing", newState == BossState.Phasing);
        
        // Debug log state transitions
        Debug.Log($"Boss state changed: {previousState} -> {currentState}");
    }
    
    // Visualize attack ranges in editor
    private void OnDrawGizmosSelected()
    {
        // Light attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightAttackDistance);
        if (lightAttackPoint != null)
        {
            Gizmos.DrawWireSphere(lightAttackPoint.position, lightAttackRange);
        }
        
        // Heavy attack range
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
        Gizmos.DrawWireSphere(transform.position, heavyAttackDistance);
        if (heavyAttackPoint != null)
        {
            Gizmos.DrawWireSphere(heavyAttackPoint.position, heavyAttackRange);
        }
        
        // Area attack range
        Gizmos.color = Color.red;
        if (areaAttackPoint != null)
        {
            Gizmos.DrawWireSphere(areaAttackPoint.position, areaAttackRadius);
        }
        
        // Projectile range (min and max)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minProjectileRange);
        Gizmos.color = new Color(0f, 0.5f, 1f); // Light blue
        Gizmos.DrawWireSphere(transform.position, maxProjectileRange);
        
        // Max follow distance
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
        
        // Ledge detection
        Gizmos.color = Color.green;
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + new Vector2(direction.x * 0.5f, -0.5f);
        Gizmos.DrawRay(origin, Vector2.down * ledgeDetectionDistance);
        
        // Obstacle detection
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * obstacleAvoidanceDistance);
        
        // Platform detection
        if (targetPlatformFound)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPlatformPosition);
            Gizmos.DrawWireSphere(targetPlatformPosition, 0.5f);
        }
    }
}