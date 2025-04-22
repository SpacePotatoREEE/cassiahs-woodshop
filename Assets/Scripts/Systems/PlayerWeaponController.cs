using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerLoadout))]
public class PlayerWeaponController : MonoBehaviour
{
    /* ---------------- Inspector ---------------- */
    [Header("Weapon Slots")]
    [Min(1)] public int weaponSlotCount = 4;
    public List<Transform> firePoints = new();

    [Header("Global Fire‑rate multiplier")]
    public float fireRateMultiplier = 1f;

    [Header("Enemy Detection")]
    public float detectionRadius = 20f;
    public LayerMask enemyLayer;
    public bool homingBullet = true;

    /* ---------------- runtime ---------------- */
    private PlayerLoadout loadout;
    private EnemySpaceShip currentClosestShip;

    private readonly List<Mount> mounts = new();

    /* one record per physical barrel */
    private class Mount
    {
        public Transform          point;
        public PlayerLoadout.Stack stack;            // weapon + qty
        public float              fireInterval;      // seconds between this gun’s shots
        public float              nextTime;          // world‑time when it may fire next
    }

    /* ---------------- life‑cycle ---------------- */
    private void Awake()
    {
        loadout = GetComponent<PlayerLoadout>();
        loadout.OnLoadoutChanged += RebuildMounts;
        RebuildMounts();
    }
    private void OnDestroy() => loadout.OnLoadoutChanged -= RebuildMounts;

    /* ---------------- update ---------------- */
    private void Update()
    {
        UpdateClosestEnemyIndicator();

        if (Input.GetKey(KeyCode.Space))
            TryFireMountedWeapons();

        if (Input.GetKeyDown(KeyCode.Tab))
            loadout.NextWeapon();
    }

    /* ---------------- firing ---------------- */
    private void TryFireMountedWeapons()
    {
        float now   = Time.time;
        Transform tgt = (homingBullet && currentClosestShip)
                      ? currentClosestShip.transform : null;

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

            // schedule next shot, preserving the phase but skipping catch‑up bursts
            do
            {
                m.nextTime += m.fireInterval;     // advance one interval
            }
            while (m.nextTime <= now);            // keep skipping until it's ahead of 'now' 
        }
    }

    /* ---------------- build mounts ---------------- */
    private void RebuildMounts()
    {
        mounts.Clear();

        int slotsLeft = Mathf.Min(weaponSlotCount, firePoints.Count);
        int fpIndex   = 0;

        foreach (var stack in loadout.inventory)
        {
            if (slotsLeft == 0) break;

            // use as many copies of this weapon as we still have slots
            int copies = Mathf.Min(stack.qty, slotsLeft);

            // compute per‑copy interval and phase offset
            // interval = 1 / (rate * multiplier)
            float baseRate    = GetFireRate(stack.weapon);
            float intervalOne = 1f / (baseRate * fireRateMultiplier);
            float phaseStep   = intervalOne / copies;        // evenly stagger

            for (int i = 0; i < copies; i++)
            {
                mounts.Add(new Mount {
                    point        = firePoints[fpIndex++],
                    stack        = stack,
                    fireInterval = intervalOne,
                    nextTime     = Time.time + i * phaseStep
                });
            }
            slotsLeft -= copies;
        }

        // notify HUD
        loadout.NotifyActiveCountsChanged(GetActiveCountMap());
    }

    private float GetFireRate(WeaponDefinition w)
    {
        var eCtrl = w.bulletPrefab.GetComponent<HitEnemyBulletController>();
        if (eCtrl) return eCtrl.fireRate;

        var pCtrl = w.bulletPrefab.GetComponent<HitPlayerBulletController>();
        if (pCtrl) return pCtrl.fireRate;

        return 2f;               // fallback default
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

    /* ---------------- closest enemy helper (unchanged) ---------------- */
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
}
