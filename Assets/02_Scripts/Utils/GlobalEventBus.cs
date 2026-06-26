using System;
using UnityEngine;

/// <summary>
/// 전역 이벤트 버스 클래스.
/// 게임 안에서 발생하는 여러 이벤트를 중앙에서 전달할 때 사용한다.
/// </summary>
public class GlobalEventBus
{
    /// <summary>

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

    /// 인게임 이벤트
    /// </summary>

    /// 플레이어 행동에 의한 이벤트 ///
    public static Action<Vector2> OnPlayerMove;                     // 플레이어 이동 버튼 입력: Action<이동 버튼 입력 벡터>
    public static Action OnAttackInput;                             // 공격 버튼 입력
    public static Action OnInteractionInput;                        // 상호작용 버튼 입력
    public static Action<Vector2> OnMousePositionInput;             // 마우스의 현재 화면 좌표 전달: Action<화면 상의 좌표>
    public static Action<GameObject> OnPlayerSpawned;               // 플레이어 생성 시 위치 정보 전달: Action<플레이어의 Transform>
    public static Action<Transform> OnPlayerDespawned;              // 플레이어 제거 시 위치 정보 전달: Action<플레이어의 Transform>
    public static Action<int, int, Sprite, int> QuickSlotLoad;      // 퀵슬롯 데이터 불러오기 시: Action<퀵슬롯 번호, 아이템 TID, 아이템 아이콘, 아이템 개수)>

    /// 특정 효과 발동 이벤트 ///
    public static Action<GameObject, float> OnHealRequested;        // 체력 회복 효과: Action<대상, 값>
    public static Action<GameObject, float> OnGainManaRequested;    // 마나 회복 효과: Action<대상, 값>

    /// <summary>
    /// 전투/상태 관련 이벤트
    /// </summary>
    public static Action<float, float> OnPlayerHealthChanged;    // 플레이어 체력 변화
    public static Action<int> onPlayerDead;                      // 플레이어 사망
    public static Action<float, float> OnPlayerManaChanged;      // 플레이어 마나 변화
    public static Func<float, bool> OnRequestManaConsume;        // 플레이어 마나 소비 요청
    public static Action<int, float, float> OnEnemyHealthChanged;// 적 체력 변화
    public static Action<int> OnEnemyDead;                       // 적 사망

    /// 게임 시스템 관련 이벤트 ///
    public static Action<bool> OnEscapeRequest;                     // 탈출 판정 발생 시: Action<탈출 성공 여부>
    public static Action OnReturnToLobby;                           // 로비로 돌아가기 이벤트 발생
    public static Action<int, bool> OnSetRecordData;                // 탈출 판정으로 다이버 기록 데이터 변경 시: Action<newLinkRateLevel, newMemoryLogUnlocked>
    public static Action<IInteractable, int> OnItemBoxOpened;       // 상자와 상호작용하여 UI를 열었을 시: Action<열린 상자, 상호작용한 플레이어 ID>

    /// UI 관리 이벤트 ///
    public static Action OnOpenPrepareUI;                          // 출격 준비 UI 오픈 
    public static Action OnOpenRecordUI;                           // 다이버/기록 UI 오픈 
    public static Action<int, bool, bool> RecordDataLoad;           // 다이버 개인 심상 기록 데이터 전달 : Action<newLinkRateLevel, newMemoryLogUnlocked, newHasNewMemoryLog>
    public static Action OnRecordRead;                              // 다이버 개인 심상 기록 읽음

    /// <summary>
    /// 로비 씬 이벤트
    /// </summary>
    
    /// UI 관리 이벤트 ///
    public static Action OnOpenLobbyUI;                             // 로비 UI 실행
    public static Action OnOpenPrepareUI;                           // 출격 준비 UI 실행
    public static Action OnOpenRecordUI;                            // 다이버 기록 UI 실행
}
