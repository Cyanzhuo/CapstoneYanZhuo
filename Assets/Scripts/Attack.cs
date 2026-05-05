/*
* Author: Cheang Wei Cheng
* Date: 23 February 2025
* Description: This script is responsible for handling the player's attack with a bat.
* The script uses a sphere cast to detect enemies within the attack range.
* It also applies a force to the player to move them towards the enemy when attacking.
*/

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Attack : MonoBehaviour
{
    private PlayerInputActions controls;
    public Weapons currentWeapon;

    [Header("Combo Settings")]
    public float comboResetTime = 1f;
    public float finisherCooldownTime = 1f;
    [SerializeField] private float shortCooldownTime = 0.25f;

    [Header("Attack Physics")]
    public float defaultRange = 1f;
    public float dashRange = 2f;
    public float defaultForce = 5f;
    public float dashForce = 10f;
    [SerializeField] public float launcherForce = 7f;
    [SerializeField] private float mediumLauncherForce = 6f;
    [SerializeField] private float lightLauncherForce = 5f;
    public float juggleForce = 3f;
    public float bounceForce = 7.5f;
    public LayerMask enemyLayer;

    [Header("Charge Attack")]
    public float chargeThreshold = 0.5f; // How long to hold for charge level 1
    public float chargeLevel2Threshold = 1f; // How long to hold for charge level 2
    [HideInInspector] public float attackPressTime; // When the button was first pressed
    [HideInInspector] public int chargeLevel = 0; // 0 = none, 1 = level 1, 2 = level 2

    [Header("Hitbox Reference")]
    public Hitbox weaponHitbox; 

    // Internal State Flags for the Hitbox to read
    public enum AttackType
    {
        None, Normal, Charged, Finisher, Launcher, GroundSlam, DashSlam, Spike, BoundSpike, AerialPush, WeakPush
    }

    [HideInInspector] public AttackType currentAttackType;
    [HideInInspector] public bool isFinisher;
    [HideInInspector] public bool isCharging;
    
    [Header("Effects")]
    public ParticleSystem attackEffect;
    public ParticleSystem finisherEffect;
    public ParticleSystem chargeEffect;

    // Component References
    private Animator animator;
    private Rigidbody rb;
    private ThirdPersonController playerController;

    // Internal State
    private int attackStage = 0;
    private float lastAttackTime;
    private float cooldownTimer;
    private bool hasPerformedChargedAttack;
    [HideInInspector] public bool isInCooldown;
    [HideInInspector] public bool countsAsDashSlam;
    [HideInInspector] public float appliedJuggleForce;
    [HideInInspector] public bool windingUpSlam;
    private float windUpTimer;
    private float windUpDuration = 0.2f;

    // Helper property for chest-level origin
    private Vector3 AttackOrigin => transform.position + Vector3.up * 0.6f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<ThirdPersonController>();
    }
    void Awake()
    {
        controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        // Enable the input actions
        controls.Player.Enable();
        
        controls.Player.Attack.started += ctx => OnAttackStarted();
        controls.Player.Attack.canceled += ctx => OnAttackCanceled();
    }

    private void OnDisable()
    {
        // Unsubscribe and disable input actions
        controls.Player.Attack.started -= ctx => OnAttackStarted();
        controls.Player.Attack.canceled -= ctx => OnAttackCanceled();
        
        controls.Player.Disable();
    }

    void Update()
    {
        // Reset combo if player idles too long
        if (attackStage > 0 && Time.time > lastAttackTime + comboResetTime && !isInCooldown)
        {
            ResetCombo();
        }

        if (isInCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                ResetCombo();
            }
        }

        // Visual feedback for being "Charged"
        if (attackPressTime > 0 && !isInCooldown)
        {
            float holdDuration = Time.time - attackPressTime;

            if (holdDuration >= chargeLevel2Threshold && chargeLevel < 2)
            {
                PlayEffect(chargeEffect);
                Debug.Log("Charge Level 2!");
                chargeLevel = 2;
            }
            else if (holdDuration >= chargeThreshold && chargeLevel < 1)
            {
                PlayEffect(chargeEffect);
                Debug.Log("Charge Level 1!");
                chargeLevel = 1;
            }
        }

        if (controls.Player.Attack.ReadValue<float>() <= 0)
        {
            attackPressTime = 0;
        }

        if (windingUpSlam)
        {
            windUpTimer -= Time.deltaTime;
            if (windUpTimer <= 0)
            {
                GroundSlam();
            }
        }
    }

    #region Input Methods
    private void OnAttackStarted()
    {
        if ((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam) && !playerController.IsGrounded)
        {
            return;
        }
        else
        {
            if (isInCooldown && !windingUpSlam) return;
            
            attackPressTime = Time.time;
            chargeLevel = 0; // Reset charge level

            if (windingUpSlam)
            {
                Spike();
            }
            else if (!isInCooldown)
            {
                if (playerController.isCrouching && playerController.IsGrounded) // Crouching on the ground
                {
                    ProcessLauncher(launcherForce); // Perform launcher immediately
                }
                else
                {
                    ProcessCombo(); // Swing immediately
                }
            }
        }
    }

    private void OnAttackCanceled()
    {
        if ((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam) && !playerController.IsGrounded)
        {
            return;
        }

        // If we are currently in cooldown, ignore the release entirely
        if (isInCooldown && !windingUpSlam)
        {
            attackPressTime = 0; 
            return;
        }

        // If attackPressTime is 0, it means the button was pressed during a cooldown or blocked state. Stop here.
        if (attackPressTime <= 0) return;
        
        float holdDuration = Time.time - attackPressTime;

        // Only trigger if we held longer than the threshold
        if (holdDuration >= chargeThreshold)
        {
            isCharging = true;

            if (windingUpSlam)
            {
                BoundSpike();
            }
            else if (!isInCooldown)
            {
                if (playerController.isCrouching && playerController.IsGrounded) // Crouching on the ground
                {
                    ProcessLauncher(dashForce); // Perform charged launcher
                }
                else
                {
                    ExecuteChargeAttack();
                }
            }
        }
        
        // Reset timer to prevent double-triggering logic
        attackPressTime = 0;
        hasPerformedChargedAttack = true;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if ((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam) && !playerController.IsGrounded)
        {
            return;
        }

        if (playerController.IsGrounded && (playerController.isCrouching || playerController.landedFromGroundSlam)) // Crouch Launcher
        {
            Launcher(launcherForce, true);
        }
        else if (windingUpSlam)
        {
            AerialPush();
        }
        else if (attackStage > 0 && !isInCooldown) // Chain Launcher
        {
            if (playerController.IsGrounded || playerController.canCoyote)
            {
                Launcher(mediumLauncherForce, false);
            }
            else if (playerController.enableDoubleJump && playerController.availableJumps > 0)
            {
                Launcher(lightLauncherForce, false);
            }
        }
    }

    public void OnCrouch(InputValue value)
    {
        if (!value.isPressed) return;

        if ((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam ||
            currentAttackType == AttackType.Spike ||
            currentAttackType == AttackType.BoundSpike ||
            currentAttackType == AttackType.AerialPush) && !playerController.IsGrounded)
        {
            return;
        }

        if (playerController.isGrabbingLedge)
        {
            playerController.ReleaseLedge();
        }
        else if (!playerController.IsGrounded && !windingUpSlam) // Mid-air
        {
            GroundSlamWindup();
        }
    }
    #endregion

    #region Core Attack Logic
    private void ProcessCombo()
    {
        lastAttackTime = Time.time;
        isFinisher = (attackStage == currentWeapon.maxComboStage - 1);

        ExecuteAttack(isFinisher, false);

        attackStage++;

        if (isFinisher)
        {
            isInCooldown = true;
            cooldownTimer = finisherCooldownTime;
        }
    }

    private void ExecuteAttack(bool finisherFlag, bool dontPerformNormalAttack)
    {
        isFinisher = finisherFlag;
        playerController.pauseFastFall = true;

        if (isFinisher)
            currentAttackType = AttackType.Finisher;
        else if (isCharging)
            currentAttackType = AttackType.Charged;
        else if (!dontPerformNormalAttack)
            currentAttackType = AttackType.Normal;
        else
            return;
        
        // 1. Setup Stats
        bool countsAsDashAttack = playerController.WasRecentlyDashing(0.1f);
        float range = countsAsDashAttack ? dashRange : defaultRange;
        float force = countsAsDashAttack ? dashForce : defaultForce;

        // 2. Visuals
        PlayEffect((isFinisher || isCharging) ? finisherEffect : attackEffect);
        
        // 3. Hitbox Activation
        // In a real game, you'd call this via an Animation Event!
        weaponHitbox.ActivateHitbox();
        Invoke(nameof(StopHitbox), shortCooldownTime); // Safety turn-off after 0.25 seconds

        // 4. Lunge toward enemy (Magnetism)
        Collider[] hits = Physics.OverlapSphere(AttackOrigin, range, enemyLayer);
        if (hits.Length > 0)
        {
            GameObject target = GetBestTarget(hits);
            LungeAtTarget(target, force);
        }
        else
        {
            if (countsAsDashAttack)
            {
                // 1. Determine the direction of the dash attack
                // We use dashDirection if it exists, otherwise fall back to player forward
                Vector3 dashAtkDir = (playerController.dashDirection != Vector3.zero) ? playerController.dashDirection : transform.forward;

                // 2. CAPPING LOGIC: If slide velocity is less than dash speed, floor it at dash speed
                if (playerController.slideVelocity.magnitude < playerController.dashSpeed)
                {
                    playerController.SetSlideVelocity(dashAtkDir * playerController.dashSpeed);
                }
            }
            if (!playerController.IsGrounded)
            {
                if (isCharging)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                    rb.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                }
                else
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0, rb.linearVelocity.y), rb.linearVelocity.z);
                }
            }
        }

        if (countsAsDashAttack) Debug.Log("Successful Dash Attack!");
    }

    private GameObject GetBestTarget(Collider[] hits)
    {
        GameObject bestTarget = null;
        float closestAngle = -1f; // Dot product range is -1 to 1 (1 is perfect alignment)

        foreach (var hit in hits)
        {
            Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
            directionToEnemy.y = 0; // Ignore height differences for the "angle" check

            float dot = Vector3.Dot(transform.forward, directionToEnemy);

            // Higher dot product means the enemy is more "in front" of the player
            if (dot > closestAngle)
            {
                closestAngle = dot;
                bestTarget = hit.gameObject;
            }
        }

        return bestTarget;
    }

    private void LungeAtTarget(GameObject target, float force)
    {
        Transform centrePoint = target.transform.Find("EnemyCentrePoint");
        if (!centrePoint) return;

        Vector3 direction = (centrePoint.position - AttackOrigin).normalized;
        
        playerController.SetAttackForce(direction, force, true);
    }
    #endregion

    #region Special Moves
    private void ProcessLauncher(float force)
    {
        Launcher(force, true);

        bool countsAsDashAttack = playerController.WasRecentlyDashing(0.1f);
        if (countsAsDashAttack)
        {
            Collider[] hits = Physics.OverlapSphere(AttackOrigin, dashRange, enemyLayer);
            if (hits.Length > 0)
            {
                GameObject target = GetBestTarget(hits);
                LungeAtTarget(target, dashForce);
            }
            else
            {
                // 1. Determine the direction of the dash attack
                // We use dashDirection if it exists, otherwise fall back to player forward
                Vector3 dashAtkDir = (playerController.dashDirection != Vector3.zero) ? playerController.dashDirection : transform.forward;

                // 2. CAPPING LOGIC: If slide velocity is less than dash speed, floor it at dash speed
                if (playerController.slideVelocity.magnitude < playerController.dashSpeed)
                {
                    playerController.SetSlideVelocity(dashAtkDir * playerController.dashSpeed);
                }
            }
        }
    }

    private void Launcher(float force, bool shouldTriggerCooldown)
    {
        PlayEffect(isCharging ? finisherEffect : attackEffect);
        appliedJuggleForce = force;
        currentAttackType = AttackType.Launcher;
        weaponHitbox.ActivateHitbox();
        Invoke(nameof(StopHitbox), shortCooldownTime);
        // ... animation/effect logic ...
        
        if (shouldTriggerCooldown)
        {
            isInCooldown = true;
            if (isCharging)
            {
                cooldownTimer = finisherCooldownTime;
            }
            else
            {
                cooldownTimer = shortCooldownTime;
            }
        }
        else // For chain launchers, set the attack stage to 1 for extending combos
        {
            ResetCombo();
        }
    }

    private void GroundSlamWindup()
    {
        windingUpSlam = true;
        playerController.pauseFastFall = true;
        currentAttackType = AttackType.None;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z); // Cut vertical velocity in half
        countsAsDashSlam = playerController.WasRecentlyDashing(0.1f);
        windUpTimer = windUpDuration;

        if (playerController.moveInput.magnitude == 0 && !countsAsDashSlam)
        {
            playerController.SetSlideVelocity(Vector3.zero); // Kill slide speed if not holding a direction for more control during slam
        }
    }

    public void GroundSlam()
    {
        windingUpSlam = false;
        playerController.freezeRotation = true;
        playerController.pauseFastFall = false;
        rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
        PlayEffect(attackEffect);
        if (countsAsDashSlam)
        {
            Vector3 slamDirection = (playerController.dashDirection != Vector3.zero) ? playerController.dashDirection : transform.forward;
            playerController.SetAttackForce(slamDirection, bounceForce, false);
            currentAttackType = AttackType.DashSlam;
        }
        else
        {
            currentAttackType = AttackType.GroundSlam;
        }
        
        weaponHitbox.ActivateHitbox();
        // ... animation/effect logic ...

        // Start checking for landing to apply slide momentum
        StartCoroutine(CheckGroundSlamLanding());
    }

    public IEnumerator CheckGroundSlamLanding()
    {
        // Wait until we land
        while (!playerController.IsGrounded)
        {
            yield return null;
        }
        
        // Check if we landed on a slope
        if (playerController.OnSlope())
        {
            // Calculate slope angle
            float slopeAngle = Vector3.Angle(playerController.GetSlopeNormal(), Vector3.up);
            
            // Calculate angle-based max speed
            float angleBasedMaxSpeed = Mathf.Clamp(slopeAngle * 0.5f, 1f, playerController.maxSlopeSlideSpeed);
            
            // Set slide velocity to 25% of max speed
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, playerController.GetSlopeNormal()).normalized;
            slideDir.y = 0;
            
            if (playerController.slideVelocity.magnitude < (angleBasedMaxSpeed * 0.5f))
            {
                playerController.SetSlideVelocity(slideDir * (angleBasedMaxSpeed * 0.5f));
            }
        }

        if (countsAsDashSlam)
        {
            playerController.SetSlideVelocity(transform.forward * Mathf.Max(playerController.slideVelocity.magnitude, bounceForce)); // Maintain momentum from dash slam after landing
        }

        StopHitbox();
        playerController.freezeRotation = false;
        playerController.landedFromGroundSlam = true;
        playerController.slamJumpTimer = playerController.slamJumpTime;
    }

    private void Spike()
    {
        windingUpSlam = false;
        PlayEffect(isCharging ? finisherEffect : attackEffect);
        currentAttackType = AttackType.Spike;
        weaponHitbox.ActivateHitbox();
        Invoke(nameof(StopHitbox), shortCooldownTime);
        // ... animation/effect logic ...
        
        if (countsAsDashSlam)
        {
            Collider[] hits = Physics.OverlapSphere(AttackOrigin, dashRange, enemyLayer);
            if (hits.Length > 0)
            {
                GameObject target = GetBestTarget(hits);
                LungeAtTarget(target, dashForce);
            }
            else
            {
                // 1. Determine the direction of the dash attack
                // We use dashDirection if it exists, otherwise fall back to player forward
                Vector3 dashAtkDir = (playerController.dashDirection != Vector3.zero) ? playerController.dashDirection : transform.forward;

                // 2. CAPPING LOGIC: If slide velocity is less than dash speed, floor it at dash speed
                if (playerController.slideVelocity.magnitude < playerController.dashSpeed)
                {
                    playerController.SetSlideVelocity(dashAtkDir * playerController.dashSpeed);
                }
            }
        }

        isInCooldown = true;
        cooldownTimer = shortCooldownTime;
    }

    private void BoundSpike()
    {
        windingUpSlam = false;
        PlayEffect(attackEffect);
        currentAttackType = AttackType.BoundSpike;
        weaponHitbox.ActivateHitbox();
        Invoke(nameof(StopHitbox), shortCooldownTime);
        // ... animation/effect logic ...
        
        if (countsAsDashSlam)
        {
            Collider[] hits = Physics.OverlapSphere(AttackOrigin, dashRange, enemyLayer);
            if (hits.Length > 0)
            {
                GameObject target = GetBestTarget(hits);
                LungeAtTarget(target, dashForce);
            }
            else
            {
                // 1. Determine the direction of the dash attack
                // We use dashDirection if it exists, otherwise fall back to player forward
                Vector3 dashAtkDir = (playerController.dashDirection != Vector3.zero) ? playerController.dashDirection : transform.forward;

                // 2. CAPPING LOGIC: If slide velocity is less than dash speed, floor it at dash speed
                if (playerController.slideVelocity.magnitude < playerController.dashSpeed)
                {
                    playerController.SetSlideVelocity(dashAtkDir * playerController.dashSpeed);
                }
            }
        }

        isInCooldown = true;
        cooldownTimer = finisherCooldownTime;
    }
    
    private void AerialPush()
    {
        windingUpSlam = false;
        PlayEffect(attackEffect);
        weaponHitbox.ActivateHitbox();
        Invoke(nameof(StopHitbox), shortCooldownTime);
        if (playerController.availableAerialPushes > 0)
        {
            currentAttackType = AttackType.AerialPush;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, playerController.shortJumpForce, rb.linearVelocity.z);
            Vector3 moveDir = playerController.GetCameraRelativeDirection(playerController.moveInput);
            Vector3 pushDirection = (moveDir != Vector3.zero) ? moveDir.normalized : transform.forward;
            playerController.SetAttackForce(pushDirection, bounceForce, false);
            playerController.availableAerialPushes --;
        }
        else
        {
            currentAttackType = AttackType.WeakPush;
        }

        isInCooldown = true;
        cooldownTimer = shortCooldownTime;
    }

    private void ExecuteChargeAttack()
    {
        Debug.Log("UNLEASHING CHARGE ATTACK!");
        if (playerController.isGrabbingLedge)
        {
            playerController.ReleaseLedge();
        }
        
        ExecuteAttack(false, true);
        
        isInCooldown = true;
        cooldownTimer = finisherCooldownTime;
    }
    #endregion

    #region Helpers
    public void ResetCombo()
    {
        attackStage = 0;
        isInCooldown = false;
        cooldownTimer = 0;
        if (hasPerformedChargedAttack) ResetCharge();
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect == null) return;
        effect.Stop();
        effect.Play();
    }

    public void StopHitbox()
    {
        currentAttackType = AttackType.None;
        isFinisher = false;
        weaponHitbox.DeactivateHitbox();
        playerController.StopAttacking();
        playerController.pauseFastFall = false;
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeLevel = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(AttackOrigin, defaultRange);
    }
    #endregion
}