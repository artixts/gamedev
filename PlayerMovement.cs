
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Sliding")]
    [SerializeField] private float slideSpeed = 8f;
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private float slideCooldown = 0.5f;

    [Header("Jump Settings")]
    [SerializeField] private float firstJumpHeight = 4f;
    [SerializeField] private float doubleJumpHeight = 6f;
    [SerializeField] private int maxJumps = 2;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.9f;
    [SerializeField] private float wallJumpUpForce = 9f;
    [SerializeField] private float wallJumpAwayForce = 7f;
    [SerializeField] private float wallJumpCooldown = 0.25f;
    [SerializeField] private LayerMask wallLayers = ~0;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioSource runningAudio;
    [SerializeField] private AudioSource slidingAudio;

    [Header("Sprint Particle Effect")]
    [SerializeField] private ParticleSystem sprintParticles;

    [Header("Sprint Start Sound")]
    [SerializeField] private AudioSource sprintStartAudio;
    [SerializeField] private AudioClip sprintStartClip;
    [SerializeField, Range(0f, 1f)]
    private float sprintStartVolume = 0.7f;

    private CharacterController controller;

    private Vector3 verticalVelocity;
    private Vector3 slideDirection;
    private Vector3 wallNormal;

    private int jumpsRemaining;

    private float slideTimer;
    private float slideCooldownTimer;
    private float wallJumpCooldownTimer;

    private bool isSliding;
    private bool isNearWall;
    private bool wasSprinting;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int IsSlidingHash =
        Animator.StringToHash("IsSliding");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("PlayerMovement: No Animator found.", this);

        jumpsRemaining = maxJumps;

        ConfigureAudio(runningAudio, true);
        ConfigureAudio(slidingAudio, false);
        ConfigureAudio(sprintStartAudio, false);

        if (sprintParticles != null)
        {
            sprintParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void ConfigureAudio(AudioSource source, bool loop)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = loop;
        source.Stop();
    }

    private void Update()
    {
        if (animator == null)
            return;

        slideCooldownTimer -= Time.deltaTime;
        wallJumpCooldownTimer -= Time.deltaTime;

        CheckForWall();

        HandleMovement();

        bool wallJumped = TryWallJump();

        if (!wallJumped)
            HandleJump();

        HandleGravity();
        HandleSlide();

        UpdateAnimator();
        UpdateAudio();
        UpdateSprintParticles();
        UpdateSprintStartSound();
    }

    private Vector3 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical);

        return Vector3.ClampMagnitude(input, 1f);
    }

    private bool IsSprintKeyPressed()
    {
        return Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.RightShift);
    }

    // MOVEMENT

    private void HandleMovement()
    {
        if (isSliding)
        {
            controller.Move(
                slideDirection * slideSpeed * Time.deltaTime
            );

            return;
        }

        Vector3 input = GetMovementInput();

        float currentSpeed =
            IsSprintKeyPressed() ? sprintSpeed : walkSpeed;

        Vector3 movement =
            transform.TransformDirection(input) * currentSpeed;

        // Prevent normal movement from pushing into a detected wall.
        if (isNearWall && !controller.isGrounded)
        {
            if (Vector3.Dot(movement, -wallNormal) > 0f)
                movement = Vector3.ProjectOnPlane(movement, wallNormal);
        }

        controller.Move(movement * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f && !isNearWall)
        {
            Vector3 facingDirection =
                transform.TransformDirection(input);

            Quaternion targetRotation =
                Quaternion.LookRotation(facingDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // NORMAL JUMP AND DOUBLE JUMP

    private void HandleJump()
    {
        if (controller.isGrounded && verticalVelocity.y <= 0f)
        {
            jumpsRemaining = maxJumps;

            animator.SetBool("IsJumping", false);
            animator.SetBool("IsDoubleJump", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) &&
            jumpsRemaining > 0 &&
            !isSliding &&
            !isNearWall)
        {
            bool isSecondJump = jumpsRemaining == 1;

            float selectedJumpHeight = isSecondJump
                ? doubleJumpHeight
                : firstJumpHeight;

            verticalVelocity.y = Mathf.Sqrt(
                selectedJumpHeight * -2f * gravity
            );

            jumpsRemaining--;

            animator.SetBool("IsJumping", true);
            animator.SetBool("IsDoubleJump", isSecondJump);

            animator.Play("Jump", 0, 0f);
        }
    }

    // WALL DETECTION

  private void CheckForWall()
{
    isNearWall = false;
    wallNormal = Vector3.zero;

    // Wall jump is only available while airborne.
    if (controller.isGrounded)
        return;

    Vector3 origin = controller.bounds.center;

    Vector3[] directions =
    {
        transform.forward,
        -transform.forward,
        transform.right,
        -transform.right
    };

    foreach (Vector3 direction in directions)
    {
        if (Physics.SphereCast(
            origin,
            0.25f,
            direction,
            out RaycastHit hit,
            wallCheckDistance,
            wallLayers,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
                continue;

            // Ignore floors and ceilings.
            if (Mathf.Abs(hit.normal.y) < 0.3f)
            {
                isNearWall = true;
                wallNormal = hit.normal;

                Debug.Log("Wall detected: " + hit.transform.name);

                return;
            }
        }
    }
}

    // WALL JUMP

private bool TryWallJump()
{
    if (!Input.GetKeyDown(KeyCode.Space))
        return false;

    Debug.Log(
        $"Wall Jump Check | Near Wall: {isNearWall} | " +
        $"Grounded: {controller.isGrounded} | " +
        $"Sliding: {isSliding} | " +
        $"Cooldown: {wallJumpCooldownTimer:F2}"
    );

    // Allow wall jumping when a wall is detected.
    if (!isNearWall || isSliding || wallJumpCooldownTimer > 0f)
        return false;

    // Apply upward force.
    verticalVelocity.y = wallJumpUpForce;

    // Apply force away from the wall.
    Vector3 pushDirection = wallNormal;
    pushDirection.y = 0f;

    if (pushDirection.sqrMagnitude > 0.01f)
    {
        pushDirection.Normalize();

        controller.Move(
            pushDirection * wallJumpAwayForce * Time.deltaTime
        );
    }

    jumpsRemaining = maxJumps;
    wallJumpCooldownTimer = wallJumpCooldown;

    if (animator != null)
    {
        animator.SetBool("IsJumping", true);
        animator.SetBool("IsDoubleJump", false);
        animator.Play("Jump", 0, 0f);
    }

    Debug.Log("WALL JUMP SUCCESSFUL!");

    return true;
}


    // GRAVITY

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // SLIDING

    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) &&
            !isSliding &&
            controller.isGrounded &&
            slideCooldownTimer <= 0f)
        {
            Vector3 input = GetMovementInput();

            slideDirection = input.sqrMagnitude > 0.01f
                ? transform.TransformDirection(input).normalized
                : transform.forward;

            isSliding = true;
            slideTimer = slideDuration;

            animator.SetBool(IsSlidingHash, true);
        }

        if (!isSliding)
            return;

        slideTimer -= Time.deltaTime;

        if (slideTimer <= 0f)
        {
            isSliding = false;
            slideCooldownTimer = slideCooldown;

            animator.SetBool(IsSlidingHash, false);
        }
    }

    // ANIMATIONS

    private void UpdateAnimator()
    {
        Vector3 input = GetMovementInput();

        float animationSpeed =
            input.magnitude * (IsSprintKeyPressed() ? 2f : 1f);

        animator.SetFloat(
            SpeedHash,
            isSliding ? 0f : animationSpeed,
            0.1f,
            Time.deltaTime
        );

        animator.SetBool(IsSlidingHash, isSliding);
    }

    // RUNNING AND SLIDING AUDIO

    private void UpdateAudio()
    {
        Vector3 input = GetMovementInput();

        bool moving = input.sqrMagnitude > 0.01f;
        bool shouldPlayRunning = moving && !isSliding;

        if (runningAudio != null)
        {
            if (shouldPlayRunning && !runningAudio.isPlaying)
                runningAudio.Play();
            else if (!shouldPlayRunning && runningAudio.isPlaying)
                runningAudio.Stop();
        }

        if (slidingAudio != null)
        {
            if (isSliding && !slidingAudio.isPlaying)
                slidingAudio.Play();
            else if (!isSliding && slidingAudio.isPlaying)
                slidingAudio.Stop();
        }
    }

    // SPRINT PARTICLES

    private void UpdateSprintParticles()
    {
        if (sprintParticles == null)
            return;

        Vector3 input = GetMovementInput();

        bool shouldPlay =
            IsSprintKeyPressed() &&
            input.sqrMagnitude > 0.01f &&
            controller.isGrounded &&
            !isSliding;

        if (shouldPlay && !sprintParticles.isPlaying)
        {
            sprintParticles.Play();
        }
        else if (!shouldPlay && sprintParticles.isPlaying)
        {
            sprintParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }
    }

    // SPRINT START SOUND

    private void UpdateSprintStartSound()
    {
        Vector3 input = GetMovementInput();

        bool isSprinting =
            IsSprintKeyPressed() &&
            input.sqrMagnitude > 0.01f &&
            controller.isGrounded &&
            !isSliding;

        if (isSprinting && !wasSprinting)
        {
            if (sprintStartAudio != null &&
                sprintStartClip != null)
            {
                sprintStartAudio.PlayOneShot(
                    sprintStartClip,
                    sprintStartVolume
                );
            }
        }

        wasSprinting = isSprinting;
    }

    // PORTAL TELEPORT SUPPORT

    public void ResetVelocityAfterTeleport()
    {
        verticalVelocity = Vector3.zero;

        isSliding = false;
        slideTimer = 0f;
        slideCooldownTimer = 0f;

        jumpsRemaining = maxJumps;
        wasSprinting = false;

        if (animator != null)
        {
            animator.SetBool(IsSlidingHash, false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsDoubleJump", false);
        }

        if (slidingAudio != null)
            slidingAudio.Stop();

        if (sprintParticles != null)
        {
            sprintParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void OnDisable()
    {
        if (runningAudio != null)
            runningAudio.Stop();

        if (slidingAudio != null)
            slidingAudio.Stop();

        if (sprintStartAudio != null)
            sprintStartAudio.Stop();

        if (sprintParticles != null)
        {
            sprintParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
}
