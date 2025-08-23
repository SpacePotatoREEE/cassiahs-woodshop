// CombatEncounterTrigger.cs
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CombatEncounterTrigger : MonoBehaviour
{
    [Tooltip("Meters. Shown as wire sphere in Scene view.")]
    [Range(.1f, 15f)] public float triggerRadius = 4f;

    [Header("Scene & Context")]
    public BattleContext context;               // drag the asset here
    [SceneName]                                   // custom attribute if you have one
    public string battleSceneName = "Battle_Template";

    private void Reset()
    {
        SphereCollider c = GetComponent<SphereCollider>();
        c.isTrigger     = true;
        c.radius        = triggerRadius;
    }

    private void OnValidate()   // keep collider radius in sync in Editor
    { GetComponent<SphereCollider>().radius = triggerRadius; }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .3f, .3f, .35f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 1. Fill the context
        context.playerPrefab = other.gameObject;
        context.enemyPrefab  = gameObject;              // this creature
        context.returnScene  = gameObject.scene.name;

        // 2. Protect them from being destroyed on load
        DontDestroyOnLoad(other.gameObject);
        DontDestroyOnLoad(gameObject);

        // 3. Kick off transition
        CombatTransition.Instance.Begin(battleSceneName, context);
    }
}