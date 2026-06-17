/// <summary>
/// 게임 내 모든 Enum형을 관리합니다
/// </summary>

// 피해를 받는 대상이 될 수 있는 것 타입
public enum Faction 
{ 
    player,     // 플래이어
    enemy,      // 적
    neutral     // 그 외
}

public enum itemCategory
{
    idle,           // 기타 아이템
    weapon,         // 무장
    armor,          // 방어구
    consume         // 소모품
}
