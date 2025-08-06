// SpawnTable.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    public int MinAreaLevel = 1;
    public int MaxAreaLevel = 99;

    [System.Serializable]
    public class Entry
    {
        public UnitTemplate Template;
        public int Weight = 10;
    }
    public List<Entry> Units = new();

    [Header("Affix count")]
    public int MinAffixes = 0;
    public int MaxAffixes = 2;
}