using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SOCharacterRepository : ICharDataRepository
{
    private Dictionary<int, CharacterData> CharDataDictionary = new();      // 캐릭터 데이터 사전

    /* 생성자로 캐릭터 데이터 로드 */
    public SOCharacterRepository()
    {
        // 캐릭터 데이터
        CharacterData[] charDatas = Resources.LoadAll<CharacterData>("ScriptableObjects/Character");
        foreach(CharacterData data in charDatas)
        {
            CharDataDictionary[data.TID] = data;
        }
    }
    
    /* 캐릭터 데이터 가져오기 */
    public CharacterData GetCharacterData(int _charTID)
    {
        if(CharDataDictionary.TryGetValue(_charTID, out CharacterData data))
        {
            return data;
        }
        else
        {
            Debug.LogWarning("char TID " + _charTID + " not found!");
            return null;
        }
    }
}
