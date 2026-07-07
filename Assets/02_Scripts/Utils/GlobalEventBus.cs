/// <summary>
/// 글로벌 이벤트 버스 클래스
/// 게임 내에서 발생하는 다양한 이벤트를 중앙에서 관리하고 전달하는 역할을 합니다.
/// </summary>
using System;
using UnityEngine;

public class GlobalEventBus
{
    /// <summary>
    /// 플레이어 입력 관련 이벤트
    /// </summary>

    /// 플레이어 행동에 의한 이벤트 ///
    public static Action<Vector2> OnPlayerMove;                     // 플레이어 이동 버튼 입력: Action<이동 버튼 입력 벡터>
    public static Action OnAttackInput;                             // 공격 버튼 입력
    public static Action OnMainActiveSkillRequested;                // 다이버 액티브 스킬 버튼 입력
    public static Action OnInteractionInput;                        // 상호작용 버튼 입력
    public static Action<bool> OnSprintInput;                       // 달리기 버튼 입력: Action<isSprint>
    public static Action<bool> SendCanSprint;                       // 달리기 여부 계산 전달: Action<canSprint>
    public static Action<bool> SendCannotSprint;                    // 마나 소진으로 달리기 불가 상태 전달: Action<cannotSprint>
    public static Action OnEvadeRequested;                          // 구르기 버튼 입력
    public static Action<Vector2> OnMousePositionInput;             // 마우스의 현재 화면 좌표 전달: Action<화면 상의 좌표>
    public static Action<GameObject> OnPlayerSpawned;               // 플레이어 생성 시 위치 정보 전달: Action<플레이어의 Transform>
    public static Action<Transform> OnPlayerDespawned;              // 플레이어 제거 시 위치 정보 전달: Action<플레이어의 Transform>

    /// <summary>
    /// 아이템 상호작용 이벤트
    /// </summary>
    public static Action<int, int, int> OnItemPickedUp;             // 아아템을 주웠을 시: Action<플레이어 식별자 코드, 아이템 코드, 아이템 갯수>
    public static Action<int, Sprite, int> OnQuickSlotChanged;      // 퀵슬롯 정보가 변경되었을 시: Action<퀵슬롯 번호, 이미지, 갯수>
    public static Action<int> OnQuickSlotUseRequested;              // 특정 퀵슬롯 번호가 눌렷을 시: Action<퀵슬롯 번호>
    public static Action<int, int> OnSwapInventorySlot;             // 인벤토리 슬롯이 바뀌었을 시: Action<1번 슬롯 번호, 2번 슬롯 번호>
    public static Action<int> OnInventoryDropRequested;             // 인벤토리 아이템을 바닥에 버리기 요청할 시: Action<인벤토리 슬롯 번호>
    public static Action<int, int> OnDropItemQuickSlot;             // 인벤토리에서 퀵슬롯에 아이템을 드래그 앤 드롭 할 시: Action<퀵슬롯 번호, 인벤토리 번호> | 인벤토리->퀵슬롯
    public static Action<int, int> OnSwapItemQuickSlot;             // 퀵슬롯에서 퀵슬롯으로 아이템을 드래그 앤 드롭 할 시: Action<1번 퀵슬롯 번호, 2번 퀵슬롯 번호)> | 2번슬롯->1번슬롯
    public static Action<int, int, Sprite, int> QuickSlotLoad;      // 퀵슬롯 데이터 불러오기 시: Action<퀵슬롯 번호, 아이템 TID, 아이템 아이콘, 아이템 개수)>

    /// <summary>
    /// 아이템 효과 이벤트
    /// </summary>
    public static Action<GameObject, float> OnHealRequested;        // 체력 회복 효과: Action<대상, 값>
    public static Action<GameObject, float> OnGainManaRequested;    // 마나 회복 효과: Action<대상, 값>

    /// <summary>
    /// 전투 관련 이벤트
    /// </summary>
    public static Action<float, float> OnPlayerHealthChanged;       // 플레이어 체력 변동: Action<현재 체력, 전체 체력>
    public static Action<int> onPlayerDead;                         // 플레이어 사망 여부: Action<고유 번호>
    public static Action<float, float> OnPlayerManaChanged;         // 플레이어 마나 변동: Action<현재 마나, 전체 마나>
    public static Action<float> OnSprintManaConsume;                // 플레이어 달리기 시 마나 소비: Action<소모량>
    public static Func<float, bool> OnRequestManaConsume;           // 플레이어 마나 소비 요청: Func<소모량, 성공여부>
    public static Action<float> OnTimePenaltyRequested;             // 루시드 낙인 등으로 남은 제한 시간 감소 요청: Action<감소 초>
    public static Action<int, float, float> OnEnemyHealthChanged;   // 적의 체력 변동: Action<고유 번호, 현재 체력, 전체 체력>
    public static Action<int> OnEnemyDead;                          // 적의 사망 여부: Action<고유 번호>
    public static Action<float> OnTimerChanged;                     // 남은 제한 시간 변동: Action<남은 시간>
    public static Action OnHitAnimate;                              // 플레이어 피격 애니메이션 발생

    /// <summary>
    /// 게임 시스템 관련 이벤트
    /// </summary>
    public static Action<bool> OnEscapeRequest;                     // 탈출 판정 발생 시: Action<탈출 성공 여부>
    public static Action<int> OnEscapeFailure;                      // 탈출 취소 판정 발생: Action<플레이어 ID>
    public static Action OnTimeOver;                                // 제한 시간 종료 시
    public static Action OnReturnToLobby;                           // 로비로 돌아가기 이벤트 발생
    public static Action<IInteractable, int> OnItemBoxOpened;       // 상자와 상호작용하여 UI를 열었을 시: Action<열린 상자, 상호작용한 플레이어 ID>
    public static Action<NoiseStimulus> OnNoiseRequested;           // 노이즈 시스템에 소음 발생을 요청: Action<요청된 소음 데이터>
    public static Action<NoiseStimulus> OnNoiseEmitted;             // 노이즈 매니저가 실제 처리한 소음 전달: Action<확정된 소음 데이터>

    /// <summary>
    /// UI 관리 이벤트
    /// </summary>
    public static Action OnOpenLobbyUI;                                     // 로비 UI 실행
    public static Action OnRequestQuickSlotCache;                           // 출격 준비 UI에서 퀵슬롯 캐시 재전송 요청
    public static Action OnOpenPrepareUI;                                   // 출격 준비 UI 오픈 (LobbyMainUI → LobbyPresenter)
    public static Action PrepareUIOpen;                                     // 출격 준비 UI 오픈 (SortiePrepareUI → ResultManager)
    public static Action OnOpenRecordUI;                                    // 다이버/기록 UI 오픈  (LobbyMainUI → LobbyPresenter)
    public static Action RecordUIOpen;                                      // 다이버/기록 UI 오픈  (DiverRecordUI → ResultManager)
    public static Action OnOpenStorageUI;                                   // 개인 창고 UI 오픈
    public static Action<string, CharacterTID> OnOpenRecordCardPopUpUI;     // 기록 카드 팝업 열기 요청: Action<기록 제목, 기록을 열 캐릭터의 ID>
    public static Action<int, bool, bool> RecordDataLoad;                   // 다이버 개인 심상 기록 데이터 전달 : Action<newLinkRateLevel, newMemoryLogUnlocked, newHasNewMemoryLog>
    public static Action OnRecordRead;                                      // 다이버 개인 심상 기록 읽음

    /// <summary>
    /// 씬 전환 이벤트
    /// </summary>
    public static Action OnGoToLobbyScene;                                  // 로비 씬으로 이동
    public static Action OnGoToGameScene;                                   // 게임 씬으로 이동
}
