using UnityEngine;

/// <summary>
/// 게임 플레이 코드가 소음을 발생시킬 때 사용하는 공용 진입점입니다.
/// 실제 처리와 브로드캐스트는 NoiseManager가 담당합니다.
/// </summary>
public static class NoiseSystem
{
    /// <summary>
    /// 특정 위치에서 소음을 발생시켜 달라고 노이즈 매니저에 요청합니다.
    /// radius / duration / priority를 음수로 넘기면 NoiseManager의 기본값을 사용합니다.
    /// </summary>
    public static void Emit(
        NoiseType type,
        Vector3 position,
        GameObject source = null,
        float radius = -1.0f,
        float duration = -1.0f,
        bool canInterruptChase = false,
        int priority = -1)
    {
        GlobalEventBus.OnNoiseRequested?.Invoke(new NoiseStimulus(
            type,
            position,
            source,
            radius,
            duration,
            canInterruptChase,
            priority,
            Time.time));
    }
}
