using UnityEngine;

/// <summary>
/// 상자 랜덤 생성에 사용할 후보 아이템 1개에 대한 정보입니다.
/// 인스펙터에서 아이템, 확률 가중치, 수량 범위를 설정합니다.
/// </summary>
[System.Serializable]
public class BoxLootOption
{
    public ItemData itemData;   // 후보 아이템 데이터
    public int weight = 1;      // 등장 확률 가중치
    public int minAmount = 1;   // 최소 생성 수량
    public int maxAmount = 1;   // 최대 생성 수량
}