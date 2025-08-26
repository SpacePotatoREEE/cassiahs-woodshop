using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float runSpeed  = 12f;
    [SerializeField] private float acceleration  = 10f;
    [SerializeField] private float deceleration  = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float walkToRunThreshold = 0.5f;

    [Header("Slope Settings")]
    [SerializeField, Range(0f, 60f)] private float maxSlopeAngle = 45f;
    [SerializeField] private float slideSpeed    = 5f;
    [SerializeField] private float slideFriction = 0.3f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpInitialVelocity = 12f;
    [SerializeField] private float jumpHangTimeGravityMultiplier = 0.5f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float normalGravity = 20f;
    [SerializeField] private float jumpHangVelocityThreshold = 2f;
    [SerializeField] private float groundedGravity = 2f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask terrainLayer = ~0;

    [Header("Animation Settings")]
    [SerializeField] private Animator animatorOverride;
    [SerializeField] private float animationSpeedSmoothTime = 0.1f;
    [SerializeField] private float minAnimationSpeed = 0.01f;
    [SerializeField] private bool  useRootMotion = false;

    [Tooltip("If ON, Speed = input magnitude (0..1). If OFF, Speed = planar velocity / runSpeed.")]
    [SerializeField] private bool useInputForSpeedParam = true;

    [Header("Animator Parameters (names)")]
    [SerializeField] private string speedParamName    = "Speed";    // float
    [SerializeField] private string yVelParamName     = "YVel";     // float
    [SerializeField] private string groundedParamName = "Grounded"; // bool
    [SerializeField] private string jumpTriggerName   = "Jump";     // trigger

    [Header("Jump Animation Guard")]
    [SerializeField] private float postJumpGroundLock = 0.08f;

    [Header("Air Control")]
    [Range(0f, 1f)] public float airControlPercent = 0.3f;

    [Header("Jump Grace Windows")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    // Components
    private CharacterController controller;
    private Camera   mainCamera;
    private Animator animator;

    // Movement state
    private Vector3 moveVelocity;     // XZ
    private Vector3 verticalVelocity; // Y
    private Vector3 slideVelocity;    // XZ slide on steep slopes
    private Vector2 inputVector;
    private bool    jumpPressed;

    // Ground state
    private bool   isGrounded;
    private bool   wasGrounded;
    private Vector3 groundNormal = Vector3.up;
    private float  currentSlopeAngle;
    private bool   isOnSlope;
    private bool   isSlidingOnSlope;

    // Input System
    private PlayerInput playerInput;
    private InputAction moveAction, jumpAction;

    // Animator hashes
    private int speedHash, yVelHash, groundedHash, jumpTriggerHash;

    // Timers
    private float groundLockTimer, coyoteTimer, jumpBufferTimer;

    // ───────── Air-control override (e.g., Glide) ─────────
    private bool  hasAirControlOverride = false;
    private float airControlOverridePercent = 0f;
    public void SetAirControlOverride(float percent) { hasAirControlOverride = true; airControlOverridePercent = Mathf.Clamp01(percent); }
    public void ClearAirControlOverride()           { hasAirControlOverride = false; }

    // ───────── Air-drive override (ability pushes planar velocity) ─────────
    private bool   airDriveActive = false;
    private Vector3 airDriveTargetVel; // world XZ
    private float  airDriveAccel;
    public void SetAirDrive(Vector3 worldXZVelocity, float accel)
    {
        airDriveActive    = true;
        airDriveTargetVel = new Vector3(worldXZVelocity.x, 0f, worldXZVelocity.z);
        airDriveAccel     = Mathf.Max(0.01f, accel);
    }
    public void ClearAirDrive() => airDriveActive = false;

    // ───────── PUBLIC HOOKS for abilities & readouts ─────────
    public bool  IsGrounded => isGrounded;
    public float JumpInitialVelocity => jumpInitialVelocity;
    public float VerticalVelocityY { get => verticalVelocity.y; set => verticalVelocity.y = value; }
    /// True if a base (first) jump would be accepted right now (ground/coyote)
    public bool IsBaseJumpEligible => ((isGrounded && groundLockTimer <= 0f) || coyoteTimer > 0f) && !isSlidingOnSlope;
    /// Set when this controller consumed a jump this frame
    public bool JumpConsumedThisFrame { get; private set; }
    /// Current world-space planar velocity (XZ) used this frame (move + slide)
    public Vector3 CurrentPlanarVelocityXZ => new Vector3(moveVelocity.x + slideVelocity.x, 0f, moveVelocity.z + slideVelocity.z);

    public void TriggerJumpFX() { if (animator) animator.SetTrigger(jumpTriggerHash); }
    public void LockGroundFor(float seconds) { groundLockTimer = Mathf.Max(groundLockTimer, seconds); }
    public void ForceJump(float initialVelocityY)
    {
        verticalVelocity.y = initialVelocityY;
        isGrounded = false;
        TriggerJumpFX();
        LockGroundFor(postJumpGroundLock);
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        jumpPressed = false;
        JumpConsumedThisFrame = true;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        animator = animatorOverride ? animatorOverride : GetComponentInChildren<Animator>(true);
        speedHash = Animator.StringToHash(speedParamName);
        yVelHash  = Animator.StringToHash(yVelParamName);
        groundedHash = Animator.StringToHash(groundedParamName);
        jumpTriggerHash = Animator.StringToHash(jumpTriggerName);

        playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput && playerInput.actions)
        {
            moveAction = playerInput.actions.FindAction("Move",  true);
            jumpAction = playerInput.actions.FindAction("Jump",  true);
            moveAction?.Enable(); jumpAction?.Enable();
        }

        if (terrainLayer == 0) terrainLayer = LayerMask.GetMask("Default");
    }

    void Update()
    {
        HandleInput();
        CheckGround();
        HandleMovement();
        HandleJump();
        HandleSlope();
        ApplyGravity();

        if (!useRootMotion)
            controller.Move((moveVelocity + verticalVelocity + slideVelocity) * Time.deltaTime);

        UpdateAnimator();
        wasGrounded = isGrounded;
    }

    void LateUpdate() => JumpConsumedThisFrame = false;

    void OnAnimatorMove()
    {
        if (!useRootMotion || !animator) return;
        var rm = animator.deltaPosition;
        rm.y = (verticalVelocity.y + slideVelocity.y) * Time.deltaTime;
        controller.Move(rm);
    }

    private void HandleInput()
    {
        inputVector = moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (inputVector.sqrMagnitude > 1f) inputVector.Normalize();

        bool pressed = (jumpAction != null && jumpAction.WasPressedThisFrame())
                       || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                       || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
        if (pressed) { jumpPressed = true; jumpBufferTimer = jumpBufferTime; }
    }

    private void CheckGround()
    {
        if (groundLockTimer > 0f) groundLockTimer -= Time.deltaTime;

        bool ccGrounded = controller.isGrounded;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, controller.height/2f + groundCheckDistance, terrainLayer))
        {
            groundNormal = hit.normal;
            currentSlopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            isOnSlope        = currentSlopeAngle > 0.1f && currentSlopeAngle < 90f;
            isSlidingOnSlope = ccGrounded && currentSlopeAngle > maxSlopeAngle;
            if (!ccGrounded && hit.distance <= controller.height/2f + groundCheckDistance) ccGrounded = true;
        }
        else { groundNormal = Vector3.up; currentSlopeAngle = 0f; isOnSlope = isSlidingOnSlope = false; }

        bool was = isGrounded;
        isGrounded = (groundLockTimer <= 0f) && ccGrounded;

        if (was && !isGrounded) coyoteTimer = coyoteTime;
        else if (!isGrounded && coyoteTimer > 0f) { coyoteTimer -= Time.deltaTime; if (coyoteTimer < 0f) coyoteTimer = 0f; }
    }

    private void HandleMovement()
    {
        if (isSlidingOnSlope) return;

        // camera-relative desiredDir
        Vector3 camF = mainCamera ? mainCamera.transform.forward : Vector3.forward;
        Vector3 camR = mainCamera ? mainCamera.transform.right   : Vector3.right;
        camF.y = camR.y = 0f; camF.Normalize(); camR.Normalize();
        Vector3 desiredDir = camR * inputVector.x + camF * inputVector.y;
        if (isOnSlope && isGrounded) desiredDir = Vector3.ProjectOnPlane(desiredDir, groundNormal);

        float inputMag  = inputVector.magnitude;
        float baseSpeed = (inputMag > walkToRunThreshold) ? runSpeed : walkSpeed;
        float targetSpd = baseSpeed * inputMag;

        if (isGrounded)
        {
            if (inputMag > 0.1f)
            {
                moveVelocity = Vector3.Lerp(moveVelocity, desiredDir * targetSpd, acceleration * Time.deltaTime);
                if (!useRootMotion && desiredDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir, Vector3.up), rotationSpeed * Time.deltaTime);
            }
            else moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }
        else
        {
            if (airDriveActive)
            {
                Vector3 currentFlat = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
                Vector3 newFlat = Vector3.MoveTowards(currentFlat, airDriveTargetVel, airDriveAccel * Time.deltaTime);
                moveVelocity = new Vector3(newFlat.x, moveVelocity.y, newFlat.z);
                airDriveActive = false; // re-set each frame by ability
            }
            else
            {
                float acp = hasAirControlOverride ? airControlOverridePercent : airControlPercent;

                Vector3 currentFlat = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
                Vector3 desiredFlat = desiredDir * ((inputMag > walkToRunThreshold ? runSpeed : walkSpeed) * inputMag);

                float airAccel = Mathf.Max(0.01f, acceleration * Mathf.Clamp01(acp));
                Vector3 wishDelta = desiredFlat - currentFlat;
                Vector3 delta = Vector3.ClampMagnitude(wishDelta, airAccel * Time.deltaTime);

                if (currentFlat.sqrMagnitude > 1e-6f) {
                    Vector3 dir  = currentFlat.normalized;
                    float   along = Vector3.Dot(delta, dir);
                    Vector3 perp  = delta - dir * along;
                    perp *= acp;
                    float maxDecel = currentFlat.magnitude;
                    if (along < -maxDecel) along = -maxDecel;
                    delta = dir * along + perp;
                } else delta *= acp;

                currentFlat += delta;
                moveVelocity = new Vector3(currentFlat.x, moveVelocity.y, currentFlat.z);

                if (!useRootMotion && desiredDir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir, Vector3.up), rotationSpeed * acp * Time.deltaTime);
            }
        }
    }

    private void HandleJump()
    {
        if (jumpBufferTimer > 0f) { jumpBufferTimer -= Time.deltaTime; if (jumpBufferTimer < 0f) jumpBufferTimer = 0f; }
        bool eligibleNow = IsBaseJumpEligible;
        if (jumpBufferTimer > 0f && eligibleNow) { DoJump(); return; }
        if (jumpPressed && eligibleNow)          { DoJump(); return; }
        jumpPressed = false;
    }

    private void DoJump()
    {
        verticalVelocity.y = jumpInitialVelocity;
        isGrounded = false;
        TriggerJumpFX();
        LockGroundFor(postJumpGroundLock);
        coyoteTimer = 0f; jumpBufferTimer = 0f; jumpPressed = false;
        JumpConsumedThisFrame = true;
    }

    private void HandleSlope()
    {
        if (isSlidingOnSlope)
        {
            Vector3 slideDir  = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            float   slideAccel = slideSpeed * (1f - slideFriction);
            slideVelocity = Vector3.Lerp(slideVelocity, slideDir * slideAccel, Time.deltaTime * 5f);
            moveVelocity  = Vector3.zero;
        }
        else slideVelocity = Vector3.Lerp(slideVelocity, Vector3.zero, Time.deltaTime * 10f);
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity.y <= 0f)
            verticalVelocity.y = -groundedGravity;
        else
        {
            float mul = 1f;
            if (verticalVelocity.y > 0f && Mathf.Abs(verticalVelocity.y) < jumpHangVelocityThreshold) mul = jumpHangTimeGravityMultiplier;
            else if (verticalVelocity.y < 0f) mul = fallGravityMultiplier;
            verticalVelocity.y -= normalGravity * mul * Time.deltaTime;
        }
    }

    private void UpdateAnimator()
    {
        if (!animator) return;
        float speed01;
        if (isSlidingOnSlope) speed01 = Mathf.Clamp01(slideVelocity.magnitude / runSpeed);
        else if (useInputForSpeedParam) speed01 = inputVector.magnitude;
        else {
            float planar = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;
            speed01 = Mathf.Clamp01(planar / runSpeed);
        }
        if (speed01 < minAnimationSpeed) speed01 = 0f;
        animator.SetFloat(speedHash, speed01, animationSpeedSmoothTime, Time.deltaTime);
        float yForAnim = (isGrounded && Mathf.Abs(verticalVelocity.y) <= groundedGravity) ? 0f : verticalVelocity.y;
        animator.SetFloat(yVelHash, yForAnim);
        animator.SetBool(groundedHash, isGrounded);
    }
}
