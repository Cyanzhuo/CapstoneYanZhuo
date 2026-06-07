using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Chaser : MonoBehaviour
{
    NavMeshAgent myAgent;
    Rigidbody rb;
    EnemyBehaviour enemyBehaviour;

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

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 5f;  // How far from start position to roam
    [SerializeField] private float patrolRadius = 5f;  // Search radius for NavMesh sampling
    private Vector3 startPosition;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    int currentPatrolIndex = 0;
    public enum State
    {
        Idle, Patrol, FocusOnTarget, ChaseTarget, Attack, Retreat, Knockback
    }
    public State currentState;

    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviour = GetComponent<EnemyBehaviour>();
    }

    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(SwitchState(State.Idle));
        rb.isKinematic = true; // Start with kinematic rigidbody for NavMeshAgent control
    }

    void Update()
    {
        if (currentState != State.FocusOnTarget && orbitAngle != 0f)
        {
            orbitAngle = 0f; // Reset orbit angle when not in FocusOnTarget state
        }
    }

    public IEnumerator SwitchState(State newState)
    {
        if (currentState == newState)
        {
            yield break; // Exit if the state is already the same
        }

        currentState = newState;

        StartCoroutine(newState switch
        {
            State.Idle => Idle(),
            State.FocusOnTarget => FocusOnTarget(),
            State.ChaseTarget => ChaseTarget(),
            State.Attack => Attack(),
            State.Retreat => Retreat(),
            State.Patrol => Patrol(),
            State.Knockback => Knockback(),
            _ => null
        });
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
                StartCoroutine(SwitchState(State.Patrol));
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
            }

            // Face the target
            Vector3 directionToTarget = targetTransform.position - transform.position;
            directionToTarget.y = 0; // Keep only horizontal direction
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

            // Move around the target in a circle
            orbitAngle += Time.deltaTime * circleDirection;
            Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * Mathf.Max(directionToTarget.magnitude, minRadius);
            myAgent.SetDestination(targetTransform.position + offset);

            if (notAttackingTimer >= focusDuration)
            {
                StartCoroutine(SwitchState(State.ChaseTarget));
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
                    StartCoroutine(SwitchState(State.Attack));
                }
            }
            
            yield return null;
        }
    }

    IEnumerator Attack()
    {
        // Wait for attack animation to finish before switching to retreat
        yield return new WaitForSeconds(1f); // Placeholder for attack duration
        StartCoroutine(SwitchState(State.Retreat));
        yield break;
    }
    
    IEnumerator Retreat()
    {
        // Move backwards for a short duration, then switch back to focus on target
        float retreatTimer = 0f;
        while (currentState == State.Retreat)
        {
            // Move backwards away from the target
            Vector3 retreatDirection = (transform.position - targetTransform.position).normalized;
            myAgent.Move(retreatDirection * myAgent.speed * Time.deltaTime);

            retreatTimer += Time.deltaTime;
            if (retreatTimer >= retreatDuration)
            {
                StartCoroutine(SwitchState(State.FocusOnTarget));
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
                    StartCoroutine(SwitchState(State.Idle));
                    yield break;
                }
            }

            yield return null;
        }
    }

    IEnumerator Knockback()
    {
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
                StartCoroutine(SwitchState(State.Idle));
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

    public void OnPlayerDetected(Transform player)
    {
        targetTransform = player;
        if (currentState == State.Idle || currentState == State.Patrol)
        {
            StartCoroutine(SwitchState(State.FocusOnTarget));
        }
    }

    public void OnPlayerLost()
    {
        targetTransform = null;
        if (currentState == State.FocusOnTarget || currentState == State.ChaseTarget || currentState == State.Attack || currentState == State.Retreat)
        {
            StartCoroutine(SwitchState(State.Idle));
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(centerPoint.position + transform.forward * playerProximityThreshold, attackBoxSize);
    }
}