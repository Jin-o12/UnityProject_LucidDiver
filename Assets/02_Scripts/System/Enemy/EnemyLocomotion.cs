using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 실제 몸 움직임을 담당하는 모듈입니다.
/// 일반 이동, 정지, 방향 전환, 공격 중 짧은 돌진을 여기서 처리합니다.
/// </summary>
[Serializable]
public class EnemyLocomotion
{
    [SerializeField] private float moveSpeed = 3.0f;     // 기본 추적 이동 속도

    public void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
    }

    /// <summary>
    /// NavMeshAgent 초기 속도를 설정합니다.
    /// </summary>
    public void Initialize(NavMeshAgent agent)
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    /// <summary>
    /// 목적지를 향해 이동시키고, 걷기/방향 애니메이션 이벤트를 갱신합니다.
    /// </summary>
    public void MoveTo(
        Transform self,
        NavMeshAgent agent,
        Vector3 destination,
        EnemyStatus status,
        EnemyStatus.EnemyState moveState,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent)
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = moveSpeed;
        agent.isStopped = false;
        agent.SetDestination(destination);
        status.SetNowState(moveState);
        onWalkEvent?.Invoke(true);
        FacePosition(self, destination, onLookDirEvent);
    }

    /// <summary>
    /// 현재 이동을 즉시 멈추고 애니메이션도 정지 상태로 전환합니다.
    /// </summary>
    public void Stop(NavMeshAgent agent, Action<bool> onWalkEvent)
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        onWalkEvent?.Invoke(false);
    }

    /// <summary>
    /// 이동과 공격 상태를 모두 풀고 대기 상태로 돌립니다.
    /// </summary>
    public void SetIdle(NavMeshAgent agent, EnemyStatus status, Action<bool> onWalkEvent)
    {
        status.SetNowState(EnemyStatus.EnemyState.Idle);
        status.SetIsAttacking(false);

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        onWalkEvent?.Invoke(false);
    }

    /// <summary>
    /// 목표 위치를 즉시 바라보도록 회전시키고 방향 애니메이션 값을 갱신합니다.
    /// </summary>
    public void FacePosition(Transform self, Vector3 targetPosition, Action<int, int> onLookDirEvent)
    {
        Vector3 lookTarget = new Vector3(targetPosition.x, self.position.y, targetPosition.z);
        if ((lookTarget - self.position).sqrMagnitude <= 0.001f)
        {
            return;
        }

        self.LookAt(lookTarget);
        UpdateLookDirection(self, onLookDirEvent);
    }

    /// <summary>
    /// 2타 직전에는 목표를 완전히 꺾지 않고, 지정된 각도까지만 보정합니다.
    /// </summary>
    public void FacePositionLimited(Transform self, Vector3 targetPosition, float maxTurnAngle, Action<int, int> onLookDirEvent)
    {
        Vector3 currentForward = self.forward;
        currentForward.y = 0.0f;

        Vector3 targetDirection = EnemyMathUtility.GetFlatDirection(self.position, targetPosition);
        if (currentForward.sqrMagnitude <= 0.001f || targetDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 limitedForward = Vector3.RotateTowards(
            currentForward.normalized,
            targetDirection.normalized,
            Mathf.Deg2Rad * Mathf.Max(0.0f, maxTurnAngle),
            0.0f);

        self.rotation = Quaternion.LookRotation(limitedForward, Vector3.up);
        UpdateLookDirection(self, onLookDirEvent);
    }

    /// <summary>
    /// 공격 연출용 짧은 전진 돌진을 수행하고, 끝난 뒤 NavMeshAgent 위치를 동기화합니다.
    /// </summary>
    public IEnumerator PerformLunge(Transform self, NavMeshAgent agent, float distance, float duration)
    {
        if (distance <= 0.0f || duration <= 0.0f)
        {
            yield break;
        }

        Vector3 flatForward = self.forward;
        flatForward.y = 0.0f;

        if (flatForward.sqrMagnitude <= 0.001f)
        {
            yield break;
        }

        flatForward.Normalize();

        Vector3 start = self.position;
        Vector3 end = start + flatForward * distance;
        end.y = start.y;

        if (NavMesh.SamplePosition(end, out NavMeshHit hit, Mathf.Max(0.2f, distance), NavMesh.AllAreas))
        {
            end = hit.position;
            end.y = start.y;
        }

        bool previousUpdatePosition = false;
        if (agent != null)
        {
            previousUpdatePosition = agent.updatePosition;
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            self.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        self.position = end;

        if (agent != null)
        {
            agent.Warp(self.position);
            agent.updatePosition = previousUpdatePosition;
        }
    }

    /// <summary>
    /// 현재 회전값을 기존 애니메이터가 쓰는 lookDir/lookRight 값으로 변환합니다.
    /// </summary>
    private void UpdateLookDirection(Transform self, Action<int, int> onLookDirEvent)
    {
        int lookUp = self.rotation.y > 0.0f ? 0 : 1;
        int lookRight = Mathf.Abs(self.rotation.y) < 0.5f ? -1 : 1;
        onLookDirEvent?.Invoke(lookUp, lookRight);
    }
}
