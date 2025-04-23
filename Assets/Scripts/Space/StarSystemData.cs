using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StarSystem",
    menuName = "Galaxy/Star System")]
public class StarSystemData : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName = "New System";

    [Tooltip("Scene that represents this system (must exist in Build Settings).")]
    public string sceneName;

    [Header("Map Position")]
    [Tooltip("2‑D co‑ordinate in the galaxy map (arbitrary units).")]
    public Vector2 mapPosition;

    [Header("Ownership")]
    public Faction ownerFaction = Faction.Uninhabited;

    [Header("Neighbouring Systems (1‑jump each)")]
    public List<StarSystemData> neighborSystems = new();

    [Header("Discovery")]
    [Tooltip("If true the player can see this system from game start.")]
    public bool discoveredAtStart = false;
}