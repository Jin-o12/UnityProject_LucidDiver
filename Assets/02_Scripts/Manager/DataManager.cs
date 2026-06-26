/// <summary>
/// 게임 데이터들을 불러오고 관리하는 인스턴스 클래스
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    // 정적 데이터
    private Dictionary<int, ItemData> itemDataDictionary;           // 아이템 데이터 사전
    private Dictionary<int, CharacterData> CharDataDictionary;      // 캐릭터 데이터 사전

    // 캐릭터 대사 데이터를 TID를 Key로 하여 딕셔너리로 관리
    private Dictionary<int, CharacterDialogueData> dialogueDataDictionary; 

    // 동적 데이터
    public GlobalRuntimeData runtimeData { get; private set; }      // 게임이 실행되는 동안의 데이터

    public PlayerSaveData playerData { get; private set; }          // 계정 데이터
    [SerializeField] private string saveFilePath;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            itemDataDictionary = new Dictionary<int, ItemData>();
            CharDataDictionary = new Dictionary<int, CharacterData>();
            dialogueDataDictionary = new Dictionary<int, CharacterDialogueData>();
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

        LoadGameData();
    }

    /* 플레이어 게임 데이터 저장하기 */
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
    }

#region 데이터 불러오기
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
            NewSaveData();
        }
    }

    /* 게임 첫 실행 시 새로운 게임 데이터 생성 */
    private void NewSaveData()
    {
        if (playerData == null)
        {
            playerData = new PlayerSaveData();
        }

        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
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

        // 캐릭터 데이터
        CharacterData[] charDatas = Resources.LoadAll<CharacterData>("ScriptableObjects/Character");
        foreach(CharacterData data in charDatas)
        {
            CharDataDictionary[data.TID] = data;
        }

        // 캐릭터 대사 스크립트 데이터
        TextAsset jsonAsset = Resources.Load<TextAsset>($"JSON/CharacterDialogues");
        if(jsonAsset != null)
        {
            // JSON 파싱
            CharacterDialogueData parsedData = JsonConvert.DeserializeObject<CharacterDialogueData>(jsonAsset.text);
            // 딕셔너리에 저장
            dialogueDataDictionary[parsedData.CharacterTID] = parsedData;
            
            Debug.Log("데이터 로드 완료");
        }
        else
        {
            Debug.LogError($"캐릭터 대사를 로드할 수 없었습니다.");
        }
    }
#endregion

#region 정적 데이터 추출 메서드
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

    /* 캐릭터 데이터 가져오기 */
    public CharacterData GetCharacterData(int charTID)
    {
        if(CharDataDictionary.TryGetValue(charTID, out CharacterData data))
        {
            return data;
        }
        else
        {
            Debug.LogWarning("char TID " + charTID + " not found!");
            return null;
        }
    }

    /* 캐릭터 대사 데이터 가져오기 */
    public string GetRandomDialogue(int _charTID, DialogueType _type)
    {
        // 해당 TID에 맞는 캐릭터 데이터가 있는지 확인
        if(dialogueDataDictionary.TryGetValue(_charTID, out CharacterDialogueData data))
        {
            // 해당 캐릭터 데이터 내에 지정한 상황의 대사 리스트가 있는지 확인
            if(data.Dialogues.TryGetValue(_type, out List<DialogueLine> lines) && lines.Count > 0)
            {
                // 리스트에서 무작위로 하나 뽑아서 텍스트 반환
                int randomIndex = Random.Range(0, lines.Count);
                return lines[randomIndex].Text;
            }
            else
            {
                Debug.LogWarning($"[TID: {_charTID}] 캐릭터에게 [{_type}] 상황의 대사가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[TID: {_charTID}] 캐릭터의 대사 데이터가 없습니다");
        }

        return string.Empty;
    }

    /* 특정 상황에 맞는 대사 중 원하는 순서의 대사를 뽑아오는 함수 */
    public string GetDialogueByIndex(int charTID, DialogueType type, int index)
    {
        // 해당 캐릭터의 데이터가 있는지 확인
        if(dialogueDataDictionary.TryGetValue(charTID, out CharacterDialogueData data))
        {
            // 캐릭터 데이터 안에 요청한 상황의 대사 리스트가 있는지 확인
            if(data.Dialogues.TryGetValue(type, out List<DialogueLine> lines))
            {
                // 요청한 순서가 실제 대사 개수 범위를 벗어나지 않는지 확인
                if (index >= 0 && index < lines.Count)
                {
                    return lines[index].Text; // 원하는 순서의 대사 반환
                }
                else
                {
                    Debug.LogWarning($"[TID: {charTID}] [{type}] 상황의 {index}번째 대사가 없습니다 (현재 총 대사 수: {lines.Count}개)");
                }
            }
            else
            {
                Debug.LogWarning($"[TID: {charTID}] 캐릭터에게 [{type}] 상황의 대사가 없습니다");
            }
        }
        else
        {
            Debug.LogWarning($"[TID: {charTID}] 캐릭터의 대사 데이터가 없습니다");
        }
        return string.Empty; // 에러 시 빈 문자열 반환
    }
#endregion
}
