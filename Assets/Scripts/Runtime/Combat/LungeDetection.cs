using UnityEngine;

public class LungeDetection : MonoBehaviour
{
    public GameObject currentAttackTarget;
    public bool hasAppliedAirBoost = false;
    public float timer;
    public float duration = 0.5f;
    private Attack attack;
    private ThirdPersonController playerController;
    private Rigidbody rb;
    public Collider sphere;
    
    void Start()
    {
        attack = GetComponentInParent<Attack>();
        playerController = GetComponentInParent<ThirdPersonController>();
        rb = GetComponentInParent<Rigidbody>();
        sphere = GetComponent<Collider>();
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                playerController.StopAttacking();
                sphere.enabled = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (currentAttackTarget != null && other.gameObject == currentAttackTarget && other.CompareTag("Enemy") && playerController.isAttacking)
        {
            playerController.StopAttacking();
            attack.directionToEnemy = Vector3.zero; // Reset direction after successfully hitting an enemy
            if (!playerController.IsGrounded && !hasAppliedAirBoost)
            {
                if (attack.currentAttackType == Attack.AttackType.Charged)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                    rb.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                    hasAppliedAirBoost = true;
                }
                else if (attack.currentAttackType == Attack.AttackType.Normal ||
                        attack.currentAttackType == Attack.AttackType.Finisher)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(playerController.slowFallSpeed, rb.linearVelocity.y), rb.linearVelocity.z);
                    hasAppliedAirBoost = true;
                }
            }
            currentAttackTarget = null;
            sphere.enabled = false;
        }
    }
}