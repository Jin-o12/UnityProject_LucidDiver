using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    [Header("효과 기본 설정")]
    public TargetType effectTarget;   // 소비 효과 대상
    public EffectType effectType;       // 소비 효과 분류
    public float effectRange;           // 소비 효과 범위
    public float effectValue;           // 소비 효과 값
    
    // 효과를 가진 아이템의 사용 효과
    public abstract void Execute(GameObject target);
}
