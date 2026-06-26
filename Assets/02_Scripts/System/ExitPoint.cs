using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPoint : MonoBehaviour, IInteractable
{
    private bool isEscaping = false;                    //탈출 코루틴 실행 중인지 판정
//  [SerializeField] private GameObject timerCanvas;    //탈출 타이머 캔버스 (P0에서는 사용 안함)
    private float escapeTime = 0.0f;                    //탈출 채널링 시간 (P0에서는 0초)
    public bool Interact(int playerID) 
    {
        Debug.Log($"player {playerID}가 탈출구와 상호작용 함");
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!PlayerStatus.IsPlayerIdle(playerID)) return false;
        // 이미 탈출 판정 중이면 탈출 판정을 중복해서 시작하지 않음
        if (isEscaping) return false;
        // 탈출 타이머 시작
        StartCoroutine(StartEscapeTimer(playerID));
        // 상호작용 성공, 상호작용 리스트에서 삭제 요청
        return false;
    }

    private IEnumerator StartEscapeTimer(int _playerID)  //채널링 후 탈출 성공 판정을 전달
    {
        //탈출 타이머 출력(P0에서는 사용 안함)
        //Debug.Log("타이머 시작");
        //GameObject timerCanvas = Instantiate(timerCanvas, );

        //플레이어 상태를 escape로 변경하고 탈출 판정 시작
        ResultServiceLocator.Instance.HandleEscapeSuccess(_playerID);
        isEscaping = true;
        // 탈출 채널링 시간 동안 대기
        yield return new WaitForSeconds(escapeTime);
        //Debug.Log("타이머 종료");
        // 탈출 판정 전달 이벤트를 발송
        GlobalEventBus.OnEscapeRequest?.Invoke(true);
        isEscaping = false;
    }
}