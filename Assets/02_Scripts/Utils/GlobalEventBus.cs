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

    /// 아이템 관련 이벤트 ///
    public static Action<int> OnItemPickedUp;                       // 아아템을 주웠을 시: Action<아이템 코드>
    public static Action<int> OnWeaponEquipped;                     // 장비가 장착될 시: Action<무기 코드>

    /// 전투 관련 이벤트 ///
    public static Action<float, float> OnPlayerHealthChanged;       // 플레이어 체력 변동: Action<현재 체력, 전체 체력>
    public static Action<int, float, float> OnEnemyHealthChanged;   // 적의 체력 변동: Action<고유 번호, 현재 체력, 전체 체력>
    public static Action<int> EnemyDead;                            // 적의 사망 여부: Action<고유 번호>
}
