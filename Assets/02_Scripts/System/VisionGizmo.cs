/// <summary>
/// 적의 시야 범위를 Scene 뷰에서 확인하기 위한 디버그 기즈모 스크립트입니다.
/// EnemyMovement가 가진 시야 거리와 시야각 정보를 읽어와 가시화합니다.
/// </summary>
using UnityEngine;

public class VisionGizmo : MonoBehaviour
{
    private enum GizmoDrawMode
    {
        SelectedOnly,   // 오브젝트를 선택했을 때만 표시
        Always          // Scene 뷰에서 항상 표시
    }

    [Header("Draw Mode")]
    [SerializeField] private GizmoDrawMode drawMode = GizmoDrawMode.SelectedOnly; // 기즈모 표시 방식

    [Header("Gizmo Option")]
    [SerializeField] private bool drawSightRadius = true;     // 시야 반경 원 표시 여부
    [SerializeField] private bool drawTargetLine = true;      // 현재 타겟까지의 선 표시 여부
    [SerializeField] private int arcSegmentCount = 24;        // 부채꼴 외곽선을 얼마나 촘촘히 그릴지 결정

    [Header("Gizmo Color")]
    [SerializeField] private Color radiusColor = Color.white;    // 시야 반경 원 색상
    [SerializeField] private Color forwardColor = Color.yellow;      // 정면 방향선 색상
    [SerializeField] private Color boundaryColor = Color.cyan;    // 좌우 시야 경계선 색상
    [SerializeField] private Color arcColor = Color.red;       // 부채꼴 외곽선 색상
    [SerializeField] private Color targetLineColor = Color.green; // 현재 타겟 연결선 색상

    private EnemyMovement enemyMovement;

    private EnemyMovement VisionSource
    {
        get
        {
            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            return enemyMovement;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawMode != GizmoDrawMode.Always)
        {
            return;
        }

        DrawVisionGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (drawMode != GizmoDrawMode.SelectedOnly)
        {
            return;
        }

        DrawVisionGizmo();
    }

    /// <summary>
    /// 적의 현재 시야 범위를 반경, 경계선, 부채꼴 외곽선으로 그립니다.
    /// </summary>
    private void DrawVisionGizmo()
    {
        EnemyMovement source = VisionSource;
        if (source == null)
        {
            return;
        }

        Vector3 eyePosition = transform.position + Vector3.up * source.EyeHeight;

        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;

        // 정면 벡터 길이가 0에 가까우면 기본값으로 보정합니다.
        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        float halfAngle = source.SightAngle * 0.5f;
        float sightLength = source.SightLength;

        Vector3 leftDirection = Quaternion.Euler(0.0f, -halfAngle, 0.0f) * flatForward;
        Vector3 rightDirection = Quaternion.Euler(0.0f, halfAngle, 0.0f) * flatForward;

        if (drawSightRadius)
        {
            Gizmos.color = radiusColor;
            Gizmos.DrawWireSphere(transform.position, sightLength);
        }

        Gizmos.color = forwardColor;
        Gizmos.DrawLine(eyePosition, eyePosition + flatForward * sightLength);

        Gizmos.color = boundaryColor;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * sightLength);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * sightLength);

        DrawVisionArc(eyePosition, flatForward, sightLength, source.SightAngle);

        // 플레이 중에 실제 타겟을 인식하고 있다면 타겟 연결선도 함께 그려 줍니다.
        if (drawTargetLine && Application.isPlaying && source.CurrentTarget != null)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(eyePosition, source.CurrentTarget.position + Vector3.up * source.EyeHeight);
        }
    }

    /// <summary>
    /// 시야 부채꼴의 외곽선을 여러 개의 선분으로 나누어 그립니다.
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
