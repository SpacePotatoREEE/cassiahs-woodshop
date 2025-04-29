using System;
using System.Collections.Generic;

/// <summary>Serializable payload written to disk by <see cref="SaveSystem"/>.</summary>
[Serializable]
public class SaveData
{
    /* ───── Player Transform ───── */
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    /* ───── Vital stats ───── */
    public float playerHealth;
    public int   playerLevel;

    /* ───── New: Energy ───── */
    public float playerEnergy;

    /* ───── Currency ───── */
    public int credits;

    /* ───── Galaxy navigation ───── */
    public string        currentStarSystem;
    public List<string>  discoveredSystems   = new();
    public List<string>  queuedRoute         = new();
    public List<string>  discoveredSystemIds = new();
}