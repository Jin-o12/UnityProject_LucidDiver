/// <summary>
/// 객체 프리팹이 게임 내에서 가지는 고유한 정보를 저장하는 데이터 레이어
/// 오브젝트로서의 고유 번호, 종류 등을 저장합니다
/// </summary>
using UnityEngine;

public class EntityIdentity : MonoBehaviour
{
    [SerializeField] public int entityID;
    [SerializeField] public Faction entityFaction;

    // 서버나 게임 매니저가 플레이어를 소환할 때 최초 1회 호출해주는 초기화 함수
    public void SetupIdentity(int id, Faction faction)
    {
        entityID = id;
        entityFaction = faction;
    }
}
