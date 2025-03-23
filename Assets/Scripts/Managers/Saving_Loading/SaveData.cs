using System;

[Serializable]
public class SaveData
{
    // Player position
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    // Player stats
    public float playerHealth;
    public int playerLevel; // If you’re not using level yet, that’s fine.

    // Current scene name
    public string currentStarSystem;
}