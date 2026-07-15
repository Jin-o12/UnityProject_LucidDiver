using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEffectProcessor : MonoBehaviour
{
    public static ItemEffectProcessor Instance { get; private set; }

    // 아이템 사용 사운드 리스트
    public int healthItemAudioID = 10701;  //붕대 소모품 사용 SFX
    public int mpGainItemAudioID = 10704;  //사탕 소모품 사용 SFX

    private void Awake()
    {
        if(Instance==null)
            Instance = this;
    }

    public void UseConsumeItem(ConsumeItemData _itemData, GameObject _user, GameObject _target=null)
    {
        // 아이템이 가진 모든 효과 실행
        foreach(ItemEffect effect in _itemData.useEffect)
        {
            ApplyEffect(effect, _user, _target);
        }

        // 사운드 재생
        if (_itemData.TID == 301)
        {
            GlobalEventBus.OnPlay3DSoundRequested?.Invoke(healthItemAudioID, _user.transform.position);
        }
        if (_itemData.TID == 302)
        {
            GlobalEventBus.OnPlay3DSoundRequested?.Invoke(mpGainItemAudioID, _user.transform.position);
        }
    }

    private void ApplyEffect(ItemEffect _effect, GameObject _user, GameObject _target)
    {
        // 타겟 설정
        GameObject target = (_effect.effectTarget==EffectTarget.self) ? _user : _target;

        if(target==null) return;

        IEffectReceiver receiver = target.GetComponent<IEffectReceiver>();

        if(receiver == null)
        {
            Debug.LogWarning($"{target.name} 객체는 아이템 효과를 받을 수 없는 대상입니다!");
            return;
        }

        // 효과 타입에 따라 인터페이스의 함수 실행
        switch (_effect.effectType)
        {
            case EffectType.healHP:
                receiver.HealthRecoverInst(_effect.effectValue);
                break;

            case EffectType.healMP:
                receiver.ManaRecoverInst(_effect.effectValue);
                break;
        }
    }
}
