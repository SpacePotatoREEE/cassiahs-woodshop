// AreaProfile.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Area Profile")]
public class AreaProfile : ScriptableObject
{
    public int AreaLevel = 1;

    public enum EnvironmentType { Planet, Space }
    public EnvironmentType Environment = EnvironmentType.Planet;

    [Tooltip("Usually level N includes tables 1…N")]
    public List<SpawnTable> SpawnTables = new();

    [Header("Spawner tuning")]
    public float SpawnInterval   = 5f;
    public int   MaxActiveUnits  = 10;
    public float UnitLifetime    = 60f;
}