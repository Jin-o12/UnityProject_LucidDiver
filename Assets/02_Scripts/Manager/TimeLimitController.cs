using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TimeLimitController : MonoBehaviour
{
    public float timeLimit = 600.0f;        //세션별 시간 제한 (초 단위로 입력)
    public float currentTimeLimit;          //현재 남은 제한 시간
    public float timeLimitSpeed = 1.0f;     //플레이 시간 제한 배율 (기본 1.0배)
    public float timeLimitAccel = 1.0f;     //가속 구역으로 인한 플레이 시간 제한 가속 (기본 1.0배)
    private bool timeFlow;                  //제한 시간 진행 스위치

    private void Awake()
    {
        currentTimeLimit = timeLimit;   //남은 제한 시간 초기화
        timeFlow = true;
    }

    private void OnEnable()
    {
        GlobalEventBus.OnTimePenaltyRequested += ApplyTimePenalty;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnTimePenaltyRequested -= ApplyTimePenalty;
    }

    private void FixedUpdate()
    {
        if (!timeFlow) return;

        // 시간 제한 가속 배율에 따라 실시간으로 남은 시간 감소
        currentTimeLimit -= timeLimitSpeed * Time.fixedDeltaTime * timeLimitAccel;

        BroadcastCurrentTime();
        TryFinishByTimeout();
    }

    /// <summary>
    /// 낙인 같은 외부 패널티가 들어오면 남은 시간을 즉시 차감합니다.
    /// 시간 제한 값은 이 컨트롤러만 소유하도록 유지해 다른 시스템이 직접 currentTimeLimit을 건드리지 않게 합니다.
    /// </summary>
    public void ApplyTimePenalty(float penaltySeconds)
    {
        if (!timeFlow || penaltySeconds <= 0.0f)
        {
            return;
        }

        currentTimeLimit = Mathf.Max(0.0f, currentTimeLimit - penaltySeconds);
        BroadcastCurrentTime();
        TryFinishByTimeout();
    }

    private void BroadcastCurrentTime()
    {
        // 타이머 UI에 남은 제한 시간을 전달하는 이벤트
        GlobalEventBus.OnTimerChanged?.Invoke(currentTimeLimit);
    }

    private void TryFinishByTimeout()
    {
        // 남은 시간 제한 값이 0이 되면 탈출 실패로 게임 종료 
        if (currentTimeLimit > 0.0f || !timeFlow)
        {
            return;
        }

        timeFlow = false;
        GlobalEventBus.OnEscapeRequest?.Invoke(false);
    }
}
