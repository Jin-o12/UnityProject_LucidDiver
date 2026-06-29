/// <summary>
/// Reads and writes local save data.
/// </summary>
using System.IO;
using UnityEngine;

public class LocalSaveRepository : ISaveRepository
{
    private PlayerSaveData playerSaveData;

    private string saveFilePath;        // Local save file path

    /* Initialize save path */
    public LocalSaveRepository()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveFile.json");
    }
    
    /* Load player save data */
    public PlayerSaveData LoadSaveData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            playerSaveData = JsonUtility.FromJson<PlayerSaveData>(json);
            return playerSaveData;
        }
        else
        {
            SaveGameData();
            return playerSaveData;
        }
    }

    /* Save the current cached save data */
    public void SaveGameData()
    {
        if (playerSaveData == null)
            playerSaveData = new PlayerSaveData();

        string json = JsonUtility.ToJson(playerSaveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved");
    }

    /* Save externally provided save data */
    public void SaveGameData(PlayerSaveData saveData)
    {
        playerSaveData = saveData ?? new PlayerSaveData();

        string json = JsonUtility.ToJson(playerSaveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved");
    }

    /* Return the selected character's link rate level */
    public int GetLinkRateLevel()
    {
        if (playerSaveData == null)
        {
            playerSaveData = LoadSaveData();
        }

        SaveCharacterData charData = playerSaveData.myCharacters.Find(x => x.TID == playerSaveData.SelectCharID);
        if (charData != null)
        {
            return charData.linkRateLevel;
        }
        else
        {
            Debug.LogWarning($"Character data for TID {playerSaveData.SelectCharID} was not found.");
            return 0;
        }
    }
}
