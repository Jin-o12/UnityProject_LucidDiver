using UnityEngine;
public class EscapePortal : MonoBehaviour, IInteractable
{
    private bool isEscaping = false;    //탈출 코루틴 실행 중 판정
    public bool Interact(int playerID)  //상호작용 메소드 실행
    {
        // 탈출 로케이터를 참조하고 null 체크
        var svc = ResultServiceLocator.Instance;
        // 로케이터 매핑에서 PlayerStatus 참조 후 null 및 idle 상태 검사
        var ps = svc.GetPlayerStatus(playerID);
        if (ps == null || ps.nowState != PlayerStatus.livingState.idle) return false;
        // 이미 실행 중인 탈출 상호작용이 있다면 중복 실행하지 않습니다.
        if (isEscaping) return false;
        //ResultManager에서 탈출 판정을 실행
        #region P0.5 이후 escapeTime 채널링 적용 시 주석 해제
        //.."채널링 시간만큼 대기 후 탈출 성공 판정" 코루틴을 시작
        // StartCoroutine(EscapeCoroutine(playerID));
        #endregion
        // (P0.5 이후 escapeTime 채널링 적용 시 주석 처리하여 EscapeCoroutine(playerID) 코루틴으로 처리 플로우를 위임)
        svc.HandleEscapeSuccess(playerID);
        //탈출 상호작용 실행 중 상태를 true로 설정
        isEscaping = true;
        return true;
    }
    #region P0.5 이후 escapeTime 채널링 적용 시 주석 해제
    // public float escapeTime = 0f; //탈출 시까지 대기하는 채널링 시간 (즉시 탈출하려면 0f로 입력)
    //채널링 시간만큼 대기 후 탈출 성공 판정
    //public IEnumerator EscapeCoroutine(int playerID)
    //{
    //escapeTime만큼 대기
    //    yield return new WaitForSeconds(escapeTime);
    //탈출 판정을 실행
    // ResultManager.Instance.HandleEscapeSuccess(playerID);
    //}
    #endregion
}