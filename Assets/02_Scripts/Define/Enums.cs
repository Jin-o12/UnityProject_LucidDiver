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

// 아이템 카테고리
public enum itemCategory
{
    idle,           // 기타 아이템
    weapon,         // 무장
    armor,          // 방어구
    consume         // 소모품
}

// 아이템 사용 방법 종류
public enum UseType
{
    target,             // 사용자가 대상을 선택
    circle_zone,        // 효과가 발동할 원형 구역의 중심 지점을 선택
    raycast,            // 사용 처리 판정 ray를 발사할 방향을 선택
    rectangle           // rectangle
}

// 아이템 효과 적용 대상
public enum EffectTarget
{
    self,               // 플레이어 자신
    enemy               // 적 캐릭터
}

// 아이템 효과 종류
public enum EffectType
{
    health_recover_inst,    // 체력 즉시 회복
    mana_recover_inst       // 마나 즉시 회복
}

// 기억 파편 사용 가능 캐릭터 타입
public enum UserType
{
    main                // 메인 캐릭터
}