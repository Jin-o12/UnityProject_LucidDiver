using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect_", menuName = "Item Data/Effects/Heal")]
public class HealEffect : ItemEffect
{
    public override void Execute(GameObject _target)
    {
        // 대상의 체력 관리 컴포넌트를 찾아 회복시킵니다.
        int finalHealAmount = (int)effectValue;

        // if (_target.TryGetComponent<PlayerStatus>(out var hp))
        // {
        //     hp.Restore(finalHealAmount);
        // }
    }
}