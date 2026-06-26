/// <summary>
/// 시야 범위를 Scene 뷰에서 확인하기 위한 디버그용 기즈모 스크립트
/// 현재는 EnemyMovement의 시야 데이터를 읽어서 표시합니다.
/// </summary>
using UnityEngine;

public class VisionGizmo : MonoBehaviour
{
    private enum GizmoDrawMode
    {
        SelectedOnly,   // 오브젝트를 선택했을 때만 표시
        Always          // 항상 표시
    }

    [Header("Draw Mode")]
    [SerializeField] private GizmoDrawMode drawMode = GizmoDrawMode.SelectedOnly; // 기즈모 표시 방식

    [Header("Gizmo Option")]
    [SerializeField] private bool drawSightRadius = true;     // 시야 반경 원 표시 여부
    [SerializeField] private bool drawTargetLine = true;      // 현재 타겟까지 선 표시 여부
    [SerializeField] private int arcSegmentCount = 24;        // 부채꼴 곡선을 몇 개 선분으로 나눌지

    [Header("Gizmo Color")]
    [SerializeField] private Color radiusColor = Color.yellow;    // 시야 반경 색
    [SerializeField] private Color forwardColor = Color.red;      // 정면 방향 색
    [SerializeField] private Color boundaryColor = Color.cyan;    // 시야 좌우 경계선 색
    [SerializeField] private Color arcColor = Color.yellow;       // 부채꼴 외곽선 색
    [SerializeField] private Color targetLineColor = Color.green; // 현재 타겟 연결선 색

    private EnemyMovement enemyMovement; // 같은 오브젝트의 EnemyMovement 참조

    private EnemyMovement VisionSource
    {
        get
        {
            if (enemyMovement == null)
                enemyMovement = GetComponent<EnemyMovement>();

            return enemyMovement;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawMode != GizmoDrawMode.Always)
            return;

        DrawVisionGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (drawMode != GizmoDrawMode.SelectedOnly)
            return;

        DrawVisionGizmo();
    }

    /// <summary>
    /// 현재 설정된 시야 정보를 기준으로 기즈모를 그립니다.
    /// </summary>
    private void DrawVisionGizmo()
    {
        EnemyMovement source = VisionSource;
        if (source == null)
            return;

        // 시야 판정을 시작하는 눈높이 위치
        Vector3 eyePosition = transform.position + Vector3.up * source.EyeHeight;

        // 정면 방향을 수평 기준으로 정리
        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;

        // 정면 방향이 거의 0이면 기본 전방값 사용
        if (flatForward.sqrMagnitude <= 0.001f)
            flatForward = Vector3.forward;

        flatForward.Normalize();

        float halfAngle = source.SightAngle * 0.5f;
        float sightLength = source.SightLength;

        // 좌우 시야 경계 방향 계산
        Vector3 leftDirection = Quaternion.Euler(0.0f, -halfAngle, 0.0f) * flatForward;
        Vector3 rightDirection = Quaternion.Euler(0.0f, halfAngle, 0.0f) * flatForward;

        // 시야 반경 원 표시
        if (drawSightRadius)
        {
            Gizmos.color = radiusColor;
            Gizmos.DrawWireSphere(transform.position, sightLength);
        }

        // 정면 방향 표시
        Gizmos.color = forwardColor;
        Gizmos.DrawLine(eyePosition, eyePosition + flatForward * sightLength);

        // 좌우 시야 경계선 표시
        Gizmos.color = boundaryColor;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * sightLength);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * sightLength);

        // 부채꼴 외곽선 표시
        DrawVisionArc(eyePosition, flatForward, sightLength, source.SightAngle);

        // 플레이 중 현재 타겟을 인식하고 있으면 타겟까지 선 표시
        if (drawTargetLine && Application.isPlaying && source.CurrentTarget != null)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(eyePosition, source.CurrentTarget.position + Vector3.up * source.EyeHeight);
        }
    }

    /// <summary>
    /// 부채꼴 외곽선을 여러 개의 선분으로 나누어 그립니다.
    /// </summary>
    private void DrawVisionArc(Vector3 origin, Vector3 forward, float radius, float angle)
    {
        int segmentCount = Mathf.Max(3, arcSegmentCount);
        float startAngle = -angle * 0.5f;
        float endAngle = angle * 0.5f;

        Gizmos.color = arcColor;

        Vector3 previousPoint = origin + (Quaternion.Euler(0.0f, startAngle, 0.0f) * forward) * radius;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            Vector3 currentPoint = origin + (Quaternion.Euler(0.0f, currentAngle, 0.0f) * forward) * radius;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}