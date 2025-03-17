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
        // Input handling
        horizontal = Input.GetAxisRaw("Horizontal");
        targetSpeed = horizontal * speed;
        
        // Platform dropping
        HandlePlatformDropping();
        
        // Jump handling 
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            
            // Reset platform effectors when jumping (to jump through platforms)
            ResetPlatformEffectors();
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
        
        // Handle direction flipping - ALWAYS allow flipping when moving
        if (horizontal != 0)
        {
            Flip();
        }
        
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
        // Save the original layer
        int originalLayer = gameObject.layer;
        
        // Switch to a layer that doesn't collide with platforms
        // Make sure you set up your Physics2D collision matrix for this!
        gameObject.layer = LayerMask.NameToLayer("PlayerDropping");
        
        // Set all platform effectors to allow dropping
        SetPlatformEffectors(180f);
        
        // Wait a short time to fall through
        yield return new WaitForSeconds(0.3f);
        
        // Reset everything
        gameObject.layer = originalLayer;
        ResetPlatformEffectors();
    }
    
    private void SetPlatformEffectors(float rotation)
    {
        // Find all platform effectors in the scene
        PlatformEffector2D[] effectors = FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None);
        foreach (PlatformEffector2D effector in effectors)
        {
            effector.rotationalOffset = rotation;
        }
    }
    
    private void ResetPlatformEffectors()
    {
        // Reset all platform effectors
        PlatformEffector2D[] effectors = FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None);
        foreach (PlatformEffector2D effector in effectors)
        {
            effector.rotationalOffset = 0f;
        }
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
    
    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
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