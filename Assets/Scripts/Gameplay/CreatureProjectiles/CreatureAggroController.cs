using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Put this on each creature (alongside CreatureShooter).
/// The creature will aggro when the player enters radius, chase until within stop distance,
/// then fire using the attached CreatureShooter.
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
    public bool useNavMesh = true;

    [Header("Optional")]
    public bool requireLineOfSight = false;
    public LayerMask losObstacles; // set to your walls/geometry

    private Transform _player;
    private CreatureShooter _shooter;
    private NavMeshAgent _agent;
    private bool _hasAggro;

    void Awake()
    {
        _shooter = GetComponent<CreatureShooter>();

        // Try to find a likely player Transform (first, something tagged Player)
        var tagged = GameObject.FindWithTag("Player");
        if (tagged) _player = tagged.transform;

        // If we still didn't get a player, fall back to anything IDamageable
        if (!_player)
        {
            var anyDmg = FindAnyObjectByType<MonoBehaviour>(FindObjectsInactive.Include);
            if (anyDmg) _player = anyDmg.transform;
        }

        if (_player) _shooter.target = _player;

        if (useNavMesh)
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = stopDistance;
            _agent.autoBraking = true;
            _agent.updateRotation = true;
        }
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

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
            // De-aggro if too far
            if (dist > deAggroRadius)
            {
                _hasAggro = false;
                _shooter.StopFiring();
            }
        }

        if (_hasAggro)
        {
            // Chase until within stop distance
            if (dist > stopDistance)
            {
                if (useNavMesh && _agent != null && _agent.isOnNavMesh)
                {
                    _agent.SetDestination(_player.position);
                }
                else
                {
                    Vector3 toPlayer = (_player.position - transform.position);
                    Vector3 step = toPlayer.normalized * (moveSpeed * Time.deltaTime);
                    if (step.sqrMagnitude > toPlayer.sqrMagnitude)
                        step = toPlayer;
                    transform.position += step;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized, Vector3.up), 10f * Time.deltaTime);
                }
            }
            else
            {
                if (useNavMesh && _agent != null && _agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                }
                // face the player while shooting
                Vector3 look = (_player.position - transform.position);
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), 10f * Time.deltaTime);
            }
        }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _hasAggro ? new Color(1f, 0.4f, 0.4f, 0.5f) : new Color(0.4f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, deAggroRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
