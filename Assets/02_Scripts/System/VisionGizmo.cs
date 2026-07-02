using UnityEngine;
using UnityEngine.Serialization;

public class VisionGizmo : MonoBehaviour
{
    private enum GizmoDrawMode
    {
        SelectedOnly,
        Always
    }

    [Header("Draw Mode")]
    [SerializeField] private GizmoDrawMode drawMode = GizmoDrawMode.SelectedOnly;

    [Header("Gizmo Option")]
    [FormerlySerializedAs("drawSightRadius")]
    [SerializeField] private bool drawAwarenessRadius = true;
    [SerializeField] private bool drawHearingRadius = true;
    [SerializeField] private bool drawTargetLine = true;
    [SerializeField] private int arcSegmentCount = 24;

    [Header("Gizmo Color")]
    [FormerlySerializedAs("radiusColor")]
    [SerializeField] private Color awarenessRadiusColor = Color.white;
    [SerializeField] private Color hearingRadiusColor = new Color(1.0f, 0.55f, 0.0f, 1.0f);
    [SerializeField] private Color forwardColor = Color.yellow;
    [SerializeField] private Color boundaryColor = Color.cyan;
    [SerializeField] private Color arcColor = Color.red;
    [SerializeField] private Color targetLineColor = Color.green;

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

        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        float halfAngle = source.SightAngle * 0.5f;
        float sightLength = source.SightLength;

        Vector3 leftDirection = Quaternion.Euler(0.0f, -halfAngle, 0.0f) * flatForward;
        Vector3 rightDirection = Quaternion.Euler(0.0f, halfAngle, 0.0f) * flatForward;

        if (drawHearingRadius)
        {
            Gizmos.color = hearingRadiusColor;
            Gizmos.DrawWireSphere(transform.position, source.HearingRange);
        }

        if (drawAwarenessRadius)
        {
            Gizmos.color = awarenessRadiusColor;
            Gizmos.DrawWireSphere(transform.position, source.AwarenessRange);
        }

        Gizmos.color = forwardColor;
        Gizmos.DrawLine(eyePosition, eyePosition + flatForward * sightLength);

        Gizmos.color = boundaryColor;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * sightLength);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * sightLength);

        DrawVisionArc(eyePosition, flatForward, sightLength, source.SightAngle);

        if (drawTargetLine && Application.isPlaying && source.CurrentTarget != null)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(eyePosition, source.CurrentTarget.position + Vector3.up * source.EyeHeight);
        }
    }

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
