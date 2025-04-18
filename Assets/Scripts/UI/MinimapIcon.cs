using UnityEngine;

/// <summary>
/// Renders a tiny sprite for the minimap camera and colours it:
/// • Green  = Planet
/// • White  = Neutral ship
/// • Red    = Ship in Attack state
/// Player icon stays white; camera keeps it centred.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MinimapIcon : MonoBehaviour
{
    public enum IconType { Player, Planet, Ship }

    [SerializeField] private IconType   type   = IconType.Ship;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Only needed when Type = Ship and you want red‑when‑attacking")]
    [SerializeField] private NPCShipAI npcRef;

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (type == IconType.Ship && !npcRef)
            npcRef = GetComponentInParent<NPCShipAI>();
        UpdateColour();                    // initial tint
    }

    private void Update()
    {
        if (type == IconType.Ship && npcRef != null)
            UpdateColour();                // refresh every frame – cheap, tiny object
    }

    private void UpdateColour()
    {
        Color c = Color.white;
        switch (type)
        {
            case IconType.Planet: c = Color.green;                 break;
            case IconType.Ship:
                c = (npcRef != null && npcRef.IsInAttackMode) ? Color.red : Color.white;
                break;
        }
        spriteRenderer.color = c;
    }
}
