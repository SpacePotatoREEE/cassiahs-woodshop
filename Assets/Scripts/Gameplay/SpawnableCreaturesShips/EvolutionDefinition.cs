// EvolutionDefinition.cs
//
// Create assets with:  Right-click → Create → Spawning → Evolution Definition
//
// Each stage points to a UnitTemplate that represents the evolved form.
// • Stage 0 is the *base* creature        (usually the same template that has the reference).
// • Stage 1, 2, … are upgraded forms.
//
// UnitFactory (or whatever spawns your units) can look at this asset, roll
// a random tier, and replace the prefab + stats accordingly.
//
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Spawning/Evolution Definition")]
public class EvolutionDefinition : ScriptableObject
{
    [System.Serializable]
    public class Stage
    {
        [Tooltip("UnitTemplate asset for this evolution tier.")]
        public UnitTemplate template;

        [Tooltip("Minimum area-level required before this stage is eligible.")]
        public int minAreaLevel = 1;
    }

    [Header("Evolution Stages (size 1 = no evolution)")]
    public List<Stage> stages = new() {
        new Stage()            // default Stage 0 to avoid empty lists
    };
}