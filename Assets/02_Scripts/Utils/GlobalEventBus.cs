using System;
using UnityEngine;

/// <summary>
/// 전역 이벤트 버스 클래스.
/// 게임 안에서 발생하는 여러 이벤트를 중앙에서 전달할 때 사용한다.
/// </summary>
public class GlobalEventBus
{
    /// <summary>
    /// 플레이어 입력 관련 이벤트
    /// </summary>
    public static Action<Vector2> OnPlayerMove;                  // 플레이어 이동 입력
    public static Action OnAttackInput;                          // 공격 입력
    public static Action OnInteractionInput;                     // 상호작용 입력
    public static Action<Vector2> OnMousePositionInput;          // 마우스 위치 전달
    public static Action<GameObject> OnPlayerSpawned;            // 플레이어 생성 전달
    public static Action<Transform> OnPlayerDespawned;           // 플레이어 제거 전달

    /// <summary>
    /// 아이템 관련 이벤트
    /// </summary>
    public static Action<int, int, int, IInteractable> OnItemPickedUp;   // 아이템 획득
    public static Action<IInteractable, int> OnItemBoxOpened;             // 상자 열기 요청
    public static Action<int, Sprite, int> OnQuickSlotChanged;            // 퀵슬롯 정보 변경
    public static Action<int> OnQuickSlotUseRequested;                    // 퀵슬롯 사용 요청
    public static Action<int, int> OnSwapInventorySlot;                   // 인벤토리 슬롯 교환
    public static Action<int> OnInventoryDropRequested;                   // 인벤토리 아이템 버리기 요청
    public static Action<int, int> OnDropItemQuickSlot;                   // 인벤토리 -> 퀵슬롯 드랍
    public static Action<int, int> OnSwapItemQuickSlot;                   // 퀵슬롯끼리 교환

    /// <summary>
    /// 아이템 효과 이벤트
    /// </summary>
    public static Action<GameObject, float> OnHealRequested;     // 체력 회복 요청
    public static Action<GameObject, float> OnGainManaRequested; // 마나 회복 요청

    /// <summary>
    /// 전투/상태 관련 이벤트
    /// </summary>
    public static Action<float, float> OnPlayerHealthChanged;    // 플레이어 체력 변화
    public static Action<int> onPlayerDead;                      // 플레이어 사망
    public static Action<float, float> OnPlayerManaChanged;      // 플레이어 마나 변화
    public static Func<float, bool> OnRequestManaConsume;        // 플레이어 마나 소비 요청
    public static Action<int, float, float> OnEnemyHealthChanged;// 적 체력 변화
    public static Action<int> OnEnemyDead;                       // 적 사망

    /// <summary>
    /// 게임 시스템 관련 이벤트
    /// </summary>
    public static Action<bool> OnEscapeRequest;                  // 탈출 판정 요청
    public static Action OnReturnToLobby;                        // 로비 복귀 요청
}
