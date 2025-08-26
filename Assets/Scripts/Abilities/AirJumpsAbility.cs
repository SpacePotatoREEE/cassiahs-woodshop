using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class AirJumpsAbility : MonoBehaviour
{
    [Header("Air Jumps")]
    [Min(0)] public int maxAirJumps = 1;
    public bool  useControllerJumpVelocity = true;
    public float customAirJumpVelocity     = 12f;

    [Header("Air Jump Spin FX")]
    public bool   enableAirJumpSpin   = true;
    public float  airJumpSpinDuration = 0.25f;
    public Vector3 airJumpSpinAxis    = Vector3.up;

    [Header("Input")]
    public PlayerInput playerInputOverride;

    private TopDownCharacterController ctrl;
    private PlayerInput playerInput;
    private InputAction jumpAction;

    private int airJumpsUsed = 0;

    private bool   spinActive = false;
    private float  spinTimer  = 0f;
    private Quaternion spinStartRotation;

    private void Awake()
    {
        ctrl = GetComponent<TopDownCharacterController>();
        if (!ctrl) { Debug.LogError("[AirJumpsAbility] Missing TopDownCharacterController."); enabled = false; return; }

        playerInput = playerInputOverride ? playerInputOverride : GetComponent<PlayerInput>();
        if (!playerInput) playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput && playerInput.actions) {
            jumpAction = playerInput.actions.FindAction("Jump", true);
            jumpAction?.Enable();
        }
    }

    private void Update()
    {
        if (!enabled) return;

        if (ctrl.IsGrounded) airJumpsUsed = 0;

        bool pressed =
            (jumpAction != null && jumpAction.WasPressedThisFrame()) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) ||
            Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);

        if (!pressed) return;

        // If the controller consumed a jump this frame (first jump), do nothing.
        if (ctrl.JumpConsumedThisFrame) return;

        // If a base jump would be accepted (ground/coyote), do nothing — no spin on first jump.
        if (ctrl.IsBaseJumpEligible) return;

        // True in-air jump path (double/triple…)
        if (!ctrl.IsGrounded && airJumpsUsed < maxAirJumps)
        {
            float vy = useControllerJumpVelocity ? ctrl.JumpInitialVelocity : customAirJumpVelocity;
            ctrl.ForceJump(vy);
            airJumpsUsed++;

            if (enableAirJumpSpin && airJumpSpinDuration > 0f)
            {
                spinActive = true;
                spinTimer  = 0f;
                spinStartRotation = transform.rotation;
            }
        }
    }

    private void LateUpdate()
    {
        if (!spinActive) return;

        spinTimer += Time.deltaTime;
        float t = Mathf.Clamp01(spinTimer / Mathf.Max(0.0001f, airJumpSpinDuration));
        float angle = 360f * t;
        transform.rotation = Quaternion.AngleAxis(angle, airJumpSpinAxis.normalized) * spinStartRotation;

        if (t >= 1f)
        {
            spinActive = false;
            transform.rotation = spinStartRotation; // finish cleanly after 360°
        }
    }
}
