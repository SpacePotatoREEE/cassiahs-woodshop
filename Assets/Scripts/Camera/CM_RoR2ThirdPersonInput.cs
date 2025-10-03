using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple Risk-of-Rain-2 style input driver for Cinemachine 3rdPersonFollow.
/// - Rotate this pivot with mouse / right stick (yaw/pitch).
/// - Zoom with wheel / triggers, clamps to min/max.
/// - Flip shoulder with Q / left bumper.
/// Requires: A CinemachineCamera with Body=Cinemachine3rdPersonFollow following this pivot.
/// </summary>
[DefaultExecutionOrder(10)]
public class CM_RoR2ThirdPersonInput : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your Cinemachine vcam that has a Cinemachine3rdPersonFollow body.")]
    public CinemachineCamera vcam;
    [Tooltip("Optional: the same object or a child used for the follow anchor. Leave null to use this Transform.")]
    public Transform followAnchor;

    [Header("Look (Yaw/Pitch)")]
    public float yawSensitivity   = 200f; // deg/sec per unit input
    public float pitchSensitivity = 160f;
    public float minPitch = -35f;
    public float maxPitch =  70f;
    public bool invertY = false;
    public float rotationDamping = 12f; // smoothing when applying to transform

    [Header("Zoom")]
    public float defaultDistance = 6.5f;
    public float minDistance     = 2.0f;
    public float maxDistance     = 12f;
    public float zoomSpeed       = 6f;   // wheel / trigger speed
    public float zoomDamping     = 12f;

    [Header("Shoulder Swap")]
    public KeyCode shoulderSwapKey = KeyCode.Q;
    public float shoulderSwapLerp  = 15f;  // how fast to lerp CameraSide on swap

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (New Input System)")]
    public InputActionReference lookAction;      // Vector2
    public InputActionReference zoomAction;      // float (positive = zoom in, negative = out), or use Scroll axis
    public InputActionReference shoulderSwapAction; // Button (optional)
#endif

    // runtime
    float _yaw;
    float _pitch;
    float _targetDistance;
    float _smoothDistance;
    float _targetCameraSide = 1f;  // +1 right shoulder, -1 left
    float _currentCameraSide = 1f;

    Cinemachine3rdPersonFollow _tpFollow;

    void Awake()
    {
        if (followAnchor == null) followAnchor = transform;
        if (vcam == null) vcam = FindObjectOfType<CinemachineCamera>(true);

        if (vcam != null)
            _tpFollow = vcam.GetComponent<Cinemachine3rdPersonFollow>();

        if (_tpFollow != null)
        {
            // Initialize distance from component or our default
            if (_tpFollow.CameraDistance > 0f)
                _targetDistance = Mathf.Clamp(_tpFollow.CameraDistance, minDistance, maxDistance);
            else
                _targetDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);

            _smoothDistance = _targetDistance;
            _currentCameraSide = Mathf.Sign(Mathf.Approximately(_tpFollow.CameraSide, 0f) ? 1f : _tpFollow.CameraSide);
            _targetCameraSide  = _currentCameraSide;
        }

        // Seed yaw from current facing
        Vector3 fwd = followAnchor.forward;
        _yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(10f, minPitch, maxPitch);

#if ENABLE_INPUT_SYSTEM
        EnableActions(true);
#endif
    }

    void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        EnableActions(false);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    void EnableActions(bool enable)
    {
        if (lookAction != null)
        {
            if (enable) lookAction.action.Enable();
            else lookAction.action.Disable();
        }
        if (zoomAction != null)
        {
            if (enable) zoomAction.action.Enable();
            else zoomAction.action.Disable();
        }
        if (shoulderSwapAction != null)
        {
            if (enable) shoulderSwapAction.action.Enable();
            else shoulderSwapAction.action.Disable();
        }
    }
#endif

    void Update()
    {
        if (_tpFollow == null || vcam == null || followAnchor == null)
            return;

        // ---------- INPUT ----------
        Vector2 look = ReadLook();
        float zoom   = ReadZoom();
        bool swap    = ReadShoulderSwap();

        // yaw/pitch
        _yaw   += look.x * yawSensitivity * Time.deltaTime;
        _pitch += (invertY ? look.y : -look.y) * pitchSensitivity * Time.deltaTime;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // zoom
        if (Mathf.Abs(zoom) > 0.0001f)
        {
            _targetDistance = Mathf.Clamp(_targetDistance - zoom * zoomSpeed, minDistance, maxDistance);
        }

        // shoulder toggle
        if (swap)
            _targetCameraSide = -Mathf.Sign(_targetCameraSide);

        // ---------- APPLY ----------
        // Smoothly rotate this pivot
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.rotation = SmoothDampRotation(transform.rotation, targetRot, rotationDamping);

        // Smooth zoom to 3rdPersonFollow
        _smoothDistance = Mathf.Lerp(_smoothDistance, _targetDistance, 1f - Mathf.Exp(-zoomDamping * Time.deltaTime));
        _tpFollow.CameraDistance = _smoothDistance;

        // Smooth shoulder side (-1..+1)
        _currentCameraSide = Mathf.Lerp(_currentCameraSide, _targetCameraSide, 1f - Mathf.Exp(-shoulderSwapLerp * Time.deltaTime));
        _tpFollow.CameraSide = _currentCameraSide;

        // Keep vcam hooked to this pivot (defensive if other code reassigns)
        if (vcam.Follow != followAnchor) vcam.Follow = followAnchor;
        if (vcam.LookAt != followAnchor) vcam.LookAt = followAnchor;
    }

    // ---------- INPUT HELPERS ----------
    Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        if (lookAction != null && lookAction.action != null)
            return lookAction.action.ReadValue<Vector2>();
#endif
        // Old Input fallback
        float x = Input.GetAxisRaw("Mouse X") + Input.GetAxisRaw("RightStickHorizontal");
        float y = Input.GetAxisRaw("Mouse Y") + Input.GetAxisRaw("RightStickVertical");
        return new Vector2(x, y);
    }

    float ReadZoom()
    {
#if ENABLE_INPUT_SYSTEM
        if (zoomAction != null && zoomAction.action != null)
            return zoomAction.action.ReadValue<float>();
#endif
        // Mouse wheel positive = zoom in
        float mouse = Input.GetAxis("Mouse ScrollWheel");
        // Optionally add triggers: RT - LT
        float rt = Input.GetAxisRaw("RT");
        float lt = Input.GetAxisRaw("LT");
        return mouse + (rt - lt) * 0.25f;
    }

    bool ReadShoulderSwap()
    {
#if ENABLE_INPUT_SYSTEM
        if (shoulderSwapAction != null && shoulderSwapAction.action != null)
            return shoulderSwapAction.action.triggered;
#endif
        return Input.GetKeyDown(shoulderSwapKey) || Input.GetButtonDown("LeftBumper");
    }

    // ---------- MATH HELPERS ----------
    static Quaternion SmoothDampRotation(Quaternion current, Quaternion target, float damping)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, damping) * Time.deltaTime);
        return Quaternion.Slerp(current, target, t);
    }
}
