using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class SkillData
{
    public int TID;                             // 고유 ID
    public UserType userType;                   // 스킬 시전자 종류
    public SkillType skillType;                 // 스킬 종류
    public string skillName;                    // 스킬의 이름 텍스트
    public AssetReferenceSprite skillIcon;      // 스킬 아이콘 이미지 경로
    public float skillCooltime;                 // 스킬 쿨타임
    public EffectType effectType_XX;            // 각 스킬의 효과 종류 타입 
    public TargetType effectTarget_XX;          // 효과 적용 대상
    public float effectDelay_XX;                // 스킬 딜레이 (발동까지 걸리는 시간)
    public float effectValue_XX;                // 스킬 효과 값
}
