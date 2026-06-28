/// <summary>
/// 피해를 입을 수 있는 대상에 대한 기능을 정의하는 인터페이스
/// </summary>


public interface IDamageable
{
    // 데미지를 받을 시 실행
    Faction EntityFaction { get; }
    void TakeDamage(float damage);
}
