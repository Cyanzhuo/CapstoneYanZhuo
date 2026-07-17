/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description: This script handels the movement and jumping mechanics of a third-person character controller in Unity.
* It allows the character to move relative to the camera's orientation, jump when grounded, and plays a sound effect upon jumping.
* The character's movement is controlled using Rigidbody physics for smooth and responsive interactions.
* The script also includes a ground check using raycast to ensure the character can only jump when on the ground.
* This script is designed to be attached to a GameObject with a Rigidbody component and a Collider.
*/

using Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
    private PlayerInputActions controls;
    [Header("Audio")]
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip dashSound;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float sneakSpeed = 2.5f;
    public float coyoteTime = 0.2f;

    [Header("Jumping")]
    public float jumpForce = 6f;
    public float shortJumpForce = 3f;
    public bool enableDoubleJump = true;
    public float doubleJumpForce = 5f;
    [SerializeField] private float highJumpMultiplier = 1.25f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float lowGravityMultiplier = 0.5f;
    [SerializeField] public float slowFallSpeed = -1f;
    [SerializeField] public float slamJumpTime = 0.2f;
    [HideInInspector] public float slamJumpTimer;

    [Header("Jump Buffering")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferTimer = 0f;
    private bool jumpBuffered = false;
    private bool bufferedJumpWasReleased = false;

    [Header("Dash")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.1f;
    public float dashCooldown = 1f;
    public float dashBufferTime = 0.1f;
    public bool enableAirDash = true;
    public bool dashUpgradeObtained = false;
    private bool dashBuffered = false;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeDownForce = 20f;
    private float upSlopeResistance = 0f;
    private float angleBasedInputReduction;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Crouch Settings")]
    [SerializeField] public float maxSlopeSlideSpeed = 20f; // Speed when sliding down slopes
    [SerializeField] private float maxSlopeSlideAcceleration = 10f; // How quickly you reach max slide speed
    [SerializeField] private float maxSlopeAngleForSlide = 40f; // Maximum angle for sliding to occur
    [SerializeField] private float friction = 2f; // How quickly you slow down on flat ground
    [SerializeField] private float slideFriction = 2.5f; // How quickly you slow down when sliding
    [SerializeField] private float airFriction = 0.5f; // How quickly you slow down in mid-air
    [SerializeField] private float flatGroundTurnSpeed = 4f; // How quickly slide speed builds
    [HideInInspector] public Vector3 slideVelocity; // Track slide momentum

    [Header("Attack Settings")]
    [SerializeField] float attackRotationSpeed = 60f;
    [HideInInspector] public Vector3 attackDirection;
    [HideInInspector] public float attackForce;
    [HideInInspector] public bool isAttacking;
    private bool attackShouldModifyYaxis;
    private GameObject lockOnTarget;
    [HideInInspector] public bool freezeRotation;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeCheckForwardDistance = 0.4f;
    [SerializeField] private float ledgeCheckDownDistance = 0.6f;
    [SerializeField] private float ledgeGrabHeight = 1.2f;
    [SerializeField] private LayerMask ledgeLayer;
    [HideInInspector] public bool isGrabbingLedge = false;
    private Vector3 ledgeGrabPoint;
    private Vector3 ledgeNormal;
    private float ledgeGrabCooldownTimer;
    [SerializeField] private float ledgeGrabCooldown = 0.5f;

    [Header("Slide Braking")]
    [SerializeField] private float brakeDeceleration = 10f; // How fast you slow down when braking
    [SerializeField] private float brakeInputThreshold = 0.8f; // How far opposite you need to flick
    private float brakeMultiplier = 1f;

    // Component References
    private Rigidbody rb;
    private Attack attack;
    private WallRun wallRun;
    private Animator animator;
    private Camera mainCamera;

    // Internal State
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 targetVelocity;
    private float lastGroundedTime;
    [HideInInspector] public int availableJumps;
    [HideInInspector] public int availableAerialPushes;
    [HideInInspector] public int availableChargeAttackJumps;
    [HideInInspector] public bool canCoyote;
    private bool wasGrounded;
    private float lastUngroundedVerticalSpeed;
    private float dashTimer;
    private float dashCooldownTimer;
    [HideInInspector] public Vector3 dashDirection;
    private float dashEndTime;
    private float dashCancelTime;
    private bool dashCancelled;
    [HideInInspector] public int availableDashes;
    [HideInInspector] public bool isDashing;
    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool pauseFastFall;
    [HideInInspector] public bool landedFromGroundSlam;
    [HideInInspector] public bool attackHasSetEndTime;
    bool isBraking = false;
    public bool WasRecentlyDashing(float leniency) => isDashing || (Time.time - dashEndTime < leniency) || (dashCancelled && (Time.time - dashCancelTime < dashDuration));

    // Input Flags (The "Intent")
    private bool jumpRequested;
    private bool jumpCanceled; // Key for variable jump height
    private bool highJumpRequested; // For crouch jump boost

    [SerializeField] float groundCheckDistance = .5f;

    public bool IsGrounded => Physics.SphereCast(transform.position + (Vector3.up * 0.4f), 0.2f, Vector3.down, out _, groundCheckDistance);

    public bool OnSlope()
    {
        if (Physics.SphereCast(transform.position + (Vector3.up * 0.4f), 0.2f, Vector3.down, out slopeHit, groundCheckDistance))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            return slopeAngle < maxSlopeAngle && slopeAngle != 0;
        }
        return false;
    }

    // Add this property to expose slope normal
    public Vector3 GetSlopeNormal()
    {
        return slopeHit.normal;
    }

    // Add this method to set slide velocity from Attack script
    public void SetSlideVelocity(Vector3 velocity)
    {
        slideVelocity = velocity;
    }

    public void SetAttackForce(Vector3 direction, float force, bool shouldModifyYaxis, bool hasSetEndTime = false)
    {
        attackDirection = direction;
        attackForce = force;
        attackHasSetEndTime = hasSetEndTime;

        if (shouldModifyYaxis)
        {
            attackShouldModifyYaxis = true;
        }
        else
        {
            attackShouldModifyYaxis = false;
        }

        isAttacking = true;
        isDashing = false;
        brakeMultiplier = 1f; // Reset brake multiplier when starting an attack

        // Redirect slide velocity to match attack direction
        SetSlideVelocity(direction * slideVelocity.magnitude);
    }

    public void StopAttacking()
    {
        if (isAttacking) isAttacking = false;
    }

    public void LockOnTarget(GameObject target)
    {
        lockOnTarget = target;
        freezeRotation = true;
    }

    public void UnlockTarget()
    {
        lockOnTarget = null;
        freezeRotation = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        attack = GetComponent<Attack>();
        wallRun = GetComponent<WallRun>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        lastGroundedTime = -coyoteTime;
    }

    void Awake()
    {
        controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        // Enable the input actions
        controls.Player.Enable();
        
        controls.Player.Jump.started += ctx => OnJumpStarted();
        controls.Player.Jump.canceled += ctx => OnJumpCanceled();
        
        controls.Player.Crouch.started += ctx => OnCrouchStarted();
        controls.Player.Crouch.canceled += ctx => OnCrouchCanceled();
    }

    private void OnDisable()
    {
        // Unsubscribe and disable input actions
        controls.Player.Jump.started -= ctx => OnJumpStarted();
        controls.Player.Jump.canceled -= ctx => OnJumpCanceled();

        controls.Player.Crouch.started -= ctx => OnCrouchStarted();
        controls.Player.Crouch.canceled -= ctx => OnCrouchCanceled();
        
        controls.Player.Disable();
    }

    #region Input Methods
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    private void OnJumpStarted()
    {
        if (attack != null && (attack.windingUpSlam || attack.currentAttackType == Attack.AttackType.AerialPush))
        {
            return;
        }

        bool performingSlam = attack != null &&
            (attack.currentAttackType == Attack.AttackType.GroundSlam ||
            attack.currentAttackType == Attack.AttackType.DashSlam);

        if (!IsGrounded && (!enableDoubleJump || availableJumps <= 0) || performingSlam)
        {
            jumpBuffered = true;
            jumpBufferTimer = jumpBufferTime;
            if (performingSlam)
            {
                highJumpRequested = true;
            }
        }
        else
        {
            jumpRequested = true;
            jumpCanceled = false;
            pauseFastFall = false;
            if (IsGrounded && (isCrouching || landedFromGroundSlam))
            {
                highJumpRequested = true;
            }
        }
    }

    private void OnJumpCanceled()
    {
        if (highJumpRequested)
        {
            highJumpRequested = false;
            return; // Don't cancel the jump if we were trying to do a crouch jump boost
        }
        jumpCanceled = true;
        if (jumpBuffered && !IsGrounded)
        {
            bufferedJumpWasReleased = true;
        }
    }

    public void OnCrouchStarted()
    {
        isCrouching = true;
        InterimAudioDirector.TryPlayPlayerCrouch(transform.position);
        
        if (IsGrounded)
        {
            // Set slide velocity to moveInput magnitude
            if (moveInput.magnitude > 0.1f && slideVelocity.magnitude < GetCameraRelativeDirection(moveInput).magnitude * moveSpeed)
            {
                SetSlideVelocity(GetCameraRelativeDirection(moveInput).normalized * moveInput.magnitude * moveSpeed);
            }
            if (WasRecentlyDashing(0.1f))
            {
                Vector3 dashDir = (dashDirection != Vector3.zero) ? dashDirection : transform.forward;
                if (slideVelocity.magnitude < dashSpeed)
                {
                    slideVelocity = dashDir * dashSpeed;
                }
                isDashing = false;
                if (!dashCancelled)
                {
                    dashCancelTime = Time.time;
                    dashCancelled = true;
                }

                InterimAudioDirector.TryPlayMove(InterimAudioCue.Slide, transform.position);
            }
        }
    }

    private void OnCrouchCanceled()
    {
        isCrouching = false;
    }
    
    public void OnDash(InputValue value)
    {
        if (value.isPressed && !isDashing && !isGrabbingLedge)
        {
            if (IsGrounded || (enableAirDash && availableDashes > 0)) 
            {
                if (dashCooldownTimer <= 0)
                {
                    StartDash();
                }
                else if (dashCooldownTimer <= dashBufferTime)
                {
                    dashBuffered = true;
                }
            }
        }
    }
    #endregion

    void Update()
    {        
        HandleTimers();
        HandleGroundedState();
    }

    void FixedUpdate()
    {
        ApplySliding();

        if (isGrabbingLedge)
        {
            ReportMovementAudio(false);
            HandleLedgeGrab();
            return;
        }

        if (rb.linearVelocity.y < 0 && !IsGrounded)
        {
            if (pauseFastFall)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowGravityMultiplier - 1) * Time.fixedDeltaTime; // Apply reduced gravity for a short time after dashing
            }
            else
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime; // Apply extra gravity when falling for snappier jumps
            }
        }
        
        HandleJumpPhysics();

        if (isDashing)
        {
            ApplyDashMovement();
            ReportMovementAudio(false);
            return;
        }

        ApplyRotationDuringAttack();

        if (isAttacking)
        {
            ApplyAttackMovement();
            ReportMovementAudio(false);
            return;
        }

        ApplyMovement();
        ApplyRotation();
        UpdateAnimation();
        ReportMovementAudio(true);
    }

    #region Movement Logic
    private void ApplyMovement()
    {
        Vector3 worldMoveDir = GetCameraRelativeDirection(moveInput);
        if (!isBraking) brakeMultiplier = Mathf.MoveTowards(brakeMultiplier, 1f, moveSpeed * Time.fixedDeltaTime); // Gradually reset brake multiplier when not braking
        
        // 1. Filter out input according to context
        if (OnSlope() && isCrouching && IsGrounded && !exitingSlope)
        {
            // Get the up-and-down-slope direction
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            
            // Check if player is trying to move up the slope
            float slopeInput = Vector3.Dot(worldMoveDir, slopeDir);
            
            if (slopeInput < 0) // Moving up the slope
            {
                // Gradually reduce up-slope movement
                upSlopeResistance = Mathf.MoveTowards(upSlopeResistance, 1f, angleBasedInputReduction * Time.fixedDeltaTime);
                Vector3 correctedDir = worldMoveDir - (slopeDir * slopeInput * upSlopeResistance);
                targetVelocity = correctedDir * sneakSpeed;
            }
            else
            {
                targetVelocity = worldMoveDir * sneakSpeed;
            }
        }
        else
        {
            upSlopeResistance = 0f; // Reset resistance when not on slope or not crouching
            float movementMultiplier = (isCrouching && IsGrounded) ? sneakSpeed : moveSpeed;
            targetVelocity = GetCameraRelativeDirection(moveInput) * movementMultiplier * brakeMultiplier;
        }
        
        if (OnSlope() && isCrouching && IsGrounded && !exitingSlope)
        {
            targetVelocity += slideVelocity;
        }
        // If slideVelocity is higher than the input velocity, or the player is braking, override it comepletely
        else if (slideVelocity.magnitude > targetVelocity.magnitude || isBraking)
        {
            targetVelocity = slideVelocity;
        }
        
        // 2. Handle slope projection
        if (OnSlope() && !exitingSlope)
        {
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, slopeHit.normal);
            rb.AddForce(-slopeHit.normal * slopeDownForce, ForceMode.Force);
        }
        
        // 3. Apply velocity directly (this is the key change)
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    private void ApplyRotation()
    {
        Vector3 moveDir = GetCameraRelativeDirection(moveInput);
        if (moveDir != Vector3.zero)
        {
            if (!freezeRotation)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
            if (!isBraking && (wallRun == null || !wallRun.IsSlideRotationFrozen)) // Freeze slide rotation during wall jump arc
            {
                // Gradually align slide velocity with input direction
                Vector3 targetVelocity = moveDir.normalized * slideVelocity.magnitude;
                slideVelocity = Vector3.RotateTowards(slideVelocity, targetVelocity, 
                    flatGroundTurnSpeed * Time.fixedDeltaTime, 0f);
            }
        }
    }

    private void ApplyRotationDuringAttack()
    {
        if (lockOnTarget != null)
        {
            Vector3 directionToTarget = lockOnTarget.transform.position - transform.position;
            // We only want to rotate on the Y axis
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, attackRotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private void ApplySliding()
    {
        angleBasedInputReduction = 0f; // Reset input reduction each frame, will be recalculated if still on slope
        if (OnSlope() && isCrouching && IsGrounded && !exitingSlope)
        {
            // Calculate slope angle
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            
            // Linearly-scaling acceleration with cap
            // At 10°: 2.5, at 20°: 5, at 40°+: 10
            float angleBasedAcceleration = Mathf.Clamp(slopeAngle * (maxSlopeSlideAcceleration / maxSlopeAngleForSlide), 1f, maxSlopeSlideAcceleration);
            angleBasedInputReduction = angleBasedAcceleration;
                    
            // Calculate max speed based on angle (steeper = faster)
            // At 10°: 5, at 20°: 10, at 40°+: 20
            float angleBasedMaxSpeed = Mathf.Clamp(slopeAngle * (maxSlopeSlideSpeed / maxSlopeAngleForSlide), 1f, maxSlopeSlideSpeed);
            
            // Calculate slide direction (down the slope)
            Vector3 desiredSlideDir = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            desiredSlideDir.y = 0; // Keep horizontal
            
            // Accelerate toward angle-based max speed
            slideVelocity = Vector3.MoveTowards(slideVelocity, 
                desiredSlideDir * angleBasedMaxSpeed, 
                angleBasedAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            if (slideVelocity.magnitude > 0 && IsGrounded)
            {
                // Get current move direction and slide direction
                Vector3 currentMoveDir = GetCameraRelativeDirection(moveInput);
                Vector3 slideDir = slideVelocity.normalized;
                
                // Check if input is roughly opposite to slide direction
                float dot = Vector3.Dot(currentMoveDir, slideDir);
                
                // If flicking opposite direction (dot < -brakeInputThreshold)
                if (moveInput.magnitude > 0.5f && dot < -brakeInputThreshold && (slideVelocity.magnitude > (moveSpeed * 0.5f) || isBraking))
                {
                    isBraking = true;
                    brakeMultiplier = 0f;
                    
                    // Apply brake deceleration
                    slideVelocity = Vector3.MoveTowards(slideVelocity, Vector3.zero, brakeDeceleration * Time.fixedDeltaTime);
                    
                    // Optional: Play brake effect (particle, sound)
                    Debug.Log("BRAKE!");
                }
                else
                {
                    isBraking = false;
                }
            }
            else
            {
                isBraking = false;
            }
            
            // If not braking, apply normal slide friction
            if (!isBraking)
            {
                // Apply friction to slow down over time
                if (IsGrounded)
                {
                    // Reduce friction when crouching on flat ground
                    float currentFriction = isCrouching ? slideFriction : friction;
                    slideVelocity = Vector3.MoveTowards(slideVelocity, Vector3.zero, currentFriction * Time.fixedDeltaTime);
                }
                else
                {
                    // Increase rate of speed loss when grabbing a ledge
                    float currentFriction = isGrabbingLedge ? friction : airFriction;
                    slideVelocity = Vector3.MoveTowards(slideVelocity, Vector3.zero, currentFriction * Time.fixedDeltaTime);
                }
            }
        }
    }
    #endregion

    #region Jump Logic
    private void HandleJumpPhysics()
    {
        // 1. Initial Jump Impulse
        if (jumpRequested)
        {
            canCoyote = Time.time - lastGroundedTime <= coyoteTime;
            if (IsGrounded || canCoyote || (enableDoubleJump && availableJumps > 0))
            {
                ExecuteJump();
            }
            jumpRequested = false;
        }

        // 2. Variable Jump Height (The "Jump Cancel")
        // If we let go of the button while still moving up fast, cap the velocity
        // This is ONLY for player-initiated jumps, not for jump impulses from attacks

        if (jumpCanceled)
        {
            if (rb.linearVelocity.y > shortJumpForce)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, shortJumpForce, rb.linearVelocity.z);
            }
            jumpCanceled = false;
        }
    }

    private void ExecuteJump()
    {
        exitingSlope = true;
        canCoyote = false;
        brakeMultiplier = 1f; // Reset brake multiplier when jumping
        if (attack && attack.isInCooldown && !isCrouching && !attack.windingUpSlam) attack.ResetCombo();

        // --- DASH JUMP MOMENTUM TRANSFER ---
        // If we jump during a dash or the 0.1s leniency window after a dash
        if (WasRecentlyDashing(0.1f))
        {
            // 1. Determine the direction of the jump
            // We use dashDirection if it exists, otherwise fall back to player forward
            Vector3 jumpDir = (dashDirection != Vector3.zero) ? dashDirection : transform.forward;

            // 2. CAPPING LOGIC: If slide velocity is less than dash speed, floor it at dash speed
            if (slideVelocity.magnitude < dashSpeed)
            {
                slideVelocity = jumpDir * dashSpeed;
            }

            // Stop the dash state so the normal jump physics take over horizontally
            isDashing = false;
            if (!dashCancelled)
            {
                dashCancelTime = Time.time;
                dashCancelled = true;
            }
        }

        if (isBraking)
        {
            // Get the direction the player is holding (or forward if no input)
            Vector3 motionRedirect = GetCameraRelativeDirection(moveInput);
            if (motionRedirect.magnitude < 0.1f)
            {
                motionRedirect = transform.forward;
            }
            else
            {
                motionRedirect = motionRedirect.normalized;
            }
            
            // REDIRECT SLIDE VELOCITY to the new direction
            SetSlideVelocity(motionRedirect * slideVelocity.magnitude);
        }

        bool isAirJump = !IsGrounded && Time.time - lastGroundedTime > coyoteTime;

        if (isAirJump)
            availableJumps--;

        if (!InterimAudioDirector.TryPlayPlayerJump(transform.position, isAirJump) && jumpSound)
        {
            AudioSource.PlayClipAtPoint(jumpSound, transform.position);
        }
        
        float force = (IsGrounded || Time.time - lastGroundedTime <= coyoteTime) ? jumpForce : doubleJumpForce;
        
        // Apply crouch multiplier
        if (highJumpRequested)
        {
            force *= highJumpMultiplier;
        }
        else if (bufferedJumpWasReleased)
        {
            force = shortJumpForce;
            bufferedJumpWasReleased = false;
        }
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        Invoke(nameof(ResetExitSlope), 0.1f);
    }

    private void ResetExitSlope()
    {
        exitingSlope = false;
    }
    #endregion

    #region Dash Logic
    private void StartDash()
    {
        StopAttacking();
        if (attack)
        {
            attack.StopHitbox();
            attack.windingUpSlam = false; // Cancel ground slam windup if we dash early
            if (attack.isInCooldown)
            {
                attack.ResetCombo();
            }
            else if (attack.cooldownTimer > 0)
            {
                attack.cooldownTimer = attack.finisherCooldownTime;
            }
            if (attack.groundSlamLandingCoroutine != null)
            {
                StopCoroutine(attack.groundSlamLandingCoroutine);
                attack.groundSlamLandingCoroutine = null;
            }
        }
        freezeRotation = false;
        if (lockOnTarget != null)
        {
            UnlockTarget();
        }

        if (!IsGrounded)
        {
            availableDashes--;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, slowFallSpeed, shortJumpForce), rb.linearVelocity.z);
        }
        
        dashCancelled = false;
        isDashing = true;
        pauseFastFall = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        brakeMultiplier = 1f; // Reset brake multiplier when starting a dash

        Vector3 moveDir = GetCameraRelativeDirection(moveInput);
        dashDirection = (moveDir != Vector3.zero) ? moveDir.normalized : transform.forward;

        if (isBraking)
        {
            SetSlideVelocity(Vector3.zero);
        }
        else // Redirect slide direction
        {
            SetSlideVelocity(slideVelocity.magnitude * dashDirection);
        }

        if (!InterimAudioDirector.TryPlayPlayerDash(transform.position) && dashSound)
        {
            AudioSource.PlayClipAtPoint(dashSound, transform.position);
        }
    }

    private void ApplyDashMovement()
    {
        // Dash overrides INPUT movement, but preserves slide velocity
        float appliedDashSpeed = dashSpeed + Mathf.Max(0, targetVelocity.magnitude - moveSpeed);
        Vector3 dashVelocity = dashDirection * appliedDashSpeed;
        
        // Preserve vertical velocity
        dashVelocity.y = rb.linearVelocity.y;
        
        // Handle slope projection
        if (OnSlope() && !exitingSlope)
        {
            dashVelocity = Vector3.ProjectOnPlane(dashVelocity, slopeHit.normal);
            rb.AddForce(-slopeHit.normal * slopeDownForce, ForceMode.Force);
        }

        // Apply combined velocity
        rb.linearVelocity = dashVelocity;

        // Rotate towards dash direction
        Quaternion dashRot = Quaternion.LookRotation(dashDirection);
        rb.rotation = Quaternion.Slerp(rb.rotation, dashRot, attackRotationSpeed * Time.fixedDeltaTime);

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0) 
        {
            isDashing = false;
            pauseFastFall = false;
            dashEndTime = Time.time; // Record the exact finish time
        }
    }

    private void ApplyAttackMovement()
    {
        float appliedAttackForce = attackForce + Mathf.Max(0, targetVelocity.magnitude - moveSpeed);
        Vector3 attackVelocity = attackDirection * appliedAttackForce;

        if (!attackShouldModifyYaxis)
        {
            attackVelocity.y = rb.linearVelocity.y;
        }
        
        // Apply combined velocity
        rb.linearVelocity = attackVelocity;
    }
    #endregion
    
    #region Ledge Grab Logic
    private bool CheckForLedge()
    {
        if (isGrabbingLedge || isDashing || isAttacking) return false;
        if (IsGrounded) return false;
        if (rb.linearVelocity.y > 0) return false; // Only check when falling
        if (attack != null && attack.currentAttackType != Attack.AttackType.None)
        {
            return false; // Don't allow ledge grab during an attack
        }

        // 1. Cast VERTICAL ray in front of the player to detect the top of a ledge
        Vector3 rayOrigin = transform.position + transform.forward * ledgeCheckForwardDistance + Vector3.up * ledgeGrabHeight;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit verticalHit, ledgeCheckDownDistance, ledgeLayer))
        {
            // Found something below - this is the top of a ledge
            Vector3 ledgeTop = verticalHit.point;
            
            // 2. Cast HORIZONTAL ray from the player slightly below the ledge top to detect the wall
            Vector3 wallCheckOrigin = transform.position;
            wallCheckOrigin.y = ledgeTop.y - 0.01f;
            
            if (Physics.Raycast(wallCheckOrigin, transform.forward, out RaycastHit wallHit, 0.5f, ledgeLayer))
            {
                // Found the wall face
                ledgeGrabPoint = ledgeTop;
                ledgeNormal = wallHit.normal;
                return true;
            }
        }
        return false;
    }

    private void GrabLedge()
    {
        isGrabbingLedge = true;
        if (attack) attack.StopHitbox();
        
        // Stop all movement
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        
        // Position player at ledge (grab point)
        Vector3 grabPosition = ledgeGrabPoint + (ledgeNormal * 0.3f);
        grabPosition.y = ledgeGrabPoint.y - 0.9f; // Hands at ledge level
        transform.position = grabPosition;
        
        // Face the wall (opposite of where the wall is facing)
        rb.rotation = Quaternion.LookRotation(-ledgeNormal);

        // Reset aerial actions
        availableJumps = 1;
        availableAerialPushes = 1;
        availableChargeAttackJumps = 1;
        if (dashUpgradeObtained)
        {
            availableDashes = 2;
        }
        else
        {
            availableDashes = 1;
        }
        
        if (animator) animator.SetBool("IsGrabbingLedge", true);
    }
    
    private void ClimbUpLedge(float climbUpForce)
    {
        ReleaseLedge();
        rb.linearVelocity = new Vector3(0, climbUpForce, 0); // Launch upward
        
        if (animator) animator.SetTrigger("Jump");
    }

    private void HandleLedgeGrab()
    {
        // Jump to climb up
        if (jumpRequested)
        {
            ClimbUpLedge(jumpForce);
            jumpRequested = false;
        }
        
        // Move away from ledge to release
        Vector3 moveDir = GetCameraRelativeDirection(moveInput);
        float dot = Vector3.Dot(moveDir, transform.forward);

        // If dot is negative, the player is pushing the stick AWAY from the wall
        if (dot < -0.5f)
        {
            ReleaseLedge();
        }
        else if (dot > 0.5f) // If dot is positive, the player is pushing TOWARDS the wall
        {
            ClimbUpLedge(doubleJumpForce);
        }
    }

    public void ReleaseLedge()
    {
        // This can be called from external scripts (like Attack) to force the player to let go of the ledge
        ledgeGrabCooldownTimer = ledgeGrabCooldown; // Start the cooldown so we don't immediately re-grab
        isGrabbingLedge = false;
        rb.useGravity = true;
        if (animator) animator.SetBool("IsGrabbingLedge", false);
    }
    #endregion

    #region Helpers
    private void HandleTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (dashBuffered && dashCooldownTimer <= 0)
        {
            StartDash();
            dashBuffered = false;
        }
        
        if (jumpBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0)
            {
                bufferedJumpWasReleased = false;
                jumpBuffered = false;
            }
            else if (IsGrounded)
            {
                jumpRequested = true;
                pauseFastFall = false;
                jumpBuffered = false;
            }
        }

        if (landedFromGroundSlam)
        {
            slamJumpTimer -= Time.deltaTime;
            if (slamJumpTimer <= 0)
            {
                landedFromGroundSlam = false;
            }
        }

        if (ledgeGrabCooldownTimer > 0) ledgeGrabCooldownTimer -= Time.deltaTime;

        // Only check for a ledge if the cooldown is over
        if (!isGrabbingLedge && ledgeGrabCooldownTimer <= 0 && CheckForLedge() && !attack.windingUpSlam)
        {
            GrabLedge();
        }
    }

    private void HandleGroundedState()
    {
        bool currentlyGrounded = IsGrounded;

        if (!currentlyGrounded && rb != null)
        {
            lastUngroundedVerticalSpeed = rb.linearVelocity.y;
        }

        if (currentlyGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wasGrounded)
            {
                bool heavyLanding = lastUngroundedVerticalSpeed < -8f || landedFromGroundSlam;
                InterimAudioDirector.TryPlayPlayerLand(transform.position, heavyLanding);

                availableJumps = 1;
                availableAerialPushes = 1;
                availableChargeAttackJumps = 1;
                if (dashUpgradeObtained)
                {
                    availableDashes = 2;
                }
                else
                {
                    availableDashes = 1;
                }
            }
        }
        wasGrounded = currentlyGrounded;
    }

    private void ReportMovementAudio(bool canPlayMovement)
    {
        if (rb == null)
        {
            return;
        }

        bool grounded = IsGrounded;
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        float horizontalSpeed = horizontalVelocity.magnitude;
        bool moving = canPlayMovement &&
                      grounded &&
                      (moveInput.magnitude > 0.1f || horizontalSpeed > 0.25f);
        bool running = !isCrouching && moveInput.magnitude > 0.65f && horizontalSpeed >= moveSpeed * 0.75f;
        float speedRatio = horizontalSpeed / Mathf.Max(moveSpeed, 0.01f);

        InterimAudioDirector.ReportPlayerMovement(
            transform.position,
            moving,
            isCrouching && grounded,
            running,
            speedRatio
        );
    }

    public Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0; right.y = 0;
        return (forward.normalized * input.y + right.normalized * input.x);
    }

    private void UpdateAnimation()
    {
        if (!animator) return;
        animator.SetFloat("Speed", moveInput.magnitude * moveSpeed, 0.1f, Time.fixedDeltaTime);
        animator.SetBool("IsFalling", !IsGrounded && rb.linearVelocity.y < 0);
        animator.SetBool("IsCrouching", isCrouching && IsGrounded);
    }
    #endregion
}
