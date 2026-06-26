using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 아이템 데이터의 기본 정보를 담는 ScriptableObject 클래스.
/// 각 아이템 종류별 상세 데이터는 이 클래스를 상속해서 확장한다.
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("아이템 기본 정보")]
    public int TID;                                     // 아이템 코드(고유 ID)
    public string itemName;                             // 아이템 이름
    public string desc;                                 // 아이템 설명
    public ItemType category;                       // 아이템 종류
    public int effectID;                                // 아이템 종류별 적용 효과 ID
    public int itemMultiple;                            // 중첩 용량

    [Header("참조 리소스")]
    public AssetReferenceSprite icon;             // UI 아이콘
    public AssetReferenceGameObject itemPrefabRef;// 장착/사용에 쓰는 프리팹
    public GameObject dropPrefab;                 // 바닥 드랍에 쓰는 프리팹
}
