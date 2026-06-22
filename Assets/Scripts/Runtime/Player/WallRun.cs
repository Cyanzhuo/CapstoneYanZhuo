using UnityEngine;

public class WallRun : MonoBehaviour
{
    bool isWallRunning = false;
    float wallRunSpeed = 7.5f;

    [Header("Wall Jump Tuning")]
    [HideInInspector] public bool IsInWallJumpArc = false;
    [HideInInspector] public bool IsSlideRotationFrozen = false;
    [HideInInspector] public Vector3 lastWallNormal;
    [HideInInspector] public float wallJumpArcTimer = 0f;
    [HideInInspector] public float slideRotationFreezeTimer = 0f;
    [SerializeField] public float wallJumpArcDuration = 0.6f; // How long input is restricted
    [SerializeField] public float slideRotationFreezeDuration = 0.3f; // How long slide rotation is frozen

    ThirdPersonController playerController;
    Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponentInParent<ThirdPersonController>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update wall jump arc timer
        if (IsInWallJumpArc)
        {
            wallJumpArcTimer -= Time.deltaTime;
            if (wallJumpArcTimer <= 0)
            {
                IsInWallJumpArc = false;
            }
        }
        
        // Update slide rotation freeze timer (shorter)
        if (IsSlideRotationFrozen)
        {
            slideRotationFreezeTimer -= Time.deltaTime;
            if (slideRotationFreezeTimer <= 0)
            {
                IsSlideRotationFrozen = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (IsInWallJumpArc)
        {
            Vector3 worldMoveDir = playerController.GetCameraRelativeDirection(playerController.moveInput);
            // Check if the player is trying to move INTO the wall normal
            float pushDirection = Vector3.Dot(worldMoveDir, lastWallNormal);
            
            if (pushDirection < 0)
            {
                // Zero out only the part of the input pointing into the wall
                // This maintains movement "Along" or "Away" from the wall
                Vector3 correctedDir = worldMoveDir - (lastWallNormal * pushDirection);
                playerController.targetVelocity = correctedDir * playerController.moveSpeed;
            }
            else
            {
                playerController.targetVelocity = worldMoveDir * playerController.moveSpeed;
            }
        }
        else if (isWallRunning && !playerController.IsGrounded)
        {
            // Apply wall running movement
            Vector3 wallDirection = Vector3.Cross(lastWallNormal, Vector3.up).normalized;
            playerController.targetVelocity = wallDirection * wallRunSpeed;
        }
    }

    void StartWallRun(Vector3 collisionNormal)
    {
        // Check if the player is moving towards the wall
        Vector3 moveDir = playerController.GetCameraRelativeDirection(playerController.moveInput);
        Vector3 wallRunDir = (moveDir != Vector3.zero) ? moveDir : transform.forward;
        if (Vector3.Dot(wallRunDir, collisionNormal) < 0)
        {
            // Start wall running by setting the player's velocity along the wall
            Vector3 wallDirection = Vector3.Cross(collisionNormal, Vector3.up).normalized;
            playerController.targetVelocity = wallDirection * wallRunSpeed;
            isWallRunning = true; // Set a flag to indicate wall running state
        }
    }

    void WallJump()
    {
        EndWallRun();

        // Set wall jump arc state
        IsInWallJumpArc = true;
        wallJumpArcTimer = wallJumpArcDuration;
        
        IsSlideRotationFrozen = true;
        slideRotationFreezeTimer = slideRotationFreezeDuration;
        
        // Existing bounce code...
        Vector3 bounceDir = lastWallNormal;
        bounceDir.y = 1;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(bounceDir * playerController.doubleJumpForce, ForceMode.Impulse);
        
        // Horizontal momentum to slide
        Vector3 horizontalDir = new Vector3(bounceDir.x, 0, bounceDir.z).normalized;
        float bounceSpeed = Mathf.Max(playerController.slideVelocity.magnitude, playerController.doubleJumpForce);
        playerController.SetSlideVelocity(horizontalDir * bounceSpeed);
    }

    void EndWallRun()
    {
        isWallRunning = false; // Reset wall running state
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Untagged") && !isWallRunning && !playerController.IsGrounded && playerController.isDashing)
        {
            ContactPoint contact = collision.contacts[0];
            lastWallNormal = contact.normal;
            StartWallRun(contact.normal);
        }
    }
}
