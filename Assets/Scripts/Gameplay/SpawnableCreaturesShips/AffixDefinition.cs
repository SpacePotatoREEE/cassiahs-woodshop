// AffixDefinition.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Affixes/Affix Definition")]
public class AffixDefinition : ScriptableObject
{
    [Header("Meta")]
    public string  AffixName   = "Fierce";
    public bool    IsPrefix    = true;        // false = suffix
    public int     MinAreaLevel = 1;
    public int     MaxAreaLevel = 99;
    public List<string> AllowedBiomes = new(); // "Earth","Water","Space" …

    [Header("Display")]
    public Color   DisplayColor = Color.white;
    public string  ExclusiveGroup;            // Prevent conflicting rolls

    [Header("Gameplay")]
    public List<StatModifier> Modifiers = new();
    public int Weight = 10;                   // Higher = more common
}
