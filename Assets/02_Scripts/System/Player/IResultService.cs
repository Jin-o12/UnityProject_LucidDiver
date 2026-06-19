using UnityEngine;
public interface IResultService  //인게임 세션 플레이 결과 (성공 / 실패) 시 처리를 관리하는 인터페이스.
{
    void Register(int playerID, PlayerStatus playerComponent);     //playerID에 정해진 플레이어 컴포넌트를 등록
    void Unregister(int playerID);                              //playerID에서 컴포넌트 등록 해제
    PlayerStatus GetPlayerStatus(int playerID);                 //playerID에 등록된 컴포넌트를 참조
    void HandleEscapeSuccess(int playerID);                     //탈출 성공 시 결과 처리
    void HandleEscapeFail(int playerID);                        //탈출 실패 시 결과 처리
}