using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownPlayerController : MonoBehaviour
{
    /* ─────────────  Inspector  ───────────── */
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Bending")]
    [Tooltip("ScriptableObject containing every material that needs bending.")]
    [SerializeField] private BendMaterialList bendMaterials;

    [Tooltip("Transform used as the player’s position reference; if left blank " +
             "the script uses its own Transform.")]
    [SerializeField] private Transform playerTransform;

    /* ─────────────  Internals  ───────────── */
    private Rigidbody rb;
    private static readonly int PlayerPosID = Shader.PropertyToID("_PlayerPos");

    /* ─────────────  Lifecycle  ───────────── */
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;                 // top-down characters shouldn’t tip over
        if (playerTransform == null) playerTransform = transform;
    }

    private void FixedUpdate()
    {
        // Standard top-down rigidbody movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        Vector3 move = new(horizontal, 0f, vertical);
        Vector3 nextPos = rb.position + move * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);

        if (move.sqrMagnitude > 1e-3f)            // turn to face motion direction
            rb.MoveRotation(Quaternion.LookRotation(move, Vector3.up));
    }

    private void Update()
    {
        // Bail out fast if there’s nothing to update
        if (bendMaterials == null ||
            bendMaterials.materials == null ||
            bendMaterials.materials.Count == 0)
            return;

        Vector3 p = playerTransform.position;
        Vector4 playerPos4 = new(p.x, 0f, p.z, 0f);   // Y not used by the shader

        // Write to every listed material
        foreach (Material m in bendMaterials.materials)
            if (m != null) m.SetVector(PlayerPosID, playerPos4);
    }
}
