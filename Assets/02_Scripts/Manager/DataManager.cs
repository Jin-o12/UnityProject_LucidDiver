/// <summary>
/// 게임 데이터들을 불러오고 관리하는 인스턴스 클래스
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    private Dictionary<int, ItemData> itemDataDictionary;           // 아이템 데이터 사전
    

    public PlayerSaveData playerData { get; private set; }          // 계정 데이터
    [SerializeField] private string saveFilePath;                   // 세이브 파일 경로


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            itemDataDictionary = new Dictionary<int, ItemData>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // 저장 경로 설정
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveFile.json");
        LoadGame();

        // 캐릭터를 고르는 로비 씬과 연결이 되지 않았으므로 캐릭터 데이터를 코드에서 설정
        playerData.SelectCharID = 1;

        LoadGameData();
    }

    /* 플레이어 게임 데이터 저장하기 */
    public void SaveGame()
    {
        if (playerData == null)
        {
            playerData = new PlayerSaveData();
        }

        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved");
    }

    /* 플레이어 게임 데이터 불러오기 */
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            // 세이브 파일이 없으면 새 데이터 생성
            playerData = new PlayerSaveData();
            SaveGame();
        }
    }

    /* 모든 게임 데이터 로드 */
    private void LoadGameData()
    {
        // 아이템 데이터
        ItemData[] itemDatas = Resources.LoadAll<ItemData>("ScriptableObjects/Item");
        foreach(ItemData data in itemDatas)
        {
            itemDataDictionary[data.TID] = data;
        }

        
    }

    /* 아이템 데이터 가져오기 */
    public ItemData GetItemData(int itemTID)
    {
        if(itemDataDictionary.TryGetValue(itemTID, out ItemData data))
        {
            return data;
        }
        else
        {
            Debug.LogWarning("Item TID " + itemTID + " not found!");
            return null;
        }
    }

    
}
