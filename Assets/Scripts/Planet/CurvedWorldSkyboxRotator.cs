using UnityEngine;

/// <summary>
/// Rotates a skydome so the sky scrolls opposite the player’s movement on an
/// Animal-Crossing-style globe, with optional smoothing to prevent “snaps”.
///
/// ──────────────────────────────────────────────────────────────────────────
///  • Auto-finds the first active object on layer “PlayerHuman”.
///  • Converts player X/Z movement into opposite Y/X sky rotations.
///  • UnitsPerFullRotation  : 360° spin every N world-units walked.
///  • RotationSmoothTime    : seconds for the dome to catch up (0 = instant).
///  • FollowPlayerPosition  : ON → dome follows player; OFF → stays fixed.
/// </summary>
[ExecuteAlways]
public class CurvedWorldSkyboxRotator : MonoBehaviour
{
    // ────────────── USER SETTINGS ──────────────

    [Tooltip("World units the player must move for the sky to complete one full "
             + "360° rotation. Smaller = faster sky.")]
    [Min(0.01f)]
    [SerializeField] private float unitsPerFullRotation = 100f;

    [Tooltip("Seconds it takes for the sky to reach its target rotation. "
             + "0 = instant (no smoothing).")]
    [Min(0f)]
    [SerializeField] private float rotationSmoothTime = 0.25f;

    [Tooltip("If ON, the skydome follows the player’s position. "
             + "If OFF, it stays where you placed it.")]
    [SerializeField] private bool followPlayerPosition = false;

    // ────────────── PRIVATE FIELDS ──────────────
    private Transform  player;                 // cached PlayerHuman transform
    private Vector3    lastPlayerPosition;     // previous frame position
    private bool       hasInitialPlayerPos;
    private int        playerLayer;

    private Vector3    staticPosition;         // original dome position
    private Quaternion targetRotation;         // where the dome *wants* to be

    // ────────────── INITIALISATION ──────────────
    private void Awake()
    {
        staticPosition  = transform.position;  // remember designer-placed position
        targetRotation  = transform.rotation;  // start from current orientation
        playerLayer     = LayerMask.NameToLayer("PlayerHuman");
        TryFindPlayer();
    }

    private void Start()
    {
        if (player != null)
        {
            lastPlayerPosition  = player.position;
            hasInitialPlayerPos = true;
        }
    }

    // ────────────── MAIN LOOP ──────────────
    private void LateUpdate()
    {
        if (!TryFindPlayer()) return;          // keep searching until found

        // First frame after discovery
        if (!hasInitialPlayerPos)
        {
            lastPlayerPosition  = player.position;
            hasInitialPlayerPos = true;
        }

        // 1. ─── Calculate desired rotation from player Δ-movement ───
        Vector3 delta = player.position - lastPlayerPosition;
        lastPlayerPosition = player.position;

        if (delta.sqrMagnitude > 1e-6f)
        {
            float degPerUnit   = 360f / unitsPerFullRotation;
            float pitchDegrees = -delta.z * degPerUnit;   // around X
            float yawDegrees   = -delta.x * degPerUnit;   // around Y

            Quaternion deltaRot = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            targetRotation      = deltaRot * targetRotation; // accumulate
        }

        // 2. ─── Smoothly move current rotation toward target ───
        if (rotationSmoothTime <= 0f)
        {
            transform.rotation = targetRotation;          // snap
        }
        else
        {
            float t = Time.deltaTime / rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        // 3. ─── Handle position ───
        transform.position = followPlayerPosition ? player.position : staticPosition;
    }

    // ────────────── HELPERS ──────────────
    /// <summary>Locate the first active GameObject on layer “PlayerHuman”.</summary>
    private bool TryFindPlayer()
    {
        if (player != null) return true;

        foreach (var go in FindObjectsOfType<GameObject>(true))
        {
            if (go.layer == playerLayer)
            {
                player = go.transform;
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    // Refresh cached data when values change in the Inspector (Edit mode)
    private void OnValidate()
    {
        unitsPerFullRotation = Mathf.Max(unitsPerFullRotation, 0.01f);
        rotationSmoothTime   = Mathf.Max(rotationSmoothTime, 0f);
        staticPosition       = transform.position;        // update if moved in scene
    }
#endif
}
