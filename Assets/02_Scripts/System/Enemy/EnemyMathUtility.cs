using UnityEngine;

/// <summary>
/// 적 AI 모듈에서 공통으로 쓰는 평면 계산 유틸리티입니다.
/// Y축 높이 차이를 제외한 거리/방향 계산을 단순화합니다.
/// </summary>
public static class EnemyMathUtility
{
    /// <summary>
    /// Y축을 제외한 평면 거리의 제곱값을 반환합니다.
    /// </summary>
    public static float GetPlanarSqrDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0.0f;
        return delta.sqrMagnitude;
    }

    /// <summary>
    /// Y축을 제외한 평면 방향 벡터를 반환합니다.
    /// </summary>
    public static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0.0f;
        return direction;
    }
}
