using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipDriftController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 10f;         // Acceleration force
    public float rotationSpeed = 80f;       // Yaw rotation speed
    public float maxSpeed = 50f;           // Optional max speed (set high or remove if you want infinite)

    private Rigidbody rb;

    // Inputs stored each frame
    private float horizontalInput;
    private float verticalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure the Rigidbody won't slow down automatically
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Freeze rotation on X/Z if you only want yaw rotation (top-down style)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // Gather input
        horizontalInput = Input.GetAxis("Horizontal");  // A/D or Left/Right
        verticalInput   = Input.GetAxis("Vertical");    // W/S or Up/Down
    }

    private void FixedUpdate()
    {
        // 1. Rotate the ship (yaw)
        float rotationAmount = horizontalInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion newRot = rb.rotation * Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(newRot);

        // 2. Thrust forward/back
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            Vector3 force = transform.forward * -1 * verticalInput * thrustForce;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        // 3. (Optional) Cap max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}