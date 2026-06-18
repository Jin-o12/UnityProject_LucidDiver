
/// <summary>
/// 인벤토리 슬롯 하나에 해당하는 데이터 구조에 대한 클래스입니다
/// 이후 프로그램의 확장이나 기능 추가에 따라 구조가 바뀔 수 있습니다
/// </summary>
using UnityEngine;
using UnityEngine.UI;
[System.Serializable]
public class InventorySlotData
{
    public int TID;         // itemData의 TID와 매칭 (0이면 빈 칸)
    public int order;       // 인벤토리상의 순서
    public int amount;      // 현재 가지고 있는 수량
    public Sprite icon;     // 아이템 이미지

    // 생성자
    public InventorySlotData(int _id, int _order, int _amount, Sprite _icon)
    {
        TID = _id;
        order = _order;
        amount = _amount;
        icon = _icon;
    }
}