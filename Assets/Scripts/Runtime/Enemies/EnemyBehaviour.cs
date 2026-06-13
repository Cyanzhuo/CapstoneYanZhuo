using UnityEngine;
using TMPro;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("Stats")]
    public int health = 100;
    public int damageAmount = 10;
    int collisionDamage = 5;
    float timeTillDemise = 0.5f;
    float demiseTimer = 0f;
    bool beingPushed;
    Vector3 pushDirection;
    Vector3 softKnockback;

    [Header("Payback Gauge")]
    [SerializeField] private float maxPayback = 50f;
    [SerializeField] private float currentPayback = 0f;
    
    [Header("Gravity")]
    float gravityMultiplier = 2f;
    float lowGravityMultiplier = 0.5f;
    float slowFallTimer = 0f;
    float slowFallTime = 0.2f;
    bool pauseFastFall;

    [Header("Detection")]
    public float groundCheckDistance = 0.5f;
    [SerializeField] private float groundSphereRadius = 0.2f;

    [Header("UI")]
    public TMP_Text healthText;

    // Components cached for performance
    private Rigidbody rb;
    NavMeshAgent myAgent;
    Chaser chaser;
    [SerializeField] private Attack attack;

    public enum EnemyState
    {
        Normal, Hitstun, Knockback, Spiked, Rebound
    }
    [HideInInspector] public EnemyState currentState = EnemyState.Normal;
    [SerializeField] private float counterChance = 0.5f;
    private bool counterTriggered = false;
    private bool enraged = false;

    /// <summary>
    /// Checks if the enemy is currently touching the floor.
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            Vector3 rayOrigin = transform.position + (Vector3.up * 0.5f);
            return Physics.SphereCast(rayOrigin, groundSphereRadius, Vector3.down, out _, groundCheckDistance);
        }
    }

    void Awake()
    {
        // Cache references once at the very start
        rb = GetComponent<Rigidbody>();
        myAgent = GetComponent<NavMeshAgent>();
        chaser = GetComponent<Chaser>();
        if (attack == null)
        {
            attack = FindFirstObjectByType<Attack>();
        }
    }

    void Start()
    {
        UpdateHealthText();
    }

    void Update()
    {
        if (IsGrounded)
        {
            switch (currentState) // Handle state-specific logic when spiked into the ground
            {
                case EnemyState.Spiked:
                    currentState = EnemyState.Normal;
                    TakeDamage(collisionDamage);
                    break;
                case EnemyState.Rebound:
                    currentState = EnemyState.Normal;
                    TakeDamage(collisionDamage);
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Reset vertical velocity
                    rb.AddForce(Vector3.up * (attack.launcherForce + 1), ForceMode.VelocityChange);
                    break;
            }
            if (demiseTimer > 0)
            {
                demiseTimer -= Time.deltaTime;
                if (demiseTimer <= 0)
                {
                    Die();
                }
            }

            pauseFastFall = false;
        }
        else
        {
            if (pauseFastFall && slowFallTimer > 0)
            {
                slowFallTimer -= Time.deltaTime;
                if (slowFallTimer <= 0)
                {
                    pauseFastFall = false;
                }
            }
        }
        if (beingPushed)
        {
            if (attack != null && attack.currentAttackType != Attack.AttackType.AerialPush)
            {
                Knockback(softKnockback.normalized, softKnockback.magnitude, false);
                pauseFastFall = false;
                beingPushed = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0 && !IsGrounded)
        {
            if (pauseFastFall)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowGravityMultiplier - 1) * Time.fixedDeltaTime; // Apply reduced gravity for a short time after dashing
            }
            else
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime; // Apply extra gravity when falling for snappier jumps
            }
        }
        if (beingPushed)
        {
            ApplyPush();
        }
    }

    #region Combat Logic
    public void TakeDamage(int amount, int payback = 0, bool shouldTriggerCounter = false)
    {
        if (enraged)
        {
            amount /= 2;
            float randomValue = Random.value; // 0 to 1
            if (randomValue < counterChance && shouldTriggerCounter)
            {
                TriggerCounter();
                return;
            }
        }
        health -= amount;
        currentPayback = Mathf.Clamp(currentPayback + payback, 0, maxPayback);
        enraged = currentPayback == maxPayback;

        if (health <= 0)
        {
            health = 0;
            demiseTimer = timeTillDemise; // Start the death timer
        }

        UpdateHealthText();
    }
    
    void TriggerCounter()
    {
        counterTriggered = true;
        if (chaser != null)
        {
            chaser.SwitchState(Chaser.State.Attack);
        }
    }

    public void Knockback(Vector3 direction, float force, bool shouldJuggle)
    {
        if (counterTriggered)
        {
            counterTriggered = false;
            return;
        }
        
        if (myAgent != null && myAgent.enabled)
        {
            myAgent.enabled = false; // Disable NavMeshAgent to allow for physics-based knockback
        }
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            
            Vector3 knockback = direction.normalized * force;
            
            if (shouldJuggle && !IsGrounded)
            {
                knockback += Vector3.up * attack.juggleForce;
                pauseFastFall = true;
                slowFallTimer = slowFallTime;
            }
            
            rb.AddForce(knockback, ForceMode.VelocityChange);
        }
        
        if (chaser != null)
        {
            chaser.SwitchState(Chaser.State.Knockback);
        }
    }

    public void Push(Vector3 direction, Vector3 verticalMotion, Vector3 knockback)
    {
        if (myAgent != null && myAgent.enabled)
        {
            myAgent.enabled = false; // Disable NavMeshAgent to allow for physics-based knockback
        }
        if (rb != null)
        {
            rb.isKinematic = false;
            beingPushed = true;
            pauseFastFall = true;
            pushDirection = direction;
            softKnockback = knockback;
            rb.linearVelocity = new Vector3(direction.x, verticalMotion.y, direction.z);
        }

        if (chaser != null)
        {
            chaser.SwitchState(Chaser.State.Knockback);
        }
    }

    void ApplyPush()
    {
        if (rb != null)
        {
            pushDirection.y = rb.linearVelocity.y;
            rb.linearVelocity = pushDirection;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            Die();
        }
    }
    #endregion

    #region Feedback & Cleanup
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = health.ToString();
        }
    }

    private void Die()
    {
        // Place for death particles or sound triggers
        Debug.Log($"{gameObject.name} defeated.");
        Destroy(gameObject);
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        // Visualize ground check in editor
        Gizmos.color = Color.yellow;
        Vector3 rayOrigin = transform.position + (Vector3.up * 0.5f);
        Gizmos.DrawWireSphere(rayOrigin + (Vector3.down * groundCheckDistance), groundSphereRadius);
    }
}