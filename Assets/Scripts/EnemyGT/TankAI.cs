using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TankAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Detection")]
    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float losePlayerRange = 14f;

    [Header("Idle")]
    [SerializeField] private float idleDuration = 2f;

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 5f;
    [SerializeField] private float patrolSampleRadius = 2f;
    [SerializeField] private int maxPatrolAttempts = 20;

    [Header("Heavy Attack")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackWindupTime = 0.7f;
    [SerializeField] private float attackActiveTime = 0.35f;
    [SerializeField] private float attackRecoveryTime = 0.9f;

    [Header("Attack Box")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackBoxSize = new Vector3(2f, 1.5f, 2f);
    [SerializeField] private Vector3 attackBoxOffset = new Vector3(0f, 0.8f, 1.3f);

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;

    private Vector3 spawnPosition;
    private Vector3 patrolPoint;
    private bool hasPatrolPoint;
    private float lastAttackTime = -999f;

    private readonly HashSet<Transform> damagedTargets = new HashSet<Transform>();

    private enum TankState
    {
        Idle,
        Patrol,
        Chase,
        HeavyAttack
    }

    [SerializeField] private TankState currentState = TankState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
                case TankState.Idle:
                    yield return StartCoroutine(IdleState());
                    break;

                case TankState.Patrol:
                    yield return StartCoroutine(PatrolState());
                    break;

                case TankState.Chase:
                    yield return StartCoroutine(ChaseState());
                    break;

                case TankState.HeavyAttack:
                    yield return StartCoroutine(HeavyAttackState());
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator IdleState()
    {
        StopMoving();

        float idleTimer = 0f;

        while (currentState == TankState.Idle)
        {
            FindPlayer();

            if (player != null)
            {
                currentState = TankState.Chase;
                yield break;
            }

            idleTimer += Time.deltaTime;

            if (idleTimer >= idleDuration)
            {
                currentState = TankState.Patrol;
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

        while (currentState == TankState.Patrol)
        {
            FindPlayer();

            if (player != null)
            {
                hasPatrolPoint = false;
                currentState = TankState.Chase;
                yield break;
            }

            if (hasPatrolPoint && AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(patrolPoint);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasPatrolPoint = false;
                    currentState = TankState.Idle;
                    yield break;
                }
            }
            else
            {
                currentState = TankState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ChaseState()
    {
        while (currentState == TankState.Chase)
        {
            if (player == null)
            {
                currentState = TankState.Idle;
                yield break;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > losePlayerRange)
            {
                player = null;
                currentState = TankState.Idle;
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
                    currentState = TankState.HeavyAttack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator HeavyAttackState()
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

        currentState = TankState.Chase;
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