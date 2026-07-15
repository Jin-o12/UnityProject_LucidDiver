using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private SkillData skillData;
    private GameObject user;
    private CasterStatPayload playerstat;

    [Header("투척 연출 설정")]
    public float flightDuration = 0.5f; // 목적지까지 날아가는 데 걸리는 시간
    public float arcHeight = 3.0f;      // 포물선의 최대 높이
    private int Decoy_AudioID = 10801;  // 어그로 디코이 사운드 이펙트 

    public void SetupAndThrow(SkillData _skill, GameObject _skillUser, CasterStatPayload _stats, Vector3 _targetPosition)
    {
        skillData = _skill;
        user = _skillUser;
        playerstat = _stats;

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

        // 같은 폭발에 동일한 effectHitVFX ID가 여러 번 있어도 한 번만 재생합니다.
        if (VFXService.Instance != null)
        {
            HashSet<string> playedVfxIds = new HashSet<string>();
            foreach (SkillEffect effect in skillData.effects)
            {
                if (!string.IsNullOrWhiteSpace(effect.effectHitVFX) && playedVfxIds.Add(effect.effectHitVFX))
                    VFXService.Instance.Play(effect.effectHitVFX, transform.position, transform.rotation);
            }
        }

        // 모든 효과 중 가장 넓은 범위와 최대 어그로 지속 시간을 탐색
        float maxRadius = 0f;
        float maxDuration = 0f;
        foreach (var effect in skillData.effects)
        {
            if (effect.areaWidth > maxRadius) maxRadius = effect.areaWidth;
            if (effect.effectType == EffectType.aggro && effect.effectValue > maxDuration) maxDuration = effect.effectValue;
        }

        foreach (SkillEffect _effect in skillData.effects)
        {
            // 어그로 효과가 있다면 3D 사운드 오브젝트를 생성합니다
            if (_effect.effectType == EffectType.aggro)
            {
                GameObject sfxObj = GlobalEventBus.OnPlay3DSoundRequestedWithHandle?.Invoke(Decoy_AudioID, transform.position);
                if (sfxObj != null)
                {
                    // 어그로 효과의 지속시간이 끝나면 사운드 오브젝트가 소멸합니다
                    StartCoroutine(StopAndDestroyTempSoundAfter(sfxObj, maxDuration));
                }
            }
        }

        // 폭발 수행 및 범위 내 콜라이더 탐색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxRadius);

        // 이미 효과를 받은 대상을 기록하여 중복 타격을 방지
        HashSet<IEffectReceiver> hitReceivers = new HashSet<IEffectReceiver>();
        IEffectReceiver userReceiver = user != null ? user.GetComponent<IEffectReceiver>() : null;

        // 범위 내 콜라이더들에 대해 차례대로 이펙트 판정 수행
        foreach(Collider hit in hitColliders)
        {
            // 적의 콜라이더가 자식 오브젝트에 있을 수 있으므로 GetComponentInParent를 사용
            IEffectReceiver receiver = hit.GetComponentInParent<IEffectReceiver>();
            
            if (receiver != null && receiver != userReceiver && !hitReceivers.Contains(receiver))
            {
                hitReceivers.Add(receiver);
                
                // 실제 거리를 계산하여 범위 내에 있는지 효과별로 확인하도록 거리를 함께 전달
                MonoBehaviour receiverMono = receiver as MonoBehaviour;
                if (receiverMono != null)
                {
                    float distance = Vector3.Distance(transform.position, receiverMono.transform.position);
                    ApplySkillEffects(receiver, distance);
                }
            }
        }

        // 어그로 효과가 있다면 타겟으로 지정될 수 있도록 파괴를 지연시킵니다
        if (maxDuration > 0f)
        {
            MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            yield return new WaitForSeconds(maxDuration);
        }

        Destroy(gameObject);
    }

    // 정해진 지속 시간 후 소리 오브젝트를 소멸하는 코루틴
    private IEnumerator StopAndDestroyTempSoundAfter(GameObject soundObj, float delay)
    {
        if (soundObj == null) yield break;

        //delay 시간 동안 대기한 후 AudioSource 오브젝트를 제거합니다
        yield return new WaitForSeconds(delay);
        if (soundObj.TryGetComponent<AudioSource>(out var src)) GlobalEventBus.OnStop3DSoundRequested?.Invoke(src);
    }

    private void ApplySkillEffects(IEffectReceiver _receiver, float distance)
    {
        foreach(SkillEffect effect in skillData.effects)
        {
            // 해당 효과의 범위를 벗어났으면 무시
            if (distance > effect.areaWidth) continue;

            switch(effect.effectType)
            {
                case EffectType.damage:
                    _receiver.TakeDamage(playerstat.attackPower * effect.effectValue); 
                    break;
                case EffectType.aggro:
                    _receiver.ApplyAggro(this.transform, effect.effectValue);
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (skillData != null)
        {
            Gizmos.color = Color.red;
            float maxRadius = 0f;
            foreach (var effect in skillData.effects)
            {
                if (effect.areaWidth > maxRadius) maxRadius = effect.areaWidth;
            }
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }
}
