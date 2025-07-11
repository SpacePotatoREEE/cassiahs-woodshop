using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StarSystem",
    menuName = "Galaxy/Star System")]
public class StarSystemData : ScriptableObject
{
    /* ───── Basic Info ───── */
    [Header("Basic Info")]
    public string displayName = "New System";

    [Tooltip("Scene that represents this system (must exist in Build Settings).")]
    public string sceneName;

    /* ───── Map Position ───── */
    [Header("Map Position")]
    [Tooltip("2-D coordinate in the galaxy map (arbitrary units).")]
    public Vector2 mapPosition;

    /* ───── Ownership / Links ───── */
    [Header("Ownership")]
    public Faction ownerFaction = Faction.Uninhabited;

    [Header("Neighbouring Systems (1-jump each)")]
    public List<StarSystemData> neighborSystems = new();

    /* ───── Discovery Flags ───── */
    [Header("Discovery")]
    [Tooltip("If true the player can see this system from game start.")]
    public bool discoveredAtStart = false;

    /* ───── UI / Lore ───── */
    [Header("UI")]
    [Tooltip("Thumbnail or render shown when hovering the system on the map.")]
    public Sprite previewSprite;

    [TextArea(2, 5)]
    [Tooltip("Short lore or economy blurb shown in the hover panel.")]
    public string description;
}