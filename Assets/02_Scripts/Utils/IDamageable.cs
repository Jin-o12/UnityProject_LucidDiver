/// <summary>
/// 피해를 입을 수 있는 대상에 대한 기능을 정의하는 인터페이스
/// </summary>

// 피해를 받는 대상이 될 수 있는 것 타입
public enum Faction 
{ 
    Player,     // 플래이어
    Enemy,      // 적
    Neutral     // 그 외
}

public interface IDamageable
{
    // 데미지를 받을 시 실행
    Faction EntityFaction { get; }
    void TakeDamage(float damage);
}
