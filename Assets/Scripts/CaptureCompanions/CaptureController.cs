using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CaptureController : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public LayerMask capturableMask;
    public GameObject captureBallPrefab;
    public CompanionRoster companionRoster;

    [Header("Aim Assist")]
    public float aimMaxDistance = 30f;
    public float aimSphereRadius = 0.6f;

    [Header("Input")]
    public KeyCode captureKey = KeyCode.C;
    public bool rightMouseCaptures = true;
    public bool logWhyItDidntThrow = true;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System (optional)")]
    public PlayerInput playerInput;
    public string captureActionName = "Capture";
    private InputAction captureAction;
#endif

    private ICapturable _aimed;
    private CaptureTargetHighlighter _aimHL;

    void Awake()
    {
        if (!aimCamera && Camera.main) aimCamera = Camera.main;

#if ENABLE_INPUT_SYSTEM
        if (!playerInput) playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput && playerInput.actions)
        {
            string[] candidates = { captureActionName, "Interact", "Fire", "Submit", "Primary", "Use" };
            foreach (var n in candidates)
            {
                captureAction = playerInput.actions.FindAction(n, false);
                if (captureAction != null) { captureAction.Enable(); break; }
            }
        }
#endif
    }

    void Update()
    {
        AcquireAim();

        bool pressed = Input.GetKeyDown(captureKey);
#if ENABLE_INPUT_SYSTEM
        if (!pressed && captureAction != null && captureAction.WasPressedThisFrame()) pressed = true;
        if (!pressed && Gamepad.current != null)
        {
            var rt = Gamepad.current.rightTrigger;
            if (rt != null && rt.wasPressedThisFrame) pressed = true;
        }
#else
        if (!pressed && rightMouseCaptures && Input.GetMouseButtonDown(1)) pressed = true;
#endif
        if (pressed) TryThrow();
    }

    void TryThrow()
    {
        if (!captureBallPrefab)
        {
            if (logWhyItDidntThrow) Debug.LogWarning("[Capture] No CaptureBall prefab assigned on CaptureController.");
            return;
        }
        if (_aimed == null)
        {
            if (logWhyItDidntThrow) Debug.Log("[Capture] No target aimed. Move crosshair toward a capturable enemy.");
            return;
        }
        if (!_aimed.CanCapture)
        {
            if (logWhyItDidntThrow) Debug.Log("[Capture] Target is not capturable yet (HP above threshold).");
            return;
        }

        var spawnPos = transform.position + Vector3.up * 1.4f;
        var ballGO = Instantiate(captureBallPrefab, spawnPos, Quaternion.identity);
        var ball   = ballGO.GetComponent<CaptureBall>();
        if (!ball) { Debug.LogError("[Capture] CaptureBall prefab missing component."); Destroy(ballGO); return; }
        ball.Launch(owner: this.transform, target: _aimed, roster: companionRoster);
    }

    void AcquireAim()
    {
        if (!aimCamera) { _aimed = null; ToggleHighlight(null, false); return; }
        Vector3 origin = aimCamera.transform.position;
        Vector3 dir = aimCamera.transform.forward;

        if (Physics.SphereCast(origin, aimSphereRadius, dir, out var hit, aimMaxDistance, capturableMask, QueryTriggerInteraction.Ignore))
        {
            var cap = hit.collider.GetComponentInParent<ICapturable>();
            if (cap != null)
            {
                ToggleHighlight(_aimed, false);
                _aimed = cap;
                ToggleHighlight(_aimed, true);
                return;
            }
        }
        ToggleHighlight(_aimed, false);
        _aimed = null;
    }

    void ToggleHighlight(ICapturable c, bool on)
    {
        if (c == null) return;
        if (_aimHL == null || _aimHL.gameObject != ((MonoBehaviour)c).gameObject)
            _aimHL = ((MonoBehaviour)c).GetComponent<CaptureTargetHighlighter>();
        if (_aimHL) _aimHL.SetHighlighted(on);
    }
}
