using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;
using UnityEngine.AI;

public class KingAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 300;
    [SerializeField] private int currentHealth;
    [SerializeField] private float phaseTwoHealthPercent = 0.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 16f;
    [SerializeField] private float losePlayerRange = 25f;

    [Header("Idle")]
    [SerializeField] private float idleDuration = 1.5f;

    [Header("Random Patrol")]
    [SerializeField] private float patrolRange = 8f;
    [SerializeField] private float patrolSampleRadius = 2f;
    [SerializeField] private int maxPatrolAttempts = 20;

    [Header("Melee Attack")]
    [SerializeField] private int meleeDamage = 25;
    [SerializeField] private float meleeRange = 2.2f;
    [SerializeField] private float meleeCooldown = 2f;
    [SerializeField] private float meleeWindupTime = 0.6f;
    [SerializeField] private float meleeActiveTime = 0.35f;
    [SerializeField] private float meleeRecoveryTime = 0.8f;

    [Header("Melee Attack Box")]
    [SerializeField] private Transform meleeAttackPoint;
    [SerializeField] private Vector3 meleeBoxSize = new Vector3(2.4f, 1.6f, 2.2f);
    [SerializeField] private Vector3 meleeBoxOffset = new Vector3(0f, 0.8f, 1.4f);

    [Header("Reposition")]
    [SerializeField] private float repositionDuration = 0.8f;
    [SerializeField] private float repositionSpeedMultiplier = 1.1f;

    [Header("Phase Two")]
    [SerializeField] private float phaseTwoSpeedMultiplier = 1.25f;
    [SerializeField] private float phaseTwoCooldownMultiplier = 0.75f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    private Vector3 spawnPosition;
    private Vector3 patrolPoint;
    private bool hasPatrolPoint;

    private float originalAgentSpeed;
    private float lastMeleeTime = -999f;
    private bool phaseTwoActive = false;

    private readonly HashSet<Transform> damagedTargets = new HashSet<Transform>();

    private enum KingState
    {
        Idle,
        Patrol,
        Chase,
        MeleeAttack,
        Reposition,
        Dead
    }

    [SerializeField] private KingState currentState = KingState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        spawnPosition = transform.position;
        currentHealth = maxHealth;

        if (agent != null)
        {
            originalAgentSpeed = agent.speed;
        }

        StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (currentState != KingState.Dead)
        {
            switch (currentState)
            {
                case KingState.Idle:
                    yield return StartCoroutine(IdleState());
                    break;

                case KingState.Patrol:
                    yield return StartCoroutine(PatrolState());
                    break;

                case KingState.Chase:
                    yield return StartCoroutine(ChaseState());
                    break;

                case KingState.MeleeAttack:
                    yield return StartCoroutine(MeleeAttackState());
                    break;

                case KingState.Reposition:
                    yield return StartCoroutine(RepositionState());
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator IdleState()
    {
        StopMoving();

        float idleTimer = 0f;

        while (currentState == KingState.Idle)
        {
            FindPlayer();

            if (player != null)
            {
                currentState = KingState.Chase;
                yield break;
            }

            idleTimer += Time.deltaTime;

            if (idleTimer >= idleDuration)
            {
                currentState = KingState.Patrol;
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

        while (currentState == KingState.Patrol)
        {
            FindPlayer();

            if (player != null)
            {
                hasPatrolPoint = false;
                currentState = KingState.Chase;
                yield break;
            }

            if (hasPatrolPoint && AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(patrolPoint);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasPatrolPoint = false;
                    currentState = KingState.Idle;
                    yield break;
                }
            }
            else
            {
                currentState = KingState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ChaseState()
    {
        while (currentState == KingState.Chase)
        {
            if (player == null)
            {
                currentState = KingState.Idle;
                yield break;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > losePlayerRange)
            {
                player = null;
                currentState = KingState.Idle;
                yield break;
            }

            FacePlayer();

            if (distanceToPlayer > meleeRange)
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

                if (CanMeleeAttack())
                {
                    currentState = KingState.MeleeAttack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator MeleeAttackState()
    {
        lastMeleeTime = Time.time;
        damagedTargets.Clear();

        StopMoving();
        FacePlayer();

        InterimAudioDirector.TryPlayMove(InterimAudioCue.ChargedAttack, transform.position);
        yield return new WaitForSeconds(meleeWindupTime);

        float activeTimer = 0f;

        while (activeTimer < meleeActiveTime)
        {
            DamagePlayersInMeleeBox();

            activeTimer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(meleeRecoveryTime);

        currentState = KingState.Reposition;
    }

    private IEnumerator RepositionState()
    {
        float timer = 0f;

        while (currentState == KingState.Reposition)
        {
            if (player == null)
            {
                currentState = KingState.Idle;
                yield break;
            }

            FacePlayer();

            Vector3 awayFromPlayer = transform.position - player.position;
            awayFromPlayer.y = 0f;

            if (awayFromPlayer.sqrMagnitude <= 0.01f)
            {
                awayFromPlayer = -transform.forward;
            }

            awayFromPlayer.Normalize();

            if (AgentReady())
            {
                agent.isStopped = false;
                agent.Move(awayFromPlayer * agent.speed * repositionSpeedMultiplier * Time.deltaTime);
            }

            timer += Time.deltaTime;

            if (timer >= repositionDuration)
            {
                currentState = KingState.Chase;
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

    private bool CanMeleeAttack()
    {
        float cooldown = phaseTwoActive ? meleeCooldown * phaseTwoCooldownMultiplier : meleeCooldown;
        return Time.time >= lastMeleeTime + cooldown;
    }

    private void DamagePlayersInMeleeBox()
    {
        Vector3 center = GetMeleeBoxCenter();

        Collider[] hits = Physics.OverlapBox(
            center,
            meleeBoxSize * 0.5f,
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
            InterimAudioDirector.TryPlayMove(InterimAudioCue.ChargedAttackHit, hit.transform.position);

            hit.SendMessageUpwards(
                "TakeDamage",
                meleeDamage,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private Vector3 GetMeleeBoxCenter()
    {
        if (meleeAttackPoint != null)
        {
            return meleeAttackPoint.position;
        }

        return transform.position +
               transform.right * meleeBoxOffset.x +
               transform.up * meleeBoxOffset.y +
               transform.forward * meleeBoxOffset.z;
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

    public void TakeDamage(int amount)
    {
        if (currentState == KingState.Dead)
        {
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (!phaseTwoActive && currentHealth <= maxHealth * phaseTwoHealthPercent)
        {
            ActivatePhaseTwo();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ActivatePhaseTwo()
    {
        phaseTwoActive = true;

        if (agent != null)
        {
            agent.speed = originalAgentSpeed * phaseTwoSpeedMultiplier;
        }

        Debug.Log($"{gameObject.name} entered Phase Two.");
    }

    private void Die()
    {
        currentState = KingState.Dead;
        StopMoving();

        Debug.Log($"{gameObject.name} defeated.");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRange);

        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.TRS(GetMeleeBoxCenter(), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, meleeBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
