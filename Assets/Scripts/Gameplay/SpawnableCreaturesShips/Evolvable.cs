// Evolvable.cs
//
// Attach to the root of a spawned creature if you plan to let it ‘level-up’
// during play.  For now it’s dormant; UnitFactory could add it automatically.
//
using UnityEngine;

public class Evolvable : MonoBehaviour
{
    public EvolutionDefinition evolution;
    public int currentStage = 0;

    /// <summary>Replace visuals/stats with the given stage, if valid.</summary>
    public void ApplyStage(int stageIndex)
    {
        if (evolution == null) return;
        if (stageIndex < 0 || stageIndex >= evolution.stages.Count) return;

        var stage = evolution.stages[stageIndex];
        if (stage.template == null) return;            // nothing to swap to

        // Destroy current model & instantiate the evolved prefab
        foreach (Transform child in transform) Destroy(child.gameObject);
        Instantiate(stage.template.Prefab, transform);

        // TODO: copy stats / affixes if needed
        currentStage = stageIndex;
    }
}