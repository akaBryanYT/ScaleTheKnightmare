using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Movement parameters
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
    }
    
	// Change this in your PlayerMovement.cs
	private void Update()
	{
		// Input handling
		horizontal = Input.GetAxisRaw("Horizontal");
		targetSpeed = horizontal * speed;
		
		// Jump handling - only if not attacking
		if (Input.GetButtonDown("Jump") && IsGrounded())
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
		}
		
		// Variable jump height - only if not attacking
		if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
		}
		
		// Better feeling jump physics
		BetterJumpPhysics();
		
		// Update animator parameters
		UpdateAnimatorParameters();
		
		// Handle direction flipping - ALWAYS allow flipping when moving, even during attacks
		if (horizontal != 0)
		{
			Flip();
		}
	}
    
    private void FixedUpdate()
    {
        // Slow horizontal movement during attacks but don't stop it completely
        float speedMultiplier = playerCombat.IsAttacking() ? 0.75f : 1f;
        
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
    
    private void UpdateAnimatorParameters()
    {
        // Update movement parameters
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        
        // Update jump state
        bool isInAir = !IsGrounded();
        animator.SetBool("isJumping", isInAir);
    }
    
    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    // Public method to check if player is in air (for PlayerCombat)
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
}