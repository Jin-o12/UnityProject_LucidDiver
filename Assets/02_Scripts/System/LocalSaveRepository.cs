/// <summary>
/// 세이브를 읽고 쓰는 클래스 (ISaveRepository 인터페이스 상속)
/// </summary>
using UnityEngine;
using System.IO;

public class LocalSaveRepository : ISaveRepository
{
    private PlayerSaveData playerSaveData;

    private string saveFilePath;        // 저장 데이터 세이브 경로

    /* 생성 시 경로 지정 */
    public LocalSaveRepository()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveFile.json");
    }
    
    /* 플레이어 저장 데이터 불러오기 */
    public PlayerSaveData LoadSaveData()
    {
        if (File.Exists(saveFilePath))
        {
            // 지정 경로에서 저장 데이터 가져오기
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            // 세이브 파일이 없으면 새 데이터 생성
            SaveGameData();
            return playerSaveData;
        }
    }

    /* 게임 데이터 저장 */
    public void SaveGameData()
    {
        // 데이터가 존재하지 않는다면 새로 생성
        if (playerSaveData == null)
            playerSaveData = new PlayerSaveData();

        // 지정 경로에 데이터 저장
        string json = JsonUtility.ToJson(playerSaveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved");
    }

     /* 현재 캐릭터의 동조율 레벨 리턴 */
    public int GetLinkRateLevel()
    {
        // 리스트에서 TID가 SelectCharID와 일치하는 캐릭터 데이터를 찾음
        SaveCharacterData charData = playerSaveData.myCharacters.Find(x => x.TID == playerSaveData.SelectCharID);
        if (charData != null)
        {
            return charData.linkRateLevel;
        }
        else
        {
            Debug.LogWarning($"TID가 {playerSaveData.SelectCharID}인 캐릭터 데이터를 찾을 수 없습니다.");
            return 0;
        }
    }
}
