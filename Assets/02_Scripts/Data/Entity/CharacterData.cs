using UnityEngine;
using UnityEngine.AddressableAssets;

// 플레이어가 조작하는 다이버의 인게임 정보 데이터
[CreateAssetMenu(fileName = "New Character", menuName = "GameData/Character")]
public class CharacterData : ScriptableObject
{
    public int TID;                             // 고유 ID
    public string charName;                     // 이름
    public AssetReferenceSprite chatIcon;       // 아이콘
    public AssetReferenceSprite charStanding;   // 캐릭터 스텐딩
    public AssetReferenceSprite charModel;      // 캐릭터 모델
    public float hpMax;                         // 최대 체력
    public float moveSpeed;                     // 이동 속도
    public float manaMax;                       // 최대 마나
    public float manaRegen;                     // 초당 마나 회복
}
