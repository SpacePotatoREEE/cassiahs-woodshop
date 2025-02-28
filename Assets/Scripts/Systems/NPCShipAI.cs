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
    [Tooltip("Reference to the player’s transform (the target).")]
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

    private MeshRenderer[] meshRenderers;
    private Collider[] shipColliders;

    // ========== TOP-LEVEL STATES ==========
    private enum PrimaryState
    {
        Normal,     // We'll run secondary states here (Cruise, Jumpable, etc.)
        Attack,
        Disabled,
        Destroying,
        Despawn
    }
    private PrimaryState primaryState = PrimaryState.Normal;

    // ========== SUB-STATES WHEN Normal ==========
    private enum SecondaryState
    {
        Cruise,
        Jumpable,
        PrepareJump,
        IsJumping
    }
    private SecondaryState secondaryState = SecondaryState.Cruise;

    // Timers for wandering / jumping
    private float wanderTimer;
    private float jumpTimer;

    // AI inputs
    private float currentHorizontal;
    private float currentVertical;

    // For disabling
    private float disabledTimer;
    private Vector3 initialVel;

    // Attack distance
    private float finalAttackDistance;

    private void Awake()
    {
        ship = GetComponent<ShipDriftController>();
        rb   = GetComponent<Rigidbody>();
        enemyShip = GetComponent<EnemySpaceShip>();

        ship.isAI = true;

        normalThrustForce   = ship.thrustForce;
        normalRotationSpeed = ship.rotationSpeed;

        // gather child mesh & colliders for disabling
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        shipColliders = GetComponentsInChildren<Collider>();
        
        // Attempt to find the player by tag
        GameObject potentialPlayer = GameObject.FindWithTag("Player");
        if (potentialPlayer == null)
        {
            Debug.LogWarning("[NPCShipAI] No GameObject found with tag 'Player'!");
        }
        else
        {
            // Safety check: if it's ourselves, that's a problem
            if (potentialPlayer == this.gameObject)
            {
                Debug.LogError("[NPCShipAI] Found 'player' is this same object! Check your tags.");
            }
            else
            {
                player = potentialPlayer;
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
        switch (primaryState)
        {
            case PrimaryState.Attack:
                AttackUpdate();
                break;
            case PrimaryState.Disabled:
                DisabledUpdate();
                break;
            case PrimaryState.Destroying:
                // do nothing here (no secondary states)
                break;
            case PrimaryState.Despawn:
                Destroy(gameObject);
                break;
            case PrimaryState.Normal:
            default:
                // run the normal sub-state machine
                RunSecondaryState();
                break;
        }
    }

    //=========================================
    //  PRIMARY STATES
    //=========================================

    public void EnterAttackState()
    {
        // If we are already disabled, destroying or despawn => skip
        if (primaryState == PrimaryState.Disabled
         || primaryState == PrimaryState.Destroying
         || primaryState == PrimaryState.Despawn)
        {
            return;
        }

        primaryState = PrimaryState.Attack;
        isAttacking = true;

        finalAttackDistance = Random.Range(attackDistanceMin, attackDistanceMax);
        Debug.Log($"[NPCShipAI] Attack state! distance ~{finalAttackDistance:F1}");
    }

    public void EnterDisabledState()
    {
        // If we're already disabled/destroying/despawn => skip
        if (primaryState == PrimaryState.Disabled
         || primaryState == PrimaryState.Destroying
         || primaryState == PrimaryState.Despawn)
        {
            return;
        }

        primaryState = PrimaryState.Disabled;
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

    public void EnterDestroyingState()
    {
        if (primaryState == PrimaryState.Destroying || primaryState == PrimaryState.Despawn)
            return;

        primaryState = PrimaryState.Destroying;
        if (enemyShip != null && enemyShip.disabledIndicator != null)
            enemyShip.disabledIndicator.SetActive(false);

        rb.linearVelocity = Vector3.zero;
        ship.SetAIInputs(0f, 0f);
        StartCoroutine(DestroySequence());
    }

    // AttackUpdate: Remains in Attack unless we forcibly leave it
    private void AttackUpdate()
    {
        if (aiWeaponController == null || player == null)
        {
            // no target => do nothing, but remain in Attack if you want
            return;
        }

        // optional check if the AI's own HP is low => disabled
        // if (enemyShip != null && some condition) EnterDisabledState();

        // 1) Check if Player is truly disabled/destroyed
        PlayerSpaceShipStats pStats = player.GetComponent<PlayerSpaceShipStats>();
        // If we REALLY want to revert to Normal if player is disabled, we keep this check
        if (pStats != null && pStats.IsDisabledOrDestroyed())
        {
            Debug.Log("[NPCShipAI] Player truly disabled => revert to Normal/Cruise");
            isAttacking = false;
            primaryState = PrimaryState.Normal;
            secondaryState = SecondaryState.Cruise;
            return;
        }

        // 2) Move to maintain distance
        Vector3 toPlayer = (player.transform.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist > finalAttackDistance + 1f)
        {
            ApproachTarget(playerTransform.position);
        }
        else if (dist < finalAttackDistance - 1f)
        {
            BackAwayFromTarget(playerTransform.position);
        }
        else
        {
            HoldPosition();
        }

        // 3) Shoot
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            aiWeaponController.FireAt(player.transform);
            fireTimer = 1f / fireRate;
        }
    }

    private void DisabledUpdate()
    {
        disabledTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(disabledTimer / disabledSlowDownTime);

        Vector3 newVel = Vector3.Lerp(initialVel, Vector3.zero, t);
        rb.linearVelocity = newVel;

        ship.SetAIInputs(0f, 0f);
    }

    private IEnumerator DestroySequence()
    {
        GameObject destroyingFX = null;
        if (destroyingParticlePrefab != null)
        {
            destroyingFX = Instantiate(destroyingParticlePrefab, transform.position, Quaternion.identity);
            destroyingFX.transform.SetParent(transform);
        }

        yield return new WaitForSeconds(destroyingParticleDuration);
        if (destroyingFX != null) Destroy(destroyingFX);

        GameObject explosionFX = null;
        if (finalExplosionPrefab != null)
            explosionFX = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);

        DisableShipVisuals();
        yield return new WaitForSeconds(finalExplosionDuration);

        primaryState = PrimaryState.Despawn;
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

    //=========================================
    //  SECONDARY STATES (only run in Normal)
    //=========================================
    private void RunSecondaryState()
    {
        switch (secondaryState)
        {
            case SecondaryState.Cruise:      CruiseUpdate();      break;
            case SecondaryState.Jumpable:    JumpableUpdate();    break;
            case SecondaryState.PrepareJump: PrepareJumpUpdate(); break;
            case SecondaryState.IsJumping:   IsJumpingUpdate();   break;
        }
    }

    private void CruiseUpdate()
    {
        if (planetTransform == null)
        {
            primaryState = PrimaryState.Despawn;
            return;
        }

        float dist = Vector3.Distance(transform.position, planetTransform.position);
        if (dist > jumpableDistance)
        {
            secondaryState = SecondaryState.Jumpable;
            chosenExtraDistance = Random.Range(minExtraDistance, maxExtraDistance);
            return;
        }
        HandleWandering();
    }

    private void JumpableUpdate()
    {
        if (planetTransform == null)
        {
            primaryState = PrimaryState.Despawn;
            return;
        }
        float dist = Vector3.Distance(transform.position, planetTransform.position);
        if (dist > jumpableDistance + chosenExtraDistance)
        {
            secondaryState = SecondaryState.PrepareJump;
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
            secondaryState = SecondaryState.IsJumping;
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
            primaryState = PrimaryState.Despawn;
            return;
        }

        ship.maxSpeed      = 999f;
        ship.thrustForce   = normalThrustForce   * jumpSpeedMultiplier;
        ship.rotationSpeed = normalRotationSpeed * jumpSpeedMultiplier;
        ship.SetAIInputs(0f, 1f);
    }

    //=========================================
    //  Movement Helpers for Attack
    //=========================================
    private void ApproachTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        dir.Normalize();

        TurnTowards(dir);
        float forwardInput = 1f * attackThrustFactor;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    private void BackAwayFromTarget(Vector3 targetPos)
    {
        Vector3 dirToTarget = (targetPos - transform.position);
        dirToTarget.y = 0f;
        dirToTarget.Normalize();

        Vector3 awayDir = -dirToTarget;
        TurnTowards(awayDir);

        float forwardInput = 1f * attackThrustFactor;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    private void HoldPosition()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (speed < 0.2f)
        {
            ship.SetAIInputs(0f, 0f);
            return;
        }

        Vector3 brakeDir = -velocity.normalized;
        TurnTowards(brakeDir);
        float forwardInput = 1f;
        ship.SetAIInputs(currentHorizontal, forwardInput);
    }

    private void TurnTowards(Vector3 desiredDir)
    {
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude < 0.001f) return;

        Vector3 currentForward = -transform.forward;
        float signedAngle = Vector3.SignedAngle(currentForward, desiredDir, Vector3.up);

        float h;
        if      (signedAngle > 5f)  h =  1f;
        else if (signedAngle < -5f) h = -1f;
        else                        h =  0f;

        currentHorizontal = h;

        float step = ship.rotationSpeed * Time.fixedDeltaTime;
        Quaternion currentRot = rb.rotation;
        Quaternion targetRot  = Quaternion.LookRotation(-desiredDir, Vector3.up);
        Quaternion newRot     = Quaternion.RotateTowards(currentRot, targetRot, step);
        rb.MoveRotation(newRot);
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
        Gizmos.color = GetPrimaryStateColor(primaryState);
        Gizmos.DrawWireSphere(transform.position, debugRadius);
    }

    private Color GetPrimaryStateColor(PrimaryState s)
    {
        switch (s)
        {
            case PrimaryState.Normal:     return Color.green;
            case PrimaryState.Attack:     return Color.red;
            case PrimaryState.Disabled:   return Color.magenta;
            case PrimaryState.Destroying: return Color.black;
            case PrimaryState.Despawn:    return Color.gray;
            default:                      return Color.white;
        }
    }
}
