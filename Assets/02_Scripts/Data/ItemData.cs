using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("아이템 기본 정보")]
    public int TID;             // 아이템 코드(고유 ID)
    public string itemName;     // 아이템 이름
    public string icon;         // 아이템 아이콘
    public int slotSize;        // 아이템이 차지하는 슬롯 수
    public string desc;         // 아이템 설명

}
