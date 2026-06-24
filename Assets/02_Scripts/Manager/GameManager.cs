/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.22_강다영] 결과 씬 제작 이후에 연결하여 탈출 시 결과 화면으로 넘어가게 할 것
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 인벤토리 기록에 관한 필드
    private PlayerInventory inventory;
    // 플레이 타임 기록에 관한 필드
    private float playTime;                             //이번 세션 플레이 시간
    private bool timeTrack = false;                     //플레이 시간 측정 중
    private float startTime;                            //플레이 시작 시점
    private readonly string playScene = "DemoScene";    //플레이 시간을 측정할 신
    // 탈출에 관한 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
    // [SerializeField] private GameObject timerCanvas; //탈출 타이머 캔버스
    private const float EscapeTimer = 0.0f;             //탈출 판정 대기 시간(P0 버전은 즉시 = 0초)
    private WaitForSeconds escapeTimerWs;               //탈출 판정 대기 WFS
    private ResultUI resultPanel;                       //결과 창 UI 캐시

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
        inventory = GameObject.Find("Player").GetComponent<PlayerInventory>();
    }

    private void OnEnable()  //신 로드 시점 이벤트 등록
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == playScene) //인게임 세션 신에서 실행
        {
            startTime = Time.time;  //시작 시점 등록
            timeTrack = true;       //시간 기록 시작
            inventory = GameObject.Find("Player").GetComponent<PlayerInventory>(); // 재바인딩
        }
        else timeTrack = false;     //인게임 세션 신을 벗어나면 기록 중단
    }

    private void OnDestroy()
    {
        GlobalEventBus.OnEscapeRequest -= StartEscape;
        GlobalEventBus.onPlayerDead -= GameOver;
    }

    private void SpawnEntities()
    {
        
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

    public bool IsPlayerIdle(int _playerID)  //플레이어 상태가 idle(대기)인지 확인하는 헬퍼 메소드
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
        // 이번 세션에서의 플레이 시간을 계산
        if (timeTrack)
        {
            playTime = Time.time - startTime;
            timeTrack = false;
        }
        Debug.Log("결과 창 패널을 출력합니다...");
        // UIManager에서 Canvas-ResultPanel을 받아와 실행
        resultPanel = UIManager.Instance.Open<ResultUI>();
        if (resultPanel == null) return;
        // 인게임 세션에서 측정된 데이터를 resultPanel에 전달해 UI 갱신
        resultPanel.extractionResult = extractionResult;
        resultPanel.potionCount = FindItemCount(301);
        resultPanel.manaStoneCount = FindItemCount(302);
        resultPanel.memoryFragmentCount = FindItemCount(401);
        resultPanel.playTime = playTime;
        resultPanel.RefreshResult();
    }
    public int FindItemCount(int _tid)
    {
        foreach (InventorySlotData slot in inventory.slots)
        {
            if (slot.TID == _tid)
            {
                return slot.amount;
            }
        }
        return 0;
    }
}