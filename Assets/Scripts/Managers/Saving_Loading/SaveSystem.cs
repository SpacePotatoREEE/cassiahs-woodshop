using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class SaveSystem
{
    private static readonly string saveFileName = "gameSave.json";

    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

    // Save the data to disk
    public static void SaveGame(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("SaveGame failed: data is null.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"Game saved to {SaveFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save game: {ex.Message}");
        }
    }

    // Load the data from disk
    public static SaveData LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning($"No save file found at {SaveFilePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded successfully");
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game: {ex.Message}");
            return null;
        }
    }
}