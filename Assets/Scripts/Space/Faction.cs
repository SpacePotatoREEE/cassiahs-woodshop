using UnityEngine;

/// <summary>
/// All political entities the player can encounter.
/// Add, reorder or recolour as you like – the GalaxyMap
/// and StarSystem assets will read from this single enum.
/// </summary>
public enum Faction
{
    Federation,
    Pirates,
    Corporate,
    Commonwealth,
    AI_Collective,
    Uninhabited,
    Aliens,        // ← NEW
    TheUnknown     // ← NEW
}

/// <summary>
/// Handy colour lookup so UI code can do
/// `Color c = someStarSystem.ownerFaction.ToColor();`
/// </summary>
public static class FactionExtensions
{
    // ─── palette ───────────────────────────────────────────────
    private static readonly Color32 FEDERATION   = new(0x3D, 0xA5, 0xFF, 0xFF);  // blue
    private static readonly Color32 PIRATES      = new(0xFF, 0x3D, 0x3D, 0xFF);  // red
    private static readonly Color32 CORPORATE    = new(0xF5, 0xC4, 0x00, 0xFF);  // gold
    private static readonly Color32 COMMONWEALTH = new(0x7A, 0xFF, 0x8E, 0xFF);  // green
    private static readonly Color32 AI_COLLECT   = new(0x9B, 0x5B, 0xFF, 0xFF);  // purple
    private static readonly Color32 UNINHABITED  = new(0xAA, 0xAA, 0xAA, 0xFF);  // grey
    private static readonly Color32 ALIENS       = new(0xFF, 0x6A, 0xF3, 0xFF);  // magenta
    private static readonly Color32 UNKNOWN      = new(0x00, 0xE0, 0xDE, 0xFF);  // teal

    public static Color ToColor(this Faction f) => f switch
    {
        Faction.Federation   => FEDERATION,
        Faction.Pirates      => PIRATES,
        Faction.Corporate    => CORPORATE,
        Faction.Commonwealth => COMMONWEALTH,
        Faction.AI_Collective=> AI_COLLECT,
        Faction.Uninhabited  => UNINHABITED,
        Faction.Aliens       => ALIENS,
        Faction.TheUnknown   => UNKNOWN,
        _                    => Color.white
    };
}