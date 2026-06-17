/// <summary>
/// 객체 프리팹이 게임 내에서 가지는 고유한 정보를 저장하는 데이터 레이어
/// 오브젝트로서의 고유 번호, 종류 등을 저장합니다
/// [26.06.17_강다영] 현재는 해당 사항을 직접 지정하고, 이후 게임에서 스폰 시스템이 개발 된 이후에는 게임이 고유 번호를 할당해주도록 변경할 것
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
