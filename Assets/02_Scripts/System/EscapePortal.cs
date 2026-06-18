using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class EscapePortal : MonoBehaviour, IInteractable
{
    public float escapeTime = 0f; //탈출 시까지 대기하는 채널링 시간 (즉시 탈출하려면 0f로 입력)
    private bool isEscaping = false;  //탈출 코루틴 실행 중 판정
    public bool Interact(int playerID)  //상호작용 메소드 실행
    {
        if (isEscaping) return false;  //탈출 코루틴 실행 중이면 상호작용을 진행하지 않음
        else {
            ChangePlayerStatus(playerID, PlayerStatus.livingState.escape);  //상호작용한 플레이어의 livingState를 escape로 바꿈
            isEscaping = true;
            StartCoroutine(Escape(playerID));  //.."채널링 시간만큼 대기 후 탈출 성공 판정" 코루틴을 시작
            return true; }
    }
    public IEnumerator Escape(int playerID)  //채널링 시간만큼 대기 후 탈출 성공 판정
    {
        yield return new WaitForSeconds(escapeTime);  //escapeTime만큼 대기
                                                      //세션 상태를 RESULT로 변경
        ChangePlayerStatus(playerID, PlayerStatus.livingState.idle);  //상호작용한 플레이어의 livingState를 idle로 변경
        #region 탈출 완료 처리
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;  //UnityEditor 임시 처리: 에디터 종료
#else
        isEscaping = false;  //탈출 코루틴 실행이 끝남
        Application.Quit();
#endif
        #endregion
    }
    private static void ChangePlayerStatus(int playerID, PlayerStatus.livingState state)  //상호작용한 플레이어의 livingState를 변경하는 메소드
    {
        EntityIdentity[] identities = FindObjectsOfType<EntityIdentity>();
        foreach (var identity in identities)
        {
            if (identity != null && identity.entityID == playerID)  // playerID와 일치하는 엔티티를 찾아...
            {
                PlayerStatus ps = identity.GetComponent<PlayerStatus>();
                if (ps != null) { ps.nowState = state; }  //...플레이어 상태를 탈출 중으로 변경
                break;  //플레이어 탈출 시 나머지 엔티티에 대해서는 처리하지 않고 끝냄
            }
        }
    }
}