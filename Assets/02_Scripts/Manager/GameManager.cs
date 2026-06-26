/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.24_강다영] playerPrefab, EnemyPrefab: 캐릭터 및 적 프리팹은 생성 시 결정되도록 바꿀 것
/// </summary>
using System;
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
    private GameObject playerSpawnPool;
    private GameObject enemySpawnPool;
    private readonly string playerSpawnPoolTag = "PlayerSpawnPool"; //플레이어 스폰 풀 태그
    private readonly string enemySpawnPoolTag = "EnemySpawnPool";   //적 스폰 풀 태그
    private List<Transform> playerSpawnPoint = new();
    private List<Transform> enemySpawnPoint = new();
    CharacterData charData;                             // 가져올 캐릭터 데이터
    public PlayerSaveData _playerSaveData;             //인벤토리 기록이 저장된 플레이어 세이브 데이터
    // 플레이 타임 기록에 관한 필드
    private readonly string playScene = "DemoScene";    //인게임 세션 신
    private bool timeTrack = false;                     //플레이 시간 측정 중
    private float startTime;                            //플레이 시작 시점

    private void Awake()
    {
        // 인스턴스 중복 방지
        if(Instance!=null)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;              //신 로드 완료 시점에 실행하는 메소드 연결
        GlobalEventBus.OnEscapeRequest += ResultTime;           //탈출 판정 이벤트에 경과 시간 기록 메소드 연결
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GlobalEventBus.OnEscapeRequest -= ResultTime;
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
        }
        else
        {
            timeTrack = false;     //인게임 세션 신을 벗어나면 시간 기록 중단
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
        int spawnNum = UnityEngine.Random.Range(0, playerSpawnPoint.Count-1);

        // 스폰 장소 오브젝트가 없을 경우 대비
        if (playerSpawnPoint[spawnNum] == null)
        {
            Debug.LogError("Player spawn point not found");
            return;
        }
        // 플레이어 오브젝트 생성
        Transform spawnPoint = playerSpawnPoint[spawnNum].transform;
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

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

    private void ResultTime(bool _extractionResult)
    {
            //시간 기록을 중단하고 기록 고정
        timeTrack = false;
            //결과 계산 시작
        ResultManager.Instance.GameResult(_extractionResult, startTime);
    }
}