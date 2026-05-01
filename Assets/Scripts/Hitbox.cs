using UnityEngine;
using System.Collections.Generic;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private Attack attack; // Reference to the main brain
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private Collider weaponCollider;
    
    // Track enemies hit during this attack
    private HashSet<EnemyBehaviour> hitEnemies = new HashSet<EnemyBehaviour>();
    private bool hasBounced; // To prevent multiple bounces on spike attack

    void Start()
    {
        DeactivateHitbox();
    }
    public void ActivateHitbox()
    {
        weaponCollider.enabled = true;
        hitEnemies.Clear();
        hasBounced = false;
    }
    public void DeactivateHitbox()
    {
        weaponCollider.enabled = false;
    }
    private void ResetDashes()
    {
        if (playerController.dashUpgradeObtained)
        {
            playerController.availableDashes = 2;
        }
        else
        {
            playerController.availableDashes = 1;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out EnemyBehaviour enemy))
            {
                // Check if we've already hit this enemy during this attack
                if (hitEnemies.Contains(enemy))
                    return; // Already hit, skip
                
                // Add to hit list first (prevents re-entrancy issues)
                hitEnemies.Add(enemy);

                // Calculate knockback direction from player to enemy
                Vector3 knockbackDir = (other.transform.position - attack.transform.position).normalized;
                knockbackDir.y = 0; // Keep it on a flat plane

                // Use the data from the Weapon script through the Attack reference
                var weaponData = attack.currentWeapon;
                Rigidbody playerRB = playerController.GetComponent<Rigidbody>();

                switch (attack.currentAttackType)
                {
                    case Attack.AttackType.Finisher:
                        enemy.TakeDamage(weaponData.finisherDamage);
                        enemy.Knockback(knockbackDir, attack.defaultForce + playerController.targetVelocity.magnitude * 0.8f, true);
                        
                        if (!playerController.IsGrounded)
                        {
                            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, Mathf.Max(0, playerRB.linearVelocity.y), playerRB.linearVelocity.z);
                        }

                        enemy.currentState = EnemyBehaviour.EnemyState.Knockback; // Set state to Knockback to trigger extra damage on wall collision
                        ResetDashes();
                        playerController.StopAttacking();

                        break;
                        
                    case Attack.AttackType.Charged:
                        // Use charge level for damage
                        int damage;
                        if (attack.chargeLevel >= 2)
                        {
                            damage = weaponData.chargeDamage;
                        }
                        else
                        {
                            damage = weaponData.finisherDamage;
                        }
                        
                        enemy.TakeDamage(damage);

                        // Calculate horizontal knockback with slide speed
                        float horizontalForce = attack.defaultForce + playerController.targetVelocity.magnitude * 0.8f;
                        Vector3 horizontalKnockback = knockbackDir * horizontalForce;
                        
                        // Calculate vertical knockback
                        Vector3 verticalKnockback = Vector3.up * playerController.doubleJumpForce;
                        
                        // Combine into final knockback vector
                        Vector3 totalKnockback = horizontalKnockback + verticalKnockback;
                        
                        // Pass the direction (normalized) and force (magnitude) separately
                        enemy.Knockback(totalKnockback.normalized, totalKnockback.magnitude, false);
                        
                        if (!playerController.IsGrounded && !hasBounced)
                        {
                            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
                            playerRB.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                            hasBounced = true;
                        }

                        enemy.currentState = EnemyBehaviour.EnemyState.Knockback; // Set state to Knockback to trigger extra damage on wall collision
                        ResetDashes();
                        playerController.StopAttacking();

                        break;
                        
                    case Attack.AttackType.Launcher:
                        if (attack.isCharging)
                        {
                            // Use charge level for damage
                            int varDamage;
                            if (attack.chargeLevel >= 2)
                            {
                                varDamage = weaponData.chargeDamage;
                            }
                            else
                            {
                                varDamage = weaponData.finisherDamage;
                            }
                            
                            enemy.TakeDamage(varDamage);
                        }
                        else
                        {
                            enemy.TakeDamage(weaponData.normalDamage);
                        }
                        
                        // Calculate horizontal knockback with slide speed
                        float launcherHorizontalForce = playerController.targetVelocity.magnitude * 0.8f;
                        Vector3 launcherHorizontalKnockback = knockbackDir * launcherHorizontalForce;
                        
                        // Calculate vertical knockback
                        Vector3 launcherVerticalKnockback = Vector3.up * attack.appliedJuggleForce;
                        
                        // Combine into final knockback vector
                        Vector3 launcherTotalKnockback = launcherHorizontalKnockback + launcherVerticalKnockback;
                        enemy.Knockback(launcherTotalKnockback.normalized, launcherTotalKnockback.magnitude, false);

                        ResetDashes();
                        playerController.StopAttacking();

                        break;
                        
                    case Attack.AttackType.GroundSlam:
                        enemy.TakeDamage(weaponData.normalDamage);
                        enemy.Knockback(Vector3.down, 10f, false);
                        break;
                        
                    case Attack.AttackType.DashSlam:
                        enemy.TakeDamage(weaponData.normalDamage);
                        Vector3 dsHorizontalKnockback = knockbackDir * (attack.bounceForce + Mathf.Max(0, playerController.targetVelocity.magnitude - playerController.moveSpeed) * 0.8f);
                        Vector3 dsVerticalKnockback = Vector3.down * 10f;
                        Vector3 dsTotalKnockback = dsHorizontalKnockback + dsVerticalKnockback;
                        enemy.Knockback(dsTotalKnockback.normalized, dsTotalKnockback.magnitude, false);
                        break;
                    
                    case Attack.AttackType.Spike:
                        enemy.TakeDamage(weaponData.normalDamage);
                        
                        Vector3 spikeHorizontalKnockback = knockbackDir * (1 + playerController.targetVelocity.magnitude * 0.8f);
                        
                        if (enemy.IsGrounded)
                        {
                            enemy.Knockback(spikeHorizontalKnockback.normalized, spikeHorizontalKnockback.magnitude, false);
                        }
                        else
                        {
                            Vector3 spikeVerticalKnockback = Vector3.down * 10f;
                            Vector3 spikeTotalKnockback = spikeHorizontalKnockback + spikeVerticalKnockback;
                            enemy.Knockback(spikeTotalKnockback.normalized, spikeTotalKnockback.magnitude, false);
                        }
                        
                        if (!hasBounced)
                        {
                            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
                            playerRB.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                            hasBounced = true; // Ensure we only bounce once per spike attack
                        }

                        if (!enemy.IsGrounded)
                        {
                            enemy.currentState = EnemyBehaviour.EnemyState.Spiked;
                        }
                        ResetDashes();
                        playerController.availableJumps = 1; // Reset jumps
                        playerController.availableAerialPushes = 1; // Reset aerial pushes
                        playerController.pauseFastFall = false;
                        playerController.StopAttacking();

                        break;

                    case Attack.AttackType.BoundSpike:
                        int BSdamage;
                        if (attack.chargeLevel >= 2)
                        {
                            BSdamage = weaponData.chargeDamage;
                        }
                        else
                        {
                            BSdamage = weaponData.finisherDamage;
                        }
                        enemy.TakeDamage(BSdamage);

                        Vector3 bsHorizontalKnockback = knockbackDir * (1 + playerController.targetVelocity.magnitude * 0.8f);
                        if (enemy.IsGrounded)
                        {
                            Vector3 bsVerticalKnockback = Vector3.up * (attack.launcherForce - 1);
                            Vector3 bsTotalKnockback = bsHorizontalKnockback + bsVerticalKnockback;
                            enemy.Knockback(bsTotalKnockback.normalized, bsTotalKnockback.magnitude, false);
                        }
                        else
                        {
                            Vector3 bsVerticalKnockback = Vector3.down * 10f;
                            Vector3 bsTotalKnockback = bsHorizontalKnockback + bsVerticalKnockback;
                            enemy.Knockback(bsTotalKnockback.normalized, bsTotalKnockback.magnitude, false);
                            enemy.currentState = EnemyBehaviour.EnemyState.Rebound; // Set state to Rebound to trigger launcher effect on landing
                        }

                        if (!hasBounced)
                        {
                            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
                            playerRB.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                            hasBounced = true;
                        }

                        ResetDashes();
                        playerController.availableJumps = 1; // Reset jumps
                        playerController.availableAerialPushes = 1; // Reset aerial pushes
                        break;

                    case Attack.AttackType.AerialPush:
                        enemy.TakeDamage(weaponData.normalDamage);
                        
                        Vector3 apHorizontalKnockback = knockbackDir * (attack.defaultForce + Mathf.Max(0, playerController.targetVelocity.magnitude - playerController.moveSpeed)) * 0.8f;
                        Vector3 apVerticalKnockback = Vector3.up * playerController.shortJumpForce;
                        Vector3 apKnockback = apHorizontalKnockback + apVerticalKnockback;
                        enemy.Knockback(apKnockback.normalized, apKnockback.magnitude, false);

                        ResetDashes();

                        break;

                    case Attack.AttackType.WeakPush:
                        enemy.TakeDamage(weaponData.normalDamage);
                        float wpKnockback = Mathf.Max(1f, playerController.targetVelocity.magnitude * 0.8f);
                        enemy.Knockback(knockbackDir, wpKnockback, true);

                        ResetDashes();

                        break;

                    case Attack.AttackType.Normal:
                    default:
                        enemy.TakeDamage(weaponData.normalDamage);
                        float knockbackForce = Mathf.Max(1f, playerController.targetVelocity.magnitude * 0.8f);

                        enemy.Knockback(knockbackDir, knockbackForce, true);
                        
                        if (!playerController.IsGrounded)
                        {
                            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, Mathf.Max(0, playerRB.linearVelocity.y), playerRB.linearVelocity.z);
                        }

                        ResetDashes();
                        playerController.StopAttacking();

                        break;
                }
            }
        }
        else if (other.CompareTag("HazardWall") && !playerController.isGrabbingLedge) // If player hits a hazard wall, bounce them up and away from it
        {
            Rigidbody playerRB = playerController.GetComponent<Rigidbody>();
            if (!playerController.IsGrounded && 
                (attack.currentAttackType == Attack.AttackType.Normal ||
                attack.currentAttackType == Attack.AttackType.Finisher ||
                attack.currentAttackType == Attack.AttackType.Charged ||
                attack.currentAttackType == Attack.AttackType.Spike))
            {
                playerController.StopAttacking();

                // Store wall normal for input filtering
                playerController.lastWallNormal = (transform.position - other.ClosestPoint(transform.position)).normalized;
                playerController.lastWallNormal.y = 0; // Flatten
                
                // Set wall jump arc state
                playerController.IsInWallJumpArc = true;
                playerController.wallJumpArcTimer = playerController.wallJumpArcDuration;
                
                playerController.IsSlideRotationFrozen = true;
                playerController.slideRotationFreezeTimer = playerController.slideRotationFreezeDuration;
                
                // Existing bounce code...
                Vector3 bounceDir = playerController.lastWallNormal;
                bounceDir.y = 1;
                
                playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
                playerRB.AddForce(bounceDir * playerController.doubleJumpForce, ForceMode.Impulse);
                
                // Horizontal momentum to slide
                Vector3 horizontalDir = new Vector3(bounceDir.x, 0, bounceDir.z).normalized;
                float bounceSpeed = Mathf.Max(playerController.slideVelocity.magnitude, playerController.doubleJumpForce);
                playerController.SetSlideVelocity(horizontalDir * bounceSpeed);
                
                DeactivateHitbox();
            }
        }
    }
}