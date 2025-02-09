using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipDriftController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 10f;
    public float rotationSpeed = 80f;
    public float maxSpeed = 50f;

    [Header("AI Override")]
    public bool isAI = false;
    private float aiHorizontal = 0f;
    private float aiVertical = 0f;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Only freeze rotation X/Z for top-down. Leave Y free.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        if (!isAI)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput   = Input.GetAxis("Vertical");
        }
        else
        {
            horizontalInput = aiHorizontal;
            verticalInput   = aiVertical;
        }
    }

    private void FixedUpdate()
    {
        // Rotate around Y
        float rotationAmount = horizontalInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion newRot = rb.rotation * Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(newRot);

        // Thrust
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            // Here we treat "forward" as negative Z
            Vector3 force = transform.forward * -1f * verticalInput * thrustForce;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        // Cap speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    public void SetAIInputs(float aiH, float aiV)
    {
        aiHorizontal = aiH;
        aiVertical   = aiV;
    }
}
