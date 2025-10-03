using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(10)]
public class CM_RoR2ThirdPersonBootstrap : MonoBehaviour
{
    [Header("Player Discovery")]
    public Transform playerRoot;             // optional explicit reference
    public string playerTag = "Player";
    public string playerNameHint = "player";

    [Header("Look (Yaw/Pitch)")]
    public float yawSensitivity   = 200f;
    public float pitchSensitivity = 160f;
    public float minPitch = -35f;
    public float maxPitch =  70f;
    public bool invertY = false;
    public float rotationDamping = 12f;

    [Header("Zoom")]
    public float defaultDistance = 6.5f;
    public float minDistance     = 2.0f;
    public float maxDistance     = 12f;
    public float zoomSpeed       = 6f;
    public float zoomDamping     = 12f;

    [Header("Shoulder")]
    public float initialCameraSide = 1f; // +1 right, -1 left
    public KeyCode shoulderSwapKey = KeyCode.Q;
    public float shoulderSwapLerp  = 15f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (New Input System)")]
    public InputActionReference lookAction;         // Vector2
    public InputActionReference zoomAction;         // float (+in/-out)
    public InputActionReference shoulderSwapAction; // Button
#endif

    [Header("Legacy Input (Optional)")]
    public bool   useLegacyOldInputFallbacks = false;
    public string legacyRightStickX = "";
    public string legacyRightStickY = "";
    public string legacyTriggerRT   = "";
    public string legacyTriggerLT   = "";
    public string legacyLeftBumper  = "";

    // Runtime
    private CinemachineCamera _vcam;
    private Cinemachine3rdPersonFollow _tpFollow;

    // World-space rig (not parented to player → no inherited rotation)
    private Transform _pivot;        // rotates yaw/pitch in world space
    private Transform _followAnchor; // optional child (not required)

    private float _yaw, _pitch;
    private float _targetDistance, _smoothDistance;
    private float _targetSide = 1f, _currentSide = 1f;

    void OnEnable()
    {
        _vcam = GetComponent<CinemachineCamera>();
        if (_vcam == null) { Debug.LogError("[CM_RoR2] CinemachineCamera required."); enabled = false; return; }

        _tpFollow = GetComponent<Cinemachine3rdPersonFollow>();
        if (_tpFollow == null) _tpFollow = gameObject.AddComponent<Cinemachine3rdPersonFollow>();

        RemoveIfExists<CinemachinePOV>();
        RemoveIfExists<CinemachineOrbitalFollow>();

        EnsureWorldPivotExists();
        BindToPlayerOrRetry();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

#if ENABLE_INPUT_SYSTEM
        EnableActions(true);
#endif
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
#if ENABLE_INPUT_SYSTEM
        EnableActions(false);
#endif
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene) => BindToPlayerOrRetry();

    void EnsureWorldPivotExists()
    {
        if (_pivot != null) return;

        // Create a world-space pivot (not parented under the player)
        GameObject pivotGO = new GameObject("CM_Pivot_RoR2");
        _pivot = pivotGO.transform;
        DontDestroyOnLoad(pivotGO); // since player is persistent, keep pivot too

        GameObject anchorGO = new GameObject("CM_FollowAnchor");
        _followAnchor = anchorGO.transform;
        _followAnchor.SetParent(_pivot, false);
        DontDestroyOnLoad(anchorGO);

        // Wire vcam once
        _vcam.Follow = _pivot;
        _vcam.LookAt = _pivot;
    }

    void BindToPlayerOrRetry()
    {
        if (playerRoot == null) playerRoot = FindPlayerRoot();
        if (playerRoot == null)
        {
            Debug.LogWarning("[CM_RoR2] Player not found yet; will retry next frame.");
            return;
        }

        // Seed yaw from player facing
        Vector3 fwd = playerRoot.forward;
        _yaw   = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(10f, minPitch, maxPitch);

        // Dist / shoulder init
        _targetDistance = Mathf.Clamp(
            _tpFollow.CameraDistance > 0f ? _tpFollow.CameraDistance : defaultDistance,
            minDistance, maxDistance);
        _smoothDistance = _targetDistance;

        _targetSide  = Mathf.Sign(Mathf.Approximately(_tpFollow.CameraSide, 0f) ? initialCameraSide : _tpFollow.CameraSide);
        _currentSide = _targetSide;

        // Nice defaults
        _tpFollow.Damping        = new Vector3(0.25f, 0.25f, 0.25f);
        _tpFollow.ShoulderOffset = new Vector3(0.85f, 1.25f, 0f);
        _tpFollow.CameraDistance = _smoothDistance;
        _tpFollow.CameraSide     = _currentSide;

        // Snap pivot to player position (rotation is independent of player)
        _pivot.position = playerRoot.position;
    }

    Transform FindPlayerRoot()
    {
        // Prefer your character controller type
        var tdc = FindObjectOfType<TopDownCharacterController>(true);
        if (tdc) return tdc.transform; // your walking controller rotates the character each frame. :contentReference[oaicite:2]{index=2}

        if (!string.IsNullOrEmpty(playerTag))
        {
            var tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged) return tagged.transform;
        }

        foreach (var t in FindObjectsOfType<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("player_planet_master") || n.Contains("player_space_master")) return t;
            if (!string.IsNullOrEmpty(playerNameHint) && n.Contains(playerNameHint.ToLowerInvariant())) return t;
        }
        return null;
    }

#if ENABLE_INPUT_SYSTEM
    void EnableActions(bool enable)
    {
        if (lookAction != null)         { if (enable) lookAction.action.Enable();         else lookAction.action.Disable(); }
        if (zoomAction != null)         { if (enable) zoomAction.action.Enable();         else zoomAction.action.Disable(); }
        if (shoulderSwapAction != null) { if (enable) shoulderSwapAction.action.Enable(); else shoulderSwapAction.action.Disable(); }
    }
#endif

    // Run AFTER your character turns, so the pivot can hold its own orientation cleanly.
    void LateUpdate()
    {
        if (_tpFollow == null || _vcam == null) return;
        if (playerRoot == null || _pivot == null) { BindToPlayerOrRetry(); return; }

        // Keep pivot at player position (no parenting, so no inherited rotation)
        _pivot.position = playerRoot.position;

        // Input
        Vector2 look = ReadLook();
        float zoom   = ReadZoom();
        bool swap    = ReadShoulderSwap();

        _yaw   += look.x * yawSensitivity * Time.deltaTime;
        _pitch += (invertY ? look.y : -look.y) * pitchSensitivity * Time.deltaTime;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // Rotate pivot in world space (independent from player turn)
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
        _pivot.rotation = SmoothDampRotation(_pivot.rotation, targetRot, rotationDamping);

        if (Mathf.Abs(zoom) > 0.0001f)
            _targetDistance = Mathf.Clamp(_targetDistance - zoom * zoomSpeed, minDistance, maxDistance);

        _smoothDistance = Mathf.Lerp(_smoothDistance, _targetDistance, 1f - Mathf.Exp(-zoomDamping * Time.deltaTime));
        _tpFollow.CameraDistance = _smoothDistance;

        if (swap) _targetSide = -Mathf.Sign(_targetSide);
        _currentSide = Mathf.Lerp(_currentSide, _targetSide, 1f - Mathf.Exp(-shoulderSwapLerp * Time.deltaTime));
        _tpFollow.CameraSide = _currentSide;

        // Defensive: some of your scene logic can reassign Follow/LookAt (e.g., CharacterSceneManager). Keep ours. :contentReference[oaicite:3]{index=3}
        if (_vcam.Follow != _pivot) _vcam.Follow = _pivot;
        if (_vcam.LookAt != _pivot) _vcam.LookAt = _pivot;
    }

    Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        if (lookAction != null && lookAction.action != null) return lookAction.action.ReadValue<Vector2>();
#endif
        float x = Input.GetAxisRaw("Mouse X");
        float y = Input.GetAxisRaw("Mouse Y");
        if (useLegacyOldInputFallbacks)
        {
            x += GetAxisRawSafe(legacyRightStickX);
            y += GetAxisRawSafe(legacyRightStickY);
        }
        return new Vector2(x, y);
    }

    float ReadZoom()
    {
#if ENABLE_INPUT_SYSTEM
        if (zoomAction != null && zoomAction.action != null) return zoomAction.action.ReadValue<float>();
#endif
        float z = Input.GetAxis("Mouse ScrollWheel");
        if (useLegacyOldInputFallbacks)
        {
            float rt = GetAxisRawSafe(legacyTriggerRT);
            float lt = GetAxisRawSafe(legacyTriggerLT);
            z += (rt - lt) * 0.25f;
        }
        return z;
    }

    bool ReadShoulderSwap()
    {
#if ENABLE_INPUT_SYSTEM
        if (shoulderSwapAction != null && shoulderSwapAction.action != null) return shoulderSwapAction.action.triggered;
#endif
        if (Input.GetKeyDown(shoulderSwapKey)) return true;
        if (useLegacyOldInputFallbacks && !string.IsNullOrEmpty(legacyLeftBumper))
        {
            try { return Input.GetButtonDown(legacyLeftBumper); } catch { /* ignore */ }
        }
        return false;
    }

    static Quaternion SmoothDampRotation(Quaternion current, Quaternion target, float damping)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, damping) * Time.deltaTime);
        return Quaternion.Slerp(current, target, t);
    }

    void RemoveIfExists<T>() where T : Component
    {
        var c = GetComponent<T>();
        if (c) Destroy(c);
    }

    static float GetAxisRawSafe(string axisName)
    {
        if (string.IsNullOrEmpty(axisName)) return 0f;
        try { return Input.GetAxisRaw(axisName); }
        catch (System.ArgumentException) { return 0f; }
    }
}
