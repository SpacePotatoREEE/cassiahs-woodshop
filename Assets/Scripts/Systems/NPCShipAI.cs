using UnityEngine;

[RequireComponent(typeof(ShipDriftController))]
public class NPCShipAI : MonoBehaviour
{
    public Transform planetTransform;

    [Header("Wander Settings")]
    public float wanderMinTime = 2f;   // Min seconds before picking a new direction
    public float wanderMaxTime = 5f;   // Max seconds before picking a new direction
    public float randomTurnChance = 0.1f; 
    // Probability each frame to pick a new heading (small random turns between major changes)

    [Header("Travel & Jump Settings")]
    public float travelDistance = 50f;    // If distance from planet > travelDistance => Stop & Jump
    public float stopThreshold = 0.2f;    // If velocity < stopThreshold => can hyper jump

    public float jumpDuration = 2f;       // Time spent accelerating in hyper jump
    public float jumpSpeedMultiplier = 5f;  // 5x faster than normal
    public float rotationAngleThreshold = 10f; // For reversing velocity

    [Header("Debug")]
    public bool showDebug = false;

    private ShipDriftController ship;
    private Rigidbody rb;

    // Normal vs. hyper jump thrust multipliers
    private float normalThrustForce;
    private float normalRotationSpeed;

    private enum State
    {
        Wander,      // move randomly
        Stop,        // slow down to near zero
        HyperJump,   // accelerate for jump
        Despawn
    }
    private State currentState = State.Wander;

    // Timers & random inputs for wandering
    private float wanderTimer;

    // These store the horizontal/vertical inputs we pass to ShipDriftController
    private float currentHorizontal;
    private float currentVertical;

    // Timers for hyper jump, etc.
    private float jumpTimer;

    private void Awake()
    {
        ship = GetComponent<ShipDriftController>();
        rb   = GetComponent<Rigidbody>();

        // Ensure this is AI-driven
        ship.isAI = true;

        // Cache normal thrust & rotation so we can multiply for jump
        normalThrustForce   = ship.thrustForce;
        normalRotationSpeed = ship.rotationSpeed;
    }

    private void Start()
    {
        // Pick an initial wander direction
        PickNewWanderDirection();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Wander:
                WanderState();
                break;
            case State.Stop:
                StopState();
                break;
            case State.HyperJump:
                HyperJumpState();
                break;
            case State.Despawn:
                Destroy(gameObject);
                break;
        }
    }

    // --------------------------------------------------------------
    // 1) WANDER STATE
    //    Randomly picks directions at intervals, or small chance
    //    each frame to change heading. Accelerates forward.
    //    Once planet distance is big enough => switch to STOP.
    // --------------------------------------------------------------
    private void WanderState()
    {
        if (planetTransform == null)
        {
            currentState = State.Despawn;
            return;
        }

        float dist = Vector3.Distance(transform.position, planetTransform.position);
        if (dist > travelDistance)
        {
            // We traveled far enough from the planet, so let's move to STOP
            currentState = State.Stop;
            return;
        }

        // Decrement wander timer
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f)
        {
            // Time to pick a new heading
            PickNewWanderDirection();
        }
        else
        {
            // Also small random chance each frame to slightly change direction
            if (Random.value < randomTurnChance * Time.fixedDeltaTime)
            {
                PickNewWanderDirection();
            }
        }

        // Feed current AI inputs into ShipDriftController
        ship.SetAIInputs(currentHorizontal, currentVertical);
    }

    // Randomly picks a heading (horizontal) and some forward thrust
    private void PickNewWanderDirection()
    {
        // Reset timer for next major direction change
        wanderTimer = Random.Range(wanderMinTime, wanderMaxTime);

        // Random turn: horizontal in [-1, 1], can be negative or positive
        currentHorizontal = Random.Range(-1f, 1f);

        // Random forward thrust in [0.5 .. 1.0]
        currentVertical = Random.Range(0.5f, 1f);

        if (showDebug)
        {
            Debug.Log($"[Wander] New heading: horiz={currentHorizontal}, " + 
                      $"vert={currentVertical}, timer={wanderTimer}");
        }
    }

    // --------------------------------------------------------------
    // 2) STOP STATE
    //    Turn opposite of velocity, apply reverse thrust
    //    until speed < stopThreshold. Then do hyper jump.
    // --------------------------------------------------------------
    private void StopState()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (showDebug)
            Debug.Log($"[Stop] Speed={speed}, thresh={stopThreshold}");

        // If nearly stopped, go to hyper jump
        if (speed < stopThreshold)
        {
            rb.linearVelocity = Vector3.zero;
            ship.SetAIInputs(0f, 0f);

            // Face "screen up" (top-down). Example: y-rotation=0
            transform.rotation = Quaternion.Euler(0, 0, 0);

            // Switch to HyperJump
            jumpTimer = jumpDuration;
            currentState = State.HyperJump;
            return;
        }

        // 1) Turn opposite velocity
        Vector3 brakeDir = -velocity.normalized;
        TurnTowards(brakeDir);

        // 2) Check angle between code’s “forward” (-transform.forward) & brakeDir
        float angle = Vector3.Angle(-transform.forward, brakeDir);

        // If near 180, apply forward input => physically "reverse"
        float forwardInput = (angle > (180f - rotationAngleThreshold)) ? 1f : 0f;

        // Combine with horizontal from TurnTowards
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    // --------------------------------------------------------------
    // 3) HYPERJUMP STATE
    //    Temporarily boost thrust and accelerate for jumpDuration,
    //    then despawn.
    // --------------------------------------------------------------
    private void HyperJumpState()
    {
        jumpTimer -= Time.fixedDeltaTime;
        if (jumpTimer <= 0f)
        {
            currentState = State.Despawn;
            return;
        }

        // Temporarily boost thrust
        ship.thrustForce   = normalThrustForce   * jumpSpeedMultiplier;
        ship.rotationSpeed = normalRotationSpeed * jumpSpeedMultiplier;

        // Full forward input => accelerate
        ship.SetAIInputs(0f, 1f);
    }

    // --------------------------------------------------------------
    // 4) DESPAWN
    //    We just destroy the object.
    // --------------------------------------------------------------
    // (Handled in FixedUpdate case State.Despawn)

    // --------------------------------------------------------------
    // HELPER: Turn smoothly towards a desired direction 
    //         using horizontal input + forced rotation
    // --------------------------------------------------------------
    private void TurnTowards(Vector3 desiredDir)
    {
        // Flatten if purely top-down
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude < 0.001f) return;

        // "code forward" = -transform.forward in ShipDriftController
        Vector3 currentForward = -transform.forward;

        // Signed angle from currentForward to desiredDir
        float signedAngle = Vector3.SignedAngle(currentForward, desiredDir, Vector3.up);

        // Decide turning left/right
        float horizontal;
        if      (signedAngle >  5f) horizontal =  1f;
        else if (signedAngle < -5f) horizontal = -1f;
        else                        horizontal =  0f;

        // Store it in currentHorizontal so the rest of the script sees it
        currentHorizontal = horizontal;

        // Also forcibly rotate for immediate turning
        float step = ship.rotationSpeed * Time.fixedDeltaTime;
        Quaternion currentRot = rb.rotation;
        Quaternion targetRot  = Quaternion.LookRotation(desiredDir, Vector3.up);
        Quaternion newRot     = Quaternion.RotateTowards(currentRot, targetRot, step);
        rb.MoveRotation(newRot);
    }
}
