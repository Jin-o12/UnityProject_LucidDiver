using UnityEngine;
public interface IResultService  //인게임 세션 플레이 결과 (성공 / 실패) 시 처리를 관리하는 인터페이스.
{
    void Register(int playerID, Component playerComponent);     //playerID에 정해진 플레이어 컴포넌트를 등록
    void Unregister(int playerID);                              //playerID에서 컴포넌트 등록 해제
    void UnregisterIfOwner(int playerID, Component requester);  //plyaerID가 requester 자신인 경우에 등록 해제
    Component GetPlayerComponent(int playerID);                 //playerID에 등록된 컴포넌트를 참조
    void HandleEscapeStart(int playerID);                       //탈출 채널링 시작 시 처리
    void HandleEscapeGameover(int playerID);                    //강제 각성 시 처리
    void HandleEscapeIdle(int playerID);                        //탈출 취소 시 기본 상태로 돌아가는 처리
    void HandleGameAbandon();                                   //메뉴에서 게임 포기를 선택했을 때 실패 정산 요청
}