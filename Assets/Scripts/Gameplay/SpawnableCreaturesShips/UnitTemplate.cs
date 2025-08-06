using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer‑facing asset that describes a base creature or ship.
/// </summary>
[CreateAssetMenu(menuName = "Spawning/Unit Template")]
public class UnitTemplate : ScriptableObject
{
    [Header("Identity")]
    public string     UnitName = "Wolf";              // Display name (no prefixes/suffixes yet)
    public GameObject Prefab;                         // Visual + collider + animation
    public string     BiomeTag = "Earth";             // "Earth", "Air", "Ship", etc.

    [Header("Ground Placement")]
    [Tooltip("Vertical offset (in metres) applied AFTER the ground ray‑cast hit. " +
             "0 = place exactly on the hit point.  " +
             "Positive values float the unit; negative values sink it slightly.")]
    public float GroundSpawnOffset = 0.5f;            // ← NEW

    [Header("Base Stat Block")]
    public List<StatModifier> BaseStats = new();      // Applied before affixes

    /* ─────────────────────────────────────────────────────────────────── */
    /* Optional Evolution fields (ignored if you haven’t added them yet)  */
    /* ─────────────────────────────────────────────────────────────────── */
    public EvolutionDefinition evolutionDef;          // Null → no evolution
    public int evolutionTier = 0;                     // 0 = base form
}