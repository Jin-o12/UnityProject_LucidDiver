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
    public static Dictionary<int, GameObject> entityList = new();

    public static int entityCount;

    public GlobalRuntimeData()
    {
        entityCount = 1;
    }

    /* 엔티티 번호 할당 */
    public static void CountingEntityData(GameObject _object)
    {
        entityList.Add(entityCount, _object);
    }

    /* 번호로 엔티티 가져오가 할당 */
    public static GameObject CountingEntityData(int _id)
    {
        if (entityList.TryGetValue(_id, out GameObject obj))
        {
            return obj;
        }
        return null;
    }
}
