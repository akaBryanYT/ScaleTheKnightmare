using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float aggroRange = 5f;
    [SerializeField] private float patrolSpeed = 1f;

    [Header("Ground Checking")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 1f;
    
    [Header("Attack Properties")]
    [SerializeField] private float generalAttackCooldown = 1f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Light Attack")]
    [SerializeField] private bool hasLightAttack = true;
    [SerializeField] private Transform lightAttackPoint;
    [SerializeField] private float lightAttackRange = 0.5f;
    [SerializeField] private int lightAttackDamage = 1;
    [SerializeField] private float lightAttackDistance = 1.5f;
    [SerializeField] private float lightAttackCooldown = 1f;
    [SerializeField] private string lightAttackTrigger = "lightAttack";
    
    [Header("Heavy Attack")]
    [SerializeField] private bool hasHeavyAttack = false;
    [SerializeField] private Transform heavyAttackPoint;
    [SerializeField] private float heavyAttackRange = 0.7f;
    [SerializeField] private int heavyAttackDamage = 2;
    [SerializeField] private float heavyAttackDistance = 1.8f;
    [SerializeField] private float heavyAttackCooldown = 3f;
    [SerializeField] private string heavyAttackTrigger = "heavyAttack";
    
    [Header("Area Attack")]
    [SerializeField] private bool hasAreaAttack = false;
    [SerializeField] private Transform areaAttackPoint;
    [SerializeField] private GameObject areaAttackIndicator;
    [SerializeField] private float areaAttackRadius = 3f;
    [SerializeField] private int areaAttackDamage = 1;
    [SerializeField] private float areaAttackCooldown = 5f;
    [SerializeField] private string areaAttackTrigger = "areaAttack";
    
    [Header("Projectile Attack")]
    [SerializeField] private bool hasProjectileAttack = false;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float minProjectileRange = 3f;
    [SerializeField] private float maxProjectileRange = 8f;
    [SerializeField] private float projectileCooldown = 2f;
    [SerializeField] private string projectileAttackTrigger = "projectileAttack";
    
    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private bool isFacingRight = true;
    
    // State tracking
    private bool isAttacking = false;
    
    // Attack cooldown timers
    private float nextLightAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;
    private float nextAreaAttackTime = 0f;
    private float nextProjectileTime = 0f;
    private float nextGeneralAttackTime = 0f;
    
    // Patrol variables
    private float patrolTimer;
    private float patrolDuration = 2f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Create attack points if not assigned
        if (lightAttackPoint == null)
        {
            lightAttackPoint = new GameObject("LightAttackPoint").transform;
            lightAttackPoint.SetParent(transform);
            lightAttackPoint.localPosition = new Vector3(0.5f, 0f, 0f);
        }
        
        if (heavyAttackPoint == null)
        {
            heavyAttackPoint = new GameObject("HeavyAttackPoint").transform;
            heavyAttackPoint.SetParent(transform);
            heavyAttackPoint.localPosition = new Vector3(0.7f, 0f, 0f);
        }
        
        if (areaAttackPoint == null)
        {
            areaAttackPoint = new GameObject("AreaAttackPoint").transform;
            areaAttackPoint.SetParent(transform);
            areaAttackPoint.localPosition = new Vector3(0f, 0f, 0f);
        }
        
        // Create projectile spawn point if needed
        if (hasProjectileAttack && projectileSpawnPoint == null)
        {
            projectileSpawnPoint = new GameObject("ProjectileSpawnPoint").transform;
            projectileSpawnPoint.SetParent(transform);
            projectileSpawnPoint.localPosition = new Vector3(0.5f, 0.25f, 0f);
        }
        
        // Create ground check if not assigned
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -1f, 0);
        }
        
        // Initialize patrol timer
        patrolTimer = Random.Range(0f, patrolDuration);
        
        // Set player layer if not assigned
        if (playerLayer == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }
    }
    
    private void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Check if player is in aggro range
        if (distanceToPlayer < aggroRange)
        {
            // Don't do anything else if already attacking
            if (isAttacking) return;
            
            // Try to attack based on distance and cooldowns
            if (Time.time > nextGeneralAttackTime)
            {
                bool attackPerformed = TryPerformAttack(distanceToPlayer);
                
                // If no attack was performed, chase the player
                if (!attackPerformed)
                {
                    ChasePlayer();
                }
            }
            else
            {
                // Chase player if we can't attack yet
                ChasePlayer();
            }
        }
        else
        {
            // Patrol behavior when not in aggro range and not attacking
            if (!isAttacking)
            {
                Patrol();
            }
        }
        
        // Update animations
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
    }
    
    private bool TryPerformAttack(float distanceToPlayer)
    {
        // Check for projectile attack first (longest range)
        if (hasProjectileAttack && 
            distanceToPlayer >= minProjectileRange && 
            distanceToPlayer <= maxProjectileRange && 
            Time.time > nextProjectileTime)
        {
            PerformProjectileAttack();
            return true;
        }
        
        // Check for area attack next
        if (hasAreaAttack && 
            distanceToPlayer <= areaAttackRadius && 
            Time.time > nextAreaAttackTime)
        {
            PerformAreaAttack();
            return true;
        }
        
        // Check for heavy attack
        if (hasHeavyAttack && 
            distanceToPlayer <= heavyAttackDistance && 
            Time.time > nextHeavyAttackTime)
        {
            // Only 30% chance to perform heavy attack when available
            if (Random.value < 0.3f)
            {
                PerformHeavyAttack();
                return true;
            }
        }
        
        // Check for light attack (most common)
        if (hasLightAttack && 
            distanceToPlayer <= lightAttackDistance && 
            Time.time > nextLightAttackTime)
        {
            PerformLightAttack();
            return true;
        }
        
        return false;
    }
    
    private void ChasePlayer()
    {
        // Direction to player
        float directionToPlayer = player.position.x - transform.position.x;
        
        // Face the player
        bool shouldFaceRight = directionToPlayer > 0;
        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
        
        // Move towards player
        rb.linearVelocity = new Vector2(Mathf.Sign(directionToPlayer) * moveSpeed, rb.linearVelocity.y);
    }
    
    private void Patrol()
    {
        // Very simple patrol: walk in current direction for a duration, then turn around
        patrolTimer += Time.deltaTime;
        
        if (patrolTimer >= patrolDuration)
        {
            // Time to turn around
            Flip();
            patrolTimer = 0f;
        }
        
        // Simple ground check in front
        bool shouldTurn = NeedToTurn();
        if (shouldTurn)
        {
            Flip();
            patrolTimer = 0f;
        }
        
        // Move in facing direction
        float direction = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
    }
    
    private bool NeedToTurn()
    {
        // Cast a ray downward from the front edge to check for ground
        Vector3 frontPos = groundCheck.position + (isFacingRight ? Vector3.right : Vector3.left) * 0.5f;
        RaycastHit2D groundAhead = Physics2D.Raycast(frontPos, Vector2.down, groundCheckDistance, groundLayer);
        
        // Cast a ray forward to check for walls
        Vector3 wallCheckPos = transform.position + Vector3.up * 0.5f;
        RaycastHit2D wallAhead = Physics2D.Raycast(wallCheckPos, isFacingRight ? Vector2.right : Vector2.left, 0.5f, groundLayer);
        
        // Turn if there's no ground ahead or if there's a wall
        return (groundAhead.collider == null || wallAhead.collider != null);
    }
    
    #region Attack Methods
    
    private void PerformLightAttack()
    {
        isAttacking = true;
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face player before attacking
        FacePlayer();
        
        // Play animation
        animator.SetTrigger(lightAttackTrigger);
        
        // Set cooldowns
        nextLightAttackTime = Time.time + lightAttackCooldown;
        nextGeneralAttackTime = Time.time + generalAttackCooldown;
    }
    
    private void PerformHeavyAttack()
    {
        isAttacking = true;
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face player before attacking
        FacePlayer();
        
        // Play animation
        animator.SetTrigger(heavyAttackTrigger);
        
        // Set cooldowns
        nextHeavyAttackTime = Time.time + heavyAttackCooldown;
        nextGeneralAttackTime = Time.time + generalAttackCooldown;
    }
    
    private void PerformAreaAttack()
    {
        isAttacking = true;
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
        animator.SetTrigger(areaAttackTrigger);
        
        // Set cooldowns
        nextAreaAttackTime = Time.time + areaAttackCooldown;
        nextGeneralAttackTime = Time.time + generalAttackCooldown;
        
        // Show area attack indicator if available
        if (areaAttackIndicator != null)
        {
            GameObject indicator = Instantiate(areaAttackIndicator, areaAttackPoint.position, Quaternion.identity);
            indicator.transform.localScale = new Vector3(areaAttackRadius * 2, areaAttackRadius * 2, 1);
            Destroy(indicator, 0.4f);
        }
    }
    
    private void PerformProjectileAttack()
    {
        isAttacking = true;
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Face player before attacking
        FacePlayer();
        
        // Play animation
        animator.SetTrigger(projectileAttackTrigger);
        
        // Set cooldowns
        nextProjectileTime = Time.time + projectileCooldown;
        nextGeneralAttackTime = Time.time + generalAttackCooldown;
    }
    
    private void DealLightAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(lightAttackPoint.position, lightAttackRange, playerLayer);
        
        if (hitPlayer != null)
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Apply damage scaling
                int scaledDamage = Mathf.RoundToInt(lightAttackDamage * GameProgressionData.enemyDamageMultiplier);
                playerHealth.TakeDamage(scaledDamage);
                
                // Debug log scaled damage
                if (GameProgressionData.progressionLevel > 0)
                {
                    Debug.Log($"Enemy light attack scaled: {lightAttackDamage} → {scaledDamage} damage");
                }
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
                // Apply damage scaling
                int scaledDamage = Mathf.RoundToInt(heavyAttackDamage * GameProgressionData.enemyDamageMultiplier);
                playerHealth.TakeDamage(scaledDamage);
                
                // Add knockback to player
                Rigidbody2D playerRb = hitPlayer.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (hitPlayer.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDir * 8f, ForceMode2D.Impulse);
                }
                
                // Debug log scaled damage
                if (GameProgressionData.progressionLevel > 0)
                {
                    Debug.Log($"Enemy heavy attack scaled: {heavyAttackDamage} → {scaledDamage} damage");
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
                // Apply damage scaling
                int scaledDamage = Mathf.RoundToInt(areaAttackDamage * GameProgressionData.enemyDamageMultiplier);
                playerHealth.TakeDamage(scaledDamage);
                
                // Add small knockback
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
                }
                
                // Debug log scaled damage
                if (GameProgressionData.progressionLevel > 0)
                {
                    Debug.Log($"Enemy area attack scaled: {areaAttackDamage} → {scaledDamage} damage");
                }
            }
        }
    }
    
    private void FireProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;
        
        // Calculate direction to player
        Vector2 directionToPlayer = (player.position - projectileSpawnPoint.position).normalized;
        
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
            projectileRb.linearVelocity = directionToPlayer * projectileSpeed;
            
            // Set projectile rotation to match direction
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // Apply damage scaling to projectile if it has an EnemyProjectile component
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.ApplyDamageScaling(GameProgressionData.enemyDamageMultiplier);
        }
    }
    
    private void EndAttackState()
    {
        isAttacking = false;
    }
    
    #endregion
    
    private void FacePlayer()
    {
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
    }
    
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    
    // Visualize attack ranges in editor
    private void OnDrawGizmosSelected()
    {
        // Aggro range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Light attack range
        if (hasLightAttack && lightAttackPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, lightAttackDistance);
            Gizmos.DrawWireSphere(lightAttackPoint.position, lightAttackRange);
        }
        
        // Heavy attack range
        if (hasHeavyAttack && heavyAttackPoint != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, heavyAttackDistance);
            Gizmos.DrawWireSphere(heavyAttackPoint.position, heavyAttackRange);
        }
        
        // Area attack range
        if (hasAreaAttack && areaAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(areaAttackPoint.position, areaAttackRadius);
        }
        
        // Projectile range
        if (hasProjectileAttack)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minProjectileRange);
            Gizmos.color = new Color(0f, 0.5f, 1f); // Light blue
            Gizmos.DrawWireSphere(transform.position, maxProjectileRange);
        }
        
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(groundCheck.position, Vector2.down * groundCheckDistance);
            
            // Forward ground check
            Vector3 frontPos = groundCheck.position + (transform.localScale.x > 0 ? Vector3.right : Vector3.left) * 0.5f;
            Gizmos.DrawRay(frontPos, Vector2.down * groundCheckDistance);
            
            // Wall check
            Vector3 wallCheckPos = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawRay(wallCheckPos, (transform.localScale.x > 0 ? Vector2.right : Vector2.left) * 0.5f);
        }
    }
}