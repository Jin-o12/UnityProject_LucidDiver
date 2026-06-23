/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.22_강다영] 결과 씬 제작 이후에 연결하여 탈출 시 결과 화면으로 넘어가게 할 것
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
    [SerializeField] private GameObject[] playerSpawnPoint;
    [SerializeField] private GameObject[] enemySpawnPoint;
    private int entityCount;                                    // 생성된 엔티티의 고유 번호
    CharacterData charData;                                     // 가져올 캐릭터 데이터


    // 탈출 타이머 관련 코드 잠시 보류 //
    //[SerializeField] private GameObject timerCanvas;
    private const float EscapeTimer = 5.0f;
    private WaitForSeconds escapeTimerWs;
    

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);

        entityCount = 0;
        escapeTimerWs = new WaitForSeconds(EscapeTimer);
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
    }

    private void SpawnPlayer()
    {
        // 플레이어 스폰 포인트 중 무작위로 하나 선정
        int spawnNum = Random.Range(0, playerSpawnPoint.Length-1);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if(playerSpawnPoint[spawnNum]==null) return;

        // 플레이어 오브젝트 생성
        Transform spawnPoint = playerSpawnPoint[spawnNum].transform;
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 플레이어 오브젝트 세션 데이터에 등록


        // 플레이어에게 세이브 데이터 넘겨주기
        if(spawnedPlayer.TryGetComponent<PlayerStatus>(out var status))
        {
            status.initialize(charData.hpMax, charData.manaMax, charData.manaRegen);
        }
        if(spawnedPlayer.TryGetComponent<PlayerMovement>(out var movement))
        {
            movement.initialize(charData.moveSpeed);
        }

        GlobalEventBus.OnPlayerSpawned?.Invoke(spawnedPlayer);
    }

    private void StartEscape(int _playerID)
    {
        // 탈출 타이머 시작
        StartCoroutine(StartEscapeTimer());
    }

    private IEnumerator StartEscapeTimer()
    {
        Debug.Log("타이머 시작");
        //GameObject timerCanvas = Instantiate(timerCanvas, );
        //playerStatus.nowState = PlayerStatus.livingState.escape;
        // 해당 시간 동안 대기
        yield return escapeTimerWs;
        // 게임 종료
        Debug.Log("타이머 종료");
        QuitGame();
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료를 시도합니다...");

#if UNITY_EDITOR
        // 유니티 에디터 환경일 경우: 플레이 모드를 해제합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 환경일 경우: 애플리케이션을 종료합니다.
        Application.Quit();
#endif
    }
}
