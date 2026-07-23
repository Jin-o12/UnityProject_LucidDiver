using UnityEngine;

/// <summary>
/// VFX 재생 위치와 선택적인 부모 Transform을 전달하는 값 객체입니다.
/// 게임 로직은 프리팹을 알 필요 없이 VFX ID와 이 정보만 전달합니다.
/// </summary>
public readonly struct VFXContext
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Transform Parent { get; }

    private VFXContext(Vector3 position, Quaternion rotation, Transform parent)
    {
        Position = position;
        Rotation = rotation;
        Parent = parent;
    }

    public static VFXContext At(Vector3 position)
    {
        return new VFXContext(position, Quaternion.identity, null);
    }

    public static VFXContext At(Vector3 position, Quaternion rotation)
    {
        return new VFXContext(position, rotation, null);
    }

    public static VFXContext AttachedTo(Transform parent)
    {
        return new VFXContext(Vector3.zero, Quaternion.identity, parent);
    }

    public static VFXContext AttachedTo(Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        return new VFXContext(localPosition, localRotation, parent);
    }
}
