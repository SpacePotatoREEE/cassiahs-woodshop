using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Put this on each creature (alongside CreatureShooter).
/// Aggros when the player enters radius, chases until within stop distance,
/// then fires using the attached CreatureShooter.
/// 
/// • If a NavMeshAgent exists AND useNavMesh = true AND the agent is on the mesh,
///   we let the agent drive movement completely (we DO NOT touch its settings).
/// • Otherwise we do simple grounded chasing using raycasts (no pathfinding).
/// </summary>
[RequireComponent(typeof(CreatureShooter))]
public class CreatureAggroController : MonoBehaviour
{
    [Header("Aggro")]
    [Min(0.5f)] public float aggroRadius = 10f;
    [Min(0.5f)] public float deAggroRadius = 14f;
    [Min(0.5f)] public float stopDistance = 4f;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2.5f;
    public bool useNavMesh = true; // if false, always use manual mode even if an agent exists

    [Header("Grounding (manual mode only)")]
    [Tooltip("Layers considered walkable ground/terrain.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Vertical offset so feet sit on the surface (use your mesh/feet offset).")]
    public float footOffset = 0.0f;
    [Tooltip("How far up from the body we start the ground ray.")]
    public float raycastUp = 2.0f;
    [Tooltip("How far down we raycast to find the ground.")]
    public float raycastDown = 50f;
    [Tooltip("Rotate the body to match slope normals (manual mode only).")]
    public bool alignToSlope = true;
    [Tooltip("Higher = snappier slope alignment.")]
    public float alignLerpSpeed = 20f;

    [Header("Optional")]
    public bool requireLineOfSight = false;
    public LayerMask losObstacles; // walls/geometry that block LOS

    private Transform _player;
    private CreatureShooter _shooter;
    private NavMeshAgent _agent;          // never added/configured by this script
    private bool _hasAggro;

    // manual-mode ground cache
    private bool _hasGround;
    private Vector3 _groundPoint;
    private Vector3 _groundNormal = Vector3.up;

    void Awake()
    {
        _shooter = GetComponent<CreatureShooter>();
        _agent   = GetComponent<NavMeshAgent>(); // use existing agent ONLY (do not add/configure)

        // Find player
        var tagged = GameObject.FindWithTag("Player");
        if (tagged) _player = tagged.transform;

        if (!_player)
        {
            // Last-ditch fallback to "anything" so it doesn't nullref in dev
            var any = FindAnyObjectByType<MonoBehaviour>(FindObjectsInactive.Include);
            if (any) _player = any.transform;
        }

        if (_player) _shooter.target = _player;

        // If we're starting without an active agent, snap to ground once to avoid floating
        if (!AgentActive())
            GroundClampImmediate();
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // Aggro gating
        if (!_hasAggro)
        {
            if (dist <= aggroRadius && HasLOS())
            {
                _hasAggro = true;
                if (_shooter.target == null) _shooter.target = _player;
                _shooter.BeginFiring();
            }
        }
        else
        {
            if (dist > deAggroRadius)
            {
                _hasAggro = false;
                _shooter.StopFiring();
            }
        }

        if (!_hasAggro) return;

        bool agentOn = AgentActive();

        // CHASE
        if (dist > stopDistance)
        {
            if (agentOn)
            {
                // Respect user's NavMeshAgent settings — only set destination
                _agent.SetDestination(_player.position);
            }
            else
            {
                // Manual grounded movement (simple straight-line chase)
                Vector3 toPlayer = (_player.position - transform.position);

                // 1) Remove vertical intent
                toPlayer.y = 0f;

                // 2) Project onto slope (if we have a ground normal)
                RaycastGround(out _groundPoint, out _groundNormal, out _hasGround);
                if (_hasGround)
                    toPlayer = Vector3.ProjectOnPlane(toPlayer, _groundNormal);

                // 3) Move
                Vector3 step = toPlayer.normalized * (moveSpeed * Time.deltaTime);
                if (step.sqrMagnitude > toPlayer.sqrMagnitude) step = toPlayer;
                transform.position += step;

                // 4) Face movement/target
                Vector3 faceDir = step.sqrMagnitude > 1e-6f ? step : toPlayer;
                if (faceDir.sqrMagnitude > 1e-6f)
                {
                    Quaternion targetRot = (_hasGround && alignToSlope)
                        ? Quaternion.LookRotation(faceDir.normalized, _groundNormal)
                        : Quaternion.LookRotation(new Vector3(faceDir.x, 0f, faceDir.z).normalized, Vector3.up);

                    float t = 1f - Mathf.Exp(-10f * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
                }
            }
        }
        else
        {
            if (agentOn)
            {
                // Stop agent movement (keep user's rotation settings intact)
                _agent.ResetPath();
            }
            else
            {
                // Manual face while shooting
                Vector3 look = (_player.position - transform.position);
                Quaternion lookRot;
                if (alignToSlope && RaycastGround(out _groundPoint, out _groundNormal, out _hasGround) && _hasGround)
                {
                    Vector3 flat = Vector3.ProjectOnPlane(look, _groundNormal);
                    if (flat.sqrMagnitude > 0.0001f)
                        lookRot = Quaternion.LookRotation(flat.normalized, _groundNormal);
                    else
                        lookRot = transform.rotation;
                }
                else
                {
                    look.y = 0f;
                    lookRot = (look.sqrMagnitude > 0.0001f)
                        ? Quaternion.LookRotation(look.normalized, Vector3.up)
                        : transform.rotation;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
            }
        }

        // IMPORTANT: never clamp/tilt when agent is active — that’s the agent’s job
        if (!agentOn)
            GroundClampVisual();
    }

    // Agent is considered "active" only if:
    // • user wants navmesh, AND
    // • an agent component exists, is enabled, and is on a NavMesh
    private bool AgentActive()
    {
        return useNavMesh && _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }

    private bool HasLOS()
    {
        if (!requireLineOfSight) return true;
        if (_player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        Vector3 toPlayer = (_player.position + Vector3.up * 0.6f) - origin;
        float dist = toPlayer.magnitude;
        if (dist < 0.01f) return true;

        return !Physics.Raycast(origin, toPlayer / dist, dist - 0.1f, losObstacles, QueryTriggerInteraction.Ignore);
    }

    // ───────────────────────────────────────────────────────────
    // Grounding helpers (manual mode only)
    // ───────────────────────────────────────────────────────────

    private bool RaycastGround(out Vector3 hitPoint, out Vector3 hitNormal, out bool hasHit)
    {
        Vector3 origin = transform.position + Vector3.up * raycastUp;

        if (Physics.Raycast(origin, Vector3.down, out var hit, raycastUp + raycastDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            hasHit = true;
            return true;
        }

        hitPoint = default;
        hitNormal = Vector3.up;
        hasHit = false;
        return false;
    }

    /// <summary>One-shot clamp at startup so we don't spawn floating (manual mode only).</summary>
    private void GroundClampImmediate()
    {
        if (RaycastGround(out var p, out _, out var ok) && ok)
        {
            Vector3 pos = transform.position;
            pos.y = p.y + footOffset;
            transform.position = pos;
        }
    }

    /// <summary>Continuously clamp Y and optionally align to slope (manual mode only).</summary>
    private void GroundClampVisual()
    {
        if (RaycastGround(out var p, out var n, out var ok) && ok)
        {
            // Snap Y to ground
            Vector3 pos = transform.position;
            pos.y = p.y + footOffset;
            transform.position = pos;

            if (alignToSlope)
            {
                Vector3 fwd = transform.forward;
                Vector3 projFwd = Vector3.ProjectOnPlane(fwd, n);
                if (projFwd.sqrMagnitude < 1e-6f) projFwd = Vector3.ProjectOnPlane(transform.right, n);

                Quaternion targetRot = Quaternion.LookRotation(projFwd.normalized, n);
                float t = 1f - Mathf.Exp(-alignLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _hasAggro ? new Color(1f, 0.4f, 0.4f, 0.5f) : new Color(0.4f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, deAggroRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Ray gizmo
        Gizmos.color = Color.yellow;
        Vector3 o = transform.position + Vector3.up * raycastUp;
        Gizmos.DrawLine(o, o + Vector3.down * (raycastUp + raycastDown));
    }
}
