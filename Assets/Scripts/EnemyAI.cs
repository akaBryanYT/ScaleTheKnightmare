using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float aggroRange = 5f;
    [SerializeField] private float patrolSpeed = 1f; // Slower patrol speed
    
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;
    
    [Header("Ground Checking")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 1f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private bool isFacingRight = true;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    
    // Simple patrol variables
    private float patrolTimer;
    private float patrolDuration = 2f; // How long to walk in one direction
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Create attack point if not assigned
        if (attackPoint == null)
        {
            attackPoint = new GameObject("AttackPoint").transform;
            attackPoint.SetParent(transform);
            attackPoint.localPosition = new Vector3(0.5f, 0, 0);
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
            // Check if in attack range and not currently attacking
            if (distanceToPlayer < 1.5f && Time.time >= nextAttackTime && !isAttacking)
            {
                FacePlayer();
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
            else if (!isAttacking)
            {
                // Chase player
                ChasePlayer();
            }
        }
        else
        {
            // Patrol behavior when not chasing player or attacking
            if (!isAttacking)
            {
                Patrol();
            }
        }
        
        // Update animations
        animator.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
    }
    
    private void FacePlayer()
    {
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
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
    
    private void Attack()
    {
        isAttacking = true;
        
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Trigger attack animation
        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");
        
        // Deal damage after a delay to match animation
        Invoke("DealDamage", 0.3f);
        
        // End attack after animation
        Invoke("EndAttack", 0.6f);
    }
    
    private void EndAttack()
    {
        isAttacking = false;
    }
    
    public void DealDamage()
    {
        // Use attack point to detect player
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        
        if (hitPlayer != null)
        {
            Debug.Log("Enemy dealing damage to player");
            
            // Deal damage to player
            PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }
    
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    
    // Visualize ranges in editor
    private void OnDrawGizmosSelected()
    {
        // Aggro range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Attack point
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
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