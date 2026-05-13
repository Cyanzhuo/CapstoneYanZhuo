using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Chaser : MonoBehaviour
{
    NavMeshAgent myAgent;
    Rigidbody rb;
    EnemyBehaviour enemyBehaviour;

    [SerializeField] Transform targetTransform;
    [SerializeField] Transform[] patrolPoints; // Array of patrol points (using Transforms for easy scene placement)
    [SerializeField] float idleDuration = 3f; // Time to stay in Idle state before patrolling
    [SerializeField] float focusDuration = 5f; // Time to stay in FocusOnTarget state
    [SerializeField] float retreatDuration = 2f; // Time to stay in Retreat state

    int currentPatrolIndex = 0;
    public string currentState;

    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviour = GetComponent<EnemyBehaviour>();
    }

    void Start()
    {
        StartCoroutine(SwitchState("Idle"));
        rb.isKinematic = true; // Start with kinematic rigidbody for NavMeshAgent control
    }

    public IEnumerator SwitchState(string newState)
    {
        if (currentState == newState)
        {
            yield break; // Exit if the state is already the same
        }

        currentState = newState;

        StartCoroutine(currentState);
    }

    IEnumerator Idle()
    {
        float idleTimer = 0f;
        
        while (currentState == "Idle")
        {
            // Perform idle behavior here
            if (targetTransform != null)
            {
                // If there is a target, go to the chasing state
                StartCoroutine(SwitchState("FocusOnTarget"));
            }

            idleTimer += Time.deltaTime;

            // After idleDuration seconds, switch to Patrol state
            if (idleTimer >= idleDuration && patrolPoints.Length > 0)
            {
                StartCoroutine(SwitchState("Patrol"));
                yield break;
            }

            yield return null; // Wait for the next frame
        }
    }

    IEnumerator FocusOnTarget()
    {
        float notAttackingTimer = 0f;
        notAttackingTimer += Time.deltaTime;
        // make the enemy turn to face the player and pace slowly around the player
        while (currentState == "FocusOnTarget")
        {
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            // Face the target
            Vector3 directionToTarget = targetTransform.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            // Move around the target in a circle while randomly switching between circling clockwise and counterclockwise every 3 seconds
            Vector3 offset = Quaternion.Euler(0, 90 * Mathf.Sign(Mathf.Sin(Time.time)), 0) * directionToTarget.normalized * directionToTarget.magnitude; // Calculate offset for circling
            myAgent.SetDestination(targetTransform.position + offset);
        }
        if (notAttackingTimer >= focusDuration)
        {
            StartCoroutine(SwitchState("ChaseTarget"));
            yield break;
        }
        yield return null;
    }

    IEnumerator ChaseTarget()
    {
        // while loop in a coroutine = mini Update function
        while (currentState == "ChaseTarget")
        {
            // Perform chasing behavior here
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState("Idle"));
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
        StartCoroutine(SwitchState("Retreat"));
    }
    
    IEnumerator Retreat()
    {
        // Move backwards for a short duration, then switch back to focus on target
        float retreatTimer = 0f;
        while (currentState == "Retreat")
        {
            if (targetTransform == null)
            {
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            // Move backwards away from the target
            Vector3 retreatDirection = (transform.position - targetTransform.position).normalized;
            myAgent.Move(retreatDirection * myAgent.speed * Time.deltaTime);

            retreatTimer += Time.deltaTime;
            if (retreatTimer >= retreatDuration)
            {
                StartCoroutine(SwitchState("FocusOnTarget"));
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
            StartCoroutine(SwitchState("Idle"));
            yield break;
        }

        // Set destination to current patrol point
        myAgent.SetDestination(patrolPoints[currentPatrolIndex].position);

        while (currentState == "Patrol")
        {
            // Check if we've reached the patrol point
            if (!myAgent.pathPending && myAgent.remainingDistance <= myAgent.stoppingDistance)
            {
                // Move to next patrol point (with wrap-around)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            // Check if we see the player
            if (targetTransform != null)
            {
                StartCoroutine(SwitchState("FocusOnTarget"));
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Knockback()
    {
        // Wait for linear velocity to be 0 and enemy to be grounded before switching back to idle
        while (currentState == "Knockback")
        {
            if (rb.linearVelocity.magnitude < 0.1f && enemyBehaviour.IsGrounded)
            {
                // Re-enable NavMesh agent and rigidbody
                myAgent.enabled = true;
                rb.isKinematic = true;
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            yield return null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // If the chaser 'sees' the player, set the target to the player
        if (other.gameObject.CompareTag("Player"))
            targetTransform = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        // If the player leaves the chaser's trigger, set the target to null
        if (other.gameObject.CompareTag("Player"))
            targetTransform = null;
    }
}
