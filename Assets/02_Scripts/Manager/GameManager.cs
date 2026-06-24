/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.24_강다영] playerPrefab, EnemyPrefab: 캐릭터 및 적 프리팹은 생성 시 결정되도록 바꿀 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("엔티티 생성")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject EnemyPrefab;
    [SerializeField] private GameObject playerSpawnPool;
    [SerializeField] private GameObject enemySpawnPool;
    private List<Transform> playerSpawnPoint = new();
    private List<Transform> enemySpawnPoint = new();
    CharacterData charData;                                     // 가져올 캐릭터 데이터

    // 탈출에 관한 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
    private const float EscapeTimer = 0.0f;             //탈출 판정 대기 시간(P0 버전은 즉시 = 0초)
    private WaitForSeconds escapeTimerWs;               //탈출 판정 대기 WFS

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);

        escapeTimerWs = new WaitForSeconds(EscapeTimer);
        GlobalEventBus.OnEscapeRequest += StartEscape;  //탈출 요청 이벤트에 탈출 시작 메소드 연결
        GlobalEventBus.onPlayerDead += GameOver;        //플레이어 사망 이벤트에 게임오버 메소드 연결

        // 스폰 포인트 등록
        foreach (Transform point in playerSpawnPool.transform)
        {
            playerSpawnPoint.Add(point);
        }

        foreach (Transform point in enemySpawnPool.transform)
        {
            enemySpawnPoint.Add(point);
        }
    }

    private void Start()
    {
        // 캐릭터 데이터 가져오기
        DataManager dataManager = DataManager.Instance;
        charData = dataManager.GetCharacterData(dataManager.playerData.SelectCharID);   

        // 플레이어 1회 생성
        SpawnPlayer();
    }

    private void OnEnable()
    {
        GlobalEventBus.OnEscapeRequest += StartEscape;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnEscapeRequest -= StartEscape;
        GlobalEventBus.onPlayerDead -= GameOver;
    }

    private void SpawnPlayer()
    {
        // 플레이어 스폰 포인트 중 무작위로 하나 선정
        int spawnNum = Random.Range(0, playerSpawnPoint.Count-1);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if(playerSpawnPoint[spawnNum]==null) return;

        // 플레이어 오브젝트 생성
        Transform spawnPoint = playerSpawnPoint[spawnNum].transform;
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 플레이어 오브젝트 세션 데이터에 등록
        GlobalRuntimeData.CountingEntityData(spawnedPlayer);

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

    private void StartEscape(int _playerID)
    {
        Debug.Log($"GameManager.StartEscape called for player {_playerID}");
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!IsPlayerIdle(_playerID)) return;
        // 이미 탈출 판정 중이면 탈출 판정을 중복해서 시작하지 않음
        if (isEscaping) return;
        // 탈출 타이머 시작
        StartCoroutine(StartEscapeTimer(_playerID));
    }

    private IEnumerator StartEscapeTimer(int _playerID)
    {
        Debug.Log("타이머 시작");
        //GameObject timerCanvas = Instantiate(timerCanvas, );
        //플레이어 상태를 escape로 변경하고 탈출 판정 시작
        ResultServiceLocator.Instance.HandleEscapeSuccess(_playerID);
        isEscaping = true;
        // 해당 시간 동안 대기
        yield return escapeTimerWs;
        // 게임 종료
        Debug.Log("타이머 종료");
        extractionResult = true;
        QuitGame();
    }

    private void GameOver(int _playerID)  //게임 오버 시 결과 정산 메소드
    {
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!IsPlayerIdle(_playerID)) return;
        ResultServiceLocator.Instance.HandleEscapeFail(_playerID);
        extractionResult = false;
        QuitGame();
    }

    private bool IsPlayerIdle(int _playerID)  //플레이어 상태가 idle(대기)인지 확인하는 헬퍼 메소드
    {
        var svc = ResultServiceLocator.Instance;
        if (svc == null)
        {
            Debug.LogWarning("StartEscape: ResultServiceLocator.Instance is null");
            return false;
        }
        PlayerStatus ps = svc.GetPlayerStatus(_playerID);
        if (ps == null)
        {
            Debug.LogWarning($"PlayerStatus를 찾을 수 없습니다. playerID: {_playerID}");
            return false;
        }
        return ps.nowState == PlayerStatus.livingState.idle;
    }

    public void QuitGame()
    {
        // extractionResult를 resultPanel에 전달해 UI 갱신
        GlobalEventBus.OnShowGameResult?.Invoke(extractionResult);
    }
}