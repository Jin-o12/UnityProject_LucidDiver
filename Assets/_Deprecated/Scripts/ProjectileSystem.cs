/// <summary>
/// 발사체에 대한 시스템 구현 스크립트
/// [26.06.15_강다영] 총알 아이템이 사라졌으므로, 이후 크게 수정되거나 삭제될 수 있음
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSystem : MonoBehaviour
{
    // 이후 무기의 스테이터스를 따르도록 변경
    public float speed = 20f;           // 총알 탄속
    public float lifeTime = 0.5f;       // 발사 시간
    public float damage;                // 데미지
    public Faction ownerFaction;        // 해당 발사체를 쏜 주체의 타입
    public float fireRate;              // 발사 간격
    public float fireRange;             // 발사 사거리

    /* 생성 시 총기와 발사 주체의 정보를 기록 (데미지, 발사 주체(Faction))*/
    public void Setup(float _damage, Faction _ownerFaction, float _fireRate, float _fireRange)
    {
        damage = _damage;
        ownerFaction = _ownerFaction;
        fireRate = _fireRate;
        fireRange = _fireRange;
    }
    
    void Start()
    {
        // 현재는 시간을 변수로 지정하지만 이후에는 발사체 데이터로 부터 시간을 가져오게 바꿀 것
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        IEffectReceiver target = other.GetComponentInParent<IEffectReceiver>();
        
        if (target != null)
        {
            // 진영(Faction)이 다른 경우에만 데미지 처리
            if (target.EntityFaction != ownerFaction)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
