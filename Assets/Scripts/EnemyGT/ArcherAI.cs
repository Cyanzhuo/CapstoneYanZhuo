using System.Collections;
using Game.Audio;
using UnityEngine;
using UnityEngine.AI;

public class ArcherAI : MonoBehaviour
{
    private NavMeshAgent agent;
    Rigidbody rb;
    EnemyBehaviour enemyBehaviour;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float losePlayerRange = 18f;

    [Header("Idle")]
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 6f;
    [SerializeField] private float patrolSampleRadius = 2f;
    [SerializeField] private int maxPatrolAttempts = 20;

    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Vector3 firePointOffset = new Vector3(0f, 1.2f, 1f);
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float minimumDistance = 4f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackWindupTime = 0.35f;
    [SerializeField] private float attackRecoveryTime = 0.4f;

    [Header("Retreat")]
    [SerializeField] private float retreatDuration = 0.8f;
    [SerializeField] private float retreatSpeedMultiplier = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    private Vector3 spawnPosition;
    private Vector3 patrolPoint;
    private bool hasPatrolPoint;
    private float lastAttackTime = -999f;

    private enum ArcherState
    {
        Idle,
        Patrol,
        MoveToRange,
        RangedAttack,
        Retreat,
        Knockback
    }

    [SerializeField] private ArcherState currentState = ArcherState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyBehaviour = GetComponent<EnemyBehaviour>();
    }

    private void Start()
    {
        spawnPosition = transform.position;
        StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case ArcherState.Idle:
                    yield return StartCoroutine(IdleState());
                    break;

                case ArcherState.Patrol:
                    yield return StartCoroutine(PatrolState());
                    break;

                case ArcherState.MoveToRange:
                    yield return StartCoroutine(MoveToRangeState());
                    break;

                case ArcherState.RangedAttack:
                    yield return StartCoroutine(RangedAttackState());
                    break;

                case ArcherState.Retreat:
                    yield return StartCoroutine(RetreatState());
                    break;
                    
                case ArcherState.Knockback:
                    yield return StartCoroutine(Knockback());
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator IdleState()
    {
        StopMoving();

        float idleTimer = 0f;

        while (currentState == ArcherState.Idle)
        {
            FindPlayer();

            if (player != null)
            {
                currentState = ArcherState.MoveToRange;
                yield break;
            }

            idleTimer += Time.deltaTime;

            if (idleTimer >= idleDuration)
            {
                currentState = ArcherState.Patrol;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator PatrolState()
    {
        if (!hasPatrolPoint)
        {
            FindRandomPatrolPoint();
        }

        while (currentState == ArcherState.Patrol)
        {
            FindPlayer();

            if (player != null)
            {
                hasPatrolPoint = false;
                currentState = ArcherState.MoveToRange;
                yield break;
            }

            if (hasPatrolPoint && AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(patrolPoint);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasPatrolPoint = false;
                    currentState = ArcherState.Idle;
                    yield break;
                }
            }
            else
            {
                currentState = ArcherState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator MoveToRangeState()
    {
        while (currentState == ArcherState.MoveToRange)
        {
            if (player == null)
            {
                currentState = ArcherState.Idle;
                yield break;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > losePlayerRange)
            {
                player = null;
                currentState = ArcherState.Idle;
                yield break;
            }

            FacePlayer();

            if (distanceToPlayer < minimumDistance)
            {
                currentState = ArcherState.Retreat;
                yield break;
            }

            if (distanceToPlayer > attackRange)
            {
                if (AgentReady())
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                StopMoving();

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    currentState = ArcherState.RangedAttack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator RangedAttackState()
    {
        lastAttackTime = Time.time;

        StopMoving();
        FacePlayer();

        InterimAudioDirector.TryPlayMove(InterimAudioCue.BasicAttack, transform.position);
        yield return new WaitForSeconds(attackWindupTime);

        ShootProjectile();

        yield return new WaitForSeconds(attackRecoveryTime);

        if (player == null)
        {
            currentState = ArcherState.Idle;
            yield break;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < minimumDistance)
        {
            currentState = ArcherState.Retreat;
        }
        else
        {
            currentState = ArcherState.MoveToRange;
        }
    }

    private IEnumerator RetreatState()
    {
        float retreatTimer = 0f;

        while (currentState == ArcherState.Retreat)
        {
            if (player == null)
            {
                currentState = ArcherState.Idle;
                yield break;
            }

            FacePlayer();

            Vector3 retreatDirection = transform.position - player.position;
            retreatDirection.y = 0f;

            if (retreatDirection.sqrMagnitude <= 0.01f)
            {
                retreatDirection = -transform.forward;
            }

            retreatDirection.Normalize();

            if (AgentReady())
            {
                agent.isStopped = false;
                agent.Move(retreatDirection * agent.speed * retreatSpeedMultiplier * Time.deltaTime);
            }

            retreatTimer += Time.deltaTime;

            if (retreatTimer >= retreatDuration)
            {
                currentState = ArcherState.MoveToRange;
                yield break;
            }

            yield return null;
        }
    }

    private void FindPlayer()
    {
        if (player != null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                player = hit.transform;
                return;
            }

            Transform root = hit.transform.root;

            if (root.CompareTag(playerTag))
            {
                player = root;
                return;
            }
        }
    }

    private void FindRandomPatrolPoint()
    {
        hasPatrolPoint = false;

        for (int i = 0; i < maxPatrolAttempts; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
            randomDirection.y = 0f;

            Vector3 randomPosition = spawnPosition + randomDirection;

            if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, patrolSampleRadius, NavMesh.AllAreas))
            {
                patrolPoint = hit.position;
                hasPatrolPoint = true;
                return;
            }
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{gameObject.name} has no projectile prefab assigned.");
            return;
        }

        if (player == null)
        {
            return;
        }

        Vector3 spawnPosition = GetFirePosition();

        Vector3 targetPosition = player.position + Vector3.up * 1f;
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction)
        );

        InterimAudioDirector.TryPlayMove(InterimAudioCue.AerialPush, spawnPosition);

        ArcherProjectile projectile = projectileObject.GetComponent<ArcherProjectile>();

        if (projectile != null)
        {
            projectile.Launch(direction, projectileSpeed, playerTag, transform.root);
        }
        else
        {
            Debug.LogWarning("Projectile prefab does not have ArcherProjectile script.");
        }

        Debug.Log($"{gameObject.name} shot projectile at player.");
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        return transform.position +
               transform.right * firePointOffset.x +
               transform.up * firePointOffset.y +
               transform.forward * firePointOffset.z;
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private void StopMoving()
    {
        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    IEnumerator Knockback()
    {
        // Wait for linear velocity to be 0 and enemy to be grounded before switching back to idle
        while (currentState == ArcherState.Knockback)
        {
            // Wait a brief moment to allow physics to apply knockback force
            yield return new WaitForSeconds(0.1f);
            if (rb.linearVelocity.magnitude < 0.1f && enemyBehaviour.IsGrounded && enemyBehaviour.health > 0)
            {
                // Re-enable NavMesh agent and rigidbody
                agent.enabled = true;
                rb.isKinematic = true;
                currentState = ArcherState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minimumDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRange);
    }
}
