/// <summary>
/// 마나 회복 아이템 효과에 대한 Scriptable Object
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "GainManaEffect", menuName = "Item Data/Effects/gainMana")]
public class GainManaEffect : ItemEffect
{
    // public override void Execute(GameObject _target)
    // {
    //     // 대상의 마나 관리 컴포넌트를 찾아 회복시킵니다.
    //     GlobalEventBus.OnGainManaRequested?.Invoke(_target, effectValue);
    // }
}
