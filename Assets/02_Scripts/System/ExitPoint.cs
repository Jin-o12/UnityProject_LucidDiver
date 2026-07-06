using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPoint : MonoBehaviour, IInteractable
{
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
    [SerializeField] private GameObject timerCanvas;    //탈출 타이머 캔버스 (P0에서는 사용 안함)
    [SerializeField] private float escapeTime = 3.0f;   //탈출 채널링 시간 (즉시 탈출하려면 0초)
    private Coroutine escapeCoroutine;                  //탈출 채털링 코루틴
    public event Action<float> timerOn;                 //타이머 출력 이벤트 

    private void Awake()
    {
        ResolveTimerCanvasReference();

        // 초기 상태에서는 타이머 캔버스를 꺼둠
        SetTimerCanvasActive(false);
    }

    private void OnEnable()
    {
        // 탈출 중단 이벤트를 구독
        GlobalEventBus.OnEscapeFailure += EscapeFailure;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnEscapeFailure -= EscapeFailure;
    }

    public bool Interact(int playerID) 
    {
        Debug.Log($"player {playerID}가 탈출구와 상호작용 함");
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!PlayerStatus.IsPlayerIdle(playerID)) return false;
        // 이미 탈출 판정 중이면 탈출 판정을 중복해서 시작하지 않음
        if (isEscaping) return false;
        // 탈출 타이머 시작
        escapeCoroutine = StartCoroutine(StartEscapeTimer(playerID));
        // 상호작용 성공, 상호작용 리스트에서 삭제 요청
        return false;
    }

    private IEnumerator StartEscapeTimer(int _playerID)  //채널링 후 탈출 성공 판정을 전달
    {
        //탈출 타이머 출력(P0에서는 사용 안함)
        Debug.Log("타이머 시작");
        SetTimerCanvasActive(true);

        if (timerCanvas != null)
        {
            timerOn?.Invoke(escapeTime);
        }

        //플레이어 상태를 escape로 변경하고 탈출 판정 시작
        ResultServiceLocator.Instance.HandleEscapeStart(_playerID);
        isEscaping = true;
        // 탈출 채널링 시간 동안 대기
        yield return new WaitForSeconds(escapeTime);
        
        // 채널링 종료 후 탈출 성공 판정 이벤트를 발송
        Debug.Log("타이머 종료");
        SetTimerCanvasActive(false);
        GlobalEventBus.OnEscapeRequest?.Invoke(true);
    }

    private void EscapeFailure(int _playerID)  //플레이어의 탈출 채널링 코루틴을 중단하는 판정을 전달
    {
        //플레이어 상태를 idle로 변경하고 탈출 판정 중단
        ResultServiceLocator.Instance.HandleEscapeIdle(_playerID);
        isEscaping = false;

        //탈출 타이머 출력 종료
        SetTimerCanvasActive(isEscaping);

        //채널링 코루틴을 중단
        if (escapeCoroutine != null)
        {
            StopCoroutine(escapeCoroutine);
            escapeCoroutine = null;
        }
    }

    private void ResolveTimerCanvasReference()
    {
        // 인스펙터 연결이 빠졌다면 자식 오브젝트에서 타이머 UI를 자동으로 탐색
        if (timerCanvas != null) return;

        EscapeTimer escapeTimer = GetComponentInChildren<EscapeTimer>(true);
        if (escapeTimer != null)
        {
            timerCanvas = escapeTimer.gameObject;
        }

        if (timerCanvas == null)
        {
            Debug.LogWarning($"{name} ExitPoint의 timerCanvas가 비어 있습니다. 타이머 UI 없이 탈출 기능만 동작합니다.", this);
        }
    }

    private void SetTimerCanvasActive(bool isActive)
    {
        // P0처럼 타이머 UI를 사용하지 않는 씬에서는 null 예외 없이 안전하게 넘어감
        if (timerCanvas == null) return;

        timerCanvas.SetActive(isActive);
    }
}
