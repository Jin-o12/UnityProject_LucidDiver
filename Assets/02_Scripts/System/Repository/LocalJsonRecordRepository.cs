using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 1차원 JSON 데이터를 받아낼 임시 DTO 클래스 선언
public class FlatRecordData
{
    public int CharacterTID;
    public string CharacterName;
    public int RequiredLevel;
    public string RecordName;
    public int DialogID;
    public string Speaker;
    public string Text;
}

public class LocalJsonRecordRepository : IRecordRepository
{
    private Dictionary<int, CharacterRecordData> recordDataDictionary = new();

    /* 생성자로 캐릭터 기록(Record) 스크립트 데이터를 읽어옴 */
    public LocalJsonRecordRepository()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"JSON/Record");
        if (jsonAsset != null)
        {
            // 1차원 JSON 배열을 임시 클래스로 파싱
            List<FlatRecordData> flatDataList = JsonConvert.DeserializeObject<List<FlatRecordData>>(jsonAsset.text);

            // 파싱된 리스트를 순회하며 계층형 딕셔너리로 조립
            foreach (var data in flatDataList)
            {
                // 해당 캐릭터 TID가 딕셔너리에 없으면 뼈대 생성
                if (!recordDataDictionary.ContainsKey(data.CharacterTID))
                {
                    recordDataDictionary[data.CharacterTID] = new CharacterRecordData
                    {
                        CharacterTID = data.CharacterTID,
                        CharacterName = data.CharacterName,
                        RecordNames = new Dictionary<int, string>(),
                        Records = new Dictionary<int, List<RecordLine>>()
                    };
                }

                // 해당 캐릭터의 RecordNames 안에 현재 해금 레벨(RequiredLevel)의 제목이 없으면 추가
                if (!recordDataDictionary[data.CharacterTID].RecordNames.ContainsKey(data.RequiredLevel))
                {
                    recordDataDictionary[data.CharacterTID].RecordNames[data.RequiredLevel] = data.RecordName;
                }

                // 해당 캐릭터의 Records 안에 현재 해금 레벨(RequiredLevel) 리스트가 없으면 생성
                if (!recordDataDictionary[data.CharacterTID].Records.ContainsKey(data.RequiredLevel))
                {
                    recordDataDictionary[data.CharacterTID].Records[data.RequiredLevel] = new List<RecordLine>();
                }

                // 실제 대사 데이터를 알맞은 위치의 리스트에 추가
                recordDataDictionary[data.CharacterTID].Records[data.RequiredLevel].Add(new RecordLine
                {
                    DialogID = data.DialogID,
                    Speaker = data.Speaker,
                    Text = data.Text
                });
            }
            Debug.Log("Record 데이터 로드 및 계층화 조립 완료");
        }
        else
        {
            Debug.LogError($"Record JSON을 로드할 수 없었습니다.");
        }
    }

    /* 특정 캐릭터의 모든 기록(레코드 그룹) 해금 레벨 리스트를 반환하는 함수 */
    public List<int> GetRecordGroupLevels(int charTID)
    {
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            return data.Records.Keys.OrderBy(k => k).ToList();
        }
        return new List<int>();
    }


    /* 특정 레벨에 맞는 기록 대사 중 원하는 순서의 대사 텍스트를 뽑아오는 함수 */
    public string GetRecordTextByIndex(int charTID, int requiredLevel, int index)
    {
        // 해당 캐릭터의 데이터가 있는지 확인
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            // 캐릭터 데이터 안에 요청한 레벨의 대사 리스트가 있는지 확인
            if (data.Records.TryGetValue(requiredLevel, out List<RecordLine> lines))
            {
                // 요청한 순서가 실제 대사 개수 범위를 벗어나지 않는지 확인
                if (index >= 0 && index < lines.Count)
                {
                    return lines[index].Text; // 원하는 순서의 대사 반환
                }
                else
                {
                    Debug.LogWarning($"[TID: {charTID}] [Level {requiredLevel}] 상황의 {index}번째 기록 대사가 없습니다 (현재 총 대사 수: {lines.Count}개)");
                }
            }
            else
            {
                Debug.LogWarning($"[TID: {charTID}] 캐릭터에게 [Level {requiredLevel}] 상황의 기록 대사가 없습니다");
            }
        }
        else
        {
            Debug.LogWarning($"[TID: {charTID}] 캐릭터의 기록 데이터가 없습니다");
        }
        return string.Empty; // 에러 시 빈 문자열 반환
    }

    /* 특정 레벨에 맞는 기록 대사 중 원하는 순서의 대사 발화자를 뽑아오는 함수 */
    public string GetRecordSpeakerByIndex(int charTID, int requiredLevel, int index)
    {
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            if (data.Records.TryGetValue(requiredLevel, out List<RecordLine> lines))
            {
                if (index >= 0 && index < lines.Count)
                {
                    return lines[index].Speaker;
                }
                else
                {
                    Debug.LogWarning($"[TID: {charTID}] [Level {requiredLevel}] 상황의 {index}번째 기록 대사가 없습니다");
                }
            }
        }
        return string.Empty;
    }

    /* 특정 레벨에서 출력 가능한 기록 대사의 개수를 뽑아오는 함수 */
    public int GetRecordCount(int charTID, int requiredLevel)
    {
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            if (data.Records.TryGetValue(requiredLevel, out List<RecordLine> lines))
            {
                return lines.Count;
            }
            else
            {
                Debug.LogWarning($"[TID: {charTID}] 캐릭터에게 [Level {requiredLevel}] 상황의 기록 대사가 없습니다");
            }
        }
        else
        {
            Debug.LogWarning($"[TID: {charTID}] 캐릭터의 기록 데이터가 없습니다");
        }
        return 0; // 에러 시 0개 반환
    }

    /* 특정 레벨에 맞는 기록 대사 중 원하는 순서의 대사 ID 값을 뽑아오는 함수 */
    public int GetRecordDialogIDByIndex(int charTID, int requiredLevel, int index)
    {
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            if (data.Records.TryGetValue(requiredLevel, out List<RecordLine> lines))
            {
                if (index >= 0 && index < lines.Count)
                {
                    return lines[index].DialogID;
                }
            }
        }
        return 0;
    }

    /* 특정 레벨에 맞는 기록의 제목(RecordName)을 뽑아오는 함수 */
    public string GetRecordName(int charTID, int requiredLevel)
    {
        if (recordDataDictionary.TryGetValue(charTID, out CharacterRecordData data))
        {
            if (data.RecordNames.TryGetValue(requiredLevel, out string recordName))
            {
                return recordName;
            }
        }
        return $"심상 기록 {requiredLevel:D2}";
    }
}
