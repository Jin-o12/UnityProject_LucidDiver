using System.Collections.Generic;

/// <summary>
/// 아이템 박스가 랜덤 보상을 만들 때 먼저 선택할 아이템 카테고리 후보입니다.
/// 카테고리 가중치와 해당 카테고리에서 뽑을 아이템 TID 목록을 인스펙터에서 관리합니다.
/// </summary>
[System.Serializable]
public class BoxLootCategoryOption
{
    public itemCategory category = itemCategory.consume; // 먼저 판정할 아이템 카테고리
    public int weight = 1;                               // 카테고리 선택 가중치
    public int minAmount = 1;                            // 해당 카테고리 아이템 최소 수량
    public int maxAmount = 1;                            // 해당 카테고리 아이템 최대 수량
    public List<int> itemTids = new();                   // 이 카테고리에서 랜덤 선택할 아이템 TID 목록
}
