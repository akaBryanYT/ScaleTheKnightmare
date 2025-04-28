using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] public int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.0f;  // Shorter cooldown
    
    [Header("Attack Speed")]
    [SerializeField] private float attackCancelTime = 0.05f;  // Time after damage when you can cancel attack
    
    [Header("Ranged Attack")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    
    private float nextAttackTime = 0f;
    private Animator animator;
    public bool inMeleeMode = true;
    private PlayerMovement playerMovement;
    private bool isPerformingAttack = false;
    private bool hasDamageOccurred = false;
    private Camera mainCamera;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        // Weapon switching - allowed any time
        if (Input.GetKeyDown(KeyCode.Q))
        {
            inMeleeMode = !inMeleeMode;
            animator.SetFloat("attackType", inMeleeMode ? 1f : 0f);
            Debug.Log("Weapon mode: " + (inMeleeMode ? "Melee" : "Bow"));
        }
        
        // Attack input - check cooldown and allow attack if previous attack has done damage
        bool canAttack = Time.time >= nextAttackTime;
        
        // Allow new attack if cooldown passed OR if damage already occurred and cancel time passed
        if (canAttack && Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }
    }
    
    private void PerformAttack()
    {
        // Reset states
        isPerformingAttack = true;
        hasDamageOccurred = false;
        
        Debug.Log("Performing attack - Melee: " + inMeleeMode);
        
        // Set the attack type parameter
        animator.SetFloat("attackType", inMeleeMode ? 1f : 0f);
        
        // Reset and trigger the attack animation
        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");
        
        // Schedule end of attack based on animation length
        // This is just a fallback - ideally animation events should handle this
        float attackAnimLength = inMeleeMode ? 0.5f : 0.9f;
        Invoke("ForceEndAttack", attackAnimLength);
    }
    
    // Called by animation event or as fallback
    private void ForceEndAttack()
    {
        if (isPerformingAttack)
        {
            EndAttack();
        }
    }
    
    private void EndAttack()
    {
        isPerformingAttack = false;
        animator.ResetTrigger("attack");
        CancelInvoke("ForceEndAttack");
    }
    
    // Called via Animation Event
    public void DealMeleeDamage()
    {
        Debug.Log("Melee damage dealing function called");
        
        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        // Damage enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
        
        // Mark damage as occurred and set cooldown
        hasDamageOccurred = true;
        nextAttackTime = Time.time + attackCooldown;
        
        // Allow canceling the attack early after a short delay
        Invoke("AllowAttackCancel", attackCancelTime);
    }
    
    // Called via Animation Event
    public void FireArrow()
    {
        Debug.Log("Arrow firing function called");
        
        if (arrowPrefab == null || mainCamera == null) return;
        
        // Get mouse position in screen space
        Vector3 mousePos = Input.mousePosition;
        
        // Convert to world position
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, -mainCamera.transform.position.z));
        
        // Calculate direction to mouse cursor
        Vector2 direction = (worldMousePos - attackPoint.position).normalized;
        
        // Create arrow
        GameObject arrow = Instantiate(arrowPrefab, attackPoint.position, Quaternion.identity);
        
        // Set arrow properties
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            // Set velocity toward cursor
            arrowRb.linearVelocity = direction * arrowSpeed;
            
            // Make arrow face the correct direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // Mark damage as occurred and set cooldown
        hasDamageOccurred = true;
        nextAttackTime = Time.time + attackCooldown;
        
        // Allow canceling the attack early after a short delay
        Invoke("AllowAttackCancel", attackCancelTime);
    }
    
    // Allow canceling the current attack to start a new one
    private void AllowAttackCancel()
    {
        if (hasDamageOccurred)
        {
            EndAttack();
        }
    }
    
    // For external scripts to check if player is attacking
    public bool IsAttacking()
    {
        return isPerformingAttack;
    }
    
    // Visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}