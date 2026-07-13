using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "New Character", menuName = "GameData/Character")]
public class CharacterData : ScriptableObject
{
    // 캐릭터 고유 정보
    public int TID;                             // 고유 ID
    public string charName;                     // 이름
    public int mindRecordNum;                   // 심상 기록 갯수
    public int skillNum;                        // 고유 스킬 번호

    // 인게임 스텟
    public float hpMax;                         // 최대 체력
    public float manaMax;                       // 최대 마나
    public float manaRegen;                     // 초당 마나 회복
    public int weaponNum;                       // 스폰 시 할당 될 무기 번호
    
    // 레벨업(동조율 상승) 기획 데이터
    [Header("Level System")]
    [Tooltip("각 레벨업 구간별 요구 동조율(경험치)량")]
    public float[] requireLinkRatePerLevel = new float[4];
    // 캐릭터 일러스트
    public AssetReferenceSprite chatIcon;       // 아이콘
    public AssetReferenceSprite charStanding;   // 캐릭터 스텐딩
    public AssetReference charModel;            // 캐릭터 모델 (프리팹)

    // 이동 관련 데이터
    public float moveSpeed;                     // 이동 속도 (걷기 중)
    public float sprintSpeed;                   // 이동 속도 (달리기 중)
    public float sprintMana;                    // 달리기 중 초당 마나 소비
    public float sprintRecoverTime;             // 달리기 불가 상태 지속 시간
    public float evadeSpeed;                    // 이동 속도 (구르기 중)
    public float evadeTime;                     // 구르기 동작 시간
    public float evadeMana;                     // 구르기 마나 소비
    public float evadeCooltime;                 // 구르기 쿨타임
}
