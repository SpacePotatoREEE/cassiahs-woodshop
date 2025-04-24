using System;
using System.Collections.Generic;

/// <summary>
/// Serializable payload written to disk by SaveSystem.
/// </summary>
[Serializable]
public class SaveData
{
    // ───── Player transform ─────
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    // ───── Vital stats ─────
    public float playerHealth;
    public int   playerLevel;

    // ───── Currency ─────
    public int credits;

    // ───── Galaxy navigation ─────
    /// <summary>Display‑name of the StarSystem the player is currently in.</summary>
    public string currentStarSystem;

    /// <summary>Systems the player has already discovered (for fog‑of‑war).</summary>
    public List<string> discoveredSystems = new();

    /// <summary>Remaining systems in a pre‑plotted course, if any.</summary>
    public List<string> queuedRoute = new();
    
    public List<string> discoveredSystemIds = new ();   // new line
}