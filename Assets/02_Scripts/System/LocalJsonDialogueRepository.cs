using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LocalJsonDialogueRepository : IDialogueRepository
{
    private Dictionary<int, CharacterDialogueData> dialogueDataDictionary = new();

    /* 생성자로 캐릭터 대사 스크립트 데이터를 읽어옴 */
    public LocalJsonDialogueRepository()
    {
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

    /* 특정 상황의 캐릭터 대사 데이터 랜덤하게 가져오기 */
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
}
