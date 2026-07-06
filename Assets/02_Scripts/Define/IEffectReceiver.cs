/// <summary>
/// 아이템, 스킬 등 효과를 받을 수 있는 대상에게 부착합니다.
/// </summary>
public interface IEffectReceiver
{
    void HealthRecoverInst(float amount);
    void ManaRecoverInst(float amount);
}
