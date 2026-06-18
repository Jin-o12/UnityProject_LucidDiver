/// <summary>
/// 글로벌 이벤트 버스 클래스
/// 게임 내에서 발생하는 다양한 이벤트를 중앙에서 관리하고 전달하는 역할을 합니다.
/// </summary>
using System;
using UnityEngine;

public class GlobalEventBus
{
    /// 플레이어 행동에 의한 이벤트 ///
    public static Action<Vector2> OnPlayerMove;                     // 플레이어 이동 버튼 입력: Action<이동 버튼 입력 벡터>
    public static Action OnAttackInput;                             // 공격 버튼 입력
    public static Action OnInteractionInput;                        // 상호작용 버튼 입력
    public static Action<Vector2> OnMousePositionInput;             // 마우스의 현재 화면 죄표 전달: Action<화면 상의 좌표>
    public static Action<Transform> OnPlayerSpawned;                // 플레이어 생성 시 위치 정보 전달: Action<플레이어의 Transform>
    public static Action<Transform> OnPlayerDespawned;              // 플레이어 제거 시 위치 정보 전달: Action<플레이어의 Transform>

    /// 아이템 관련 이벤트 ///
    public static Action<int, int, int> OnItemPickedUp;             // 아아템을 주웠을 시: Action<플레이어 식별자 코드, 아이템 코드, 아이템 갯수>

    /// 전투 관련 이벤트 ///
    public static Action<float, float> OnPlayerHealthChanged;       // 플레이어 체력 변동: Action<현재 체력, 전체 체력>
    public static Action<float, float> OnPlayerManaChanged;         // 플레이어 마나 변동: Action<현재 마나, 전체 마나>
    public static Func<float, bool> OnRequestManaConsume;           // 플레이어 마나 소비 요청: Func<소모량, 성공여부>
    public static Action<int, float, float> OnEnemyHealthChanged;   // 적의 체력 변동: Action<고유 번호, 현재 체력, 전체 체력>
    public static Action<int> EnemyDead;                            // 적의 사망 여부: Action<고유 번호>
}
