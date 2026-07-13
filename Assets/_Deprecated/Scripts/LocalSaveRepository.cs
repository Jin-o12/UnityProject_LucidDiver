/// <summary>
/// 세이브를 읽고 쓰는 클래스 (ISaveRepository 인터페이스 상속)
/// </summary>
using System.IO;
using UnityEngine;

public class LocalSaveRepository : ISaveRepository
{
    private PlayerSaveData playerSaveData;

    private string saveFilePath;        // 로컬 세이브 파일 경로

    /* 세이브 경로 초기화 */
    public LocalSaveRepository()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveFile.json");
    }
    
    /* 플레이어 세이브 데이터 불러오기 */
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

    /* 현재 캐싱된 세이브 데이터 저장 */
    public void SaveGameData()
    {
        if (playerSaveData == null)
            playerSaveData = new PlayerSaveData();

        string json = JsonUtility.ToJson(playerSaveData, true);
        File.WriteAllText(saveFilePath, json);
    }

    /* 외부에서 전달된 세이브 데이터 저장 */
    public void SaveGameData(PlayerSaveData saveData)
    {
        playerSaveData = saveData ?? new PlayerSaveData();

        string json = JsonUtility.ToJson(playerSaveData, true);
        File.WriteAllText(saveFilePath, json);
    }

    /* 현재 출격 중인 캐릭터의 세이브 데이터를 불러옴*/
    public SaveCharacterData GetNowCharacterData()
    {
        if (playerSaveData == null)
        {
            playerSaveData = LoadSaveData();
        }

        // 캐릭터 데이터 저장 리스트에서 목표하는 번호의 캐릭터 데이터를 찾음
        SaveCharacterData charData = playerSaveData.myCharacters.Find(x => x.TID == playerSaveData.SelectCharID);
        if(charData!=null)
        {
            return charData;
        }
        else
        {
            return null;
        }
    }

    public void AddlinkRatePoint(float _point)
    {
        SaveCharacterData charData = GetNowCharacterData();
        charData.TotallinkRateValue += _point;
    }

    public float GetlinkRatePoint()
    {
        SaveCharacterData charData = GetNowCharacterData();
        return charData.TotallinkRateValue;
    }

    /* 선택된 캐릭터의 링크 비율 레벨 반환 */
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
            Debug.LogWarning($"TID {playerSaveData.SelectCharID}에 대한 캐릭터 데이터를 찾을 수 없습니다.");
            return 0;
        }
    }
}
