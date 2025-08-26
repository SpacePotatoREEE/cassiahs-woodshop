using UnityEngine;

/// <summary>
/// Keeps a projector/decal oriented to project straight down world -Y,
/// even if its parent (the character) rotates.
/// Works for: URP Decal Projector objects or custom projector rigs.
/// </summary>
[ExecuteAlways]
public class KeepWorldDownRotation : MonoBehaviour
{
    [Header("Projection")]
    [Tooltip("If true, sets rotation so the projector looks straight down (-Y). If false, keeps a flat-up orientation for quads.")]
    public bool projectDown = true;

    [Tooltip("Optional extra yaw around the down axis (degrees). Useful if your projector expects a different forward.")]
    public float extraYawDegrees = 0f;

    void LateUpdate()
    {
        if (projectDown)
        {
            // Look straight down world -Y. 
            // Default 'forward' is -Z; we build a rotation that points forward toward -Y.
            // We define an arbitrary world-forward for a stable roll: Vector3.forward projected onto the horizontal plane.
            var worldDown = Vector3.down;
            var worldFwd = Vector3.forward;

            // Orthonormal basis with 'down' as forward axis of projection rigs that expect forward to be -Y:
            // We'll construct a rotation that has its up opposite to 'down' (i.e., up = +Y).
            // Use LookRotation(forward, up). We want forward along worldDown (any roll is fine for a circular blob).
            Quaternion q = Quaternion.LookRotation(worldDown, Vector3.up);

            if (Mathf.Abs(extraYawDegrees) > 0.0001f)
            {
                q = q * Quaternion.AngleAxis(extraYawDegrees, worldDown);
            }

            transform.rotation = q;
        }
        else
        {
            // Keep flat on world XY plane (up = +Y), ignoring parent rotation.
            transform.rotation = Quaternion.identity;
        }
    }
}