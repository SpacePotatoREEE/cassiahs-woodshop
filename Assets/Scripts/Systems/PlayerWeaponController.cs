using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerLoadout))]
public class PlayerWeaponController : MonoBehaviour
{
    /* ──────────────  INSPECTOR  ────────────── */
    [Header("Weapon Slots")]
    [Min(1)] public int weaponSlotCount = 4;
    public List<Transform> firePoints = new();

    [Header("Global Fire-rate multiplier")]
    public float fireRateMultiplier = 1f;

    [Header("Enemy Detection")]
    public float  detectionRadius = 20f;
    public LayerMask enemyLayer;
    public bool   homingBullet = true;

    /* ──────────────  RUNTIME  ────────────── */
    private PlayerLoadout   loadout;
    private EnemySpaceShip  currentClosestShip;

    private readonly List<Mount> mounts = new();

    // Prefab-defined rotations for each fire-point
    private readonly List<Quaternion> defaultFirePointRotations = new();

    /* one record per physical barrel */
    private class Mount
    {
        public Transform          point;
        public PlayerLoadout.Stack stack;       // weapon + qty
        public float              fireInterval; // seconds between this gun’s shots
        public float              nextTime;     // world-time when it may fire next
    }

    /* ──────────────  LIFECYCLE  ────────────── */
    private void Awake()
    {
        loadout = GetComponent<PlayerLoadout>();
        loadout.OnLoadoutChanged += RebuildMounts;
        RebuildMounts();

        // Cache prefab-defined local rotations
        defaultFirePointRotations.Clear();
        foreach (Transform fp in firePoints)
            defaultFirePointRotations.Add(fp ? fp.localRotation : Quaternion.identity);
    }

    private void OnDestroy() => loadout.OnLoadoutChanged -= RebuildMounts;

    /* ──────────────  UPDATE  ────────────── */
    private bool holdingFire = false;

    private void Update()
    {
        // 1) find target
        UpdateClosestEnemyIndicator();
        Transform tgt = (homingBullet && currentClosestShip)
                      ? currentClosestShip.transform : null;

        // 2) rotate barrels
        AimFirePoints(tgt);

        // 3) handle input + shooting
        bool fireHeld = Input.GetKey(KeyCode.Space);

        if (fireHeld && !holdingFire)               // first press this frame
            SeedMountTimers(Time.time);

        holdingFire = fireHeld;

        if (fireHeld)
            TryFireMountedWeapons(tgt);

        if (Input.GetKeyDown(KeyCode.Tab))
            loadout.NextWeapon();
    }

    /* ──────────────  AIMING  ────────────── */
    /// <summary>
    /// If <paramref name="target"/> exists, point local –Z of every fire-point at it.
    /// Otherwise restore the prefab rotation captured on Awake.
    /// </summary>
    private void AimFirePoints(Transform target)
    {
        if (target)
        {
            for (int i = 0; i < firePoints.Count; i++)
            {
                var fp = firePoints[i];
                if (!fp) continue;

                Vector3 toTarget = target.position - fp.position;
                toTarget.y = 0f;                      // keep barrels level in top-down view
                if (toTarget.sqrMagnitude < 0.0001f) continue;

                // We want –Z to look at the target ⇒ Z looks away ⇒ use –toTarget.
                fp.rotation = Quaternion.LookRotation(-toTarget.normalized, Vector3.up);
            }
        }
        else
        {
            // No active target – snap back to prefab orientation.
            for (int i = 0; i < firePoints.Count; i++)
            {
                var fp = firePoints[i];
                if (!fp) continue;
                fp.localRotation = defaultFirePointRotations[i];
            }
        }
    }

    /* ──────────────  FIRING  ────────────── */
    private void TryFireMountedWeapons(Transform tgt)
    {
        float now = Time.time;

        foreach (var m in mounts)
        {
            if (now < m.nextTime) continue;

            // spawn bullet
            GameObject bullet = Instantiate(
                m.stack.weapon.bulletPrefab,
                m.point.position,
                m.point.rotation);

            // set homing
            if (bullet.TryGetComponent(out HitEnemyBulletController eCtrl))
            {
                eCtrl.isHoming = tgt;
                eCtrl.SetTarget(tgt);
            }
            else if (bullet.TryGetComponent(out HitPlayerBulletController pCtrl))
            {
                pCtrl.isHoming = tgt;
                pCtrl.SetTarget(tgt);
            }

            // schedule next shot, preserving phase but skipping catch-ups
            do { m.nextTime += m.fireInterval; }
            while (m.nextTime <= now);
        }
    }

    /* ──────────────  BUILD MOUNTS  ────────────── */
    private void RebuildMounts()
    {
        mounts.Clear();

        int slotsLeft = Mathf.Min(weaponSlotCount, firePoints.Count);
        int fpIndex   = 0;

        foreach (var stack in loadout.inventory)
        {
            if (slotsLeft == 0) break;

            int copies       = Mathf.Min(stack.qty, slotsLeft);
            float baseRate   = GetFireRate(stack.weapon);
            float interval   = 1f / (baseRate * fireRateMultiplier);
            float phaseStep  = interval / copies;   // evenly stagger

            for (int i = 0; i < copies; i++)
            {
                mounts.Add(new Mount {
                    point        = firePoints[fpIndex++],
                    stack        = stack,
                    fireInterval = interval,
                    nextTime     = Time.time + i * phaseStep
                });
            }
            slotsLeft -= copies;
        }

        loadout.NotifyActiveCountsChanged(GetActiveCountMap());
    }

    private float GetFireRate(WeaponDefinition w)
    {
        var eCtrl = w.bulletPrefab.GetComponent<HitEnemyBulletController>();
        if (eCtrl) return eCtrl.fireRate;

        var pCtrl = w.bulletPrefab.GetComponent<HitPlayerBulletController>();
        if (pCtrl) return pCtrl.fireRate;

        return 2f; // fallback default
    }

    private Dictionary<WeaponDefinition,int> GetActiveCountMap()
    {
        var map = new Dictionary<WeaponDefinition,int>();
        foreach (var m in mounts)
        {
            if (!map.ContainsKey(m.stack.weapon)) map[m.stack.weapon] = 0;
            map[m.stack.weapon]++;
        }
        return map;
    }

    /* ───── CLOSEST ENEMY HELPER (unchanged) ───── */
    private void UpdateClosestEnemyIndicator()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        EnemySpaceShip newClosest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            var enemy = c.GetComponent<EnemySpaceShip>();
            if (!enemy) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist) { minDist = dist; newClosest = enemy; }
        }

        if (newClosest != currentClosestShip)
        {
            currentClosestShip?.SetPlayerClosest(false);
            newClosest?.SetPlayerClosest(true);
            currentClosestShip = newClosest;
        }
    }

    /// <summary>Give every mount a nextTime based on "now + its phase offset"</summary>
    private void SeedMountTimers(float now)
    {
        var perWeaponIndex = new Dictionary<WeaponDefinition,int>();

        foreach (var m in mounts)
        {
            int index = perWeaponIndex.TryGetValue(m.stack.weapon, out var v) ? v : 0;
            perWeaponIndex[m.stack.weapon] = index + 1;

            float phaseStep = m.fireInterval / m.stack.qty;
            m.nextTime = now + index * phaseStep;
        }
    }
}
