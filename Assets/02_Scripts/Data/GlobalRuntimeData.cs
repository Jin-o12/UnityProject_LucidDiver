/// <summary>
/// 게임 실행 중에 여러 시스템이 공통으로 참조해야 하는 데이터를 보관합니다.
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GlobalRuntimeData
{
    // 게임 안에 있는 모든 플레이어 오브젝트 목록
    public static List<GameObject> ActivePlayers = new();

    // 생성된 엔티티를 고유 번호와 함께 저장하는 목록
    public static Dictionary<int, GameObject> entityList = new();

    // 다음에 부여할 엔티티 번호
    // static으로 바로 초기화해야 실제 사용 시점에 값이 유지된다.
    public static int entityCount = 1;

    /* 엔티티 번호를 부여하고 런타임 데이터에 등록 */
    public static void CountingEntityData(GameObject _object)
    {
        if (_object == null)
        {
            Debug.LogWarning("CountingEntityData: 등록할 오브젝트가 없습니다.");
            return;
        }

        entityList.Add(entityCount, _object);

        // 다음 엔티티는 다른 번호를 사용하도록 증가시킨다.
        entityCount++;
    }

    /* 번호로 엔티티 오브젝트를 가져오기 */
    public static GameObject CountingEntityData(int _id)
    {
        if (entityList.TryGetValue(_id, out GameObject obj))
        {
            return obj;
        }

        return null;
    }
}