using UnityEngine;

[System.Serializable]
public class ItemEffect
{
    [Header("효과 기본 설정")]
    public EffectTarget effectTarget;   // 소비 효과 대상
    public EffectType effectType;       // 소비 효과 분류
    public float effectRange;           // 소비 효과 범위
    public float effectValue;           // 소비 효과 값
}
