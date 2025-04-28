using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Current movement parameters
    public float speed = 8f;
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float accelerationTime = 0.1f;
    [SerializeField] private float decelerationTime = 0.1f;
    
    // Ground check
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    
    // Ceiling check (from CharacterController2D)
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private LayerMask ceilingLayer;
    [SerializeField] private float ceilingCheckRadius = 0.2f;
    
    // Platform dropping
    [SerializeField] private float platformDropDelay = 0.1f;
    private float platformDropTimer = 0f;
    private bool wantsToDrop = false;
    
    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerCombat playerCombat;
    private Camera mainCamera;
    private Collider2D playerCollider;
    
    // State variables
    private float horizontal;
    private bool isFacingRight = true;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCombat = GetComponent<PlayerCombat>();
        mainCamera = Camera.main;
        playerCollider = GetComponent<Collider2D>();
        
        // Create ceiling check if it doesn't exist
        if (ceilingCheck == null)
        {
            ceilingCheck = new GameObject("CeilingCheck").transform;
            ceilingCheck.SetParent(transform);
            ceilingCheck.localPosition = new Vector3(0, 0.7f, 0); // Adjust height as needed
        }
    }
    
    private void Update()
    {
        // Input handling - now only affects movement, not direction
        horizontal = Input.GetAxisRaw("Horizontal");
        targetSpeed = horizontal * speed;
        
        // Platform dropping
        HandlePlatformDropping();
        
        // Jump handling 
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        }
        
        // Variable jump height 
        if (Input.GetButtonUp("Jump"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
        
        // Better feeling jump physics
        BetterJumpPhysics();
        
        // Update animator parameters
        if (playerCombat == null || !playerCombat.IsAttacking())
        {
            UpdateAnimatorParameters();
        }
        
        // Handle direction flipping based on cursor position if player is in ranged
        HandleDirectionFlipping();
        
        // Check ceiling collisions
        CheckCeiling();
    }
    
    private void FixedUpdate()
    {
        // Slow horizontal movement during attacks but don't stop it completely
        float speedMultiplier = playerCombat != null && playerCombat.IsAttacking() ? 0.75f : 1f;
        
        // Smoothly accelerate/decelerate
        if (Mathf.Approximately(targetSpeed, 0f))
        {
            // Deceleration (stopping)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, speed / decelerationTime * Time.fixedDeltaTime);
        }
        else
        {
            // Acceleration or changing direction
            float acceleration = speed / accelerationTime * Time.fixedDeltaTime;
            
            // If changing direction, apply faster acceleration
            if (Mathf.Sign(currentSpeed) != Mathf.Sign(targetSpeed) && currentSpeed != 0)
            {
                acceleration *= 2f;
            }
            
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration);
        }
        
        // Apply horizontal movement with multiplier
        rb.linearVelocity = new Vector2(currentSpeed * speedMultiplier, rb.linearVelocity.y);
    }
    
    private void HandleDirectionFlipping()
{
    if (mainCamera == null) return;

    if (playerCombat != null && !playerCombat.inMeleeMode)
    {
        // Ranged mode: face mouse position
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
        bool shouldFaceRight = worldMousePos.x > transform.position.x;

        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
    else
    {
        // Melee mode: face movement direction
        if (horizontal > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontal < 0 && isFacingRight)
        {
            Flip();
        }
    }
}
    
    private void HandlePlatformDropping()
    {
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            if (IsGrounded())
            {
                wantsToDrop = true;
                platformDropTimer += Time.deltaTime;
                
                if (platformDropTimer >= platformDropDelay)
                {
                    // Temporarily change the layer of the player to not collide with one-way platforms
                    StartCoroutine(DropThroughPlatforms());
                    wantsToDrop = false;
                    platformDropTimer = 0;
                }
            }
        }
        else
        {
            wantsToDrop = false;
            platformDropTimer = 0;
        }
    }
    
    private System.Collections.IEnumerator DropThroughPlatforms()
    {
        // Find the platform the player is standing on
        Collider2D platformCollider = FindCurrentPlatformCollider();
        
        if (platformCollider != null)
        {
            // Temporarily ignore collision between player and this specific platform
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            
            // Apply a small downward force to ensure the player drops
            rb.AddForce(Vector2.down * 5f, ForceMode2D.Impulse);
            
            // Wait a short time to fall through
            yield return new WaitForSeconds(0.3f);
            
            // Re-enable collision
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        else
        {
            // Fallback method if we can't find the specific platform
            // Save the original layer
            int originalLayer = gameObject.layer;
            
            // Switch to a layer that doesn't collide with platforms
            gameObject.layer = LayerMask.NameToLayer("PlayerDropping");
            
            // Wait a short time to fall through
            yield return new WaitForSeconds(0.3f);
            
            // Reset player layer
            gameObject.layer = originalLayer;
        }
    }
    
    private Collider2D FindCurrentPlatformCollider()
    {
        // Cast a ray downward from the player to find the platform
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position, 
            Vector2.down, 
            0.1f, 
            groundLayer
        );
        
        if (hit.collider != null)
        {
            // Check if this has a platform effector (is a one-way platform)
            if (hit.collider.GetComponent<PlatformEffector2D>() != null)
            {
                return hit.collider;
            }
        }
        
        return null;
    }
    
    private void CheckCeiling()
    {
        // Check if there's a ceiling above (similar to CharacterController2D)
        if (Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, ceilingLayer))
        {
            // If there's a ceiling and we're moving upward, stop upward movement
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
    }
    
    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    public bool IsInAir()
    {
        return !IsGrounded();
    }
    
    private void BetterJumpPhysics()
    {
        // Apply higher gravity when falling
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // Apply lower gravity when ascending but not holding jump
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }
    
    private void UpdateAnimatorParameters()
    {
        // Update movement animations
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetBool("isJumping", !IsGrounded());
    }
    
    // This method is now only used when needed by other scripts
    public void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    
    public bool IsFacingRight()
    {
        return isFacingRight;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (ceilingCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
        }
    }
}