using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Game/Creatures/Companion Definition")]
public class CreatureDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "wolf";
    public string displayName = "Wolf";
    public Sprite icon;

    [Header("Capture")]
    [Range(0f, 1f)] public float baseCaptureRate = 0.20f;

    [Header("Companion Prefab")]
    [Tooltip("What to spawn around the player after a successful capture.")]
    public GameObject companionPrefab;

    [Header("Fallback combat (used only if the companion prefab has NO UnitStats)")]
    public float fallbackDamage = 6f;
    public float fallbackAttackRange = 1.8f;
    public float fallbackAttackCooldown = 0.8f;
    public float fallbackMoveSpeed = 6f;
}