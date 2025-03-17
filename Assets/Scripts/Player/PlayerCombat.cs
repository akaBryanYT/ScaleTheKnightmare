using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.0f;  // Shorter cooldown
    
    [Header("Attack Speed")]
    [SerializeField] private float attackCancelTime = 0.05f;  // Time after damage when you can cancel attack
    
    [Header("Ranged Attack")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    
    private float nextAttackTime = 0f;
    private Animator animator;
    private bool inMeleeMode = true;
    private PlayerMovement playerMovement;
    private bool isPerformingAttack = false;
    private bool hasDamageOccurred = false;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
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
        
        if (arrowPrefab == null) return;
        
        // Create arrow
        GameObject arrow = Instantiate(arrowPrefab, attackPoint.position, Quaternion.identity);
        
        // Use current direction
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        
        // Set arrow properties
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = new Vector2(direction * arrowSpeed, 0);
            
            // Make arrow face the correct direction
            Vector3 currentScale = arrow.transform.localScale;
            if (direction < 0)
            {
                arrow.transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), 
                                                      currentScale.y, 
                                                      currentScale.z);
            }
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