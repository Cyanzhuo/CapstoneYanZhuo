using System.Collections;
using Game.Audio;
using UnityEngine;
using UnityEngine.AI;

public class Chaser : MonoBehaviour, IEnemyAI
{
    NavMeshAgent myAgent;
    Rigidbody rb;
    EnemyBehaviour enemyBehaviour;
    Animator animator;
    [SerializeField] EnemyHitbox hitbox;

    [Header("Audio")]
    [SerializeField] private InterimAudioCue attackStartCue = InterimAudioCue.BasicAttack;
    [SerializeField] private InterimAudioCue attackHitCue = InterimAudioCue.BasicAttackHit;

    [SerializeField] Transform targetTransform;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float playerProximityThreshold = 1f;
    [SerializeField] Transform centerPoint;
    [SerializeField] Vector3 attackBoxSize = new Vector3(0.6f, 1.2f, 0.6f);
    [SerializeField] float idleDuration = 3f; // Time to stay in Idle state before patrolling
    [SerializeField] float focusDuration = 5f; // Time to stay in FocusOnTarget state
    [SerializeField] float retreatDuration = 2f; // Time to stay in Retreat state
    [SerializeField] float directionChangeInterval = 3f; // Time interval to change circling direction in FocusOnTarget state
    [SerializeField] float rotationSpeed = 60f; // Speed at which the enemy rotates to face the target in FocusOnTarget state
    [SerializeField] float minRadius = 3f; // Minimum radius to maintain while circling the target
    private float orbitAngle = 0f;
    private Vector3 directionToTarget;

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 5f;  // How far from start position to roam
    [SerializeField] private float patrolRadius = 5f;  // Search radius for NavMesh sampling
    private Vector3 startPosition;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    [Header("Focus State")]
    [SerializeField] private float focusMoveSpeed = 2f; // Slower than normal chase speed
    private float originalSpeed;

    [Header("Only for Boss")]
    [SerializeField] private GameObject bombPrefab; // Prefab for the bomb to drop in phase two
    [SerializeField] private float bossP2FocusMoveSpeed = 3f;
    [SerializeField] private float bossP2ChaseMoveSpeed = 3.5f;

    public enum State
    {
        Idle, Patrol, FocusOnTarget, ChaseTarget, Attack, Retreat, Knockback
    }
    public State currentState;

    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        enemyBehaviour = GetComponent<EnemyBehaviour>();

        if (hitbox != null)
        {
            hitbox.SetHitCue(attackHitCue);
        }
    }

    void Start()
    {
        originalSpeed = myAgent.speed;
        startPosition = transform.position;
        SwitchState(State.Idle);
        StartCoroutine(StateMachine());
        rb.isKinematic = true; // Start with kinematic rigidbody for NavMeshAgent control
    }

    void Update()
    {
        if (currentState != State.FocusOnTarget && orbitAngle != 0f)
        {
            orbitAngle = 0f; // Reset orbit angle when not in FocusOnTarget state
        }

        UpdateAnimation();
    }

    public void SwitchState(State newState)
    {
        if (currentState == newState)
        {
            return; // Exit if the state is already the same
        }

        currentState = newState;

        if (newState == State.FocusOnTarget)
        {
            if (enemyBehaviour != null && enemyBehaviour.isBoss && enemyBehaviour.isInPhaseTwo)
            {
                myAgent.speed = bossP2FocusMoveSpeed;
            }
            else
            {
                myAgent.speed = focusMoveSpeed;
            }
        }
        else
        {
            if (enemyBehaviour != null && enemyBehaviour.isBoss && enemyBehaviour.isInPhaseTwo)
            {
                myAgent.speed = bossP2ChaseMoveSpeed;
            }
            else
            {
                myAgent.speed = originalSpeed;
            }
        }
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case State.Idle:
                    yield return StartCoroutine(Idle());
                    break;
                case State.Patrol:
                    yield return StartCoroutine(Patrol());
                    break;
                case State.FocusOnTarget:
                    yield return StartCoroutine(FocusOnTarget());
                    break;
                case State.ChaseTarget:
                    yield return StartCoroutine(ChaseTarget());
                    break;
                case State.Attack:
                    yield return StartCoroutine(Attack());
                    break;
                case State.Retreat:
                    yield return StartCoroutine(Retreat());
                    break;
                case State.Knockback:
                    yield return StartCoroutine(Knockback());
                    break;
            }

            yield return null;
        }
    }

    IEnumerator Idle()
    {
        float idleTimer = 0f;
        
        while (currentState == State.Idle)
        {
            idleTimer += Time.deltaTime;

            // After idleDuration seconds, switch to Patrol state
            if (idleTimer >= idleDuration)
            {
                SwitchState(State.Patrol);
                yield break;
            }

            yield return null; // Wait for the next frame
        }
    }

    IEnumerator FocusOnTarget()
    {
        float notAttackingTimer = 0f;
        float directionChangeTimer = 0f;
        float circleDirection = 1f; // 1 for clockwise, -1 for counter
        if (animator != null)
        {
            animator.SetFloat("StrafeDirection", circleDirection);
        }
        // make the enemy turn to face the player and pace slowly around the player
        while (currentState == State.FocusOnTarget)
        {
            notAttackingTimer += Time.deltaTime;
            directionChangeTimer += Time.deltaTime;

            // Change circling direction at intervals
            if (directionChangeTimer >= directionChangeInterval)
            {
                circleDirection *= -1f;
                directionChangeTimer = 0f;
                if (animator != null)
                {
                    animator.SetFloat("StrafeDirection", circleDirection);
                }
                if (enemyBehaviour != null && enemyBehaviour.isBoss && enemyBehaviour.isInPhaseTwo)
                {
                    Instantiate(bombPrefab, transform.position, Quaternion.identity);
                }
            }

            FaceTarget();

            // Move around the target in a circle
            orbitAngle += Time.deltaTime * circleDirection;
            Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * Mathf.Max(directionToTarget.magnitude, minRadius);
            myAgent.SetDestination(targetTransform.position + offset);

            if (notAttackingTimer >= focusDuration)
            {
                SwitchState(State.ChaseTarget);
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator ChaseTarget()
    {
        // while loop in a coroutine = mini Update function
        while (currentState == State.ChaseTarget)
        {
            // Perform chasing behavior here
            if (targetTransform != null)
            {
                myAgent.SetDestination(targetTransform.position);
                Vector3 boxCenter = centerPoint.position + transform.forward * playerProximityThreshold;
                Collider[] hits = Physics.OverlapBox(boxCenter, attackBoxSize * 0.5f, transform.rotation, playerLayer);
                if (hits.Length > 0)
                {
                    SwitchState(State.Attack);
                }
            }
            
            yield return null;
        }
    }

    IEnumerator Attack()
    {
        InterimAudioDirector.TryPlayMove(attackStartCue, transform.position);
        FaceTarget();
        animator.SetTrigger("Attack");
        
        while (currentState == State.Attack)
        {
            yield return null;
        }
    }
    
    IEnumerator Retreat()
    {
        // Move backwards for a short duration, then switch back to focus on target
        float retreatTimer = 0f;
        while (currentState == State.Retreat)
        {
            if (targetTransform == null)
            {
                SwitchState(State.Idle);
                yield break;
            }
            // Move backwards away from the target
            Vector3 retreatDirection = (transform.position - targetTransform.position).normalized;
            myAgent.Move(retreatDirection * myAgent.speed * Time.deltaTime);

            retreatTimer += Time.deltaTime;
            if (retreatTimer >= retreatDuration)
            {
                SwitchState(State.FocusOnTarget);
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Patrol()
    {
        while (currentState == State.Patrol)
        {
            // Set destination to current patrol point
            if (!hasPatrolPoint)
            {
                FindRandomPatrolPoint();
            }

            if (hasPatrolPoint)
            {
                myAgent.SetDestination(currentPatrolPoint);

                // Check if we've reached the patrol point
                if (!myAgent.pathPending && myAgent.remainingDistance <= myAgent.stoppingDistance)
                {
                    hasPatrolPoint = false; // Need to find a new patrol point
                    SwitchState(State.Idle);
                    yield break;
                }
            }

            yield return null;
        }
    }

    IEnumerator Knockback()
    {
        if (animator != null)
        {
            if (enemyBehaviour.currentState == EnemyBehaviour.EnemyState.Hitstun && enemyBehaviour.IsGrounded)
            {
                animator.SetTrigger("Hit");
            }
            else
            {
                animator.SetTrigger("Knockback");
            }
        }
        
        // Wait for linear velocity to be 0 and enemy to be grounded before switching back to idle
        while (currentState == State.Knockback)
        {
            // Wait a brief moment to allow physics to apply knockback force
            yield return new WaitForSeconds(0.1f);
            if (rb.linearVelocity.magnitude < 0.1f && enemyBehaviour.IsGrounded && enemyBehaviour.health > 0)
            {
                // Re-enable NavMesh agent and rigidbody
                myAgent.enabled = true;
                rb.isKinematic = true;
                enemyBehaviour.currentState = EnemyBehaviour.EnemyState.Normal; // Reset enemy state to normal
                if (targetTransform != null)
                {
                    SwitchState(State.FocusOnTarget);
                }
                else
                {
                    SwitchState(State.Idle);
                }
                yield break;
            }

            yield return null;
        }
    }

    private void FindRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += startPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            currentPatrolPoint = hit.position;
            hasPatrolPoint = true;
        }
        else
        {
            hasPatrolPoint = false; // Failed to find a valid patrol point
        }
    }

    void FaceTarget()
    {
        if (targetTransform != null)
        {
            directionToTarget = targetTransform.position - transform.position;
            directionToTarget.y = 0; // Keep only horizontal direction
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void OnPlayerDetected(Transform player)
    {
        targetTransform = player;
        if (currentState == State.Idle || currentState == State.Patrol)
        {
            SwitchState(State.FocusOnTarget);
        }
    }

    public void OnPlayerLost()
    {
        targetTransform = null;
        if (currentState == State.FocusOnTarget || currentState == State.ChaseTarget || currentState == State.Attack || currentState == State.Retreat)
        {
            SwitchState(State.Idle);
        }
    }

    public void OnCounterTriggered()
    {
        SwitchState(Chaser.State.Attack);
    }

    public void EnterKnockbackState()
    {
        SwitchState(Chaser.State.Knockback);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(centerPoint.position + transform.forward * playerProximityThreshold, attackBoxSize);
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            bool isMoving = currentState == State.Patrol || 
                            currentState == State.ChaseTarget;

            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsRetreating", currentState == State.Retreat);
            animator.SetBool("IsStrafing", currentState == State.FocusOnTarget);
            animator.SetFloat("Speed", myAgent.velocity.magnitude);
            animator.SetBool("IsKnockback", currentState == State.Knockback);
            animator.SetBool("IsGrounded", enemyBehaviour != null ? enemyBehaviour.IsGrounded : true);
        }
    }

    // Called by animation event
    public void OnAttackWindupEnd()
    {
        hitbox.ActivateHitbox();
    }

    // Called by animation event
    public void OnAttackActiveEnd()
    {
        hitbox.DeactivateHitbox();
    }

    // Called by animation event at end of attack animation
    public void OnAttackComplete()
    {
        SwitchState(State.Retreat);
    }
}
