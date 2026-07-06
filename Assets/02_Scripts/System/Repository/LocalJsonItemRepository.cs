using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// JSON 데이터를 그대로 받아낼 1차원 평면 클래스
[System.Serializable]
public class FlatItemData
{
    // 공통 정보
    public int itemTID;
    public string itemName;
    public string desc;
    public string category; // Enum 대신 문자열로 먼저 받습니다.
    public int itemMultiple;
    public string itemGrade;
    public string icon;

    // 무기(Weapon) 전용 정보
    public float FireRate;
    public float FireRange;
    public float AtkValue;
    public float dreamBarrierBreakValue;
    public float useMana;

    // 소비(Consume) 전용 정보[cite: 6]
    public string useType;
    public float useRange;
    public float useDelay;
    public float useValue;
    public string useTarget;
    public string effectType;

    // 기억 조각(Memory) 전용 정보[cite: 6]
    public string userType;
    public int charID;
    public int linkRateGain;
}

public class LocalJsonItemRepository : IItemDataRepository
{
    private Dictionary<int, ItemData> itemDatabase = new ();

    public LocalJsonItemRepository()
    {
        LoadGameData();
    }

    public void LoadGameData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("JSON/Item");

        if(jsonAsset==null)
        {
            Debug.Log("아이템 데이터 JSON 파일을 찾을 수 없습니다.");
            return;
        }

        List<FlatItemData> flatDataList = JsonConvert.DeserializeObject<List<FlatItemData>>(jsonAsset.text);

        foreach(var data in flatDataList)
        {
            if(!System.Enum.TryParse(data.category, out itemCategory parsedCategory))
            {
                Debug.Log("알 수 없는 카테고리입니다.");
                continue;
            }

            if(!System.Enum.TryParse(data.itemGrade, out ItemGrade parsedGrade))
            {
                Debug.Log("알 수 없는 등급입니다.");
                parsedGrade = ItemGrade.empty;
            }

            switch(data.category.ToLower())
            {
                case "weapon":
                    WeaponItemData weapon = ScriptableObject.CreateInstance<WeaponItemData>();
                    SetCommonData(weapon, data, parsedCategory, parsedGrade);
                    
                    // 무기 전용 속성 할당[cite: 6]
                    weapon.fireRate = data.FireRate;
                    weapon.fireRange = data.FireRange;
                    weapon.AtkValue = data.AtkValue;
                    weapon.dreamBarrierBreakValue = data.dreamBarrierBreakValue;
                    weapon.useMana = data.useMana;
                    
                    itemDatabase[weapon.TID] = weapon;
                    break;

                case "consume":
                    ConsumeItemData consume = ScriptableObject.CreateInstance<ConsumeItemData>();
                    SetCommonData(consume, data, parsedCategory, parsedGrade);
                    
                    // 소비 전용 속성 할당
                    if (System.Enum.TryParse(data.useType, out AreaType areaType))
                        consume.useType = areaType;

                    consume.useRange = data.useRange;
                    consume.useDelay = data.useDelay;
                    consume.useValue = data.useValue;
                    
                    // Enum 속성 변환
                    consume.useEffect = new List<ItemEffect>();

                    EffectTarget finalTarget = EffectTarget.self;
                    if (!string.IsNullOrEmpty(data.useTarget) && !System.Enum.TryParse(data.useTarget, true, out finalTarget))
                    {
                        Debug.Log($"알 수 없는 카테고리입니다: {consume.itemName}");
                        continue;
                    }
                    EffectType finalType = EffectType.none;
                    if (!string.IsNullOrEmpty(data.effectType) && !System.Enum.TryParse(data.effectType, true, out finalType))
                    {
                        Debug.Log($"알 수 없는 카테고리입니다: {consume.itemName}");
                        continue;
                    }

                    consume.useEffect.Add(new ItemEffect 
                        {
                            effectTarget = finalTarget,
                            effectType = finalType,
                            effectRange = consume.useRange,
                            effectValue = consume.useValue
                        });
                    
                    itemDatabase[consume.TID] = consume;
                    break;

                case "memory":
                    MemoryPieceitemData memory = ScriptableObject.CreateInstance<MemoryPieceitemData>();
                    SetCommonData(memory, data, parsedCategory, parsedGrade);
                    
                    // 기억 조각 전용 속성 할당
                    if(!System.Enum.TryParse(data.userType, out UserType parsedUserType))
                    {
                        Debug.Log("알 수 없는 카테고리입니다.");
                        continue;
                    }

                    memory.userType = parsedUserType;
                    memory.charID = data.charID;
                    memory.linkRateGain = data.linkRateGain;
                    
                    itemDatabase[memory.TID] = memory;
                    break;

                default:
                    Debug.LogWarning($"[{data.itemTID}] 지원하지 않는 아이템 타입입니다: {data.category}");
                    break;
            }
        }
    }

    // 반복되는 공통 속성 할당을 위한 헬퍼 함수
    private void SetCommonData(ItemData item, FlatItemData data, itemCategory cat, ItemGrade grade)
    {
        item.TID = data.itemTID;
        item.itemName = data.itemName;
        item.desc = data.desc;
        item.category = cat; 
        item.itemMultiple = data.itemMultiple;
        item.iconAddress = data.icon;
        item.itemGrade = grade;
    }

    /* ID를 이용해 아이템의 기본 데이터를 가져옴 */
    public ItemData GetItemDataByID(int itemID)
    {
        if(itemDatabase.TryGetValue(itemID, out ItemData item))
        {
            return item;
        }
        Debug.LogError($"{itemID}번 아이템을 데이터베이스에서 찾을 수 없었습니다");
        return null;
    }

    /* ID를 이용해 특정 자식 클래스 타입으로 캐스팅하여 가져옴 */
    public T GetTypeItemData<T>(int itemID) where T: ItemData
    {
        // ID가 동일한 아이템 데이터를 가져옴
        ItemData item = GetItemDataByID(itemID);
        // 데이터를 얻지 못했을 경우
        if(item==null) return null;

        // 가져온 아이템이 요청한 타입과 일치한다면 변환하여 반환
        if(item is T typedItem)
        {
            return typedItem;
        }

        // 타입이 일치하지 않을 경우 null 리턴
        Debug.LogWarning($"ID {itemID} 아이템은 {typeof(T).Name} 타입이 아닙니다. (실제 타입: {item.GetType().Name})");
        return null;
    }
}
