/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.24_강다영] playerPrefab, EnemyPrefab: 캐릭터 및 적 프리팹은 생성 시 결정되도록 바꿀 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("엔티티 생성")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject EnemyPrefab;
    [SerializeField] private GameObject playerSpawnPool;
    [SerializeField] private GameObject enemySpawnPool;
    private readonly string playerSpawnPoolTag = "PlayerSpawnPool"; //플레이어 스폰 풀 태그
    private readonly string enemySpawnPoolTag = "EnemySpawnPool";   //적 스폰 풀 태그
    private List<Transform> playerSpawnPoint = new();
    private List<Transform> enemySpawnPoint = new();
    CharacterData charData;                                         // 가져올 캐릭터 데이터

    // 인벤토리 기록에 관한 필드
    private PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    private ItemData potionData;                        //변질된 붕대 아이템 데이터
    private ItemData manaStoneData;                     //기묘한 사탕 아이템 데이터
    private ItemData memoryFragmentData;                //기억 파편 아이템 데이터
    // 플레이 타임 기록에 관한 필드
    private float playTime;                             //이번 세션 플레이 시간
    private bool timeTrack = false;                     //플레이 시간 측정 중
    private float startTime;                            //플레이 시작 시점
    private readonly string playScene = "DemoScene";    //플레이 시간을 측정할 신
    // 탈출에 관한 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    private ResultUI resultPanel;                       //결과 창 UI 캐시
    // 동조율 관련 코드
    public int linkRateLevel = 0;                       //동조율 상승 후 다이버와의 동조율 단계
    public int linkRateGain = 1;                        //세션 탈출 성공 시 가산되는 동조율 단계 증가치
    private bool MemoryLogUnlocked;                     //세션 탈출 시 개인 심상 기록 해금 여부 저장

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;              //신 로드 완료 시점에 실행하는 메소드 연결
        GlobalEventBus.OnEscapeRequest += QuitGame;             //탈출 판정 이벤트에 탈출 처리 메소드 연결
        GlobalEventBus.OnReturnToLobby += CloseResultPanel;     //로비로 돌아가기 버튼에 결과 창 닫기 연결
        GlobalEventBus.OnSetRecordData += RenewLinkRateData;    //로비로 돌아가기 버튼에 동조율 데이터 갱신 연결
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GlobalEventBus.OnEscapeRequest -= QuitGame;
        GlobalEventBus.OnReturnToLobby -= CloseResultPanel;
        GlobalEventBus.OnSetRecordData -= RenewLinkRateData;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == playScene) //인게임 세션 신에서 실행
        {
            RefreshSpawnPoints();   //스폰 포인트 최신화

            // ResultManager의 플레이어 캐시 새로고침 (씬 전환 후)
            if (ResultManager.Instance != null) ResultManager.Instance.RefreshPlayerCache();

            startTime = Time.time;  //시작 시점 등록
            timeTrack = true;       //시간 기록 시작

            // 캐릭터 데이터 가져오기
            DataManager dataManager = DataManager.Instance;
            _playerSaveData = dataManager.playerData;
            charData = dataManager.GetCharacterData(dataManager.playerData.SelectCharID);

            // 플레이어 1회 생성
            SpawnPlayer();

            // ResultManager에서 ResultServiceLocator 연결이 끊겨 있으면 다시 등록
            StartCoroutine(EnsureResultServiceLocator());
        }
        else
        {
            timeTrack = false;     //인게임 세션 신을 벗어나면 시간 기록 중단
        }
    }

    private IEnumerator EnsureResultServiceLocator()
    {
        yield return null;                                          //1프레임 대기하여 ResultManager 시작
        //이미 연결되어 있다면 추가 처리 없음
        if (ResultServiceLocator.Instance != null)
        {
            Debug.Log("1. Already Connected");
            yield break;
        }
        //ResultManager가 있다면 Locator 연결
        if (ResultManager.Instance != null)
        {
            ResultServiceLocator.Instance = ResultManager.Instance;
            Debug.Log("2. ResultManager Connected");
            yield break;
        }
        //ResultManager가 없다면 폴백: 씬에서 ResultManager 컴포넌트 직접 검색
        var foundManager = FindObjectOfType<ResultManager>();
        if (foundManager != null)
        {
            ResultServiceLocator.Instance = foundManager;
            Debug.Log("3. ResultManager Pooled back");
            yield break;
        }
    }

    private void RefreshSpawnPoints()
    {
        playerSpawnPoint.Clear();
        enemySpawnPoint.Clear();

        //스폰 풀 불러오기 (풀 오브젝트에 전용 태그를 붙여 인식)
        playerSpawnPool = GameObject.FindGameObjectWithTag(playerSpawnPoolTag);
        enemySpawnPool = GameObject.FindGameObjectWithTag(enemySpawnPoolTag);

        // 플레이어와 적 스폰지점 불러오기
        foreach (Transform point in playerSpawnPool.transform)
        {
            playerSpawnPoint.Add(point);
        }
        foreach (Transform point in enemySpawnPool.transform)
        {
            enemySpawnPoint.Add(point);
        }
    }

    private void SpawnPlayer()
    {
        // 이미 플레이어가 있으면 스폰을 스킵
        if (FindObjectOfType<PlayerStatus>() != null) return;

        // 플레이어 스폰 포인트 중 무작위로 하나 선정
        int spawnNum = Random.Range(0, playerSpawnPoint.Count-1);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if (playerSpawnPoint[spawnNum] == null)
        {
            Debug.LogError("Player spawn point not found");
            return;
        }
        // 플레이어 오브젝트 생성
        Transform spawnPoint = playerSpawnPoint[spawnNum].transform;
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // 생성한 플레이어를 ResultServiceLocator에 등록
        if (spawnedPlayer != null)
        {
            var entityIdComp = spawnedPlayer.GetComponent<EntityIdentity>();
            var statusComp = spawnedPlayer.GetComponent<PlayerStatus>();

            if (entityIdComp != null && statusComp != null)
            {
                // ResultServiceLocator가 이미 세팅되어 있다면 바로 등록
                if (ResultServiceLocator.Instance != null)
                {
                    ResultServiceLocator.Instance.Register(entityIdComp.entityID, statusComp);
                    Debug.Log($"SpawnPlayer: Registered playerID={entityIdComp.entityID} to ResultServiceLocator.");
                }
                else
                {
                    // ResultManager 싱글턴이 이미 존재하면 로케이터에 연결 후 등록
                    if (ResultManager.Instance != null)
                    {
                        ResultServiceLocator.Instance = ResultManager.Instance;
                        ResultServiceLocator.Instance.Register(entityIdComp.entityID, statusComp);
                        Debug.Log($"SpawnPlayer: ResultServiceLocator was null - set from ResultManager and registered playerID={entityIdComp.entityID}.");
                    }
                    else
                    {
                        Debug.LogWarning("SpawnPlayer: ResultServiceLocator 및 ResultManager가 모두 준비되지 않았습니다. Player 등록이 누락될 수 있습니다.");
                    }
                }
            }
        }

        // 플레이어 오브젝트 세션 데이터에 등록 (이미 있는 경우에는 스킵)
        if (!GlobalRuntimeData.ActivePlayers.Contains(spawnedPlayer))
        {
            GlobalRuntimeData.CountingEntityData(spawnedPlayer);
        }

        // 플레이어에게 세이브 데이터 넘겨주기
        if(spawnedPlayer.TryGetComponent<PlayerStatus>(out var status))
        {
            status.initialize(charData.hpMax, charData.manaMax, charData.manaRegen);
        }
        if(spawnedPlayer.TryGetComponent<PlayerMovement>(out var movement))
        {
            movement.initialize(charData.moveSpeed);
        }

        // 플레이어 스폰 여부 이벤트
        GlobalEventBus.OnPlayerSpawned?.Invoke(spawnedPlayer);
    }

    public void QuitGame(bool _extractionResult)
    {
        extractionResult = _extractionResult;
        // 이번 세션에서의 플레이 시간을 계산
        if (timeTrack)
        {
            playTime = Time.time - startTime;
            timeTrack = false;
        }
        // 인벤토리 데이터를 불러와 동기화 갱신
        InventorySync();
        // 갱신 후 DataManager에서 playerData를 저장
        DataManager.Instance.SaveGame();
        // 결과 창 패널 출력 메소드
        OpenResultPanel();
        // 탈출 실패 시 각 아이템을 인벤토리에서 제거
        if (!_extractionResult)
        {
            RemoveFromInventory(301);
            RemoveFromInventory(302);
            RemoveFromInventory(401);
            resultPanel.memoryLogUnlocked = false;
        }
        // 탈출 성공 시에는 기억 파편을 사용해 동조율 상승 → 심상 기록 해금 처리를 실행
        else
        {
            LinkRateUp();
        }
        // 모든 처리 완료 후 후 DataManager에서 playerData를 저장
        DataManager.Instance.SaveGame();
    }

    private void LinkRateUp()
    {
        // 사용할 기억 파편이 있는지 체크
        bool memoryLogUnlocked = resultPanel.memoryFragmentCount > 0;
        resultPanel.memoryLogUnlocked = memoryLogUnlocked;

        // 기억 파편을 인벤토리에서 제거
        RemoveFromInventory(401);
        // 결과 창 UI 출력 갱신
        resultPanel.RefreshResult();
    }

    private void OpenResultPanel()
    {
        Debug.Log("결과 창 패널을 출력합니다...");
        // UIManager에서 Canvas-ResultPanel을 받아와 UI 오픈
        resultPanel = UIManager.Instance.Open<ResultUI>();
        if (resultPanel == null) return;
        // 인게임 세션에서 저장된 데이터를 resultPanel에 전달해 UI 갱신
        _playerSaveData = DataManager.Instance.playerData;
        resultPanel.extractionResult = extractionResult;
        resultPanel.playTime = playTime;
        // 아이템 ID에 따라 개수 및 데이터 값 추출
        FindItem(301, out resultPanel.potionCount, out potionData);
        FindItem(302, out resultPanel.manaStoneCount, out manaStoneData);
        FindItem(401, out resultPanel.memoryFragmentCount, out memoryFragmentData);
        resultPanel.potionData = potionData;
        resultPanel.manaStoneData = manaStoneData;
        resultPanel.memoryFragmentData = memoryFragmentData;
        // 동조율 단계 데이터 전달
        resultPanel.prevLinkRateLevel = linkRateLevel;
        resultPanel.linkRateLevel = linkRateLevel + linkRateGain;
        resultPanel.linkRateGain = linkRateGain;
        // 결과 창 UI 출력 갱신
        resultPanel.RefreshResult();
    }

    // 결과 창 패널 닫기
    private void CloseResultPanel()
    {
        UIManager.Instance.Close<ResultUI>();
    }

    // 로비로 이동 시 동조율 데이터 갱신
    public void RenewLinkRateData(int a, bool b)
    {
        linkRateLevel = a;      //동조율 단계 갱신
        MemoryLogUnlocked = b;  //심상 기록 해금 상태 갱신
    }

    private void RemoveFromInventory(int _tid)  //아이템 ID별로 인벤토리에서 제거
    {
        foreach (SaveSlotData slot in _playerSaveData.inventorySlots)
        {
            // 해당 아이템이 이미 창고에 존재한다면 보유 개수를 창고에 더함
            if (slot.TID == _tid)
            {
                slot.amount = 0;
            }
        }
    }

    public void FindItem(int _tid, out int count, out ItemData data)
    {
        foreach (SaveSlotData slot in _playerSaveData.inventorySlots)
        {
            if (slot.TID == _tid)
            {
                count = slot.amount;
                data = DataManager.Instance.GetItemData(_tid);
                return;
            }
        }
        count = 0;
        data = null;
        return;
    }

    private void InventorySync()
    {
        // 플레이어 오브젝트에서 PlayerInventory 컴포넌트를 찾아 데이터 동기화
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("PlayerInventory를 찾을 수 없습니다.");
            return;
        }

        // PlayerSaveData의 인벤토리 슬롯을 비우고 현재 플레이어 인벤토리 데이터로 채우기
        _playerSaveData.inventorySlots.Clear();
        foreach (var slot in playerInventory.slots)
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
    }
}