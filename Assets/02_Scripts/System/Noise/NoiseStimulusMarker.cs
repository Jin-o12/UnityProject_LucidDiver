using UnityEngine;

/// <summary>
/// Scene에서 소음이 어디서 났는지 눈으로 확인하기 위한 런타임 마커입니다.
/// 실제 AI 로직은 NoiseStimulus 데이터를 기준으로 동작하고, 이 오브젝트는 위치 앵커와 디버그 표시 역할을 맡습니다.
/// </summary>
public class NoiseStimulusMarker : MonoBehaviour
{
    private NoiseStimulus stimulus;
    private Color debugColor = Color.white;

    public NoiseStimulus Stimulus => stimulus;

    public void Initialize(NoiseStimulus newStimulus, Color newDebugColor)
    {
        stimulus = newStimulus;
        debugColor = newDebugColor;

        transform.position = stimulus.Position;

        // 디버그 마커이므로 소음이 사라지면 자동으로 정리합니다.
        Destroy(gameObject, Mathf.Max(0.05f, stimulus.Duration));
    }

    private void OnDrawGizmos()
    {
        if (stimulus.Radius <= 0.0f)
        {
            return;
        }

        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, stimulus.Radius);
    }
}
