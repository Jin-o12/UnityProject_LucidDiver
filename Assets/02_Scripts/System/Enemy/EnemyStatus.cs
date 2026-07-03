using System;
using UnityEngine;

public class EnemyStatus : MonoBehaviour, IDamageable
{
    // 적의 상태
    public enum EnemyState { Idle, Chase, Attack, Dead }        // 적이 가질 수 있는 상태의 종류
    public EnemyState nowState { get; private set; }    // 현재 적 상태
    public bool isAttacking { get; private set; }       // 공격 실행 중 여부

    public int objID = 0;                               // 고유 번호: 씬 내의 오브젝트로서의 고유 값을 의미합니다
    public float hpMax;                                 // 전체 체력
    public float hpCurrent;                             // 현재 체력
    public float atkValue  { get; private set; }        // 공격력
    private float defValue;                             // 방어력

    public event Action OnLocalDeath;                   // 내부 이벤트로 죽음여부 전달
    
    public void SetIsAttacking(bool _isAttacking) { isAttacking = _isAttacking; }

    public void SetNowState(EnemyState _nowState) { nowState = _nowState; }

    private void Awake()
    {
        nowState = EnemyState.Idle;

        // 아래 수치들은 프로토타입 환경에서의 일시적인 수치로
        // 이후 게임에서는 적에 따라 유동적으로 값이 지정되게 할 예정 
        hpMax = 100;
        hpCurrent = hpMax;
        atkValue = 10;
        defValue = 0;
    }

    void Start()
    {
        // UI 초기 업데이트
        UpdataHp();
    }

    /* 체력 UI 업데이트 */
    private void UpdataHp()
    {
        // 체력 변동 사항을 이벤트 버스로 전송
        GlobalEventBus.OnEnemyHealthChanged?.Invoke(0, hpCurrent, hpMax);
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
    }

    /* 피격 시 자신의 타입을 반환 */
    public Faction EntityFaction => Faction.enemy;

    /* 피해를 받는 메소드 */
    public void TakeDamage(float dmg)
    {
        hpCurrent = Mathf.Clamp(hpCurrent-dmg, 0, hpMax);
        GlobalEventBus.OnEnemyHealthChanged?.Invoke(objID, hpCurrent, hpMax);

        // 체력이 0 이하로 내려갈 경우 사망처리를 위한 이벤트 발생
        if(hpCurrent <= 0)
        {
            nowState = EnemyState.Dead;
            OnLocalDeath?.Invoke();
            GlobalEventBus.OnEnemyDead?.Invoke(objID);
        }
    }
}
