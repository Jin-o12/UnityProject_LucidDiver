/// <summary>
/// 인게임 전반의 시스템을 관리하는 인스턴스 클래스
/// [26.06.22_강다영] 결과 씬 제작 이후에 연결하여 탈출 시 결과 화면으로 넘어가게 할 것
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 참조 컴포넌트
    private PlayerStatus playerStatus;

    // 탈출에 관한 필드
    [SerializeField] private GameObject timerCanvas;
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

        escapeTimerWs = new WaitForSeconds(EscapeTimer);
    }

    private void OnEnable()
    {
        GlobalEventBus.OnEscapeRequest += StartEscape;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnEscapeRequest -= StartEscape;
    }

    private void SpawnEntities()
    {
        
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
