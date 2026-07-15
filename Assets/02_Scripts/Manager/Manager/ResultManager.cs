/// <summary>
/// 인게임 세션 종료 시 데이터 변동을 관리하는 클래스
/// (탈출 성공 여부, 플레이 타임, 인벤토리 및 퀵슬롯, 동조율 단계)
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ResultManager : MonoBehaviour, IResultService
{
    public static ResultManager Instance { get; private set; }  //싱글톤 인스턴스 지정
    // playerID -> PlayerStatus 매핑 딕셔너리
    public readonly Dictionary<int, PlayerStatus> _players = new Dictionary<int, PlayerStatus>();

    // 플레이 타임 기록 필드
    private float playTime;                             //이번 세션 플레이 시간
    private float startTime;                            //플레이 시작 시점
    // 탈출 여부 및 결과 창 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    private ResultUI resultPanel;                       //결과 창 UI 
    private Coroutine resultCoroutine;                  //결과 창 출력 코루틴
    // 동조율 저장 필드
    private int prevLinkRateLevel;                      //동조율 상승 전 다이버와의 동조율 단계 값을 저장
    public int linkRateLevel;                           //동조율 상승 후 다이버와의 동조율 단계
    private int linkRateGain = 1;                       //P0: 세션 탈출 성공 시 가산되는 동조율 단계 증가치
    public float linkRatePoint = 1.0f;                  //세션 탈출 성공 시 기억 파편이 전환되는 동조율 경험치 값
    private bool linkRateUp = false;                    //동조율 단계 상승 여부 전달
    private bool MemoryLogUnlocked = false;             //세션 탈출 시 개인 심상 기록 해금 여부 저장
    private bool hasNewMemoryLog = true;                //개인 심상 기록 확인 여부 저장
    // 인벤토리 기록에 관한 필드
    private PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    private PlayerInventory _inven;                     //현재 인게임 세션의 플레이어 인벤토리 데이터
    private PlayerArtifactEquipment artifactEquipment;  //플레이어가 장착한 아티팩트 데이터
    private ItemData memoryFragmentData;                //기억 파편 아이템 데이터
    private int memoryFragmentCount;                    //기억 파편 개수
    // 퀵슬롯 저장 필드
    public int slotTID1;                                //1번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite1;                          //1번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount1;                              //1번 슬롯 아이템의 개수 데이터를 받아옴
    public int slotTID2;                                //2번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite2;                          //2번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount2;                              //2번 슬롯 아이템의 개수 데이터를 받아옴
    public int slotTID3;                                //3번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite3;                          //3번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount3;                              //3번 슬롯 아이템의 개수 데이터를 받아옴

    // 저장 데이터 인터페이스
    private IItemDataRepository itemRepo;               // 아이템 데이터 접근 인터페이스
    private ICharDataRepository charRepo;               // 캐릭터 데이터 접근 인터페이스
    
    private void Awake()
    {
        // 싱글톤 인스턴스 중복 방지 설정
        if (Instance != null)
        {
            // 같은 Manager 오브젝트에 다른 매니저 컴포넌트가 함께 붙어 있으므로 오브젝트 전체가 아니라 ResultManager 컴포넌트만 제거합니다.
            Destroy(this);
            return;
        }
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // ResultServiceLocator에 자신을 등록
        ResultServiceLocator.Instance = this;
        // 인터페이스 등록
        
        itemRepo = new LocalJsonItemRepository();
        charRepo = new SOCharacterRepository();

        // 씬에 이미 존재하는 PlayerStatus를 찾아 등록 (타이밍 안전성 보장)
        foreach (var p in FindObjectsOfType<PlayerStatus>())
        {
            if (p == null) continue;
            var idComp = p.GetComponent<EntityIdentity>();
            if (idComp == null)
            {
                Debug.LogWarning($"ResultManager Awake: EntityIdentity 없음 - gameObject={p.gameObject.name}");
                continue;
            }
            // Register는 내부적으로 동일 key를 덮어쓰므로 중복 걱정 없음
            Register(idComp.entityID, p);
            Debug.Log($"ResultManager Awake: Registered existing playerID={idComp.entityID} (gameObject={p.gameObject.name})");
        }
        
        //로비로 돌아가기 버튼에 결과 창 닫기 연결
        GlobalEventBus.OnReturnToLobby += CloseResultPanel;
        //출격 준비 UI의 퀵슬롯 캐시 재전송 요청 이벤트 연결
        GlobalEventBus.OnRequestQuickSlotCache += SendQuickSlotCacheEvent;
        //출격 준비 UI 오픈 이벤트 연결
        GlobalEventBus.PrepareUIOpen += SendQuickSlotCacheEvent;
        //다이버/기록 UI 오픈 이벤트 연결
        GlobalEventBus.RecordUIOpen += SendLinkRecordData;
        //다이버/기록 UI 읽음 이벤트 연결
        GlobalEventBus.OnRecordRead += NewMemoryRead;
    }

    private void OnDestroy()  //IResultService 구현체 (로케이터에 등록)
    {
        GlobalEventBus.OnReturnToLobby -= CloseResultPanel;
        GlobalEventBus.OnRequestQuickSlotCache -= SendQuickSlotCacheEvent;
        GlobalEventBus.PrepareUIOpen -= SendQuickSlotCacheEvent;
        GlobalEventBus.RecordUIOpen -= SendLinkRecordData;
        GlobalEventBus.OnRecordRead -= NewMemoryRead;
    }

    public void RefreshPlayerCache()
    {
        // 기존 매핑 중 파괴된 오브젝트 제거
        var keysToRemove = new List<int>();
        foreach (var kv in _players)
        {
            if (kv.Value == null) keysToRemove.Add(kv.Key);
        }
        foreach (var key in keysToRemove)
        {
            _players.Remove(key);
            Debug.Log($"ResultManager.RefreshPlayerCache: Removed null playerID={key}");
        }

        // 씬의 모든 PlayerStatus를 스캔해 등록
        foreach (var p in FindObjectsOfType<PlayerStatus>())
        {
            if (p == null) continue;
            if (!p.TryGetComponent<EntityIdentity>(out var idComp)) { Debug.LogWarning($"ResultManager.RefreshPlayerCache: EntityIdentity 없음 - {p.gameObject.name}"); continue; }
            if (!_players.ContainsKey(idComp.entityID))
            {
                _players[idComp.entityID] = p;
                Debug.Log($"ResultManager.RefreshPlayerCache: Registered playerID={idComp.entityID} ({p.gameObject.name})");
            }
        }
    }

    // 플레이어 등록
    public void Register(int playerID, Component ps)
    {
        // 플레이어 상태 값 null 체크를 먼저 실행
        if (ps == null) return;
        // 플레이어 EntityIdentity 컴포넌트를 가져오고 null 체크
        var idComp = ps.GetComponent<EntityIdentity>();
        if (idComp == null) return;
        // EntityIdentity에서 ID 값을 불러옴
        _players[playerID] = (PlayerStatus)ps;
    }

    // 플레이어 등록 해제
    public void Unregister(int playerID)
    {
        // 플레이어 ID 값을 제외
        _players.Remove(playerID);
    }

    // 플레이어가 requester 자신인 경우에 등록 해제
    public void UnregisterIfOwner(int playerID, Component requester)
    {
        // 딕셔너리에 등록된 것이 requester 자신인 경우에만 제거
        if (_players.TryGetValue(playerID, out var current) && current == requester)
        {
            _players.Remove(playerID);
        }
    }

    // 조회 유틸
    public Component GetPlayerComponent(int playerID)
    {
        // playerID에 매핑된 PlayerStatus를 가져옴
        if (_players.TryGetValue(playerID, out var ps)) return ps;
        // 매핑되지 않았으면 null 처리
        else return null;
    }

    // 탈출 채널링 시작 처리
    public void HandleEscapeStart(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.escape);

    // 강제 각성(게임오버) 처리
    public void HandleEscapeGameover(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.gameover);

    //탈출 취소로 기본 상태로 돌아가는 처리
    public void HandleEscapeIdle(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.idle);

    // 플레이어 상태 변경
    private void SetPlayerState(int playerID, PlayerStatus.livingState state)
    {
        if (_players.TryGetValue(playerID, out var ps)) ps.SetPlayerState(state);
    }

    public void GameResult(bool _extractionResult, float beginTime)
    {
        // 탈출 성공 여부를 가장 먼저 기록
        extractionResult = _extractionResult;
        // 이번 세션에서의 플레이 시간을 계산
        startTime = beginTime;
        playTime = Time.time - startTime;

        // {StorageInventoryUI가 저장한 최신 창고 데이터를 파일에서 다시 불러온다}
        PlayerSaveDataSO.Instance.LoadSaveData();
        // 플레이어 세이브 데이터를 가져옴
        _playerSaveData = PlayerSaveDataSO.Instance.currentData;
        // {저장 리스트가 null이면 보정한다}
        EnsureSaveLists();
        // 플레이어 오브젝트에서 PlayerInventory 컴포넌트를 찾아 데이터 동기화
        _inven = FindObjectOfType<PlayerInventory>();
        if (_inven == null)
        {
            Debug.LogWarning("PlayerInventory를 찾을 수 없습니다.");
            return;
        }
        // 인벤토리 및 퀵슬롯 데이터를 불러와 동기화 갱신
        InventorySync();
        CacheQuickSlotData(_extractionResult);

        // 스냅샷 방식: (최종 인벤토리 상태) - (게임 시작 시점 상태) = 이번 세션 순수 획득량
        SessionDataSO.Instance.AcquiredItems.Clear();

        // 1. 현재 인벤토리의 모든 아이템을 TID 기준으로 합산
        Dictionary<int, int> finalItems = new Dictionary<int, int>();
        foreach (InventorySlotData slot in _inven.anySlots)
        {
            if (slot == null || slot.TID == 0 || slot.amount <= 0) continue;
            
            if (finalItems.ContainsKey(slot.TID))
                finalItems[slot.TID] += slot.amount;
            else
                finalItems[slot.TID] = slot.amount;
        }

        // 1.5. 장착 중인 아티팩트도 최종 소지량에 합산
        if (artifactEquipment != null && artifactEquipment.equippedArtifacts != null)
        {
            foreach (var artifact in artifactEquipment.equippedArtifacts)
            {
                if (artifact == null || artifact.TID == 0) continue;

                if (finalItems.ContainsKey(artifact.TID))
                    finalItems[artifact.TID] += 1;
                else
                    finalItems[artifact.TID] = 1;
            }
        }

        if (_extractionResult)
        {
            // 탈출 성공: 합산된 최종 수량에서 시작 수량을 뺀 순수 변화량을 기록 (양수: 획득, 음수: 손실)
            foreach (var kvp in finalItems)
            {
                int tid = kvp.Key;
                int finalAmount = kvp.Value;
                int startingAmount = 0;

                if (SessionDataSO.Instance.StartingItems.TryGetValue(tid, out int amount))
                {
                    startingAmount = amount;
                }

                int acquiredAmount = finalAmount - startingAmount;
                if (acquiredAmount != 0)
                {
                    SessionDataSO.Instance.AddAcquiredItem(tid, acquiredAmount);
                }
            }

            // 시작 시점에는 있었지만 최종 인벤토리에는 없는 아이템 = 전량 손실
            foreach (var kvp in SessionDataSO.Instance.StartingItems)
            {
                int tid = kvp.Key;
                if (!finalItems.ContainsKey(tid))
                {
                    SessionDataSO.Instance.AddAcquiredItem(tid, -kvp.Value);
                }
            }
        }
        else
        {
            // 탈출 실패: 최종 인벤토리의 모든 아이템을 전량 손실로 기록
            foreach (var kvp in finalItems)
            {
                SessionDataSO.Instance.AddAcquiredItem(kvp.Key, -kvp.Value);
            }

            // 시작 시점에는 있었지만 최종 인벤토리에는 없는 아이템도 손실로 기록
            foreach (var kvp in SessionDataSO.Instance.StartingItems)
            {
                int tid = kvp.Key;
                if (!finalItems.ContainsKey(tid))
                {
                    SessionDataSO.Instance.AddAcquiredItem(tid, -kvp.Value);
                }
            }
        }

        // SessionDataSO에서 기억 파편(401) 개수 추출 및 데이터 설정
        memoryFragmentCount = 0;
        if (SessionDataSO.Instance.AcquiredItems.TryGetValue(401, out int count))
        {
            memoryFragmentCount = count;
        }
        memoryFragmentData = itemRepo.GetItemDataByID(401);
        // 기억 파편을 사용해 동조율 상승 → 심상 기록 해금 처리를 실행
        LinkRateUp(_extractionResult);
        // 탈출 실패 시 인벤토리 및 아티팩트 슬롯의 아이템을 인벤토리에서 제거
        if (!_extractionResult)
        {
            foreach (var slot in _inven.slots)
            {
                RemoveFromInventory(slot.TID);
            }

            foreach (var eSlot in artifactEquipment.equippedArtifacts)
            {
                if (eSlot != null) RemoveFromInventory(eSlot.TID);
            }
        }
        // 모든 처리 완료 후 후 DataManager에서 playerData를 저장
        PlayerSaveDataSO.Instance.SaveGameData();

        // 저장 처리 후 기존 코루틴을 중단
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }
        // 결과 창 패널 출력 코루틴
        resultCoroutine = StartCoroutine(OpenResultPanel());
    }

    private void InventorySync()
    {
        // PlayerSaveData의 인벤토리 슬롯을 비우고 현재 플레이어 인벤토리 데이터로 채우기
        _playerSaveData.inventorySlots.Clear();
        _playerSaveData.safeSlots.Clear();
        foreach (var slot in _inven.slots)
        {
            // TID가 0이 아닌 슬롯만 저장 (빈 슬롯 제외)
            if (slot.TID != 0)
            {
                _playerSaveData.inventorySlots.Add(new SaveSlotData
                {
                    index = slot.order,
                    TID = slot.TID,
                    amount = slot.amount
                });
            }
        }

        // PlayerSaveData의 각성 보존 슬롯을 비우고 현재 플레이어 각성 보존 데이터로 채우기 
        foreach (var sSlot in _inven.safeSlots)
        {
            // TID가 0이 아닌 슬롯만 저장 (빈 슬롯 제외)
            if (sSlot.TID != 0)
            {
                _playerSaveData.safeSlots.Add(new SaveSlotData
                {
                    index = sSlot.order,
                    TID = sSlot.TID,
                    amount = sSlot.amount
                });
            }
        }

        // PlayerSaveData의 퀵슬롯을 비우고 현재 플레이어 퀵슬롯 데이터로 채우기
        _playerSaveData.quickSlots.Clear();
        _playerSaveData.quickSlots.Add(_inven.quickSlots[0].TID);
        _playerSaveData.quickSlots.Add(_inven.quickSlots[1].TID);
        _playerSaveData.quickSlots.Add(_inven.quickSlots[2].TID);

        // 장착 중인 아티팩트는 인벤토리 슬롯에서 빠져 있으므로 별도 장착 슬롯 데이터로 저장한다.
        // 이 동기화가 없으면 탈출 성공 후 장착 상태의 아티팩트가 저장 목록에 남지 않아 사라질 수 있다.
        artifactEquipment = FindObjectOfType<PlayerArtifactEquipment>();
        if (artifactEquipment != null)
            artifactEquipment.WriteToSave(_playerSaveData);

        // 갱신 후 DataManager에서 playerData를 저장
        //DataManager.Instance.SaveGame();
    }

    // 퀵슬롯 데이터 캐싱 (탈출 실패 시 전부 소실됨)
    private void CacheQuickSlotData(bool _extractionResult)
    {
        slotTID1 = _extractionResult ? _inven.quickSlots[0].TID : 0;
        slotSprite1 = _extractionResult ? _inven.quickSlots[0].icon : null;
        slotCount1 = _extractionResult ? _inven.quickSlots[0].amount : 0;
        slotTID2 = _extractionResult ? _inven.quickSlots[1].TID : 0;
        slotSprite2 = _extractionResult ? _inven.quickSlots[1].icon : null;
        slotCount2 = _extractionResult ? _inven.quickSlots[1].amount : 0;
        slotTID3 = _extractionResult ? _inven.quickSlots[2].TID : 0;
        slotSprite3 = _extractionResult ? _inven.quickSlots[2].icon : null;
        slotCount3 = _extractionResult ? _inven.quickSlots[2].amount : 0;
    }

    // 탈출 성공 여부에 따라 동조율 상승 → 심상 기록 해금을 실행하는 메소드
    private void LinkRateUp(bool _extractionResult)
    {
        // 현재 선택한 캐릭터의 세이브 데이터를 가져옴
        SaveCharacterData saveCharData = PlayerSaveDataSO.Instance.GetNowCharacterData();
        // 현재 선택한 캐릭터의 데이터(레벨별 요구 경험치 등)를 불러옴
        CharacterData charData = charRepo.GetCharacterData(PlayerSaveDataSO.Instance.currentData.SelectCharID);

        // 세이브 데이터에서 이전 동조율 단계 값을 불러와 저장
        prevLinkRateLevel = saveCharData.linkRateLevel;
        
        // 탈출 성공 시 기억 파편 개수만큼 동조율 경험치 값 증가
        if (_extractionResult)
        {
            float linkRatePointAdd = memoryFragmentCount * linkRatePoint;
            saveCharData.TotallinkRateValue += linkRatePointAdd;
        }

        /// 경험치 정산 로직 ///
        float[] requiredExpPerLevel = charData.requireLinkRatePerLevel;
        int currentLevel = saveCharData.linkRateLevel;
        float currentExp = saveCharData.TotallinkRateValue;
        
        int maxLevel = requiredExpPerLevel.Length - 1;

        // 경험치가 다음 레벨 요구치를 충족할 경우 레벨업 및 남은 경험치 이월 처리
        while (currentLevel < maxLevel)
        {
            // 현재 레벨에서 다음 레벨로 가기 위한 요구 경험치
            float requiredExp = requiredExpPerLevel[currentLevel + 1];

            if (currentExp >= requiredExp)
            {
                currentExp -= requiredExp;
                currentLevel++;
            }
            else
            {
                break;
            }
        }
        
        // 정산된 경험치 업데이트
        saveCharData.TotallinkRateValue = currentExp;

        // 다음 동조율 단계 값 갱신
        linkRateLevel = currentLevel;
        
        // 레벨업 증가량 계산
        linkRateGain = linkRateLevel - prevLinkRateLevel;

        // '동조율 단계 상승=true' 조건 계산
        linkRateUp = linkRateGain > 0;
        
        // 기억 파편을 사용해 동조율 단계가 상승했거나 이미 해금 상태(이전 동조율 단계 > 0)라면 '해금됨=true' 전달
        MemoryLogUnlocked = linkRateUp || prevLinkRateLevel > 0;
        
        // 기억 파편을 인벤토리에서 제거 (성공/실패 양쪽 모두 제거 처리는 실행함)
        RemoveFromInventory(401);
        
        // 각성 보존 슬롯의 기억 파편을 제거
        _playerSaveData.safeSlots.RemoveAll(slot => slot != null && slot.TID == 401);
        
        // 동조율 단계 값을 세이브 데이터에 전달
        saveCharData.linkRateLevel = linkRateLevel;
        
        // 세이브 데이터를 저장
        // DataManager.Instance.SaveGame()은 GameResult 끝부분에서 호출됨
    }

    private void RemoveFromInventory(int _tid)  //아이템 ID별로 인벤토리에서 제거
    {
        // {저장 데이터나 인벤토리 슬롯 리스트가 없으면 중단한다}
        if (_playerSaveData == null || _playerSaveData.inventorySlots == null)
        {
            return;
        }

        // {지정한 TID의 아이템을 인벤토리 저장 슬롯에서 제거한다}
        _playerSaveData.inventorySlots.RemoveAll(slot => slot != null && slot.TID == _tid);

        // {지정한 TID의 아이템을 아티팩트 장착 슬롯에서 제거한다}
        _playerSaveData.artifactSlots.RemoveAll(slot => slot != null && slot.TID == _tid);
    }

    // 결과 창 패널 출력 메소드
    public IEnumerator OpenResultPanel()
    {
        // 플레이어 Die 애니메이션 재생 시간만큼 대기 후 UI 오픈
        yield return new WaitForSeconds(1.25f);
        // UIManager에서 Canvas-ResultPanel을 받아와 UI 오픈
        resultPanel = UIManager.Instance.Open<ResultUI>();
        if (resultPanel == null) yield break;
        // 인게임 세션에서 저장된 데이터를 resultPanel에 전달해 UI 갱신
        _playerSaveData = PlayerSaveDataSO.Instance.currentData;
        resultPanel.extractionResult = extractionResult;
        resultPanel.playTime = playTime;

        resultPanel.memoryFragmentCount = memoryFragmentCount;
        resultPanel.memoryFragmentData = memoryFragmentData;
        resultPanel.prevLinkRateLevel = prevLinkRateLevel;
        resultPanel.memoryLogUnlocked = MemoryLogUnlocked;
        resultPanel.linkRateLevel = linkRateLevel;
        resultPanel.linkRateGain = linkRateGain;
        resultPanel.linkRateUp = linkRateUp;
        // 결과 창 UI에 각성 보존 슬롯 출력
        resultPanel.CreateSafeSlots(_inven.safeSlotNum);
        for (int k = 0; k < _inven.safeSlotNum; k++)
        {
            resultPanel.UpdateSafeSlot(k, _inven.safeSlots[k]);
        }
        // 각성 보존 슬롯 인덱스 보정용 인벤토리 칸 수 전달
        resultPanel.invenSlotsCount = _inven.slotNum;
        // 결과 창 UI 출력 갱신
        resultPanel.RefreshResult();
        // 결과 창을 출력하면 게임플레이 UI는 닫기
        UIManager.Instance.Close<GamePlayUI>();
    }

    // 아이템 개수 및 데이터를 추출
    public void FindItemCountAndData(int _tid, out int count, out ItemData data)
    {
        count = 0;
        data = null;

        // 인벤토리 슬롯에서 아이템 개수 합계 및 각 아이템 데이터를 추출
        foreach (InventorySlotData slot in _inven.anySlots)
        {
            if (slot.TID == _tid)
            {
                count += slot.amount;
                if (data == null) data = itemRepo.GetItemDataByID(_tid);
            }
        }
    }

    // 결과 창 패널 닫기
    private void CloseResultPanel()
    {
        // 결과 창 패널이 열린 후에는 결과 창 열기 코루틴을 중단시킨다
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }

        UIManager.Instance.Close<ResultUI>();
    }

    // 출격 준비 패널 오픈 시 퀵슬롯 정보 전달 이벤트를 발송
    public void SendQuickSlotCacheEvent()
    {
        // 캐싱한 퀵슬롯 정보 저장 이벤트를 전송
        GlobalEventBus.QuickSlotLoad?.Invoke(0, slotTID1, slotSprite1, slotCount1);
        GlobalEventBus.QuickSlotLoad?.Invoke(1, slotTID2, slotSprite2, slotCount2);
        GlobalEventBus.QuickSlotLoad?.Invoke(2, slotTID3, slotSprite3, slotCount3);
    }

    // 다이버/기록 패널 오픈 시 심상 기록 해금 상태 전달 이벤트를 발송
    public void SendLinkRecordData()
    {
        _playerSaveData = PlayerSaveDataSO.Instance.currentData;
        GlobalEventBus.RecordDataLoad?.Invoke(linkRateLevel, MemoryLogUnlocked, hasNewMemoryLog);
    }

    // 다이버/기록 패널에서 심상 기록 최초 확인 시 NEW 레드닷 비활성화
    public void NewMemoryRead()
    {
        hasNewMemoryLog = false;
        _playerSaveData = PlayerSaveDataSO.Instance.currentData;
        //_playerSaveData.hasNewMemoryLog = false;
        PlayerSaveDataSO.Instance.SaveGameData();
    }
    private void EnsureSaveLists()
    {
        // {플레이어 저장 데이터가 없으면 중단한다}
        if (_playerSaveData == null)
        {
            Debug.LogWarning("ResultManager: PlayerSaveData가 없습니다.");
            return;
        }

        // {창고 슬롯 리스트가 없으면 새로 만든다. 기존 창고 데이터는 지우지 않는다}
        if (_playerSaveData.storageSlots == null)
            _playerSaveData.storageSlots = new List<SaveSlotData>();

        // {인벤토리 슬롯 리스트가 없으면 새로 만든다}
        if (_playerSaveData.inventorySlots == null)
            _playerSaveData.inventorySlots = new List<SaveSlotData>();

        // {각성 보존 슬롯 리스트가 없으면 새로 만든다}
        if (_playerSaveData.safeSlots == null)
            _playerSaveData.safeSlots = new List<SaveSlotData>();

        // {아티팩트 장착 슬롯 리스트가 없으면 새로 만든다}
        if (_playerSaveData.artifactSlots == null)
            _playerSaveData.artifactSlots = new List<SaveSlotData>();

        // {퀵슬롯 리스트가 없으면 새로 만든다}
        if (_playerSaveData.quickSlots == null)
            _playerSaveData.quickSlots = new List<int>();
    }
}
