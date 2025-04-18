using UnityEngine;

/// Keeps an orthographic minimap camera centred on the target but
/// (optionally) locks rotation so “north” is always up.
[RequireComponent(typeof(Camera))]
public class MinimapCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;           // drag your player
    [SerializeField] private float     height = 60f;     // distance above target
    [SerializeField] private bool      lockRotation = true;
    [Tooltip("Euler angles to use when lockRotation = true")]
    [SerializeField] private Vector3   fixedEulerAngles = new Vector3(90, 0, 0);

    private void LateUpdate()
    {
        if (!target) return;

        // 1) Keep the camera positioned above the player
        Vector3 p = target.position;
        p.y += height;
        transform.position = p;

        // 2) Force a constant orientation if requested
        if (lockRotation)
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
    }

#if UNITY_EDITOR      // auto‑configure on first add
    private void Reset()
    {
        var cam = GetComponent<Camera>();
        cam.orthographic      = true;
        cam.orthographicSize  = 60f;
        cam.cullingMask       = LayerMask.GetMask("Minimap");
        cam.clearFlags        = CameraClearFlags.SolidColor;
        cam.backgroundColor   = new Color(0,0,0,0);
        transform.rotation    = Quaternion.Euler(fixedEulerAngles);
    }
#endif
}