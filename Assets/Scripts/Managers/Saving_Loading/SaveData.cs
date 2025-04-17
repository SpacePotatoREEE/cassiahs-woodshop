using System;

[Serializable]
public class SaveData
{
    // Position
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    // Health / level
    public float playerHealth;
    public int   playerLevel;

    // NEW ► currency
    public int   credits;          // starts at 0

    public string currentStarSystem;
}