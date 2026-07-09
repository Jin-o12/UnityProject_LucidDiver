/// <summary>
/// 아이템 박스에서 아티팩트 카테고리가 선택된 뒤 등급별로 다시 판정할 후보입니다.
/// 현재 P0.5에서는 등급당 아티팩트 1종을 TID로 연결합니다.
/// </summary>
[System.Serializable]
public class BoxArtifactLootOption
{
    public ItemGrade grade = ItemGrade.normal; // 아티팩트 등급 구분
    public int itemTid = 1001;                 // 해당 등급에 대응하는 아티팩트 TID
    public int weight = 1;                     // 등급 선택 가중치
}
