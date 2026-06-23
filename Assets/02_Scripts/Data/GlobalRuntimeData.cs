/// <summary>
/// 게임 실행 중에 여러 시스템이 공통으로 접근해야 하는 데이터를 보관합니다
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GlobalRuntimeData
{   
    // 게임 상에 있는 모든 플레이어의 Transform 값
    public static List<GameObject> ActivePlayers = new();

    // 엔티티 생성 시 해당 위치에 저장
    public static Dictionary<EntityIdentity, GameObject> entityList = new(); 
}
