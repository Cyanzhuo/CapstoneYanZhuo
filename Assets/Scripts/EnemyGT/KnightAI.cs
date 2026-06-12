using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KnightAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float losePlayerRange = 12f;

    [Header("Idle")]
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 6f;
    [SerializeField] private float patrolSampleRadius = 2f;
    [SerializeField] private int maxPatrolAttempts = 20;

    [Header("Attack")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackWindupTime = 0.25f;
    [SerializeField] private float attackActiveTime = 0.25f;
    [SerializeField] private float attackRecoveryTime = 0.45f;

    [Header("Retreat")]
    [SerializeField] private float retreatDuration = 0.7f;
    [SerializeField] private float retreatSpeedMultiplier = 1.2f;

    [Header("Attack Box")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackBoxSize = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 attackBoxOffset = new Vector3(0f, 0.8f, 1f);

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    private Vector3 spawnPosition;
    private Vector3 patrolPoint;
    private bool hasPatrolPoint;
    private float lastAttackTime = -999f;

    private readonly HashSet<Transform> damagedTargets = new HashSet<Transform>();

    private enum KnightState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Retreat
    }

    [SerializeField] private KnightState currentState = KnightState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        spawnPosition = transform.position;

        // This fixes the "idle timer not counting on startup" issue.
        // The state machine starts immediately and enters IdleState properly.
        StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case KnightState.Idle:
                    yield return StartCoroutine(IdleState());
                    break;

                case KnightState.Patrol:
                    yield return StartCoroutine(PatrolState());
                    break;

                case KnightState.Chase:
                    yield return StartCoroutine(ChaseState());
                    break;

                case KnightState.Attack:
                    yield return StartCoroutine(AttackState());
                    break;

                case KnightState.Retreat:
                    yield return StartCoroutine(RetreatState());
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator IdleState()
    {
        StopMoving();

        float idleTimer = 0f;

        while (currentState == KnightState.Idle)
        {
            FindPlayer();

            if (player != null)
            {
                currentState = KnightState.Chase;
                yield break;
            }

            idleTimer += Time.deltaTime;

            if (idleTimer >= idleDuration)
            {
                currentState = KnightState.Patrol;
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

        while (currentState == KnightState.Patrol)
        {
            FindPlayer();

            if (player != null)
            {
                hasPatrolPoint = false;
                currentState = KnightState.Chase;
                yield break;
            }

            if (hasPatrolPoint && AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(patrolPoint);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasPatrolPoint = false;
                    currentState = KnightState.Idle;
                    yield break;
                }
            }
            else
            {
                currentState = KnightState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ChaseState()
    {
        while (currentState == KnightState.Chase)
        {
            if (player == null)
            {
                currentState = KnightState.Idle;
                yield break;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > losePlayerRange)
            {
                player = null;
                currentState = KnightState.Idle;
                yield break;
            }

            FacePlayer();

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
                    currentState = KnightState.Attack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator AttackState()
    {
        lastAttackTime = Time.time;
        damagedTargets.Clear();

        StopMoving();
        FacePlayer();

        yield return new WaitForSeconds(attackWindupTime);

        float activeTimer = 0f;

        while (activeTimer < attackActiveTime)
        {
            DamagePlayersInAttackBox();

            activeTimer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(attackRecoveryTime);

        currentState = KnightState.Retreat;
    }

    private IEnumerator RetreatState()
    {
        float retreatTimer = 0f;

        while (currentState == KnightState.Retreat)
        {
            if (player == null)
            {
                currentState = KnightState.Idle;
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
                currentState = KnightState.Chase;
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

    private void DamagePlayersInAttackBox()
    {
        Vector3 center = GetAttackBoxCenter();

        Collider[] hits = Physics.OverlapBox(
            center,
            attackBoxSize * 0.5f,
            transform.rotation
        );

        foreach (Collider hit in hits)
        {
            Transform targetRoot = hit.transform.root;

            if (!targetRoot.CompareTag(playerTag))
            {
                continue;
            }

            if (damagedTargets.Contains(targetRoot))
            {
                continue;
            }

            damagedTargets.Add(targetRoot);

            hit.SendMessageUpwards(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private Vector3 GetAttackBoxCenter()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        return transform.position +
               transform.right * attackBoxOffset.x +
               transform.up * attackBoxOffset.y +
               transform.forward * attackBoxOffset.z;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRange);

        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.TRS(GetAttackBoxCenter(), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}