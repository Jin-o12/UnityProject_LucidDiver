using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 1차원 JSON 데이터를 받아낼 임시 DTO 클래스 선언
public class FlatDialogueData
{
    public int CharacterTID;
    public string CharacterName;
    public string Situation;
    public int DialogID;
    public string Text;
}

public class LocalJsonDialogueRepository : IDialogueRepository
{
    private Dictionary<int, CharacterDialogueData> dialogueDataDictionary = new();

    /* 생성자로 캐릭터 대사 스크립트 데이터를 읽어옴 */
    public LocalJsonDialogueRepository()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"JSON/Dialogues");
        if(jsonAsset != null)
        {
            // 1차원 JSON 배열을 임시 클래스로 파싱
            List<FlatDialogueData> flatDataList = JsonConvert.DeserializeObject<List<FlatDialogueData>>(jsonAsset.text);

            // 파싱된 리스트를 순회하며 계층형 딕셔너리로 조립
            foreach(var data in flatDataList)
            {
                //JSON 데이터 중 일부 Situation 값이 누락된 경우를 대비한 예외 처리
                if(string.IsNullOrEmpty(data.Situation))
                {
                    Debug.LogWarning($"[TID: {data.CharacterTID}] 대사({data.DialogID})에 Situation 값이 누락되어 건너뜁니다");
                    continue;
                }

                // 문자열로 가져와진 대사 상황 Situation을 DialogueType Enum으로 변환 시도
                if(System.Enum.TryParse(data.Situation, out DialogueType dialogueType))
                {
                    // 해당 캐릭터 TID가 딕서너리에 없으면 뼈대 생성
                    if(!dialogueDataDictionary.ContainsKey(data.CharacterTID))
                    {
                        dialogueDataDictionary[data.CharacterTID] = new CharacterDialogueData
                        {
                            CharacterTID = data.CharacterTID,
                            Dialogues = new Dictionary<DialogueType, List<DialogueLine>>()
                        };
                    }

                    // 해당 캐릭터의 Dialogues 안에 현재 상황(Situation) 리스트가 없으면 생성
                    if (!dialogueDataDictionary[data.CharacterTID].Dialogues.ContainsKey(dialogueType))
                    {
                        dialogueDataDictionary[data.CharacterTID].Dialogues[dialogueType] = new List<DialogueLine>();
                    }

                    // 실제 대사 데이터를 알맞은 위치의 리스트에 추가
                    dialogueDataDictionary[data.CharacterTID].Dialogues[dialogueType].Add(new DialogueLine 
                    { 
                        ID = data.DialogID, 
                        Text = data.Text 
                    });
                }
                else
                {
                    Debug.LogWarning($"알 수 없는 Situation 값입니다: {data.Situation}");
                }
            }
            Debug.Log("데이터 로드 및 계층화 조립 완료");
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

    /* 특정 상황에 맞는 대사 중 원하는 순서의 대사에서 TID 값을 뽑아오는 함수 */
    public int GetTIDByIndex(int charTID, DialogueType type, int index)
    {
        if (dialogueDataDictionary.TryGetValue(charTID, out CharacterDialogueData data))
        {
            // 캐릭터 데이터 안에 요청한 상황의 대사 리스트가 있는지 확인
            if (data.Dialogues.TryGetValue(type, out List<DialogueLine> lines))
            {
                // 요청한 순서가 실제 대사 개수 범위를 벗어나지 않는지 확인
                if (index >= 0 && index < lines.Count)
                {
                    return lines[index].ID; // 원하는 순서의 대사에서 ID 값을 반환
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
        return 0; // 에러 시 0 값을 반환
    }

    /* 특정 상황에서 출력 가능한 대사의 개수를 뽑아오는 함수 */
    public int GetDialogueCount(int charTID, DialogueType type)
    {
        if (dialogueDataDictionary.TryGetValue(charTID, out CharacterDialogueData data))
        {
            // 캐릭터 데이터 안에 요청한 상황의 대사 리스트가 있는지 확인
            if (data.Dialogues.TryGetValue(type, out List<DialogueLine> lines))
            {
                // 요청한 상황의 실제 대사 개수를 반환
                return lines.Count;
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
        return 0; // 에러 시 0개 반환
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
