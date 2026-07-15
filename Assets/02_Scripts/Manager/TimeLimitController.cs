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
    private float artifactTimeLimitSpeedMultiplier = 1.0f; // 아티팩트로 적용되는 시간 감소 속도 배율
    private bool timeFlow;                  //제한 시간 진행 스위치

    //제한 시간 루프 사운드 트리거
    private bool lowTimeSFX_1 = false;
    private bool lowTimeSFX_2 = false;

    //제한 시간 루프 사운드 재생 시점
    private float lowTimeTirggerRatio_1 = 0.5f;  //1차 사운드: 50% 이상 경과
    private float lowTimeTirggerRatio_2 = 0.2f;  //2차 사운드: 80% 이상 경과

    //제한 시간 루프 사운드 ID
    private int lowTimeAudioID_1 = 10301;
    private int lowTimeAudioID_2 = 10302;

    private void Awake()
    {
        currentTimeLimit = timeLimit;   //남은 제한 시간 초기화
        timeFlow = true;
    }

    private void OnEnable()
    {
        GlobalEventBus.OnTimePenaltyRequested += ApplyTimePenalty;
        GlobalEventBus.OnEscapeRequest += StopTimeFlow;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnTimePenaltyRequested -= ApplyTimePenalty;
        GlobalEventBus.OnEscapeRequest -= StopTimeFlow;
    }

    private void FixedUpdate()
    {
        if (!timeFlow) return;

        // 시간 제한 가속 배율에 따라 실시간으로 남은 시간 감소
        currentTimeLimit -= timeLimitSpeed * artifactTimeLimitSpeedMultiplier * Time.fixedDeltaTime * timeLimitAccel;

        BroadcastCurrentTime();
        TryFinishByTimeout();
        TryTimeLoopSFX();
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

    /// <summary>
    /// 사망 또는 탈출이 확정되면 제한 시간 진행과 이후의 모든 시간 패널티를 중단합니다.
    /// 성공 여부와 관계없이 세션이 끝난 시점부터 남은 시간은 고정되어야 합니다.
    /// </summary>
    private void StopTimeFlow(bool extractionResult)
    {
        timeFlow = false;
    }

    /// <summary>
    /// 아티팩트 장착 효과로 제한 시간 감소 속도 배율을 갱신합니다.
    /// 예: 0.6이면 기존 시간 감소 속도의 60%만 적용되어 시간 제한이 더 천천히 줄어듭니다.
    /// </summary>
    public void ApplyArtifactTimeLimitSpeedMultiplier(float multiplier)
    {
        artifactTimeLimitSpeedMultiplier = Mathf.Max(0.0f, multiplier);
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

        // 루프 2번 사운드 종료
        GlobalEventBus.OnStop2DSoundRequested(lowTimeAudioID_2);

        timeFlow = false;
        GlobalEventBus.OnTimeOver?.Invoke();
    }
    private void TryTimeLoopSFX()
    {
        // 50% 경과 시 루프 1번 사운드를 재생 시작
        if (currentTimeLimit < lowTimeTirggerRatio_1 * timeLimit && !lowTimeSFX_1)
        {
            lowTimeSFX_1 = true;
            GlobalEventBus.OnPlay2DSoundRequested(lowTimeAudioID_1);
        }

        // 80% 경과 시 루프 1번 사운드를 종료하고 루프 2번 사운드를 재생 시작
        if (currentTimeLimit < lowTimeTirggerRatio_2 * timeLimit && !lowTimeSFX_2)
        {
            lowTimeSFX_2 = true;
            GlobalEventBus.OnStop2DSoundRequested(lowTimeAudioID_1);
            GlobalEventBus.OnPlay2DSoundRequested(lowTimeAudioID_2);
        }
    }
}
