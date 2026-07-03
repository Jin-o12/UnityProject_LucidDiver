using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 감지 능력을 담당하는 모듈입니다.
/// 시야 범위, 인지 유지 범위, 청각 범위를 각각 분리해 관리합니다.
/// </summary>
[Serializable]
public class EnemyPerception
{
    [SerializeField] private float sightRange = 10.0f;       // 최초 발견이 가능한 시야 거리
    [SerializeField] private float awarenessRange = 20.0f;   // 한 번 인지한 뒤 놓치지 않는 유지 거리
    [SerializeField] private float hearingRange = 40.0f;     // 들을 수 있는 최대 거리
    [SerializeField] private float sightAngle = 120.0f;      // 시야각
    [SerializeField] private float eyeHeight = 1.4f;         // 레이캐스트 시선 높이

    [NonSerialized] private float sightRangeSqr;             // 시야 거리 제곱값 캐시
    [NonSerialized] private float awarenessRangeSqr;         // 인지 유지 거리 제곱값 캐시
    [NonSerialized] private float hearingRangeSqr;           // 청각 거리 제곱값 캐시
    [NonSerialized] private float halfSightAngle;            // 반 시야각 캐시

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
    /// 전달된 소음 자극이 청각 범위 안에 들어오는지 확인합니다.
    /// </summary>
    public bool CanHear(Vector3 selfPosition, NoiseStimulus stimulus)
    {
        float effectiveRadius = Mathf.Min(hearingRange, Mathf.Max(0.0f, stimulus.Radius));
        if (effectiveRadius <= 0.0f)
        {
            return false;
        }

        float sqrDistance = EnemyMathUtility.GetPlanarSqrDistance(selfPosition, stimulus.Position);
        return sqrDistance <= hearingRangeSqr && sqrDistance <= effectiveRadius * effectiveRadius;
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
}
