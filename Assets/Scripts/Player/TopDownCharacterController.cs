using UnityEngine;
using UnityEngine.InputSystem;

/// Top-down mover that drives an Animator (Speed, YVel, Grounded, Jump trigger)
/// and reads input from the New Input System (with safe fallbacks).
/// Uses CharacterController (Unity 6 / URP).
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
    [SerializeField] private LayerMask terrainLayer = ~0; // Everything by default

    [Header("Animation Settings")]
    [SerializeField] private Animator animatorOverride;        // drag Animator here if it lives on a child
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
    [Tooltip("Ignore grounding for this many seconds after a jump starts (prevents flicker).")]
    [SerializeField] private float postJumpGroundLock = 0.08f;

    [Header("Air Control")]
    [Tooltip("0 = no steering in air, 1 = same as on ground.\nOpposite input can only cancel current forward motion (no instant reverse).")]
    [Range(0f, 1f)] public float airControlPercent = 0.3f;

    [Header("Jump Grace Windows")]
    [Tooltip("Time AFTER leaving ground where jumping is still allowed (coyote time).")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Time AFTER pressing jump while NOT grounded during which the jump will trigger on landing (jump buffer).")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool logAnimatorValues = false;
    [SerializeField] private bool logInputRouting   = false;

    // Components
    private CharacterController controller;
    private Camera   mainCamera;
    private Animator animator;

    // Movement
    private Vector3 moveVelocity;     // horizontal intent (XZ)
    private Vector3 verticalVelocity; // Y
    private Vector3 slideVelocity;    // slope slide
    private Vector2 inputVector;
    private bool    jumpPressed;

    // Ground state
    private bool   isGrounded;
    private bool   wasGrounded;
    private Vector3 groundNormal = Vector3.up;
    private float  currentSlopeAngle;
    private bool   isOnSlope;
    private bool   isSlidingOnSlope;

    // New Input System
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    // Animator hashes
    private int speedHash, yVelHash, groundedHash, jumpTriggerHash;

    // Guards / timers
    private float groundLockTimer;   // prevents immediate re-grounding after jump
    private float coyoteTimer;       // counts down after leaving ground
    private float jumpBufferTimer;   // counts down after pressing jump in air

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        // Animator hookup
        animator = animatorOverride ? animatorOverride : GetComponentInChildren<Animator>(true);
        if (!animator)
        {
            Debug.LogWarning($"[{name}] No Animator found on self or children. Animation params won't update.");
        }
        speedHash      = Animator.StringToHash(speedParamName);
        yVelHash       = Animator.StringToHash(yVelParamName);
        groundedHash   = Animator.StringToHash(groundedParamName);
        jumpTriggerHash= Animator.StringToHash(jumpTriggerName);

        // Input System: PlayerInput on self OR parent
        playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput && playerInput.actions)
        {
            moveAction = playerInput.actions.FindAction("Move",  true);
            jumpAction = playerInput.actions.FindAction("Jump",  true);
            moveAction?.Enable();
            jumpAction?.Enable();

            if (logInputRouting)
                Debug.Log($"[{name}] PlayerInput OK. Map='{playerInput.currentActionMap?.name}', Scheme='{playerInput.currentControlScheme}'");
        }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        else
        {
            Debug.LogWarning($"[{name}] No PlayerInput found but project uses the New Input System. Add a PlayerInput or set Active Input Handling = Both.");
        }
#endif

        if (terrainLayer == 0) terrainLayer = LayerMask.GetMask("Default");
    }

    void Update()
    {
        HandleInput();
        CheckGround();
        HandleMovement();   // ground vs air (uses airControlPercent)
        HandleJump();       // now consumes coyote & jump buffer
        HandleSlope();
        ApplyGravity();

        if (!useRootMotion)
        {
            Vector3 finalMove = (moveVelocity + verticalVelocity + slideVelocity) * Time.deltaTime;
            controller.Move(finalMove);
        }

        UpdateAnimationParameters();
        wasGrounded = isGrounded;
    }

    void OnAnimatorMove()
    {
        if (useRootMotion && animator)
        {
            Vector3 rm = animator.deltaPosition;
            rm.y = (verticalVelocity.y + slideVelocity.y) * Time.deltaTime;
            controller.Move(rm);
        }
    }

    // ───────────────────────── input ─────────────────────────
    private void HandleInput()
    {
        inputVector = moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (inputVector.sqrMagnitude > 1f) inputVector.Normalize();

        // Jump press: buffer it if we're not eligible right now
        bool pressedThisFrame = (jumpAction != null && jumpAction.WasPressedThisFrame());
        if (!pressedThisFrame && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            pressedThisFrame = true;
            if (logInputRouting) Debug.Log("Gamepad South pressed (✕/A) – fallback");
        }
        if (!pressedThisFrame)
            pressedThisFrame = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);

        if (pressedThisFrame)
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBufferTime; // start/refresh jump buffer
        }

        if (logInputRouting && Time.frameCount % 60 == 0)
        {
            var scheme = playerInput ? playerInput.currentControlScheme : "(no PlayerInput)";
            var map    = playerInput ? playerInput.currentActionMap?.name : null;
            Debug.Log($"[{name}] Scheme={scheme}, Map={map}, Input={inputVector}, JumpBuffered={(jumpBufferTimer>0 ? jumpBufferTimer.ToString("F2") : "0")}, Coyote={(coyoteTimer>0 ? coyoteTimer.ToString("F2") : "0")}");
        }
    }

    // ───────────────────────── grounding ─────────────────────────
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

            // small grace
            if (!ccGrounded && hit.distance <= controller.height/2f + groundCheckDistance)
                ccGrounded = true;
        }
        else
        {
            groundNormal = Vector3.up;
            currentSlopeAngle = 0f;
            isOnSlope = isSlidingOnSlope = false;
        }

        // Grounded state with jump lockout applied
        bool wasGroundedBefore = isGrounded;
        isGrounded = (groundLockTimer <= 0f) && ccGrounded;

        // Update coyote timer when we *leave* ground
        if (wasGroundedBefore && !isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else if (!isGrounded && coyoteTimer > 0f)
        {
            coyoteTimer -= Time.deltaTime;
            if (coyoteTimer < 0f) coyoteTimer = 0f;
        }

        // If we just landed, we can consume a buffered jump in HandleJump()
    }

    // ───────────────────────── movement (ground vs air) ─────────────────────────
    private void HandleMovement()
    {
        if (isSlidingOnSlope) return;

        // camera-relative
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
            // Grounded acceleration / deceleration
            if (inputMag > 0.1f)
            {
                moveVelocity = Vector3.Lerp(moveVelocity, desiredDir * targetSpd, acceleration * Time.deltaTime);

                if (!useRootMotion && desiredDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                moveVelocity = Vector3.Lerp(moveVelocity, Vector3.zero, deceleration * Time.deltaTime);
            }
        }
        else
        {
            // Airborne: limited steering, no instant reverse
            Vector3 currentFlat = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
            Vector3 desiredFlat = desiredDir * targetSpd;

            float airAccel = Mathf.Max(0.01f, acceleration * Mathf.Clamp01(airControlPercent));
            Vector3 wishDelta = desiredFlat - currentFlat;
            Vector3 delta = Vector3.ClampMagnitude(wishDelta, airAccel * Time.deltaTime);

            if (currentFlat.sqrMagnitude > 1e-6f)
            {
                Vector3 dir  = currentFlat.normalized;
                float   along = Vector3.Dot(delta, dir);
                Vector3 perp  = delta - dir * along;

                perp *= airControlPercent;

                float maxDecel = currentFlat.magnitude; // can cancel up to current speed
                if (along < -maxDecel) along = -maxDecel;

                delta = dir * along + perp;
            }
            else
            {
                delta *= airControlPercent;
            }

            currentFlat += delta;
            moveVelocity = new Vector3(currentFlat.x, moveVelocity.y, currentFlat.z);

            // Optional facing in air (yaw only)
            if (!useRootMotion && desiredDir.sqrMagnitude > 1e-6f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * airControlPercent * Time.deltaTime);
            }
        }
    }

    // ───────────────────────── jump (consumes buffer & coyote) ─────────────────────────
    private void HandleJump()
    {
        // Timers tick down every frame
        if (jumpBufferTimer > 0f) { jumpBufferTimer -= Time.deltaTime; if (jumpBufferTimer < 0f) jumpBufferTimer = 0f; }

        // Are we eligible to jump right now?
        bool canUseCoyote = coyoteTimer > 0f;
        bool eligibleNow  = ((isGrounded && groundLockTimer <= 0f) || canUseCoyote) && !isSlidingOnSlope;

        // If we have a buffered jump and we become eligible, consume it
        if (jumpBufferTimer > 0f && eligibleNow)
        {
            DoJump();
            return;
        }

        // If we pressed jump this frame and are eligible, jump immediately
        if (jumpPressed && eligibleNow)
        {
            DoJump();
            return;
        }

        // Clear one-frame press flag
        jumpPressed = false;
    }

    private void DoJump()
    {
        verticalVelocity.y = jumpInitialVelocity;
        isGrounded = false;

        // Animation trigger ONCE
        if (animator) animator.SetTrigger(jumpTriggerHash);

        // Reset timers so we don't chain
        groundLockTimer = postJumpGroundLock;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        jumpPressed = false;
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
        else
        {
            slideVelocity = Vector3.Lerp(slideVelocity, Vector3.zero, Time.deltaTime * 10f);
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity.y <= 0f)
        {
            verticalVelocity.y = -groundedGravity;
        }
        else
        {
            float mul = 1f;
            if (verticalVelocity.y > 0f && Mathf.Abs(verticalVelocity.y) < jumpHangVelocityThreshold)
                mul = jumpHangTimeGravityMultiplier;
            else if (verticalVelocity.y < 0f)
                mul = fallGravityMultiplier;

            verticalVelocity.y -= normalGravity * mul * Time.deltaTime;
        }
    }

    // ───────────────────────── animator ─────────────────────────
    private void UpdateAnimationParameters()
    {
        if (!animator) return;

        // Speed 0..1 for your blend tree
        float speed01;
        if (isSlidingOnSlope)
        {
            speed01 = Mathf.Clamp01(slideVelocity.magnitude / runSpeed);
        }
        else if (useInputForSpeedParam)
        {
            speed01 = inputVector.magnitude; // 0 idle, ~0.5 walk, 1 run
        }
        else
        {
            float planar = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;
            speed01 = Mathf.Clamp01(planar / runSpeed);
        }
        if (speed01 < minAnimationSpeed) speed01 = 0f;

        animator.SetFloat(speedHash, speed01, animationSpeedSmoothTime, Time.deltaTime);

        float yForAnim = (isGrounded && Mathf.Abs(verticalVelocity.y) <= groundedGravity) ? 0f : verticalVelocity.y;
        animator.SetFloat(yVelHash, yForAnim);
        animator.SetBool(groundedHash, isGrounded);

        if (logAnimatorValues && Time.frameCount % 30 == 0)
        {
            float rb = animator.GetFloat(speedHash);
            Debug.Log($"[{name}] Speed→{speed01:F2} readback→{rb:F2}  YVel→{yForAnim:F2}  Grounded→{isGrounded}  Buffer→{jumpBufferTimer:F2}  Coyote→{coyoteTimer:F2}");
        }
    }

    // ───────────────────────── gizmos ─────────────────────────
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, groundNormal * 2f);

        if (moveVelocity.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, (new Vector3(moveVelocity.x,0f,moveVelocity.z)).normalized * 2f);
        }

        if (isSlidingOnSlope && slideVelocity.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, slideVelocity.normalized * 2f);
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }

    // Public getters
    public bool   IsGrounded()          => isGrounded;
    public bool   IsSliding()           => isSlidingOnSlope;
    public float  GetCurrentSlopeAngle()=> currentSlopeAngle;
    public Vector3 GetVelocity()        => moveVelocity + verticalVelocity + slideVelocity;
    public float  GetNormalizedSpeed()  => Mathf.Clamp01(useInputForSpeedParam ? inputVector.magnitude : new Vector3(moveVelocity.x,0f,moveVelocity.z).magnitude / runSpeed);
    public float  GetVerticalVelocity() => verticalVelocity.y;
    public float  GetInputMagnitude()   => inputVector.magnitude;
}
