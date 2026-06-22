/// <summary>
/// 체력 회복 아이템 효과에 대한 Scriptable Object
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect_", menuName = "Item Data/Effects/Heal")]
public class HealEffect : ItemEffect
{
    public override void Execute(GameObject _target)
    {
        // 대상의 체력 관리 컴포넌트를 찾아 회복시킵니다.
        GlobalEventBus.OnHealRequested?.Invoke(_target, effectValue);
    }
}