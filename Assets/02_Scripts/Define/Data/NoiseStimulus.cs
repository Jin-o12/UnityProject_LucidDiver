using UnityEngine;

[System.Serializable]
public struct NoiseStimulus
{
    // 이번에 실제로 발생한 소음 1건의 런타임 데이터입니다.
    public NoiseType Type;
    public Vector3 Position;
    public float Radius;
    public float Duration;
    public int Priority;
    public bool CanInterruptChase;
    public float CreatedTime;
    public GameObject Source;
    public Transform AnchorTransform;

    public NoiseStimulus(
        NoiseType type,
        Vector3 position,
        GameObject source,
        float radius = -1.0f,
        float duration = -1.0f,
        bool canInterruptChase = false,
        int priority = -1,
        float createdTime = -1.0f,
        Transform anchorTransform = null)
    {
        Type = type;
        Position = position;
        Radius = radius;
        Duration = duration;
        Priority = priority;
        CanInterruptChase = canInterruptChase;
        CreatedTime = createdTime >= 0.0f ? createdTime : Time.time;
        Source = source;
        AnchorTransform = anchorTransform;
    }
}
