using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    // 캐릭터 데이터
    public List<SaveCharacterData> myCharacters = new();    // 보유 캐릭터 리스트
    public int SelectCharID;                                // 선택한 캐릭터 ID
    
    // 아이템 데이터
    public List<SaveSlotData> storageSlots = new();         // 창고
    public List<SaveSlotData> inventorySlots = new();       // 인벤토리
    public List<SaveSlotData> safeSlots = new();            // 각성 보존 슬롯
    public List<int> quickSlots = new();                    // 퀵슬롯 (아이템 코드만 기억하고 인벤토리에서 갯수를 셈)
    public int storageSlotNum;                              // 창고 최대 슬롯 수
    public int invenSlotNum;                                // 인벤토리 최대 슬롯 수
    public int safeSlotNum;                                 // 각성 보존 슬롯 수
    public int quickSlotsNum;                               // 퀵슬롯 최대 슬롯 수
    

    /* 세이브 데이터 첫 생성 시 초기화 */
    public PlayerSaveData()
    {
        // 첫 캐릭터 유안 지급
        SaveCharacterData charData = new();
        charData.TID = 101;
        charData.linkRateLevel = 0;
        charData.TotallinkRateValue = 0;
        myCharacters.Add(charData);

        // 유안 기본 선택
        SelectCharID = 101;

        // 인벤토리 및 창고 슬롯 갯수 지정
        invenSlotNum = 16;
        storageSlotNum = 16;
        safeSlotNum = 4;
        quickSlotsNum = 3;
    }
}

[System.Serializable]
public class SaveSlotData
{
    public int index;   // 슬롯 위치
    public int TID;     // 아이템 고유 번호
    public int amount;  // 수량
}

[System.Serializable]
public class SaveCharacterData
{
    public int TID;                     // 캐릭터 고유 번호
    public int linkRateLevel;           // 각 플레이어 캐릭터 동조율 단계 값 (현재 레벨: 0~4)
    public float TotallinkRateValue;    // 누적된 동조율 값 (현재 경험치량)
}