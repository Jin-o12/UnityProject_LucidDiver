/// <summary>
/// 게임 실행 중에 여러 시스템이 공통으로 접근해야 하는 데이터를 보관합니다
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GlobalRuntimeData
{   
    // 게임 상에 있는 모든 플레이어의 Transform 값
    public static List<Transform> ActivePlayers = new List<Transform>();
}
