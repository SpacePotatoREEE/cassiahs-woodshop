//
// AreaSpawnManager.cs   – 2025‑08‑06  (NaN / Infinity safe)
//
// • Uses a bool‑returning TryGetGroundPoint() instead of “positiveInfinity”
// • Absolutely no GameObject is spawned unless a valid, finite point is found
//
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AreaVolume))]
public class AreaSpawnManager : MonoBehaviour
{
    [Tooltip("Min distance between spawns (m).")]
    [SerializeField] private float spawnSeparation = 2f;

    [Tooltip("Max ground slope (deg) allowed for spawns.")]
    [Range(0f, 89f)] [SerializeField] private float maxSpawnSlope = 45f;

    private AreaVolume area;
    private readonly List<GameObject> active = new();
    private bool  playerInside;
    private float timer;
    private float cosMaxSlope;

    /* ───────────────────────────────────────────── */

    private void Awake()
    {
        area        = GetComponent<AreaVolume>();
        cosMaxSlope = Mathf.Cos(maxSpawnSlope * Mathf.Deg2Rad);
    }

    private void Update()
    {
        if (!playerInside) return;

        timer += Time.deltaTime;

        if (timer >= area.Profile.SpawnInterval &&
            active.Count < area.Profile.MaxActiveUnits)
        {
            SpawnOneUnit();
            timer = 0f;
        }

        active.RemoveAll(g => g == null);
    }

    private void OnTriggerEnter(Collider other)
    { if (other.CompareTag("Player")) playerInside = true; }

    private void OnTriggerExit(Collider other)
    { if (other.CompareTag("Player")) playerInside = false; }

    /* ───────────────────────────────────────────── */

    private void SpawnOneUnit()
    {
        // 1) pick table + entry
        var tables = area.Profile.SpawnTables
            .Where(t => area.Profile.AreaLevel >= t.MinAreaLevel &&
                        area.Profile.AreaLevel <= t.MaxAreaLevel).ToList();
        if (tables.Count == 0) return;

        var   table = tables[Random.Range(0, tables.Count)];
        var   entry = WeightedPick(table.Units);
        if (entry?.Template == null) return;

        // 2) find valid position
        Vector3 pos;
        for (int i = 0; i < 16; i++)            // 16 attempts max
        {
            if (area.Profile.Environment == AreaProfile.EnvironmentType.Space)
                pos = RandomPointInSphere();
            else if (!TryGetGroundPoint(entry.Template.GroundSpawnOffset, out pos))
                continue;                       // slope too steep or ray miss

            if (!IsFarFromOthers(pos)) continue;

            active.Add(UnitFactory.SpawnUnit(entry, area.Profile, pos, Quaternion.identity));
            return;
        }
    }

    /* ───────── helpers ───────── */

    private static SpawnTable.Entry WeightedPick(IEnumerable<SpawnTable.Entry> list)
    {
        int total = list.Sum(e => e.Weight);
        int pick  = Random.Range(0, total);
        int acc   = 0;
        foreach (var e in list) { acc += e.Weight; if (pick < acc) return e; }
        return null;
    }

    private Vector3 RandomPointInSphere() =>
        transform.position + Random.onUnitSphere * 30f;

    private bool TryGetGroundPoint(float offset, out Vector3 point)
    {
        var box = (BoxCollider)area.GetComponent<Collider>();

        // random X‑Z inside box
        Vector3 local = new(
            Random.Range(-box.size.x * 0.5f, box.size.x * 0.5f),
            0,
            Random.Range(-box.size.z * 0.5f, box.size.z * 0.5f));

        Vector3 worldXZ  = transform.TransformPoint(box.center + local);
        Vector3 rayStart = worldXZ + Vector3.up * 100f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                            200f, ~0, QueryTriggerInteraction.Ignore))
        {
            // slope filter
            if (Vector3.Dot(hit.normal, Vector3.up) < cosMaxSlope)
            { point = default; return false; }

            point = hit.point + Vector3.up * offset;
            return true;
        }

        point = default;
        return false;
    }

    private bool IsFarFromOthers(Vector3 pos)
    {
        float minSqr = spawnSeparation * spawnSeparation;
        foreach (var g in active)
            if (g && (g.transform.position - pos).sqrMagnitude < minSqr)
                return false;
        return true;
    }
}
