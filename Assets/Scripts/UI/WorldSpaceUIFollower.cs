using UnityEngine;

/// <summary>
/// Keeps a world‑space UI element fixed above a target without inheriting rotation.
/// Two rotation styles:
/// • FixedUp   – forward = +Y (top‑down prompt)  
/// • Billboard – faces the active camera
/// “Flip 180” turns the canvas to its readable side when needed.
/// </summary>
[DisallowMultipleComponent]
public class WorldSpaceUIFollower : MonoBehaviour
{
    public enum RotationMode { FixedUp, Billboard }

    [Header("Follow Target")]
    public Transform target;                         // enemy ship root
    public Vector3  worldOffset = Vector3.up * 2f;   // height above ship

    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.FixedUp;

    [Tooltip("If the text looks mirrored, tick this to spin the canvas 180° about Y.")]
    public bool flip180 = false;                     // <── NEW toggle

    private Camera mainCam;

    private void Awake()
    {
        transform.SetParent(null, true);             // stay at scene root
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1) Position
        transform.position = target.position + worldOffset;

        // 2) Orientation
        if (rotationMode == RotationMode.FixedUp)
        {
            Quaternion rot = Quaternion.Euler(-90f, 0f, 0f); // forward → +Y
            if (flip180) rot *= Quaternion.Euler(0f, 180f, 0f);
            transform.rotation = rot;
        }
        else if (rotationMode == RotationMode.Billboard && mainCam != null)
        {
            Vector3 toCam = transform.position - mainCam.transform.position;
            transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
        }
    }
}