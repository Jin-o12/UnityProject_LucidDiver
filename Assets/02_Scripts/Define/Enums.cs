/// <summary>
/// 게임 내 모든 Enum형을 관리합니다
/// </summary>

// 캐릭터 종류
public enum UserType
{
    main,       // 메인 다이버
    enemy       // 적
}

// 아이템 종류
public enum ItemType
{
    weapon,         // 무장
    consume,        // 소모품
    memory          // 기억 파편
}

// 아이템 사용 범위 설정 종류
public enum AreaType
{
    self,             // 사용자가 대상을 선택
    single
}

// 아이템 효과 적용 대상
public enum TargetType
{
    none,               // 사용되지 않음
    self,               // 플레이어 자신
    enemy,              // 적 캐릭터
    main                // 다이버 (메인 플레이어 캐릭터)
}

// 스킬&아이템 효과 종류
public enum EffectType
{
    none,                   // 사용되지 않음
    damage,                 // 보유 체력 피해
    leap,                   // 돌진
    hp_recover_inst,        // 체력 즉시 회복
    mana_recover_inst       // 마나 즉시 회복
}

// 대사 출력 상황
public enum DialogueType
{
    lobbyEnter,         // 관제실 화면 진입 시
    storyOpen,          // 개방된 심상 기록
    worldEnter,         // 인게임 세션 진입 시 말풍선 툴팁
    enemyEncounter,     // 인게임에서 적 캐릭터 발견 시 말풍선 툴팁
    hitAttack,          // 인게임에서 적 공격에 피격 시 말풍선 툴팁
    getMemory,          // 인게임에서 기억 파편 획득 시 말풍선 툴팁
    getConsume,         // 인게임에서 소비 기물 획득 시 말풍선 툴팁
    escapeSuccess,      // 탈출 성공 시
    escapeFailed        // 탈출 실패 시
}

// 아이템 보관함 오브젝트 종류
public enum BoxSize
{
    small,      // 소형 상자
    medium,     // 중형 상자
    large       // 대형 상자
}

// 스킬 종류
public enum SkillType
{
    normal
}

// 무장 종류
public enum WeaponType
{
    pistol      // 에너지 권총
}

// 탈출 조건 종류
public enum ExitCondition
{
    none,       // 조건 없음
}

// 피해를 받는 대상이 될 수 있는 것 타입
public enum Faction 
{ 
    player,     // 플래이어
    enemy,      // 적
    neutral     // 그 외
}