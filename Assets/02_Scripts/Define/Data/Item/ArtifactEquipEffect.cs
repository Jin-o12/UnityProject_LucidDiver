using UnityEngine;

/// <summary>
/// 아티팩트 장착 시 적용될 효과 1개에 대한 데이터입니다.
/// 하나의 아티팩트가 여러 효과를 가질 수 있으므로 ArtifactItemData에서 리스트로 보관합니다.
/// </summary>
[System.Serializable]
public class ArtifactEquipEffect
{
    public ArtifactEffectType effectType; // 장착 시 적용할 효과 종류
    public float effectValue;             // 효과 수치
}
