﻿/// <summary>
/// 인게임 세션 종료 시 데이터 변동을 관리하는 클래스
/// (탈출 성공 여부, 플레이 타임, 인벤토리 및 퀵슬롯, 동조율 단계)
/// </summary>
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
    // 동조율 저장 필드
    public int linkRateLevel = 0;                       //동조율 상승 후 다이버와의 동조율 단계
    public int linkRateGain = 1;                        //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    private bool linkRateUp = false;                    //동조율 단계 상승 여부 전달
    private bool MemoryLogUnlocked = false;             //세션 탈출 시 개인 심상 기록 해금 여부 저장
    private bool hasNewMemoryLog = true;                //개인 심상 기록 확인 여부 저장
    // 인벤토리 기록에 관한 필드
    private PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    private PlayerInventory _inven;                     //현재 인게임 세션의 플레이어 인벤토리 데이터
    private ItemData potionData;                        //변질된 붕대 아이템 데이터
    private ItemData manaStoneData;                     //기묘한 사탕 아이템 데이터
    private ItemData memoryFragmentData;                //기억 파편 아이템 데이터
    private int potionCount;                            //변질된 붕대 개수
    private int mpStoneCount;                           //기묘한 사탕 개수
    private int memoryFragmentCount;                    //기억 파편 개수
    // 퀵슬롯 저장 필드
    public int slotTID1;                                //1번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite1;                          //1번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount1;                              //1번 슬롯 아이템의 개수 데이터를 받아옴
    public int slotTID2;                                //2번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite2;                          //2번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount2;                              //2번 슬롯 아이템의 개수 데이터를 받아옴

    // 저장 데이터 인터페이스
    private IItemDataRepository itemRepo;               // 아이템 데이터 접근 인터페이스

    public int slotTID3;                                //3번 슬롯 아이템의 ID값 데이터를 받아옴
    public Sprite slotSprite3;                          //3번 슬롯 아이템의 아이콘 스프라이트 데이터를 받아옴
    public int slotCount3;                              //3번 슬롯 아이템의 개수 데이터를 받아옴
    
    private void Awake()
    {
        // 싱글톤 인스턴스 중복 방지 설정
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
        // ResultServiceLocator에 자신을 등록
        ResultServiceLocator.Instance = this;
        // 인터페이스 구현부 연결
        itemRepo = new SOItemRepository();

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
        
        //로비로 돌아가기 버튼에 동조율 데이터 갱신 연결
        GlobalEventBus.OnSetRecordData += RenewLinkRateData;
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
        //if (ResultServiceLocator.Instance == (IResultService)this) ResultServiceLocator.Instance = null;
        //if (Instance == this) Instance = null;
        GlobalEventBus.OnSetRecordData -= RenewLinkRateData;
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
        Debug.Log($"ResultManager.Register: playerID={playerID} registered (obj={ps.gameObject.name})");
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

    // 탈출 성공 처리
    public void HandleEscapeSuccess(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.escape);

    // 탈출 실패 처리
    public void HandleEscapeFail(int playerID) => SetPlayerState(playerID, PlayerStatus.livingState.gameover);

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
        DataManager.Instance.LoadGame();
        // 플레이어 세이브 데이터를 가져옴
        _playerSaveData = DataManager.Instance.playerData;
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
        // 아이템 ID에 따라 개수 및 데이터 값 추출
        FindItemCountAndData(301, out potionCount, out potionData);
        FindItemCountAndData(302, out mpStoneCount, out manaStoneData);
        FindItemCountAndData(401, out memoryFragmentCount, out memoryFragmentData);
        // 결과 창 패널 출력 메소드
        OpenResultPanel();
        // 기억 파편을 사용해 동조율 상승 → 심상 기록 해금 처리를 실행
        LinkRateUp(_extractionResult);
        // 심상 기록 읽기 상태 저장
        // _playerSaveData.hasNewMemoryLog = hasNewMemoryLog;
        // 탈출 실패 시 소비 기물 아이템을 인벤토리에서 제거
        if (!_extractionResult)
        {
            RemoveFromInventory(301);
            RemoveFromInventory(302);
        }
        // 모든 처리 완료 후 후 DataManager에서 playerData를 저장
        DataManager.Instance.SaveGame();
    }

    private void InventorySync()
    {
        // PlayerSaveData의 인벤토리 슬롯을 비우고 현재 플레이어 인벤토리 데이터로 채우기
        _playerSaveData.inventorySlots.Clear();
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

        // PlayerSaveData의 퀵슬롯을 비우고 현재 플레이어 퀵슬롯 데이터로 채우기
        _playerSaveData.quickSlots.Clear();
        _playerSaveData.quickSlots.Add(_inven.quickSlots[0].TID);
        _playerSaveData.quickSlots.Add(_inven.quickSlots[1].TID);
        _playerSaveData.quickSlots.Add(_inven.quickSlots[2].TID);

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
        // 기억 파편 획득 AND 탈출 성공이면 '동조율 단계 상승=true' 전달
        linkRateUp = memoryFragmentCount > 0 && _extractionResult;
        resultPanel.linkRateUp = linkRateUp;
        // 기억 파편을 사용해 동조율 단계가 상승했거나 이미 해금 상태(기억 동조율 단계 > 0)라면 '해금됨=true' 전달
        MemoryLogUnlocked = linkRateUp || linkRateLevel > 0;
        resultPanel.memoryLogUnlocked = MemoryLogUnlocked;
        // 기억 파편을 인벤토리에서 제거 (성공/실패 양쪽 모두 제거 처리는 실행함)
        RemoveFromInventory(401);
        // 결과 창 UI 출력 갱신
        resultPanel.RefreshResult();
    }

    private void RemoveFromInventory(int _tid)  //아이템 ID별로 인벤토리에서 제거
    {
        //foreach (SaveSlotData slot in _playerSaveData.inventorySlots)
        //{
        //    if (slot.TID == _tid)
        //    {
        //        slot.amount = 0;
        //    }
        //}
        // {저장 데이터나 인벤토리 슬롯 리스트가 없으면 중단한다}
        if (_playerSaveData == null || _playerSaveData.inventorySlots == null)
        {
            return;
        }

        // {지정한 TID의 아이템을 인벤토리 저장 슬롯에서 제거한다}
        _playerSaveData.inventorySlots.RemoveAll(slot => slot != null && slot.TID == _tid);
    }

    // 결과 창 패널 출력 메소드
    public void OpenResultPanel()
    {
        // UIManager에서 Canvas-ResultPanel을 받아와 UI 오픈
        resultPanel = UIManager.Instance.Open<ResultUI>();
        if (resultPanel == null) return;
        // 인게임 세션에서 저장된 데이터를 resultPanel에 전달해 UI 갱신
        _playerSaveData = DataManager.Instance.playerData;
        resultPanel.extractionResult = extractionResult;
        resultPanel.playTime = playTime;
        resultPanel.potionCount = potionCount;
        resultPanel.potionData = potionData;
        resultPanel.manaStoneCount = mpStoneCount;
        resultPanel.manaStoneData = manaStoneData;
        resultPanel.memoryFragmentCount = memoryFragmentCount;
        resultPanel.memoryFragmentData = memoryFragmentData;
        // 동조율 단계 데이터를 결과 창에 전달 (상승 전 / 상승 후)
        int prevLinkRateLevel = linkRateLevel;
        resultPanel.prevLinkRateLevel = prevLinkRateLevel;
        int nextLinkRateLevel = linkRateLevel + linkRateGain;
        resultPanel.linkRateLevel = nextLinkRateLevel;
        resultPanel.linkRateGain = linkRateGain;
        // 동조율 단계 데이터 갱신
        linkRateLevel = extractionResult ? nextLinkRateLevel : prevLinkRateLevel;
        RenewLinkRateData(linkRateLevel, linkRateLevel > 0);
        // 결과 창 UI 출력 갱신
        resultPanel.RefreshResult();
    }

    // 아이템 개수 및 데이터를 추출
    public void FindItemCountAndData(int _tid, out int count, out ItemData data)
    {
        count = 0;
        data = null;

        // 인벤토리 슬롯에서 아이템 개수 합계 및 각 아이템 데이터를 추출
        foreach (InventorySlotData slot in _inven.slots)
        {
            if (slot.TID == _tid)
            {
                count += slot.amount;
                if (data == null) data = itemRepo.GetItemData(_tid);
            }
        }
    }

    // 로비로 이동 시 동조율 데이터 갱신
    public void RenewLinkRateData(int a, bool b)
    {
        linkRateLevel = a;      //동조율 단계 갱신
        MemoryLogUnlocked = b;  //심상 기록 해금 상태 갱신
    }

    // 결과 창 패널 닫기
    private void CloseResultPanel()
    {
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
        _playerSaveData = DataManager.Instance.playerData;
        GlobalEventBus.RecordDataLoad?.Invoke(linkRateLevel, MemoryLogUnlocked, hasNewMemoryLog);
    }

    // 다이버/기록 패널에서 심상 기록 최초 확인 시 NEW 레드닷 비활성화
    public void NewMemoryRead()
    {
        hasNewMemoryLog = false;
        _playerSaveData = DataManager.Instance.playerData;
        //_playerSaveData.hasNewMemoryLog = false;
        DataManager.Instance.SaveGame();
    }
}
