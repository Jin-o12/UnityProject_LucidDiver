using UnityEngine;

/// <summary>
/// 런타임에 잠깐 생성되어 "소리가 여기서 났다"는 기준점을 제공하는 오브젝트입니다.
/// 적 AI는 이 오브젝트의 위치를 조사 대상으로 삼을 수 있고,
/// 이후 SoundManager를 붙이면 같은 위치에서 실제 SFX를 재생하는 용도로도 확장할 수 있습니다.
/// </summary>
public class NoiseSourceObject : MonoBehaviour
{
    [Header("Optional Components")]
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRadius = true;

    private NoiseStimulus stimulus;
    private Color debugColor = Color.white;

    public NoiseStimulus Stimulus => stimulus;
    public AudioSource CachedAudioSource => audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 노이즈 매니저가 확정한 소음 데이터를 이 오브젝트에 주입합니다.
    /// </summary>
    public void Initialize(NoiseStimulus newStimulus, Color newDebugColor)
    {
        stimulus = newStimulus;
        debugColor = newDebugColor;

        transform.position = stimulus.Position;

        // 현재는 실제 SFX를 자동 재생하지 않지만, AudioSource는 이후 확장 포인트로 유지합니다.
        Destroy(gameObject, Mathf.Max(0.05f, stimulus.Duration));
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugRadius || stimulus.Radius <= 0.0f)
        {
            return;
        }

        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, stimulus.Radius);
    }
}
