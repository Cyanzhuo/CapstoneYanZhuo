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
    [SerializeField] float searchRadius = 5f;
    [SerializeField] Vector3 attackBoxSize = new Vector3(0.6f, 1.2f, 0.6f);
    [SerializeField] Transform[] patrolPoints; // Array of patrol points (using Transforms for easy scene placement)
    [SerializeField] float idleDuration = 3f; // Time to stay in Idle state before patrolling
    [SerializeField] float focusDuration = 5f; // Time to stay in FocusOnTarget state
    [SerializeField] float retreatDuration = 2f; // Time to stay in Retreat state
    [SerializeField] float directionChangeInterval = 3f; // Time interval to change circling direction in FocusOnTarget state
    [SerializeField] float rotationSpeed = 60f;

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
        StartCoroutine(SwitchState(State.Idle));
        rb.isKinematic = true; // Start with kinematic rigidbody for NavMeshAgent control
    }

    void Update()
    {
        Collider[] player = Physics.OverlapSphere(centerPoint.position, searchRadius, playerLayer);
        if (player.Length > 0)
        {
            targetTransform = player[0].transform;
        }
         else
        {
            targetTransform = null;
        }
        Vector3 boxCenter = centerPoint.position + transform.forward * playerProximityThreshold;
        Collider[] hits = Physics.OverlapBox(boxCenter, attackBoxSize * 0.5f, transform.rotation, playerLayer);
        if (hits.Length > 0 && currentState == State.ChaseTarget)
        {
            StartCoroutine(SwitchState(State.Attack));
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
            // Perform idle behavior here
            if (targetTransform != null)
            {
                // If there is a target, go to the chasing state
                StartCoroutine(SwitchState(State.FocusOnTarget));
                yield break;
            }

            idleTimer += Time.deltaTime;

            // After idleDuration seconds, switch to Patrol state
            if (idleTimer >= idleDuration && patrolPoints.Length > 0)
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
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState(State.Idle));
                yield break;
            }

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
            Vector3 perpendicular = Vector3.Cross(Vector3.up, directionToTarget.normalized);
            Vector3 offset = perpendicular * circleDirection * directionToTarget.magnitude;
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
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState(State.Idle));
                yield break;
            }
            else
            {
                myAgent.SetDestination(targetTransform.position);
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
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState(State.Idle));
                yield break;
            }

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
        // Make sure we have patrol points
        if (patrolPoints.Length == 0)
        {
            StartCoroutine(SwitchState(State.Idle));
            yield break;
        }

        // Set destination to current patrol point
        myAgent.SetDestination(patrolPoints[currentPatrolIndex].position);

        while (currentState == State.Patrol)
        {
            // Check if we've reached the patrol point
            if (!myAgent.pathPending && myAgent.remainingDistance <= myAgent.stoppingDistance)
            {
                // Move to next patrol point (with wrap-around)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                StartCoroutine(SwitchState(State.Idle));
                yield break;
            }

            // Check if we see the player
            if (targetTransform != null)
            {
                StartCoroutine(SwitchState(State.FocusOnTarget));
                yield break;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centerPoint.position, searchRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(centerPoint.position + transform.forward * playerProximityThreshold, attackBoxSize);
    }
}