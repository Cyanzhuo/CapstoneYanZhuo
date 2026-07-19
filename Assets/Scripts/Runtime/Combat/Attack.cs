/*
* Author: Cheang Wei Cheng
* Date: 23 February 2025
* Description: This script is responsible for handling the player's attack with a bat.
* The script uses a sphere cast to detect enemies within the attack range.
* It also applies a force to the player to move them towards the enemy when attacking.
*/

using Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Attack : MonoBehaviour
{
    private PlayerInputActions controls;
    public Weapons currentWeapon;

    [Header("Combo Settings")]
    public float finisherCooldownTime = 1f;
    [SerializeField] private float shortCooldownTime = 0.25f;
    [SerializeField] private float windUpDuration = 0.2f;
    [SerializeField] private float aerialPushDuration = 0.25f;

    [Header("Attack Physics")]
    [SerializeField] private float defaultRange = 1f;
    [SerializeField] private float dashRange = 2f;
    [SerializeField] private float enemyProximityThreshold = 0.3f; // How close the enemy needs to be to trigger StopAttacking()
    [SerializeField] private float verticalExclusionAngle = 30f;
    [SerializeField] private float horizontalExclusionAngle = 45f;
    public float defaultForce = 5f;
    public float dashForce = 10f;
    [SerializeField] public float launcherForce = 7f;
    [SerializeField] private float mediumLauncherForce = 6f;
    [SerializeField] private float lightLauncherForce = 5f;
    public float juggleForce = 3f;
    public float bounceForce = 7.5f;
    public LayerMask enemyLayer;

    [Header("Charge Attack")]
    [SerializeField] private float chargeThreshold = 0.5f; // How long to hold for charge level 1
    [SerializeField] private float chargeLevel2Threshold = 1f; // How long to hold for charge level 2
    [HideInInspector] public float attackPressTime; // When the button was first pressed
    [HideInInspector] public int chargeLevel = 0; // 0 = none, 1 = level 1, 2 = level 2

    [Header("Hitbox Reference")]
    [HideInInspector] public Hitbox weaponHitbox;

    // Internal State Flags for the Hitbox to read
    public enum AttackType
    {
        None, Normal, Charged, Finisher, Launcher, GroundSlam, DashSlam, Spike, BoundSpike, AerialPush, WeakPush
    }

    [HideInInspector] public AttackType currentAttackType;
    [HideInInspector] public bool isFinisher;
    [HideInInspector] public bool isCharging;

    [Header("Input Buffering")]
    [SerializeField] private float attackBufferTime = 0.15f;
    private float attackBufferTimer = 0f;
    private bool attackBuffered = false;
    private bool holdBuffered = false;
    private float holdBufferTimer = 0f;

    [Header("Effects")]
    public ParticleSystem attackEffect;
    public ParticleSystem finisherEffect;
    public ParticleSystem chargeEffect;

    // Component References
    private Animator animator;
    private Rigidbody rb;
    private ThirdPersonController playerController;
    [SerializeField] LungeDetection lungeTrigger;

    // Internal State
    private int attackStage = 0;
    [HideInInspector] public float cooldownTimer;
    private float attackDurationTimer;
    private bool hasPerformedChargedAttack;
    [HideInInspector] public bool isInCooldown;
    [HideInInspector] public bool countsAsDashSlam;
    [HideInInspector] public float appliedJuggleForce;
    [HideInInspector] public bool windingUpSlam;
    private float windUpTimer;
    public Coroutine groundSlamLandingCoroutine;
    [HideInInspector] public Vector3 directionToEnemy;

    // Helper property for chest-level origin
    private Vector3 AttackOrigin => transform.position + Vector3.up * 0.6f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<ThirdPersonController>();
        lungeTrigger = GetComponentInChildren<LungeDetection>();
        lungeTrigger.sphere.enabled = false;
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
        if (attackBuffered)
        {
            attackBufferTimer -= Time.deltaTime;
            if (attackBufferTimer <= 0)
            {
                attackBuffered = false;
            }
            else if (currentAttackType == AttackType.None && !isInCooldown) // Only allow buffered input if we're not in the middle of another attack
            {
                ExecuteAttackInput();
                attackBuffered = false;
            }
        }

        if (holdBuffered)
        {
            holdBufferTimer -= Time.deltaTime;
            if (holdBufferTimer <= 0)
            {
                holdBuffered = false;
            }
            else if (currentAttackType == AttackType.None && !isInCooldown) // Only allow buffered input if we're not in the middle of another attack
            {
                ExecuteHoldInput();
                holdBuffered = false;
            }
        }

        if ((attackStage > 0 || isInCooldown) && (!playerController.isAttacking || playerController.attackHasSetEndTime))
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                ResetCombo();
            }
        }

        // Placeholder, in the final game this should be handled by Animation Events
        if ((attackDurationTimer > 0) && (!playerController.isAttacking || playerController.attackHasSetEndTime)) // Only start counting down if you're not lunging (unless the attack has a set end time)
        {
            attackDurationTimer -= Time.deltaTime;
            if (attackDurationTimer <= 0)
            {
                StopHitbox();
            }
        }

        // Visual feedback for being "Charged"
        if (attackPressTime > 0 && !isInCooldown)
        {
            float holdDuration = Time.time - attackPressTime;

            if (holdDuration >= chargeLevel2Threshold && chargeLevel < 2)
            {
                InterimAudioDirector.TryPlayMove(InterimAudioCue.Charge, transform.position);
                PlayEffect(chargeEffect);
                Debug.Log("Charge Level 2!");
                chargeLevel = 2;
            }
            else if (holdDuration >= chargeThreshold && chargeLevel < 1)
            {
                InterimAudioDirector.TryPlayMove(InterimAudioCue.Charge, transform.position);
                PlayEffect(chargeEffect);
                Debug.Log("Charge Level 1!");
                chargeLevel = 1;
            }
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
        if ((currentAttackType != AttackType.None && // Prevent interrupting your own attacks...
            currentAttackType != AttackType.WeakPush) || // ...except for the weak push
            (isInCooldown && !windingUpSlam)) // Buffer input if we're in cooldown, but allow it if we're winding up a slam since that's a different state
        {
            attackBuffered = true;
            attackBufferTimer = attackBufferTime;
            return;
        }

        ExecuteAttackInput();
    }

    private void ExecuteAttackInput()
    {            
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

    private void OnAttackCanceled()
    {
        // If attackPressTime is 0, it means the button was pressed during a cooldown or blocked state. Stop here.
        if (attackPressTime <= 0) return;

        float holdDuration = Time.time - attackPressTime;
        attackPressTime = 0; // Hold duration calculated, can now safely reset the timer

        // Only trigger if we held longer than the threshold
        if (holdDuration >= chargeThreshold)
        {
            if ((currentAttackType != AttackType.None &&
                currentAttackType != AttackType.WeakPush) ||
                (isInCooldown && !windingUpSlam))
            {
                holdBuffered = true;
                holdBufferTimer = attackBufferTime;
                return;
            }
            ExecuteHoldInput();
        }
    }

    private void ExecuteHoldInput()
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
        
        hasPerformedChargedAttack = true;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if ((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam ||
            currentAttackType == AttackType.AerialPush) && !playerController.IsGrounded)
        {
            return;
        }

        if (playerController.IsGrounded && (playerController.isCrouching || playerController.landedFromGroundSlam)) // Crouch Launcher
        {
            return; // Let TPC handle this
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
        else // Normal Jump
        {
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
    }

    public void OnCrouch(InputValue value)
    {
        if (!value.isPressed) return;

        if (((currentAttackType == AttackType.GroundSlam ||
            currentAttackType == AttackType.DashSlam ||
            currentAttackType == AttackType.Spike ||
            currentAttackType == AttackType.BoundSpike ||
            currentAttackType == AttackType.AerialPush) && !playerController.IsGrounded) ||
            playerController.isAttacking)
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
        cooldownTimer = finisherCooldownTime;
        isFinisher = (attackStage == currentWeapon.maxComboStage - 1);

        ExecuteAttack(isFinisher, false);

        attackStage++;

        if (isFinisher)
        {
            isInCooldown = true;
        }
    }

    private void ExecuteAttack(bool finisherFlag, bool dontPerformNormalAttack)
    {
        isFinisher = finisherFlag;

        if (isFinisher)
            currentAttackType = AttackType.Finisher;
        else if (isCharging)
            currentAttackType = AttackType.Charged;
        else if (!dontPerformNormalAttack)
            currentAttackType = AttackType.Normal;
        else
            return;

        playerController.pauseFastFall = true;

        // 1. Setup Stats
        bool countsAsDashAttack = playerController.WasRecentlyDashing(0.1f);
        float range = countsAsDashAttack ? dashRange : defaultRange;
        float force = countsAsDashAttack ? dashForce : defaultForce;

        // 2. Visuals
        InterimAudioDirector.TryPlayMove(isCharging ? InterimAudioCue.ChargedAttack : InterimAudioCue.BasicAttack, transform.position);
        PlayEffect((isFinisher || isCharging) ? finisherEffect : attackEffect);
        
        // 3. Hitbox Activation
        // In the final game, this should be called via an Animation Event
        weaponHitbox.ActivateHitbox();
        attackDurationTimer = currentWeapon.hitboxLifetime;
        if (animator != null)
        {
            switch (currentAttackType)
            {
                case AttackType.Normal:
                    animator.SetTrigger("Attack" + (attackStage + 1));
                    break;
                case AttackType.Charged:
                case AttackType.Finisher:
                    animator.SetTrigger("ChargedOrFinisher");
                    break;
                default:
                    break;
            }
        }

        // 4. Lunge toward enemy (Magnetism)
        Collider[] hits = Physics.OverlapSphere(AttackOrigin, range, enemyLayer);
        bool hasValidTarget = false;
        if (hits.Length > 0)
        {
            GameObject target = GetBestTarget(hits);
            if (target != null)
            {
                LungeAtTarget(target, force);
                playerController.LockOnTarget(target);
                hasValidTarget = true;
                lungeTrigger.sphere.enabled = true;
                lungeTrigger.currentAttackTarget = target;
                lungeTrigger.timer = lungeTrigger.duration;
            }
        }
        if (!hasValidTarget)
        {
            if (countsAsDashAttack)
            {
                ApplyDashPhysics();
            }
            if (!playerController.IsGrounded)
            {
                if (isCharging && playerController.availableChargeAttackJumps > 0)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                    rb.AddForce(Vector3.up * playerController.doubleJumpForce, ForceMode.Impulse);
                    playerController.availableChargeAttackJumps --;
                }
                else
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(playerController.slowFallSpeed, rb.linearVelocity.y), rb.linearVelocity.z);
                }
            }
        }

        if (countsAsDashAttack) Debug.Log("Successful Dash Attack!");
    }

    private GameObject GetBestTarget(Collider[] hits)
    {
        GameObject bestTarget = null;
        float closestAngle = -1f; // Dot product range is -1 to 1 (1 is perfect alignment)

        Vector3 moveDir = playerController.GetCameraRelativeDirection(playerController.moveInput);
        Vector3 aimDir = (moveDir != Vector3.zero) ? moveDir.normalized : transform.forward;
        Vector3 behindCheckDir = (moveDir != Vector3.zero) ? -moveDir.normalized : Vector3.zero;
        
        foreach (var hit in hits)
        {
            Vector3 directionToEnemy = hit.transform.position - transform.position;
            
            // Calculate vertical angle (starting from horizontal plane)
            float verticalAngle = Mathf.Abs(Mathf.Asin(directionToEnemy.normalized.y) * Mathf.Rad2Deg);
            
            // Skip enemies above the threshold
            if (verticalAngle > (90 - verticalExclusionAngle)) continue;
            
            directionToEnemy.y = 0;
            directionToEnemy.Normalize();

            // Skip enemies directly behind
            if (behindCheckDir != Vector3.zero)
            {
                float behindDot = Vector3.Dot(behindCheckDir, directionToEnemy);
                if (behindDot > Mathf.Cos(horizontalExclusionAngle * Mathf.Deg2Rad)) continue;
            }
            
            float dot = Vector3.Dot(aimDir, directionToEnemy);

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

        // Add targeting offset (aim at the front of the enemy, not the centre)
        Vector3 directDirectionToEnemy = (centrePoint.position - AttackOrigin).normalized;
        // Now that we have the direct direction to the enemy, we can apply an offset to it to aim at the front of the enemy
        Vector3 offset = directDirectionToEnemy * enemyProximityThreshold;
        Vector3 targetPosition = centrePoint.position - offset;
        directionToEnemy = (targetPosition - AttackOrigin).normalized;
        
        playerController.SetAttackForce(directionToEnemy, force, true);

        if (animator != null)
        {
            animator.SetFloat("AtkAnimaSpeed", 0f);
        }
    }
    #endregion

    #region Special Moves
    private void ProcessLauncher(float force)
    {
        bool countsAsDashAttack = playerController.WasRecentlyDashing(0.1f);
        Launcher(force, true, countsAsDashAttack);
    }

    public void Launcher(float force, bool shouldTriggerCooldown, bool countsAsDashAttack = false)
    {
        InterimAudioDirector.TryPlayMove(
            isCharging ? InterimAudioCue.ChargedCrouchAttack : InterimAudioCue.LauncherJump,
            transform.position
        );
        PlayEffect(isCharging ? finisherEffect : attackEffect);
        appliedJuggleForce = force;
        currentAttackType = AttackType.Launcher;
        weaponHitbox.ActivateHitbox();
        attackDurationTimer = currentWeapon.hitboxLifetime;
        if (animator != null)
        {
            animator.SetTrigger("Launcher");
        }

        float range = countsAsDashAttack ? dashRange : defaultRange;
        Collider[] hits = Physics.OverlapSphere(AttackOrigin, range, enemyLayer);
        bool hasValidTarget = false;
        if (hits.Length > 0)
        {
            GameObject target = GetBestTarget(hits);
            if (target != null)
            {
                playerController.LockOnTarget(target);
                if (countsAsDashAttack)
                {
                    LungeAtTarget(target, dashForce);
                    lungeTrigger.sphere.enabled = true;
                    lungeTrigger.currentAttackTarget = target;
                    lungeTrigger.timer = lungeTrigger.duration;
                }
                hasValidTarget = true;
            }
        }
        if (!hasValidTarget && countsAsDashAttack)
        {
            ApplyDashPhysics();
        }
        
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
        else
        {
            ResetCombo();
        }
    }

    private void GroundSlamWindup()
    {
        windingUpSlam = true;
        StopHitbox(true);
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
        InterimAudioDirector.TryPlayMove(InterimAudioCue.GroundSlamJump, transform.position);
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
        groundSlamLandingCoroutine = StartCoroutine(CheckGroundSlamLanding());
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

        InterimAudioDirector.TryPlayMove(InterimAudioCue.GroundSlamHit, transform.position);
        StopHitbox();
        isInCooldown = true;
        cooldownTimer = shortCooldownTime;
        playerController.landedFromGroundSlam = true;
        playerController.slamJumpTimer = playerController.slamJumpTime;
    }

    private void Spike()
    {
        windingUpSlam = false;
        InterimAudioDirector.TryPlayMove(InterimAudioCue.SpikeSecondJump, transform.position);
        PlayEffect(isCharging ? finisherEffect : attackEffect);
        currentAttackType = AttackType.Spike;
        weaponHitbox.ActivateHitbox();
        attackDurationTimer = currentWeapon.hitboxLifetime;
        // ... animation/effect logic ...
        
        float range = countsAsDashSlam ? dashRange : defaultRange;
        Collider[] hits = Physics.OverlapSphere(AttackOrigin, range, enemyLayer);
        bool hasValidTarget = false;
        if (hits.Length > 0)
        {
            GameObject target = GetBestTarget(hits);
            if (target != null)
            {
                playerController.LockOnTarget(target);
                if (countsAsDashSlam)
                {
                    LungeAtTarget(target, dashForce);
                    lungeTrigger.sphere.enabled = true;
                    lungeTrigger.currentAttackTarget = target;
                    lungeTrigger.timer = lungeTrigger.duration;
                }
                hasValidTarget = true;
            }
        }
        if (!hasValidTarget && countsAsDashSlam)
        {
            ApplyDashPhysics();
        }

        isInCooldown = true;
        cooldownTimer = shortCooldownTime;
    }

    private void BoundSpike()
    {
        windingUpSlam = false;
        InterimAudioDirector.TryPlayMove(InterimAudioCue.ChargedAttack, transform.position);
        PlayEffect(attackEffect);
        currentAttackType = AttackType.BoundSpike;
        weaponHitbox.ActivateHitbox();
        attackDurationTimer = currentWeapon.hitboxLifetime;
        // ... animation/effect logic ...
        
        float range = countsAsDashSlam ? dashRange : defaultRange;
        Collider[] hits = Physics.OverlapSphere(AttackOrigin, range, enemyLayer);
        bool hasValidTarget = false;
        if (hits.Length > 0)
        {
            GameObject target = GetBestTarget(hits);
            if (target != null)
            {
                playerController.LockOnTarget(target);
                if (countsAsDashSlam)
                {
                    LungeAtTarget(target, dashForce);
                    lungeTrigger.sphere.enabled = true;
                    lungeTrigger.currentAttackTarget = target;
                    lungeTrigger.timer = lungeTrigger.duration;
                }
                hasValidTarget = true;
            }
        }
        if (!hasValidTarget && countsAsDashSlam)
        {
            ApplyDashPhysics();
        }

        isInCooldown = true;
        cooldownTimer = finisherCooldownTime;
    }
    
    private void AerialPush()
    {
        windingUpSlam = false;
        InterimAudioDirector.TryPlayMove(InterimAudioCue.AerialPush, transform.position);
        PlayEffect(attackEffect);
        weaponHitbox.ActivateHitbox();
        attackDurationTimer = aerialPushDuration;
        if (playerController.availableAerialPushes > 0)
        {
            currentAttackType = AttackType.AerialPush;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, playerController.shortJumpForce, rb.linearVelocity.z);
            Vector3 moveDir = playerController.GetCameraRelativeDirection(playerController.moveInput);
            Vector3 pushDirection = (moveDir != Vector3.zero) ? moveDir.normalized : transform.forward;
            playerController.SetAttackForce(pushDirection, bounceForce, false, true);
            playerController.availableAerialPushes --;

            isInCooldown = true;
            cooldownTimer = shortCooldownTime;
        }
        else
        {
            currentAttackType = AttackType.WeakPush;
            ResetCombo();
        }
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

    public void StopHitbox(bool shouldPauseFastFall = false)
    {
        currentAttackType = AttackType.None;
        isFinisher = false;
        lungeTrigger.hasAppliedAirBoost = false;
        weaponHitbox.DeactivateHitbox();
        playerController.StopAttacking();
        playerController.UnlockTarget();
        playerController.pauseFastFall = shouldPauseFastFall;
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeLevel = 0;
    }

    private void ApplyDashPhysics()
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(AttackOrigin, defaultRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(AttackOrigin, dashRange);
    }
    #endregion
}