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
    
    [Header("Attack Properties")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float generalAttackCooldown = 1.5f;
    
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
    private enum BossState { Idle, Chasing, Jumping, Phasing, Attacking }
    private BossState currentState = BossState.Idle;
    
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
    
    // Platform phasing
    private PlatformEffector2D currentPlatform;
    private bool isPhasing = false;
    private bool isFacingRight = true;
    
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
    }
    
    private void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }
        
        // Calculate distances
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = player.position.y - transform.position.y;
        float directDistance = Vector2.Distance(transform.position, player.position);
        
        // Face the player
        FacePlayer();
        
        // Determine appropriate action based on player position
        if (directDistance > maxFollowDistance)
        {
            // Player is too far away, try to close the gap
            TeleportCloserToPlayer();
            return;
        }
        
        // Is player above us and we need to jump?
        if (verticalDistance > 1.5f && IsGrounded() && Time.time > nextJumpTime)
        {
            Jump();
            return;
        }
        
        // Should we phase through a platform to reach the player?
        if (verticalDistance < -1.5f && !isPhasing)
        {
            StartCoroutine(PhaseDownThroughPlatform());
            return;
        }
        
        // Attempt attacks based on distance
        if (Time.time > nextGeneralAttackTime)
        {
            bool attackPerformed = false;
            
            // In close range - try light or heavy attacks
            if (horizontalDistance <= lightAttackDistance && Time.time > nextLightAttackTime)
            {
                PerformLightAttack();
                attackPerformed = true;
            }
            else if (horizontalDistance <= heavyAttackDistance && Time.time > nextHeavyAttackTime)
            {
                PerformHeavyAttack();
                attackPerformed = true;
            }
            // Medium range - try area attack
            else if (directDistance <= areaAttackRadius && Time.time > nextAreaAttackTime)
            {
                PerformAreaAttack();
                attackPerformed = true;
            }
            // Long range - try projectile attack
            else if (hasProjectileAttack && directDistance >= minProjectileRange && directDistance <= maxProjectileRange && Time.time > nextProjectileTime)
            {
                PerformProjectileAttack();
                attackPerformed = true;
            }
            
            if (attackPerformed)
            {
                nextGeneralAttackTime = Time.time + generalAttackCooldown;
                return;
            }
        }
        
        // If we haven't attacked or jumped, chase the player
        ChasePlayer();
    }
    
    private void ChasePlayer()
    {
        if (currentState == BossState.Attacking || isPhasing) return;
        
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        
        SetState(BossState.Chasing);
        
        // Update animation
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
    }
    
    private void Jump()
    {
        if (!IsGrounded()) return;
        
        SetState(BossState.Jumping);
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
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
        }
    }
    
    private void TeleportCloserToPlayer()
    {
        // Find a safe position closer to the player
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 teleportPosition = (Vector2)transform.position + direction * 8f;
        
        // Check if the position is valid (not inside terrain)
        Collider2D overlap = Physics2D.OverlapCircle(teleportPosition, 1f, groundLayer);
        if (overlap == null)
        {
            // Play teleport effect/animation
            animator.SetTrigger("teleport");
            
            // Move to the new position
            transform.position = teleportPosition;
        }
    }
    
    private void FacePlayer()
    {
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            // Flip the sprite
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
    
    #region Attack Methods
    
    private void PerformLightAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
        animator.SetTrigger("lightAttack");
        
        // Set cooldown
        nextLightAttackTime = Time.time + lightAttackCooldown;
    }
    
    private void PerformHeavyAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
        animator.SetTrigger("heavyAttack");
        
        // Set cooldown
        nextHeavyAttackTime = Time.time + heavyAttackCooldown;
    }
    
    private void PerformAreaAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
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
    }
    
    private void PerformProjectileAttack()
    {
        SetState(BossState.Attacking);
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play animation
        animator.SetTrigger("projectileAttack");
        
        // Set cooldown
        nextProjectileTime = Time.time + projectileCooldown;
    }
    
    private void DealLightAttackDamage()
    {
        Collider2D hitPlayer = Physics2D.OverlapCircle(lightAttackPoint.position, lightAttackRange, playerLayer);
        
        if (hitPlayer != null)
        {
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(lightAttackDamage);
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
                playerHealth.TakeDamage(heavyAttackDamage);
                
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
                playerHealth.TakeDamage(areaAttackDamage);
                
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
    
    private void SetState(BossState newState)
    {
        currentState = newState;
        
        // Update animator parameters
        animator.SetBool("isAttacking", newState == BossState.Attacking);
        animator.SetBool("isJumping", newState == BossState.Jumping);
        animator.SetBool("isPhasing", newState == BossState.Phasing);
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
    }
}