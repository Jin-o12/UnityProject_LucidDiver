/// <summary>
/// 게임 내 모든 Enum형을 관리합니다
/// </summary>

// 캐릭터 종류
public enum UserType
{
    main,           // 메인 다이버
    support         // 적
}

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
    consume,        // 소모품
    memory,         // 기억 파편
    artifact        // 장비형 아이템
}

// 아이템 사용 방법 종류
public enum AreaType
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

// 아이템&스킬 효과 종류
public enum EffectType
{
    // 플레이어 & 적 공통 사용
    none,                   // 효과 할당되지 않음 (오류)
    healHP,                 // 체력 즉시 회복
    healMP,                 // 마나 즉시 회복
    damage,                 // 일정량의 피해를 입음
    
    // 플레이어가 받는 효과
    leap,
    knockback,
    parry,

    // 적이 받는 효과
    vision                  // 자신의 위치를 드러냄
}

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

/* 캐릭터 고유 번호 */
public enum CharacterTID
{
    Yuan = 101          // 유안
}

public enum ItemGrade
{
	empty = 0,     //빈 칸 (default)
	normal = 1,    //일반 등급
	uncommon = 2,  //고급 등급
	rare = 3,      //희귀 등급
	epic = 4,      //서사 등급
	legend = 5     //전설 등급
}

public enum ArtifactCategory
{
    None, // 기본값
    HP_Related, // 체력 관련
    MP_Related, // 마나 관련
    Mobility, // 이동 관련
    TimeLimit, // 시간 제한 관련
    Combat // 전투 관련
}

public enum ArtifactEffectType
{
    none, // 효과 없음
    hpRegen, // 체력 회복
    mpRegen, // 마나 회복
    moveSpeedUp, // 이동 속도 증가
    timeLimitSpeed, // 시간 제한 속도 증가
    fireSpeedUp, // 공격 속도 증가
    fireMPDown // 공격 시 마나 소모 감소
}