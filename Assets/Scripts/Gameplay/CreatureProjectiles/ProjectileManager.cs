using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [Header("Visual Setup")]
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private LayerMask worldLayers;
    [SerializeField] private Transform playerOverride;

    [Header("Simulation")]
    [SerializeField] private int   maxProjectiles      = 5000;
    [SerializeField] private bool  flattenDirectionY   = false; // keep bullets in XZ plane
    [SerializeField] private int   renderLayer         = 0;

    [Header("Player Hit Volume (capsule)")]
    [Tooltip("Horizontal radius of the player’s hit capsule.")]
    [SerializeField] private float playerHitRadius = 0.6f;
    [Tooltip("Half-height of the player’s hit capsule (center to either end).")]
    [SerializeField] private float playerHitHalfHeight = 0.8f;
    [Tooltip("Vertical offset applied to the capsule center (e.g., raise from the feet).")]
    [SerializeField] private float playerHitCenterYOffset = 0.6f;
    [Tooltip("If ON, do a 2D (XZ only) circle test instead of a 3D capsule.")]
    [SerializeField] private bool  useXZOnly = false;

    private struct Projectile
    {
        public float3 pos, dir;
        public float  speed, radius, life;
        public int    damage;
        public float4 color;
        public bool   active;
    }

    private List<Projectile> _p;
    private int              _activeCount;

    private Mesh                        _orbMesh;
    private Material                    _orbMaterial;
    private readonly List<Matrix4x4>    _matrices = new List<Matrix4x4>(1023);
    private readonly List<Vector4>      _colors   = new List<Vector4>(1023);
    private MaterialPropertyBlock       _mpb;

    // Player bindings
    private Transform        _playerT;
    private IDamageable      _playerDmg;
    private PlayerHitEffects _playerEffects;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Pool as a list (you already moved to List)
        _p = new List<Projectile>(maxProjectiles);
        for (int i = 0; i < maxProjectiles; i++) _p.Add(new Projectile { active = false });

        _mpb = new MaterialPropertyBlock();
        ResolveVisuals();
        FindPlayer();
    }

    private void ResolveVisuals()
    {
        if (!orbPrefab) return;
        var mf = orbPrefab.GetComponentInChildren<MeshFilter>(true);
        var rd = orbPrefab.GetComponentInChildren<Renderer>(true);
        if (mf && rd) { _orbMesh = mf.sharedMesh; _orbMaterial = rd.sharedMaterial; }
    }

    void FindPlayer()
    {
        // First check override
        if (playerOverride)
        {
            _playerT = playerOverride;
            _playerDmg = playerOverride.GetComponent<IDamageable>();
            _playerEffects = playerOverride.GetComponent<PlayerHitEffects>();
        
            if (_playerDmg == null)
                _playerDmg = playerOverride.GetComponentInChildren<IDamageable>();
            if (_playerEffects == null)
                _playerEffects = playerOverride.GetComponentInChildren<PlayerHitEffects>();
        }

        // If no override or components not found, search for any GameObject with PlayerStatsHuman
        if (_playerDmg == null)
        {
            var playerStats = FindObjectOfType<PlayerStatsHuman>();
            if (playerStats != null)
            {
                _playerT = playerStats.transform;
                _playerDmg = playerStats.GetComponent<IDamageable>();
                _playerEffects = playerStats.GetComponent<PlayerHitEffects>();
                Debug.Log($"[PM] Found player via PlayerStatsHuman: {playerStats.gameObject.name}");
            }
        }

        // Fallback to tag search
        if (_playerT == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player)
            {
                _playerT = player.transform;
                _playerDmg = player.GetComponent<IDamageable>();
                _playerEffects = player.GetComponent<PlayerHitEffects>();
            }
        }

        Debug.Log($"[PM] Final result: Player={_playerT?.name}, IDamageable={_playerDmg != null}, Effects={_playerEffects != null}");
    }
    
    public void RefreshPlayerReference()
    {
        FindPlayer();
    }

    void Update()
    {
        // IMPORTANT: don’t gate Simulate on _activeCount (bootstrap issue you hit)
        if (_p != null && _p.Count > 0)
        {
            Simulate(Time.deltaTime);
            if (_activeCount > 0) DrawBatches();
        }
    }

    private void Simulate(float dt)
    {
        float3 playerPos = _playerT ? (float3)(_playerT.position) : float3.zero;
        bool   playerInvuln = _playerEffects && _playerEffects.IsInvulnerable();

        _activeCount = 0;

        for (int i = 0; i < _p.Count; i++)
        {
            if (!_p[i].active) continue;

            var b = _p[i];
            b.life -= dt;
            if (b.life <= 0f) { b.active = false; _p[i] = b; continue; }

            // Integrate
            float3 dir = b.dir;
            if (flattenDirectionY) dir.y = 0f;
            float3 oldPos = b.pos;
            float3 newPos = oldPos + dir * (b.speed * dt);

            // World collision (continuous)
            Vector3 s = new Vector3(oldPos.x, oldPos.y, oldPos.z);
            Vector3 e = new Vector3(newPos.x, newPos.y, newPos.z);
            Vector3 ray = e - s;
            float   len = ray.magnitude;
            if (len > 1e-4f)
            {
                if (Physics.SphereCast(s, b.radius, ray / len, out RaycastHit hit, len, worldLayers, QueryTriggerInteraction.Ignore))
                {
                    b.active = false; _p[i] = b; continue;
                }
            }

            // Commit position
            b.pos = newPos;

            // Player collision
            if (_playerT && !playerInvuln)
            {
                
                if (useXZOnly)
                {
                    // 2D (XZ) circle vs point
                    Vector2 bp = new Vector2(b.pos.x, b.pos.z);
                    Vector2 pp = new Vector2(playerPos.x, playerPos.z);
                    float   rr = b.radius + playerHitRadius;
                    if ((bp - pp).sqrMagnitude <= rr * rr)
                    {
                        HitPlayer(ref b, i);
                        continue;
                    }
                }
                else
                {
                    // 3D capsule vs point
                    Vector3 c = new Vector3(playerPos.x, playerPos.y + playerHitCenterYOffset, playerPos.z);
                    Vector3 a = c + Vector3.up * (+playerHitHalfHeight);
                    Vector3 d = c + Vector3.up * (-playerHitHalfHeight);
                    float   rr = b.radius + playerHitRadius;

                    if (PointToSegmentSqr(new Vector3(b.pos.x, b.pos.y, b.pos.z), a, d) <= rr * rr)
                    {
                        HitPlayer(ref b, i);
                        continue;
                    }
                }
            }

            _p[i] = b;
            _activeCount++;
        }
    }

    private void HitPlayer(ref Projectile b, int index)
    {
        Debug.Log($"[PM] HitPlayer: _playerDmg={_playerDmg != null}, _playerEffects={_playerEffects != null}");
    
        // Apply damage first
        if (_playerDmg != null)
        {
            Vector3 hp = new Vector3(b.pos.x, b.pos.y, b.pos.z);
            _playerDmg.ApplyDamage(b.damage, hp, Vector3.up);
            Debug.Log($"[PM] Damage applied: {b.damage}");
        }
    
        // Then trigger flash / shake
        if (_playerEffects != null) 
        {
            _playerEffects.OnHit();
            Debug.Log("[PM] Called OnHit on PlayerHitEffects");
        }
        else
        {
            Debug.LogWarning("[PM] _playerEffects is NULL - no visual effects!");
        }

        // Destroy projectile
        b.active = false;
        _p[index] = b;
    }

    // Distance from point P to segment AB (squared)
    private static float PointToSegmentSqr(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab);
        if (t <= 0f) return (p - a).sqrMagnitude;
        float denom = Vector3.Dot(ab, ab);
        if (t >= denom) return (p - b).sqrMagnitude;
        t /= denom;
        Vector3 proj = a + t * ab;
        return (p - proj).sqrMagnitude;
    }

    private void DrawBatches()
    {
        if (!_orbMesh || !_orbMaterial) return;

        _matrices.Clear();
        _colors.Clear();

        for (int i = 0; i < _p.Count; i++)
        {
            if (!_p[i].active) continue;
            var b = _p[i];

            _matrices.Add(Matrix4x4.TRS(
                new Vector3(b.pos.x, b.pos.y, b.pos.z),
                Quaternion.identity,
                Vector3.one * (b.radius * 2f)));

            // (You can store color per instance if you want later)
            _colors.Add(b.color);

            if (_matrices.Count == 1023) FlushBatch();
        }

        if (_matrices.Count > 0) FlushBatch();
    }

    private void FlushBatch()
    {
        // We don't push _colors yet (material color is fine). Easy to add a _BaseColor array if needed.
        Graphics.DrawMeshInstanced(
            _orbMesh, 0, _orbMaterial, _matrices, null,
            UnityEngine.Rendering.ShadowCastingMode.Off, false, renderLayer, null,
            UnityEngine.Rendering.LightProbeUsage.Off);

        _matrices.Clear();
        _colors.Clear();
    }

    // ───────── Public API ─────────
    public void SpawnProjectile(
        Vector3 position, Vector3 direction,
        float speed = 12f, int damage = 5, float radius = 0.085f, Color color = default, float lifetime = 5f)
    {
        if (color == default) color = Color.red;

        Vector3 dir = direction.sqrMagnitude < 1e-6f ? Vector3.forward : direction.normalized;
        if (flattenDirectionY) dir.y = 0f;

        // Safety nudge forward so we don't start inside the shooter
        position += dir * Mathf.Max(0.25f, radius * 2.5f);

        for (int i = 0; i < _p.Count; i++)
        {
            if (!_p[i].active)
            {
                _p[i] = new Projectile
                {
                    pos    = (float3)position,
                    dir    = (float3)dir,
                    speed  = speed,
                    radius = radius,
                    life   = lifetime,
                    damage = damage,
                    color  = new float4(color.r, color.g, color.b, color.a <= 0f ? 1f : color.a),
                    active = true
                };
                return;
            }
        }
        // Optional: expand list if you want to avoid drops.
    }
}
