/// <summary>
/// 게임 실행 중에 여러 시스템이 공통으로 접근해야 하는 데이터를 보관합니다
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GlobalRuntimeData
{   
    // 게임 상에 있는 모든 플레이어의 Transform 값
    private static List<GameObject> ActivePlayers = new();
    public static List<GameObject> GetActivePlayers() => ActivePlayers;

    // 엔티티 생성 시 해당 위치에 저장
    private static Dictionary<int, GameObject> playerList = new();
    public static Dictionary<int, GameObject> GetPlayerList() => playerList;
    private static Dictionary<int, GameObject> enemyList = new();
    public static Dictionary<int, GameObject> GetEnemyList() => enemyList;

    private static int entityCount = 1;

    /* 플레이어를 생성해 고유 엔티티 번호를 할당 해주고 카운팅 */    
    public static int CountingPlayerData(GameObject _obbject)
    {
        playerList.Add(entityCount, _obbject);
        return entityCount++;   
    }

    //* 적을 생성해 고유 엔티티 번호를 할당 해주고 카운팅 */ 
    public static int CountingEnemyData(GameObject _object)
    {
        enemyList.Add(entityCount, _object);
        return entityCount++; 
    }

    /* 번호로 플레이어 엔티티 가져오가 할당 */
    public static GameObject FindPlayerData(int _id)
    {
        if (playerList.TryGetValue(_id, out GameObject obj))
        {
            return obj;
        }

        return null;
    }

    /* 번호로 적 엔티티 가져오가 할당 */
    public static GameObject FindEnemyData(int _id)
    {
        if (enemyList.TryGetValue(_id, out GameObject obj))
        {
            return obj;
        }

        return null;
    }
}
