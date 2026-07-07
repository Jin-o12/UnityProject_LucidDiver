using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class FlatSkillData
{
    public int TID;
    public string skillName;
    public string skillDesc;
    public string skillIcon;
    public string skillPrefab;
    public float skillCooltime;
    public float mpCost;
    public float fireRange;
    public string areaType;
    public float areaWidth;
    
    // 효과 변수들
    public string effectType_0;
    public float effectDelay_0;
    public float effectValue_0;

    public string effectType_1;
    public float effectDelay_1;
    public float effectValue_1;

    public string effectType_2;
    public float effectDelay_2;
    public float effectValue_2;
}

public class LocalJsonSkillRepository : ISkillRepository
{
    // 스킬들을 TID를 키값으로 보관하는 딕셔너리
    private Dictionary<int, SkillData> skillDatabase = new Dictionary<int, SkillData>();

    public LocalJsonSkillRepository()
    {
        LoadAllSkillData();
    }

    public void LoadAllSkillData()
    {
        // JSON 파일 로드
        TextAsset jsonAsset = Resources.Load<TextAsset>("JSON/Skill");

        if (jsonAsset == null)
        {
            Debug.LogError("[SkillRepository] 스킬 데이터 JSON 파일을 찾을 수 없습니다.");
            return;
        }

        // JSON 역직렬화
        List<FlatSkillData> flatDataList = JsonConvert.DeserializeObject<List<FlatSkillData>>(jsonAsset.text);

        // Flat 데이터를 실제 SkillData로 변환
        foreach (var data in flatDataList)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();

            // 기본 데이터 할당
            skill.TID = data.TID;
            skill.skillName = data.skillName;
            skill.skillDesc = data.skillDesc;
            skill.skillIcon = data.skillIcon;
            skill.skillPrefab = data.skillPrefab;
            skill.skillCooltime = data.skillCooltime;
            skill.mpCost = data.mpCost;
            skill.fireRange = data.fireRange;
            skill.areaWidth = data.areaWidth;

            // 열거형(Enum) 파싱
            if (!string.IsNullOrEmpty(data.areaType) && System.Enum.TryParse(data.areaType, true, out AreaType parsedArea))
            {
                skill.areaType = parsedArea;
            }
            else
            {
                Debug.LogWarning($"[{data.skillName}] 알 수 없는 AreaType 입니다: {data.areaType}");
            }

            // 넘버링 된 다중 효과(Effect) 리스트 파싱을 위한 로컬 함수
            void AddEffect(string eType, float eDelay, float eValue)
            {
                // 타입 문자열이 비어있으면 아예 효과가 없는 슬롯이므로 무시
                if (string.IsNullOrEmpty(eType)) return;

                if (System.Enum.TryParse(eType, true, out EffectType finalType))
                {
                    skill.effects.Add(new SkillEffect 
                    {
                        effectType = finalType,
                        effectDelay = eDelay,
                        effectValue = eValue
                    });
                }
                else
                {
                    Debug.LogWarning($"[{skill.skillName}] 알 수 없는 Effect 타입입니다: {eType}");
                }
            }

            // 만들어둔 함수를 이용해 0번, 1번, 2번 데이터를 차례대로 넣기
            AddEffect(data.effectType_0, data.effectDelay_0, data.effectValue_0);
            AddEffect(data.effectType_1, data.effectDelay_1, data.effectValue_1);
            AddEffect(data.effectType_2, data.effectDelay_2, data.effectValue_2);

            // 완성된 스킬 데이터를 딕셔너리에 등록
            skillDatabase[skill.TID] = skill;
        }

        Debug.Log($"[SkillRepository] 총 {skillDatabase.Count}개의 스킬 데이터 로드 완료");
    }

    /* 외부에서 TID로 스킬 데이터를 꺼내갈 때 사용하는 함수 */
    public SkillData GetSkillData(int skillID)
    {
        if (skillDatabase.TryGetValue(skillID, out SkillData skill))
        {
            return skill;
        }
        
        Debug.LogError($"[SkillRepository] ID {skillID}에 해당하는 스킬이 없습니다.");
        return null;
    }
}
