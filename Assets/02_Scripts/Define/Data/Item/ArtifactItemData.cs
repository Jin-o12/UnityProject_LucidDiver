using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아티팩트 아이템 전용 데이터입니다.
/// 기본 아이템 정보는 ItemData를 사용하고, 장착 분류와 장착 효과 목록만 추가로 보관합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Artifact", menuName = "GameData/Item/Artifact")]
public class ArtifactItemData : ItemData
{
    [Header("아티팩트 아이템 정보")]
    public string itemFlavorText;                         // 플레이버 텍스트
    public ArtifactCategory artifactCategory;              // 아티팩트 분류
    public List<ArtifactEquipEffect> equipEffects = new(); // 장착 효과 목록
    public int sellValue;                                  // 판매 가격
}
