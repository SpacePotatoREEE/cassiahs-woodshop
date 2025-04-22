using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerCharacterStats", 
    menuName = "Scriptable Objects/PlayerCharacterStats"
)]
public class PlayerCharacterStats : ScriptableObject
{
    // Example stats that you want to save/load

    public float playerHealth = 100f;
    public int playerLevel = 1;

    // Example position data
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    // Example resources
    public int woodCount;
    public int goldCount;

    // Example current scene name
    public string currentSceneName = "StarSystemScene";

    // Add more fields as needed: relationships, morality, quest states, etc.
}