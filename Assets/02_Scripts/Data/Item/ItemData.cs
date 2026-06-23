/// <summary>
/// 아이템 데이터의 기본 틀이 되는 ScriptableObject 클래스
/// 각 아이템은 N*101번대부터 시작하며, 1의 자리가 0인 경우의 TID는 비워놓습니다
/// 
/// * 현재 아이템 번호 규칙
///  - 100번대: 무기
///  - 200번대: 방어구 (추후 구현)
///  - 300번대: 소모품
///  - 400번대: 그 외 아이템
/// 위 내용은 기획서의 변동에 따라 내용이 달라지거나 삭제될 수 있습니다.
/// </summary>
using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class ItemData : ScriptableObject
{
    [Header("아이템 기본 정보")]
    public int TID;                                     // 아이템 코드(고유 ID)
    public string itemName;                             // 아이템 이름
    public string desc;                                 // 아이템 설명
    public itemCategory category;                       // 아이템 종류
    public int effectID;                                // 아이템 종류별 적용 효과 ID
    public int itemMultiple;                            // 중첩 용량

    [Header("연관 파일 주소")]
    public AssetReferenceSprite icon;                   // 아이템 아이콘
    public AssetReferenceGameObject itemPrefabRef;      // 아이템 프리팹 주소
}
