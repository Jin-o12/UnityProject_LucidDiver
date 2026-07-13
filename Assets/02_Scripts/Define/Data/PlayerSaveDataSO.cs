using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSaveDataSO", menuName = "Runtime Data/Player Save Data")]

public class PlayerSaveDataSO : ScriptableObject, ISaveRepository
{
    // 싱글톤 선언
    private static PlayerSaveDataSO _instance;
    public static PlayerSaveDataSO Instance
    {
        get
        {
            if(_instance==null)
            {
                _instance = Resources.Load<PlayerSaveDataSO>("ScriptableObjects/PlayerData/PlayerSaveDataSO");

                if(_instance == null)
                {
                    Debug.LogError("PlayerSaveDataSO 에셋을 찾을 수 없습니다");
                }
            }
            return _instance;
        }
    }

    // 실제 세이브 데이터를 런타임 내 여기서 가짐
    public PlayerSaveData currentData { get; private set; }

    // 값이 변할 때 UI를 갱신하라고 알려줄 이벤트들
    public event Action OnInventoryUpdated;
    public event Action<int> OnSelectedCharacterChanged;

    // 게임 시작 시, 매니저가 세이브 파일을 읽어와서 이 SO에 세팅
    public void Initialize(PlayerSaveData loadData)
    {
        currentData = loadData;
    }

    /* 현재 출격 중인 캐릭터의 세이브 데이터를 불러옴*/
    public SaveCharacterData GetNowCharacterData()
    {
        if (currentData == null)
        {
            LoadSaveData();
        }

        // 캐릭터 데이터 저장 리스트에서 목표하는 번호의 캐릭터 데이터를 찾음
        SaveCharacterData charData = currentData.myCharacters.Find(x => x.TID == currentData.SelectCharID);
        if(charData!=null)
        {
            return charData;
        }
        else
        {
            return null;
        }
    }

    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "SaveFile.json");
    }

    // 플레이어 저장 데이터 불러오기
    public PlayerSaveData LoadSaveData()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            currentData = JsonUtility.FromJson<PlayerSaveData>(json);
            return currentData;
        }
        else
        {
            SaveGameData();
            return currentData;
        }
    }

    // 게임 데이터 저장
    public void SaveGameData()
    {
        if (currentData == null)
            currentData = new PlayerSaveData();

        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(GetSaveFilePath(), json);
    }

    // 외부에서 전달 된 세이브 데이터 저장
    public void SaveGameData(PlayerSaveData saveData)
    {
        currentData = saveData ?? new PlayerSaveData();

        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(GetSaveFilePath(), json);
    }

    // 현재 동조율 경험치 값 증가
    public void AddlinkRatePoint(float _point)
    {
        SaveCharacterData charData = GetNowCharacterData();
        if (charData != null)
        {
            charData.TotallinkRateValue += _point;
        }
    }

    // 현재 동조율 경험치 값 리턴
    public float GetlinkRatePoint()
    {
        SaveCharacterData charData = GetNowCharacterData();
        return charData != null ? charData.TotallinkRateValue : 0f;
    }

    // 특정 캐릭터의 동조율 레벨 리턴
    public int GetLinkRateLevel()
    {
        SaveCharacterData charData = GetNowCharacterData();
        if (charData != null)
        {
            return charData.linkRateLevel;
        }
        else
        {
            Debug.LogWarning($"TID {currentData.SelectCharID}에 대한 캐릭터 데이터를 찾을 수 없습니다.");
            return 0;
        }
    }

    // // 예시 1: 인벤토리에 아이템 추가
    // public void AddItemToInventory(int itemTID, int amount)
    // {
    //     // (여기에 슬롯 빈자리 찾기 등 로직 추가)
    //     SaveSlotData newSlot = new SaveSlotData { TID = itemTID, amount = amount, index = 0 };
    //     CurrentData.inventorySlots.Add(newSlot);

    //     // UI 갱신 방송 송출!
    //     OnInventoryUpdated?.Invoke(); 
    // }

    // // 예시 2: 선택 캐릭터 변경
    // public void ChangeSelectedCharacter(int charTID)
    // {
    //     CurrentData.SelectCharID = charTID;
    //     OnSelectedCharacterChanged?.Invoke(charTID);
    // }
}