using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownPlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody rb;
    public Material horizonBendMaterial;
    public Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // For a top-down game, you usually freeze rotation so it doesn't tip over
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        // Read input
        float horizontal = Input.GetAxis("Horizontal");  // default WASD / arrow keys
        float vertical = Input.GetAxis("Vertical");

        // Create a movement vector in XZ plane
        Vector3 move = new Vector3(horizontal, 0f, vertical);

        // Adjust speed and deltaTime
        Vector3 newPosition = rb.position + move * moveSpeed * Time.fixedDeltaTime;

        // Move the rigidbody
        rb.MovePosition(newPosition);

        // If you want your character to face the movement direction, do this:
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion newRotation = Quaternion.LookRotation(move, Vector3.up);
            rb.MoveRotation(newRotation);
        }
    }
    
    void Update()
    {
        Vector3 playerPos = playerTransform.position;
        // We only care about XZ for bending
        horizonBendMaterial.SetVector("_PlayerPos", new Vector4(playerPos.x, 0, playerPos.z, 0));
    }
}