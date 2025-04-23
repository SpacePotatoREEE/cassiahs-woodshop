using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight list of every StarSystemData asset so runtime
/// systems (map UI, path‑finding, save/load) can iterate quickly.
/// Create one asset and drop all systems into the list.
/// </summary>
[CreateAssetMenu(
    fileName = "GalaxyDatabase",
    menuName = "Galaxy/Galaxy Database")]
public class GalaxyDatabase : ScriptableObject
{
    public List<StarSystemData> allSystems = new();
}