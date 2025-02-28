using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ShipDriftController))]
public class NPCShipAI : MonoBehaviour
{
    [Header("Planet Reference")]
    public Transform planetTransform;

    [Header("Distances & Speeds")]
    public float jumpableDistance = 20f;
    public float minExtraDistance = 5f;
    public float maxExtraDistance = 15f;

    [Header("Jump Parameters")]
    public float maxVelocityToJump = 0.5f;
    public float jumpDuration = 2f;
    public float jumpSpeedMultiplier = 2f;

    [Header("Cruise / Wandering")]
    public float wanderMinTime = 2f;
    public float wanderMaxTime = 5f;
    public float randomTurnChance = 0.1f;

    [Header("Disabled Settings")]
    public float disabledSlowDownTime = 2f;

    [Header("Destroying Settings")]
    public GameObject destroyingParticlePrefab;
    public float destroyingParticleDuration = 2f;
    public GameObject finalExplosionPrefab;
    public float finalExplosionDuration = 1f;
    
    [Header("Attack Settings")]
    public AIWeaponController aiWeaponController;
    [Tooltip("The player's transform (the target).")]
    public Transform playerTransform;
    [Tooltip("Shots per second")]
    public float fireRate = 1f;
    private float fireTimer = 0f;

    [Tooltip("Minimum distance the AI wants to maintain from the player.")]
    public float attackDistanceMin = 5f;
    [Tooltip("Maximum distance the AI wants to maintain from the player.")]
    public float attackDistanceMax = 10f;
    [Tooltip("How strongly we accelerate toward/away from the player.")]
    public float attackThrustFactor = 1.0f;

    [Header("Debug Settings")]
    public bool showDebug = true;
    public float debugRadius = 1f;

    private ShipDriftController ship;
    private Rigidbody rb;
    private EnemySpaceShip enemyShip;

    private GameObject player;

    private float normalThrustForce;
    private float normalRotationSpeed;
    private float chosenExtraDistance;
    private bool isAttacking = false;

    private enum State
    {
        Cruise,
        Jumpable,
        PrepareJump,
        IsJumping,
        Disabled,
        Destroying,
        Attack,
        Despawn
    }
    private State currentState = State.Cruise;

    // Timers
    private float wanderTimer;
    private float jumpTimer;

    // AI inputs
    private float currentHorizontal;
    private float currentVertical;

    // Disabled
    private float disabledTimer;
    private Vector3 initialVel;

    // For visuals
    private MeshRenderer[] meshRenderers;
    private Collider[] shipColliders;

    // The final distance we want to maintain from the player (picked randomly once).
    private float finalAttackDistance;

    private void Awake()
    {
        ship = GetComponent<ShipDriftController>();
        rb   = GetComponent<Rigidbody>();
        enemyShip = GetComponent<EnemySpaceShip>();

        ship.isAI = true;

        normalThrustForce   = ship.thrustForce;
        normalRotationSpeed = ship.rotationSpeed;

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        shipColliders = GetComponentsInChildren<Collider>();
        
        // Attempt to find the player by tag
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[AIWeaponController] No GameObject found with tag 'Player'!");
        }
        else
        {
            // Safety check: if the object we found is ourselves, that's a problem
            if (player == this.gameObject)
            {
                Debug.LogError("[AIWeaponController] Found 'player' is this same object! Check your tags.");
            }
            else
            {
                playerTransform = player.transform;
            }
        }
    }

    private void Start()
    {
        PickNewWanderDirection();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Cruise:      CruiseUpdate();      break;
            case State.Jumpable:    JumpableUpdate();    break;
            case State.PrepareJump: PrepareJumpUpdate(); break;
            case State.IsJumping:   IsJumpingUpdate();   break;
            case State.Disabled:    DisabledUpdate();    break;
            case State.Destroying:  /* handled by coroutine */ break;
            case State.Attack:      AttackUpdate();      break;
            case State.Despawn:     Destroy(gameObject); break;
        }
    }

    // --------------------------------------
    //  PUBLIC: ENTER ATTACK STATE
    // --------------------------------------
    public void EnterAttackState()
    {
        if (currentState == State.Disabled || currentState == State.Despawn)
            return;

        currentState = State.Attack;
        isAttacking = true;

        // Pick a random "final distance" to maintain
        finalAttackDistance = Random.Range(attackDistanceMin, attackDistanceMax);
        Debug.Log($"[NPCShipAI] Attack state! Maintaining distance ~{finalAttackDistance:F1}");
    }

    // --------------------------------------
    //  ATTACK LOGIC
    // --------------------------------------
    private void AttackUpdate()
    {
        if (aiWeaponController == null || player == null)
        {
            // fallback: do nothing
            return;
        }

        // Check if the player is disabled or destroyed
        PlayerSpaceShipStats playerStats = player.GetComponent<PlayerSpaceShipStats>();
        if (playerStats != null && playerStats.IsDisabledOrDestroyed())
        {
            // If the player is disabled, revert to Cruise
            Debug.Log("[NPCShipAI] Player is disabled, stopping attack => Cruise");
            isAttacking = false;
            currentState = State.Cruise;
            return;
        }

        // 1) Move/rotate to maintain finalAttackDistance from the player
        Vector3 toPlayer = (player.transform.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist > finalAttackDistance + 1f)
        {
            // We are too far: move forward to close in
            //  => rotate to face player, apply forward thrust
            ApproachTarget(playerTransform.position);
        }
        else if (dist < finalAttackDistance - 1f)
        {
            // We are too close: back away or brake to near zero
            // => rotate 180 from player, apply forward to reverse
            BackAwayFromTarget(playerTransform.position);
        }
        else
        {
            // We are within the "comfort zone" => just brake slightly to keep velocity near 0
            HoldPosition();
        }

        fireTimer -= Time.deltaTime;
        // 2) Fire at the player
        if (fireTimer <= 0f)
        {
            aiWeaponController.FireAt(player.transform);
            fireTimer = 1f / fireRate;
        }
    }

    /// <summary> Turn & thrust TOWARD the target position. </summary>
    private void ApproachTarget(Vector3 targetPos)
    {
        // 1) Aim
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f; // top-down+
        dir.Normalize();

        // Turn towards that direction
        TurnTowards(dir);

        // Apply forward thrust: we do a partial factor
        float forwardInput = 1f * attackThrustFactor;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    /// <summary> Turn & thrust AWAY from the target position (to back up). </summary>
    private void BackAwayFromTarget(Vector3 targetPos)
    {
        Vector3 dirToTarget = (targetPos - transform.position);
        dirToTarget.y = 0f;
        dirToTarget.Normalize();

        // Opposite direction
        Vector3 awayDir = -dirToTarget;

        // Turn towards awayDir
        TurnTowards(awayDir);

        // full forward input => physically "reverse"
        float forwardInput = 1f * attackThrustFactor;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    /// <summary> Brake to near zero by reversing velocity. </summary>
    private void HoldPosition()
    {
        // read velocity
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (speed < 0.2f)
        {
            // Already basically still => no input
            ship.SetAIInputs(0f, 0f);
            return;
        }

        // otherwise, turn opposite velocity & apply forward => brake
        Vector3 brakeDir = -velocity.normalized;
        TurnTowards(brakeDir);
        float forwardInput = 1f;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    /// <summary> Utility: Turn smoothly towards a desired direction. </summary>
    private void TurnTowards(Vector3 desiredDir)
    {
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude < 0.001f) return;

        // Our "forward" is negative Z in ShipDriftController
        Vector3 currentForward = -transform.forward;
        float signedAngle = Vector3.SignedAngle(currentForward, desiredDir, Vector3.up);

        float h;
        if (signedAngle > 5f)      h =  1f;
        else if (signedAngle < -5f)h = -1f;
        else                       h =  0f;

        currentHorizontal = h;

        // Also forcibly rotate for immediate turning
        float step = ship.rotationSpeed * Time.fixedDeltaTime;
        Quaternion currentRot = rb.rotation;
        Quaternion targetRot  = Quaternion.LookRotation(-desiredDir, Vector3.up); 
        // note the negative if your forward is -Z
        Quaternion newRot     = Quaternion.RotateTowards(currentRot, targetRot, step);
        rb.MoveRotation(newRot);
    }

    // --------------------------------------
    // DISABLED / DESTROYING (unchanged)
    // --------------------------------------
    public void EnterDisabledState()
    {
        if (currentState == State.Disabled || currentState == State.Destroying || currentState == State.Despawn)
            return;

        currentState = State.Disabled;

        // Mark the ship as disabled so targetingIndicator can't re‐enable
        if (enemyShip != null)
        {
            enemyShip.isDisabled = true;
            if (enemyShip.disabledIndicator != null)
                enemyShip.disabledIndicator.SetActive(true);
            if (enemyShip.targetingIndicator != null)
                enemyShip.targetingIndicator.SetActive(false);
        }

        initialVel = rb.linearVelocity;
        disabledTimer = 0f;
        ship.SetAIInputs(0f, 0f);
    }

    private void DisabledUpdate()
    {
        disabledTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(disabledTimer / disabledSlowDownTime);

        Vector3 newVel = Vector3.Lerp(initialVel, Vector3.zero, t);
        rb.linearVelocity = newVel;

        ship.SetAIInputs(0f, 0f);
    }

    public void EnterDestroyingState()
    {
        if (currentState == State.Destroying || currentState == State.Despawn)
            return;

        currentState = State.Destroying;

        // turn off disabled indicator if it was on
        if (enemyShip != null && enemyShip.disabledIndicator != null)
        {
            enemyShip.disabledIndicator.SetActive(false);
        }

        rb.linearVelocity = Vector3.zero;
        ship.SetAIInputs(0f, 0f);
        StartCoroutine(DestroySequence());
    }

    private IEnumerator DestroySequence()
    {
        // spawn "destroying" effect
        GameObject destroyingFX = null;
        if (destroyingParticlePrefab != null)
        {
            destroyingFX = Instantiate(destroyingParticlePrefab, transform.position, Quaternion.identity);
            destroyingFX.transform.SetParent(transform);
        }

        yield return new WaitForSeconds(destroyingParticleDuration);

        if (destroyingFX != null) Destroy(destroyingFX);

        // final explosion
        GameObject explosionFX = null;
        if (finalExplosionPrefab != null)
        {
            explosionFX = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
        }

        DisableShipVisuals();

        yield return new WaitForSeconds(finalExplosionDuration);

        currentState = State.Despawn;
    }

    private void DisableShipVisuals()
    {
        if (meshRenderers != null)
        {
            foreach (var mr in meshRenderers)
                mr.enabled = false;
        }
        if (shipColliders != null)
        {
            foreach (var col in shipColliders)
                col.enabled = false;
        }
    }

    // --------------------------------------
    // CRUISE / JUMPABLE / PREPAREJUMP / ISJUMPING
    // (unchanged from your script)
    // --------------------------------------
    private float wanderTimerRemaining;
    private void CruiseUpdate()
    {
        if (planetTransform == null)
        {
            currentState = State.Despawn;
            return;
        }
        float dist = Vector3.Distance(transform.position, planetTransform.position);
        if (dist > jumpableDistance)
        {
            currentState = State.Jumpable;
            chosenExtraDistance = Random.Range(minExtraDistance, maxExtraDistance);
            return;
        }
        HandleWandering();
    }

    private void JumpableUpdate()
    {
        if (planetTransform == null)
        {
            currentState = State.Despawn;
            return;
        }
        float dist = Vector3.Distance(transform.position, planetTransform.position);
        if (dist > jumpableDistance + chosenExtraDistance)
        {
            currentState = State.PrepareJump;
            return;
        }
        HandleWandering();
    }

    private void PrepareJumpUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (speed < maxVelocityToJump)
        {
            rb.linearVelocity = Vector3.zero;
            ship.SetAIInputs(0f, 0f);

            jumpTimer = jumpDuration;
            currentState = State.IsJumping;
            return;
        }

        Vector3 velDir = velocity.normalized;
        Quaternion targetRot = Quaternion.LookRotation(velDir, Vector3.up);

        float step = ship.rotationSpeed * Time.fixedDeltaTime;
        Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, step);
        rb.MoveRotation(newRot);

        // braking
        ship.SetAIInputs(0f, 1f);
    }

    private void IsJumpingUpdate()
    {
        jumpTimer -= Time.fixedDeltaTime;
        if (jumpTimer <= 0f)
        {
            currentState = State.Despawn;
            return;
        }

        ship.maxSpeed      = 999f;
        ship.thrustForce   = normalThrustForce   * jumpSpeedMultiplier;
        ship.rotationSpeed = normalRotationSpeed * jumpSpeedMultiplier;
        ship.SetAIInputs(0f, 1f);
    }

    private void HandleWandering()
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f)
        {
            PickNewWanderDirection();
        }
        else
        {
            if (Random.value < randomTurnChance * Time.fixedDeltaTime)
                PickNewWanderDirection();
        }
        ship.SetAIInputs(currentHorizontal, currentVertical);
    }

    private void PickNewWanderDirection()
    {
        wanderTimer = Random.Range(wanderMinTime, wanderMaxTime);
        currentHorizontal = Random.Range(-1f, 1f);
        currentVertical   = Random.Range(0.5f, 1f);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        Gizmos.color = GetStateColor(currentState);
        Gizmos.DrawWireSphere(transform.position, debugRadius);
    }

    private Color GetStateColor(State s)
    {
        switch (s)
        {
            case State.Cruise:      return Color.green;
            case State.Jumpable:    return Color.yellow;
            case State.PrepareJump: return Color.blue;
            case State.IsJumping:   return Color.gray;
            case State.Disabled:    return Color.magenta;
            case State.Destroying:  return Color.black;
            case State.Attack:      return Color.red;
            default:                return Color.white;
        }
    }
}
