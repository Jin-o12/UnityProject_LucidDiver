using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPoint : MonoBehaviour, IInteractable
{
    // 탈출에 관한 필드
    private bool extractionResult;                      //탈출 성공 여부 판정
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
    private const float EscapeTimer = 0.0f;             //탈출 판정 대기 시간(P0 버전은 즉시 = 0초)
    private WaitForSeconds escapeTimerWs;               //탈출 판정 대기 WFS
    private GameObject nowPlayer;                       // 현재 탈출을 시도하는 플레이어

    public bool Interact(int _playerID) 
    {
        Debug.Log("탈출구와 상호작용 함");

        if (!IsPlayerIdle(_playerID)) return false;
        // 이미 탈출 판정 중이면 탈출 판정을 중복해서 시작하지 않음
        if (isEscaping) return false;

        // 플레이어 오브젝트 등록
        GlobalRuntimeData.CountingEntityData(_playerID);
        // 탈출 타이머 시작
        StartCoroutine(StartEscapeTimer(_playerID));
        return false;
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
