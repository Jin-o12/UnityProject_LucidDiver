using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 감지 능력을 담당하는 모듈입니다.
/// 시야 범위, 추적 유지 범위, 청각 범위를 분리해서 관리합니다.
/// </summary>
[Serializable]
public class EnemyPerception
{
    [SerializeField] private float sightRange = 10.0f;       // 시야로 플레이어를 새로 발견할 수 있는 거리
    [SerializeField] private float awarenessRange = 20.0f;   // 이미 본 대상을 놓치지 않고 유지하는 거리
    [SerializeField] private float hearingRange = 40.0f;     // 소리를 들을 수 있는 최대 거리
    [SerializeField] private float sightAngle = 120.0f;      // 시야각
    [SerializeField] private float eyeHeight = 1.4f;         // 시야/청각 판정 시작 높이

    [Header("Hearing")]
    [SerializeField] private bool requireLineOfHearing = true; // 벽이 끼어 있으면 청각 반응을 막을지 여부
    [SerializeField] private LayerMask hearingObstacleMask;    // 소리를 막는 벽/장애물 레이어
    [SerializeField] private float soundSampleHeight = 1.2f;   // 소리 위치를 판정할 때 사용할 기준 높이

    [NonSerialized] private float sightRangeSqr;               // 시야 거리 제곱 캐시
    [NonSerialized] private float awarenessRangeSqr;           // 인지 유지 거리 제곱 캐시
    [NonSerialized] private float hearingRangeSqr;             // 청각 거리 제곱 캐시
    [NonSerialized] private float halfSightAngle;              // 반 시야각 캐시

    public float SightRange => sightRange;
    public float AwarenessRange => awarenessRange;
    public float HearingRange => hearingRange;
    public float SightAngle => sightAngle;
    public float EyeHeight => eyeHeight;

    public void OnValidate()
    {
        sightRange = Mathf.Max(0.0f, sightRange);
        awarenessRange = Mathf.Max(sightRange, awarenessRange);
        hearingRange = Mathf.Max(awarenessRange, hearingRange);
        sightAngle = Mathf.Clamp(sightAngle, 0.0f, 360.0f);
        eyeHeight = Mathf.Max(0.0f, eyeHeight);
        soundSampleHeight = Mathf.Max(0.0f, soundSampleHeight);

        sightRangeSqr = sightRange * sightRange;
        awarenessRangeSqr = awarenessRange * awarenessRange;
        hearingRangeSqr = hearingRange * hearingRange;
        halfSightAngle = sightAngle * 0.5f;
    }

    /// <summary>
    /// 현재 시야에 들어온 플레이어 중 가장 가까운 대상을 반환합니다.
    /// </summary>
    public Transform FindVisibleTarget(Transform self, ICollection<GameObject> players)
    {
        float closestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (GameObject player in players)
        {
            if (player == null)
            {
                continue;
            }

            float sqrDistance = EnemyMathUtility.GetPlanarSqrDistance(self.position, player.transform.position);
            if (sqrDistance > sightRangeSqr)
            {
                continue;
            }

            if (!IsTargetInSight(self, player.transform))
            {
                continue;
            }

            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                bestTarget = player.transform;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// 이미 인지한 대상을 아직 놓치지 않았는지 거리 기준으로 확인합니다.
    /// </summary>
    public bool CanKeepAwareness(Vector3 selfPosition, Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return EnemyMathUtility.GetPlanarSqrDistance(selfPosition, target.position) <= awarenessRangeSqr;
    }

    /// <summary>
    /// 전달된 소리가 청각 범위 안에 있고, 필요하면 벽 차단 판정까지 통과하는지 확인합니다.
    /// </summary>
    public bool CanHear(Transform self, NoiseStimulus stimulus)
    {
        if (self == null)
        {
            return false;
        }

        float effectiveRadius = Mathf.Min(hearingRange, Mathf.Max(0.0f, stimulus.Radius));
        if (effectiveRadius <= 0.0f)
        {
            return false;
        }

        float sqrDistance = EnemyMathUtility.GetPlanarSqrDistance(self.position, stimulus.Position);
        if (sqrDistance > hearingRangeSqr || sqrDistance > effectiveRadius * effectiveRadius)
        {
            return false;
        }

        // 수정 이유:
        // 기존에는 거리만 맞으면 소리를 들은 것으로 처리해서,
        // 벽 너머 총성이나 노이즈 오브젝트에도 적이 그대로 반응했습니다.
        // 이제는 적의 귀 위치와 소리 위치 사이에 차단물이 있으면 청각 반응을 막습니다.
        if (!requireLineOfHearing)
        {
            return true;
        }

        LayerMask resolvedObstacleMask = ResolveHearingObstacleMask();
        if (resolvedObstacleMask.value == 0)
        {
            return true;
        }

        Vector3 earPosition = self.position + Vector3.up * eyeHeight;
        Vector3 soundPosition = stimulus.Position + Vector3.up * soundSampleHeight;

        return !Physics.Linecast(
            earPosition,
            soundPosition,
            resolvedObstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 시야각과 레이캐스트 차폐를 함께 검사해 실제로 보이는 대상인지 판정합니다.
    /// </summary>
    private bool IsTargetInSight(Transform self, Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 flatForward = self.forward;
        flatForward.y = 0.0f;

        Vector3 flatDirectionToTarget = target.position - self.position;
        flatDirectionToTarget.y = 0.0f;

        if (flatDirectionToTarget.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angleToTarget = Vector3.Angle(flatForward.normalized, flatDirectionToTarget.normalized);
        if (angleToTarget > halfSightAngle)
        {
            return false;
        }

        Vector3 eyePosition = self.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up * eyeHeight;
        Vector3 directionToTarget = targetPosition - eyePosition;
        float targetDistance = directionToTarget.magnitude;

        if (targetDistance <= 0.001f)
        {
            return true;
        }

        if (Physics.Raycast(
                eyePosition,
                directionToTarget.normalized,
                out RaycastHit hit,
                targetDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return false;
    }

    /// <summary>
    /// 인스펙터에서 따로 지정하지 않았으면 프로젝트의 Wall 레이어를 기본 청각 차단물로 사용합니다.
    /// 기존 프리팹을 바로 다시 저장하지 않아도 벽 차단 로직이 우선 동작하도록 하기 위한 보정입니다.
    /// </summary>
    private LayerMask ResolveHearingObstacleMask()
    {
        if (hearingObstacleMask.value != 0)
        {
            return hearingObstacleMask;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        return wallLayer >= 0 ? 1 << wallLayer : 0;
    }
}
