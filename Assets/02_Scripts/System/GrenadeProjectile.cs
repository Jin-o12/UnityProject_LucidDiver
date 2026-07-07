using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private SkillData skillData;
    private GameObject user;

    [Header("투척 연출 설정")]
    public float flightDuration = 0.5f; // 목적지까지 날아가는 데 걸리는 시간
    public float arcHeight = 3.0f;      // 포물선의 최대 높이

    public void SetupAndThrow(SkillData _skill, GameObject _skillUser, Vector3 _targetPosition)
    {
        skillData = _skill;
        user = _skillUser;

        // 목표 지점으로 날아가는 물리 로직
        StartCoroutine(MoveToTarget(_targetPosition));
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        // flightDuration(0.5초) 동안 목적지를 향해 날아갑니다.
        while (elapsedTime < flightDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 0.0 ~ 1.0 사이의 진행률 (시간 비율)
            float percent = elapsedTime / flightDuration;

            // 1. 선형 이동 (시작점에서 목표점까지 직선 이동)
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, percent);

            // 2. 포물선 높이 계산 (Mathf.Sin을 사용해 0->1->0 으로 부드럽게 올라갔다 내려오는 곡선 생성)
            float heightOffset = Mathf.Sin(percent * Mathf.PI) * arcHeight;

            // 직선 이동 좌표에 높이를 더해 최종 좌표 적용
            currentPos.y += heightOffset;
            transform.position = currentPos;

            yield return null; // 다음 프레임까지 대기
        }

        // 임시로 목표 지점에 바로 도착했다고 가정
        transform.position = targetPosition;

        // 도착했으니 폭발 대기열 시작
        StartCoroutine(ExplosionSequence());
        yield return null;
    }

    private IEnumerator ExplosionSequence()
    {
        // 첫번째 스킬 효과의 딜레이를 가져와 대기
        float delay = skillData.effects.Count > 0 ? skillData.effects[0].effectDelay : 0.0f;
        yield return new WaitForSeconds(delay);

        // 폭팔 수행 및 범위 내 적 탐색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillData.areaWidth);
        Debug.Log("투척");

        // 범위 내 콜라이더들에 대해 차례대로 이펙트 판정 수행
        foreach(Collider hit in hitColliders)
        {
            IEffectReceiver receiver = hit.GetComponent<IEffectReceiver>();
            if (receiver != null && hit.gameObject != user)
            {
                ApplySkillEffects(receiver);
            }
        }
        Destroy(gameObject);
    }

    private void ApplySkillEffects(IEffectReceiver _receiver)
    {
        foreach(SkillEffect effect in skillData.effects)
        {
            switch(effect.effectType)
            {
                case EffectType.Get_Damage:
                    _receiver.TakeDamage(effect.effectValue); 
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (skillData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, skillData.areaWidth);
        }
    }
}
