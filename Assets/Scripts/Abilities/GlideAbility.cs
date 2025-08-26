using UnityEngine;
using UnityEngine.InputSystem;

/// Glide (BotW-style):
/// • Caps descent only while FALLING (won’t kill jump takeoff)
/// • Drives planar velocity from camera-relative input (like walking)
/// • Rotates ROOT toward input at glideTurnSpeed (deg/s)
/// • Forward LEAN (X) uses leanActivationDelay (writes X only; skipped when falling)
/// • Yaw visual offset (LOCAL-Y on CHILD mesh) scales with ANGLE between INPUT and ROOT FORWARD:
///     0° diff   →  0° offset
///     yawAngleForFullOffset° diff → ±yawOffsetMax
///   Blends toward target using yawOffsetResponseTime
/// • Wings: delay + scale (delay skipped when falling)
/// • Near-ground EXIT: if grounded OR within groundExitDistance, glide cancels immediately (upright + wings closed)
[DisallowMultipleComponent]
public class GlideAbility : MonoBehaviour
{
    [Header("Glide")]
    [Min(0f)] public float maxDescentSpeed = 1.0f;

    [Header("Glide Steering")]
    [Min(0f)]    public float glideMaxSpeed     = 7.0f;
    [Min(0.01f)] public float glideAcceleration = 20f;
    [Min(1f)]    public float glideTurnSpeed    = 540f;
    [Range(0f,1f)] public float airGlideControlPercent = 0.6f;

    [Header("Lean (forward X tilt)")]
    [Tooltip("Transform to tilt forward while gliding (usually a CHILD mesh). If empty, uses the player root.")]
    public Transform leanTarget;
    [Range(0f, 80f)] public float leanAngle = 45f;
    [Min(0f)] public float leanSmoothTime = 0.12f;
    [Min(0f)] public float leanActivationDelay = 0.08f;

    [Header("Yaw Visual Offset (local Y on CHILD mesh)")]
    public bool enableYawVisualOffset = true;
    [Tooltip("CHILD mesh to twist on local Y. If empty or set to root, yaw offset is disabled (safe).")]
    public Transform yawVisualTarget;
    [Range(0f, 90f)]  public float yawOffsetMax = 90f;
    [Range(1f, 180f)] public float yawAngleForFullOffset = 90f;
    [Min(0f)]         public float yawOffsetResponseTime = 0.5f;
    public bool       invertYawVisualOffset = false;

    [Header("Wings Visual")]
    public GameObject wingMesh;
    [Min(0f)] public float wingActivationDelay = 0.08f;
    [Min(0f)] public float wingOpenDuration = 0.15f;
    [Min(0f)] public float wingCloseDuration = 0.12f;
    public Vector3 wingScaleStart = new Vector3(0f, 1f, 1f);
    public Vector3 wingScaleEnd   = new Vector3(1f, 1f, 1f);
    public bool disableWingsWhenClosed = true;

    [Header("Near-Ground Exit (snap out of glide)")]
    [Tooltip("If ON, approaching the ground exits glide and stands you upright.")]
    public bool exitGlideNearGround = true;
    [Tooltip("Distance from feet at which to exit glide (meters).")]
    [Min(0.01f)] public float groundExitDistance = 0.5f;
    [Tooltip("Layers considered ground for the proximity check.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Upward offset of the proximity ray origin (meters).")]
    [Min(0f)] public float groundProbeUpOffset = 0.15f;

    [Header("Input")]
    public PlayerInput playerInputOverride;

    // refs
    private TopDownCharacterController ctrl;
    private PlayerInput playerInput;
    private InputAction moveAction, jumpAction;
    private Camera mainCam;

    // state
    private bool glidingLastFrame = false;
    private float leanDelayTimer = 0f;

    // LEAN (X)
    private float currentLean = 0f, leanVel = 0f;

    // YAW offset (local Y on child mesh)
    private float targetYawOffset = 0f;
    private float currentYawOffset = 0f;
    private float yawOffsetVel = 0f;

    // wings
    private enum WingPhase { Closed, Delay, Opening, Open, Closing }
    private WingPhase wingPhase = WingPhase.Closed;
    private float wingTimer = 0f;

    private bool warnedRootYawTarget = false;

    private void Awake()
    {
        ctrl = GetComponent<TopDownCharacterController>();
        if (!ctrl) { Debug.LogError("[GlideAbility] Missing TopDownCharacterController."); enabled = false; return; }

        playerInput = playerInputOverride ? playerInputOverride : GetComponent<PlayerInput>();
        if (!playerInput) playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput && playerInput.actions) {
            moveAction = playerInput.actions.FindAction("Move", true);
            jumpAction = playerInput.actions.FindAction("Jump", true);
            moveAction?.Enable(); jumpAction?.Enable();
        }

        mainCam = Camera.main;
        if (!leanTarget) leanTarget = transform;

        if (wingMesh) {
            wingMesh.transform.localScale = wingScaleStart;
            if (disableWingsWhenClosed) wingMesh.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ResetVisualsAndOverrides();
        glidingLastFrame = false;
    }

    private void Update()
    {
        // Hold Jump to glide
        bool holdingJump =
            (jumpAction != null && jumpAction.IsPressed()) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed) ||
            Input.GetKey(KeyCode.Space);

        // Camera-relative input (flat)
        Vector2 stick = moveAction != null ? moveAction.ReadValue<Vector2>()
                                           : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (stick.sqrMagnitude > 1f) stick = stick.normalized;

        Vector3 camF = (mainCam ? mainCam.transform.forward : Vector3.forward);  camF.y = 0f; camF.Normalize();
        Vector3 camR = (mainCam ? mainCam.transform.right   : Vector3.right  );  camR.y = 0f; camR.Normalize();
        Vector3 inputDir = (camR * stick.x + camF * stick.y);

        // Detect proximity to ground or grounding
        bool grounded = ctrl.IsGrounded;
        bool nearGround = exitGlideNearGround && NearGround();   // raycast check

        // We call it "gliding" only if: holding jump AND not grounded AND not near ground
        bool glidingNow = holdingJump && !grounded && !(exitGlideNearGround && nearGround);

        // If we WERE gliding but now grounded/near ground, cancel immediately (snap upright)
        if (holdingJump && (grounded || nearGround) && glidingLastFrame)
        {
            CancelGlideVisualsAndOverrides();
        }

        if (glidingNow && !glidingLastFrame)
        {
            // (re)start lean delay
            leanDelayTimer = leanActivationDelay;
        }

        if (glidingNow)
        {
            // 1) Planar drive (camera-relative, like walking)
            Vector3 desiredVel = inputDir * (glideMaxSpeed * stick.magnitude);
            ctrl.SetAirDrive(desiredVel, glideAcceleration);
            ctrl.SetAirControlOverride(airGlideControlPercent);

            // 2) Rotate ROOT toward input with glideTurnSpeed
            if (inputDir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, glideTurnSpeed * Time.deltaTime);
            }

            // 3) Always descend only while FALLING (never cancel takeoff)
            float vy = ctrl.VerticalVelocityY;
            if (vy <= 0f && vy < -maxDescentSpeed) ctrl.VerticalVelocityY = -maxDescentSpeed;

            // Skip lean delay while falling
            if (vy < 0f && leanDelayTimer > 0f) leanDelayTimer = 0f;
        }
        else
        {
            ctrl.ClearAirControlOverride();
            ctrl.ClearAirDrive();
        }

        // Wings
        UpdateWings(glidingNow);

        // Lean (X) with delay
        UpdateLean(glidingNow);

        // Yaw visual offset from angle(INPUT, ROOT FORWARD)
        ComputeTargetYawOffset(glidingNow, inputDir);

        glidingLastFrame = glidingNow;
    }

    private void LateUpdate()
    {
        // Apply LEAN (X only)
        if (leanTarget)
        {
            var e = leanTarget.localEulerAngles;
            e.x = currentLean;              // preserve Y/Z
            leanTarget.localEulerAngles = e;
        }

        // Apply YAW VISUAL OFFSET (Y only), blending to target
        if (enableYawVisualOffset && yawVisualTarget && yawVisualTarget != transform)
        {
            if (yawOffsetResponseTime <= 0f)
                currentYawOffset = targetYawOffset;
            else
                currentYawOffset = Mathf.SmoothDampAngle(currentYawOffset, targetYawOffset, ref yawOffsetVel, yawOffsetResponseTime);

            var e = yawVisualTarget.localEulerAngles;
            e.y = currentYawOffset;         // preserve X/Z
            yawVisualTarget.localEulerAngles = e;
        }
    }

    // ───────────────────────── helpers ─────────────────────────

    private bool NearGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundProbeUpOffset;
        float maxDist = groundExitDistance + groundProbeUpOffset;
        return Physics.Raycast(origin, Vector3.down, out _, maxDist, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void CancelGlideVisualsAndOverrides()
    {
        // Clear controller overrides
        ctrl.ClearAirControlOverride();
        ctrl.ClearAirDrive();

        // Wings: snap closed
        if (wingMesh)
        {
            wingMesh.transform.localScale = wingScaleStart;
            if (disableWingsWhenClosed && wingMesh.activeSelf) wingMesh.SetActive(false);
        }
        wingPhase = WingPhase.Closed; wingTimer = 0f;

        // Lean & yaw: snap upright/zero
        if (leanTarget)
        {
            var e = leanTarget.localEulerAngles; e.x = 0f; leanTarget.localEulerAngles = e;
        }
        currentLean = 0f; leanVel = 0f; leanDelayTimer = 0f;

        if (yawVisualTarget && yawVisualTarget != transform)
        {
            var e = yawVisualTarget.localEulerAngles; e.y = 0f; yawVisualTarget.localEulerAngles = e;
        }
        currentYawOffset = 0f; targetYawOffset = 0f; yawOffsetVel = 0f;
    }

    private void ResetVisualsAndOverrides()
    {
        ctrl.ClearAirControlOverride();
        ctrl.ClearAirDrive();

        if (wingMesh)
        {
            wingMesh.transform.localScale = wingScaleStart;
            if (disableWingsWhenClosed && wingMesh.activeSelf) wingMesh.SetActive(false);
        }
        wingPhase = WingPhase.Closed; wingTimer = 0f;

        if (leanTarget)
        {
            var e = leanTarget.localEulerAngles; e.x = 0f; leanTarget.localEulerAngles = e;
        }
        if (yawVisualTarget && yawVisualTarget != transform)
        {
            var e = yawVisualTarget.localEulerAngles; e.y = 0f; yawVisualTarget.localEulerAngles = e;
        }

        currentLean = 0f; leanVel = 0f;
        currentYawOffset = 0f; targetYawOffset = 0f; yawOffsetVel = 0f;
        leanDelayTimer = 0f;
    }

    // Forward lean (X) with activation delay (skipped when falling in Update)
    private void UpdateLean(bool gliding)
    {
        if (gliding && leanDelayTimer > 0f) leanDelayTimer -= Time.deltaTime;
        float targetLean = (gliding && leanDelayTimer <= 0f) ? leanAngle : 0f;
        currentLean = Mathf.SmoothDamp(currentLean, targetLean, ref leanVel, leanSmoothTime);
    }

    // Target yaw offset from angle difference between INPUT and ROOT FORWARD
    private void ComputeTargetYawOffset(bool gliding, Vector3 inputDir)
    {
        targetYawOffset = 0f;

        if (!enableYawVisualOffset || !yawVisualTarget) return;
        if (yawVisualTarget == transform)
        {
            if (!warnedRootYawTarget)
            {
                Debug.LogWarning("[GlideAbility] yawVisualTarget is the root; yaw twist is disabled to avoid blocking rotation.");
                warnedRootYawTarget = true;
            }
            return;
        }
        if (!gliding || inputDir.sqrMagnitude <= 0.0001f) return;

        Vector3 fwdFlat = transform.forward; fwdFlat.y = 0f; fwdFlat.Normalize();
        Vector3 inpFlat = inputDir;          inpFlat.y = 0f;  inpFlat.Normalize();
        if (fwdFlat.sqrMagnitude < 1e-6f || inpFlat.sqrMagnitude < 1e-6f) return;

        float signed = Vector3.SignedAngle(fwdFlat, inpFlat, Vector3.up); // -180..180
        float mag    = Mathf.Abs(signed);                                 // 0..180
        float sign   = Mathf.Sign(signed);
        float dirSign = invertYawVisualOffset ? -1f : 1f;

        float norm = mag / Mathf.Max(1f, yawAngleForFullOffset); // 0..∞ → clamp
        norm = Mathf.Clamp01(norm);                              // 0..1

        float amt = yawOffsetMax * norm;                         // 0..yawOffsetMax
        targetYawOffset = dirSign * sign * amt;                  // blended in LateUpdate
    }

    // Wings: delay + scale, delay skipped when falling (YVel < 0)
    private void UpdateWings(bool gliding)
    {
        if (!wingMesh) return;

        bool falling = ctrl.VerticalVelocityY < 0f;

        switch (wingPhase)
        {
            case WingPhase.Closed:
                if (gliding)
                {
                    if (falling)
                    {
                        wingPhase = WingPhase.Opening; wingTimer = 0f;
                        if (!wingMesh.activeSelf) wingMesh.SetActive(true);
                        wingMesh.transform.localScale = wingScaleStart;
                    }
                    else
                    {
                        wingPhase = (wingActivationDelay > 0f) ? WingPhase.Delay : WingPhase.Opening;
                        wingTimer = wingActivationDelay;
                        wingMesh.transform.localScale = wingScaleStart;
                        if (!disableWingsWhenClosed && !wingMesh.activeSelf) wingMesh.SetActive(true);
                    }
                }
                break;

            case WingPhase.Delay:
                if (!gliding)
                {
                    wingPhase = WingPhase.Closed; wingTimer = 0f;
                    wingMesh.transform.localScale = wingScaleStart;
                    if (disableWingsWhenClosed && wingMesh.activeSelf) wingMesh.SetActive(false);
                    break;
                }
                if (falling)
                {
                    wingPhase = WingPhase.Opening; wingTimer = 0f;
                    if (!wingMesh.activeSelf) wingMesh.SetActive(true);
                    wingMesh.transform.localScale = wingScaleStart;
                    break;
                }
                wingTimer -= Time.deltaTime;
                if (wingTimer <= 0f)
                {
                    wingPhase = WingPhase.Opening; wingTimer = 0f;
                    if (!wingMesh.activeSelf) wingMesh.SetActive(true);
                    wingMesh.transform.localScale = wingScaleStart;
                }
                break;

            case WingPhase.Opening:
                if (!gliding) { wingPhase = WingPhase.Closing; wingTimer = 0f; break; }
                if (wingOpenDuration <= 0f) { wingMesh.transform.localScale = wingScaleEnd; wingPhase = WingPhase.Open; break; }
                wingTimer += Time.deltaTime;
                {
                    float t = Mathf.Clamp01(wingTimer / wingOpenDuration);
                    wingMesh.transform.localScale = Vector3.Lerp(wingScaleStart, wingScaleEnd, t);
                    if (t >= 1f) wingPhase = WingPhase.Open;
                }
                break;

            case WingPhase.Open:
                if (!gliding) { wingPhase = WingPhase.Closing; wingTimer = 0f; }
                break;

            case WingPhase.Closing:
                if (gliding)
                {
                    if (falling)
                    {
                        wingPhase = WingPhase.Opening; wingTimer = 0f;
                        if (!wingMesh.activeSelf) wingMesh.SetActive(true);
                        wingMesh.transform.localScale = wingScaleStart;
                    }
                    else
                    {
                        wingPhase = (wingActivationDelay > 0f) ? WingPhase.Delay : WingPhase.Opening;
                        wingTimer = wingActivationDelay;
                    }
                    break;
                }
                if (wingCloseDuration <= 0f)
                {
                    wingMesh.transform.localScale = wingScaleStart;
                    if (disableWingsWhenClosed && wingMesh.activeSelf) wingMesh.SetActive(false);
                    wingPhase = WingPhase.Closed;
                    break;
                }
                wingTimer += Time.deltaTime;
                {
                    float t = Mathf.Clamp01(wingTimer / wingCloseDuration);
                    wingMesh.transform.localScale = Vector3.Lerp(wingScaleEnd, wingScaleStart, t);
                    if (t >= 1f)
                    {
                        if (disableWingsWhenClosed && wingMesh.activeSelf) wingMesh.SetActive(false);
                        wingPhase = WingPhase.Closed;
                    }
                }
                break;
        }
    }
}
