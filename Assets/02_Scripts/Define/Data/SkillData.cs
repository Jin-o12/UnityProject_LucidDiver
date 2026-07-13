using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 스킬 데이터의 원형 클래스
/// </summary>

// 단일 효과(Effect) 클래스
[System.Serializable]
public class SkillEffect
{
    public EffectType effectType;
    public AreaType areaType;
    public float areaWidth;
    public EffectTarget effectTarget;
    public float effectDelay;
    public float effectValue;
    public string effectHitSFX;
    public string effectHitVFX;
}

public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public int TID;
    public string skillName;
    public string skillDesc;
    public string skillIcon;
    public string skillPrefab;
    public float skillCooltime;
    public float mpCost;

    [Header("시전 정보")]
    public float fireRange;
    public float activateTime;
    public string activateSFX;
    public string activateVFX;
    public EffectTargetType targetType;
    public EffectTarget targetObjectCategory;

    [Header("효과 리스트")]
    public List<SkillEffect> effects = new List<SkillEffect>();
}