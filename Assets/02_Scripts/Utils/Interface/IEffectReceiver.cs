/// <summary>
/// 피해를 입거나 아이템, 스킬 등 효과를 받을 수 있는 대상에 대한 기능을 정의하는 인터페이스
/// </summary>
public interface IEffectReceiver
{
    Faction EntityFaction { get; }
    void TakeDamage(float damage);
    void HealthRecoverInst(float amount);
    void ManaRecoverInst(float amount);
    void ApplyAggro(UnityEngine.Transform target, float duration);
}
